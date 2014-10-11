using System;
using System.Collections.Generic;
using System.Text;

namespace gMKVToolnix
{
    public enum JobState
    {
        Ready,
        Pending,
        Running,
        Completed,
        Failed
    }

    public class gMKVJobInfo
    {

        private gMKVJob _Job = null;

        public gMKVJob Job
        {
            get { return _Job; }
            set { _Job = value; }
        }

        private DateTime? _StartTime = null;

        public DateTime? StartTime
        {
            get { return _StartTime; }
            set { _StartTime = value; }
        }

        private DateTime? _EndTime = null;

        public DateTime? EndTime
        {
            get { return _EndTime; }
            set { _EndTime = value; }
        }

        private JobState _State = JobState.Ready;

        public JobState State
        {
            get { return _State; }
            set { _State = value; }
        }

        public TimeSpan? Duration
        {
            get
            {
                if (_StartTime.HasValue)
                {
                    if (_EndTime.HasValue)
                    {
                        return ((TimeSpan?)(_EndTime - _StartTime)).Value;
                    }
                    else
                    {
                        return ((TimeSpan?)(DateTime.Now - _StartTime)).Value;
                    }
                }
                else
                {
                    return null;
                }
            }
        }

        public gMKVJobInfo(gMKVJob argJob)
        {
            _Job = argJob;
        }

        public void Reset()
        {
            _EndTime = null;
            _StartTime = null;
            _State = JobState.Ready;
        }
    }
}
