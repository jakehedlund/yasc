namespace YascTestApp
{
    partial class Form1
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
            this.btnStart = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.rbtnLocal = new System.Windows.Forms.RadioButton();
            this.gbxSrc = new System.Windows.Forms.GroupBox();
            this.cmbUri = new System.Windows.Forms.ComboBox();
            this.nudLocalIdx = new System.Windows.Forms.NumericUpDown();
            this.rbtnRtsp = new System.Windows.Forms.RadioButton();
            this.gbxControl = new System.Windows.Forms.GroupBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.btnRecord = new System.Windows.Forms.Button();
            this.yascControl1 = new YetAnotherStreamingContol.YascControl();
            this.rbtnTest = new System.Windows.Forms.RadioButton();
            this.nudTestSrc = new System.Windows.Forms.NumericUpDown();
            this.gbxSrc.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudLocalIdx)).BeginInit();
            this.gbxControl.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudTestSrc)).BeginInit();
            this.SuspendLayout();
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(6, 17);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(75, 23);
            this.btnStart.TabIndex = 1;
            this.btnStart.Text = "Start";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
            // 
            // btnStop
            // 
            this.btnStop.Location = new System.Drawing.Point(6, 46);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(75, 23);
            this.btnStop.TabIndex = 1;
            this.btnStop.Text = "Stop";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(3, 76);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(37, 13);
            this.lblStatus.TabIndex = 2;
            this.lblStatus.Text = "Status";
            // 
            // rbtnLocal
            // 
            this.rbtnLocal.AutoSize = true;
            this.rbtnLocal.Checked = true;
            this.rbtnLocal.Location = new System.Drawing.Point(6, 19);
            this.rbtnLocal.Name = "rbtnLocal";
            this.rbtnLocal.Size = new System.Drawing.Size(57, 17);
            this.rbtnLocal.TabIndex = 4;
            this.rbtnLocal.Text = "Local: ";
            this.rbtnLocal.UseVisualStyleBackColor = true;
            this.rbtnLocal.CheckedChanged += new System.EventHandler(this.rbtnLocal_CheckedChanged);
            // 
            // gbxSrc
            // 
            this.gbxSrc.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.gbxSrc.Controls.Add(this.cmbUri);
            this.gbxSrc.Controls.Add(this.nudTestSrc);
            this.gbxSrc.Controls.Add(this.nudLocalIdx);
            this.gbxSrc.Controls.Add(this.rbtnRtsp);
            this.gbxSrc.Controls.Add(this.rbtnTest);
            this.gbxSrc.Controls.Add(this.rbtnLocal);
            this.gbxSrc.Location = new System.Drawing.Point(12, 412);
            this.gbxSrc.Name = "gbxSrc";
            this.gbxSrc.Size = new System.Drawing.Size(372, 94);
            this.gbxSrc.TabIndex = 5;
            this.gbxSrc.TabStop = false;
            this.gbxSrc.Text = "Source";
            // 
            // cmbUri
            // 
            this.cmbUri.Enabled = false;
            this.cmbUri.FormattingEnabled = true;
            this.cmbUri.Location = new System.Drawing.Point(71, 44);
            this.cmbUri.Name = "cmbUri";
            this.cmbUri.Size = new System.Drawing.Size(292, 21);
            this.cmbUri.TabIndex = 7;
            this.cmbUri.Text = "rtsp://192.168.0.250:554/cam0_0";
            // 
            // nudLocalIdx
            // 
            this.nudLocalIdx.Location = new System.Drawing.Point(72, 19);
            this.nudLocalIdx.Maximum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nudLocalIdx.Name = "nudLocalIdx";
            this.nudLocalIdx.Size = new System.Drawing.Size(42, 20);
            this.nudLocalIdx.TabIndex = 6;
            this.nudLocalIdx.ValueChanged += new System.EventHandler(this.nudIdx_ValueChanged);
            // 
            // rbtnRtsp
            // 
            this.rbtnRtsp.AutoSize = true;
            this.rbtnRtsp.Location = new System.Drawing.Point(6, 44);
            this.rbtnRtsp.Name = "rbtnRtsp";
            this.rbtnRtsp.Size = new System.Drawing.Size(60, 17);
            this.rbtnRtsp.TabIndex = 4;
            this.rbtnRtsp.Text = "RTSP: ";
            this.rbtnRtsp.UseVisualStyleBackColor = true;
            this.rbtnRtsp.CheckedChanged += new System.EventHandler(this.rbtnRtsp_CheckedChanged);
            // 
            // gbxControl
            // 
            this.gbxControl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.gbxControl.Controls.Add(this.btnBrowse);
            this.gbxControl.Controls.Add(this.btnRecord);
            this.gbxControl.Controls.Add(this.btnStop);
            this.gbxControl.Controls.Add(this.btnStart);
            this.gbxControl.Controls.Add(this.lblStatus);
            this.gbxControl.Location = new System.Drawing.Point(710, 12);
            this.gbxControl.Name = "gbxControl";
            this.gbxControl.Size = new System.Drawing.Size(96, 167);
            this.gbxControl.TabIndex = 6;
            this.gbxControl.TabStop = false;
            this.gbxControl.Text = "Controls";
            // 
            // btnBrowse
            // 
            this.btnBrowse.Location = new System.Drawing.Point(6, 127);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(75, 23);
            this.btnBrowse.TabIndex = 4;
            this.btnBrowse.Text = "Browse...";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // btnRecord
            // 
            this.btnRecord.Location = new System.Drawing.Point(7, 98);
            this.btnRecord.Name = "btnRecord";
            this.btnRecord.Size = new System.Drawing.Size(75, 23);
            this.btnRecord.TabIndex = 3;
            this.btnRecord.Text = "Record";
            this.btnRecord.UseVisualStyleBackColor = true;
            this.btnRecord.Click += new System.EventHandler(this.btnRecord_Click);
            // 
            // yascControl1
            // 
            this.yascControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.yascControl1.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.yascControl1.CamType = YetAnotherStreamingContol.GstEnums.CamType.Local;
            this.yascControl1.CapFilename = null;
            this.yascControl1.CaptureFrameRate = 0;
            this.yascControl1.Connected = false;
            this.yascControl1.ConnectionUri = null;
            this.yascControl1.DeviceIndex = 0;
            this.yascControl1.IsRecording = false;
            this.yascControl1.Location = new System.Drawing.Point(12, 12);
            this.yascControl1.Name = "yascControl1";
            this.yascControl1.OverlayObjects = null;
            this.yascControl1.Preview = false;
            this.yascControl1.Size = new System.Drawing.Size(683, 394);
            this.yascControl1.TabIndex = 3;
            this.yascControl1.DoubleClick += new System.EventHandler(this.yascControl1_DoubleClick);
            // 
            // rbtnTest
            // 
            this.rbtnTest.AutoSize = true;
            this.rbtnTest.Location = new System.Drawing.Point(6, 68);
            this.rbtnTest.Name = "rbtnTest";
            this.rbtnTest.Size = new System.Drawing.Size(66, 17);
            this.rbtnTest.TabIndex = 4;
            this.rbtnTest.Text = "Testsrc: ";
            this.rbtnTest.UseVisualStyleBackColor = true;
            this.rbtnTest.CheckedChanged += new System.EventHandler(this.rbtnTest_CheckedChanged);
            // 
            // nudTestSrc
            // 
            this.nudTestSrc.Enabled = false;
            this.nudTestSrc.Location = new System.Drawing.Point(72, 68);
            this.nudTestSrc.Maximum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nudTestSrc.Name = "nudTestSrc";
            this.nudTestSrc.Size = new System.Drawing.Size(42, 20);
            this.nudTestSrc.TabIndex = 6;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(818, 536);
            this.Controls.Add(this.gbxControl);
            this.Controls.Add(this.gbxSrc);
            this.Controls.Add(this.yascControl1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.gbxSrc.ResumeLayout(false);
            this.gbxSrc.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudLocalIdx)).EndInit();
            this.gbxControl.ResumeLayout(false);
            this.gbxControl.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudTestSrc)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion


        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Label lblStatus;
        private YetAnotherStreamingContol.YascControl yascControl1;
        private System.Windows.Forms.RadioButton rbtnLocal;
        private System.Windows.Forms.GroupBox gbxSrc;
        private System.Windows.Forms.NumericUpDown nudLocalIdx;
        private System.Windows.Forms.RadioButton rbtnRtsp;
        private System.Windows.Forms.ComboBox cmbUri;
        private System.Windows.Forms.GroupBox gbxControl;
        private System.Windows.Forms.Button btnRecord;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.NumericUpDown nudTestSrc;
        private System.Windows.Forms.RadioButton rbtnTest;
    }
}

