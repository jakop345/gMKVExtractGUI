using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace gMKVToolnix
{
    public enum MkvExtractModes
    {
        tracks,
        tags,
        attachments,
        chapters,
        cuesheet,
        timecodes_v2
    }

    public enum MkvExtractGlobalOptions
    {
        parse_fully,
        verbose,
        quiet,
        ui_language,
        command_line_charset,
        output_charset,
        redirect_output,
        help,
        version,
        check_for_updates
    }

    public enum TimecodesExtractionMode
    {
        NoTimecodes,
        WithTimecodes,
        OnlyTimecodes
    }

    public delegate void MkvExtractProgressUpdatedEventHandler(Int32 progress);
    public delegate void MkvExtractTrackUpdatedEventHandler(String trackName);

    public class gMKVExtract
    {
        private String _MKVToolnixPath = String.Empty;
        private String _MKVExtractFilename = String.Empty;
        private StringBuilder _MKVExtractOutput = new StringBuilder();
        private StreamWriter _ChapterWriter = null;
        private StringBuilder _ErrorBuilder = new StringBuilder();

        public event MkvExtractProgressUpdatedEventHandler MkvExtractProgressUpdated;
        public event MkvExtractTrackUpdatedEventHandler MkvExtractTrackUpdated;

        private Exception _ThreadedException = null;
        public Exception ThreadedException { get { return _ThreadedException; } }
        private bool _Abort = false;
        public bool Abort
        {
            get { return _Abort; }
            set { _Abort = value; }
        }

        private bool _AbortAll = false;
        public bool AbortAll
        {
            get { return _AbortAll; }
            set { _AbortAll = value; }
        }

        public gMKVExtract(String mkvToonlixPath)
        {
            _MKVToolnixPath = mkvToonlixPath;
            _MKVExtractFilename = Path.Combine(_MKVToolnixPath, "mkvextract.exe");
        }

        public void ExtractMKVSegmentsThreaded(Object parameters)
        {
            _ThreadedException = null;
            _Abort = false;
            _AbortAll = false;
            try
            {
                List<Object> objParameters = (List<Object>)parameters;
                ExtractMKVSegments((String)objParameters[0],
                    (List<gMKVSegment>)objParameters[1],
                    (String)objParameters[2],
                    (MkvChapterTypes)objParameters[3],
                    (TimecodesExtractionMode)objParameters[4]);
            }
            catch (Exception ex)
            {
                _ThreadedException = ex;
            }
        }

        public void ExtractMKVSegments(String argMKVFile, List<gMKVSegment> argMKVSegmentsToExtract, 
            String argOutputDirectory, MkvChapterTypes argChapterType, TimecodesExtractionMode argTimecodesExtractionMode)
        {
            _Abort = false;
            _AbortAll = false;
            _ErrorBuilder.Length = 0;
            _MKVExtractOutput.Length = 0;
            foreach (gMKVSegment seg in argMKVSegmentsToExtract)
            {
                if (_AbortAll)
                {
                    _ErrorBuilder.AppendLine("User aborted all the processes!");
                    break;
                }
                try
                {
                    String trackName = String.Empty;
                    String par = String.Empty;
                    String chapFile = String.Empty;
                    if (seg is gMKVTrack)
                    {
                        trackName = "Track " + ((gMKVTrack)seg).TrackNumber.ToString();
                        Double audioDelay = 0;
                        String outputFileExtension = String.Empty;
                        String extraOutputPart = String.Empty;
                        String parTimecodes = String.Empty;
                        String timecodesFilename = String.Empty;
                        bool timecodesExtracted = false;

                        timecodesFilename = Path.Combine(argOutputDirectory,
                                Path.GetFileNameWithoutExtension(argMKVFile) + "_track" + ((gMKVTrack)seg).TrackNumber +
                            "_" + ((gMKVTrack)seg).Language + ".tc.txt");

                        parTimecodes = String.Format("timecodes_v2 \"{0}\" {1}:\"{2}\"", argMKVFile,
                            ((gMKVTrack)seg).TrackID, timecodesFilename);
                        
                        switch (((gMKVTrack)seg).TrackType)
                        {
                            case MkvTrackType.video:
                                if (((gMKVTrack)seg).CodecID.ToUpper().Contains("V_MS/VFW/FOURCC"))
                                {
                                    outputFileExtension = "avi";
                                }
                                else if (((gMKVTrack)seg).CodecID.ToUpper().Contains("V_UNCOMPRESSED"))
                                {
                                    outputFileExtension = "raw";
                                }
                                else if (((gMKVTrack)seg).CodecID.ToUpper().Contains("V_MPEG4/ISO/"))
                                {
                                    outputFileExtension = "avc";
                                }
                                else if (((gMKVTrack)seg).CodecID.ToUpper().Contains("V_MPEG4/MS/V3"))
                                {
                                    outputFileExtension = "mp4";
                                }
                                else if (((gMKVTrack)seg).CodecID.ToUpper().Contains("V_MPEG1"))
                                {
                                    outputFileExtension = "mpg";
                                }
                                else if (((gMKVTrack)seg).CodecID.ToUpper().Contains("V_MPEG2"))
                                {
                                    outputFileExtension = "mpg";
                                }
                                else if (((gMKVTrack)seg).CodecID.ToUpper().Contains("V_REAL/"))
                                {
                                    outputFileExtension = "rm";
                                }
                                else if (((gMKVTrack)seg).CodecID.ToUpper().Contains("V_QUICKTIME"))
                                {
                                    outputFileExtension = "mov";
                                }
                                else if (((gMKVTrack)seg).CodecID.ToUpper().Contains("V_THEORA"))
                                {
                                    outputFileExtension = "ogg";
                                }
                                else if (((gMKVTrack)seg).CodecID.ToUpper().Contains("V_PRORES"))
                                {
                                    outputFileExtension = "mov";
                                }
                                else if (((gMKVTrack)seg).CodecID.ToUpper().Contains("V_VP"))
                                {
                                    outputFileExtension = "ivf";
                                }
                                else
                                {
                                    outputFileExtension = "mkv";
                                }
                                break;
                            case MkvTrackType.audio:
                                // extract timecodes to find the delay
                                OnMkvExtractTrackUpdated(trackName + " (timecodes)");

                                ExtractMkvSegment(argMKVFile, parTimecodes, String.Empty);
                                timecodesExtracted = true;

                                // Now check the timecode file and find the first time
                                using (StreamReader sr = new StreamReader(timecodesFilename))
                                {
                                    String line = String.Empty;
                                    while ((line = sr.ReadLine()) != null)
                                    {
                                        if (!line.StartsWith("#"))
                                        {
                                            audioDelay = Double.Parse(line.Trim(), System.Globalization.CultureInfo.InvariantCulture);
                                            break;
                                        }
                                    }
                                }
                                // if no timecodes where asked from the user, delete the file
                                if (argTimecodesExtractionMode == TimecodesExtractionMode.NoTimecodes)
                                {
                                    File.Delete(timecodesFilename);
                                }
                                // add the delay to the extraOutput for the track filename
                                extraOutputPart = " DELAY " + audioDelay.ToString(System.Globalization.CultureInfo.InvariantCulture) + "ms";

                                if (((gMKVTrack)seg).CodecID.ToUpper().Contains("A_MPEG/L3"))
                                {
                                    outputFileExtension = "mp3";
                                }
                                else if (((gMKVTrack)seg).CodecID.ToUpper().Contains("A_MPEG/L2"))
                                {
                                    outputFileExtension = "mp2";
                                }
                                else if (((gMKVTrack)seg).CodecID.ToUpper().Contains("A_MPEG/L1"))
                                {
                                    outputFileExtension = "mpa";
                                }
                                else if (((gMKVTrack)seg).CodecID.ToUpper().Contains("A_PCM"))
                                {
                                    outputFileExtension = "wav";
                                }
                                else if (((gMKVTrack)seg).CodecID.ToUpper().Contains("A_MPC"))
                                {
                                    outputFileExtension = "mpc";
                                }
                                else if (((gMKVTrack)seg).CodecID.ToUpper().Contains("A_AC3"))
                                {
                                    outputFileExtension = "ac3";
                                }
                                else if (((gMKVTrack)seg).CodecID.ToUpper().Contains("A_ALAC"))
                                {
                                    outputFileExtension = "caf";
                                }
                                else if (((gMKVTrack)seg).CodecID.ToUpper().Contains("A_DTS"))
                                {
                                    outputFileExtension = "dts";
                                }
                                else if (((gMKVTrack)seg).CodecID.ToUpper().Contains("A_VORBIS"))
                                {
                                    outputFileExtension = "ogg";
                                }
                                else if (((gMKVTrack)seg).CodecID.ToUpper().Contains("A_FLAC"))
                                {
                                    outputFileExtension = "flac";
                                }
                                else if (((gMKVTrack)seg).CodecID.ToUpper().Contains("A_REAL"))
                                {
                                    outputFileExtension = "ra";
                                }
                                else if (((gMKVTrack)seg).CodecID.ToUpper().Contains("A_MS/ACM"))
                                {
                                    outputFileExtension = "wav";
                                }
                                else if (((gMKVTrack)seg).CodecID.ToUpper().Contains("A_AAC"))
                                {
                                    outputFileExtension = "aac";
                                }
                                else if (((gMKVTrack)seg).CodecID.ToUpper().Contains("A_QUICKTIME"))
                                {
                                    outputFileExtension = "mov";
                                }
                                else if (((gMKVTrack)seg).CodecID.ToUpper().Contains("A_TTA1"))
                                {
                                    outputFileExtension = "thd";
                                }
                                else if (((gMKVTrack)seg).CodecID.ToUpper().Contains("A_WAVPACK4"))
                                {
                                    outputFileExtension = "wv";
                                }
                                else if (((gMKVTrack)seg).CodecID.ToUpper().Contains("A_OPUS"))
                                {
                                    outputFileExtension = "opus";
                                }
                                else
                                {
                                    outputFileExtension = "mka";
                                }
                                break;
                            case MkvTrackType.subtitles:
                                if (((gMKVTrack)seg).CodecID.ToUpper().Contains("S_TEXT/UTF8"))
                                {
                                    outputFileExtension = "srt";
                                }
                                else if (((gMKVTrack)seg).CodecID.ToUpper().Contains("S_TEXT/SSA"))
                                {
                                    outputFileExtension = "ass";
                                }
                                else if (((gMKVTrack)seg).CodecID.ToUpper().Contains("S_TEXT/ASS"))
                                {
                                    outputFileExtension = "ass";
                                }
                                else if (((gMKVTrack)seg).CodecID.ToUpper().Contains("S_TEXT/USF"))
                                {
                                    outputFileExtension = "usf";
                                }
                                else if (((gMKVTrack)seg).CodecID.ToUpper().Contains("S_IMAGE/BMP"))
                                {
                                    outputFileExtension = "sub";
                                }
                                else if (((gMKVTrack)seg).CodecID.ToUpper().Contains("S_VOBSUB"))
                                {
                                    outputFileExtension = "sub";
                                }
                                else if (((gMKVTrack)seg).CodecID.ToUpper().Contains("S_HDMV/PGS"))
                                {
                                    outputFileExtension = "sup";
                                }
                                else if (((gMKVTrack)seg).CodecID.ToUpper().Contains("S_KATE"))
                                {
                                    outputFileExtension = "ogg";
                                }
                                else
                                {
                                    outputFileExtension = "sub";
                                }
                                break;
                            default:
                                break;
                        }

                        par = String.Format("tracks \"{0}\" {1}:\"{2}\"",
                            argMKVFile,
                            ((gMKVTrack)seg).TrackID,
                            Path.Combine(argOutputDirectory,
                            Path.GetFileNameWithoutExtension(argMKVFile) + "_track" + ((gMKVTrack)seg).TrackNumber +
                            "_" + ((gMKVTrack)seg).Language + extraOutputPart +
                            "." + outputFileExtension));
                        
                        // check if timecodes are needed
                        if (argTimecodesExtractionMode != TimecodesExtractionMode.NoTimecodes)
                        {
                            // check if timecodes have already been extracted (in case of audio tracks)
                            if (!timecodesExtracted)
                            {
                                OnMkvExtractTrackUpdated(trackName + " (timecodes)");
                                ExtractMkvSegment(argMKVFile, parTimecodes, String.Empty);
                            }
                        }
                    }
                    else if (seg is gMKVAttachment)
                    {
                        par = String.Format("attachments \"{0}\" {1}:\"{2}\"",
                            argMKVFile,
                            ((gMKVAttachment)seg).ID,
                            Path.Combine(argOutputDirectory, ((gMKVAttachment)seg).Filename));
                        trackName = "Attachment " + ((gMKVAttachment)seg).ID.ToString();
                    }
                    else if (seg is gMKVChapter)
                    {
                        String outputFileExtension = String.Empty;
                        switch (argChapterType)
                        {
                            case MkvChapterTypes.XML:
                                outputFileExtension = "xml";
                                par = String.Format("chapters \"{0}\"", argMKVFile);
                                break;
                            case MkvChapterTypes.OGM:
                                outputFileExtension = "ogm.txt";
                                par = String.Format("chapters --simple \"{0}\"", argMKVFile);
                                break;
                            default:
                                break;
                        }

                        chapFile = Path.Combine(argOutputDirectory,
                            Path.GetFileNameWithoutExtension(argMKVFile) + "_chapters." + outputFileExtension);
                        trackName = "Chapters";
                    }
                    // check if track is actually needed to be extracted
                    if (argTimecodesExtractionMode != TimecodesExtractionMode.OnlyTimecodes)
                    {
                        OnMkvExtractTrackUpdated(trackName);
                        ExtractMkvSegment(argMKVFile, par, chapFile);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    _ErrorBuilder.AppendLine("Segment: " + seg.ToString() + "\r\nException: " + ex.Message + "\r\n");
                }                
            }
            // check for errors
            if (_ErrorBuilder.Length > 0)
            {
                throw new Exception(_ErrorBuilder.ToString());
            }
        }

        public void ExtractMKVTimecodesThreaded(Object parameters)
        {
            _ThreadedException = null;
            _Abort = false;
            _AbortAll = false;
            try
            {
                List<Object> objParameters = (List<Object>)parameters;
                ExtractMKVSegments((String)objParameters[0],
                    (List<gMKVSegment>)objParameters[1],
                    (String)objParameters[2],
                    (MkvChapterTypes)objParameters[3],
                    TimecodesExtractionMode.OnlyTimecodes);
            }
            catch (Exception ex)
            {
                _ThreadedException = ex;
            }
        }
        
        public void ExtractMkvCuesheetThreaded(Object parameters)
        {
            _ThreadedException = null;
            _Abort = false;
            _AbortAll = false;
            try
            {
                List<Object> objParameters = (List<Object>)parameters;
                ExtractMkvCuesheet((String)objParameters[0], (String)objParameters[1]);
            }
            catch (Exception ex)
            {
                _ThreadedException = ex;
            }
        }

        public void ExtractMkvCuesheet(String argMKVFile, String argOutputDirectory)
        {
            _Abort = false;
            _AbortAll = false;
            _ErrorBuilder.Length = 0;
            _MKVExtractOutput.Length = 0;
            String par = String.Format("cuesheet \"{0}\"", argMKVFile);
            String chapFile = Path.Combine(argOutputDirectory,
                Path.GetFileNameWithoutExtension(argMKVFile) + "_cuesheet.cue");
            try
            {
                OnMkvExtractTrackUpdated("Cue Sheet");
                ExtractMkvSegment(argMKVFile, par, chapFile);
            }
            catch (Exception ex)
            {                
                Debug.WriteLine(ex);
            }
            // check for errors
            if (_ErrorBuilder.Length > 0)
            {
                throw new Exception(_ErrorBuilder.ToString());
            }
        }

        public void ExtractMkvTagsThreaded(Object parameters)
        {
            _Abort = false;
            _AbortAll = false;
            _ThreadedException = null;
            try
            {
                List<Object> objParameters = (List<Object>)parameters;
                ExtractMkvTags((String)objParameters[0], (String)objParameters[1]);
            }
            catch (Exception ex)
            {
                _ThreadedException = ex;
            }
        }

        public void ExtractMkvTags(String argMKVFile, String argOutputDirectory)
        {
            _Abort = false;
            _AbortAll = false;
            _ErrorBuilder.Length = 0;
            _MKVExtractOutput.Length = 0;
            String par = String.Format("tags \"{0}\"", argMKVFile);
            String tagsFile = Path.Combine(argOutputDirectory,
                Path.GetFileNameWithoutExtension(argMKVFile) + "_tags.xml");
            try
            {
                OnMkvExtractTrackUpdated("Tags");
                ExtractMkvSegment(argMKVFile, par, tagsFile);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            // check for errors
            if (_ErrorBuilder.Length > 0)
            {
                throw new Exception(_ErrorBuilder.ToString());
            }
        }

        protected void OnMkvExtractProgressUpdated(Int32 progress)
        {
            if (MkvExtractProgressUpdated != null)
                MkvExtractProgressUpdated(progress);
        }

        protected void OnMkvExtractTrackUpdated(String trackName)
        {
            if (MkvExtractTrackUpdated != null)
                MkvExtractTrackUpdated(trackName);
        }

        private void ExtractMkvSegment(String argMKVFile, String argParameters, String argChapterFile)
        {
            OnMkvExtractProgressUpdated(0);
            // check for existence of MKVExtract
            if (!File.Exists(_MKVExtractFilename)) { throw new Exception("Could not find mkvextract.exe!\r\n" + _MKVExtractFilename); }
            using (Process myProcess = new Process())
            {
                ProcessStartInfo myProcessInfo = new ProcessStartInfo();
                myProcessInfo.FileName = _MKVExtractFilename;
                myProcessInfo.Arguments = argParameters;
                myProcessInfo.UseShellExecute = false;
                myProcessInfo.RedirectStandardOutput = true;
                myProcessInfo.StandardOutputEncoding = Encoding.UTF8;
                myProcessInfo.RedirectStandardError = true;
                myProcessInfo.StandardErrorEncoding = Encoding.UTF8;
                myProcessInfo.CreateNoWindow = true;
                myProcessInfo.WindowStyle = ProcessWindowStyle.Hidden;
                myProcess.StartInfo = myProcessInfo;
                if (argChapterFile == String.Empty)
                {
                    myProcess.OutputDataReceived += myProcess_OutputDataReceived;
                }
                else
                {
                    _ChapterWriter = new StreamWriter(argChapterFile, false, new UTF8Encoding(false, true));
                    myProcess.OutputDataReceived += myProcess_ChapterDataReceived;
                }

                Debug.WriteLine(myProcessInfo.Arguments);
                // Start the mkvinfo process
                myProcess.Start();
                // Start reading the output
                myProcess.BeginOutputReadLine();
                // Wait for the process to exit
                myProcess.WaitForExit();
                // unregister the event
                if (argChapterFile == String.Empty)
                {
                    myProcess.OutputDataReceived -= myProcess_OutputDataReceived;
                }
                else
                {
                    _ChapterWriter.Close();
                    _ChapterWriter = null;
                    myProcess.OutputDataReceived -= myProcess_ChapterDataReceived;
                }

                // Debug write the exit code
                Debug.WriteLine("Exit code: " + myProcess.ExitCode);

                // Check the exit code
                if (myProcess.ExitCode > 0)
                {
                    // something went wrong!
                    throw new Exception(String.Format("Mkvextract exited with error code {0}!\r\n\r\nErrors reported:\r\n{1}", 
                        myProcess.ExitCode, _ErrorBuilder.ToString()));
                }
                else if (myProcess.ExitCode < 0)
                {
                    // user aborted the current procedure!
                    throw new Exception("User aborted the current process!");
                }
            }
        }

        void myProcess_ChapterDataReceived(object sender, DataReceivedEventArgs e)
        {
            // check for user abort
            if (_Abort) 
            {
                ((Process)sender).Kill();
                _Abort = false;
                return;
            }
            if (e.Data != null)
            {
                if (e.Data.Trim().Length > 0)
                {
                    // add the line to the output stringbuilder
                    _ChapterWriter.WriteLine(e.Data);
                    // check for errors
                    if (e.Data.Contains("Error:"))
                    {
                        _ErrorBuilder.AppendLine(e.Data.Substring(e.Data.IndexOf(":") + 1).Trim());
                    }
                    // check for progress
                    if (e.Data.Contains("Progress:"))
                    {
                        OnMkvExtractProgressUpdated(Convert.ToInt32(e.Data.Substring(e.Data.IndexOf(":") + 1, e.Data.IndexOf("%") - e.Data.IndexOf(":") - 1)));
                    }
                    // debug write the output line
                    Debug.WriteLine(e.Data);
                    // log the output
                    gMKVLogger.Log(e.Data);
                }
            }
        }

        void myProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            // check for user abort
            if (_Abort)
            {
                ((Process)sender).Kill();
                _Abort = false;
                return;
            }
            if (e.Data != null)
            {
                if (e.Data.Trim().Length > 0)
                {
                    // add the line to the output stringbuilder
                    _MKVExtractOutput.AppendLine(e.Data);
                    // check for errors
                    if (e.Data.Contains("Error:"))
                    {
                        _ErrorBuilder.AppendLine(e.Data.Substring(e.Data.IndexOf(":") + 1).Trim());
                    }
                    // check for progress
                    if (e.Data.Contains("Progress:"))
                    {
                        OnMkvExtractProgressUpdated(Convert.ToInt32(e.Data.Substring(e.Data.IndexOf(":") + 1, e.Data.IndexOf("%") - e.Data.IndexOf(":") - 1)));
                    }
                    // debug write the output line
                    Debug.WriteLine(e.Data);
                    // log the output
                    gMKVLogger.Log(e.Data);
                }
            }
        }

    }
}
