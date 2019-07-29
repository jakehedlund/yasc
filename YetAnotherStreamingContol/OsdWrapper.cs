using Gst;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static YetAnotherStreamingContol.GstEnums;

namespace YetAnotherStreamingContol
{
    public class OsdObject
    {
        [Browsable(true)]
        [Description("Supports Pango markup, including <span> tags for precise format control.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public string FontDescription
        {
            get
            {
                return _font_desc;
            }
            set
            {
                if (OverlayElement != null)
                {
                    OverlayElement["font-desc"] = value;
                }
                _font_desc = value;
            }
        }

        /// <summary>
        /// Text to display. Supports Pango markup, including span tags with color= attributes and newline characters (\n)
        /// </summary>
        public string Text
        {
            get { return _text; }
            set { _text = value; }
        }
        public float FontSize
        {
            get
            {
                if (_font != null) return _font.Size;
                else
                    return 12f;
            }

        }
        public Font Font
        {
            get
            {
                return _font; 
            }
            set
            {
                _font = value;
                this.FontDescription = _font.FontFamily.ToString() + ", " + _font.Size;
            }
        }

        /// <summary>
        /// Name (id) of this element. 
        /// </summary>
        [Browsable(true)]
        [Description("The Name of this element. ")]
        public string Name
        {
            get
            {
                if (this.OverlayElement != null) return (string)OverlayElement["name"];
                else return "";
            }
            set
            {
                if (OverlayElement != null)
                    OverlayElement["name"] = value;
            }
        }

        [Browsable(true)]
        [Description("Specifies vertical alignment mode.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public TextOverlayVAlign VerticalAlignment {
            get
            {
                return _valign;
            }
            set
            {
                if (OverlayElement != null)
                {
                    OverlayElement["valignment"] = (int)value;
                }
                _valign = value;
            }
        }

        [Browsable(true)]
        [Description("Specifies horizontal alignment mode.")]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public TextOverlayHAlign HorizontalAlignment {
            get
            {
                return _halign;
            }
            set
            {
                if (OverlayElement != null)
                {
                    OverlayElement["halignment"] = (int)value;
                }
                _halign = value;
            }
        }

        /// <summary>
        /// Access to the raw Gstreamer element if necessary. (Use with caution!)
        /// </summary>
        [Browsable(false)]
        [DefaultValue(null)]
        public Element OverlayElement { get; internal set; }

        #region fields

        private string _text;
        private TextOverlayHAlign _halign = TextOverlayHAlign.HALIGN_CENTER;
        private TextOverlayVAlign _valign = TextOverlayVAlign.VALIGN_BOTTOM;
        private string _font_desc;
        private Font _font = new Font(FontFamily.GenericSansSerif, 15f);

        #endregion

        public OsdObject(string text) : this (text, "Arial", 15)
        {

        }

        public OsdObject(string text, string font, int size)
        {
            _text = text;
            FontDescription = font + ", " + size.ToString();
        }

        [ToolboxItem(false)]
        public OsdObject() : this("", "Arial", 15)
        {

        }

        public void RefreshElement()
        {
            if(this.OverlayElement != null)
            {
                OverlayElement["halignment"] = (int)_halign;
                OverlayElement["valignment"] = (int)_valign;
                OverlayElement["font-desc"] = _font_desc;
                OverlayElement["text"] = _text; 
            }
        }

    }
}
