using System;
using System.Collections.Generic;
using System.Text;

namespace gMKVToolnix
{
    public class gCheckedListBox: System.Windows.Forms.CheckedListBox
    {
        public gCheckedListBox()
            : base()
        {
            SetStyle(System.Windows.Forms.ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(System.Windows.Forms.ControlStyles.OptimizedDoubleBuffer, true);            
        }

    }
}
