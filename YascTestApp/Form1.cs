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
using Svg;
using System.IO;
using BitmapToSvgTest;

namespace YascTestApp
{
    public partial class Form1 : Form
    {

        Form prevForm;
        Timer bouncetimer = new Timer() { Interval = 100 };

        public bool IsClosing { get; private set; }

        private int ballX = 0, ballY = 0, incX = 5, incY = 5;
        string svgData = "";
        SvgDocument svgDoc = null;
        SvgOsd osd; 

        public Form1()
        {
            InitializeComponent();

            yascControl1.PreviewStarted += YascControl1_PreviewStarted;
            yascControl1.PreviewStopped += YascControl1_PreviewStopped;
            yascControl1.RecordingStarted += YascControl1_RecordingStarted;
            yascControl1.ErrorStreaming += this.YascControl1_ErrorStreaming;

            bouncetimer.Tick += this.Bouncetimer_Tick;

            osd = new SvgOsd(yascControl1.Width, yascControl1.Height);
            //yascControl1.OverlayObjects = new List<OsdObject>();

            //yascControl1.OverlayObjects.Add(new OsdObject("Test", "Arial", 13) { HorizontalAlignment = GstEnums.TextOverlayHAlign.HALIGN_LEFT });
            //yascControl1.OverlayObjects.Add(new OsdObject("Test222", "Arial", 13) { HorizontalAlignment = GstEnums.TextOverlayHAlign.HALIGN_RIGHT, VerticalAlignment = GstEnums.TextOverlayVAlign.VALIGN_TOP });
        }

        private void Bouncetimer_Tick(object sender, EventArgs e)
        {
            if ((ballX > yascControl1.CamWidth && incX > 0) || (ballX < 0 && incX < 0))
                incX *= -1;

            if ((ballY > yascControl1.CamHeight && incY > 0) || (ballY < 0 && incY < 0))
                incY *= -1;

            ballX += incX;
            ballY += incY;

            //UpdateSvg(); 
            // yascControl1.SvgData = this.svgData;
            osd.UpdateOsd();
            osd.Pitch = (ballX % 100) - 50;
            osd.Roll = (ballY % 180) - 90;
            osd.Heading = ballY % 360;
            osd.Depth = ballX;
            osd.Turns = - ballY; 

            yascControl1.SvgData = osd.SvgData; 
        }

        private void YascControl1_ErrorStreaming(object sender, YascStreamingException e)
        {
            SetStatusText("Error streaming: " + e.Message); 
        }

        private void YascControl1_PreviewStopped(object sender, EventArgs e)
        {
            SetStatusText("Preview stopped.");
            enableControls(true); 
        }

        private void YascControl1_RecordingStarted(object sender, EventArgs e)
        {
            SetStatusText("Recording started."); 
        }

