using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Xml.Serialization;
using System.Xml;
using gMKVToolNix.CueSheet;

namespace gMKVToolnix
{
    public enum MkvExtractModes
    {
        tracks,
        tags,
        attachments,
        chapters,
        cuesheet,
        timecodes_v2,
        cues
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

    public enum CuesExtractionMode
    {
        NoCues,
        WithCues,
        OnlyCues
    }

    public delegate void MkvExtractProgressUpdatedEventHandler(Int32 progress);
    public delegate void MkvExtractTrackUpdatedEventHandler(String trackName);

    public class gMKVExtract
    {
        internal class TrackParameter
        {
            public MkvExtractModes ExtractMode = MkvExtractModes.tracks;
            public String Options = String.Empty;
            public String TrackOutput = String.Empty;
            public Boolean WriteOutputToFile = false;
            public String OutputFilename = String.Empty;

            public TrackParameter(MkvExtractModes argExtractMode,
                String argOptions,
                String argTrackOutput,
                Boolean argWriteOutputToFile,
                String argOutputFilename)
            {
                ExtractMode = argExtractMode;
                Options = argOptions;
                TrackOutput = argTrackOutput;
                WriteOutputToFile = argWriteOutputToFile;
                OutputFilename = argOutputFilename;
            }

            public TrackParameter() { }
        }

        /// <summary>
        /// Gets the mkvextract executable filename
        /// </summary>
        public static String MKV_EXTRACT_FILENAME
        {
            get { return gMKVHelper.IsOnLinux ? "mkvextract" : "mkvextract.exe"; }
        }

        private String _MKVToolnixPath = String.Empty;
        private String _MKVExtractFilename = String.Empty;
        private StringBuilder _MKVExtractOutput = new StringBuilder();
        private StreamWriter _OutputFileWriter = null;
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
            _MKVExtractFilename = Path.Combine(_MKVToolnixPath, MKV_EXTRACT_FILENAME);
        }

        public void ExtractMKVSegmentsThreaded(Object parameters)
        {
            _ThreadedException = null;
            try
            {
                List<Object> objParameters = (List<Object>)parameters;
                ExtractMKVSegments((String)objParameters[0],
                    (List<gMKVSegment>)objParameters[1],
                    (String)objParameters[2],
                    (MkvChapterTypes)objParameters[3],
                    (TimecodesExtractionMode)objParameters[4],
                    (CuesExtractionMode)objParameters[5]);
            }
            catch (Exception ex)
            {
                _ThreadedException = ex;
            }
        }

