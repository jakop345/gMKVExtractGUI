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
    public enum MkvExtractionMode
    {
        Tracks,
        Cue_Sheet,
        Tags,
        Timecodes,
        Tracks_And_Timecodes
    }

    public partial class frmMain : Form
    {
        private frmLog _LogForm = null;
        private gMKVExtract _gMkvExtract = null;
        private gSettings _settings = new gSettings(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

        private void ShowErrorMessage(String argMessage)
        {
            MessageBox.Show("An error has occured!\r\n\r\n" + argMessage, "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void ShowSuccessMessage(String argMessage)
        {
            MessageBox.Show(argMessage, "Success!", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private DialogResult ShowQuestion(String argQuestion, String argTitle)
        {
            return MessageBox.Show(argQuestion, argTitle, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
        }

        private Boolean _FromConstructor = false;

        public frmMain()
        {
            try
            {
                InitializeComponent();
                Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
                Text = "gMKVExtractGUI v" + Assembly.GetExecutingAssembly().GetName().Version + " -- By Gpower2";
                btnAbort.Enabled = false;
                btnAbortAll.Enabled = false;
                _FromConstructor = true;
                cmbChapterType.DataSource = Enum.GetNames(typeof(MkvChapterTypes));
                cmbExtractionMode.DataSource = Enum.GetNames(typeof(MkvExtractionMode));
                // load settings
                _settings.Reload();
                cmbChapterType.SelectedItem = Enum.GetName(typeof(MkvChapterTypes), _settings.ChapterType);
                _FromConstructor = false;
                try
                {
                    txtMKVToolnixPath.Text = gMKVHelper.GetMKVToolnixPathViaRegistry();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    // MKVToolnix was not found in registry
                    // check in the current directory
                    if (File.Exists(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), gMKVHelper.MKV_MERGE_GUI_FILENAME)))
                    {
                        txtMKVToolnixPath.Text = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    }
                    else
                    {
                        // check for ini file
                        if (File.Exists(Path.Combine(_settings.MkvToolnixPath, gMKVHelper.MKV_MERGE_GUI_FILENAME)))
                        {
                            _FromConstructor = true;
                            txtMKVToolnixPath.Text = _settings.MkvToolnixPath;
                            _FromConstructor = false;
                        }
                        else
                        {
                            throw new Exception("Could not find MKVToolNix in registry, or in the current directory, or in the ini file!\r\nPlease download and reinstall or provide a manual path!");
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
                // check if the drop data is actually a file or folder
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    // check for sender
                    if (((TextBox)sender) == txtMKVToolnixPath)
                    {
                        // check if MKVToolnix Path is already set
                        if (txtMKVToolnixPath.Text.Trim().Length > 0)
                        {
                            if (ShowQuestion("Do you really want to change MKVToolnix path?", "Are you sure?") != DialogResult.Yes)
                            {
                                return;
                                //throw new Exception("User abort!");
                            }
                        }
                    }
                    else if (((TextBox)sender) == txtOutputDirectory)
                    {
                        // check if output directory is locked
                        if (chkLockOutputDirectory.Checked)
                        {
                            return;
                        }                        
                    }
                    String[] s = (String[])e.Data.GetData(DataFormats.FileDrop, false);
                    ((TextBox)sender).Text = s[0];
                }
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
                {
                    if (((TextBox)sender) == txtOutputDirectory)
                    {
                        // check if output directory is locked
                        if (chkLockOutputDirectory.Checked)
                        {
                            e.Effect = DragDropEffects.None;
                        }
                        else
                        {
                            // check if it is a directory or not
                            if (Directory.Exists(((String[])e.Data.GetData(DataFormats.FileDrop))[0]))
                            {
                                e.Effect = DragDropEffects.All;
                            }
                        }
                    }
                    else
                    {
                        e.Effect = DragDropEffects.All;
                    }
                }
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
            // check if output directory is locked
            if (!chkLockOutputDirectory.Checked)
            {
                txtOutputDirectory.Text = string.Empty;
            }
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
                    // check if the folder actually contains MKVToolnix
                    if (!File.Exists(Path.Combine(txtMKVToolnixPath.Text.Trim(), gMKVHelper.MKV_MERGE_GUI_FILENAME)))
                    {
                        _FromConstructor = true;
                        txtMKVToolnixPath.Text = String.Empty;
                        _FromConstructor = false;
                        throw new Exception("The folder does not contain MKVToolnix!");
                    }

                    // Write the value to the ini file
                    _settings.MkvToolnixPath = txtMKVToolnixPath.Text.Trim();
                    _settings.Save();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                ShowErrorMessage(ex.Message);
            }
        }

        private void cmbChapterType_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (!_FromConstructor)
                {
                    if (cmbChapterType.SelectedIndex > -1)
                    {
                        // Write the value to the ini file
                        _settings.ChapterType = (MkvChapterTypes)Enum.Parse(typeof(MkvChapterTypes), (String)cmbChapterType.SelectedItem);
                        _settings.Save();
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
                    // check if output directory is locked
                    if (!chkLockOutputDirectory.Checked)
                    {
                        // set output directory to the source directory
                        txtOutputDirectory.Text = Path.GetDirectoryName(txtInputFile.Text.Trim());
                    }
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
                if (txtInputFile.Text.Trim().Length > 0) 
                {
                    if (Directory.Exists(Path.GetDirectoryName(txtInputFile.Text.Trim())))
                    {
                        ofd.InitialDirectory = Path.GetDirectoryName(txtInputFile.Text.Trim());
                    }
                }
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
                // check if output directory is locked
                if (!chkLockOutputDirectory.Checked)
                {
                    FolderBrowserDialog fbd = new FolderBrowserDialog();
                    fbd.Description = "Select output directory...";
                    fbd.ShowNewFolderButton = true;
                    if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        txtOutputDirectory.Text = fbd.SelectedPath;
                    }
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
                // check if MKVToolnix Path is already set
                if (txtMKVToolnixPath.Text.Trim().Length > 0)
                {
                    if (ShowQuestion("Do you really want to change MKVToolnix path?", "Are you sure?") != DialogResult.Yes)
                    {
                        return;
                        //throw new Exception("User abort!");
                    }
                }
                FolderBrowserDialog fbd = new FolderBrowserDialog();
                if (txtMKVToolnixPath.Text.Trim().Length > 0) 
                {
                    if (Directory.Exists(txtMKVToolnixPath.Text.Trim()))
                    {
                        fbd.SelectedPath = txtMKVToolnixPath.Text.Trim();
                    }
                }
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
     
        private void btnExtract_Click(object sender, EventArgs e)
        {
            try
            {
                tlpMain.Enabled = false;
                _gMkvExtract = new gMKVExtract(txtMKVToolnixPath.Text);
                _gMkvExtract.MkvExtractProgressUpdated += g_MkvExtractProgressUpdated;
                _gMkvExtract.MkvExtractTrackUpdated += g_MkvExtractTrackUpdated;

                Thread t = null;
                List<Object> parList = new List<object>();
                List<gMKVSegment> segments = new List<gMKVSegment>();
                switch ((MkvExtractionMode)Enum.Parse(typeof(MkvExtractionMode), (String)cmbExtractionMode.SelectedItem))
                {
                    case MkvExtractionMode.Tracks:
                        CheckNeccessaryInputFields(true, true);
                        
                        foreach (gMKVSegment seg in chkLstInputFileTracks.CheckedItems)
                        {
                            segments.Add(seg);
                        }
                 
                        t = new Thread(new ParameterizedThreadStart(_gMkvExtract.ExtractMKVSegmentsThreaded));
                        parList.Add(txtInputFile.Text);
                        parList.Add(segments);
                        parList.Add(txtOutputDirectory.Text);
                        parList.Add((MkvChapterTypes)Enum.Parse(typeof(MkvChapterTypes), (String)cmbChapterType.SelectedItem));
                        parList.Add(TimecodesExtractionMode.NoTimecodes);

                        break;
                    case MkvExtractionMode.Cue_Sheet:
                        CheckNeccessaryInputFields(false, false);
                  
                        t = new Thread(new ParameterizedThreadStart(_gMkvExtract.ExtractMkvCuesheetThreaded));
                        parList = new List<object>();
                        parList.Add(txtInputFile.Text);
                        parList.Add(txtOutputDirectory.Text);

                        break;
                    case MkvExtractionMode.Tags:
                        CheckNeccessaryInputFields(false, false);
                   
                        t = new Thread(new ParameterizedThreadStart(_gMkvExtract.ExtractMkvTagsThreaded));
                        parList = new List<object>();
                        parList.Add(txtInputFile.Text);
                        parList.Add(txtOutputDirectory.Text);

                        break;
                    case MkvExtractionMode.Timecodes:
                        CheckNeccessaryInputFields(true, true);
                       
                        foreach (gMKVSegment seg in chkLstInputFileTracks.CheckedItems)
                        {
                            segments.Add(seg);
                        }
                    
                        t = new Thread(new ParameterizedThreadStart(_gMkvExtract.ExtractMKVTimecodesThreaded));
                        parList = new List<object>();
                        parList.Add(txtInputFile.Text);
                        parList.Add(segments);
                        parList.Add(txtOutputDirectory.Text);
                        parList.Add((MkvChapterTypes)Enum.Parse(typeof(MkvChapterTypes), (String)cmbChapterType.SelectedItem));
                        parList.Add(TimecodesExtractionMode.OnlyTimecodes);

                        break;
                    case MkvExtractionMode.Tracks_And_Timecodes:
                        CheckNeccessaryInputFields(true, true);
                       
                        foreach (gMKVSegment seg in chkLstInputFileTracks.CheckedItems)
                        {
                            segments.Add(seg);
                        }
                    
                        t = new Thread(new ParameterizedThreadStart(_gMkvExtract.ExtractMKVSegmentsThreaded));
                        parList = new List<object>();
                        parList.Add(txtInputFile.Text);
                        parList.Add(segments);
                        parList.Add(txtOutputDirectory.Text);
                        parList.Add((MkvChapterTypes)Enum.Parse(typeof(MkvChapterTypes), (String)cmbChapterType.SelectedItem));
                        parList.Add(TimecodesExtractionMode.WithTimecodes);

                        break;
                }
                
                t.Start(parList);

                btnAbort.Enabled = true;
                btnAbortAll.Enabled = true;
                while (t.ThreadState != System.Threading.ThreadState.Stopped)
                {
                    Application.DoEvents();
                }
                // check for exceptions
                if (_gMkvExtract.ThreadedException != null)
                {
                    throw _gMkvExtract.ThreadedException;
                }
                ShowSuccessMessage("The extraction was completed successfully!");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                ShowErrorMessage(ex.Message);
            }
            finally
            {
                if (_gMkvExtract != null)
                {
                    _gMkvExtract.MkvExtractProgressUpdated -= g_MkvExtractProgressUpdated;
                    _gMkvExtract.MkvExtractTrackUpdated -= g_MkvExtractTrackUpdated;
                }
                _gMkvExtract = null;
                ClearStatus();
                tlpMain.Enabled = true;
                btnAbort.Enabled = false;
                btnAbortAll.Enabled = false;
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

        private void chkLockOutputDirectory_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                txtOutputDirectory.ReadOnly = chkLockOutputDirectory.Checked;
                btnBrowseOutputDirectory.Enabled = !chkLockOutputDirectory.Checked;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                ShowErrorMessage(ex.Message);
            }
        }

        private void btnAbort_Click(object sender, EventArgs e)
        {
            try
            {
                _gMkvExtract.Abort = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                ShowErrorMessage(ex.Message);
            }
        }

        private void btnAbortAll_Click(object sender, EventArgs e)
        {
            try
            {
                _gMkvExtract.AbortAll = true;
                _gMkvExtract.Abort = true;
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
