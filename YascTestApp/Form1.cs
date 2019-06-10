using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using YetAnotherStreamingContol;

namespace YascTestApp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            yascControl1.PreviewStarted += YascControl1_PreviewStarted;
            yascControl1.PreviewStopped += YascControl1_PreviewStopped;
            yascControl1.RecordingStarted += YascControl1_RecordingStarted;
        }

        private void YascControl1_PreviewStopped(object sender, EventArgs e)
        {
            SetStatusText("preview stopped."); 
        }

        private void YascControl1_RecordingStarted(object sender, EventArgs e)
        {
            SetStatusText("Recording started."); 
        }

        private void YascControl1_PreviewStarted(object sender, EventArgs e)
        {
            SetStatusText("Preview Started"); 
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            yascControl1.StartPreview();
        }

        /// <summary>
        /// Update status label text. 
        /// </summary>
        /// <param name="newStatus"></param>
        private void SetStatusText(string newStatus)
        {
            if (lblStatus.InvokeRequired)
            {
                this.Invoke(new Action<string>(SetStatusText), newStatus);
            }
            else
                lblStatus.Text = newStatus; 
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            yascControl1.StopPreview();
        }

        private void yascControl1_DoubleClick(object sender, EventArgs e)
        {
            yascControl1.ToggleFullscreen(); 
        }

        private void nudIdx_ValueChanged(object sender, EventArgs e)
        {
            yascControl1.DeviceIndex = (int)((NumericUpDown)sender).Value; 
        }

        private void rbtnRtsp_CheckedChanged(object sender, EventArgs e)
        {
            cmbUri.Enabled = rbtnRtsp.Checked;
            if (rbtnRtsp.Checked)
            {
                yascControl1.ConnectionUri = cmbUri.Text;
                yascControl1.CamType = GstEnums.CamType.Rtsp;
            }
            else
            {
                yascControl1.CamType = GstEnums.CamType.Local; 
                yascControl1.DeviceIndex = (int)nudLocalIdx.Value;
            }
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            var saveDialog = new SaveFileDialog();
            saveDialog.DefaultExt = ".avi";
            saveDialog.AddExtension = true;
            saveDialog.CheckFileExists = false;
            saveDialog.OverwritePrompt = true;
            saveDialog.ValidateNames = true;
            saveDialog.Title = "Choose path to record";
            saveDialog.Filter = "Video files (*.avi, *.mp4)|*.avi;*.mp4|All files (*.*)|*.*"; 
            var res = saveDialog.ShowDialog(); 
            if(res == DialogResult.OK)
            {
                yascControl1.CapFilename = saveDialog.FileName;
            }
        }

        private void btnRecord_Click(object sender, EventArgs e)
        {
            if(yascControl1.IsRecording)
            {
                yascControl1.StopRecord(); 
            }
            else
            {
                if (string.IsNullOrEmpty(yascControl1.CapFilename))
                    btnBrowse_Click(null, null);

                yascControl1.StartRecord();
            }
        }

        private void rbtnLocal_CheckedChanged(object sender, EventArgs e)
        {
            nudLocalIdx.Enabled = rbtnLocal.Checked;
        }

        private void rbtnTest_CheckedChanged(object sender, EventArgs e)
        {
            nudTestSrc.Enabled = rbtnTest.Checked;
        }
    }
}
