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
        }
    }
}
