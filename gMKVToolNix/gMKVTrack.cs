using System;
using System.Collections.Generic;
using System.Text;

namespace gMKVToolNix
{
    public enum MkvTrackType
    {
        video,
        audio,
        subtitles
    }

    [Serializable]
    public class gMKVTrack : gMKVSegment
    {
        private int _TrackNumber;

        public int TrackNumber
        {
            get { return _TrackNumber; }
            set { _TrackNumber = value; }
        }

        private int _TrackID;

        public int TrackID
        {
            get { return _TrackID; }
            set { _TrackID = value; }
        }

        private MkvTrackType _TrackType;

        public MkvTrackType TrackType
        {
            get { return _TrackType; }
            set { _TrackType = value; }
        }

        private String _CodecID;

        public String CodecID
        {
            get { return _CodecID; }
            set { _CodecID = value; }
        }

        private String _CodecPrivate = String.Empty;

        public String CodecPrivate
        {
            get { return _CodecPrivate; }
            set { _CodecPrivate = value; }
        }

        private String _CodecPrivateData = String.Empty;

        public String CodecPrivateData
        {
            get { return _CodecPrivateData; }
            set { _CodecPrivateData = value; }
        }

        private String _Language;

        public String Language
        {
            get { return _Language; }
            set { _Language = value; }
        }

        private String _TrackName;

        public String TrackName
        {
            get { return _TrackName; }
            set { _TrackName = value; }
        }

        private String _ExtraInfo;

        public String ExtraInfo
        {
            get { return _ExtraInfo; }
            set { _ExtraInfo = value; }
        }

        private Int32 _Delay = Int32.MinValue;

        public Int32 Delay
        {
            get { return _Delay; }
            set { _Delay = value; }
        }

        private Int32 _EffectiveDelay = Int32.MinValue;

        public Int32 EffectiveDelay
        {
            get { return _EffectiveDelay; }
            set { _EffectiveDelay = value; }
        }

        private Int64 _MinimumTimestamp = Int64.MinValue;

        public Int64 MinimumTimestamp // In nanoseconds
        {
            get { return _MinimumTimestamp; }
            set { _MinimumTimestamp = value; }
        }

        public override string ToString()
        {
            String str = String.Format("Track {0} [TID {1}][{2}][{3}][{4}][{5}][{6}]", 
                _TrackNumber, _TrackID, Enum.GetName(typeof(MkvTrackType), _TrackType), _CodecID, _TrackName, _Language, _ExtraInfo);
            if (!String.IsNullOrEmpty(_CodecPrivate))
            {
                str = String.Format("{0}[{1}]", str, _CodecPrivate);
            }
            if (_TrackType != MkvTrackType.subtitles)
            {
                str = String.Format("{0}[{1} ms][{2} ms]", str, _Delay, _EffectiveDelay);
            }
            return str;
        }
    }
}