        private List<TrackParameter> GetTrackParameters(gMKVSegment argSeg,
            String argMKVFile, String argOutputDirectory, MkvChapterTypes argChapterType, 
            TimecodesExtractionMode argTimecodesExtractionMode, CuesExtractionMode argCueExtractionMode)
        {
            // create the new parameter list type
            List<TrackParameter> trackParameterList = new List<TrackParameter>();
            
            // check the selected segment's type
            if (argSeg is gMKVTrack)
            {
                // if we are in a mode that requires timecodes extraction, add the parameter for the track
                if (argTimecodesExtractionMode != TimecodesExtractionMode.NoTimecodes)
                {
                    trackParameterList.Add(new TrackParameter(
                        MkvExtractModes.timecodes_v2,
                        String.Empty,
                        String.Format("{0}:\"{1}\"",
                            ((gMKVTrack)argSeg).TrackID,
                            Path.Combine(
                                argOutputDirectory,
                                String.Format("{0}_track{1}_{2}.tc.txt",
                                    Path.GetFileNameWithoutExtension(argMKVFile),
                                    ((gMKVTrack)argSeg).TrackNumber,
                                    ((gMKVTrack)argSeg).Language))),
                        false,
                        String.Empty
                    ));
                }

                // if we are in a mode that requires cues extraction, add the parameter for the track
                if (argCueExtractionMode != CuesExtractionMode.NoCues)
                {
                    trackParameterList.Add(new TrackParameter(
                        MkvExtractModes.cues,
                        String.Empty,
                        String.Format("{0}:\"{1}\"",
                            ((gMKVTrack)argSeg).TrackID,
                            Path.Combine(
                                argOutputDirectory,
                                String.Format("{0}_track{1}_{2}.cue",
                                    Path.GetFileNameWithoutExtension(argMKVFile),
                                    ((gMKVTrack)argSeg).TrackNumber,
                                    ((gMKVTrack)argSeg).Language))),
                        false,
                        String.Empty
                    ));
                }

                // check if the mode requires the extraction of the segment itself
                if (
                    !(
                    (argTimecodesExtractionMode == TimecodesExtractionMode.OnlyTimecodes &&
                    argCueExtractionMode == CuesExtractionMode.NoCues)
                    || (argTimecodesExtractionMode == TimecodesExtractionMode.NoTimecodes &&
                    argCueExtractionMode == CuesExtractionMode.OnlyCues)
                    )
                    || (argTimecodesExtractionMode == TimecodesExtractionMode.OnlyTimecodes &&
                    argCueExtractionMode == CuesExtractionMode.OnlyCues)                    
                    )
                {
                    String outputFileExtension = String.Empty;
                    String outputDelayPart = String.Empty;

                    // check the track's type in order to get the output file's extension and the delay for audio tracks
                    switch (((gMKVTrack)argSeg).TrackType)
                    {
                        case MkvTrackType.video:
                            // get the extension of the output via the CODEC_ID of the track
                            outputFileExtension = getVideoFileExtensionFromCodecID((gMKVTrack)argSeg);
                            break;
                        case MkvTrackType.audio:
                            // add the delay to the extraOutput for the track filename
                            outputDelayPart = String.Format("_DELAY {0}ms", ((gMKVTrack)argSeg).EffectiveDelay.ToString(CultureInfo.InvariantCulture));
                            // get the extension of the output via the CODEC_ID of the track
                            outputFileExtension = getAudioFileExtensionFromCodecID((gMKVTrack)argSeg);
                            break;
                        case MkvTrackType.subtitles:
                            // get the extension of the output via the CODEC_ID of the track
                            outputFileExtension = getSubtitleFileExtensionFromCodecID((gMKVTrack)argSeg);
                            break;
                        default:
                            break;
                    }

                    // add the parameter for extracting the track
                    trackParameterList.Add(new TrackParameter(
                        MkvExtractModes.tracks,
                        String.Empty,
                        String.Format("{0}:\"{1}\"",
                            ((gMKVTrack)argSeg).TrackID,
                            Path.Combine(
                                argOutputDirectory,
                                String.Format("{0}_track{1}_{2}{3}.{4}",
                                    Path.GetFileNameWithoutExtension(argMKVFile),
                                    ((gMKVTrack)argSeg).TrackNumber,
                                    ((gMKVTrack)argSeg).Language,
                                    outputDelayPart,
                                    outputFileExtension))),
                        false,
                        String.Empty
                    ));
                }
            }
            else if (argSeg is gMKVAttachment)
            {
                // check if the mode requires the extraction of the segment itself
                if (
                    !(
                    (argTimecodesExtractionMode == TimecodesExtractionMode.OnlyTimecodes &&
                    argCueExtractionMode == CuesExtractionMode.NoCues)
                    || (argTimecodesExtractionMode == TimecodesExtractionMode.NoTimecodes &&
                    argCueExtractionMode == CuesExtractionMode.OnlyCues)
                    )
                    || (argTimecodesExtractionMode == TimecodesExtractionMode.OnlyTimecodes &&
                    argCueExtractionMode == CuesExtractionMode.OnlyCues)
                    )
                {
                    // add the parameter for extracting the attachment
                    trackParameterList.Add(new TrackParameter(
                        MkvExtractModes.attachments,
                        String.Empty,
                        String.Format("{0}:\"{1}\"",
                            ((gMKVAttachment)argSeg).ID,
                            Path.Combine(
                                argOutputDirectory,
                                ((gMKVAttachment)argSeg).Filename)),
                        false,
                        String.Empty
                    ));
                }
            }
            else if (argSeg is gMKVChapter)
            {
                // check if the mode requires the extraction of the segment itself
                if (
                    !(
                    (argTimecodesExtractionMode == TimecodesExtractionMode.OnlyTimecodes &&
                    argCueExtractionMode == CuesExtractionMode.NoCues)
                    || (argTimecodesExtractionMode == TimecodesExtractionMode.NoTimecodes &&
                    argCueExtractionMode == CuesExtractionMode.OnlyCues)
                    )
                    || (argTimecodesExtractionMode == TimecodesExtractionMode.OnlyTimecodes &&
                    argCueExtractionMode == CuesExtractionMode.OnlyCues)
                    )
                {
                    String outputFileExtension = String.Empty;
                    String options = String.Empty;
                    // check the chapter's type to determine the output file's extension and options
                    switch (argChapterType)
                    {
                        case MkvChapterTypes.XML:
                            outputFileExtension = "xml";
                            break;
                        case MkvChapterTypes.OGM:
                            outputFileExtension = "ogm.txt";
                            options = "--simple";
                            break;
                        case MkvChapterTypes.CUE:
                            outputFileExtension = "cue";
                            break;
                        default:
                            break;
                    }

                    // add the parameter for extracting the chapters
                    trackParameterList.Add(new TrackParameter(
                        MkvExtractModes.chapters,
                        options,
                        String.Empty,
                        true,
                        Path.Combine(
                            argOutputDirectory,
                            String.Format("{0}_chapters.{1}",
                                Path.GetFileNameWithoutExtension(argMKVFile),
                                outputFileExtension))
                    ));
                }
            }

            return trackParameterList;
        }

