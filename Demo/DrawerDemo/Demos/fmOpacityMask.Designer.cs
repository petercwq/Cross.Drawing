namespace DemoWinForm
{
    partial class fmOpacityMask
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
            this.pbView = new DemoHelpers.ScrollablePictureBox();
            this.sbZoom = new System.Windows.Forms.HScrollBar();
            this.lblZoomFactor = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.btnDrawMask = new System.Windows.Forms.Button();
            this.btnDrawLion = new System.Windows.Forms.Button();
            this.btnDrawWithMask = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // pbView
            // 
            this.pbView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.pbView.AutoScroll = true;
            this.pbView.Location = new System.Drawing.Point(4, 94);
            this.pbView.Name = "pbView";
            this.pbView.Size = new System.Drawing.Size(494, 400);
            this.pbView.TabIndex = 13;
            // 
            // sbZoom
            // 
            this.sbZoom.Location = new System.Drawing.Point(82, 60);
            this.sbZoom.Minimum = 5;
            this.sbZoom.Name = "sbZoom";
            this.sbZoom.Size = new System.Drawing.Size(273, 17);
            this.sbZoom.TabIndex = 12;
            this.sbZoom.Value = 10;
            this.sbZoom.Scroll += new System.Windows.Forms.ScrollEventHandler(this.sbZoom_Scroll);
            // 
            // lblZoomFactor
            // 
            this.lblZoomFactor.AutoSize = true;
            this.lblZoomFactor.Location = new System.Drawing.Point(367, 64);
            this.lblZoomFactor.Name = "lblZoomFactor";
            this.lblZoomFactor.Size = new System.Drawing.Size(33, 13);
            this.lblZoomFactor.TabIndex = 11;
            this.lblZoomFactor.Text = "100%";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(0, 64);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(34, 13);
            this.label1.TabIndex = 10;
            this.label1.Text = "Zoom";
            // 
            // btnDrawMask
            // 
            this.btnDrawMask.Location = new System.Drawing.Point(12, 12);
            this.btnDrawMask.Name = "btnDrawMask";
            this.btnDrawMask.Size = new System.Drawing.Size(124, 23);
            this.btnDrawMask.TabIndex = 14;
            this.btnDrawMask.Text = "Draw clipping mask";
            this.btnDrawMask.UseVisualStyleBackColor = true;
            this.btnDrawMask.Click += new System.EventHandler(this.btnDrawMask_Click);
            // 
            // btnDrawLion
            // 
            this.btnDrawLion.Location = new System.Drawing.Point(142, 12);
            this.btnDrawLion.Name = "btnDrawLion";
            this.btnDrawLion.Size = new System.Drawing.Size(124, 23);
            this.btnDrawLion.TabIndex = 14;
            this.btnDrawLion.Text = "Draw lion";
            this.btnDrawLion.UseVisualStyleBackColor = true;
            this.btnDrawLion.Click += new System.EventHandler(this.btnDrawLion_Click);
            // 
            // btnDrawWithMask
            // 
            this.btnDrawWithMask.Location = new System.Drawing.Point(272, 12);
            this.btnDrawWithMask.Name = "btnDrawWithMask";
            this.btnDrawWithMask.Size = new System.Drawing.Size(176, 23);
            this.btnDrawWithMask.TabIndex = 14;
            this.btnDrawWithMask.Text = "Draw lion with clipping mask";
            this.btnDrawWithMask.UseVisualStyleBackColor = true;
            this.btnDrawWithMask.Click += new System.EventHandler(this.btnDrawWithMask_Click);
            // 
            // fmOpacityMask
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(504, 503);
            this.Controls.Add(this.btnDrawWithMask);
            this.Controls.Add(this.btnDrawLion);
            this.Controls.Add(this.btnDrawMask);
            this.Controls.Add(this.pbView);
            this.Controls.Add(this.sbZoom);
            this.Controls.Add(this.lblZoomFactor);
            this.Controls.Add(this.label1);
            this.Name = "fmOpacityMask";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Drawing with Opacity Mask";
            this.Load += new System.EventHandler(this.fmOpacityMask_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private DemoHelpers.ScrollablePictureBox pbView;
        private System.Windows.Forms.HScrollBar sbZoom;
        private System.Windows.Forms.Label lblZoomFactor;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnDrawMask;
        private System.Windows.Forms.Button btnDrawLion;
        private System.Windows.Forms.Button btnDrawWithMask;
    }
}