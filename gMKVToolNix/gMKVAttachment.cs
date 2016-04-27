using System;
using System.Collections.Generic;
using System.Text;

namespace gMKVToolnix
{
    [Serializable]
    public class gMKVAttachment : gMKVSegment
    {
        private String _Filename;

        public String Filename
        {
            get { return _Filename; }
            set { _Filename = value; }
        }

        private String _MimeType;

        public String MimeType
        {
            get { return _MimeType; }
            set { _MimeType = value; }
        }

        private String _FileSize;

        public String FileSize
        {
            get { return _FileSize; }
            set { _FileSize = value; }
        }

        private int _ID;

        public int ID
        {
            get { return _ID; }
            set { _ID = value; }
        }

        public override string ToString()
        {
            return String.Format("Attachment {0} [{1}][{2}][{3} bytes]", _ID, _Filename, _MimeType, _FileSize);
        }
    }
}
