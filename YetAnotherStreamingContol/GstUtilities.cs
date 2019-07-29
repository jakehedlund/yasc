
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gst; 

namespace YetAnotherStreamingContol
{
    public class GstUtilities
    {
        public static void DumpGraph(Pipeline pipe, string name, string path = @"C:\gstreamer\dotfiles")
        {
#if true //YASC_DEBUG
            string dotdata = Gst.Debug.BinToDotData(pipe, DebugGraphDetails.All);
            if (!string.IsNullOrWhiteSpace(dotdata))
            {
                string full = Path.Combine(path, name + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".dot");

                File.WriteAllText(full, dotdata);
            }
#endif
        }

        /// <summary>
        /// Throws if any element is null. 
        /// </summary>
        /// <param name="elts"></param>
        /// <returns></returns>
        public static bool CheckError(params Element[] elts)
        {
            if (elts == null)
                return true;

            if (elts.Any(e => e == null))
            {
                int i = System.Array.FindIndex(elts, el => el == null);
                throw new YascElementNullException($"Element {i} is null. "); 
            }

            return false;
        }

        /// <summary>
        /// Returns true if the element is null. 
        /// </summary>
        /// <param name="elt"></param>
        /// <returns></returns>
        public static bool CheckError(Element elt)
        {
            if (elt == null)
            {
                //System.Diagnostics.Debug.WriteLine("Element is null.");
                throw new YascElementNullException("Error creating an element.");
            }

            return false;
        }

        public static void CheckError(bool err)
        {
            if (err) return;
            //System.Diagnostics.Debug.WriteLine("Error ");
            throw new YascBaseException("Input was false. "); 
        }

        public static double SecondsToNs(uint s)
        {
            return (double)(s * 1e9);
        }

        public static double MbToBytes(uint mb)
        {
            return (double)(mb * 1e6);
        }
    }

    public class GstEnums
    {

        public enum TextOverlayVAlign
        {
            VALIGN_BASELINE,
            VALIGN_BOTTOM,
            VALIGN_TOP,
            VALIGN_POS,
            VALIGN_CENTER,
            VALIGN_ABSOLUTE
        }
        public enum TextOverlayHAlign
        {
            HALIGN_LEFT,
            HALIGN_CENTER,
            HALIGN_RIGHT,
            HALIGN_UNUSED,
            HALIGN_POS,
            HALIGN_ABSOLUTE
        }

        public enum TextOverlayWrapMode
        {
            WRAP_MODE_NONE = -1,
            WRAP_MODE_WORD = 0,
            WRAP_MODE_CHAR = 1,
            WRAP_MODE_WORD_CHAR = 2
        }


        /**
         * GstBaseTextOverlayLineAlign:
         * @GST_BASE_TEXT_OVERLAY_LINE_ALIGN_LEFT: lines are left-aligned
         * @GST_BASE_TEXT_OVERLAY_LINE_ALIGN_CENTER: lines are center-aligned
         * @GST_BASE_TEXT_OVERLAY_LINE_ALIGN_RIGHT: lines are right-aligned
         *
         * Alignment of text lines relative to each other
         */

        public enum TextOverlayLineAlign
        {
            LINE_ALIGN_LEFT = 0,
            LINE_ALIGN_CENTER = 1,
            LINE_ALIGN_RIGHT = 2
        }

        /**
         * GstBaseTextOverlayScaleMode:
         * @GST_BASE_TEXT_OVERLAY_SCALE_MODE_NONE: no compensation
         * @GST_BASE_TEXT_OVERLAY_SCALE_MODE_PAR: compensate pixel-aspect-ratio scaling
         * @GST_BASE_TEXT_OVERLAY_SCALE_MODE_DISPLAY: compensate for scaling to display (as determined by overlay allocation meta)
         * @GST_BASE_TEXT_OVERLAY_SCALE_MODE_USER: compensate scaling set by #GstBaseTextOverlay:scale-pixel-aspect-ratio property
         *
         * Scale text to compensate for and avoid aspect distortion by subsequent
         * scaling of video
         */
        public enum TextOverlayScaleMode
        {
            SCALE_MODE_NONE,
            SCALE_MODE_PAR,
            SCALE_MODE_DISPLAY,
            SCALE_MODE_USER
        }

        public enum CamState
        {
            Disconnected, 
            Pending,
            Connected, 
            Previewing, 
            Recording,
            Stopped
        }

        public enum CamType
        { 
            Local, 
            Rtsp, 
            Mjpg,
            TestSrc,
            FileSrc
        }
    }

    public class YascElementNullException : YascBaseException
    {
        public YascElementNullException(string msg) : base(msg)
        {

        }

        public YascElementNullException(Element element) : this(string.Format("Element {0} is null! ", element.Name)) { }

    }

    public class YascBaseException : Exception
    {
        public YascBaseException(string msg) : base(msg) { }
    }

    public class YascStreamingException : YascBaseException
    {
        public YascStreamingException(StateChangeReturn state) : base(string.Format("State change error: {0} ", state.ToString())) { }

        public YascStreamingException(string msg) : base(msg) { }
    }
}
