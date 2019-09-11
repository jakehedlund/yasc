using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Yasc;

namespace YascTestApp
{
    /// <summary>
    /// UI for setting up the OSD.
    /// TODO: RTF -> pango support. 
    /// </summary>
    public partial class OsdTextSettings : UserControl
    {
        public OsdObject OsdObject { get; set; }
        private bool initing = false;

        public OsdTextSettings() : this ("")
        {
        }

        public OsdTextSettings(OsdObject obj)
        {
            this.OsdObject = obj;
            setupUiFields(); 
        }

        public OsdTextSettings(string text)
        {
            OsdObject = new OsdObject(text, this.Font.Name, this.Font.Size);
            setupUiFields(); 
        }

        private void setupUiFields()
        {
            initing = true;
            InitializeComponent();

            this.SuspendLayout(); 
            nudFontSize.Value = (decimal)this.OsdObject.FontSize;
            textBox1.Text = this.OsdObject.Text;

            cmbHoriz.DataSource = Enum.GetValues(typeof(GstEnums.TextOverlayHAlign));
            cmbVert.DataSource = Enum.GetValues(typeof(GstEnums.TextOverlayVAlign));


            cmbHoriz.SelectedIndex = 0;
            cmbVert.SelectedIndex = 0; 

            cmbHoriz.SelectedIndex = (int)this.OsdObject.HorizontalAlignment;
            cmbVert.SelectedIndex = (int)this.OsdObject.VerticalAlignment;

            this.ResumeLayout();
            initing = false;
        }

        private void chkEnable_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            OsdObject.Text = textBox1.Text; 
        }

        private void cmbHoriz_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(this.OsdObject != null && !initing)
            {
                GstEnums.TextOverlayHAlign align; 
                Enum.TryParse<GstEnums.TextOverlayHAlign>(cmbHoriz.SelectedValue.ToString(), out align);
                OsdObject.HorizontalAlignment = align;
                OsdObject.RefreshElement(); 
            }
        }

        private void cmbVert_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.OsdObject != null && !initing)
            {
                GstEnums.TextOverlayVAlign align;
                Enum.TryParse<GstEnums.TextOverlayVAlign>(cmbVert.SelectedValue.ToString(), out align);
                OsdObject.VerticalAlignment = align;
                OsdObject.RefreshElement(); 
            }
        }

        private void nudFontSize_ValueChanged(object sender, EventArgs e)
        {
            if(this.OsdObject != null && !initing)
            {
                
            }
        }
    }
}