        private void YascControl1_PreviewStarted(object sender, EventArgs e)
        {
            SetStatusText("Preview started");
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

                if (this.InvokeRequired)
                {
                    this.Invoke(new Action<string>(SetStatusText), newStatus);
                    return;
                }

                if (!this.Disposing)
                    lblStatus.Text = newStatus;
            }
            catch (Exception ex)
            {
                sysDbg.WriteLine("Error setting status: " + ex.Message);
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
                Properties.Settings.Default.LastRecordPath = saveDialog.FileName;
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
            bool prev = GstUtilities.DumpIntermediateGraphs;
            GstUtilities.DumpIntermediateGraphs = true;
            yascControl1.DumpGraph("myDump");
            GstUtilities.DumpIntermediateGraphs = prev;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string path = Properties.Settings.Default.LastRecordPath;
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

        private void btnFilesrcBrowse_Click(object sender, EventArgs e)
        {
            var openDialog = new OpenFileDialog();
            openDialog.CheckFileExists = true;
            openDialog.ValidateNames = true;
            openDialog.Title = "Choose file to open...";
            openDialog.Filter = "Video files (*.avi, *.mp4, *.mkv)|*.avi;*.mp4;*.mkv|Audio files (*.mp3, *.wav)|*.mp3;*.wav;*.ogg|All files (*.*)|*.*";
            var res = openDialog.ShowDialog();
            if (res == DialogResult.OK)
            {
                yascControl1.FileSoureName = openDialog.FileName;
                Properties.Settings.Default.LastFileSrcPath = openDialog.FileName;
                Properties.Settings.Default.Save();
            }
        }

        private void rbtnFileSrc_CheckedChanged(object sender, EventArgs e)
        {
            btnFilesrcBrowse.Enabled = rbtnFileSrc.Checked;

            yascControl1.CamType = GstEnums.CamType.FileSrc;

            yascControl1.FileSoureName = Properties.Settings.Default.LastFileSrcPath; 
        }

        private void btnGstLaunch_Click(object sender, EventArgs e)
        {
            var f = new LaunchLineForm();
            f.Show(); 
        }

        private void chkDumpInter_CheckedChanged(object sender, EventArgs e)
        {
            GstUtilities.DumpIntermediateGraphs = chkDumpInter.Checked;
        }

        private void btnBounce_Click(object sender, EventArgs e)
        {
            // toggle bounce timer. 
            bouncetimer.Enabled ^= true;

            if(string.IsNullOrEmpty(this.svgData))
            {
                UpdateSvg(); 
            }
        }

        /// <summary>
        /// Create or update SVG ball location. 
        /// </summary>
        private void UpdateSvg()
        {
            if (svgDoc == null)
            {
                svgDoc = new SvgDocument()
                {
                    Width = yascControl1.CamWidth,
                    Height = yascControl1.CamHeight,
                    ViewBox = new SvgViewBox(0, 0, yascControl1.CamWidth, yascControl1.CamHeight)
                };

                var group = new SvgGroup();

                svgDoc.Children.Add(group);

                group.Children.Add(new SvgCircle()
                {
                    Radius = 10,
                    Fill = new SvgColourServer(Color.Red),
                    Stroke = new SvgColourServer(Color.Black),
                    StrokeWidth = 1,
                    CenterX = this.ballX,
                    CenterY = this.ballY,
                    ID = "bouncyball"
                });
                var txtContent = new SvgContentNode() { Content = "TEST SVG" };

                group.Children.Add(new SvgText("SVG TEXT")
                {
                    Stroke = new SvgColourServer(Color.Black),
                    Fill = new SvgColourServer(Color.White),
                    FontSize = 50,
                    FontFamily = "Segoe UI, Sans",
                    ID = "svgtext",
                    StrokeWidth = 1,
                    X = { 200 },
                    Y = { 100 }
                });
            }

            var ball = svgDoc.GetElementById<SvgCircle>("bouncyball");
            ball.CenterX = this.ballX;
            ball.CenterY = this.ballY; 

            //pbxTest.Image = svgDoc.Draw();

            using (var st = new MemoryStream())
            {
                svgDoc.Write(st);
                svgData = Encoding.UTF8.GetString(st.GetBuffer());
            }

        }

        private void CreateSvgOsd()
        {
            int _width = yascControl1.CamWidth, _height = yascControl1.CamHeight;
            Pen _whitePen = Pens.White; 

            svgDoc = new SvgDocument()
            {
                Width = yascControl1.CamWidth,
                Height = yascControl1.CamHeight,
                ViewBox = new SvgViewBox(0, 0, yascControl1.CamWidth, yascControl1.CamHeight)
            };

            var group = new SvgGroup();
            svgDoc.Children.Add(group); 

            var headbg = new RectangleF(0, 0, _width, _height * .07f);
            // Bottom line

            var line = new SvgLine()
            {
                StartX = headbg.Left + 5,
                StartY = headbg.Bottom - headbg.Height / 5,
                EndX = (SvgUnit)( headbg.Width - 5),
                EndY = (SvgUnit)( headbg.Bottom - headbg.Height * .2 ),
            };

            using (var st = new MemoryStream())
            {
                svgDoc.Write(st);
                svgData = Encoding.UTF8.GetString(st.GetBuffer());
            }
        }
    }
}
