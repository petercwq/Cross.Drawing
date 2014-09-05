namespace DemoWinForm
{
    partial class fmFill
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
            this.pbView = new Demo.Helpers.ScrollablePictureBox();
            this.label1 = new System.Windows.Forms.Label();
            this.lstFills = new System.Windows.Forms.ListBox();
            this.label2 = new System.Windows.Forms.Label();
            this.lstGradientStyle = new System.Windows.Forms.ListBox();
            this.label3 = new System.Windows.Forms.Label();
            this.lstShapes = new System.Windows.Forms.ListBox();
            this.SuspendLayout();
            // 
            // pbView
            // 
            this.pbView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.pbView.AutoScroll = true;
            this.pbView.Location = new System.Drawing.Point(192, 25);
            this.pbView.Name = "pbView";
            this.pbView.Size = new System.Drawing.Size(600, 500);
            this.pbView.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(24, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Fills";
            // 
            // lstFills
            // 
            this.lstFills.FormattingEnabled = true;
            this.lstFills.Items.AddRange(new object[] {
            "Uniform",
            "Radial - Circle",
            "Radial - Ellipse",
            "Radial - Focal",
            "Linear - Vertical",
            "Linear - Horizontal",
            "Linear - Forward Diagonal",
            "Linear - Backward Diagonal"});
            this.lstFills.Location = new System.Drawing.Point(12, 25);
            this.lstFills.Name = "lstFills";
            this.lstFills.Size = new System.Drawing.Size(174, 238);
            this.lstFills.TabIndex = 3;
            this.lstFills.SelectedIndexChanged += new System.EventHandler(this.lstFills_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 279);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(73, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Gradient Style";
            // 
            // lstGradientStyle
            // 
            this.lstGradientStyle.FormattingEnabled = true;
            this.lstGradientStyle.Items.AddRange(new object[] {
            "Reflect",
            "Repeat",
            "Pad"});
            this.lstGradientStyle.Location = new System.Drawing.Point(12, 295);
            this.lstGradientStyle.Name = "lstGradientStyle";
            this.lstGradientStyle.Size = new System.Drawing.Size(174, 56);
            this.lstGradientStyle.TabIndex = 5;
            this.lstGradientStyle.SelectedIndexChanged += new System.EventHandler(this.lstGradientStyle_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(9, 366);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(38, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Shape";
            // 
            // lstShapes
            // 
            this.lstShapes.FormattingEnabled = true;
            this.lstShapes.Items.AddRange(new object[] {
            "Rectangle",
            "Triangle",
            "Star",
            "Crown",
            "Circle Pattern 1",
            "Circle Pattern 2",
            "Complex 1",
            "Complex 2",
            "Complex 3"});
            this.lstShapes.Location = new System.Drawing.Point(12, 382);
            this.lstShapes.Name = "lstShapes";
            this.lstShapes.Size = new System.Drawing.Size(174, 134);
            this.lstShapes.TabIndex = 6;
            this.lstShapes.SelectedIndexChanged += new System.EventHandler(this.lstShapes_SelectedIndexChanged);
            // 
            // fmFill
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(804, 534);
            this.Controls.Add(this.lstShapes);
            this.Controls.Add(this.lstGradientStyle);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.lstFills);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pbView);
            this.Name = "fmFill";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Fill Demo";
            this.Load += new System.EventHandler(this.fmFill_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Demo.Helpers.ScrollablePictureBox pbView;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListBox lstFills;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ListBox lstGradientStyle;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ListBox lstShapes;
    }
}