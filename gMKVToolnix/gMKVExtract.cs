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

        public Exception ThreadedException = null;

        public gMKVExtract(String mkvToonlixPath)
        {
            _MKVToolnixPath = mkvToonlixPath;
            _MKVExtractFilename = Path.Combine(_MKVToolnixPath, "mkvextract.exe");
        }

        public void ExtractMKVSegmentsThreaded(Object parameters)
        {
            ThreadedException = null;
            try
            {
                List<Object> objParameters = (List<Object>)parameters;
                ExtractMKVSegments((String)objParameters[0],
                    (List<gMKVSegment>)objParameters[1],
                    (String)objParameters[2],
                    (MkvChapterTypes)objParameters[3]);
            }
            catch (Exception ex)
            {
                ThreadedException = ex;
            }
        }

        public void ExtractMKVSegments(String argMKVFile, List<gMKVSegment> argMKVSegmentsToExtract, String argOutputDirectory, MkvChapterTypes argChapterType)
        {
            _ErrorBuilder.Length = 0;
            _MKVExtractOutput.Length = 0;
            foreach (gMKVSegment seg in argMKVSegmentsToExtract)
            {
                String trackName = String.Empty;
                String par = String.Empty;
                String chapFile = String.Empty;
                if (seg is gMKVTrack)
                {
                    String outputFileExtension = String.Empty;
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
                        "_" + ((gMKVTrack)seg).Language + "." + outputFileExtension));
                    trackName = "Track " + ((gMKVTrack)seg).TrackNumber.ToString();
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
                            outputFileExtension = "ogm";
                            par = String.Format("chapters --simple \"{0}\"", argMKVFile);
                            break;
                        default:
                            break;
                    }

                    chapFile = Path.Combine(argOutputDirectory,
                        Path.GetFileNameWithoutExtension(argMKVFile) + "_chapters." + outputFileExtension);
                    trackName = "Chapters";
                }
                try
                {
                    OnMkvExtractTrackUpdated(trackName);
                    ExtractMkvSegment(argMKVFile, par, chapFile);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    _ErrorBuilder.AppendLine("for segment " + seg.ToString());
                }                
            }
            // check for errors
            if (_ErrorBuilder.Length > 0)
            {
                throw new Exception(_ErrorBuilder.ToString());
            }
        }

        public void ExtractMkvCuesheetThreaded(Object parameters)
        {
            ThreadedException = null;
            try
            {
                List<Object> objParameters = (List<Object>)parameters;
                ExtractMkvCuesheet((String)objParameters[0], (String)objParameters[1]);
            }
            catch (Exception ex)
            {
                ThreadedException = ex;
            }
        }

        public void ExtractMkvCuesheet(String argMKVFile, String argOutputDirectory)
        {
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
            ThreadedException = null;
            try
            {
                List<Object> objParameters = (List<Object>)parameters;
                ExtractMkvTags((String)objParameters[0], (String)objParameters[1]);
            }
            catch (Exception ex)
            {
                ThreadedException = ex;
            }
        }

        public void ExtractMkvTags(String argMKVFile, String argOutputDirectory)
        {
            _ErrorBuilder.Length = 0;
            _MKVExtractOutput.Length = 0;
            String par = String.Format("tags \"{0}\"", argMKVFile);
            String chapFile = Path.Combine(argOutputDirectory,
                Path.GetFileNameWithoutExtension(argMKVFile) + "_cuesheet.xml");
            try
            {
                OnMkvExtractTrackUpdated("Tags");
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
            }
        }

        void myProcess_ChapterDataReceived(object sender, DataReceivedEventArgs e)
        {
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
