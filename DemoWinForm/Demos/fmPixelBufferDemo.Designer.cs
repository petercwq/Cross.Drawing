namespace DemoWinForm
{
    partial class fmPixelBufferDemo
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
            this.sbZoom = new System.Windows.Forms.HScrollBar();
            this.lblZoomFactor = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.btnBufferInBuffer = new System.Windows.Forms.Button();
            this.btnInverseDraw = new System.Windows.Forms.Button();
            this.btnDrawPixels = new System.Windows.Forms.Button();
            this.btnDrawStar = new System.Windows.Forms.Button();
            this.btnInverseDrawStar = new System.Windows.Forms.Button();
            this.btnBufferInBufferDraw = new System.Windows.Forms.Button();
            this.txtDescription = new System.Windows.Forms.TextBox();
            this.pbView = new Cross.Helpers.ScrollablePictureBox();
            this.SuspendLayout();
            // 
            // sbZoom
            // 
            this.sbZoom.Location = new System.Drawing.Point(49, 9);
            this.sbZoom.Minimum = 5;
            this.sbZoom.Name = "sbZoom";
            this.sbZoom.Size = new System.Drawing.Size(192, 17);
            this.sbZoom.TabIndex = 8;
            this.sbZoom.Value = 10;
            this.sbZoom.Scroll += new System.Windows.Forms.ScrollEventHandler(this.sbZoom_Scroll);
            // 
            // lblZoomFactor
            // 
            this.lblZoomFactor.AutoSize = true;
            this.lblZoomFactor.Location = new System.Drawing.Point(253, 13);
            this.lblZoomFactor.Name = "lblZoomFactor";
            this.lblZoomFactor.Size = new System.Drawing.Size(33, 13);
            this.lblZoomFactor.TabIndex = 7;
            this.lblZoomFactor.Text = "100%";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(34, 13);
            this.label1.TabIndex = 6;
            this.label1.Text = "Zoom";
            // 
            // btnBufferInBuffer
            // 
            this.btnBufferInBuffer.Location = new System.Drawing.Point(10, 101);
            this.btnBufferInBuffer.Name = "btnBufferInBuffer";
            this.btnBufferInBuffer.Size = new System.Drawing.Size(133, 23);
            this.btnBufferInBuffer.TabIndex = 12;
            this.btnBufferInBuffer.Text = "3 - Buffer in buffer pixel";
            this.btnBufferInBuffer.UseVisualStyleBackColor = true;
            this.btnBufferInBuffer.Click += new System.EventHandler(this.btnBufferInBuffer_Click);
            // 
            // btnInverseDraw
            // 
            this.btnInverseDraw.Location = new System.Drawing.Point(10, 72);
            this.btnInverseDraw.Name = "btnInverseDraw";
            this.btnInverseDraw.Size = new System.Drawing.Size(133, 23);
            this.btnInverseDraw.TabIndex = 11;
            this.btnInverseDraw.Text = "1 - Inverse draw pixels";
            this.btnInverseDraw.UseVisualStyleBackColor = true;
            this.btnInverseDraw.Click += new System.EventHandler(this.btnInverseDraw_Click);
            // 
            // btnDrawPixels
            // 
            this.btnDrawPixels.Location = new System.Drawing.Point(10, 43);
            this.btnDrawPixels.Name = "btnDrawPixels";
            this.btnDrawPixels.Size = new System.Drawing.Size(133, 23);
            this.btnDrawPixels.TabIndex = 10;
            this.btnDrawPixels.Text = "0 - Draw pixels";
            this.btnDrawPixels.UseVisualStyleBackColor = true;
            this.btnDrawPixels.Click += new System.EventHandler(this.btnDrawPixels_Click);
            // 
            // btnDrawStar
            // 
            this.btnDrawStar.Location = new System.Drawing.Point(10, 160);
            this.btnDrawStar.Name = "btnDrawStar";
            this.btnDrawStar.Size = new System.Drawing.Size(133, 23);
            this.btnDrawStar.TabIndex = 10;
            this.btnDrawStar.Text = "a - Draw star";
            this.btnDrawStar.UseVisualStyleBackColor = true;
            this.btnDrawStar.Click += new System.EventHandler(this.btnDrawStar_Click);
            // 
            // btnInverseDrawStar
            // 
            this.btnInverseDrawStar.Location = new System.Drawing.Point(10, 189);
            this.btnInverseDrawStar.Name = "btnInverseDrawStar";
            this.btnInverseDrawStar.Size = new System.Drawing.Size(133, 23);
            this.btnInverseDrawStar.TabIndex = 11;
            this.btnInverseDrawStar.Text = "b - Inverse draw star";
            this.btnInverseDrawStar.UseVisualStyleBackColor = true;
            this.btnInverseDrawStar.Click += new System.EventHandler(this.btnInverseDrawStar_Click);
            // 
            // btnBufferInBufferDraw
            // 
            this.btnBufferInBufferDraw.Location = new System.Drawing.Point(10, 218);
            this.btnBufferInBufferDraw.Name = "btnBufferInBufferDraw";
            this.btnBufferInBufferDraw.Size = new System.Drawing.Size(133, 23);
            this.btnBufferInBufferDraw.TabIndex = 12;
            this.btnBufferInBufferDraw.Text = "c - Multiple views";
            this.btnBufferInBufferDraw.UseVisualStyleBackColor = true;
            this.btnBufferInBufferDraw.Click += new System.EventHandler(this.btnBufferInBufferDraw_Click);
            // 
            // txtDescription
            // 
            this.txtDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.txtDescription.Location = new System.Drawing.Point(10, 455);
            this.txtDescription.Multiline = true;
            this.txtDescription.Name = "txtDescription";
            this.txtDescription.Size = new System.Drawing.Size(565, 188);
            this.txtDescription.TabIndex = 13;
            // 
            // pbView
            // 
            this.pbView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.pbView.AutoScroll = true;
            this.pbView.Location = new System.Drawing.Point(175, 43);
            this.pbView.Name = "pbView";
            this.pbView.Size = new System.Drawing.Size(400, 400);
            this.pbView.TabIndex = 9;
            // 
            // fmPixelBufferDemo
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(591, 655);
            this.Controls.Add(this.txtDescription);
            this.Controls.Add(this.btnBufferInBufferDraw);
            this.Controls.Add(this.btnBufferInBuffer);
            this.Controls.Add(this.btnInverseDrawStar);
            this.Controls.Add(this.btnInverseDraw);
            this.Controls.Add(this.btnDrawStar);
            this.Controls.Add(this.btnDrawPixels);
            this.Controls.Add(this.pbView);
            this.Controls.Add(this.sbZoom);
            this.Controls.Add(this.lblZoomFactor);
            this.Controls.Add(this.label1);
            this.Name = "fmPixelBufferDemo";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Pixel Buffer demo";
            this.Load += new System.EventHandler(this.fmPixelBufferDemo_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.HScrollBar sbZoom;
        private System.Windows.Forms.Label lblZoomFactor;
        private System.Windows.Forms.Label label1;
        private Cross.Helpers.ScrollablePictureBox pbView;
        private System.Windows.Forms.Button btnBufferInBuffer;
        private System.Windows.Forms.Button btnInverseDraw;
        private System.Windows.Forms.Button btnDrawPixels;
        private System.Windows.Forms.Button btnDrawStar;
        private System.Windows.Forms.Button btnInverseDrawStar;
        private System.Windows.Forms.Button btnBufferInBufferDraw;
        private System.Windows.Forms.TextBox txtDescription;
    }
}