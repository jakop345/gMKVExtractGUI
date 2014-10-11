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

        private BindingList<gMKVJobInfo> _JobList = new BindingList<gMKVJobInfo>();

        private Boolean _AbortAll = false;
        
        public frmJobManager(gMKVExtract argGMkvExtract, frmMain argMainForm)
        {
            InitializeComponent();

            Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
            Text = String.Format("gMKVExtractGUI v{0} -- Job Manager", Assembly.GetExecutingAssembly().GetName().Version);

            _gMkvExtract = argGMkvExtract;
            _MainForm = argMainForm;

            grdJobs.DataSource = _JobList;

            SetAbortStatus(false);
        }

        public void AddJob(gMKVJobInfo argJobInfo)
        {
            //lstJobs.Items.Add(argJob);
            _JobList.Add(argJobInfo);
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
                if (grdJobs.Rows.Count > 0)
                {
                    if (grdJobs.SelectedRows.Count > 0)
                    {
                        List<Int32> selectionList = new List<Int32>();
                        foreach (DataGridViewRow item in grdJobs.SelectedRows)
                        {
                            selectionList.Add(item.Index);
                        }
                        foreach (Int32 idx in selectionList)
                        {
                            grdJobs.Rows.RemoveAt(idx);
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

        private void RunJobs(List<gMKVJobInfo> argJobInfoList)
        {
            _ExceptionBuilder.Length = 0;
            foreach (gMKVJobInfo jobInfo in argJobInfoList)
            {
                try
                {
                    // get job from jobInfo
                    gMKVJob job = jobInfo.Job;
                    // check for abort
                    if (_AbortAll)
                    {
                        break;
                    }
                    // increate the current job index
                    _CurrentJob++;
                    // start the thread
                    Thread myThread = new Thread(new ParameterizedThreadStart(job.ExtractMethod));
                    jobInfo.StartTime = DateTime.Now;
                    jobInfo.State = JobState.Running;
                    grdJobs.Refresh();
                    myThread.Start(job.ParametersList);

                    btnAbort.Enabled = true;
                    btnAbortAll.Enabled = true;
                    gTaskbarProgress.SetState(this, gTaskbarProgress.TaskbarStates.Normal);
                    Application.DoEvents();
                    while (myThread.ThreadState != System.Threading.ThreadState.Stopped)
                    {
                        Application.DoEvents();
                    }
                    jobInfo.EndTime = DateTime.Now;
                    // check for exceptions
                    if (_gMkvExtract.ThreadedException != null)
                    {
                        jobInfo.State = JobState.Failed;
                        grdJobs.Refresh();
                        throw _gMkvExtract.ThreadedException;
                    }
                    else
                    {
                        jobInfo.State = JobState.Completed;
                        grdJobs.Refresh();
                        Application.DoEvents();
                    }
                }
                catch (Exception ex)
                {
                    _ExceptionBuilder.AppendFormat("Exception for job {0}: {1}\r\n", jobInfo.ToString(), ex.Message);
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
            grdJobs.Refresh();
            Application.DoEvents();
        }

        private void btnRunAll_Click(object sender, EventArgs e)
        {
            if (grdJobs.SelectedRows.Count == 0)
            {
                throw new Exception("There are no available jobs to run!");
            }
            List<gMKVJobInfo> jobList = new List<gMKVJobInfo>();
            foreach (Object item in grdJobs.Rows)
            {
                gMKVJobInfo jobInfo = (gMKVJobInfo)((DataGridViewRow)item).DataBoundItem;
                jobInfo.State = JobState.Pending;
                jobList.Add(jobInfo);
            }
            grdJobs.Refresh();
            PrepareForRunJobs(jobList);
        }

        private void PrepareForRunJobs(List<gMKVJobInfo> argJobInfoList)
        {
            try
            {
                SetActionStatus(false);
                SetAbortStatus(true);
                _MainForm.SetTableLayoutMainStatus(false);
                _gMkvExtract.MkvExtractProgressUpdated += _gMkvExtract_MkvExtractProgressUpdated;
                _gMkvExtract.MkvExtractTrackUpdated += _gMkvExtract_MkvExtractTrackUpdated;
                _TotalJobs = argJobInfoList.Count;
                prgBrTotal.Maximum = _TotalJobs * 100;
                RunJobs(new List<gMKVJobInfo>(argJobInfoList));
                // Check exception builder for exceptions
                if (_ExceptionBuilder.Length > 0)
                {
                    // reset the status from pending to ready
                    foreach (DataGridViewRow item in grdJobs.Rows)
                    {
                        gMKVJobInfo jobInfo = (gMKVJobInfo)item.DataBoundItem;
                        if (jobInfo.State == JobState.Pending)
                        {
                            jobInfo.State = JobState.Ready;
                        }
                    }
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
                grdJobs.Refresh();
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
