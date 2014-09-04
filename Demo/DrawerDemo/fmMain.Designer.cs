namespace DemoWinForm
{
    partial class fmMain
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
            this.btnPrimitives = new System.Windows.Forms.Button();
            this.btnTransformations = new System.Windows.Forms.Button();
            this.btnFills = new System.Windows.Forms.Button();
            this.btnGammaCorrection = new System.Windows.Forms.Button();
            this.btnPixelBuffer = new System.Windows.Forms.Button();
            this.btnOpacityMask = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // btnPrimitives
            // 
            this.btnPrimitives.Location = new System.Drawing.Point(26, 55);
            this.btnPrimitives.Name = "btnPrimitives";
            this.btnPrimitives.Size = new System.Drawing.Size(200, 23);
            this.btnPrimitives.TabIndex = 0;
            this.btnPrimitives.Text = "Primitives rendering";
            this.btnPrimitives.UseVisualStyleBackColor = true;
            this.btnPrimitives.Click += new System.EventHandler(this.btnPrimitives_Click);
            // 
            // btnTransformations
            // 
            this.btnTransformations.Location = new System.Drawing.Point(26, 84);
            this.btnTransformations.Name = "btnTransformations";
            this.btnTransformations.Size = new System.Drawing.Size(200, 23);
            this.btnTransformations.TabIndex = 0;
            this.btnTransformations.Text = "Transformations";
            this.btnTransformations.UseVisualStyleBackColor = true;
            this.btnTransformations.Click += new System.EventHandler(this.btnTransformations_Click);
            // 
            // btnFills
            // 
            this.btnFills.Location = new System.Drawing.Point(26, 113);
            this.btnFills.Name = "btnFills";
            this.btnFills.Size = new System.Drawing.Size(200, 23);
            this.btnFills.TabIndex = 0;
            this.btnFills.Text = "Fills";
            this.btnFills.UseVisualStyleBackColor = true;
            this.btnFills.Click += new System.EventHandler(this.btnFills_Click);
            // 
            // btnGammaCorrection
            // 
            this.btnGammaCorrection.Location = new System.Drawing.Point(26, 142);
            this.btnGammaCorrection.Name = "btnGammaCorrection";
            this.btnGammaCorrection.Size = new System.Drawing.Size(200, 23);
            this.btnGammaCorrection.TabIndex = 0;
            this.btnGammaCorrection.Text = "Gamma correction";
            this.btnGammaCorrection.UseVisualStyleBackColor = true;
            this.btnGammaCorrection.Click += new System.EventHandler(this.btnGammaCorrection_Click);
            // 
            // btnPixelBuffer
            // 
            this.btnPixelBuffer.Location = new System.Drawing.Point(26, 26);
            this.btnPixelBuffer.Name = "btnPixelBuffer";
            this.btnPixelBuffer.Size = new System.Drawing.Size(200, 23);
            this.btnPixelBuffer.TabIndex = 0;
            this.btnPixelBuffer.Text = "Pixel buffer";
            this.btnPixelBuffer.UseVisualStyleBackColor = true;
            this.btnPixelBuffer.Click += new System.EventHandler(this.btnPixelBuffer_Click);
            // 
            // btnOpacityMask
            // 
            this.btnOpacityMask.Location = new System.Drawing.Point(26, 173);
            this.btnOpacityMask.Name = "btnOpacityMask";
            this.btnOpacityMask.Size = new System.Drawing.Size(200, 23);
            this.btnOpacityMask.TabIndex = 0;
            this.btnOpacityMask.Text = "Opacity mask";
            this.btnOpacityMask.UseVisualStyleBackColor = true;
            this.btnOpacityMask.Click += new System.EventHandler(this.btnOpacityMask_Click);
            // 
            // fmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(250, 231);
            this.Controls.Add(this.btnOpacityMask);
            this.Controls.Add(this.btnPixelBuffer);
            this.Controls.Add(this.btnGammaCorrection);
            this.Controls.Add(this.btnFills);
            this.Controls.Add(this.btnTransformations);
            this.Controls.Add(this.btnPrimitives);
            this.Name = "fmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Drawer demo";
            this.Load += new System.EventHandler(this.fmMain_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnPrimitives;
        private System.Windows.Forms.Button btnTransformations;
        private System.Windows.Forms.Button btnFills;
        private System.Windows.Forms.Button btnGammaCorrection;
        private System.Windows.Forms.Button btnPixelBuffer;
        private System.Windows.Forms.Button btnOpacityMask;
    }
}

