using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace gMKVToolNix.Controls
{
    public class gListBox:ListBox
    {
        public gListBox()
            : base()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.Opaque, true);
            this.DoubleBuffered = true;
        }

    }
}
