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
        private frmLog _LogForm = null;

        private void ShowErrorMessage(String argMessage)
        {
            MessageBox.Show("An error has occured!\r\n\r\n" + argMessage, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void ShowSuccessMessage(String argMessage)
        {
            MessageBox.Show(argMessage, "Success!", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private Boolean _FromConstructor = false;

        public frmMain()
        {
            try
            {
                InitializeComponent();
                Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
                Text = "gMKVExtractGUI v" + Assembly.GetExecutingAssembly().GetName().Version + " -- By Gpower2";
                cmbChapterType.DataSource = Enum.GetNames(typeof(MkvChapterTypes));
                try
                {
                    txtMKVToolnixPath.Text = gMKVHelper.GetMKVToolnixPathViaRegistry();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    // MKVToolnix was not found in registry
                    // last hope is in the current directory
                    if (File.Exists(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), gMKVHelper.MKV_MERGE_GUI_FILENAME)))
                    {
                        txtMKVToolnixPath.Text = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    }
                    else
                    {
                        // check for ini file
                        if (File.Exists(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "gMKVExtractGUI.ini")))
                        {
                            using (StreamReader sr = new StreamReader(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "gMKVExtractGUI.ini")))
                            {
                                // check if ini file contains a valid path
                                String iniMkvToolnixPath = sr.ReadLine();
                                if (File.Exists(Path.Combine(iniMkvToolnixPath, gMKVHelper.MKV_MERGE_GUI_FILENAME)))
                                {
                                    _FromConstructor = true;
                                    txtMKVToolnixPath.Text = iniMkvToolnixPath;
                                    _FromConstructor = false;
                                }
                                else
                                {
                                    throw new Exception("Could not find MKVToolNix in registry, or in the current directory, or in the ini file!\r\nPlease download and reinstall or provide a manual path!");
                                }
                            }
                        }
                        else
                        {
                            throw new Exception("Could not find MKVToolNix in registry or in the current directory!\r\nPlease download and reinstall or provide a manual path!");
                        }
                    }
                }
                // check if user provided with a filename
                if (Environment.GetCommandLineArgs().Length > 1)
                {
                    txtInputFile.Text = Environment.GetCommandLineArgs()[1];
                }
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

        private void txtMKVToolnixPath_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (!_FromConstructor)
                {
                    // Write the value to the ini file
                    using (StreamWriter sw = new StreamWriter(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "gMKVExtractGUI.ini")))
                    {
                        sw.Write(txtMKVToolnixPath.Text);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                ShowErrorMessage(ex.Message);
            }
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
                    String inputExtension = Path.GetExtension(txtInputFile.Text.Trim()).ToLower();
                    if (inputExtension != ".mkv"
                        && inputExtension != ".mka"
                        && inputExtension != ".mks"
                        && inputExtension != ".mk3d"
                        && inputExtension != ".webm")
                    {
                        throw new Exception("The input file \r\n\r\n" + txtInputFile.Text.Trim() + "\r\n\r\nis not a valid matroska file!");
                    }
                    // set output directory to the source directory
                    txtOutputDirectory.Text = Path.GetDirectoryName(txtInputFile.Text.Trim());
                    // get the file information                    
                    gMKVMerge g = new gMKVMerge(txtMKVToolnixPath.Text.Trim());
                    List<gMKVSegment> segmentList = g.GetMKVSegments(txtInputFile.Text.Trim());
                    gMKVInfo gInfo = new gMKVInfo(txtMKVToolnixPath.Text.Trim());
                    List<gMKVSegment> segmentListInfo = gInfo.GetMKVSegments(txtInputFile.Text.Trim());
                    foreach (gMKVSegment seg in segmentListInfo)
                    {
                        if (seg is gMKVSegmentInfo)
                        {
                            segmentList.Insert(0, seg);
                            break;
                        }
                    }
                    gInfo = null;
                    segmentListInfo = null;
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
                // Empty the text since input was wrong or something happened
                txtInputFile.Text = String.Empty;
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
                ofd.Filter = "Matroska files (*.mkv;*.mka;*.mks;*.mk3d;*.webm)|*.mkv;*.mka;*.mks;*.mk3d;*.webm|Matroska video files (*.mkv)|*.mkv|Matroska audio files (*.mka)|*.mka|Matroska subtitle files (*.mks)|*.mks|Matroska 3D files (*.mk3d)|*.mk3d|Webm files (*.webm)|*.webm";
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
                CheckNeccessaryInputFields(true, true);
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
                CheckNeccessaryInputFields(false, false);
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
                CheckNeccessaryInputFields(false, false);
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

        private void btnShowLog_Click(object sender, EventArgs e)
        {
            try
            {
                if (_LogForm == null) { _LogForm = new frmLog(); }
                if (_LogForm.IsDisposed) { _LogForm = new frmLog(); }
                _LogForm.Show();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                ShowErrorMessage(ex.Message);
            }
        }

        private void CheckNeccessaryInputFields(Boolean checkSelectedTracks, Boolean checkSelectedChapterType) 
        {
            if (txtInputFile.Text.Trim().Length == 0)
            {
                throw new Exception("You must provide with a valid Matroska file!");
            }
            else
            {
                if (!File.Exists(txtInputFile.Text.Trim()))
                {
                    throw new Exception("The input file does not exist!");
                }
            }
            if (txtMKVToolnixPath.Text.Trim().Length == 0)
            {
                throw new Exception("You must provide with MKVToolnix path!");
            }
            else
            {
                if (!File.Exists(Path.Combine(txtMKVToolnixPath.Text.Trim(), gMKVHelper.MKV_MERGE_GUI_FILENAME)))
                {
                    throw new Exception("The MKVToolnix path provided does not contain MKVToolnix files!");
                }
            }
            if (checkSelectedTracks)
            {
                if (chkLstInputFileTracks.CheckedItems.Count == 0)
                {
                    throw new Exception("You must select a track to extract!");
                }
            }
            if (checkSelectedChapterType)
            {
                foreach (gMKVSegment item in chkLstInputFileTracks.CheckedItems)
                {
                    if (item is gMKVChapter)
                    {
                        if (cmbChapterType.SelectedIndex == -1)
                        {
                            throw new Exception("You must select a chapter type!");
                        }
                    }
                }
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
