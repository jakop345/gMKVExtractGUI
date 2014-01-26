using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace gMKVToolnix
{
    public partial class frmMain : Form
    {
        public frmMain()
        {
            InitializeComponent();
            Text = "gMKVExtractGUI v" + Assembly.GetExecutingAssembly().GetName().Version + " -- By Gpower2";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                gMKVExtract g = new gMKVExtract();
                Debug.WriteLine(gMKVHelper.GetMKVToolnixPath());
                g.GetMKVTracks("");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
    }
}
