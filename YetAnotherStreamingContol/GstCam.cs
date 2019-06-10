#define YASC_DEBUG

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using sysThread = System.Threading;
using sysDbg = System.Diagnostics.Debug;
using System.Threading.Tasks;
using static YetAnotherStreamingContol.GstEnums;
using Gst;

/*
 * Dependencies: GstSharp (gstreamer-sharp, glib-sharp, gio-sharp).  
 * Basic camera object containing the Gst pipeline. One pipeline per GstCam. YascControl can have multiple GstCams for e.g. a preview stream and record stream.  
 * */


namespace YetAnotherStreamingContol
{
    /// <summary>
    /// A GstCam is the base class for a streaming camera. It can have any type of streaming source (USB, RTSP, MJPG, etc.) and any 
    /// type of recording branch. The rough outline of the pipeline is as follows: 
    /// 
    ///                                     /--> PreviewSink (autovideosink)
    /// SourceBIN --> Overlay --> Tee _____/
    ///                                    \ 
    ///                                     \ --> RecordBIN
    ///                                     
    /// Easier said than done.
    /// </summary>
    public class GstCam : IDisposable
    {
        private Pipeline pipeline;
        sysThread.Thread glibThread;
        static GLib.MainLoop glibLoop;
        Bin binSource, // Either an RTSP or local (USB cam) or test source
            binRecord; // Bin to hold all the recording elements (to add/remove at once). 
        Pad padSrcBinSource, // Pad from source bin
            padTeeRec,       // Pad from record branch
            padTeeDisp,
            padRecBinSink;

        // Source elements. 
        Element mRtspSrc, mRtpDepay, mQueueDec, mDecode;

        // Display elements
        Element mQueueDisp, mDispSink, mConvert; 

        // "Accessories"
        Element mOverlayLeft, mOverlayRight, mOverlayClock, mCaps, mTee;
        

        // Recording elements
        private Element mQueueRec, mEncx264, muxMp4, mFileSink, mSplitMuxSink;

        private CamState _camState = CamState.Disconnected;

        public ulong HdlPreviewPanel { get; set; }
        public bool IsRecording { get; private set; }
        public bool IsConnected { get; private set; }
        public string ConnectionUri { get; set; }
        public string CapFilename { get; set; }
        public int DeviceIndex { get; set; }
        public bool UseTestSource { get; set; } = false;
        public bool EnableFullscreenDblClick { get; set; } = true;
        public int VideoSplitTimeS { get; set; }
        public int VideoSplitSizeMb { get; set; }
        public bool SplitRecordedVideo { get; set; }

        public CamState CameraState { get {
                return _camState; 
                //State st = State.Null, pend = State.Null;
                //pipeline.GetState(out st, out pend, 100);
                //switch(st)
                //{

                //}
            } private set {
                switch (value)
                {
                    case CamState.Previewing:
                        IsConnected = true;
                        _camState = value;
                        PreviewStarted?.Invoke(this, new EventArgs()); 
                        break;
                    case CamState.Pending:
                        _camState = value;
                        break;
                    case CamState.Stopped:
                        IsConnected = false;
                        _camState = value;
                        PreviewStopped?.Invoke(this, new EventArgs());
                        break;
                    case CamState.Recording:
                        _camState = value;
                        RecordingStarted?.Invoke(this, new EventArgs());
                        break;
                    default:
                        _camState = value;
                        break;
                }
            } }

        /// <summary>
        /// TODO: Implement this - Continue recording on the previous saved video file. 
        /// </summary>
        public bool StitchVideos { get { return false; } set { throw new NotImplementedException(); } }

        public bool IsFullscreen { get; set; }

        #region Events
        public event EventHandler PreviewStarted;
        public event EventHandler PreviewStopped;
        public event EventHandler RecordingStarted;
        public event EventHandler RecordingEnded;
        public event EventHandler ErrorStreaming;
        public event EventHandler<MessageArgs> GstStateChanged;
        #endregion

        public GstCam()
        {
            //detectGstPath();

        }

        /// <summary>
        /// TODO: better path detection/searching. 
        /// </summary>
        private void detectGstPath()
        {
            //string pathGst = @"C:\gstreamer\1.0\x86\bin";
            string pathGst = @"C:\gstreamer\1.0\x86\bin";

            string path = Environment.GetEnvironmentVariable("Path");
            Environment.SetEnvironmentVariable("Path", path + ";" + pathGst);
        }

