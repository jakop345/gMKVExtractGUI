using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace gMKVToolnix
{
    public class gForm : Form
    {
        public gForm() :base()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.Opaque, true);
            this.DoubleBuffered = true;
        }

        protected void ShowErrorMessage(String argMessage)
        {
            MessageBox.Show("An error has occured!\r\n\r\n" + argMessage, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        protected void ShowSuccessMessage(String argMessage)
        {
            MessageBox.Show(argMessage, "Success!", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        protected DialogResult ShowQuestion(String argQuestion, String argTitle)
        {
            return MessageBox.Show(argQuestion, argTitle, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        }

    }
}
