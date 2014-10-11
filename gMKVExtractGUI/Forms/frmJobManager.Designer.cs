namespace gMKVToolnix
{
    partial class frmJobManager
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tlpMain = new gMKVToolnix.gTableLayoutPanel();
            this.grpProgress = new gMKVToolnix.gGroupBox();
            this.lblTotalProgressValue = new System.Windows.Forms.Label();
            this.lblCurrentProgressValue = new System.Windows.Forms.Label();
            this.lblTotalProgress = new System.Windows.Forms.Label();
            this.lblCurrentProgress = new System.Windows.Forms.Label();
            this.prgBrTotal = new System.Windows.Forms.ProgressBar();
            this.prgBrCurrent = new System.Windows.Forms.ProgressBar();
            this.tlpJobs = new gMKVToolnix.gTableLayoutPanel();
            this.grpJobs = new gMKVToolnix.gGroupBox();
            this.grdJobs = new gMKVToolnix.Controls.gDataGridView();
            this.grpActions = new gMKVToolnix.gGroupBox();
            this.btnAbortAll = new System.Windows.Forms.Button();
            this.btnAbort = new System.Windows.Forms.Button();
            this.btnRunAll = new System.Windows.Forms.Button();
            this.btnRemove = new System.Windows.Forms.Button();
            this.tlpMain.SuspendLayout();
            this.grpProgress.SuspendLayout();
            this.tlpJobs.SuspendLayout();
            this.grpJobs.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdJobs)).BeginInit();
            this.grpActions.SuspendLayout();
            this.SuspendLayout();
            // 
            // tlpMain
            // 
            this.tlpMain.ColumnCount = 1;
            this.tlpMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tlpMain.Controls.Add(this.grpProgress, 0, 1);
            this.tlpMain.Controls.Add(this.tlpJobs, 0, 0);
            this.tlpMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpMain.Location = new System.Drawing.Point(0, 0);
            this.tlpMain.Name = "tlpMain";
            this.tlpMain.RowCount = 2;
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.tlpMain.Size = new System.Drawing.Size(624, 361);
            this.tlpMain.TabIndex = 5;
            // 
            // grpProgress
            // 
            this.grpProgress.Controls.Add(this.lblTotalProgressValue);
            this.grpProgress.Controls.Add(this.lblCurrentProgressValue);
            this.grpProgress.Controls.Add(this.lblTotalProgress);
            this.grpProgress.Controls.Add(this.lblCurrentProgress);
            this.grpProgress.Controls.Add(this.prgBrTotal);
            this.grpProgress.Controls.Add(this.prgBrCurrent);
            this.grpProgress.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpProgress.Location = new System.Drawing.Point(3, 284);
            this.grpProgress.Name = "grpProgress";
            this.grpProgress.Size = new System.Drawing.Size(618, 74);
            this.grpProgress.TabIndex = 3;
            this.grpProgress.TabStop = false;
            this.grpProgress.Text = "Progress";
            // 
            // lblTotalProgressValue
            // 
            this.lblTotalProgressValue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblTotalProgressValue.AutoSize = true;
            this.lblTotalProgressValue.Location = new System.Drawing.Point(573, 50);
            this.lblTotalProgressValue.Name = "lblTotalProgressValue";
            this.lblTotalProgressValue.Size = new System.Drawing.Size(0, 15);
            this.lblTotalProgressValue.TabIndex = 6;
            // 
            // lblCurrentProgressValue
            // 
            this.lblCurrentProgressValue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblCurrentProgressValue.AutoSize = true;
            this.lblCurrentProgressValue.Location = new System.Drawing.Point(573, 22);
            this.lblCurrentProgressValue.Name = "lblCurrentProgressValue";
            this.lblCurrentProgressValue.Size = new System.Drawing.Size(0, 15);
            this.lblCurrentProgressValue.TabIndex = 5;
            // 
            // lblTotalProgress
            // 
            this.lblTotalProgress.AutoSize = true;
            this.lblTotalProgress.Location = new System.Drawing.Point(8, 50);
            this.lblTotalProgress.Name = "lblTotalProgress";
            this.lblTotalProgress.Size = new System.Drawing.Size(82, 15);
            this.lblTotalProgress.TabIndex = 4;
            this.lblTotalProgress.Text = "Total Progress";
            // 
            // lblCurrentProgress
            // 
            this.lblCurrentProgress.AutoSize = true;
            this.lblCurrentProgress.Location = new System.Drawing.Point(8, 22);
            this.lblCurrentProgress.Name = "lblCurrentProgress";
            this.lblCurrentProgress.Size = new System.Drawing.Size(95, 15);
            this.lblCurrentProgress.TabIndex = 3;
            this.lblCurrentProgress.Text = "Current Progress";
            // 
            // prgBrTotal
            // 
            this.prgBrTotal.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.prgBrTotal.Location = new System.Drawing.Point(107, 46);
            this.prgBrTotal.Name = "prgBrTotal";
            this.prgBrTotal.Size = new System.Drawing.Size(460, 20);
            this.prgBrTotal.TabIndex = 2;
            // 
            // prgBrCurrent
            // 
            this.prgBrCurrent.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.prgBrCurrent.Location = new System.Drawing.Point(107, 18);
            this.prgBrCurrent.Name = "prgBrCurrent";
            this.prgBrCurrent.Size = new System.Drawing.Size(460, 20);
            this.prgBrCurrent.TabIndex = 1;
            // 
            // tlpJobs
            // 
            this.tlpJobs.ColumnCount = 2;
            this.tlpJobs.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpJobs.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110F));
            this.tlpJobs.Controls.Add(this.grpJobs, 0, 0);
            this.tlpJobs.Controls.Add(this.grpActions, 1, 0);
            this.tlpJobs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpJobs.Location = new System.Drawing.Point(0, 0);
            this.tlpJobs.Margin = new System.Windows.Forms.Padding(0);
            this.tlpJobs.Name = "tlpJobs";
            this.tlpJobs.RowCount = 1;
            this.tlpJobs.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpJobs.Size = new System.Drawing.Size(624, 281);
            this.tlpJobs.TabIndex = 4;
            // 
            // grpJobs
            // 
            this.grpJobs.Controls.Add(this.grdJobs);
            this.grpJobs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpJobs.Location = new System.Drawing.Point(3, 3);
            this.grpJobs.Name = "grpJobs";
            this.grpJobs.Size = new System.Drawing.Size(508, 275);
            this.grpJobs.TabIndex = 0;
            this.grpJobs.TabStop = false;
            this.grpJobs.Text = "Jobs";
            // 
            // grdJobs
            // 
            this.grdJobs.AllowUserToAddRows = false;
            this.grdJobs.AllowUserToDeleteRows = false;
            this.grdJobs.AllowUserToResizeRows = false;
            this.grdJobs.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.grdJobs.BackgroundColor = System.Drawing.Color.White;
            this.grdJobs.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.grdJobs.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grdJobs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdJobs.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.grdJobs.Location = new System.Drawing.Point(3, 19);
            this.grdJobs.Name = "grdJobs";
            this.grdJobs.ReadOnly = true;
            this.grdJobs.RowHeadersVisible = false;
            this.grdJobs.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grdJobs.Size = new System.Drawing.Size(502, 253);
            this.grdJobs.TabIndex = 1;
            this.grdJobs.CellContentDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.grdJobs_CellContentDoubleClick);
            // 
            // grpActions
            // 
            this.grpActions.Controls.Add(this.btnAbortAll);
            this.grpActions.Controls.Add(this.btnAbort);
            this.grpActions.Controls.Add(this.btnRunAll);
            this.grpActions.Controls.Add(this.btnRemove);
            this.grpActions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpActions.Location = new System.Drawing.Point(517, 3);
            this.grpActions.Name = "grpActions";
            this.grpActions.Size = new System.Drawing.Size(104, 275);
            this.grpActions.TabIndex = 4;
            this.grpActions.TabStop = false;
            this.grpActions.Text = "Actions";
            // 
            // btnAbortAll
            // 
            this.btnAbortAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnAbortAll.Location = new System.Drawing.Point(7, 242);
            this.btnAbortAll.Name = "btnAbortAll";
            this.btnAbortAll.Size = new System.Drawing.Size(90, 30);
            this.btnAbortAll.TabIndex = 4;
            this.btnAbortAll.Text = "Abort All";
            this.btnAbortAll.UseVisualStyleBackColor = true;
            this.btnAbortAll.Click += new System.EventHandler(this.btnAbortAll_Click);
            // 
            // btnAbort
            // 
            this.btnAbort.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnAbort.Location = new System.Drawing.Point(7, 206);
            this.btnAbort.Name = "btnAbort";
            this.btnAbort.Size = new System.Drawing.Size(90, 30);
            this.btnAbort.TabIndex = 3;
            this.btnAbort.Text = "Abort";
            this.btnAbort.UseVisualStyleBackColor = true;
            this.btnAbort.Click += new System.EventHandler(this.btnAbort_Click);
            // 
            // btnRunAll
            // 
            this.btnRunAll.Location = new System.Drawing.Point(7, 72);
            this.btnRunAll.Name = "btnRunAll";
            this.btnRunAll.Size = new System.Drawing.Size(90, 30);
            this.btnRunAll.TabIndex = 2;
            this.btnRunAll.Text = "Run Jobs";
            this.btnRunAll.UseVisualStyleBackColor = true;
            this.btnRunAll.Click += new System.EventHandler(this.btnRunAll_Click);
            // 
            // btnRemove
            // 
            this.btnRemove.Location = new System.Drawing.Point(7, 18);
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.Size = new System.Drawing.Size(90, 30);
            this.btnRemove.TabIndex = 0;
            this.btnRemove.Text = "Remove";
            this.btnRemove.UseVisualStyleBackColor = true;
            this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);
            // 
            // frmJobManager
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(624, 361);
            this.Controls.Add(this.tlpMain);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(161)));
            this.MinimumSize = new System.Drawing.Size(640, 400);
            this.Name = "frmJobManager";
            this.Text = "frmJobManager";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmJobManager_FormClosing);
            this.tlpMain.ResumeLayout(false);
            this.grpProgress.ResumeLayout(false);
            this.grpProgress.PerformLayout();
            this.tlpJobs.ResumeLayout(false);
            this.grpJobs.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grdJobs)).EndInit();
            this.grpActions.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private gGroupBox grpJobs;
        private System.Windows.Forms.ProgressBar prgBrCurrent;
        private System.Windows.Forms.ProgressBar prgBrTotal;
        private gGroupBox grpProgress;
        private System.Windows.Forms.Label lblTotalProgressValue;
        private System.Windows.Forms.Label lblCurrentProgressValue;
        private System.Windows.Forms.Label lblTotalProgress;
        private System.Windows.Forms.Label lblCurrentProgress;
        private gGroupBox grpActions;
        private System.Windows.Forms.Button btnRemove;
        private System.Windows.Forms.Button btnRunAll;
        private gTableLayoutPanel tlpMain;
        private gTableLayoutPanel tlpJobs;
        private System.Windows.Forms.Button btnAbortAll;
        private System.Windows.Forms.Button btnAbort;
        private Controls.gDataGridView grdJobs;
    }
}