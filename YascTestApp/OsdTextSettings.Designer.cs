namespace YascTestApp
{
    partial class OsdTextSettings
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.nudFontSize = new System.Windows.Forms.NumericUpDown();
            this.chkEnable = new System.Windows.Forms.CheckBox();
            this.cmbHoriz = new System.Windows.Forms.ComboBox();
            this.cmbVert = new System.Windows.Forms.ComboBox();
            ((System.ComponentModel.ISupportInitialize)(this.nudFontSize)).BeginInit();
            this.SuspendLayout();
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(3, 2);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(225, 20);
            this.textBox1.TabIndex = 0;
            this.textBox1.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // nudFontSize
            // 
            this.nudFontSize.DecimalPlaces = 2;
            this.nudFontSize.Location = new System.Drawing.Point(234, 2);
            this.nudFontSize.Name = "nudFontSize";
            this.nudFontSize.Size = new System.Drawing.Size(47, 20);
            this.nudFontSize.TabIndex = 1;
            this.nudFontSize.ValueChanged += new System.EventHandler(this.nudFontSize_ValueChanged);
            // 
            // chkEnable
            // 
            this.chkEnable.AutoSize = true;
            this.chkEnable.Checked = true;
            this.chkEnable.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkEnable.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkEnable.Location = new System.Drawing.Point(287, 4);
            this.chkEnable.Name = "chkEnable";
            this.chkEnable.Size = new System.Drawing.Size(44, 21);
            this.chkEnable.TabIndex = 2;
            this.chkEnable.Text = "En";
            this.chkEnable.UseVisualStyleBackColor = true;
            this.chkEnable.CheckedChanged += new System.EventHandler(this.chkEnable_CheckedChanged);
            // 
            // cmbHoriz
            // 
            this.cmbHoriz.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbHoriz.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.cmbHoriz.FormattingEnabled = true;
            this.cmbHoriz.Location = new System.Drawing.Point(330, 1);
            this.cmbHoriz.Name = "cmbHoriz";
            this.cmbHoriz.Size = new System.Drawing.Size(106, 21);
            this.cmbHoriz.TabIndex = 3;
            this.cmbHoriz.SelectedIndexChanged += new System.EventHandler(this.cmbHoriz_SelectedIndexChanged);
            // 
            // cmbVert
            // 
            this.cmbVert.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbVert.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.cmbVert.FormattingEnabled = true;
            this.cmbVert.Location = new System.Drawing.Point(438, 1);
            this.cmbVert.Name = "cmbVert";
            this.cmbVert.Size = new System.Drawing.Size(106, 21);
            this.cmbVert.TabIndex = 4;
            this.cmbVert.SelectedIndexChanged += new System.EventHandler(this.cmbVert_SelectedIndexChanged);
            // 
            // OsdTextSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.cmbVert);
            this.Controls.Add(this.cmbHoriz);
            this.Controls.Add(this.chkEnable);
            this.Controls.Add(this.nudFontSize);
            this.Controls.Add(this.textBox1);
            this.Name = "OsdTextSettings";
            this.Size = new System.Drawing.Size(549, 26);
            ((System.ComponentModel.ISupportInitialize)(this.nudFontSize)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.NumericUpDown nudFontSize;
        private System.Windows.Forms.CheckBox chkEnable;
        private System.Windows.Forms.ComboBox cmbHoriz;
        private System.Windows.Forms.ComboBox cmbVert;
    }
}
