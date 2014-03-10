using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace gMKVToolnix
{
    public class gMKVMerge
    {
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
            _MKVMergeFilename = Path.Combine(_MKVToolnixPath, "mkvmerge.exe");            
        }

        public List<gMKVSegment> GetMKVSegments(String argMKVFile)
        {
            // check for existence of MKVMerge
            if (!File.Exists(_MKVMergeFilename)) { throw new Exception("Could not find mkvmerge.exe!\r\n" + _MKVMergeFilename); }
            // First clear the segment list
            _SegmentList.Clear();
            // Clear the mkvmerge output
            _MKVMergeOutput.Length = 0;
            // Clear the error builder
            _ErrorBuilder.Length = 0;

            using (Process myProcess = new Process())
            {
                List<OptionValue> optionList = new List<OptionValue>();
                optionList.Add(new OptionValue(MkvMergeOptions.ui_language, "en"));
                //optionList.Add(new OptionValue(MkvInfoOptions.gui_mode, String.Empty));
                //optionList.Add(new OptionValue(MkvMergeOptions.command_line_charset, "\"UFT-8\""));
                //optionList.Add(new OptionValue(MkvMergeOptions.output_charset, "\"UFT-8\""));
                optionList.Add(new OptionValue(MkvMergeOptions.identify_verbose, String.Empty));

                ProcessStartInfo myProcessInfo = new ProcessStartInfo();
                myProcessInfo.FileName = _MKVMergeFilename;
                //myProcessInfo.Arguments = " -?";
                myProcessInfo.Arguments = String.Format("{0} \"{1}\"", ConvertOptionValueListToString(optionList), argMKVFile);
                //myProcessInfo.Arguments = String.Format("--parse-mode fast \"{0}\"", argMKVFile);
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
                Debug.WriteLine("Exit code: " + myProcess.ExitCode);

                // Check the exit code
                if (myProcess.ExitCode > 0)
                {
                    // something went wrong!
                    throw new Exception(String.Format("Mkvmerge exited with error code {0}!\r\n\r\nErrors reported:\r\n{1}",
                        myProcess.ExitCode, _ErrorBuilder.ToString()));
                }
                // Start the parsing of the output
                ParseMkvMergeOutput();
                return _SegmentList;
            }
        }

        private void ParseMkvMergeOutput()
        {
            // start the loop for each line of the output
            foreach (String outputLine in _MKVMergeOutput.ToString().Split(new String[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (outputLine.StartsWith("Track ID "))
                {
                    gMKVTrack tmp = new gMKVTrack();
                    tmp.TrackType = (MkvTrackType)Enum.Parse(typeof(MkvTrackType), outputLine.Substring(outputLine.IndexOf(":") + 1, outputLine.IndexOf("(") - outputLine.IndexOf(":") - 1).Trim());
                    tmp.TrackID = Int32.Parse(outputLine.Substring(0, outputLine.IndexOf(":")).Replace("Track ID", String.Empty).Trim());
                    tmp.TrackNumber = Int32.Parse(ExtractProperty(outputLine, "number"));
                    tmp.CodecID = ExtractProperty(outputLine, "codec_id");
                    if (outputLine.Contains("language:"))
                    {
                        tmp.Language = ExtractProperty(outputLine, "language");
                    }
                    if (outputLine.Contains("track_name:"))
                    {
                        tmp.TrackName = ExtractProperty(outputLine, "track_name"); 
                    }
                    switch (tmp.TrackType)
                    {
                        case MkvTrackType.video:
                            if (outputLine.Contains("pixel_dimensions:"))
                            {
                                tmp.ExtraInfo = ExtractProperty(outputLine, "pixel_dimensions"); 
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
                    tmp.Filename = outputLine.Substring(outputLine.IndexOf("file name")).Replace("file name", string.Empty).Replace("'", String.Empty).Trim();
                    tmp.FileSize = outputLine.Substring(outputLine.IndexOf("size")).Replace("size", string.Empty).Split(new String[] { "," }, StringSplitOptions.RemoveEmptyEntries)[0].Replace("bytes", String.Empty).Trim();
                    tmp.MimeType = outputLine.Substring(outputLine.IndexOf("type")).Replace("type", string.Empty).Split(new String[] { "," }, StringSplitOptions.RemoveEmptyEntries)[0].Replace("'", String.Empty).Trim();
                    _SegmentList.Add(tmp);
                }
                else if (outputLine.StartsWith("Chapters: "))
                {
                    gMKVChapter tmp = new gMKVChapter();
                    tmp.ChapterCount = Int32.Parse(outputLine.Replace("Chapters: ", string.Empty).Replace("entries", string.Empty).Trim());
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
            return line.Substring(line.IndexOf(propertyName + ":")).Substring(0, line.Substring(line.IndexOf(propertyName + ":")).IndexOf(endCharacter)).Replace(propertyName + ":", String.Empty).Trim();            
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
