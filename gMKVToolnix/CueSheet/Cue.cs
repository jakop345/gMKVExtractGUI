using System;
using System.Collections.Generic;
using System.Text;

namespace gMKVToolNix.CueSheet
{
    public class Cue
    {
        /*
        REM GENRE "Electronica"
REM DATE "1998"
PERFORMER "Faithless"
TITLE "Live in Berlin"
FILE "Faithless - Live in Berlin.mp3" MP3
        */
        public String Genre { get; set; }
        public String Date { get; set; }
        public String Performer { get; set; }
        public String Title { get; set; }
        public String File { get; set; }
        public String FileType { get; set; }

        public List<CueTrack> Tracks { get; set; }
    }
}