        public void StartPreview()
        {
            if (this.CameraState == CamState.Disconnected)
            {
                if (!this.Connect())
                    throw new YascStreamingException("Error connecting to camera."); 
            }
            else if (this.CameraState == CamState.Previewing)
            {
                sysDbg.WriteLine("Already previewing.");
                return;
            }

            var ret = pipeline.SetState(State.Null);
            sysDbg.WriteLine("SetState null: " + ret.ToString());
            ret = pipeline.SetState(State.Ready);
            sysDbg.WriteLine("SetState ready: " + ret.ToString());
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
                sysDbg.WriteLine("Started preview...");
                this.CameraState = CamState.Previewing;
                //PreviewStarted?.Invoke(this, new EventArgs()); 
            }

            GstUtilities.DumpGraph(pipeline, "yascCam.dot");
        }

        /// <summary>
        /// Stop the preview but don't disconnect or destroy the pipeline. 
        /// </summary>
        public void StopPreview()
        {
            try
            {
                if (CameraState >= CamState.Connected)
                {
                    var ret = pipeline.SetState(State.Null);
                    if (ret != StateChangeReturn.Success)
                        sysDbg.WriteLine("Error setting state to null. ");
                    CameraState = CamState.Stopped;
                    PreviewStopped?.Invoke(this, new EventArgs());
                }
            }
            catch (Exception ex)
            {
                sysDbg.WriteLine("Error stopping preview."); 
                throw ex;
            }
        }

        public bool StartRecord()
        {
            Console.WriteLine("Start record.");

            if (mTee == null)
                return false;

            var padTemplate = mTee.GetPadTemplate("src_%u");
            padTeeRec = mTee.RequestPad(padTemplate);

            if (padTeeRec == null)
                return false;

            padTeeRec.AddProbe(PadProbeType.Idle, linkRecordTeeCb);

            this.CameraState = CamState.Recording;
            return true;
        }

        public void StopRecord()
        {

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

            if (CameraState == CamState.Connected || CameraState == CamState.Stopped || CameraState == CamState.Previewing)
                return true;
            try
            {

                //bool srcCreated = this.createSourceBin();
                bool pipelineCreated = this.SetupPipeline();


                //if(!srcCreated) sysDbg.WriteLine("error creating source bin.");

                if (!pipelineCreated) sysDbg.WriteLine("Error creating pipeline.");


                StateChangeReturn ret = pipeline.SetState(State.Ready);

                r = pipelineCreated && (ret != StateChangeReturn.Failure);
            }
            catch (Exception ex)
            {
                sysDbg.WriteLine("Error connecting: " + ex.Message);
            }
            if (r)
                CameraState = CamState.Connected; 
            
            return r; 
        }

        public void Disconnect()
        {

        }

        public string GetCaps()
        {
            return ""; 
        }

        protected virtual bool SetupSourceBin()
        {
            // Actual source element.
            Element src;
            bool ret = true;

            CreateElements(); 

            if (this.UseTestSource)
            {
                sysDbg.WriteLine("Creating video test source."); 
                src = ElementFactory.Make("videotestsrc", "testsrc0");
                ret &= binSource.Add(src);
                padSrcBinSource = new GhostPad("srcPad", src.GetStaticPad("src"));
            }
            else if (string.IsNullOrWhiteSpace(ConnectionUri))
            {
                sysDbg.WriteLine("Creating local source."); 
                src = ElementFactory.Make("ksvideosrc", "localSrc0");
                if (src == null) throw new ElementNullException("failed to create ksvideosrc.");

                configureLocalSrc(src);
                Element caps = ElementFactory.Make("capsfilter", "caps0");
                caps["caps"] = Caps.FromString("video/x-raw,width=1280,height=720"); 
                //caps["width"] = 1280;
                //caps["height"] = 720;

                binSource.Add(src, caps); 
                ret &= src.Link(caps);

                padSrcBinSource = new GhostPad("srcPad", caps.GetStaticPad("src")); 
            }
            else if(ConnectionUri.StartsWith("rtsp://")) // Rtspsrc
            {
                sysDbg.WriteLine("Creating rtsp source."); 
                src = ElementFactory.Make("rtspsrc", "rtspsrc");
                if (src == null) throw new ElementNullException("failed to create rtspsrc.");

                Element mRtpDepay = ElementFactory.Make("rtph264depay", "rtpdepay0");
                if (src == null) throw new ElementNullException("failed to create rtpDepay.");

                binSource.Add(src, mRtpDepay);
                ret &= src.Link(mRtpDepay);

                padSrcBinSource = new GhostPad("srcPad", mRtpDepay.GetStaticPad("src")); 
            }
            else if(false) // MJPG
            {            }
            else
            {
                sysDbg.WriteLine("Error creating source. ");
                return false; 
            }

            binSource.AddPad(padSrcBinSource); 

            return true;
        }

