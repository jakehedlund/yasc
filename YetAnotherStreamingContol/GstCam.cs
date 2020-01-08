#define YASC_DEBUG

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using sysThread = System.Threading;
using sysDbg = System.Diagnostics.Debug;
using System.Threading.Tasks;
using static Yasc.GstEnums;
using Gst;
using System.Drawing;
using System.IO;

/*
 * Dependencies: GstSharp (gstreamer-sharp, glib-sharp, gio-sharp).  
 * Basic camera object containing the Gst pipeline. One pipeline per GstCam. YascControl can have multiple GstCams for e.g. a preview stream and record stream.  
 * 
 */
namespace Yasc
{
    /// <summary>
    /// A GstCam is the base class for a streaming camera. It can have any type of streaming source (USB, RTSP, MJPG, etc.) and any 
    /// type of recording branch. The rough outline of the pipeline is as follows: 
    /// 
    ///                                                   /--> queueDisp --> PreviewSink (autovideosink)
    /// SourceBIN --> decodeBin --> Overlay --> Tee _____/
    ///                                                  \ 
    ///                                                   \ [--> RecordBIN]
    ///                                     
    /// Easier said than done.
    /// </summary>
    public class GstCam : IDisposable
    {
        protected Pipeline pipeline;
        private sysThread.Thread glibThread;
        static GLib.MainLoop glibLoop;
        Bin binSource,       // Either an RTSP or local (USB cam) or test source
            binRecord,       // Bin to hold all the recording elements (to add/remove at once). 
            binDecode;       // Autoplugger bin to create decode path. 

        Pad padSrcBinSource, // Pad from source bin
            padTeeRec,       // Pad from record branch
            padTeeDisp,
            padRecBinSink,
            padOverlay0; 

        // Source elements (RTSP). Use decodebin instead. 
        //Element mRtspSrc, mRtpDepay, mQueueDec, mDecode;

        private Element srcElt;

        // Display elements
        private Element mQueueDisp, mDispSink, mConvert; 

        // "Accessories"
        private Element mOverlayClock, mCaps, mTee;
        private List<OsdObject> _osdObjects;

        // Recording elements
        private Element mQueueRec, mEncx264, muxMp4, mFileSink, mSplitMuxSink;

        private CamState _camState = CamState.Disconnected;

        public ulong HdlPreviewPanel { get; set; }

        public bool IsRecording { get { return this.CameraState == CamState.Recording; } }
        public bool IsPreviewing { get { return this.CameraState == CamState.Recording || this.CameraState == CamState.Previewing; } }
        public bool IsConnected { get { return this.CameraState >= CamState.Connected; } }

        /// <summary>
        /// RTSP connection URI. Or in the case of streaming a file, the file path. 
        /// </summary>
        public string ConnectionUri {
            get => _connectionUri;
            set => _connectionUri = value;
        }
        private string _connectionUri = ""; 

        /// <summary>
        /// File name (including path) to which record. 
        /// </summary>
        public string RecFilename
        {
            get {
                if (mSplitMuxSink != null)
                {
                    if (!string.IsNullOrEmpty(mSplitMuxSink["location"] as string))
                        return mSplitMuxSink["location"] as string;
                    else
                        return ""; 
                }
                else
                    return _recFname;
            }
            set
            {
                _recFname = value;
                if (mSplitMuxSink != null)
                {
                    mSplitMuxSink["location"] = value;
                }
            }

        }
        private string _recFname = "";
        /// <summary>
        /// For local cameras only, the index of the device. 
        /// </summary>
        public int DeviceIndex { get; set; }

        /// <summary>
        /// Split video using the SplitMuxSink. Not always precise. 
        /// </summary>
        public int VideoSplitTimeS { get; set; } = 1800;

        /// <summary>
        /// Split video using the SplitMuxSink. Not always precise. 
        /// </summary>
        public int VideoSplitSizeMb { get; set; } = 500;

        /// <summary>
        /// Always true (for now). 
        /// </summary>
        public bool SplitRecordedVideo { get; set; } = true;

        // Local camera height/width used by the caps filter to select the correct output.
        public int CamHeight { get; set; } = 720;
        public int CamWidth { get; set; } = 1280;

        // Sometimes a bit of buffer is desirable. 
        public int Latency { get; set; } = 0;

        public List<OsdObject> OverlayList { get { return _osdObjects; } set { _osdObjects = value; } }

        public string OverlayImagePath { get; set; }

        private CamType _type = CamType.Local; 
        public CamType CamType {
            get { return _type; }
            set {
                if (_type != value)
                {
                    _type = value;
                    SetupSourceBin();
                }
            } }

        /// <summary>
        /// Current state of the camera. Change using the Start.../Stop...() methods.
        /// Invokes corresponding events when set internally. 
        /// TODO: I don't like this. Shouldn't raise events from inside the GST message handler. 
        /// </summary>
        public CamState CameraState { get {
                return _camState; 
            }

            private set {
                var prev = _camState;
                switch (value)
                {
                    case CamState.Previewing:
                        //IsConnected = true;
                        if (prev == CamState.Recording)
                            RecordingStopped?.Invoke(this, new EventArgs()); 

                        _camState = value;
                        if (prev != value)  
                            PreviewStarted?.Invoke(this, new EventArgs()); 
                        break;
                    case CamState.Pending:
                        _camState = value;
                        break;
                    case CamState.Stopped:
                        //IsConnected = false;
                        if (prev == CamState.Recording)
                            RecordingStopped?.Invoke(this, new EventArgs());

                        _camState = value;
                        if(prev != value)
                            PreviewStopped?.Invoke(this, new EventArgs());
                        break;
                    case CamState.Recording:
                        _camState = value;
                        if (prev != value) 
                            RecordingStarted?.Invoke(this, new EventArgs());
                        break;
                    default:
                        _camState = value;
                        break;
                }
                prev = value;
            } }

