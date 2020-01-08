using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace YascTestApp
{
    public partial class LaunchLineForm : Form
    {
        public LaunchLineForm()
        {
            InitializeComponent();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void btnLaunch_Click(object sender, EventArgs e)
        {
            string launchline = fixLaunchLine(comboBox1.Text);

            try
            {
                var p = System.Diagnostics.Process.Start("gst-launch-1.0.exe " + launchline);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to launch: " + launchline + Environment.NewLine + "Stack: " + ex.StackTrace);
            }
        }

        private string fixLaunchLine(string line)
        {

            return line.Trim(); 
        }
    }
}
