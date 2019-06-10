using Gst;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static YetAnotherStreamingContol.GstEnums;

namespace YetAnotherStreamingContol
{
    public class OsdObject
    {
        public string FontDescription { get; set; }
        public string Text { get; set; }
        public int FontSize { get; set; }
        public TextOverlayVAlign VerticalAlignment { get; set; }
        public TextOverlayHAlign HorizontalAlignment { get; set; }

        public Element Overlay { get; private set; }

        public OsdObject(string text) : this (text, "Arial", 15)
        {
        }

        public OsdObject(string text, string font, int size)
        {
            Text = text;
            FontDescription = font + ", " + size.ToString();

            Overlay = ElementFactory.Make("overlay"); 
        }
    }
}
