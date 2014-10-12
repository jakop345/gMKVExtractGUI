using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace gMKVToolnix
{
    public enum FormMkvExtractionMode
    {
        Tracks,
        Cue_Sheet,
        Tags,
        Timecodes,
        Tracks_And_Timecodes
    }

    public delegate void gMkvExtractMethod(Object parameterList);

    public class gMKVJob
    {
        private FormMkvExtractionMode _ExtractionMode;
        public FormMkvExtractionMode ExtractionMode { get { return _ExtractionMode; } }

        private String _MKVToolnixPath;

        public String MKVToolnixPath
        {
            get { return _MKVToolnixPath; }
        }

        public gMkvExtractMethod ExtractMethod(gMKVExtract argGmkvExtract) {
                switch (_ExtractionMode)
                {
                    case FormMkvExtractionMode.Tracks:
                        return argGmkvExtract.ExtractMKVSegmentsThreaded;
                    case FormMkvExtractionMode.Cue_Sheet:
                        return argGmkvExtract.ExtractMkvCuesheetThreaded;
                    case FormMkvExtractionMode.Tags:
                        return argGmkvExtract.ExtractMkvTagsThreaded;
                    case FormMkvExtractionMode.Timecodes:
                        return argGmkvExtract.ExtractMKVTimecodesThreaded;
                    case FormMkvExtractionMode.Tracks_And_Timecodes:
                        return argGmkvExtract.ExtractMKVSegmentsThreaded;
                    default:
                        throw new Exception("Unsupported Extraction Mode!");
                }
        }

        private List<Object> _ParametersList;
        public List<Object> ParametersList { get { return _ParametersList; } }

        public gMKVJob(FormMkvExtractionMode argExtractionMode, String argMKVToolnixPath, List<Object> argParameters)
        {
            _ExtractionMode = argExtractionMode;
            _MKVToolnixPath = argMKVToolnixPath;
            _ParametersList = argParameters;
        }

        private String GetTracks(List<gMKVSegment> argSegmentList)
        {
            StringBuilder trackBuilder = new StringBuilder();
            foreach (gMKVSegment item in argSegmentList)
            {
                if (item is gMKVTrack)
                {
                    trackBuilder.AppendFormat("[{0}:{1}]",
                        ((gMKVTrack)item).TrackType.ToString().Substring(0, 3),
                        ((gMKVTrack)item).TrackID);
                }
                else if (item is gMKVAttachment)
                {
                    trackBuilder.AppendFormat("[Att:{0}]",                        
                        ((gMKVAttachment)item).ID);
                }
                else if (item is gMKVChapter)
                {
                    trackBuilder.AppendFormat("[{0}]", "Chap");
                }
            }
            return trackBuilder.ToString();
        }

        public override string ToString()
        {
            StringBuilder retValue = new StringBuilder();
            switch (_ExtractionMode)
            {
                case FormMkvExtractionMode.Tracks:
                    retValue.AppendFormat("Tracks {0} \r\n{1} \r\n{2}",
                        GetTracks((List<gMKVSegment>)_ParametersList[1]),
                        Path.GetFileName((String)_ParametersList[0]), _ParametersList[2]);
                    break;
                case FormMkvExtractionMode.Cue_Sheet:
                    retValue.AppendFormat("Cue Sheet \r\n{0} \r\n{1}'",
                        Path.GetFileName((String)_ParametersList[0]), _ParametersList[1]);
                    break;
                case FormMkvExtractionMode.Tags:
                    retValue.AppendFormat("Tags \r\n{0} \r\n{1}",
                        Path.GetFileName((String)_ParametersList[0]), _ParametersList[1]);
                    break;
                case FormMkvExtractionMode.Timecodes:
                    retValue.AppendFormat("Timecodes {0} \r\n{1} \r\n{2}",
                        GetTracks((List<gMKVSegment>)_ParametersList[1]),
                        Path.GetFileName((String)_ParametersList[0]), _ParametersList[2]);
                    break;
                case FormMkvExtractionMode.Tracks_And_Timecodes:
                    retValue.AppendFormat("Tracks/Timecodes {0} \r\n{1} \r\n{2}",
                        GetTracks((List<gMKVSegment>)_ParametersList[1]),
                        Path.GetFileName((String)_ParametersList[0]), _ParametersList[2]);
                    break;
                default:
                    retValue.AppendFormat("Unknown job!!!");
                    break;
            }
            return retValue.ToString();
        }
    }
}
