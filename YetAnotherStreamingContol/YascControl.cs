#define YASC_DEBUG

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using sysDbg = System.Diagnostics.Debug;
using static Yasc.GstEnums;
using System.Drawing;
using System.IO;

namespace Yasc
{
    public class YascControl : UserControl
    {
        #region Fields
        private Label lblConnStatus;
        private Panel pnlPreview;
        private bool connected = false;
        private Form host = null; // Form for showing fullscreen preview. 

        [Browsable(false)]
        private GstCam gstCam;
        #endregion

        #region Properties

        /// <summary>
        /// RTSP or Local camera. 
        /// </summary>
        public CamType CamType
        {
            get
            {
                return gstCam == null ? CamType.Local : gstCam.CamType;
            }
            set
            {
                if (gstCam != null)
                    gstCam.CamType = value;
            }
        }

        /// <summary>
        ///  Local device index. Only has an effect in CamType.Local or CamType.TestSrc modes. 
        /// </summary>
        public int DeviceIndex { get { return gstCam == null ? 0 : gstCam.DeviceIndex; } set { if (gstCam != null) gstCam.DeviceIndex = value; } }

        /// <summary>
        /// Full path including filename to save files. 
        /// </summary>
        [Description("Location to which the recorded file is saved. ")]
        [DefaultValue("")]
        public string CapFilename
        {
            get { return gstCam == null ? _fname : gstCam.RecFilename; }
            set
            {
                _fname = value;
                if (gstCam != null)
                    gstCam.RecFilename = value;
            }
        }
        private string _fname = "";

        [Description("Open a video file for playback. ")]
        [DefaultValue("")]
        public string FileSoureName
        {
            get { return gstCam == null ? _fsourcename : gstCam.FileSourceLocation; }
            set
            {
                _fsourcename = value;
                if (gstCam != null)
                    gstCam.FileSourceLocation = value;
            }
        }
        private string _fsourcename = ""; 
        /// <summary>
        /// Not used. 
        /// </summary>
        public int CaptureFrameRate { get; set; }

        /// <summary>
        /// Set to true to connect; false to disconnect.
        /// </summary>
        [Browsable(false)]
        public bool Connected
        {
            get
            {
                return connected;
            }
            set
            {
                //this.connected = value;
                //if (value)
                //    this.connected = gstCam.Connect();
                //else
                //{
                //    this.connected = value;
                //    gstCam.Disconnect();
                //}
            }
        }

        [DefaultValue(false)]
        [Description("Keep last frame on preview panel or show background color on pause.")]
        public bool ShowLastFrameOnStop { get; set; } = false;

        [Browsable(false)]
        public bool IsRecording { get { return gstCam?.CameraState == CamState.Recording; } }

        /// <summary>
        /// Set to true to start preview (will try to connect automatically). Set to false to stop preview (will stay connected). 
        /// </summary>
        [Browsable(false)]
        public bool Preview
        {
            get {
                return gstCam.CameraState == GstEnums.CamState.Previewing;
            }
            set
            {
                //if (value) this.StartPreview();
                //else this.StopPreview(); 
                
            }
        }


        /// <summary>
        /// Allow double-clicks to make the window fullscreen. 
        /// </summary>
        [DefaultValue(true)]
        public bool EnableFullscreenDblClick { get; set; } = true;

