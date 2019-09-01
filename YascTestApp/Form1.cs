using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using sysDbg = System.Diagnostics.Debug;
using Yasc;

namespace YascTestApp
{
    public partial class Form1 : Form
    {

        Form prevForm;

        public bool IsClosing { get; private set; }

        public Form1()
        {
            InitializeComponent();

            yascControl1.PreviewStarted += YascControl1_PreviewStarted;
            yascControl1.PreviewStopped += YascControl1_PreviewStopped;
            yascControl1.RecordingStarted += YascControl1_RecordingStarted;

            //yascControl1.OverlayObjects = new List<OsdObject>();

            //yascControl1.OverlayObjects.Add(new OsdObject("Test", "Arial", 13) { HorizontalAlignment = GstEnums.TextOverlayHAlign.HALIGN_LEFT });
            //yascControl1.OverlayObjects.Add(new OsdObject("Test222", "Arial", 13) { HorizontalAlignment = GstEnums.TextOverlayHAlign.HALIGN_RIGHT, VerticalAlignment = GstEnums.TextOverlayVAlign.VALIGN_TOP });
        }

        private void YascControl1_PreviewStopped(object sender, EventArgs e)
        {
            SetStatusText("preview stopped.");
            enableControls(true); 
        }

        private void YascControl1_RecordingStarted(object sender, EventArgs e)
        {
            SetStatusText("Recording started."); 
        }

        private void YascControl1_PreviewStarted(object sender, EventArgs e)
        {
            SetStatusText("Preview Started");
            enableControls(false);
            
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            yascControl1.StartPreview();
        }

        private void enableControls(bool en)
        {
            if (IsClosing || this.Disposing || this.IsDisposed)
                return;

            if(this.InvokeRequired)
            {
                this.Invoke(new Action<bool>(enableControls), en);
                return;
            }

            rbtnLocal.Enabled = en;
            rbtnRtsp.Enabled = en;
            rbtnTest.Enabled = en;

            nudLocalIdx.Enabled = en;
            nudTestSrc.Enabled = en;
        }

        /// <summary>
        /// Update status label text safely. 
        /// </summary>
        /// <param name="newStatus"></param>
        private void SetStatusText(string newStatus)
        {
            try
            {
                if (this.IsDisposed) return;
                if (IsClosing) return;

                if (!this.Disposing)
                    lblStatus.Text = newStatus;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error setting status: " + ex.Message);
            }
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
            //else
            //{
            //    yascControl1.CamType = GstEnums.CamType.Local; 
            //    yascControl1.DeviceIndex = (int)nudLocalIdx.Value;
            //}
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            var saveDialog = new SaveFileDialog();
            saveDialog.DefaultExt = ".mp4";
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
                Properties.Settings.Default.LastPath = saveDialog.FileName;
                Properties.Settings.Default.Save(); 
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
            if(rbtnLocal.Checked)
                yascControl1.CamType = GstEnums.CamType.Local;
        }

        private void rbtnTest_CheckedChanged(object sender, EventArgs e)
        {
            nudTestSrc.Enabled = rbtnTest.Checked;
            if(rbtnTest.Checked)
                yascControl1.CamType = GstEnums.CamType.TestSrc;
        }

        private void btnDump_Click(object sender, EventArgs e)
        {
            yascControl1.DumpGraph("myDump"); 
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string path = Properties.Settings.Default.LastPath;
            if (!string.IsNullOrEmpty(path))
                yascControl1.CapFilename = path; 

            foreach(var osd in yascControl1.OverlayObjects)
            {
                flpOsd.Controls.Add(new OsdTextSettings(osd));
            }
        }

        private void cmbUri_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.IsClosing = true;
            yascControl1.StopPreview();
        }

        private void yascControl1_SnapshotReady(object sender, Image e)
        {
            pbxSnapshot.Image = e;
        }

        private void btnSnapshot_Click(object sender, EventArgs e)
        {
            yascControl1.TakeSnapshot(); 
        }

        private void cmbUri_Leave(object sender, EventArgs e)
        {
            if (rbtnRtsp.Checked && yascControl1 != null)
                yascControl1.ConnectionUri = cmbUri.Text;
        }

        private void pbxSnapshot_Click(object sender, EventArgs e)
        {
            PictureBox prevPanel = new PictureBox()
            {
                Image = pbxSnapshot.Image,
                SizeMode = PictureBoxSizeMode.Zoom,
                Dock = DockStyle.Fill
            };

            if (prevForm == null || prevForm.IsDisposed)
            {
                prevForm = new Form() { Width = 640, Height = 360 };
                prevForm.Controls.Add(prevPanel); 
            }
            else
            {
                prevForm.Controls.Clear();
                prevForm.Controls.Add(prevPanel); 
            }
            prevForm.Show(); 


        }

        private void nudTestSrc_ValueChanged(object sender, EventArgs e)
        {
            yascControl1.DeviceIndex = (int)nudTestSrc.Value; 
        }

        private void btnAddOsd_Click(object sender, EventArgs e)
        {
            if(yascControl1 != null)
            {
                var c = new OsdTextSettings("test"); 
                if(yascControl1.OverlayObjects!=null)
                {
                    yascControl1.OverlayObjects.Add(c.OsdObject);
                    flpOsd.Controls.Add(c); 
                }
                else
                {
                    c.Dispose(); 
                }
            }
        }

        private void btnRemoveOsd_Click(object sender, EventArgs e)
        {
            if (flpOsd.Controls.Count > 0)
            {
                try
                {
                    var c = (OsdTextSettings)flpOsd.Controls[flpOsd.Controls.Count - 1];
                    yascControl1.OverlayObjects.Remove(c.OsdObject);
                    flpOsd.Controls.Remove(c);
                    c.Dispose(); 
                }
                catch(Exception ex)
                {
                    sysDbg.WriteLine("Error removing OSD object: " + ex.Message);
                }
            }

        }
    }
}
