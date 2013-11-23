using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace gMKVExtractGUI
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();
            Text = "gMKVExtractGUI v" + Assembly.GetExecutingAssembly().GetName().Version + " -- By Gpower2";
            gMKVExtract g = new gMKVExtract();
            Debug.WriteLine(g.GetMKVToolnixPath());
            g.GetMKVTracks("");
        }
    }
}
