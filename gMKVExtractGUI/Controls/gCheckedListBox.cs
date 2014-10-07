using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace gMKVToolnix
{

    public class gCheckedListBox: System.Windows.Forms.CheckedListBox
    {
        public const Int32 WM_ERASEBKGND = 0x0014;
        public const Int32 WM_NCPAINT = 0x0085;

        public gCheckedListBox()
            : base()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.Opaque, true);
            this.DoubleBuffered = true;
        }
    }
}
