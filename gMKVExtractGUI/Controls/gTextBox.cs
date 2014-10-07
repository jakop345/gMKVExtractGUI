using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

namespace gMKVToolnix
{
    public class gTextBox : TextBox
    {
        public const Int32 WM_ERASEBKGND = 0x0014;

        public gTextBox()
            : base()
        {
            //SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            //SetStyle(ControlStyles.EnableNotifyMessage, true);
            this.DoubleBuffered = true;
        }

        protected Boolean isOkToRefresh = true;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_ERASEBKGND)
            {
                if (isOkToRefresh)
                {
                    isOkToRefresh = false;
                }
                else
                {
                    isOkToRefresh = true;
                    return;
                }
            }
            base.WndProc(ref m);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            try
            {
                if (e.Control && e.KeyCode == Keys.A)
                {
                    this.SelectAll();
                }
                else if (e.Control && e.KeyCode == Keys.C)
                {
                    Clipboard.SetText(this.SelectedText, TextDataFormat.UnicodeText);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                throw;
            }
        }
    }
}