        /// <summary>
        /// List of OsdObject. Must be populated before entering the playing state. 
        /// The Text property can be modified at runtime/playtime as often as needed. 
        /// </summary>
        [Browsable(true)]
        [Description("Add up to four text overlays. Or more at your own risk. ")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        public List<OsdObject> OverlayObjects
        {
            get { return gstCam?.OverlayList; }
            set
            {
                if (gstCam != null) gstCam.OverlayList = value;
            }
        }

        /// <summary>
        /// The RTSP or MJPG connection URI. 
        /// </summary>
        [Description("The RTSP URI from where to stream.")]
        [DefaultValue("rtsp://192.168.0.250:554/cam0_0")]
        [Browsable(true)]
        public string ConnectionUri
        {
            get
            {
                return gstCam == null ? "" : gstCam.ConnectionUri; 
            }
            set {
                if (gstCam != null && !string.IsNullOrEmpty(value))
                    gstCam.ConnectionUri = value;
            }
        }
        #endregion

        #region Events
        public event EventHandler PreviewStarted;
        public event EventHandler PreviewStopped;
        public event EventHandler RecordingStarted;
        public event EventHandler RecordingEnded;
        public event EventHandler<YascStreamingException> ErrorStreaming;
        public event EventHandler<Image> SnapshotReady; 
        #endregion

        #region Methods
        public YascControl() : this(new GstCam())
        {

        }

        /// <summary>
        /// Create an RTSP streaming control. 
        /// </summary>
        /// <param name="connectionUri"></param>
        public YascControl(Uri connectionUri) : this()
        {
            gstCam.ConnectionUri = connectionUri.ToString();
        }

        /// <summary>
        /// Constructor for custom GstCam implementation. 
        /// </summary>
        /// <param name="gst"></param>
        public YascControl(GstCam gst)
        {
            InitializeComponent();
            gstCam = gst;
            OverlayObjects = new List<OsdObject>(); 
        }

        private void GstCam_PreviewStopped(object sender, EventArgs e)
        {
            PreviewStopped?.Invoke(sender, e);

            if(!ShowLastFrameOnStop)
                pnlPreview.Invalidate(); 
        }

        private void GstCam_PreviewStarted(object sender, EventArgs e)
        {
            PreviewStarted?.Invoke(sender, e);
        }


        public object GetUnderlyingObject()
        {
            //return this.gstCam;
            return null;
        }

        public void StartPreview()
        {
            if (gstCam != null) 
            {
                gstCam.HdlPreviewPanel = (ulong)this.pnlPreview.Handle;

                gstCam.PreviewStarted -= GstCam_PreviewStarted;
                gstCam.PreviewStopped -= GstCam_PreviewStopped;
                gstCam.PreviewStarted += GstCam_PreviewStarted;
                gstCam.PreviewStopped += GstCam_PreviewStopped;
                gstCam.ErrorStreaming -= GstCam_ErrorStreaming;
                gstCam.ErrorStreaming += GstCam_ErrorStreaming;
                gstCam.SnapshotReady -= GstCam_SnapshotReady;
                gstCam.SnapshotReady += GstCam_SnapshotReady;

                try
                {
                    gstCam.DeviceIndex = this.DeviceIndex;
                    gstCam.ConnectionUri = this.ConnectionUri;
                    gstCam.StartPreview();
                }
                catch(Exception ex)
                {
                    sysDbg.WriteLine(ex.Message); 
                }
            }
        }

        public void TakeSnapshot()
        {
            if (gstCam != null) gstCam.TakeSnapshot(); 
        }

        private void GstCam_SnapshotReady(object sender, Image e)
        {
            this.SnapshotReady?.Invoke(this, e); 
        }

        private void GstCam_ErrorStreaming(object sender, YascStreamingException e)
        {
            this.ErrorStreaming?.Invoke(this, e);
        }

        public void StopPreview()
        {
            gstCam.StopPreview(); 
        }

        public void StartRecord()
        {
            if(gstCam.StartRecord())
            {
                RecordingStarted?.Invoke(this, new EventArgs()); 
            }
        }

        public void StopRecord()
        {
            gstCam.StopRecord();
            RecordingEnded?.Invoke(this, new EventArgs());
        }

        public void UpdateOsd(string formattedText)
        {
            UpdateOsd(formattedText, 0);
        }

        public void UpdateOsd(OsdObject osdobj)
        {

        }

        public void UpdateOsd(string formattedText, int idx)
        {
            if(OverlayObjects != null)
            {
                if (idx > 0 && idx < OverlayObjects.Count)
                {
                    OverlayObjects[idx].Text = formattedText;
                }
                else
                    throw new YascElementNullException("Invalid index given."); 
            }
        }
        #endregion

        #region PrivateMethods

        #endregion

        #region InitComponent
        private void InitializeComponent()
        {
            this.pnlPreview = new System.Windows.Forms.Panel();
            this.lblConnStatus = new System.Windows.Forms.Label();
            this.pnlPreview.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlPreview
            // 
            this.pnlPreview.Controls.Add(this.lblConnStatus);
            this.pnlPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlPreview.Location = new System.Drawing.Point(0, 0);
            this.pnlPreview.Name = "pnlPreview";
            this.pnlPreview.Size = new System.Drawing.Size(570, 341);
            this.pnlPreview.TabIndex = 0;
            this.pnlPreview.DoubleClick += new System.EventHandler(this.pnlPreview_DoubleClick);
            // 
            // lblConnStatus
            // 
            this.lblConnStatus.AutoSize = true;
            this.lblConnStatus.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.lblConnStatus.Location = new System.Drawing.Point(3, 0);
            this.lblConnStatus.Name = "lblConnStatus";
            this.lblConnStatus.Size = new System.Drawing.Size(0, 13);
            this.lblConnStatus.TabIndex = 0;
            // 
            // YascControl
            // 
            this.BackColor = System.Drawing.SystemColors.Desktop;
            this.Controls.Add(this.pnlPreview);
            this.Name = "YascControl";
            this.Size = new System.Drawing.Size(570, 341);
            this.pnlPreview.ResumeLayout(false);
            this.pnlPreview.PerformLayout();
            this.ResumeLayout(false);

        }
        #endregion
        
        public void ToggleFullscreen()
        {
            ShowFullScreen(pnlPreview); 
        }

        #region helperFunctions
        private void ShowFullScreen(Control ctl)
        {
            if (gstCam.CameraState != CamState.Previewing) return;

            if (ctl.InvokeRequired)
            {
                ctl.BeginInvoke(new Action<Control>(ShowFullScreen), ctl);
                return;
            }

            if (!gstCam.IsFullscreen)
            {
                Screen screen = Screen.FromControl(this);

                // Setup host form to be full screen
                host = new Form
                {
                    FormBorderStyle = FormBorderStyle.None,
                    Width = screen.Bounds.Width,
                    Height = screen.Bounds.Height,
                    BackColor = Color.Black,
                    ShowInTaskbar = false,
                    StartPosition = FormStartPosition.Manual
                };

                // Save properties of control
                Point loc = ctl.Location;
                int width = ctl.Width;
                int height = ctl.Height;
                DockStyle dock = ctl.Dock;
                Control parent = ctl.Parent;
                Control form = parent;

                while (!(form is Form)) form = form.Parent;
                // Move control to host
                ctl.Parent = host;
                ctl.Dock = DockStyle.None;
                ctl.Width = host.Width;
                ctl.Height = (int)(host.Width * gstCam.CamHeight / (double)gstCam.CamWidth);
                //ctl.Width = host.Width;
                //ctl.Height = host.Width / 16 * 9;
                ctl.Location = new Point(0, (host.Height - ctl.Height) / 2);

                // Setup event handler to restore control back to form
                host.FormClosing += delegate
                {
                    ctl.Parent = parent;
                    ctl.Dock = dock;
                    ctl.Width = width;
                    ctl.Height = height;
                    ctl.Location = loc;
                    gstCam.IsFullscreen = false;
                    ctl.DoubleClick -= pnlPreview_DoubleClick;
                    ctl.DoubleClick += pnlPreview_DoubleClick;
                    
                };

                // Exit full screen with escape key
                host.KeyPreview = true;
                host.KeyDown += (KeyEventHandler)((s, e) =>
                {
                    if (e.KeyCode == Keys.Escape)
                    {
                        host.Close();
                    }
                });

                ctl.DoubleClick += (s, e) =>
                {
                    host.Close();
                };
                
                // Go full screen
                host.Show();
                host.TopMost = true;
                ctl.MouseDoubleClick -= pnlPreview_DoubleClick;

                gstCam.IsFullscreen = true;
            }
            else
            {
                host.Close();
            }
        }
        
        /// <summary>
        /// Generate a .dot graph file representative of the current pipeline, viewable in a program like GVEdit. 
        /// </summary>
        /// <param name="fname">File name.</param>
        /// <param name="path">File path.</param>
        public void DumpGraph(string fname, string path = @"C:\gstreamer\dotfiles")
        {
            gstCam?.DumpPipeline(Path.Combine(path, fname));
        }
        #endregion
        
        private void pnlPreview_DoubleClick(object sender, EventArgs e)
        {
            if (this.EnableFullscreenDblClick)
            {
                sysDbg.WriteLine("Toggling fullscreen."); 
                this.ToggleFullscreen();
            }
        }
    }
}
