using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

namespace gMKVToolnix
{
    public class gTableLayoutPanel:TableLayoutPanel
    {
        public const Int32 WM_ERASEBKGND = 0x0014;
        
        public gTableLayoutPanel()
            : base()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected Boolean isOkToRefresh = true;

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
                    return;
                }
            }
            //Debug.WriteLine(m);
            base.WndProc(ref m);
        }

    }
}
