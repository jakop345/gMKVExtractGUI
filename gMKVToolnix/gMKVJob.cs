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

        private gMkvExtractMethod _ExtractMethod;
        public gMkvExtractMethod ExtractMethod { get { return _ExtractMethod; } }

        private List<Object> _ParametersList;
        public List<Object> ParametersList { get { return _ParametersList; } }

        public gMKVJob(FormMkvExtractionMode argExtractionMode, gMkvExtractMethod argExtractMethod, List<Object> argParameters)
        {
            _ExtractionMode = argExtractionMode;
            _ExtractMethod = argExtractMethod;
            _ParametersList = argParameters;
        }

        public override string ToString()
        {
            StringBuilder retValue = new StringBuilder();
            switch (_ExtractionMode)
            {
                case FormMkvExtractionMode.Tracks:
                    StringBuilder trackBuilder = new StringBuilder();
                    foreach (gMKVSegment item in (List<gMKVSegment>)_ParametersList[1])
                    {
                        if (item is gMKVTrack)
                        {
                            trackBuilder.AppendFormat("[{0}]", ((gMKVTrack)item).TrackID);
                        }
                        else if (item is gMKVChapter)
                        {
                            trackBuilder.AppendFormat("[{0}]", "Chap");
                        }
                    }
                    retValue.AppendFormat("Tracks {0} '{1}' => '{2}'",
                        trackBuilder.ToString(),
                        Path.GetFileName((String)_ParametersList[0]), _ParametersList[2]);
                    break;
                case FormMkvExtractionMode.Cue_Sheet:
                    retValue.AppendFormat("Cue Sheet '{0}' => '{1}'",
                        Path.GetFileName((String)_ParametersList[0]), _ParametersList[1]);
                    break;
                case FormMkvExtractionMode.Tags:
                    retValue.AppendFormat("Tags '{0}' => '{1}'",
                        Path.GetFileName((String)_ParametersList[0]), _ParametersList[1]);
                    break;
                case FormMkvExtractionMode.Timecodes:
                    StringBuilder tcBuilder = new StringBuilder();
                    foreach (gMKVSegment item in (List<gMKVSegment>)_ParametersList[1])
                    {
                        if (item is gMKVTrack)
                        {
                            tcBuilder.AppendFormat("[{0}]", ((gMKVTrack)item).TrackID);
                        }
                        else if (item is gMKVChapter)
                        {
                            tcBuilder.AppendFormat("[{0}]", "Chap");
                        }
                    }
                    retValue.AppendFormat("Timecodes {0} '{1}' => '{2}'",
                        tcBuilder.ToString(),
                        Path.GetFileName((String)_ParametersList[0]), _ParametersList[2]);
                    break;
                case FormMkvExtractionMode.Tracks_And_Timecodes:
                    StringBuilder trackTcBuilder = new StringBuilder();
                    foreach (gMKVSegment item in (List<gMKVSegment>)_ParametersList[1])
                    {
                        if (item is gMKVTrack)
                        {
                            trackTcBuilder.AppendFormat("[{0}]", ((gMKVTrack)item).TrackID);
                        }
                        else if (item is gMKVChapter)
                        {
                            trackTcBuilder.AppendFormat("[{0}]", "Chap");
                        }
                    }
                    retValue.AppendFormat("Tracks/Timecodes {0} '{1}' => '{2}'",
                        trackTcBuilder.ToString(),
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
