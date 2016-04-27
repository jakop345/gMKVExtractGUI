using System;
using System.Collections.Generic;
using System.Text;

namespace gMKVToolNix.CueSheet
{
    public class CueTrack
    {
        /*
  TRACK 01 AUDIO
    TITLE "Reverence"
    PERFORMER "Faithless"
    INDEX 01 00:00:00
        */

        public Int32 Number { get; set; }
        public String Title { get; set; }
        public String Performer { get; set; }
        public String Index { get; set; }

    }
}