        /// <summary>
        /// TODO: Implement this - Continue recording on the previous saved video file. 
        /// (hint: use segments but you have to find wait for the next keyframe to avoid the long frozen frame). 
        /// </summary>
        public bool StitchVideos { get { return false; } set { throw new NotImplementedException(); } }

        public bool IsFullscreen { get; set; }

        public string FileSourceLocation { get
            {
                return _fileSrcLoc;
            }

            set
            {
                _fileSrcLoc = value;
                if(srcElt != null)
                {
                    srcElt["location"] = value;
                    
                }
            }

        }
        private string _fileSrcLoc = ""; 

        #region Events
        public event EventHandler PreviewStarted;
        public event EventHandler PreviewStopped;
        public event EventHandler RecordingStarted;
        public event EventHandler RecordingStopped;
        public event EventHandler<YascStreamingException> ErrorStreaming;
        public event EventHandler<MessageArgs> GstStateChanged;
        public event EventHandler<System.Drawing.Image> SnapshotReady;
        public event EventHandler RawGstMessage;
        #endregion

        public GstCam()
        {
            OverlayList = new List<OsdObject>();

            //if (!pipelineCreated)
            //    SetupPipeline(); 

            //osdObjects = new List<OsdObject>(); 
        }

        /// <summary>
        /// TODO: better path detection/searching. 
        /// </summary>
        private void detectGstPath()
        {
            // Fallback path. 
            string pathGst = @"C:\gstreamer\1.0\x86\bin";

            var p = Environment.GetEnvironmentVariable("GSTREAMER_1_0_ROOT_X86");
            if (!string.IsNullOrEmpty(p))
                pathGst = System.IO.Path.Combine(p, "bin");

            // Only use x64 if we're built for x64.
            p = Environment.GetEnvironmentVariable("GSTREAMER_1_0_ROOT_X86_64");
            if (!string.IsNullOrEmpty(p) && IntPtr.Size == 8)
            {
                pathGst = System.IO.Path.Combine(p, "bin");
                sysDbg.WriteLine("Using the 64-bit version of GStreamer..."); 
            }
            
            if (!System.IO.Directory.Exists(pathGst))
                throw new YascBaseException($"Couldn't locate a GStreamer installation at {pathGst}. Please check your environment variable GSTREAMER_1_0_ROOT_X86 or _X86_64 and install either the x86 or x86_64 version of GStreamer.");

            var path = Environment.GetEnvironmentVariable("Path");

            // GStreamer uses the Path variable to find itself. 
            if (!path.StartsWith(pathGst))
                Environment.SetEnvironmentVariable("Path", pathGst + ";" + path);
        }

        /// <summary>
        /// Start camera preview. 
        /// </summary>
        public void StartPreview()
        {
            if (this.CameraState == CamState.Disconnected || this.CameraState == CamState.Stopped)
            {
                if (!this.Connect())
                    throw new YascStreamingException("Error connecting to camera."); 
            }
            else if (this.CameraState == CamState.Previewing)
            {
                sysDbg.WriteLine("Already previewing.");
                return;
            }

            //var ret = pipeline.SetState(State.Null);
            //sysDbg.WriteLine("SetState null: " + ret.ToString());
            //ret = pipeline.SetState(State.Ready);
            //sysDbg.WriteLine("SetState ready: " + ret.ToString());
            StateChangeReturn ret;

            ret = pipeline.SetState(State.Playing);
            sysDbg.WriteLine("SetState playing: " + ret.ToString());

            if (ret == StateChangeReturn.Failure)
            {
                sysDbg.WriteLine("Error setting state to playing: " + ret);
                //IsPreviewing = false;
                this.CameraState = CamState.Disconnected;   
                return;
            }
            else
            {
                sysDbg.WriteLine("Starting preview...");
            }

            GstUtilities.DumpGraph(pipeline, "startPreview");
        }

