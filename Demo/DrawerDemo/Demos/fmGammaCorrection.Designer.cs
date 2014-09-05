namespace DemoWinForm
{
    partial class fmGammaCorrection
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.labelGamma = new System.Windows.Forms.Label();
            this.chkUseGamma = new System.Windows.Forms.CheckBox();
            this.sbFactorRed = new System.Windows.Forms.HScrollBar();
            this.sbZoom = new System.Windows.Forms.HScrollBar();
            this.lblZoomFactor = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.pbView = new DemoHelpers.ScrollablePictureBox();
            this.lblGammaFactorRed = new System.Windows.Forms.Label();
            this.lblGammaFactorGreen = new System.Windows.Forms.Label();
            this.sbFactorGreen = new System.Windows.Forms.HScrollBar();
            this.lblGammaFactorBlue = new System.Windows.Forms.Label();
            this.sbFactorBlue = new System.Windows.Forms.HScrollBar();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // labelGamma
            // 
            this.labelGamma.AutoSize = true;
            this.labelGamma.Location = new System.Drawing.Point(12, 43);
            this.labelGamma.Name = "labelGamma";
            this.labelGamma.Size = new System.Drawing.Size(57, 13);
            this.labelGamma.TabIndex = 0;
            this.labelGamma.Text = "Red factor";
            // 
            // chkUseGamma
            // 
            this.chkUseGamma.AutoSize = true;
            this.chkUseGamma.Checked = true;
            this.chkUseGamma.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkUseGamma.Location = new System.Drawing.Point(16, 23);
            this.chkUseGamma.Name = "chkUseGamma";
            this.chkUseGamma.Size = new System.Drawing.Size(132, 17);
            this.chkUseGamma.TabIndex = 1;
            this.chkUseGamma.Text = "Use gamma correction";
            this.chkUseGamma.UseVisualStyleBackColor = true;
            this.chkUseGamma.CheckedChanged += new System.EventHandler(this.chkUseGamma_CheckedChanged);
            // 
            // sbFactorRed
            // 
            this.sbFactorRed.Location = new System.Drawing.Point(94, 43);
            this.sbFactorRed.Maximum = 50;
            this.sbFactorRed.Minimum = 1;
            this.sbFactorRed.Name = "sbFactorRed";
            this.sbFactorRed.Size = new System.Drawing.Size(273, 17);
            this.sbFactorRed.TabIndex = 2;
            this.sbFactorRed.Value = 12;
            this.sbFactorRed.Scroll += new System.Windows.Forms.ScrollEventHandler(this.sbFactor_Scroll);
            // 
            // sbZoom
            // 
            this.sbZoom.Location = new System.Drawing.Point(94, 102);
            this.sbZoom.Minimum = 5;
            this.sbZoom.Name = "sbZoom";
            this.sbZoom.Size = new System.Drawing.Size(273, 17);
            this.sbZoom.TabIndex = 8;
            this.sbZoom.Value = 10;
            this.sbZoom.Scroll += new System.Windows.Forms.ScrollEventHandler(this.sbZoom_Scroll);
            // 
            // lblZoomFactor
            // 
            this.lblZoomFactor.AutoSize = true;
            this.lblZoomFactor.Location = new System.Drawing.Point(379, 102);
            this.lblZoomFactor.Name = "lblZoomFactor";
            this.lblZoomFactor.Size = new System.Drawing.Size(33, 13);
            this.lblZoomFactor.TabIndex = 7;
            this.lblZoomFactor.Text = "100%";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 102);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(34, 13);
            this.label1.TabIndex = 6;
            this.label1.Text = "Zoom";
            // 
            // pbView
            // 
            this.pbView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.pbView.AutoScroll = true;
            this.pbView.Location = new System.Drawing.Point(16, 154);
            this.pbView.Name = "pbView";
            this.pbView.Size = new System.Drawing.Size(494, 436);
            this.pbView.TabIndex = 9;
            // 
            // lblGammaFactorRed
            // 
            this.lblGammaFactorRed.AutoSize = true;
            this.lblGammaFactorRed.Location = new System.Drawing.Point(379, 47);
            this.lblGammaFactorRed.Name = "lblGammaFactorRed";
            this.lblGammaFactorRed.Size = new System.Drawing.Size(22, 13);
            this.lblGammaFactorRed.TabIndex = 7;
            this.lblGammaFactorRed.Text = "1.2";
            // 
            // lblGammaFactorGreen
            // 
            this.lblGammaFactorGreen.AutoSize = true;
            this.lblGammaFactorGreen.Location = new System.Drawing.Point(379, 64);
            this.lblGammaFactorGreen.Name = "lblGammaFactorGreen";
            this.lblGammaFactorGreen.Size = new System.Drawing.Size(22, 13);
            this.lblGammaFactorGreen.TabIndex = 11;
            this.lblGammaFactorGreen.Text = "1.2";
            // 
            // sbFactorGreen
            // 
            this.sbFactorGreen.Location = new System.Drawing.Point(94, 60);
            this.sbFactorGreen.Maximum = 50;
            this.sbFactorGreen.Minimum = 1;
            this.sbFactorGreen.Name = "sbFactorGreen";
            this.sbFactorGreen.Size = new System.Drawing.Size(273, 17);
            this.sbFactorGreen.TabIndex = 10;
            this.sbFactorGreen.Value = 12;
            this.sbFactorGreen.Scroll += new System.Windows.Forms.ScrollEventHandler(this.sbFactorGreen_Scroll);
            // 
            // lblGammaFactorBlue
            // 
            this.lblGammaFactorBlue.AutoSize = true;
            this.lblGammaFactorBlue.Location = new System.Drawing.Point(379, 81);
            this.lblGammaFactorBlue.Name = "lblGammaFactorBlue";
            this.lblGammaFactorBlue.Size = new System.Drawing.Size(22, 13);
            this.lblGammaFactorBlue.TabIndex = 13;
            this.lblGammaFactorBlue.Text = "1.2";
            this.lblGammaFactorBlue.Click += new System.EventHandler(this.label3_Click);
            // 
            // sbFactorBlue
            // 
            this.sbFactorBlue.Location = new System.Drawing.Point(94, 77);
            this.sbFactorBlue.Maximum = 50;
            this.sbFactorBlue.Minimum = 1;
            this.sbFactorBlue.Name = "sbFactorBlue";
            this.sbFactorBlue.Size = new System.Drawing.Size(273, 17);
            this.sbFactorBlue.TabIndex = 12;
            this.sbFactorBlue.Value = 12;
            this.sbFactorBlue.Scroll += new System.Windows.Forms.ScrollEventHandler(this.hScrollBar2_Scroll);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 60);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(66, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "Green factor";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 77);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(58, 13);
            this.label3.TabIndex = 0;
            this.label3.Text = "Blue factor";
            // 
            // fmGammaCorrection
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(522, 602);
            this.Controls.Add(this.lblGammaFactorBlue);
            this.Controls.Add(this.sbFactorBlue);
            this.Controls.Add(this.lblGammaFactorGreen);
            this.Controls.Add(this.sbFactorGreen);
            this.Controls.Add(this.pbView);
            this.Controls.Add(this.sbZoom);
            this.Controls.Add(this.lblGammaFactorRed);
            this.Controls.Add(this.lblZoomFactor);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.sbFactorRed);
            this.Controls.Add(this.chkUseGamma);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.labelGamma);
            this.Name = "fmGammaCorrection";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Gamma Correction Demo";
            this.Load += new System.EventHandler(this.fmGammaCorrection_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelGamma;
        private System.Windows.Forms.CheckBox chkUseGamma;
        private System.Windows.Forms.HScrollBar sbFactorRed;
        private System.Windows.Forms.HScrollBar sbZoom;
        private System.Windows.Forms.Label lblZoomFactor;
        private System.Windows.Forms.Label label1;
        private DemoHelpers.ScrollablePictureBox pbView;
        private System.Windows.Forms.Label lblGammaFactorRed;
        private System.Windows.Forms.Label lblGammaFactorGreen;
        private System.Windows.Forms.HScrollBar sbFactorGreen;
        private System.Windows.Forms.Label lblGammaFactorBlue;
        private System.Windows.Forms.HScrollBar sbFactorBlue;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
    }
}