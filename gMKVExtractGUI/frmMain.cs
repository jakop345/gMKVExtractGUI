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
using System.Threading;

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
                cmbChapterType.DataSource = Enum.GetNames(typeof(MkvChapterTypes));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                ShowErrorMessage(ex.Message);
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
            ClearStatus();
        }

        private void ClearStatus()
        {
            lblTrack.Text = string.Empty;
            lblStatus.Text = string.Empty;
            prgBrStatus.Value = 0;
        }

        private void txtInputFile_TextChanged(object sender, EventArgs e)
        {
            try
            {
                tlpMain.Enabled = false;
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
                tlpMain.Enabled = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                ShowErrorMessage(ex.Message);
                tlpMain.Enabled = true;
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
                tlpMain.Enabled = false;
                gMKVExtract g = new gMKVExtract(txtMKVToolnixPath.Text);
                List<gMKVSegment> segments = new List<gMKVSegment>();
                foreach (gMKVSegment seg in chkLstInputFileTracks.CheckedItems)
                {
                    segments.Add(seg);
                }
                g.MkvExtractProgressUpdated += g_MkvExtractProgressUpdated;
                g.MkvExtractTrackUpdated += g_MkvExtractTrackUpdated;
                Thread t = new Thread(new ParameterizedThreadStart(g.ExtractMKVSegmentsThreaded));
                List<Object> parList = new List<object>();
                parList.Add(txtInputFile.Text);
                parList.Add(segments);
                parList.Add(txtOutputDirectory.Text);
                parList.Add((MkvChapterTypes)Enum.Parse(typeof(MkvChapterTypes), (String)cmbChapterType.SelectedItem));
                t.Start(parList);
                while (t.ThreadState != System.Threading.ThreadState.Stopped)
                {
                    Application.DoEvents();
                }
                g.MkvExtractProgressUpdated -= g_MkvExtractProgressUpdated;
                g.MkvExtractTrackUpdated -= g_MkvExtractTrackUpdated;
                // check for exceptions
                if (g.ThreadedException != null)
                {
                    throw g.ThreadedException;
                }
                ShowSuccessMessage("The selected tracks were extracted successfully!");
                ClearStatus();
                tlpMain.Enabled = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                ShowErrorMessage(ex.Message);
                ClearStatus();
                tlpMain.Enabled = true;
            }
        }

        void g_MkvExtractTrackUpdated(string trackName)
        {
            this.Invoke(new UpdateTrackLabelDelegate(UpdateTrackLabel), new object[] { trackName });
        }

        void g_MkvExtractProgressUpdated(int progress)
        {
            this.Invoke(new UpdateProgressDelegate(UpdateProgress), new object[] { progress });
        }

        private void btnExtractCue_Click(object sender, EventArgs e)
        {
            try
            {
                tlpMain.Enabled = false;
                gMKVExtract g = new gMKVExtract(txtMKVToolnixPath.Text);
                g.MkvExtractProgressUpdated += g_MkvExtractProgressUpdated;
                g.MkvExtractTrackUpdated += g_MkvExtractTrackUpdated;
                Thread t = new Thread(new ParameterizedThreadStart(g.ExtractMkvCuesheetThreaded));
                List<Object> parList = new List<object>();
                parList.Add(txtInputFile.Text);
                parList.Add(txtOutputDirectory.Text);
                t.Start(parList);
                while (t.ThreadState != System.Threading.ThreadState.Stopped)
                {
                    Application.DoEvents();
                }
                //g.ExtractMkvCuesheet(txtInputFile.Text, txtOutputDirectory.Text);
                g.MkvExtractProgressUpdated -= g_MkvExtractProgressUpdated;
                g.MkvExtractTrackUpdated -= g_MkvExtractTrackUpdated;
                // check for exceptions
                if (g.ThreadedException != null)
                {
                    throw g.ThreadedException;
                }
                ShowSuccessMessage("The Cue Sheet was extracted successfully!");
                ClearStatus();
                tlpMain.Enabled = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                ShowErrorMessage(ex.Message);
                ClearStatus();
                tlpMain.Enabled = true;
            }
        }

        private void btnExtractTags_Click(object sender, EventArgs e)
        {
            try
            {
                tlpMain.Enabled = false;
                gMKVExtract g = new gMKVExtract(txtMKVToolnixPath.Text);
                g.MkvExtractProgressUpdated += g_MkvExtractProgressUpdated;
                g.MkvExtractTrackUpdated += g_MkvExtractTrackUpdated;
                Thread t = new Thread(new ParameterizedThreadStart(g.ExtractMkvTagsThreaded));
                List<Object> parList = new List<object>();
                parList.Add(txtInputFile.Text);
                parList.Add(txtOutputDirectory.Text);
                t.Start(parList);
                while (t.ThreadState != System.Threading.ThreadState.Stopped)
                {
                    Application.DoEvents();
                }
                //g.ExtractMkvTags(txtInputFile.Text, txtOutputDirectory.Text);
                g.MkvExtractProgressUpdated -= g_MkvExtractProgressUpdated;
                g.MkvExtractTrackUpdated -= g_MkvExtractTrackUpdated;
                // check for exceptions
                if (g.ThreadedException != null)
                {
                    throw g.ThreadedException;
                }
                ShowSuccessMessage("The tags were extracted successfully!");
                ClearStatus();
                tlpMain.Enabled = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                ShowErrorMessage(ex.Message);
                ClearStatus();
                tlpMain.Enabled = true;
            }
        }

        public delegate void UpdateProgressDelegate(Object val);

        public void UpdateProgress(Object val)
        {
            prgBrStatus.Value = Convert.ToInt32(val);
            lblStatus.Text = String.Format("{0}%", Convert.ToInt32(val));
            Application.DoEvents();
        }

        public delegate void UpdateTrackLabelDelegate(Object val);

        public void UpdateTrackLabel(Object val)
        {
            lblTrack.Text = (String)val;
            Application.DoEvents();
        }
    }
}