        /// <summary>
        /// Stop the preview but don't disconnect or destroy the pipeline. 
        /// </summary>
        public void StopPreview()
        {
            try
            {
                if(binRecord != null && binRecord.CurrentState == State.Playing)
                {
                    StopRecord(); 
                }

                if (CameraState >= CamState.Pending)
                {
                    var ret = pipeline.SetState(State.Null);
                    if (ret != StateChangeReturn.Success)
                        sysDbg.WriteLine("Error setting state to null. ");
                    CameraState = CamState.Stopped;
                    //PreviewStopped?.Invoke(this, new EventArgs());
                }
            }
            catch (Exception ex)
            {
                sysDbg.WriteLine("Error stopping preview."); 
                throw ex;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void StopPreviewClosing()
        {
            try
            {
                if(CameraState == CamState.Recording)
                {
                    StopRecord(); 
                }
            }
            catch(Exception ex)
            {
                sysDbg.WriteLine("Failed closing: " + ex.Message); 
            }
        }

        /// <summary>
        /// Start recording by setting up a probe and linking in the recording Tee branch. 
        /// </summary>
        /// <returns>true on success, false on failure</returns>
        public bool StartRecord()
        {
            sysDbg.WriteLine("Start record.");

            if (mTee == null)
                return false;

            // Already recording; do nothing. 
            if (binRecord.CurrentState == State.Playing)
                return true;

            var padTemplate = mTee.GetPadTemplate("src_%u");
            padTeeRec = mTee.RequestPad(padTemplate);

            //var seg = new Segment();
            //seg.Init(Format.Buffers);
            //seg.Flags = SegmentFlags.None;

            //pipeline.SendEvent(Event.NewSegment(seg)); 

            if (padTeeRec == null)
                return false;

            padTeeRec.AddProbe(PadProbeType.Idle, cb_linkRecordTee);

            //this.CameraState = CamState.Recording;
            return true;
        }

        /// <summary>
        /// Stop recording by unlinking the record bin (on a callback). 
        /// </summary>
        /// <returns></returns>
        public bool StopRecord()
        {
            sysDbg.WriteLine("Stop record.");

            if (padTeeRec == null)
                return false;

            padTeeRec.AddProbe(PadProbeType.Idle, cb_unlinkRecordTee);
            this.CameraState = CamState.Previewing;
            return true;

        }

        /// <summary>
        /// Take a snapshot. Attach a handler to the SnapshotReady event. 
        /// </summary>
        public void TakeSnapshot()
        {
            if (!this.IsPreviewing)
                return; 

            if (mDispSink == null) return;

            Gst.Sample rawSample = null;

            // Get the last frame Sample from the display sink. 
            Bin vidBin = mDispSink as Bin;
            Element vidSink = null; 
            if (vidBin.ChildrenCount > 0)
                vidSink = (Element)vidBin.GetChildByIndex(vidBin.ChildrenCount - 1);

            if (vidSink == null)
                return;

            rawSample = (Gst.Sample)(vidSink["last-sample"]);

            if (rawSample != null)
            {

                Sample jpgSample;

                try
                {
                    // Convert the frame to a JPG. 
                    // Timeout is 2 seconds... consider changing. 
                    jpgSample = Gst.Video.Global.VideoConvertSample(rawSample, Caps.FromString("image/jpeg"), Util.Uint64Scale(2, Gst.Constants.SECOND, 1));

                    if (jpgSample == null)
                    {
                        sysDbg.WriteLine("Error converting sample to JPG. ");
                        return;
                    }

                    Gst.Buffer imgBuf = jpgSample.Buffer;

                    // Extract the bytes. 
                    byte[] bytes = new byte[imgBuf.Size];
                    imgBuf.Extract(0, ref bytes);

                    // Create a new image, set the preview window, then save the JPG. 
                    MemoryStream ms = new MemoryStream(bytes);
                    Image img = Image.FromStream(ms);

                    //img.Save(Path.Combine(Option.GetSnapshotFullPath(), Option.GetSnapshotFileName(title)));
                    SnapshotReady?.Invoke(this, img);

                    imgBuf.Dispose();
                    jpgSample.Dispose();
                }
                catch (Exception ex)
                {
                    throw new YascBaseException("Error converting sample: " + ex.Message);
                }
                finally
                {
                    rawSample.Dispose();
                }
            }

        }

        /// <summary>
        /// Initializes the Gstreamer pipeline and Glib loop. 
        /// </summary>
        /// <returns></returns>
        public bool Connect()
        {
            detectGstPath();

            bool r = false;

            if (glibLoop == null)
            {
                glibLoop = new GLib.MainLoop();
                glibThread = new sysThread.Thread(glibLoop.Run) { IsBackground = true };
            }

            try
            {
                if(pipeline == null)
                    pipelineCreated = this.SetupPipeline();
                

                if (!pipelineCreated) sysDbg.WriteLine("Error creating pipeline.");


                StateChangeReturn ret = pipeline.SetState(State.Ready);

                r = pipelineCreated && (ret != StateChangeReturn.Failure);
            }
            catch (Exception ex)
            {
                sysDbg.WriteLine("Error connecting: " + ex.Message);
                throw new YascStreamingException("Error initializing pipeline. "); 
            }
            if (r)
                CameraState = CamState.Connected; 
            
            return r; 
        }

        public void DumpPipeline(string path)
        {
            GstUtilities.DumpGraph(this.pipeline, path); 
        }

        public void Disconnect()
        {

        }

        public string GetCaps()
        {
            return ""; 
        }

        /// <summary>
        /// Setup the source bin. This method can be overridden by inheriting classes for different sources. 
        /// </summary>
        /// <returns>true on success</returns>
        protected virtual bool SetupSourceBin()
        {
            bool ret = true;

            if (pipeline == null)
                return false;

            if (pipeline.CurrentState == State.Playing)
                return false;

            if (!CreateElements())
                return false;

            // Empty the bin to start fresh. 
            foreach (Element e in binSource.IterateElements())
            {
                binSource.Remove(e);
                e.Dispose();
                e.Unref();
            }

            // Unlink from decodeBin.
            if(padSrcBinSource != null)
            {
                padSrcBinSource.Unlink(binDecode.GetStaticPad("sink"));
                //padSrcBinSource.Unref();
                padSrcBinSource = null;
            }

            // Pick source element based on choice.
            if (this._type == CamType.TestSrc)
            {
                sysDbg.WriteLine("Creating video test source.");
                srcElt = ElementFactory.Make("videotestsrc", "testsrc0");
                ret &= binSource.Add(srcElt);

                srcElt["pattern"] = this.DeviceIndex; 

                padSrcBinSource = new GhostPad("srcPad", srcElt.GetStaticPad("src"));
                binSource.AddPad(padSrcBinSource);
                binSource.Link(binDecode); 
            }
            else if (
                (string.IsNullOrWhiteSpace(this.ConnectionUri) || this._type == CamType.Local) 
                && _type != CamType.FileSrc)
            {
                sysDbg.WriteLine("Creating local source.");
                srcElt = ElementFactory.Make("ksvideosrc", "localSrc0");
                if (srcElt == null) throw new YascElementNullException("failed to create ksvideosrc.");

                configureLocalSrc(srcElt);
                Element caps = ElementFactory.Make("capsfilter", "caps0");
                caps["caps"] = Caps.FromString($"video/x-raw,width={CamWidth},height={CamHeight}");

                binSource.Add(srcElt, caps);

                ret &= srcElt.Link(caps);
                if (!ret) return false;

                // If we're using ksvideosrc, we know the src pad already so link it now. 
                padSrcBinSource = new GhostPad("srcPad", caps.GetStaticPad("src"));
                binSource.AddPad(padSrcBinSource);
                //var r = padSrcBinSource.Link(binDecode.GetStaticPad("sink"));
                bool rr = binSource.Link(binDecode); 
                var r = PadLinkReturn.Ok;
                if (r != PadLinkReturn.Ok)
                    sysDbg.WriteLine("Error linking source to decode bin: " + r);
                srcElt["device-index"] = this.DeviceIndex; 
            }
            else if (ConnectionUri.StartsWith("rtsp://") && this._type == CamType.Rtsp) // Rtspsrc
            {
                sysDbg.WriteLine("Creating rtsp source.");
                srcElt = ElementFactory.Make("rtspsrc", "rtspsrc");
                if (srcElt == null) throw new YascElementNullException("failed to create rtspsrc.");

                srcElt["location"] = this.ConnectionUri;
                srcElt["drop-on-latency"] = true;
                srcElt["latency"] = this.Latency;

                // We don't have a pad until we enter the playing state, so can't add the ghost pad until later. 
                srcElt.PadAdded += cb_binSrcPadAdded;
                ret &= binSource.Add(srcElt);
                
            }
            else if(this._type == CamType.FileSrc)
            {
                sysDbg.WriteLine("Creating file source. ");
                srcElt = ElementFactory.Make("filesrc", "filesrc0");
                if (srcElt == null) throw new YascElementNullException("Failed to create file source.");
                srcElt["location"] = this.FileSourceLocation;

                //srcElt.PadAdded += cb_binSrcPadAdded;
                ret &= binSource.Add(srcElt);

                padSrcBinSource = new GhostPad("srcPad", srcElt.GetStaticPad("src"));
                ret &= binSource.AddPad(padSrcBinSource);
                ret &= binSource.Link(binDecode);
            }
            else
            {
                sysDbg.WriteLine("Error creating source. ");
                return false;
            }

            mTee = ElementFactory.Make("tee", "tee0");
            GstUtilities.CheckError(mTee);

            return ret;
        }

        

        protected void configureLocalSrc(Element src)
        {
            src["device-index"] = this.DeviceIndex;
            //caps["width"] = 1280;
            //caps["height"] = 720;

        }

        /// <summary>
        /// Setup the pipeline, including starting the glib and gtk loops. Then, create and link the elements. 
        /// In hindsight: Doing a ParseLaunch would have been much easier. Then just get the elements needed with pipeline.GetChildByName (or a variant).
        /// //Parse.Launch($"rtspsrc location={this.ConnectionUri} latency={this.Latency} drop-on-latency=true ! decodebin ! textoverlay ! tee name=t0 ! queue ! videoconvert ! autovideosink name=vidsink"); 
        /// Not sure if this would take care of auto linking though - can't link until we start the pipeline and get rtsp data flowing. 
        /// </summary>
        /// <returns></returns>
        protected bool SetupPipeline()
        {
            sysDbg.WriteLine("Setting up pipeline...");

            // Init GStreamer and GLib.
            try
            {
                if (!Gst.Application.InitCheck())
                    Gst.Application.Init();
                GtkSharp.GstreamerSharp.ObjectManager.Initialize();
            }
            catch (Exception ex)
            {
                sysDbg.WriteLine("Error initing gst applicaiton: " + ex.Message);
            }
            
            if (glibThread.ThreadState != sysThread.ThreadState.Running)
            {
                glibThread.Start();
                sysDbg.WriteLine("glib thread started.");
            }

            // Create bins.
            try
            {
                if (_type == CamType.FileSrc)
                {
                    // FIXME: files aren't playing properly for some reason. 
                    pipeline = (Pipeline)Parse.Launch($"filesrc name=src0 ! decodebin name=decode0 ! videoconvert ! autovideosink name=sink0");

                    ((Element)pipeline.GetChildByName("src0"))["location"] = this._fileSrcLoc; 
                    binDecode = (Bin)pipeline.GetChildByName("decode0");

                    if(binDecode != null)
                        binDecode.PadAdded += cb_binDecPadAdded;
                }
                else
                {
                    pipeline = new Pipeline("pipeline0");
                    GstUtilities.CheckError(pipeline);
                    binSource = new Bin("srcbin0");
                    GstUtilities.CheckError(binSource);
                    binRecord = new Bin("recordbin0");
                    GstUtilities.CheckError(binRecord);
                    binDecode = (Bin)ElementFactory.Make("decodebin");
                    GstUtilities.CheckError(binDecode);
                }
            }
            catch (Exception ex)
            {
                sysDbg.WriteLine("failed creating bins: " + ex.Message);
                return false;
            }

            // Listen for messages. 
            var bus = pipeline.Bus;
            if (bus != null)
            {
                bus.AddSignalWatch();
                bus.Connect("message::Error", HandleError);
                bus.Connect("message::Info", HandleInfo);
                // Need sync message for binding the rendering window to a panel. 
                bus.EnableSyncMessageEmission();
                bus.SyncMessage += cb_busSyncMessage;
                bus.AddSignalWatch();
                bus.Message += HandleMessage;
            }
            else
                throw new YascElementNullException("Bus is null. Failed to setup pipeline."); 

            if(binSource != null && binDecode != null)
                pipeline.Add(binSource, binDecode);

            if (_type != CamType.FileSrc)
            {
                SetupSourceBin();
                SetupRecordBinMp4();

                try
                {
                    // Add in the rendering elements. 
                    pipeline.Add(mTee, mQueueDisp, mDispSink);

                    var template = mTee.GetPadTemplate("src_%u");
                    padTeeDisp = mTee.RequestPad(template);

                    var ret = padTeeDisp.Link(mQueueDisp.GetStaticPad("sink"));
                    if (ret != PadLinkReturn.Ok)
                        sysDbg.WriteLine("Error linking tee to queue: " + ret.ToString());

                    // We have to link after a pad is created on the decode bin. 
                    binDecode.PadAdded += cb_binDecPadAdded;

                    // Link the last pad. 
                    if (!mQueueDisp.Link(mDispSink))
                        sysDbg.WriteLine("Error linking display queue to display sink.");

                }
                catch (Exception ex)
                {
                    sysDbg.WriteLine("Error linking: " + ex.Message);
                    return false;
                }
                //this.DumpPipeline("AfterSetup"); 
            }
            return true;
        }

        #region Callbacks
        /// <summary>
        /// Add all of the overlay elements to the pipeline. Assumes the source half is already linked to the Tee element. 
        /// </summary>
        /// <param name="p"></param>
        /// <param name="inf"></param>
        /// <returns></returns>
        private PadProbeReturn cb_addOverlayElts(Pad p, PadProbeInfo inf)
        {
            // tPad is the sink of the Tee element. Therefore tPrev should be the decodeBin src elt. 
            Pad tPad = padOverlay0; 
            Pad tPrev = tPad.Peer; // Previous src pad and tail of linked-list.
            // The start of the chain of overlays elts. 
            padOverlay0 = tPrev; 

            if (!tPrev.Unlink(tPad))
                sysDbg.WriteLine("Unlink failed.");

            if (OverlayList != null)
            {
                foreach (var osd in OverlayList)
                {
                    // Don't recreate the Elts. 
                    if (osd.OverlayElement == null)
                    {
                        osd.OverlayElement = ElementFactory.Make("textoverlay");
                        osd.RefreshElement();
                    }
                    else
                    {
                        osd.RefreshElement();
                    }

                    var elt = osd.OverlayElement;

                    if (elt != null && elt.Parent == null)
                    {
                        pipeline.Add(elt);

                        if (tPrev.Link(elt.GetStaticPad("video_sink")) != PadLinkReturn.Ok)
                            sysDbg.WriteLine("Error linking overlay. ");

                        //tPrev.Unref();
                        tPrev = elt.GetStaticPad("src");
                        if (!elt.SyncStateWithParent())
                            sysDbg.WriteLine("Error syncing overlay state.");
                    }
                }
            }

            // TODO: expose height/width/position options for image overlay. 
            var img = ElementFactory.Make("gdkpixbufoverlay");
            if(!string.IsNullOrEmpty(this.OverlayImagePath))
                img["location"] = @"C:\gstreamer\1.0\x86\bin\test.png";

            img["overlay-height"] = 200;
            pipeline.Add(img);
            var ret = tPrev.Link(img.GetStaticPad("sink"));
            if(ret != PadLinkReturn.Ok)
            {
                sysDbg.WriteLine("Error linking image overlay."); 
            }

            img.SyncStateWithParent(); 
            tPrev = img.GetStaticPad("src");

            if(tPrev.Link(tPad) != PadLinkReturn.Ok)
            {
                sysDbg.WriteLine("Error linking tprev overlay."); 
            }
            padOverlay0 = padOverlay0.Peer; 

            tPrev?.Unref();
            tPad?.Unref(); 
            DumpPipeline("After overlay add."); 
            return PadProbeReturn.Remove; 
        }

        /// <summary>
        /// Fired after data starts flowing through pipeline and stream type is detected. 
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        private void cb_binDecPadAdded(object o, PadAddedArgs args)
        {
            sysDbg.WriteLine("Decode bin pad added.");

            var newPad = args.NewPad;

            if (newPad.IsLinked)
                return;

            Pad nextPad; 

            if (padOverlay0 == null)
                nextPad = mTee?.GetStaticPad("sink");
            else
                nextPad = padOverlay0; 

            if(nextPad.IsLinked)
            {
                if (!nextPad.Peer.Unlink(nextPad))
                    sysDbg.WriteLine("Failed to unlink."); 
            }

            var ret = newPad.Link(nextPad);
            if (ret != PadLinkReturn.Ok)
                sysDbg.WriteLine("Error linking decode bin to tee: " + ret.ToString());

            //if (mQueueDisp.Link(mDispSink))
            //    sysDbg.WriteLine("Failed to link to dispsink. "); 


            DumpPipeline("afterDecodeLink");
            // Add overlay elements. 
            padOverlay0 = nextPad; 
            newPad.AddProbe(PadProbeType.Idle, cb_addOverlayElts);
        }

        /// <summary>
        /// Fired after pipeline enters paused state for rtspsrc, or other sources where the codec/type isn't known until playing. 
        /// Try to link to DecodeBin.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        private void cb_binSrcPadAdded(object o, PadAddedArgs args)
        {
            var src = (Element)o;
            var newPad = args.NewPad;
            try
            {

                if (padSrcBinSource != null)
                {
                    if (!padSrcBinSource.Unlink(binDecode.GetStaticPad("sink")))
                        sysDbg.WriteLine("Failed to unlink decode bin.");
                    if (!binSource.RemovePad(padSrcBinSource))
                        sysDbg.WriteLine("failed to remove existing source pad.");
                    padSrcBinSource.Unref();
                }

                padSrcBinSource = new GhostPad("srcPad", newPad);
                binSource.AddPad(padSrcBinSource);
            }
            catch (Exception ex)
            {
                sysDbg.WriteLine("Failed adding a pad: " + ex.Message); 
            }

            if (!padSrcBinSource.IsLinked)
            {
                Pad bindecpad = binDecode.GetStaticPad("sink");
                //if (bindecpad.IsLinked) bindecpad.Unlink(); 
                var ret = padSrcBinSource.Link(binDecode.GetStaticPad("sink"));
                if (ret != PadLinkReturn.Ok)
                    sysDbg.WriteLine("Error linking to decbin " + ret.ToString());
            }
            DumpPipeline("afterRtspLink");
        }

        /// <summary>
        /// Set the panel on which to display the video. This must be done at the right time and via this callback. 
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        private void cb_busSyncMessage(object o, SyncMessageArgs args)
        {

            if (Gst.Video.Global.IsVideoOverlayPrepareWindowHandleMessage(args.Message))
            {
                Element src = (Gst.Element)args.Message.Src;

#if DEBUG
                sysDbg.WriteLine("Message'prepare-window-handle' received by: " + src.Name + " " + src.ToString());
#endif

                //if (src != null && (src is Gst.Video.VideoSink | src is Gst.Bin))
                {
                    //    Try to set Aspect Ratio
                    try
                    { 
                        src["force-aspect-ratio"] = true;

                    }
                    catch (PropertyNotFoundException ex) {
                        sysDbg.WriteLine("Error setting aspect ratio: " + ex.Message);
                    }

                    //    Try to set Overlay
                    try
                    {
                        Gst.Video.VideoOverlayAdapter overlay_ = new Gst.Video.VideoOverlayAdapter(src.Handle);
                        overlay_.WindowHandle = (IntPtr)HdlPreviewPanel;
                        overlay_.HandleEvents(true);
                    }
                    catch (Exception ex) {
                        sysDbg.WriteLine("Error setting overlay: " + ex.Message);
                    }
                }
            }
            if (args.Message.Type == MessageType.Eos)
            {
                sysDbg.WriteLine("EOS sync message received...");
            }
        }

        /// <summary>
        /// Handle all bus messages. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void HandleMessage(object sender, MessageArgs args)
        {
            
            var msg = args.Message;
            //System.Diagnostics.Debug.WriteLine("HandleMessage received msg of type: {0}", msg.Type);
            switch (msg.Type)
            {
                case MessageType.Error:
                    //
                    GLib.GException err;
                    string debug;
                    msg.ParseError(out err, out debug);
                    if (debug == null) { debug = "none"; }
                    sysDbg.WriteLine("\tError received from element {0}: {1}", msg.Src.Name, err.Message);
                    //sysDbg.WriteLine("\tDebugging information: " + debug);
                    GstUtilities.DumpGraph(pipeline, "error");
                    StopPreview();
                    this.ErrorStreaming?.Invoke(this, new YascStreamingException($"Stream failed, {err.Message} from element {msg.Src}. Debug info: {debug}"));
                    break;
                case MessageType.StreamStatus:
                    Gst.StreamStatusType status;
                    Element theOwner;
                    msg.ParseStreamStatus(out status, out theOwner);
                    sysDbg.WriteLine("\tCase StreamingStatus: status is: " + status + "; Owner is: " + theOwner.Name);
                    break;
                case MessageType.StateChanged:
                    //System.Diagnostics.Debug.WriteLine("Case StateChanged: " + args.Message.ToString());
                    State oldState, newState, pendingState;
                    msg.ParseStateChanged(out oldState, out newState, out pendingState);
                    if (newState == State.Paused)
                        args.RetVal = false;
                    sysDbg.WriteLine("\tPipeline state changed from {0} to {1}; Pending: {2}", Element.StateGetName(oldState), Element.StateGetName(newState), Element.StateGetName(pendingState));
                    switch(newState)
                    {
                        case State.VoidPending:
                        case State.Null:
                            CameraState = CamState.Stopped;
                            //this.PreviewStopped?.Invoke(this, new EventArgs());
                            break;
                        case State.Playing:
                            if (CameraState != CamState.Recording)
                            {
                                CameraState = CamState.Previewing;
                                //this.PreviewStarted?.Invoke(this, new EventArgs()); 
                            }
                            if (msg.Src.Name == binRecord?.Name)
                            {
                                CameraState = CamState.Recording;
                                //this.RecordingStarted(this, new EventArgs()); 
                            }
                            break;
                        case State.Ready:
                            CameraState = CamState.Connected;
                            break;
                        case State.Paused:
                            CameraState = CamState.Pending;
                            break;
                        default:
                            //CameraState = CamState.Disconnected;
                            //this.PreviewStopped?.Invoke(this, new EventArgs()); 
                            break;
                    }

                    GstStateChanged?.Invoke(sender, args);

                    break;
                // !!!!! Important !!!!! Need to end recording cleanly to writeout headers. 
                case MessageType.Element:
                    var structure = msg.Structure;

                    // Remove the recording bin when recording is stopped, but only after EOS is received. 
                    if (structure.HasName("GstBinForwarded"))
                    {
                        sysDbg.WriteLine("Gst bin forwarded message...");
                        var m = (Gst.Message)structure.GetValue("message").Val;
                        if (m.Type == MessageType.Eos)
                        {
                            sysDbg.WriteLine("EOS received from: " + m.Src.Name);
                            if (m.Src.Name == binRecord.Name)
                            {
                                removeBinRec();
                                this.CameraState = CamState.Previewing;
                            }
                        }
                    }
                    // Triggered when the mouse moves over the preview window (among other things). 
                    //IntPtr gError;
                    //string dbg;
                    //msg.ParseInfo(out gError, out dbg);
                    //var src = msg.Src as Gst.Video.VideoSink;

                    //var s = msg.Structure;
                    //string sname = s.Name;

                    //System.Diagnostics.Debug.WriteLine(string.Format("Element message: {0}:{1}", msg.Src.Name, sname));
                    //System.Diagnostics.Debug.WriteLine("Element msg received...");

                    break;
                case MessageType.Qos:
                    Format fmt;
                    ulong processed, dropped;
                    long jitt; double prop; int qual;

                    msg.ParseQosStats(out fmt, out processed, out dropped);
                    msg.ParseQosValues(out jitt, out prop, out qual);
                    sysDbg.WriteLine($"\tQoS Stats from {msg.Src.Name} - format: {fmt}, processed: {processed}, dropped: {dropped}");
                    sysDbg.WriteLine($"\tQoS Values - jitter:{jitt}, proportion: {prop}, quality: {qual}");
                    break;
                case MessageType.Warning:
                    IntPtr gError;
                    string dbg;
                    msg.ParseWarning(out gError, out dbg);
                    sysDbg.WriteLine("\tWarning! code " + gError + ". Debug - " + dbg);
                    break;
                case MessageType.Eos:
                    sysDbg.WriteLine("EOS received...");
                    break;
                default:
                    sysDbg.WriteLine("\tHandleMessage received msg of type: {0}", msg.Type);
                    break;
            }
            args.RetVal = true;
            RawGstMessage?.Invoke(sender, args);
        }

        /// <summary>
        /// This function is called when an error message is posted on the bus
        /// </summary>
        private static void HandleError(object sender, GLib.SignalArgs args)
        {
            GLib.GException err;
            string debug;
            var msg = (Message)args.Args[0];

            // Print error details on the screen
            msg.ParseError(out err, out debug);
            sysDbg.WriteLine("Error received from element {0}: {1}", msg.Src.Name, err.Message);
            sysDbg.WriteLine("Debugging information: {0}", debug != null ? debug : "none");

            glibLoop.Quit();
        }

        /// <summary>
        /// Handle bus info messages.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void HandleInfo(object sender, GLib.SignalArgs args)
        {
            //GLib.GException err;
            //string debug;
            //var msg = (Message)args.Args[0];
            //string msgInfo;

            //IntPtr gInfo;

            //msg.ParseInfo(out gInfo, out msgInfo);
        }

        /// <summary>
        /// Link and sync the recording bin to start recording. 
        /// </summary>
        /// <param name="p"></param>
        /// <param name="inf"></param>
        /// <returns></returns>
        private PadProbeReturn cb_linkRecordTee(Pad p, PadProbeInfo inf)
        {
            try
            {
                sysDbg.WriteLine("Linking....");

                if (IsRecording)
                    return PadProbeReturn.Ok;

                if (pipeline == null || padTeeRec == null)
                    return PadProbeReturn.Remove;

                if (binRecord == null)
                    SetupRecordBinMp4();

                // Recreate the splitmuxsink to restart recording. 
                if (mSplitMuxSink != null)
                {
                    var encPad = mEncx264.GetStaticPad("src");
                    if (encPad.IsLinked)
                    {
                        encPad.Unlink(mSplitMuxSink.GetStaticPad("sink"));
                        if (!binRecord.Remove(mSplitMuxSink))
                            sysDbg.WriteLine("Remove failed for splitmux.");
                        //Console.WriteLine("remove failed."); 

                        mSplitMuxSink.Dispose();

                        mSplitMuxSink = null;
                    }
                }

                if (mSplitMuxSink == null)
                {
                    mSplitMuxSink = ElementFactory.Make("splitmuxsink", "splitmux1");
                    if (GstUtilities.CheckError(mSplitMuxSink))
                        sysDbg.WriteLine("Error recreating splitmuxsink.");

                    if (!binRecord.Add(mSplitMuxSink))
                        sysDbg.WriteLine("Error adding splitmux.");

                    if (!mEncx264.Link(mSplitMuxSink))
                        sysDbg.WriteLine("error linking splitmux.");
                }

                if (SplitRecordedVideo && mSplitMuxSink != null)
                {
                    mSplitMuxSink["location"] = _recFname;
                    mSplitMuxSink["max-size-time"] = GstUtilities.SecondsToNs((uint)(VideoSplitTimeS == 0 ? 600 : VideoSplitTimeS)); //600 * 1E9; // in ns (10 mins)
                    mSplitMuxSink["max-size-bytes"] = GstUtilities.MbToBytes((uint)(VideoSplitSizeMb == 0 ? 200 : VideoSplitSizeMb)); //200 * 1E6; // 200 MB                                                               
                    //mSplitMuxSink["async-handling"] = true;
                }
                else
                {

                }

                //padTeeDisp.Unlink(mQueueDisp.GetStaticPad("sink"));

                sysDbg.WriteLine("padRec TaskState: " + padTeeRec.TaskState);

                pipeline.Add(binRecord);

                var retLink = padTeeRec.Link(padRecBinSink);

                if (retLink != PadLinkReturn.Ok)
                    sysDbg.WriteLine("Error linking tee to record bin. " + retLink.ToString());

                //retLink = padTeeDisp.Link(mQueueDisp.GetStaticPad("sink"));
                //if (retLink != PadLinkReturn.Ok)
                //    sysDbg.WriteLine("Error linking display branch. " + retLink.ToString());


                StateChangeReturn ret;
                //var ret = binRec.SetState(State.Playing);
                //if (ret != StateChangeReturn.Success)
                //    Console.WriteLine("Link ret=" + ret.ToString());

                sysDbg.WriteLine("Pipeline state: " + pipeline.CurrentState.ToString());

                if (pipeline.CurrentState != State.Playing)
                {
                    sysDbg.WriteLine("Set pipeline state to playing...");
                    ret = pipeline.SetState(State.Playing);
                    if (ret != StateChangeReturn.Success)
                        sysDbg.WriteLine("SetState ret: " + ret);
                }

                if (!binRecord.SyncStateWithParent())
                    sysDbg.WriteLine("Error syncing state with parent.");
                
                sysDbg.WriteLine("Recording started...");

                GstUtilities.DumpGraph(pipeline, "rtsp_after_linkTee");
                
            }
            catch (Exception ex)
            {
                sysDbg.WriteLine("Error linking in record tee: " + ex.Message + "\n" + ex.StackTrace);
            }
            return PadProbeReturn.Remove;

        }

        /// <summary>
        /// Do the actual unlink to stop recording. However, we must wait until the EOS message is posted on the bus before removing the bin. 
        /// </summary>
        /// <param name="p"></param>
        /// <param name="inf"></param>
        /// <returns></returns>
        private PadProbeReturn cb_unlinkRecordTee(Pad p, PadProbeInfo inf)
        {
            sysDbg.WriteLine("unlinking...");
            // If we're not recording, simply do nothing (remove the probe).  
            if (this.CameraState != CamState.Recording)
                return PadProbeReturn.Remove;

            mEncx264.SendEvent(Event.NewEos());

            padTeeRec.Unlink(padRecBinSink);

            mTee.ReleaseRequestPad(padTeeRec);
            padTeeRec.Unref(); 

            GstUtilities.DumpGraph(this.pipeline, "testsrc_after_unlink.dot");

            this.CameraState = CamState.Previewing;

            return PadProbeReturn.Remove;

        }

        #endregion

        /// <summary>
        /// Remove the recording bin to stop recording. Very important that this is done _after_ the mux finishes the file. 
        /// </summary>
        private void removeBinRec()
        {
            var ret = binRecord.SetState(State.Null);

            if (ret != StateChangeReturn.Success)
                sysDbg.WriteLine("Error stopping record bin: " + ret.ToString());

            if (!pipeline.Remove(binRecord))
                sysDbg.WriteLine("Error removing record bin from pipeline.");
            else
                sysDbg.WriteLine("Record bin removed successfully.");
        }


        /// <summary>
        /// Create the Bin for all the recording elements so starting/stopping the recording becomes easy (add/remove Bin as needed). 
        /// </summary>
        protected void SetupRecordBinMp4()
        {
            // Create elements
            mEncx264 = ElementFactory.Make("x264enc", "enc0");
            GstUtilities.CheckError(mEncx264);
            //muxMp4 = ElementFactory.Make("mp4mux", "mux0");
            //GstUtilities.CheckError(muxMp4);

            //mFileSink = ElementFactory.Make("filesink", "fsink0");
            mSplitMuxSink = ElementFactory.Make("splitmuxsink", "splitsink0");
            GstUtilities.CheckError(mSplitMuxSink);

            mQueueRec = ElementFactory.Make("queue", "qRec");

            // Add to bin. 
            binRecord.Add(mQueueRec, mEncx264, mSplitMuxSink);

            padRecBinSink = new GhostPad("sink", mQueueRec.GetStaticPad("sink"));
            binRecord.AddPad(padRecBinSink);
            //mPipeline.Add(mEncx264, mQueue2, mMux, mFileSink);

            // Configure elements.
            mEncx264["sliced-threads"] = true; // lower effiency, higher speed.
            mEncx264["cabac"] = false; // try to use baseline profile
            mEncx264["tune"] = 4;
            mEncx264["speed-preset"] = 1; //ultrafast
            mEncx264["pass"] = 4; // constant quantizer encode mode. 
            mEncx264["bitrate"] = (uint)6144; // 6mbps (max when quantizer enabled)
            mEncx264["quantizer"] = 26; // quality hardcoded for now
            mEncx264["qp-min"] = 19; // minimum quality
            mEncx264["qp-max"] = 32; 

            //muxMp4["faststart"] = true;

            //mFileSink["location"] = this.CapFilename;
            //mFileSink["async"] = true;

            mSplitMuxSink["location"] = _recFname;
            mSplitMuxSink["max-size-time"] = GstUtilities.SecondsToNs((uint)(VideoSplitTimeS == 0 ? 600 : VideoSplitTimeS)); //600 * 1E9; // in ns (10 mins)
            mSplitMuxSink["max-size-bytes"] = GstUtilities.MbToBytes((uint)(VideoSplitSizeMb == 0 ? 200 : VideoSplitSizeMb)); //200 * 1E6; // 200 MB   


            mQueueRec["leaky"] = 2;
            mQueueDisp["leaky"] = 2;


            // Link elements (inside bin)
            if (!Element.Link(mQueueRec, mEncx264, mSplitMuxSink))
                sysDbg.WriteLine("Error linking recording pipeline.");

            // Need to forward messages to propogate the EOS event. I think. 
            pipeline.MessageForward = true;

        }

        /// <summary>
        /// Create the elements for rendering preview.
        /// </summary>
        /// <returns>true on success</returns>
        protected bool CreateElements()
        {
            sysDbg.WriteLine("Creating elements...");

            try
            {
                if (mDispSink == null)
                    mDispSink = ElementFactory.Make("autovideosink", "videosink0");
                GstUtilities.CheckError(mDispSink);

                if (this._type == CamType.FileSrc)
                    mDispSink["sync"] = true;
                else
                    mDispSink["sync"] = false;

                if (mQueueDisp == null)
                    mQueueDisp = ElementFactory.Make("queue", "qDisp");
                GstUtilities.CheckError(mQueueDisp);

                //if (mConvert == null)
                //    mConvert = ElementFactory.Make("videoconvert", "conv0");
                //GstUtilities.CheckError(mConvert);

                if (mTee == null)
                    mTee = ElementFactory.Make("tee", "tee0");
                GstUtilities.CheckError(mTee);
            }
            catch (Exception ex)
            {
                sysDbg.WriteLine("Error creating elements: " + ex.Message);
                return false;
            }

            return true;
        }


        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls
        private bool pipelineCreated;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if(IsRecording && mEncx264 != null)
                    {
                        mEncx264.SendEvent(Event.NewEos()); 
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~GstCam() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        //~GstCam()
        //{
        //    if (IsRecording && mEncx264 != null)
        //    {
        //        mEncx264.SendEvent(Event.NewEos());
        //    }

        //    if (glibLoop != null)
        //    {
        //        glibLoop.Quit();
        //    }
        //}
        #endregion

    }
}
