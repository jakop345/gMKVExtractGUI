using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;

namespace gMKVToolNix
{
    public class gTextBox:TextBox
    {
        public gTextBox()
            : base()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.DoubleBuffered = true;
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
            }
        }

    }
}
