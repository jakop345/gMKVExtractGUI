using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace gMKVToolNix
{
    public class gMKVMerge
    {
        static readonly string[] formats = { 
            // Basic formats
            "yyyyMMddTHHmmsszzz",
            "yyyyMMddTHHmmsszz",
            "yyyyMMddTHHmmssZ",
            // Extended formats
            "yyyy-MM-ddTHH:mm:sszzz",
            "yyyy-MM-ddTHH:mm:sszz",
            "yyyy-MM-ddTHH:mm:ssZ",
            // All of the above with reduced accuracy
            "yyyyMMddTHHmmzzz",
            "yyyyMMddTHHmmzz",
            "yyyyMMddTHHmmZ",
            "yyyy-MM-ddTHH:mmzzz",
            "yyyy-MM-ddTHH:mmzz",
            "yyyy-MM-ddTHH:mmZ",
            // Accuracy reduced to hours
            "yyyyMMddTHHzzz",
            "yyyyMMddTHHzz",
            "yyyyMMddTHHZ",
            "yyyy-MM-ddTHHzzz",
            "yyyy-MM-ddTHHzz",
            "yyyy-MM-ddTHHZ",
            // 
            "yyyyMMdd",
            "yyyyMMddT",
            "yyyy-MM-dd",
            "yyyy-MM-ddT"
        };

        internal class OptionValue
        {
            private MkvMergeOptions _Option;
            private String _Parameter;

            public MkvMergeOptions Option
            {
                get { return _Option; }
                set { _Option = value; }
            }

            public String Parameter
            {
                get { return _Parameter; }
                set { _Parameter = value; }
            }

            public OptionValue(MkvMergeOptions opt, String par)
            {
                _Option = opt;
                _Parameter = par;
            }
        }

        /// <summary>
        /// Gets the mkvinfo executable filename
        /// </summary>
        public static String MKV_MERGE_FILENAME
        {
            get { return gMKVHelper.IsOnLinux ? "mkvmerge" : "mkvmerge.exe"; }
        }

        private String _MKVToolnixPath = String.Empty;
        private String _MKVMergeFilename = String.Empty;
        private List<gMKVSegment> _SegmentList = new List<gMKVSegment>();
        private StringBuilder _MKVMergeOutput = new StringBuilder();
        private StringBuilder _ErrorBuilder = new StringBuilder();

        public enum MkvMergeOptions
        {
            identify, // Will let mkvmerge(1) probe the single file and report its type, the tracks contained in the file and their track IDs. If this option is used then the only other option allowed is the filename. 
            identify_verbose, // Will let mkvmerge(1) probe the single file and report its type, the tracks contained in the file and their track IDs. If this option is used then the only other option allowed is the filename. 
            ui_language, //Forces the translations for the language code to be used 
            command_line_charset,
            output_charset
        }

        public gMKVMerge(String mkvToonlixPath)
        {
            _MKVToolnixPath = mkvToonlixPath;
            _MKVMergeFilename = Path.Combine(_MKVToolnixPath, MKV_MERGE_FILENAME);            
        }

        public List<gMKVSegment> GetMKVSegments(String argMKVFile)
        {
            // check for existence of MKVMerge
            if (!File.Exists(_MKVMergeFilename)) { throw new Exception(String.Format("Could not find {0}!\r\n{1}", MKV_MERGE_FILENAME, _MKVMergeFilename)); }
            // First clear the segment list
            _SegmentList.Clear();
            // Clear the mkvmerge output
            _MKVMergeOutput.Length = 0;
            // Clear the error builder
            _ErrorBuilder.Length = 0;
            // Execute the mkvmerge
            ExecuteMkvMerge(argMKVFile);
            // Start the parsing of the output
            ParseMkvMergeOutput();
            return _SegmentList;
        }

        public bool FindDelays(List<gMKVSegment> argSegmentList)
        {
            // Check to see if the list contains segments
            if(argSegmentList == null || argSegmentList.Count == 0)
            {
                return false;
            }
            
            Int32 videoDelay = Int32.MinValue;

            // First, find the video delay
            foreach (gMKVSegment seg in argSegmentList)
            {
                if (seg is gMKVTrack)
                {
                    gMKVTrack track = (gMKVTrack)seg;
                    if(track.TrackType == MkvTrackType.video)
                    {
                        // Check if MinimumTimestamp property was found
                        if (track.MinimumTimestamp == Int64.MinValue)
                        {
                            // Could not determine the delays
                            return false;
                        }
                        // Convert to ms from ns
                        videoDelay = Convert.ToInt32(track.MinimumTimestamp / 1000000);
                        track.Delay = videoDelay;
                        track.EffectiveDelay = videoDelay;
                        break;
                    }
                }
            }

            // Check if video delay was found
            if (videoDelay == Int32.MinValue)
            {
                // Could not determine the delays
                return false;
            }

            // If video delay was found, then determine the audio track delay
            foreach (gMKVSegment seg in argSegmentList)
            {
                if (seg is gMKVTrack)
                {
                    gMKVTrack track = (gMKVTrack)seg;
                    if (track.TrackType == MkvTrackType.audio)
                    {
                        // Check if MinimumTimestamp property was found
                        if (track.MinimumTimestamp == Int64.MinValue)
                        {
                            // Could not determine the delays
                            return false;
                        }
                        // Convert to ms from ns
                        track.Delay = Convert.ToInt32(track.MinimumTimestamp / 1000000);
                        track.EffectiveDelay = track.Delay - videoDelay;
                    }
                }
            }

            // If everything went all right, then we return true
            return true;
        }

        private byte[] HexStringToByteArray(string hexString)
        {
            if (hexString.Length % 2 == 1)
            {
                throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "The binary key cannot have an odd number of digits: {0}", hexString));
            }

            byte[] HexAsBytes = new byte[hexString.Length / 2];
            for (int index = 0; index < HexAsBytes.Length; index++)
            {
                string byteValue = hexString.Substring(index * 2, 2);
                HexAsBytes[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return HexAsBytes;
        }

        public void FindCodecPrivate(List<gMKVSegment> argSegmentList)
        {
            foreach (gMKVSegment seg in argSegmentList)
            {
                if (seg is gMKVTrack)
                {
                    gMKVTrack track = (gMKVTrack)seg;
                    // Check if the track has CodecPrivateData
                    // and it doesn't have a text representation of CodecPrivate
                    if (!String.IsNullOrEmpty(track.CodecPrivateData)
                        && String.IsNullOrEmpty(track.CodecPrivate))
                    {
                        byte[] codecPrivateBytes = HexStringToByteArray(track.CodecPrivateData);
                        if (track.TrackType == MkvTrackType.video)
                        {
                            if (track.CodecID == "V_MS/VFW/FOURCC")
                            {
                                track.CodecPrivate = String.Format("length {0} (FourCC: \"{1}\")"
                                    , codecPrivateBytes.Length
                                    , ((32 <= codecPrivateBytes[16]) && (127 > codecPrivateBytes[16]) ? System.Text.Encoding.ASCII.GetString(new byte[] { codecPrivateBytes[16] }) : "?") +
                                    ((32 <= codecPrivateBytes[17]) && (127 > codecPrivateBytes[17]) ? System.Text.Encoding.ASCII.GetString(new byte[] { codecPrivateBytes[17] }) : "?") +
                                    ((32 <= codecPrivateBytes[18]) && (127 > codecPrivateBytes[18]) ? System.Text.Encoding.ASCII.GetString(new byte[] { codecPrivateBytes[18] }) : "?") +
                                    ((32 <= codecPrivateBytes[19]) && (127 > codecPrivateBytes[19]) ? System.Text.Encoding.ASCII.GetString(new byte[] { codecPrivateBytes[19] }) : "?")
                                );
                            }
                            else if (track.CodecID == "V_MPEG4/ISO/AVC")
                            {
                                Int32 profileIdc = codecPrivateBytes[1];
                                Int32 levelIdc = codecPrivateBytes[3];

                                String profileIdcString = "";

                                switch (profileIdc)
                                {
                                    case 44:
                                        profileIdcString = "CAVLC 4:4:4 Intra";
                                        break;
                                    case 66:
                                        profileIdcString = "Baseline";
                                        break;
                                    case 77:
                                        profileIdcString = "Main";
                                        break;
                                    case 83:
                                        profileIdcString = "Scalable Baseline";
                                        break;
                                    case 86:
                                        profileIdcString = "Scalable High";
                                        break;
                                    case 88:
                                        profileIdcString = "Extended";
                                        break;
                                    case 100:
                                        profileIdcString = "High";
                                        break;
                                    case 110:
                                        profileIdcString = "High 10";
                                        break;
                                    case 118:
                                        profileIdcString = "Multiview High";
                                        break;
                                    case 122:
                                        profileIdcString = "High 4:2:2";
                                        break;
                                    case 128:
                                        profileIdcString = "Stereo High";
                                        break;
                                    case 144:
                                        profileIdcString = "High 4:4:4";
                                        break;
                                    case 244:
                                        profileIdcString = "High 4:4:4 Predictive";
                                        break;
                                    default:
                                        profileIdcString = "Unknown";
                                        break;
                                }

                                track.CodecPrivate = String.Format("length {0} (h.264 profile: {1} @L{2}.{3})"
                                    , codecPrivateBytes.Length
                                    , profileIdcString
                                    , levelIdc / 10
                                    , levelIdc % 10
                                );
                            }
                            else if (track.CodecID == "V_MPEGH/ISO/HEVC")
                            {
                                BitArray codecPrivateBits = new BitArray(new byte[] { codecPrivateBytes[1] });

                                Int32 profileIdc = Convert.ToInt32((codecPrivateBits[4] ? "1" : "0") +
                                    (codecPrivateBits[3] ? "1" : "0") +
                                    (codecPrivateBits[2] ? "1" : "0") +
                                    (codecPrivateBits[1] ? "1" : "0") +
                                    (codecPrivateBits[0] ? "1" : "0"), 2);

                                Int32 levelIdc = codecPrivateBytes[12];

                                String profileIdcString = "";

                                switch (profileIdc)
                                {
                                    case 1:
                                        profileIdcString = "Main";
                                        break;
                                    case 2:
                                        profileIdcString = "Main 10";
                                        break;
                                    case 3:
                                        profileIdcString = "Main Still Picture";
                                        break;
                                    default:
                                        profileIdcString = "Unknown";
                                        break;
                                }

                                track.CodecPrivate = String.Format("length {0} (HEVC profile: {1} @L{2}.{3})"
                                    , codecPrivateBytes.Length
                                    , profileIdcString
                                    , levelIdc / 3 / 10
                                    , levelIdc / 3 % 10
                                );
                            }
                            else
                            {
                                track.CodecPrivate = String.Format("length {0}", codecPrivateBytes.Length);
                            }
                        }
                        else if (track.TrackType == MkvTrackType.audio)
                        {
                            if (track.CodecID == "A_MS/ACM")
                            {
                                //UInt16 formatTag = BitConverter.ToUInt16(new byte[] { codecPrivateBytes[1], codecPrivateBytes[0] }, 0);
                                track.CodecPrivate = String.Format("length {0} (format tag: 0x{1:x2}{2:x2})"
                                    , codecPrivateBytes.Length
                                    , codecPrivateBytes[0]
                                    , codecPrivateBytes[1]
                                );
                            }
                            else
                            {
                                track.CodecPrivate = String.Format("length {0}", codecPrivateBytes.Length);
                            }
                        }
                        else
                        {
                            track.CodecPrivate = String.Format("length {0}", codecPrivateBytes.Length);
                        }
                    }
                }
            }
        }

        private void ExecuteMkvMerge(String argMKVFile)
        {
            using (Process myProcess = new Process())
            {
                List<OptionValue> optionList = new List<OptionValue>();
                // if on Linux, the language output must be defined from the environment variables LC_ALL, LANG, and LC_MESSAGES
                if (!gMKVHelper.IsOnLinux)
                {
                    optionList.Add(new OptionValue(MkvMergeOptions.ui_language, "en"));
                }                
                //optionList.Add(new OptionValue(MkvMergeOptions.command_line_charset, "\"UTF-8\""));
                //optionList.Add(new OptionValue(MkvMergeOptions.output_charset, "\"UTF-8\""));
                optionList.Add(new OptionValue(MkvMergeOptions.identify_verbose, String.Empty));

                ProcessStartInfo myProcessInfo = new ProcessStartInfo();
                myProcessInfo.FileName = _MKVMergeFilename;
                myProcessInfo.Arguments = String.Format("{0} \"{1}\"", ConvertOptionValueListToString(optionList), argMKVFile);
                myProcessInfo.UseShellExecute = false;
                myProcessInfo.RedirectStandardOutput = true;
                myProcessInfo.StandardOutputEncoding = Encoding.UTF8;
                myProcessInfo.RedirectStandardError = true;
                myProcessInfo.StandardErrorEncoding = Encoding.UTF8;
                myProcessInfo.CreateNoWindow = true;
                myProcessInfo.WindowStyle = ProcessWindowStyle.Hidden;
                myProcess.StartInfo = myProcessInfo;
                myProcess.OutputDataReceived += myProcess_OutputDataReceived;

                Debug.WriteLine(myProcessInfo.Arguments);
                // Start the mkvinfo process
                myProcess.Start();
                // Start reading the output
                myProcess.BeginOutputReadLine();
                // Wait for the process to exit
                myProcess.WaitForExit();
                // unregister the event
                myProcess.OutputDataReceived -= myProcess_OutputDataReceived;

                // Debug write the exit code
                Debug.WriteLine(String.Format("Exit code: {0}", myProcess.ExitCode));

                // Check the exit code
                // ExitCode 1 is for warnings only, so ignore it
                if (myProcess.ExitCode > 1)
                {
                    // something went wrong!
                    throw new Exception(String.Format("Mkvmerge exited with error code {0}!\r\n\r\nErrors reported:\r\n{1}",
                        myProcess.ExitCode, _ErrorBuilder.ToString()));
                }
            }
        }

        private void ParseMkvMergeOutput()
        {
            // start the loop for each line of the output
            foreach (String outputLine in _MKVMergeOutput.ToString().Split(new String[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (outputLine.StartsWith("File "))
                {
                    gMKVSegmentInfo tmp = new gMKVSegmentInfo();
                    if (outputLine.Contains("muxing_application:"))
                    {
                        tmp.MuxingApplication = ExtractProperty(outputLine, "muxing_application");
                    }
                    if (outputLine.Contains("writing_application:"))
                    {
                        tmp.WritingApplication = ExtractProperty(outputLine, "writing_application");
                    }
                    if (outputLine.Contains("date_utc:"))
                    {
                        tmp.Date = DateTime.ParseExact(ExtractProperty(outputLine, "date_utc"), formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToUniversalTime().
                            ToString("ddd MMM dd HH:mm:ss yyyy UTC", CultureInfo.InvariantCulture);
                    }
                    if (outputLine.Contains("duration:"))
                    {
                        //Duration: 5979.008s (01:39:39.008)
                        String originalDuration = ExtractProperty(outputLine, "duration");
                        TimeSpan tmpTime = TimeSpan.FromMilliseconds(Convert.ToDouble(Int64.Parse(originalDuration)) / 1000000.0);
                        tmp.Duration = String.Format("{0}s ({1}:{2}:{3}.{4})",
                            (Convert.ToDouble(Int64.Parse(originalDuration)) / 1000000000.0).ToString("#0.000", CultureInfo.InvariantCulture),
                            tmpTime.Hours.ToString("00"),
                            tmpTime.Minutes.ToString("00"),
                            tmpTime.Seconds.ToString("00"),
                            tmpTime.Milliseconds.ToString("000"));
                    }
                    if (!String.IsNullOrEmpty(tmp.MuxingApplication)
                        && !String.IsNullOrEmpty(tmp.WritingApplication)
                        && !String.IsNullOrEmpty(tmp.Duration))
                    {
                        _SegmentList.Add(tmp);
                    }
                }
                else if (outputLine.StartsWith("Track ID "))
                {
                    Int32 trackID = Int32.Parse(outputLine.Substring(0, outputLine.IndexOf(":")).Replace("Track ID", String.Empty).Trim());
                    // Check if there is already a track with the same TrackID (workaround for a weird bug in MKVToolnix v4 when identifying files from AviDemux)
                    bool trackFound = false;
                    foreach (gMKVSegment tmpSeg in _SegmentList)
                    {
                        if(tmpSeg is gMKVTrack && ((gMKVTrack)tmpSeg).TrackID == trackID)
                        {
                            // If we already have a track with the same trackID, then don't bother adding it again
                            trackFound = true;
                            break;
                        }
                    }
                    // Check if track found
                    if (trackFound)
                    {
                        // If we already have a track with the same trackID, then don't bother adding it again
                        continue;
                    }
                    gMKVTrack tmp = new gMKVTrack();
                    tmp.TrackType = (MkvTrackType)Enum.Parse(typeof(MkvTrackType), outputLine.Substring(outputLine.IndexOf(":") + 1, outputLine.IndexOf("(") - outputLine.IndexOf(":") - 1).Trim());
                    tmp.TrackID = trackID;
                    if (outputLine.Contains("number"))
                    {
                        // if we have version 5.x and newer
                        tmp.TrackNumber = Int32.Parse(ExtractProperty(outputLine, "number"));
                    }
                    else
                    {
                        // if we have version 4.x and older
                        tmp.TrackNumber = tmp.TrackID;
                    }
                    if (outputLine.Contains("codec_id"))
                    {
                        // if we have version 5.x and newer
                        tmp.CodecID = ExtractProperty(outputLine, "codec_id");
                    }
                    else
                    {
                        // if we have version 4.x and older
                        tmp.CodecID = outputLine.Substring(outputLine.IndexOf("(") + 1, outputLine.IndexOf(")") - outputLine.IndexOf("(") - 1);
                    }

                    if (outputLine.Contains("language:"))
                    {
                        tmp.Language = ExtractProperty(outputLine, "language");
                    }
                    if (outputLine.Contains("track_name:"))
                    {
                        tmp.TrackName = ExtractProperty(outputLine, "track_name"); 
                    }
                    if (outputLine.Contains("codec_private_data:"))
                    {
                        tmp.CodecPrivateData = ExtractProperty(outputLine, "codec_private_data");
                    }
                    switch (tmp.TrackType)
                    {
                        case MkvTrackType.video:
                            if (outputLine.Contains("pixel_dimensions:"))
                            {
                                tmp.ExtraInfo = ExtractProperty(outputLine, "pixel_dimensions"); 
                            }
                            // in versions after v9.0.1, Mosu was kind enough to provide us with the minimum_timestamp property
                            // in order to determine the current track's delay
                            if (outputLine.Contains("minimum_timestamp:"))
                            {
                                Int64 tmpInt64;
                                if (Int64.TryParse(ExtractProperty(outputLine, "minimum_timestamp"), out tmpInt64))
                                {
                                    tmp.MinimumTimestamp = tmpInt64;
                                }
                            }
                            break;
                        case MkvTrackType.audio:
                            if (outputLine.Contains("audio_sampling_frequency:"))
                            {
                                tmp.ExtraInfo = ExtractProperty(outputLine, "audio_sampling_frequency"); 
                            }
                            if (outputLine.Contains("audio_channels:"))
                            {
                                tmp.ExtraInfo += ", Ch:" + ExtractProperty(outputLine, "audio_channels");
                            }
                            // in versions after v9.0.1, Mosu was kind enough to provide us with the minimum_timestamp property
                            // in order to determine the current track's delay
                            if (outputLine.Contains("minimum_timestamp:"))
                            {
                                Int64 tmpInt64;
                                if (Int64.TryParse(ExtractProperty(outputLine, "minimum_timestamp"), out tmpInt64))
                                {
                                    tmp.MinimumTimestamp = tmpInt64;
                                }
                            }
                            break;
                        case MkvTrackType.subtitles:
                            break;
                        default:
                            break;
                    }
                    _SegmentList.Add(tmp);
                }
                else if (outputLine.StartsWith("Attachment ID "))
                {
                    gMKVAttachment tmp = new gMKVAttachment();
                    tmp.ID = Int32.Parse(outputLine.Substring(0, outputLine.IndexOf(":")).Replace("Attachment ID", String.Empty).Trim());
                    tmp.Filename = outputLine.Substring(outputLine.IndexOf("file name")).Replace("file name", string.Empty);
                    tmp.Filename = tmp.Filename.Substring(tmp.Filename.IndexOf("'") + 1, tmp.Filename.LastIndexOf("'") - 2).Trim();
                    tmp.FileSize = outputLine.Substring(outputLine.IndexOf("size")).Replace("size", string.Empty).Split(new String[] { "," }, StringSplitOptions.RemoveEmptyEntries)[0].Replace("bytes", String.Empty).Trim();
                    tmp.MimeType = outputLine.Substring(outputLine.IndexOf("type")).Replace("type", string.Empty).Split(new String[] { "," }, StringSplitOptions.RemoveEmptyEntries)[0].Replace("'", String.Empty).Trim();
                    _SegmentList.Add(tmp);
                }
                else if (outputLine.StartsWith("Chapters: "))
                {
                    gMKVChapter tmp = new gMKVChapter();
                    tmp.ChapterCount = Int32.Parse(outputLine.Replace("Chapters: ", string.Empty).Replace("entry", String.Empty).Replace("entries", string.Empty).Trim());
                    _SegmentList.Add(tmp);
                }
            }
        }

        private String ExtractProperty(String line, String propertyName)
        {
            String endCharacter = String.Empty;
            if (line.Substring(line.IndexOf(propertyName + ":")).Contains(" "))
            {
                endCharacter = " ";
            }
            else
            {
                endCharacter = "]";
            }
            return gMKVHelper.UnescapeString(line.Substring(line.IndexOf(propertyName + ":")).
                Substring(0, line.Substring(line.IndexOf(propertyName + ":")).IndexOf(endCharacter)).
                Replace(propertyName + ":", String.Empty)).
                Trim();            
        }

        private void myProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                if (e.Data.Trim().Length > 0)
                {
                    // add the line to the output stringbuilder
                    _MKVMergeOutput.AppendLine(e.Data);
                    // check for errors
                    if (e.Data.Contains("Error:"))
                    {
                        _ErrorBuilder.AppendLine(e.Data.Substring(e.Data.IndexOf(":") + 1).Trim());
                    }
                    // debug write the output line
                    Debug.WriteLine(e.Data);
                    // log the output
                    gMKVLogger.Log(e.Data);
                }
            }
        }

        private String ConvertOptionValueListToString(List<OptionValue> listOptionValue)
        {
            StringBuilder optionString = new StringBuilder();
            foreach (OptionValue optVal in listOptionValue)
            {
                optionString.AppendFormat(" {0} {1}", ConvertEnumOptionToStringOption(optVal.Option), optVal.Parameter);
            }
            return optionString.ToString();
        }

        private String ConvertEnumOptionToStringOption(MkvMergeOptions enumOption)
        {
            return String.Format("--{0}", Enum.GetName(typeof(MkvMergeOptions), enumOption).Replace("_", "-"));
        }

    }
}