        public void ExtractMKVSegments(String argMKVFile, List<gMKVSegment> argMKVSegmentsToExtract, 
            String argOutputDirectory, MkvChapterTypes argChapterType, TimecodesExtractionMode argTimecodesExtractionMode, CuesExtractionMode argCueExtractionMode)
        {
            _Abort = false;
            _AbortAll = false;
            _ErrorBuilder.Length = 0;
            _MKVExtractOutput.Length = 0;
            // Analyze the MKV segments and get the initial parameters
            List<TrackParameter> initialParameters = new List<TrackParameter>();
            foreach (gMKVSegment seg in argMKVSegmentsToExtract)
            {
                if (_AbortAll)
                {
                    _ErrorBuilder.AppendLine("User aborted all the processes!");
                    break;
                }
                try
                {
                    initialParameters.AddRange(GetTrackParameters(seg, argMKVFile, argOutputDirectory, argChapterType, argTimecodesExtractionMode, argCueExtractionMode));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    _ErrorBuilder.AppendLine(String.Format("Segment: {0}\r\nException: {1}\r\n", seg, ex.Message));
                }                
            }

            // Group the initial parameters, in order to batch extract the mkv segments
            List<TrackParameter> finalParameters = new List<TrackParameter>();
            foreach (TrackParameter initPar in initialParameters)
            {
                TrackParameter currentPar = null;
                foreach (TrackParameter finalPar in finalParameters)
                {
                    if (finalPar.ExtractMode == initPar.ExtractMode)
                    {
                        currentPar = finalPar;
                        break;
                    }
                }
                if (currentPar != null)
                {
                    currentPar.TrackOutput = String.Format("{0} {1}", currentPar.TrackOutput, initPar.TrackOutput);
                }
                else
                {
                    finalParameters.Add(initPar);
                }
            }

            // Time to extract the mkv segments
            foreach (TrackParameter finalPar in finalParameters)
            {
                if (_AbortAll)
                {
                    _ErrorBuilder.AppendLine("User aborted all the processes!");
                    break;
                }
                try
                {
                    if (finalPar.WriteOutputToFile)
                    {
                        _OutputFileWriter = new StreamWriter(finalPar.OutputFilename, false, new UTF8Encoding(false, true));
                    }

                    OnMkvExtractTrackUpdated(Enum.GetName(finalPar.ExtractMode.GetType(), finalPar.ExtractMode));
                    ExtractMkvSegment(argMKVFile, 
                        String.Format("{0} {1} \"{2}\" {3}", 
                            Enum.GetName(finalPar.ExtractMode.GetType(),finalPar.ExtractMode),
                            finalPar.Options,
                            argMKVFile,
                            finalPar.TrackOutput), 
                        finalPar.WriteOutputToFile);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    _ErrorBuilder.AppendLine(String.Format("Track output: {0}\r\nException: {1}\r\n", finalPar.TrackOutput, ex.Message));
                }
                finally
                {
                    if (finalPar.WriteOutputToFile)
                    {
                        _OutputFileWriter.Close();
                        _OutputFileWriter = null;

                        try
                        {
                            // If we have chapters with CUE format, then we read the XML chapters and convert it to CUE
                            if (finalPar.ExtractMode == MkvExtractModes.chapters)
                            {
                                if (finalPar.OutputFilename.EndsWith("cue"))
                                {
                                    Chapters c = null;
                                    using (StreamReader sr = new StreamReader(finalPar.OutputFilename))
                                    {
                                        XmlSerializer serializer = new XmlSerializer(typeof(Chapters));
                                        c = (Chapters)serializer.Deserialize(sr);
                                    }
                                    Cue cue = new Cue();
                                    cue.File = Path.GetFileName(argMKVFile);
                                    cue.FileType = "WAVE";
                                    cue.Title = Path.GetFileName(argMKVFile);
                                    cue.Tracks = new List<CueTrack>();

                                    if (c.EditionEntry != null 
                                        && c.EditionEntry.Length > 0
                                        && c.EditionEntry[0].ChapterAtom != null
                                        && c.EditionEntry[0].ChapterAtom.Length > 0)
                                    {
                                        Int32 currentChapterTrackNumber = 1;
                                        foreach (ChapterAtom atom in c.EditionEntry[0].ChapterAtom)
                                        {
                                            CueTrack tr = new CueTrack();
                                            tr.Number = currentChapterTrackNumber;
                                            if (atom.ChapterDisplay != null
                                                && atom.ChapterDisplay.Length > 0)
                                            {
                                                tr.Title = atom.ChapterDisplay[0].ChapterString;
                                            }
                                            if (!String.IsNullOrEmpty(atom.ChapterTimeStart)
                                                && atom.ChapterTimeStart.Contains(":"))
                                            {
                                                String[] timeElements = atom.ChapterTimeStart.Split(new String[] { ":" }, StringSplitOptions.None);
                                                if (timeElements.Length == 3)
                                                {
                                                    // Find cue minutes from hours and minutes
                                                    Int32 hours = Int32.Parse(timeElements[0]);
                                                    Int32 minutes = Int32.Parse(timeElements[1]) + 60 * hours;
                                                    // Convert nanoseconds to frames (each second is 75 frames)
                                                    Int64 nanoSeconds = 0;
                                                    Int32 frames = 0;
                                                    Int32 secondsLength = timeElements[2].Length;
                                                    if (timeElements[2].Contains("."))
                                                    {
                                                        secondsLength = timeElements[2].IndexOf(".");
                                                        nanoSeconds = Int64.Parse(timeElements[2].Substring(timeElements[2].IndexOf(".") + 1));
                                                        // I take the integer part of the result action in order to get the first frame
                                                        frames = Convert.ToInt32(Math.Floor(Convert.ToDouble(nanoSeconds) / 1000000000.0 * 75.0));
                                                    }
                                                    tr.Index = String.Format("{0}:{1}:{2}",
                                                        minutes.ToString("#00")
                                                        , timeElements[2].Substring(0, secondsLength)
                                                        , frames.ToString("00")
                                                        );
                                                }
                                            }

                                            cue.Tracks.Add(tr);
                                            currentChapterTrackNumber++;
                                        }
                                    }

                                    StringBuilder cueBuilder = new StringBuilder();

                                    cueBuilder.AppendFormat("REM GENRE \"\"\r\n");
                                    cueBuilder.AppendFormat("REM DATE \"\"\r\n");
                                    cueBuilder.AppendFormat("PERFORMER \"\"\r\n");
                                    cueBuilder.AppendFormat("TITLE \"{0}\"\r\n", cue.Title);
                                    cueBuilder.AppendFormat("FILE \"{0}\" {1}\r\n", cue.File, cue.FileType);

                                    foreach (CueTrack tr in cue.Tracks)
                                    {
                                        cueBuilder.AppendFormat("\tTRACK {0} AUDIO\r\n", tr.Number.ToString("00"));
                                        cueBuilder.AppendFormat("\t\tTITLE \"{0}\"\r\n", tr.Title);
                                        cueBuilder.AppendFormat("\t\tPERFORMER \"\"\r\n");
                                        cueBuilder.AppendFormat("\t\tINDEX 01 {0}\r\n", tr.Index);
                                    }

                                    using (StreamWriter sw = new StreamWriter(finalPar.OutputFilename, false, Encoding.UTF8))
                                    {
                                        sw.Write(cueBuilder.ToString());
                                    }
                                }
                            }
                        }
                        catch (Exception exc)
                        {
                            Debug.WriteLine(exc);
                            _ErrorBuilder.AppendLine(String.Format("Track output: {0}\r\nException: {1}\r\n", finalPar.TrackOutput, exc.Message));
                        }
                    }
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
            try
            {
                List<Object> objParameters = (List<Object>)parameters;
                ExtractMKVSegments((String)objParameters[0],
                    (List<gMKVSegment>)objParameters[1],
                    (String)objParameters[2],
                    (MkvChapterTypes)objParameters[3],
                    TimecodesExtractionMode.OnlyTimecodes,
                    CuesExtractionMode.NoCues);
            }
            catch (Exception ex)
            {
                _ThreadedException = ex;
            }
        }

        public void ExtractMKVCuesThreaded(Object parameters)
        {
            _ThreadedException = null;
            try
            {
                List<Object> objParameters = (List<Object>)parameters;
                ExtractMKVSegments((String)objParameters[0],
                    (List<gMKVSegment>)objParameters[1],
                    (String)objParameters[2],
                    (MkvChapterTypes)objParameters[3],
                    TimecodesExtractionMode.NoTimecodes,
                    CuesExtractionMode.OnlyCues);
            }
            catch (Exception ex)
            {
                _ThreadedException = ex;
            }
        }


        public void ExtractMkvCuesheetThreaded(Object parameters)
        {
            _ThreadedException = null;
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
            String cueFile = Path.Combine(argOutputDirectory,
                String.Format("{0}_cuesheet.cue", Path.GetFileNameWithoutExtension(argMKVFile)));
            try
            {
                OnMkvExtractTrackUpdated("Cue Sheet");
                _OutputFileWriter = new StreamWriter(cueFile, false, new UTF8Encoding(false, true));
                ExtractMkvSegment(argMKVFile, par, true);
            }
            catch (Exception ex)
            {                
                Debug.WriteLine(ex);
            }
            finally
            {
                _OutputFileWriter.Close();
                _OutputFileWriter = null;
            }
            // check for errors
            if (_ErrorBuilder.Length > 0)
            {
                throw new Exception(_ErrorBuilder.ToString());
            }
        }

        public void ExtractMkvTagsThreaded(Object parameters)
        {
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
                String.Format("{0}_tags.xml", Path.GetFileNameWithoutExtension(argMKVFile)));
            try
            {
                OnMkvExtractTrackUpdated("Tags");
                _OutputFileWriter = new StreamWriter(tagsFile, false, new UTF8Encoding(false, true));
                ExtractMkvSegment(argMKVFile, par, true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                _OutputFileWriter.Close();
                _OutputFileWriter = null;
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

        private void ExtractMkvSegment(String argMKVFile, String argParameters, Boolean argUseOutputFileWriter)
        {
            OnMkvExtractProgressUpdated(0);
            // check for existence of MKVExtract
            if (!File.Exists(_MKVExtractFilename)) { throw new Exception(String.Format("Could not find mkvextract.exe!\r\n{0}", _MKVExtractFilename)); }
            DataReceivedEventHandler handler;
            if (argUseOutputFileWriter)
            {
                handler = myProcess_OutputDataReceived_WriteToFile;
            }
            else
            {
                handler = myProcess_OutputDataReceived;
            }

            ExecuteMkvExtract(argParameters, handler);
        }

        private void ExecuteMkvExtract(String argParameters, DataReceivedEventHandler argHandler)
        {
            using (Process myProcess = new Process())
            {
                ProcessStartInfo myProcessInfo = new ProcessStartInfo();
                myProcessInfo.FileName = _MKVExtractFilename;
                // if on Linux, the language output must be defined from the environment variables LC_ALL, LANG, and LC_MESSAGES
                if (!gMKVHelper.IsOnLinux)
                {
                    myProcessInfo.Arguments = String.Format("--ui-language en {0}", argParameters);
                }
                else
                {
                    myProcessInfo.Arguments = String.Format("{0}", argParameters);
                }                    
                myProcessInfo.UseShellExecute = false;
                myProcessInfo.RedirectStandardOutput = true;
                myProcessInfo.StandardOutputEncoding = Encoding.UTF8;
                myProcessInfo.RedirectStandardError = true;
                myProcessInfo.StandardErrorEncoding = Encoding.UTF8;
                myProcessInfo.CreateNoWindow = true;
                myProcessInfo.WindowStyle = ProcessWindowStyle.Hidden;
                myProcess.StartInfo = myProcessInfo;
                
                myProcess.OutputDataReceived += argHandler;

                Debug.WriteLine(myProcessInfo.Arguments);
                // Start the mkvinfo process
                myProcess.Start();
                // Start reading the output
                myProcess.BeginOutputReadLine();
                // Wait for the process to exit
                myProcess.WaitForExit();
                // unregister the event
                myProcess.OutputDataReceived -= argHandler;

                // Debug write the exit code
                Debug.WriteLine(String.Format("Exit code: {0}", myProcess.ExitCode));

                // Check the exit code
                // ExitCode 1 is for warnings only, so ignore it
                if (myProcess.ExitCode > 1)
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

        void myProcess_OutputDataReceived_WriteToFile(object sender, DataReceivedEventArgs e)
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
                    _OutputFileWriter.WriteLine(e.Data);
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

        private String getVideoFileExtensionFromCodecID(gMKVTrack argTrack)
        {
            String outputFileExtension = String.Empty;
            if (argTrack.CodecID.ToUpper().Contains("V_MS/VFW/FOURCC"))
            {
                outputFileExtension = "avi";
            }
            else if (argTrack.CodecID.ToUpper().Contains("V_UNCOMPRESSED"))
            {
                outputFileExtension = "raw";
            }
            else if (argTrack.CodecID.ToUpper().Contains("V_MPEG4/ISO/"))
            {
                outputFileExtension = "avc";
            }
            else if (argTrack.CodecID.ToUpper().Contains("V_MPEGH/ISO/HEVC"))
            {
                outputFileExtension = "hevc";
            }
            else if (argTrack.CodecID.ToUpper().Contains("V_MPEG4/MS/V3"))
            {
                outputFileExtension = "mp4";
            }
            else if (argTrack.CodecID.ToUpper().Contains("V_MPEG1"))
            {
                outputFileExtension = "mpg";
            }
            else if (argTrack.CodecID.ToUpper().Contains("V_MPEG2"))
            {
                outputFileExtension = "mpg";
            }
            else if (argTrack.CodecID.ToUpper().Contains("V_REAL/"))
            {
                outputFileExtension = "rm";
            }
            else if (argTrack.CodecID.ToUpper().Contains("V_QUICKTIME"))
            {
                outputFileExtension = "mov";
            }
            else if (argTrack.CodecID.ToUpper().Contains("V_THEORA"))
            {
                outputFileExtension = "ogg";
            }
            else if (argTrack.CodecID.ToUpper().Contains("V_PRORES"))
            {
                outputFileExtension = "mov";
            }
            else if (argTrack.CodecID.ToUpper().Contains("V_VP"))
            {
                outputFileExtension = "ivf";
            }
            else
            {
                outputFileExtension = "mkv";
            }
            return outputFileExtension;
        }

        private String getAudioFileExtensionFromCodecID(gMKVTrack argTrack)
        {
            String outputFileExtension = String.Empty;
            if (argTrack.CodecID.ToUpper().Contains("A_MPEG/L3"))
            {
                outputFileExtension = "mp3";
            }
            else if (argTrack.CodecID.ToUpper().Contains("A_MPEG/L2"))
            {
                outputFileExtension = "mp2";
            }
            else if (argTrack.CodecID.ToUpper().Contains("A_MPEG/L1"))
            {
                outputFileExtension = "mpa";
            }
            else if (argTrack.CodecID.ToUpper().Contains("A_PCM"))
            {
                outputFileExtension = "wav";
            }
            else if (argTrack.CodecID.ToUpper().Contains("A_MPC"))
            {
                outputFileExtension = "mpc";
            }
            else if (argTrack.CodecID.ToUpper().Contains("A_AC3"))
            {
                outputFileExtension = "ac3";
            }
            else if (argTrack.CodecID.ToUpper().Contains("A_ALAC"))
            {
                outputFileExtension = "caf";
            }
            else if (argTrack.CodecID.ToUpper().Contains("A_DTS"))
            {
                outputFileExtension = "dts";
            }
            else if (argTrack.CodecID.ToUpper().Contains("A_VORBIS"))
            {
                outputFileExtension = "ogg";
            }
            else if (argTrack.CodecID.ToUpper().Contains("A_FLAC"))
            {
                outputFileExtension = "flac";
            }
            else if (argTrack.CodecID.ToUpper().Contains("A_REAL"))
            {
                outputFileExtension = "ra";
            }
            else if (argTrack.CodecID.ToUpper().Contains("A_MS/ACM"))
            {
                outputFileExtension = "wav";
            }
            else if (argTrack.CodecID.ToUpper().Contains("A_AAC"))
            {
                outputFileExtension = "aac";
            }
            else if (argTrack.CodecID.ToUpper().Contains("A_QUICKTIME"))
            {
                outputFileExtension = "mov";
            }
            else if (argTrack.CodecID.ToUpper().Contains("A_TRUEHD"))
            {
                outputFileExtension = "thd";
            }
            else if (argTrack.CodecID.ToUpper().Contains("A_TTA1"))
            {
                outputFileExtension = "tta";
            }
            else if (argTrack.CodecID.ToUpper().Contains("A_WAVPACK4"))
            {
                outputFileExtension = "wv";
            }
            else if (argTrack.CodecID.ToUpper().Contains("A_OPUS"))
            {
                outputFileExtension = "opus";
            }
            else
            {
                outputFileExtension = "mka";
            }
            return outputFileExtension;
        }

        private String getSubtitleFileExtensionFromCodecID(gMKVTrack argTrack)
        {
            String outputFileExtension = String.Empty;
            if (argTrack.CodecID.ToUpper().Contains("S_TEXT/UTF8"))
            {
                outputFileExtension = "srt";
            }
            else if (argTrack.CodecID.ToUpper().Contains("S_TEXT/SSA"))
            {
                outputFileExtension = "ass";
            }
            else if (argTrack.CodecID.ToUpper().Contains("S_TEXT/ASS"))
            {
                outputFileExtension = "ass";
            }
            else if (argTrack.CodecID.ToUpper().Contains("S_TEXT/USF"))
            {
                outputFileExtension = "usf";
            }
            else if (argTrack.CodecID.ToUpper().Contains("S_IMAGE/BMP"))
            {
                outputFileExtension = "sub";
            }
            else if (argTrack.CodecID.ToUpper().Contains("S_VOBSUB"))
            {
                outputFileExtension = "sub";
            }
            else if (argTrack.CodecID.ToUpper().Contains("S_HDMV/PGS"))
            {
                outputFileExtension = "sup";
            }
            else if (argTrack.CodecID.ToUpper().Contains("S_KATE"))
            {
                outputFileExtension = "ogg";
            }
            else
            {
                outputFileExtension = "sub";
            }
            return outputFileExtension;
        }
    }
}