        protected void configureLocalSrc(Element src)
        {
            src["device-index"] = this.DeviceIndex; 
        }

        protected bool SetupPipeline()
        {
            sysDbg.WriteLine("Setting up pipeline...");


            try
            {
                if(!Gst.Application.InitCheck())
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

            pipeline = new Pipeline("pipeline0");
            binSource = new Bin("srcbin0");
            binRecord = new Bin("recordbin0");

            var bus = pipeline.Bus;
            if (bus != null)
            {
                bus.AddSignalWatch();
                bus.Connect("message::Error", HandleError);
                bus.Connect("message::Info", HandleInfo);
                bus.EnableSyncMessageEmission();
                bus.SyncMessage += Bus_SyncMessage;
                bus.AddSignalWatch();
                bus.Message += HandleMessage;
            }


            SetupSourceBin(); 

            pipeline.Add(binSource, mQueueDisp, mDispSink);
            padSrcBinSource.Link(mQueueDisp.GetStaticPad("sink"));
            //srcBin.Link(mQueueDisp);
            mQueueDisp.Link(mDispSink); 

            return true;
        }

        protected void SetupRecordBinMp4()
        {
            // Create elements
            binRecord = new Bin("record_bin");
            GstUtilities.CheckError(binRecord);
            mEncx264 = ElementFactory.Make("x264enc", "enc0");
            GstUtilities.CheckError(mEncx264);
            mTee = ElementFactory.Make("tee", "tee0");
            GstUtilities.CheckError(mTee);
            muxMp4 = ElementFactory.Make("mp4mux", "mux0");
            GstUtilities.CheckError(muxMp4);

            mFileSink = ElementFactory.Make("filesink", "fsink0");
            mQueueRec = ElementFactory.Make("queue", "qRec");

            // Add to bin. 
            binRecord.Add(mQueueRec, mEncx264, muxMp4, mFileSink);

            padRecBinSink = new GhostPad("sink", mQueueRec.GetStaticPad("sink"));
            binRecord.AddPad(padRecBinSink);
            //mPipeline.Add(mEncx264, mQueue2, mMux, mFileSink);

            // Configure elements.
            mEncx264["sliced-threads"] = true;
            mEncx264["tune"] = 4;
            mEncx264["speed-preset"] = 1;
            mEncx264["bitrate"] = 10000000;

            muxMp4["faststart"] = true;

            mFileSink["location"] = @"C:\gstreamer\recording\test_from_cs.mp4";
            //mFileSink["async"] = true;


            mQueueRec["leaky"] = 2;
            mQueueDisp["leaky"] = 2;


            // Link elements. 
            if (!Element.Link(mQueueRec, mEncx264, muxMp4, mFileSink))
                Console.WriteLine("Error linking recording pipeline.");

            pipeline.MessageForward = true;

        }

        protected bool CreateElements()
        {
            sysDbg.WriteLine("Creating elements...");

            mDispSink = ElementFactory.Make("autovideosink", "videosink0");
            mQueueDisp = ElementFactory.Make("queue", "qDisp");

            mDispSink["sync"] = false;

            return true;
        }

        /// <summary>
        /// Set the panel on which to display the video. This must be done at the right time and via this callback. 
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        private void Bus_SyncMessage(object o, SyncMessageArgs args)
        {
            //System.Diagnostics.Debug.WriteLine("bus_SyncMessage: " + args.Message.Type.ToString());
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
                    catch (Exception ex) { sysDbg.WriteLine("Error setting overlay: " + ex.Message); }
                }
            }
            if (args.Message.Type == MessageType.Eos)
            {
                sysDbg.WriteLine("EOS sync message received...");
            }
        }

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
                    sysDbg.WriteLine("\tError received: " + msg.ToString());
                    msg.ParseError(out err, out debug);
                    if (debug == null) { debug = "none"; }
                    sysDbg.WriteLine("\tError received from element {0}: {1}", msg.Src, err.Message);
                    sysDbg.WriteLine("\tDebugging information: " + debug);
                    StopPreview();
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
                        case State.Null:
                            CameraState = CamState.Stopped;
                            break;
                        case State.Playing:
                            CameraState = CamState.Previewing;
                            break;
                        case State.Ready:
                        case State.Paused:
                            CameraState = CamState.Pending;
                            break;
                        default:
                            CameraState = CamState.Disconnected;
                            break;
                    }
                    break;
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
                                //removeBinRec();
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
                    Console.WriteLine($"\tQoS Stats from {msg.Src.Name} - format: {fmt}, processed: {processed}, dropped: {dropped}");
                    Console.WriteLine($"\tQoS Values - jitter:{jitt}, proportion: {prop}, quality: {qual}");
                    break;
                case MessageType.Warning:
                    IntPtr gError;
                    string dbg;
                    msg.ParseWarning(out gError, out dbg);
                    Console.WriteLine("\tWarning! code " + gError + ". Debug - " + dbg);
                    break;
                case MessageType.Eos:
                    Console.WriteLine("EOS received...");
                    break;
                default:
                    sysDbg.WriteLine("\tHandleMessage received msg of type: {0}", msg.Type);
                    break;
            }
            args.RetVal = true;

            if (this.GstStateChanged != null) GstStateChanged(sender, args);
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
            Console.WriteLine("Error received from element {0}: {1}", msg.Src.Name, err.Message);
            Console.WriteLine("Debugging information: {0}", debug != null ? debug : "none");

            glibLoop.Quit();
        }

        /// <summary>
        /// Handle error
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void HandleInfo(object sender, GLib.SignalArgs args)
        {
            //GLib.GException err;
            //string debug;
            var msg = (Message)args.Args[0];
            string msgInfo;

            IntPtr gInfo;

            msg.ParseInfo(out gInfo, out msgInfo);
        }

        private PadProbeReturn linkRecordTeeCb(Pad p, PadProbeInfo inf)
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
                        mSplitMuxSink = ElementFactory.Make("splitmuxsink", "splitmux1");
                        if (GstUtilities.CheckError(mSplitMuxSink))
                            sysDbg.WriteLine("Error recreating splitmuxsink.");

                        if (!binRecord.Add(mSplitMuxSink))
                            sysDbg.WriteLine("Error adding splitmux.");

                        if (!mEncx264.Link(mSplitMuxSink))
                            sysDbg.WriteLine("error linking splitmux.");
                    }
                }

                if (SplitRecordedVideo && mSplitMuxSink != null)
                {
                    mSplitMuxSink["location"] = CapFilename;
                    mSplitMuxSink["max-size-time"] = GstUtilities.SecondsToNs((uint)(VideoSplitTimeS == 0 ? 600 : VideoSplitTimeS)); //10 * 1E9; // in ns
                    mSplitMuxSink["max-size-bytes"] = GstUtilities.MbToBytes((uint)(VideoSplitSizeMb == 0 ? 200 : VideoSplitSizeMb)); //10 * 1E6; // 10 MB                                                               //mSplitMuxSink["async-handling"] = true;
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

                retLink = padTeeDisp.Link(mQueueDisp.GetStaticPad("sink"));
                if (retLink != PadLinkReturn.Ok)
                    sysDbg.WriteLine("Error linking display branch. " + retLink.ToString());


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

                IsRecording = true;
                sysDbg.WriteLine("Recording started...");

                GstUtilities.DumpGraph(pipeline, "rtsp_after_linkTee.dot");

            }
            catch (Exception ex)
            {
                sysDbg.WriteLine("Error linking in record tee: " + ex.Message + "\n" + ex.StackTrace);
            }
            return PadProbeReturn.Remove;

        }

        private PadProbeReturn unlinkRecordTeeCb(Pad p, PadProbeInfo inf)
        {
            Console.WriteLine("unlinking...");
            // If we're not recording, simply remove the probe. 
            if (this.CameraState != CamState.Recording)
                return PadProbeReturn.Remove;

            mEncx264.SendEvent(Event.NewEos());

            padTeeRec.Unlink(padRecBinSink);

            //mPipeline.Remove(binRec);

            // note: have to wait for EOS to be sent to close the mp4 file correctly. 
            var ret = binRecord.SetState(State.Null);
            if (ret != StateChangeReturn.Success)
                Console.WriteLine("Stopping record bin: " + ret.ToString());

            mTee.ReleaseRequestPad(padTeeRec);

            GstUtilities.DumpGraph(this.pipeline, "testsrc_after_unlink.dot");

            return PadProbeReturn.Remove;

        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

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
