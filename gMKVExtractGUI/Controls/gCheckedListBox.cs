using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            SetStyle(ControlStyles.ResizeRedraw, true);
        }

        protected Boolean isOkToRefresh = true;
        protected Message previousMessage;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_ERASEBKGND)
            {
                if (isOkToRefresh)
                {
                    isOkToRefresh = false;
                    m.Result = (IntPtr)1;
                }
                else
                {
                    isOkToRefresh = true;
                    previousMessage = m;
                    return;
                }
            }
            Debug.WriteLine(m);
            previousMessage = m;
            base.WndProc(ref m);
        }

    }
}
