using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace gMKVToolnix
{
    public partial class frmJobManager : gForm
    {
        private gMKVExtract _gMkvExtract = null;
        private StringBuilder _ExceptionBuilder = new StringBuilder();
        private frmMain _MainForm = null;
        private Int32 _CurrentJob = 0;
        private Int32 _TotalJobs = 0;

        private Boolean _AbortAll = false;
        
        public frmJobManager(gMKVExtract argGMkvExtract, frmMain argMainForm)
        {
            InitializeComponent();

            Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
            Text = String.Format("gMKVExtractGUI v{0} -- Job Manager", Assembly.GetExecutingAssembly().GetName().Version);

            _gMkvExtract = argGMkvExtract;
            _MainForm = argMainForm;

            SetAbortStatus(false);
        }

        public void AddJob(gMKVJob argJob)
        {
            lstJobs.Items.Add(argJob);
        }

        private void SetAbortStatus(Boolean argStatus)
        {
            btnAbort.Enabled = argStatus;
            btnAbortAll.Enabled = argStatus;
        }

        private void SetActionStatus(Boolean argStatus)
        {
            btnRemove.Enabled = argStatus;
            btnRunAll.Enabled = argStatus;
            btnRunSelection.Enabled = argStatus;
        }

        void _gMkvExtract_MkvExtractTrackUpdated(string trackName)
        {
            Debug.WriteLine(trackName);
        }

        void _gMkvExtract_MkvExtractProgressUpdated(int progress)
        {
            this.Invoke(new UpdateProgressDelegate(UpdateCurrentProgress), new object[] { progress });
        }

        private void frmJobManager_FormClosing(object sender, FormClosingEventArgs e)
        {
            // To avoid getting disposed
            e.Cancel = true;
            this.Hide();
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            try
            {
                if (lstJobs.Items.Count > 0)
                {
                    if (lstJobs.SelectedItems.Count > 0)
                    {
                        while (lstJobs.SelectedItems.Count > 0)
                        {
                            lstJobs.Items.Remove(lstJobs.SelectedItems[0]);
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

        private void RunJobs(List<gMKVJob> argJobList)
        {
            _ExceptionBuilder.Length = 0;
            foreach (gMKVJob job in argJobList)
            {
                try
                {
                    // check for abort
                    if (_AbortAll)
                    {
                        break;
                    }
                    // increate the current job index
                    _CurrentJob++;
                    // start the thread
                    Thread myThread = new Thread(new ParameterizedThreadStart(job.ExtractMethod));
                    myThread.Start(job.ParametersList);

                    btnAbort.Enabled = true;
                    btnAbortAll.Enabled = true;
                    gTaskbarProgress.SetState(this, gTaskbarProgress.TaskbarStates.Normal);
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
                    else
                    {
                        // Remove the finished job
                        lstJobs.Items.Remove(job);
                        Application.DoEvents();
                    }
                }
                catch (Exception ex)
                {
                    _ExceptionBuilder.AppendFormat("Exception for job {0}: {1}\r\n", job.ToString(), ex.Message);
                }
            }
        }

        public void UpdateCurrentProgress(Object val)
        {
            prgBrCurrent.Value = Convert.ToInt32(val);
            prgBrTotal.Value = (_CurrentJob - 1) * 100 + Convert.ToInt32(val);
            lblCurrentProgressValue.Text = String.Format("{0}%", Convert.ToInt32(val));
            lblTotalProgressValue.Text = String.Format("{0}%", prgBrTotal.Value / _TotalJobs);
            gTaskbarProgress.SetValue(this, Convert.ToUInt64(val), (UInt64)100);
            Application.DoEvents();
        }

        private void btnRunSelection_Click(object sender, EventArgs e)
        {
            if (lstJobs.SelectedItems.Count == 0)
            {
                throw new Exception("There are no selected jobs!");
            }
            List<gMKVJob> jobList = new List<gMKVJob>();
            foreach (Object item in lstJobs.SelectedItems)
            {
                jobList.Add((gMKVJob)item);
            }
            PrepareForRunJobs(jobList);
        }

        private void btnRunAll_Click(object sender, EventArgs e)
        {
            if (lstJobs.Items.Count == 0)
            {
                throw new Exception("There are no available jobs to run!");
            }
            List<gMKVJob> jobList = new List<gMKVJob>();
            foreach (Object item in lstJobs.Items)
            {
                jobList.Add((gMKVJob)item);
            }
            PrepareForRunJobs(jobList);
        }

        private void PrepareForRunJobs(List<gMKVJob> argJobList)
        {
            try
            {
                SetActionStatus(false);
                SetAbortStatus(true);
                _MainForm.SetTableLayoutMainStatus(false);
                _gMkvExtract.MkvExtractProgressUpdated += _gMkvExtract_MkvExtractProgressUpdated;
                _gMkvExtract.MkvExtractTrackUpdated += _gMkvExtract_MkvExtractTrackUpdated;
                _TotalJobs = argJobList.Count;
                prgBrTotal.Maximum = _TotalJobs * 100;
                RunJobs(new List<gMKVJob>(argJobList));
                // Check exception builder for exceptions
                if (_ExceptionBuilder.Length > 0)
                {
                    throw new Exception(_ExceptionBuilder.ToString());
                }
                UpdateCurrentProgress(100);
                ShowSuccessMessage("The jobs completed successfully!");
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
                    _gMkvExtract.MkvExtractProgressUpdated -= _gMkvExtract_MkvExtractProgressUpdated;
                    _gMkvExtract.MkvExtractTrackUpdated -= _gMkvExtract_MkvExtractTrackUpdated;
                }
                UpdateCurrentProgress(0);
                prgBrTotal.Value = 0;
                lblCurrentProgressValue.Text = string.Empty;
                lblTotalProgressValue.Text = string.Empty;
                _AbortAll = false;
                SetActionStatus(true);
                SetAbortStatus(false);
                _MainForm.SetTableLayoutMainStatus(true);
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
                _AbortAll = true;
                _gMkvExtract.Abort = true;
                _gMkvExtract.AbortAll = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                ShowErrorMessage(ex.Message);
            }
        }
    }
}
