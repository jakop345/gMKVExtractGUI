using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace gMKVToolnix
{
    public partial class frmMain : Form
    {
        private void ShowErrorMessage(String argMessage)
        {
            MessageBox.Show("An error has occured!\r\n\r\n" + argMessage, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void ShowSuccessMessage(String argMessage)
        {
            MessageBox.Show(argMessage, "Success!", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public frmMain()
        {
            try
            {
                InitializeComponent();
                Text = "gMKVExtractGUI v" + Assembly.GetExecutingAssembly().GetName().Version + " -- By Gpower2";
                txtMKVToolnixPath.Text = gMKVHelper.GetMKVToolnixPath();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                ShowErrorMessage(ex.Message);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                chkLstInputFileTracks.Items.Clear();
                gMKVInfo g = new gMKVInfo(txtMKVToolnixPath.Text);
                List<gMKVSegment> segmentList = g.GetMKVSegments(txtInputFile.Text);
                foreach (gMKVSegment seg in segmentList)
                {
                    if (seg is gMKVSegmentInfo)
                    {
                    }
                    else
                    {
                        chkLstInputFileTracks.Items.Add(seg);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void txt_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                String[] s = (String[])e.Data.GetData(DataFormats.FileDrop, false);
                ((TextBox)sender).Text = s[0];
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                ShowErrorMessage(ex.Message);
            }
        }

        private void txt_DragEnter(object sender, DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    e.Effect = DragDropEffects.All;
                else
                    e.Effect = DragDropEffects.None;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                ShowErrorMessage(ex.Message);
            }
        }

        private void ClearControls()
        {
            txtOutputDirectory.Text = string.Empty;
            txtSegmentInfo.Text = string.Empty;
            chkLstInputFileTracks.Items.Clear();
        }

        private void txtInputFile_TextChanged(object sender, EventArgs e)
        {
            try
            {
                // empty all the controls in any case
                ClearControls();
                // if user provided with a filename
                if (txtInputFile.Text.Trim().Length > 0)
                {                    
                    // check if input file is valid
                    if (!File.Exists(txtInputFile.Text.Trim()))
                    {
                        throw new Exception("The input file \r\n\r\n" + txtInputFile.Text.Trim()+ "\r\n\r\ndoes not exist!");
                    }
                    // check if file is an mkv file
                    if (Path.GetExtension(txtInputFile.Text.Trim()).ToLower() != ".mkv")
                    {
                        throw new Exception("The input file \r\n\r\n" + txtInputFile.Text.Trim() + "\r\n\r\nis not an mkv file!");
                    }
                    // set output directory to the source directory
                    txtOutputDirectory.Text = Path.GetDirectoryName(txtInputFile.Text.Trim());
                    // get the file information
                    gMKVInfo g = new gMKVInfo(txtMKVToolnixPath.Text.Trim());
                    List<gMKVSegment> segmentList = g.GetMKVSegments(txtInputFile.Text.Trim());
                    foreach (gMKVSegment seg in segmentList)
                    {
                        if (seg is gMKVSegmentInfo)
                        {
                            txtSegmentInfo.Text = String.Format("Writing Application: {0}\r\nMuxing Application: {1}\r\nDuration: {2}\r\nDate: {3}", 
                                ((gMKVSegmentInfo)seg).WritingApplication,
                                ((gMKVSegmentInfo)seg).MuxingApplication,
                                ((gMKVSegmentInfo)seg).Duration,
                                ((gMKVSegmentInfo)seg).Date);
                        }
                        else
                        {
                            chkLstInputFileTracks.Items.Add(seg);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                ShowErrorMessage(ex.Message);
            }
        }

        private void btnBrowseInputFile_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Title = "Select an input mkv file...";
                ofd.Filter = "(*.mkv)|*.mkv";
                ofd.Multiselect = false;
                ofd.AutoUpgradeEnabled = true;
                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    txtInputFile.Text = ofd.FileName;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                ShowErrorMessage(ex.Message);
            }
        }

        private void btnBrowseOutputDirectory_Click(object sender, EventArgs e)
        {
            try
            {
                FolderBrowserDialog fbd = new FolderBrowserDialog();
                fbd.Description = "Select output directory...";
                fbd.ShowNewFolderButton = true;
                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    txtOutputDirectory.Text = fbd.SelectedPath;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                ShowErrorMessage(ex.Message);
            }
        }

        private void btnBrowseMKVToolnixPath_Click(object sender, EventArgs e)
        {
            try
            {
                FolderBrowserDialog fbd = new FolderBrowserDialog();
                fbd.Description = "Select MKVToolnix directory...";
                fbd.ShowNewFolderButton = true;
                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    txtMKVToolnixPath.Text = fbd.SelectedPath;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                ShowErrorMessage(ex.Message);
            }
        }

        private void btnExtractTracks_Click(object sender, EventArgs e)
        {
            try
            {
                gMKVExtract g = new gMKVExtract(txtMKVToolnixPath.Text);
                g.ExtractMKVTracks(txtInputFile.Text, new List<gMKVSegment>(), txtOutputDirectory.Text); 
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                ShowErrorMessage(ex.Message);
            }
        }
    }
}
