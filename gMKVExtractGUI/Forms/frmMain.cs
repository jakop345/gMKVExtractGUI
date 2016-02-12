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
    public enum TrackSelectionMode
    {
        video,
        audio,
        subtitle,
        chapter,
        attachment,
        all,
        none
    }

    public delegate void UpdateProgressDelegate(Object val);
    public delegate void UpdateTrackLabelDelegate(Object val);

    public partial class frmMain : gForm
    {
        private frmLog _LogForm = null;
        private frmJobManager _JobManagerForm = null;

        private gMKVExtract _gMkvExtract = null;
        private gSettings _Settings = new gSettings(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
        private Boolean _FromConstructor = false;
        private ToolTip _ToolTip = new ToolTip();
        private Boolean _JobMode = false;
        private Boolean _ExtractRunning = false;

        public frmMain()
        {
            try
            {
                InitializeComponent();

                // Set form icon from the executing assembly
                Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
                
                // Set form title 
                Text = String.Format("gMKVExtractGUI v{0} -- By Gpower2", Assembly.GetExecutingAssembly().GetName().Version);
                
                // Set tooltips
                SetTooltips();

                btnAbort.Enabled = false;
                btnAbortAll.Enabled = false;
                
                _FromConstructor = true;
                
                cmbChapterType.DataSource = Enum.GetNames(typeof(MkvChapterTypes));
                cmbExtractionMode.DataSource = Enum.GetNames(typeof(FormMkvExtractionMode));
                
                // Load settings
                _Settings.Reload();
                
                // Set form size and position from settings
                this.StartPosition = FormStartPosition.Manual;
                this.Location = new Point(_Settings.WindowPosX, _Settings.WindowPosY);
                this.Size = new System.Drawing.Size(_Settings.WindowSizeWidth, _Settings.WindowSizeHeight);
                this.WindowState = _Settings.WindowState;

                // Set chapter type, output durectory and job mode from settings
                cmbChapterType.SelectedItem = Enum.GetName(typeof(MkvChapterTypes), _Settings.ChapterType);
                txtOutputDirectory.Text = _Settings.OutputDirectory;
                chkLockOutputDirectory.Checked = _Settings.LockedOutputDirectory;
                chkJobMode.Checked = _Settings.JobMode;

                _FromConstructor = false;
                
                // Find MKVToolnix path
                try
                {
                    txtMKVToolnixPath.Text = gMKVHelper.GetMKVToolnixPathViaRegistry();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    // MKVToolnix was not found in registry
                    // check in the current directory
                    if (File.Exists(Path.Combine(GetCurrentDirectory(), gMKVHelper.MKV_MERGE_GUI_FILENAME))
                        || File.Exists(Path.Combine(GetCurrentDirectory(), gMKVHelper.MKV_MERGE_NEW_GUI_FILENAME)))
                    {
                        txtMKVToolnixPath.Text = GetCurrentDirectory();
                    }
                    else
                    {
                        // check for ini file
                        if (File.Exists(Path.Combine(_Settings.MkvToolnixPath, gMKVHelper.MKV_MERGE_GUI_FILENAME))
                            || File.Exists(Path.Combine(_Settings.MkvToolnixPath, gMKVHelper.MKV_MERGE_NEW_GUI_FILENAME)))
                        {
                            _FromConstructor = true;
                            txtMKVToolnixPath.Text = _Settings.MkvToolnixPath;
                            _FromConstructor = false;
                        }
                        else
                        {
                            throw new Exception("Could not find MKVToolNix in registry, or in the current directory, or in the ini file!\r\nPlease download and reinstall or provide a manual path!");
                        }
                    }
                }                
                
                // check if user provided with a filename when executing the application
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

        private void SetTooltips()
        {
            // General ToolTip properties
            //_ToolTip.AutoPopDelay = 10000;
            //_ToolTip.InitialDelay = 500;
            //_ToolTip.ReshowDelay = 100;
            //_ToolTip.IsBalloon = true;
            //_ToolTip.ToolTipIcon = ToolTipIcon.Info;
            //_ToolTip.ToolTipTitle = "gMKVExtractGUI Help";

            //// Assign Control Tooltip Text
            //StringBuilder txtBuilder = new StringBuilder();
            //txtBuilder.AppendFormat("\r\n");
            //txtBuilder.AppendFormat("The list of Tracks that the matroska file contains.\r\n");
            //txtBuilder.AppendFormat("\r\n");
            //txtBuilder.AppendFormat("Each Track is shown with its properties in brackets as follows:\r\n");
            //txtBuilder.AppendFormat("\r\n");
            //txtBuilder.AppendFormat("Video/Audio/Subtitle Tracks:\r\n");
            //txtBuilder.AppendFormat("\tTrack ID\r\n");
            //txtBuilder.AppendFormat("\tTrack Type\r\n");
            //txtBuilder.AppendFormat("\tCodec ID\r\n");
            //txtBuilder.AppendFormat("\tTrack Name\r\n");
            //txtBuilder.AppendFormat("\tLanguage\r\n");
            //txtBuilder.AppendFormat("\tExtra Info (resolution for video tracks, sample rate and channels for audio tracks)\r\n");
            //txtBuilder.AppendFormat("\tDelay (the actual track delay as defined in the mkv header)\r\n");
            //txtBuilder.AppendFormat("\tEffective Delay (the delay that players use while playing the file)\r\n");
            //txtBuilder.AppendFormat("\r\n");
            //txtBuilder.AppendFormat("Attachments:\r\n");
            //txtBuilder.AppendFormat("\tID\r\n");
            //txtBuilder.AppendFormat("\tFile name\r\n");
            //txtBuilder.AppendFormat("\tMime Type\r\n");
            //txtBuilder.AppendFormat("\tFile size\r\n");
            //txtBuilder.AppendFormat("\r\n");
            //txtBuilder.AppendFormat("Chapters:\r\n");
            //txtBuilder.AppendFormat("\tCount of chapter entries\r\n");
            //_ToolTip.SetToolTip(chkLstInputFileTracks, txtBuilder.ToString());
        }

        private void txt_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                // check if the drop data is actually a file or folder
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    // check for sender
                    if (((gTextBox)sender) == txtMKVToolnixPath)
                    {
                        // check if MKVToolnix Path is already set
                        if (txtMKVToolnixPath.Text.Trim().Length > 0)
                        {
                            if (ShowQuestion("Do you really want to change MKVToolnix path?", "Are you sure?") != DialogResult.Yes)
                            {
                                return;
                            }
                        }
                    }
                    else if (((gTextBox)sender) == txtOutputDirectory)
                    {
                        // check if output directory is locked
                        if (chkLockOutputDirectory.Checked)
                        {
                            return;
                        }                        
                    }
                    String[] s = (String[])e.Data.GetData(DataFormats.FileDrop, false);
                    ((gTextBox)sender).Text = s[0];
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
                    if (((gTextBox)sender) == txtOutputDirectory)
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

        private void grpInputFile_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                // check if the drop data is actually a file or folder
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    String[] s = (String[])e.Data.GetData(DataFormats.FileDrop, false);
                    txtInputFile.Text = s[0];
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                ShowErrorMessage(ex.Message);
            }
        }

        private void grpInputFile_DragEnter(object sender, DragEventArgs e)
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

        private void grpInputFileInfo_DragDrop(object sender, DragEventArgs e)
        {
            try
            {
                // check if the drop data is actually a file or folder
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    String[] s = (String[])e.Data.GetData(DataFormats.FileDrop, false);
                    txtInputFile.Text = s[0];
                }
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
                    if (!File.Exists(Path.Combine(txtMKVToolnixPath.Text.Trim(), gMKVHelper.MKV_MERGE_GUI_FILENAME))
                        && !File.Exists(Path.Combine(txtMKVToolnixPath.Text.Trim(), gMKVHelper.MKV_MERGE_NEW_GUI_FILENAME)))
                    {
                        _FromConstructor = true;
                        txtMKVToolnixPath.Text = String.Empty;
                        _FromConstructor = false;
                        throw new Exception("The folder does not contain MKVToolnix!");
                    }

                    // Write the value to the ini file
                    _Settings.MkvToolnixPath = txtMKVToolnixPath.Text.Trim();
                    _Settings.Save();
                }
                _gMkvExtract = new gMKVExtract(txtMKVToolnixPath.Text);
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
                        _Settings.ChapterType = (MkvChapterTypes)Enum.Parse(typeof(MkvChapterTypes), (String)cmbChapterType.SelectedItem);
                        _Settings.Save();
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
                Application.DoEvents();
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
                    gTaskbarProgress.SetState(this, gTaskbarProgress.TaskbarStates.Indeterminate);
                    List<gMKVSegment> segmentList = gMKVHelper.GetMergedMkvSegmentList(txtMKVToolnixPath.Text.Trim(), txtInputFile.Text.Trim());
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
                // Empty the text since input was wrong or something happened
                txtInputFile.Text = String.Empty;
                gTaskbarProgress.SetState(this, gTaskbarProgress.TaskbarStates.Error);
                ShowErrorMessage(ex.Message);
            }
            finally
            {
                tlpMain.Enabled = true;
                grpInputFileInfo.Text = String.Format("Input File Information ({0} Tracks)", chkLstInputFileTracks.Items.Count);
                this.Refresh();
                gTaskbarProgress.SetState(this, gTaskbarProgress.TaskbarStates.NoProgress);
                Application.DoEvents();
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

        private void txtOutputDirectory_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (!_FromConstructor)
                {
                    _Settings.OutputDirectory = txtOutputDirectory.Text;
                    _Settings.Save();
                }
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
                if (sender == chkLockOutputDirectory)
                {
                    if (txtOutputDirectory.Text.Trim().Length == 0)
                    {
                        chkLockOutputDirectory.Checked = false;
                    }
                }
                txtOutputDirectory.ReadOnly = chkLockOutputDirectory.Checked;
                btnBrowseOutputDirectory.Enabled = !chkLockOutputDirectory.Checked;
                if (!_FromConstructor)
                {
                    _Settings.LockedOutputDirectory = chkLockOutputDirectory.Checked;
                    _Settings.Save();
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
                _ExtractRunning = true;
                Application.DoEvents();
                _gMkvExtract.MkvExtractProgressUpdated += g_MkvExtractProgressUpdated;
                _gMkvExtract.MkvExtractTrackUpdated += g_MkvExtractTrackUpdated;

                Thread myThread = null;
                List<Object> parameterList = new List<object>();
                List<gMKVSegment> segmentList = new List<gMKVSegment>();
                gMKVJob job = null;
                FormMkvExtractionMode extractionMode = (FormMkvExtractionMode)Enum.Parse(typeof(FormMkvExtractionMode), (String)cmbExtractionMode.SelectedItem);
                switch (extractionMode)
                {
                    case FormMkvExtractionMode.Tracks:
                        CheckNeccessaryInputFields(true, true);
                        
                        foreach (gMKVSegment seg in chkLstInputFileTracks.CheckedItems)
                        {
                            segmentList.Add(seg);
                        }

                        parameterList.Add(txtInputFile.Text);
                        parameterList.Add(segmentList);
                        parameterList.Add(txtOutputDirectory.Text);
                        parameterList.Add((MkvChapterTypes)Enum.Parse(typeof(MkvChapterTypes), (String)cmbChapterType.SelectedItem));
                        parameterList.Add(TimecodesExtractionMode.NoTimecodes);
                        parameterList.Add(CuesExtractionMode.NoCues);

                        job = new gMKVJob(extractionMode, txtMKVToolnixPath.Text, parameterList);
                        break;
                    case FormMkvExtractionMode.Cue_Sheet:
                        CheckNeccessaryInputFields(false, false);
                  
                        parameterList = new List<object>();
                        parameterList.Add(txtInputFile.Text);
                        parameterList.Add(txtOutputDirectory.Text);

                        job = new gMKVJob(extractionMode, txtMKVToolnixPath.Text, parameterList);
                        break;
                    case FormMkvExtractionMode.Tags:
                        CheckNeccessaryInputFields(false, false);
                   
                        parameterList = new List<object>();
                        parameterList.Add(txtInputFile.Text);
                        parameterList.Add(txtOutputDirectory.Text);

                        job = new gMKVJob(extractionMode, txtMKVToolnixPath.Text, parameterList);
                        break;
                    case FormMkvExtractionMode.Timecodes:
                        CheckNeccessaryInputFields(true, false);
                       
                        foreach (gMKVSegment seg in chkLstInputFileTracks.CheckedItems)
                        {
                            segmentList.Add(seg);
                        }
                    
                        parameterList = new List<object>();
                        parameterList.Add(txtInputFile.Text);
                        parameterList.Add(segmentList);
                        parameterList.Add(txtOutputDirectory.Text);
                        parameterList.Add((MkvChapterTypes)Enum.Parse(typeof(MkvChapterTypes), (String)cmbChapterType.SelectedItem));
                        parameterList.Add(TimecodesExtractionMode.OnlyTimecodes);
                        parameterList.Add(CuesExtractionMode.NoCues);

                        job = new gMKVJob(extractionMode, txtMKVToolnixPath.Text, parameterList);
                        break;
                    case FormMkvExtractionMode.Tracks_And_Timecodes:
                        CheckNeccessaryInputFields(true, true);
                       
                        foreach (gMKVSegment seg in chkLstInputFileTracks.CheckedItems)
                        {
                            segmentList.Add(seg);
                        }
                    
                        parameterList = new List<object>();
                        parameterList.Add(txtInputFile.Text);
                        parameterList.Add(segmentList);
                        parameterList.Add(txtOutputDirectory.Text);
                        parameterList.Add((MkvChapterTypes)Enum.Parse(typeof(MkvChapterTypes), (String)cmbChapterType.SelectedItem));
                        parameterList.Add(TimecodesExtractionMode.WithTimecodes);
                        parameterList.Add(CuesExtractionMode.NoCues);

                        job = new gMKVJob(extractionMode, txtMKVToolnixPath.Text, parameterList);

                        break;
                    case FormMkvExtractionMode.Cues:
                        CheckNeccessaryInputFields(true, false);

                        foreach (gMKVSegment seg in chkLstInputFileTracks.CheckedItems)
                        {
                            segmentList.Add(seg);
                        }

                        parameterList = new List<object>();
                        parameterList.Add(txtInputFile.Text);
                        parameterList.Add(segmentList);
                        parameterList.Add(txtOutputDirectory.Text);
                        parameterList.Add((MkvChapterTypes)Enum.Parse(typeof(MkvChapterTypes), (String)cmbChapterType.SelectedItem));
                        parameterList.Add(TimecodesExtractionMode.NoTimecodes);
                        parameterList.Add(CuesExtractionMode.OnlyCues);

                        job = new gMKVJob(extractionMode, txtMKVToolnixPath.Text, parameterList);
                        break;
                    case FormMkvExtractionMode.Tracks_And_Cues:
                        CheckNeccessaryInputFields(true, false);

                        foreach (gMKVSegment seg in chkLstInputFileTracks.CheckedItems)
                        {
                            segmentList.Add(seg);
                        }

                        parameterList = new List<object>();
                        parameterList.Add(txtInputFile.Text);
                        parameterList.Add(segmentList);
                        parameterList.Add(txtOutputDirectory.Text);
                        parameterList.Add((MkvChapterTypes)Enum.Parse(typeof(MkvChapterTypes), (String)cmbChapterType.SelectedItem));
                        parameterList.Add(TimecodesExtractionMode.NoTimecodes);
                        parameterList.Add(CuesExtractionMode.WithCues);

                        job = new gMKVJob(extractionMode, txtMKVToolnixPath.Text, parameterList);
                        break;
                    case FormMkvExtractionMode.Tracks_And_Cues_And_Timecodes:
                        CheckNeccessaryInputFields(true, false);

                        foreach (gMKVSegment seg in chkLstInputFileTracks.CheckedItems)
                        {
                            segmentList.Add(seg);
                        }

                        parameterList = new List<object>();
                        parameterList.Add(txtInputFile.Text);
                        parameterList.Add(segmentList);
                        parameterList.Add(txtOutputDirectory.Text);
                        parameterList.Add((MkvChapterTypes)Enum.Parse(typeof(MkvChapterTypes), (String)cmbChapterType.SelectedItem));
                        parameterList.Add(TimecodesExtractionMode.WithTimecodes);
                        parameterList.Add(CuesExtractionMode.WithCues);

                        job = new gMKVJob(extractionMode, txtMKVToolnixPath.Text, parameterList);
                        break;
                }
                if (_JobMode)
                {
                    if (_JobManagerForm == null)
                    {
                        _JobManagerForm = new frmJobManager(this);
                    }
                    _JobManagerForm.Show();
                    _JobManagerForm.AddJob(new gMKVJobInfo(job));
                }
                else
                {
                    // start the thread
                    myThread = new Thread(new ParameterizedThreadStart(job.ExtractMethod(_gMkvExtract)));
                    myThread.Start(job.ParametersList);

                    btnAbort.Enabled = true;
                    btnAbortAll.Enabled = true;
                    gTaskbarProgress.SetState(this, gTaskbarProgress.TaskbarStates.Normal);
                    gTaskbarProgress.SetOverlayIcon(this, SystemIcons.Shield, "Extracting...");
                    Application.DoEvents();
                    while (myThread.ThreadState != System.Threading.ThreadState.Stopped)
                    {
                        Application.DoEvents();
                    }
                    // check for exceptions
                    if (_gMkvExtract.ThreadedException != null)
                    {
                        throw _gMkvExtract.ThreadedException;
                    }
                    UpdateProgress(100);
                    ShowSuccessMessage("The extraction was completed successfully!");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);

                gTaskbarProgress.SetState(this, gTaskbarProgress.TaskbarStates.Error);
                gTaskbarProgress.SetOverlayIcon(this, SystemIcons.Error, "Error!");
                ShowErrorMessage(ex.Message);
            }
            finally
            {
                if (_gMkvExtract != null)
                {
                    _gMkvExtract.MkvExtractProgressUpdated -= g_MkvExtractProgressUpdated;
                    _gMkvExtract.MkvExtractTrackUpdated -= g_MkvExtractTrackUpdated;
                }
                ClearStatus();
                _ExtractRunning = false;
                tlpMain.Enabled = true;
                btnAbort.Enabled = false;
                btnAbortAll.Enabled = false;
                gTaskbarProgress.SetState(this, gTaskbarProgress.TaskbarStates.NoProgress);
                gTaskbarProgress.SetOverlayIcon(this, null, null);
                this.Refresh(); 
                Application.DoEvents();
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
                if (_JobMode)
                {
                    if (_JobManagerForm == null)
                    {
                        _JobManagerForm = new frmJobManager(this);
                    }
                    _JobManagerForm.Show();
                    _JobManagerForm.Focus();
                    _JobManagerForm.Select();
                }
                else
                {
                    if (_LogForm == null) { _LogForm = new frmLog(); }
                    _LogForm.Show();
                    _LogForm.Focus();
                    _LogForm.Select();
                }
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
                if (!File.Exists(Path.Combine(txtMKVToolnixPath.Text.Trim(), gMKVHelper.MKV_MERGE_GUI_FILENAME))
                    && !File.Exists(Path.Combine(txtMKVToolnixPath.Text.Trim(), gMKVHelper.MKV_MERGE_NEW_GUI_FILENAME)))
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

                FormMkvExtractionMode selectedExtractionMode = (FormMkvExtractionMode)Enum.Parse(typeof(FormMkvExtractionMode), (String)cmbExtractionMode.SelectedItem);

                if (selectedExtractionMode == FormMkvExtractionMode.Timecodes ||
                    selectedExtractionMode == FormMkvExtractionMode.Tracks_And_Timecodes ||
                    selectedExtractionMode == FormMkvExtractionMode.Tracks_And_Cues_And_Timecodes)
                {
                    Boolean ok = false;
                    foreach (gMKVSegment item in chkLstInputFileTracks.CheckedItems)
                    {
                        if (item is gMKVTrack)
                        {
                            ok = true;
                            break;
                        }
                    }
                    if (!ok)
                    {
                        throw new Exception("You must select a video, audio or subtitles track to extract timecodes!");
                    }
                }

                if (selectedExtractionMode == FormMkvExtractionMode.Cues ||
                    selectedExtractionMode == FormMkvExtractionMode.Tracks_And_Cues ||
                    selectedExtractionMode == FormMkvExtractionMode.Tracks_And_Cues_And_Timecodes)
                {
                    Boolean ok = false;
                    foreach (gMKVSegment item in chkLstInputFileTracks.CheckedItems)
                    {
                        if (item is gMKVTrack)
                        {
                            ok = true;
                            break;
                        }
                    }
                    if (!ok)
                    {
                        throw new Exception("You must select a video, audio or subtitles track to extract cues!");
                    }
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

        public void UpdateProgress(Object val)
        {
            prgBrStatus.Value = Convert.ToInt32(val);
            lblStatus.Text = String.Format("{0}%", Convert.ToInt32(val));
            gTaskbarProgress.SetValue(this, Convert.ToUInt64(val), (UInt64)100);
            Application.DoEvents();
        }

        public void UpdateTrackLabel(Object val)
        {
            lblTrack.Text = (String)val;
            Application.DoEvents();
        }

        private void SetContextMenuText()
        {
            Int32 videoTracksCount = 0;
            Int32 audioTracksCount = 0;
            Int32 subtitleTracksCount = 0;
            Int32 attachmentTracksCount = 0;
            Int32 chapterTracksCount = 0;
            Int32 selectedVideoTracksCount = 0;
            Int32 selectedAudioTracksCount = 0;
            Int32 selectedSubtitleTracksCount = 0;
            Int32 selectedAttachmentTracksCount = 0;
            Int32 selectedChapterTracksCount = 0;

            for (Int32 i = 0; i < chkLstInputFileTracks.Items.Count; i++)
            {
                gMKVSegment segObject = (gMKVSegment)chkLstInputFileTracks.Items[i];
                if (segObject is gMKVTrack)
                {
                    switch (((gMKVTrack)segObject).TrackType)
                    {
                        case MkvTrackType.video:
                            videoTracksCount++;
                            if (chkLstInputFileTracks.GetItemChecked(i)) { selectedVideoTracksCount++; }
                            break;
                        case MkvTrackType.audio:
                            audioTracksCount++;
                             if (chkLstInputFileTracks.GetItemChecked(i)) { selectedAudioTracksCount++; }
                           break;
                        case MkvTrackType.subtitles:
                            subtitleTracksCount++;
                              if (chkLstInputFileTracks.GetItemChecked(i)) { selectedSubtitleTracksCount++; }
                          break;
                        default:
                            break;
                    }
                }
                else if (segObject is gMKVAttachment)
                {
                    attachmentTracksCount++;
                    if (chkLstInputFileTracks.GetItemChecked(i)) { selectedAttachmentTracksCount++; }
                }
                else if (segObject is gMKVChapter)
                {
                    chapterTracksCount++;
                    if (chkLstInputFileTracks.GetItemChecked(i)) { selectedChapterTracksCount++; }
                }
            }
            selectAllVideoTracksToolStripMenuItem.Enabled = (videoTracksCount > 0);
            selectAllAudioTracksToolStripMenuItem.Enabled = (audioTracksCount > 0);
            selectAllSubtitleTracksToolStripMenuItem.Enabled = (subtitleTracksCount > 0);
            selectAllChapterTracksToolStripMenuItem.Enabled = (chapterTracksCount > 0);
            selectAllAttachmentsToolStripMenuItem.Enabled = (attachmentTracksCount > 0);
            selectAllTracksToolStripMenuItem.Enabled = (chkLstInputFileTracks.Items.Count > 0);
            unselectAllTracksToolStripMenuItem.Enabled = (chkLstInputFileTracks.Items.Count > 0);

            selectAllVideoTracksToolStripMenuItem.Text = String.Format("Select All Video Tracks ({1}/{0})", videoTracksCount, selectedVideoTracksCount);
            selectAllAudioTracksToolStripMenuItem.Text = String.Format("Select All Audio Tracks ({1}/{0})", audioTracksCount, selectedAudioTracksCount);
            selectAllSubtitleTracksToolStripMenuItem.Text = String.Format("Select All Subtitle Tracks ({1}/{0})", subtitleTracksCount, selectedSubtitleTracksCount);
            selectAllChapterTracksToolStripMenuItem.Text = String.Format("Select All Chapter Tracks ({1}/{0})", chapterTracksCount, selectedChapterTracksCount);
            selectAllAttachmentsToolStripMenuItem.Text = String.Format("Select All Attachment Tracks ({1}/{0})", attachmentTracksCount, selectedAttachmentTracksCount);
            selectAllTracksToolStripMenuItem.Text = String.Format("Select All Tracks ({1}/{0})", chkLstInputFileTracks.Items.Count, chkLstInputFileTracks.CheckedItems.Count);
            unselectAllTracksToolStripMenuItem.Text = String.Format("Unselect All Tracks ({1}/{0})", chkLstInputFileTracks.Items.Count, chkLstInputFileTracks.CheckedItems.Count);
        }

        private void SetTrackSelection(TrackSelectionMode argSelectionMode)
        {
            for (Int32 i = 0; i < chkLstInputFileTracks.Items.Count; i++)
            {
                gMKVSegment seg = (gMKVSegment)chkLstInputFileTracks.Items[i];
                switch (argSelectionMode)
                {
                    case TrackSelectionMode.video:
                        if (seg is gMKVTrack)
                        {
                            if (((gMKVTrack)seg).TrackType == MkvTrackType.video)
                            {
                                chkLstInputFileTracks.SetItemChecked(i, true);
                            }
                        }
                        break;
                    case TrackSelectionMode.audio:
                        if (seg is gMKVTrack)
                        {
                            if (((gMKVTrack)seg).TrackType == MkvTrackType.audio)
                            {
                                chkLstInputFileTracks.SetItemChecked(i, true);
                            }
                        }
                        break;
                    case TrackSelectionMode.subtitle:
                        if (seg is gMKVTrack)
                        {
                            if (((gMKVTrack)seg).TrackType == MkvTrackType.subtitles)
                            {
                                chkLstInputFileTracks.SetItemChecked(i, true);
                            }
                        }
                        break;
                    case TrackSelectionMode.chapter:
                        if (seg is gMKVChapter)
                        {
                            chkLstInputFileTracks.SetItemChecked(i, true);
                        }
                        break;
                    case TrackSelectionMode.attachment:
                        if (seg is gMKVAttachment)
                        {
                            chkLstInputFileTracks.SetItemChecked(i, true);
                        }
                        break;
                    case TrackSelectionMode.all:
                        chkLstInputFileTracks.SetItemChecked(i, true);
                        break;
                    case TrackSelectionMode.none:
                        chkLstInputFileTracks.SetItemChecked(i, false);
                        break;
                    default:
                        break;
                }
            }
        }

        private void selectAllTracksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetTrackSelection(TrackSelectionMode.all);
        }

        private void selectAllVideoTracksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetTrackSelection(TrackSelectionMode.video);
        }

        private void selectAllAudioTracksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetTrackSelection(TrackSelectionMode.audio);
        }

        private void selectAllSubtitleTracksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetTrackSelection(TrackSelectionMode.subtitle);
        }

        private void selectAllChapterTracksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetTrackSelection(TrackSelectionMode.chapter);
        }

        private void selectAllAttachmentsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetTrackSelection(TrackSelectionMode.attachment);
        }

        private void unselectAllTracksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SetTrackSelection(TrackSelectionMode.none);
        }

        private void contextMenuStrip_Opening(object sender, CancelEventArgs e)
        {
            SetContextMenuText();
        }

        private void frmMain_Move(object sender, EventArgs e)
        {
            try
            {
                if (!_FromConstructor && !(
                    this.WindowState == FormWindowState.Minimized
                    || this.WindowState == FormWindowState.Maximized))
                {
                    _Settings.WindowPosX = this.Location.X;
                    _Settings.WindowPosY = this.Location.Y;
                    _Settings.WindowState = this.WindowState;
                    _Settings.Save();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void frmMain_ResizeEnd(object sender, EventArgs e)
        {
            try
            {
                if (!_FromConstructor)
                {
                    _Settings.WindowSizeWidth = this.Size.Width;
                    _Settings.WindowSizeHeight = this.Size.Height;
                    _Settings.WindowState = this.WindowState;
                    _Settings.Save();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void chkJobMode_CheckedChanged(object sender, EventArgs e)
        {
            _JobMode = chkJobMode.Checked;
            btnExtract.Text = _JobMode ? "Add job" : "Extract";
            btnShowLog.Text = _JobMode ? "Jobs..." : "Log...";
            if (!_FromConstructor)
            {
                _Settings.JobMode = chkJobMode.Checked;
                _Settings.Save();
            }
        }

        public void SetTableLayoutMainStatus(Boolean argStatus)
        {
            tlpMain.Enabled = argStatus;
            Application.DoEvents();
        }

        private void frmMain_ClientSizeChanged(object sender, EventArgs e)
        {
            try
            {
                if (!_FromConstructor)
                {
                    _Settings.WindowState = this.WindowState;
                    _Settings.Save();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (_ExtractRunning)
                {
                    e.Cancel = true;
                    ShowErrorMessage("There is an extraction process running! Please abort before closing!");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                e.Cancel = true;
                ShowErrorMessage(ex.Message);
            }
        }
    }
}
