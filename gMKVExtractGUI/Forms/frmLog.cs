using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using gMKVToolNix;
using System.Diagnostics;

namespace gMKVToolNix
{
    public partial class frmLog : gForm
    {
        public frmLog()
        {
            InitializeComponent();
            InitForm();
        }

        private void InitForm()
        {
            Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
            Text = String.Format("gMKVExtractGUI v{0} -- Log", Assembly.GetExecutingAssembly().GetName().Version);
        }

        private void frmLog_Activated(object sender, EventArgs e)
        {
            txtLog.Text = gMKVLogger.LogText;
        }

        private void txtLog_TextChanged(object sender, EventArgs e)
        {
            txtLog.Select(txtLog.TextLength + 1, 0);
            txtLog.ScrollToCaret();
            grpLog.Text = String.Format("Log ({0})", txtLog.Lines.LongLength);
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            try
            {
                this.Close();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                ShowErrorMessage(ex.Message);
            }
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            try
            {
                Clipboard.SetData(DataFormats.UnicodeText, txtLog.SelectedText);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                ShowErrorMessage(ex.Message);
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            try
            {
                txtLog.Text = gMKVLogger.LogText;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                ShowErrorMessage(ex.Message);
            }
        }

        private void frmLog_FormClosing(object sender, FormClosingEventArgs e)
        {
            // To avoid getting disposed
            e.Cancel = true;
            this.Hide();
        }

    }
}
