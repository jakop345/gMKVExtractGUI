using System;
using System.Collections.Generic;
using System.Text;

namespace gMKVToolNix
{
    public enum MkvChapterTypes
    {
        XML,
        OGM,
        CUE
    }

    [Serializable]
    public class gMKVChapter : gMKVSegment
    {
        private int _ChapterCount = 0;

        public int ChapterCount
        {
            get { return _ChapterCount; }
            set { _ChapterCount = value; }
        }

        public override string ToString()
        {
            return String.Format("Chapters {0} entries", _ChapterCount);
        }
    }
}
