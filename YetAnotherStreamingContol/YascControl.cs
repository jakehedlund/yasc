﻿#define YASC_DEBUG

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using sysDbg = System.Diagnostics.Debug;
using static YetAnotherStreamingContol.GstEnums;
using System.Drawing;

namespace YetAnotherStreamingContol
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
        public CamType CamType { get; set; }

        /// <summary>
        ///  Local device index (set to -2 for IP cameras... maybe...). 
        /// </summary>
        public int DeviceIndex { get; set; }

        /// <summary>
        /// Full path including filename to save files. 
        /// </summary>
        public string CapFilename { get; set; }

        /// <summary>
        /// Not used. 
        /// </summary>
        public int CaptureFrameRate { get; set; }

        /// <summary>
        /// Set to true to connect; false to disconnect.
        /// </summary>
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

        public bool IsRecording { get; set; }

        public List<OsdObject> OverlayObjects {get; set; }
        public string ConnectionUri { get; set; }
        #endregion

        #region Events
        public event EventHandler PreviewStarted;
        public event EventHandler PreviewStopped;
        public event EventHandler RecordingStarted;
        public event EventHandler RecordingEnded;
        public event EventHandler ErrorStreaming;
        #endregion

        #region Methods
        public YascControl()
        {
            InitializeComponent();
            gstCam = new GstCam();

        }

        private void GstCam_PreviewStopped(object sender, EventArgs e)
        {
            PreviewStopped?.Invoke(sender, e); 
        }

        private void GstCam_PreviewStarted(object sender, EventArgs e)
        {
            PreviewStarted?.Invoke(sender, e);
        }

        /// <summary>
        /// Create an RTSP streaming control. 
        /// </summary>
        /// <param name="connectionUri"></param>
        public YascControl(Uri connectionUri) : this()
        {
            gstCam = new GstCam();
            gstCam.ConnectionUri = connectionUri.ToString();
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

                try
                {
                    gstCam.DeviceIndex = this.DeviceIndex;
                    gstCam.ConnectionUri = "";
                    gstCam.StartPreview();
                }
                catch(Exception ex)
                {
                    sysDbg.WriteLine(ex.Message); 
                }
            }
        }

        public void StopPreview()
        {
            gstCam.StopPreview(); 
        }

        public void StartRecord()
        {
            gstCam.StartRecord();
        }

        public void StopRecord()
        {
            gstCam.StopRecord();
        }

        public void UpdateOsd(string formattedText)
        {
            
        }

        public void UpdateOsd(OsdObject osdobj)
        {

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
            this.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
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
                //_log.Info("Showing fullscreen");

                Screen screen = Screen.PrimaryScreen;
                //Screen screen = Screen.FromControl(this);
                // Setup host form to be full screen
                host = new Form
                {
                    FormBorderStyle = FormBorderStyle.None,
                    //Width = Screen.PrimaryScreen.Bounds.Width,
                    //Height = Screen.PrimaryScreen.Bounds.Height,
                    Width = screen.Bounds.Width,
                    Height = screen.Bounds.Height,
                    BackColor = Color.Black,
                    ShowInTaskbar = false,
                    StartPosition = FormStartPosition.Manual
                };
                //host.WindowState = FormWindowState.Maximized;

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
                //ctl.Location = Point.Empty;            
                //ctl.Dock = DockStyle.Fill;   
                ctl.Dock = DockStyle.None;
                ctl.Width = host.Width;
                ctl.Height = host.Width / 16 * 9;
                ctl.Location = new Point(0, (host.Height - ctl.Height) / 2);
                //ctl.Location = new Point(screen.Bounds.Left, screen.Bounds.Top + (host.Height - ctl.Height) / 2);

                // Setup event handler to restore control back to form
                host.FormClosing += delegate
                {
                    ctl.Parent = parent;
                    ctl.Dock = dock;
                    ctl.Width = width;
                    ctl.Height = height;
                    ctl.Location = loc;
                    //form.Show();
                    gstCam.IsFullscreen = false;
                    ctl.DoubleClick -= pnlPreview_DoubleClick;
                    ctl.DoubleClick += pnlPreview_DoubleClick;

                    //_log.Info("Leaving fullscreen");
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
                //form.Hide(); // Disabled because OSD won't get update if form is not visible.
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
        
        #endregion
        
        private void pnlPreview_DoubleClick(object sender, EventArgs e)
        {
            Console.WriteLine("test double click.");

            if (gstCam.EnableFullscreenDblClick)
            {
                Console.WriteLine("Toggling fullscreen.");
                this.ToggleFullscreen();
            }
        }
    }
}