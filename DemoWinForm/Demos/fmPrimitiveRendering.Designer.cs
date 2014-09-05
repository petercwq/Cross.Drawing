namespace DemoWinForm
{
    partial class fmPrimitiveRendering
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
            this.btnDrawRectangle = new System.Windows.Forms.Button();
            this.btnDrawEllipse = new System.Windows.Forms.Button();
            this.btnDrawPolygon = new System.Windows.Forms.Button();
            this.btnDrawPath = new System.Windows.Forms.Button();
            this.pnlNav = new System.Windows.Forms.Panel();
            this.lstTests = new System.Windows.Forms.ListBox();
            this.btnDrawTest = new System.Windows.Forms.Button();
            this.btnDrawRoundRect = new System.Windows.Forms.Button();
            this.pnlControls = new System.Windows.Forms.Panel();
            this.sbZoom = new System.Windows.Forms.HScrollBar();
            this.lblZoomFactor = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.pbView = new Cross.Helpers.ScrollablePictureBox();
            this.pnlNav.SuspendLayout();
            this.pnlControls.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnDrawRectangle
            // 
            this.btnDrawRectangle.Location = new System.Drawing.Point(12, 13);
            this.btnDrawRectangle.Name = "btnDrawRectangle";
            this.btnDrawRectangle.Size = new System.Drawing.Size(116, 23);
            this.btnDrawRectangle.TabIndex = 2;
            this.btnDrawRectangle.Text = "Rectangle";
            this.btnDrawRectangle.UseVisualStyleBackColor = true;
            this.btnDrawRectangle.Click += new System.EventHandler(this.btnDrawRectangle_Click);
            // 
            // btnDrawEllipse
            // 
            this.btnDrawEllipse.Location = new System.Drawing.Point(12, 71);
            this.btnDrawEllipse.Name = "btnDrawEllipse";
            this.btnDrawEllipse.Size = new System.Drawing.Size(116, 23);
            this.btnDrawEllipse.TabIndex = 2;
            this.btnDrawEllipse.Text = "Ellipse";
            this.btnDrawEllipse.UseVisualStyleBackColor = true;
            this.btnDrawEllipse.Click += new System.EventHandler(this.btnDrawEllipse_Click);
            // 
            // btnDrawPolygon
            // 
            this.btnDrawPolygon.Location = new System.Drawing.Point(12, 100);
            this.btnDrawPolygon.Name = "btnDrawPolygon";
            this.btnDrawPolygon.Size = new System.Drawing.Size(116, 23);
            this.btnDrawPolygon.TabIndex = 2;
            this.btnDrawPolygon.Text = "Polygon";
            this.btnDrawPolygon.UseVisualStyleBackColor = true;
            this.btnDrawPolygon.Click += new System.EventHandler(this.btnDrawPolygon_Click);
            // 
            // btnDrawPath
            // 
            this.btnDrawPath.Location = new System.Drawing.Point(12, 129);
            this.btnDrawPath.Name = "btnDrawPath";
            this.btnDrawPath.Size = new System.Drawing.Size(116, 23);
            this.btnDrawPath.TabIndex = 2;
            this.btnDrawPath.Text = "Path";
            this.btnDrawPath.UseVisualStyleBackColor = true;
            this.btnDrawPath.Click += new System.EventHandler(this.btnDrawPath_Click);
            // 
            // pnlNav
            // 
            this.pnlNav.Controls.Add(this.lstTests);
            this.pnlNav.Controls.Add(this.btnDrawTest);
            this.pnlNav.Controls.Add(this.btnDrawPath);
            this.pnlNav.Controls.Add(this.btnDrawRoundRect);
            this.pnlNav.Controls.Add(this.btnDrawRectangle);
            this.pnlNav.Controls.Add(this.btnDrawPolygon);
            this.pnlNav.Controls.Add(this.btnDrawEllipse);
            this.pnlNav.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlNav.Location = new System.Drawing.Point(0, 0);
            this.pnlNav.Name = "pnlNav";
            this.pnlNav.Size = new System.Drawing.Size(147, 576);
            this.pnlNav.TabIndex = 3;
            // 
            // lstTests
            // 
            this.lstTests.FormattingEnabled = true;
            this.lstTests.Items.AddRange(new object[] {
            "Triangle",
            "Star",
            "Crown",
            "Circle pattern 1",
            "Circle pattern 2",
            "Complex 1",
            "Complex 2",
            "Complex 3",
            "Lion"});
            this.lstTests.Location = new System.Drawing.Point(12, 189);
            this.lstTests.Name = "lstTests";
            this.lstTests.Size = new System.Drawing.Size(120, 160);
            this.lstTests.TabIndex = 4;
            this.lstTests.SelectedIndexChanged += new System.EventHandler(this.lstTests_SelectedIndexChanged);
            // 
            // btnDrawTest
            // 
            this.btnDrawTest.Location = new System.Drawing.Point(12, 160);
            this.btnDrawTest.Name = "btnDrawTest";
            this.btnDrawTest.Size = new System.Drawing.Size(116, 23);
            this.btnDrawTest.TabIndex = 2;
            this.btnDrawTest.Text = "Draw test";
            this.btnDrawTest.UseVisualStyleBackColor = true;
            this.btnDrawTest.Click += new System.EventHandler(this.btnDrawTest_Click);
            // 
            // btnDrawRoundRect
            // 
            this.btnDrawRoundRect.Location = new System.Drawing.Point(12, 42);
            this.btnDrawRoundRect.Name = "btnDrawRoundRect";
            this.btnDrawRoundRect.Size = new System.Drawing.Size(116, 23);
            this.btnDrawRoundRect.TabIndex = 2;
            this.btnDrawRoundRect.Text = "Rounded rectangle";
            this.btnDrawRoundRect.UseVisualStyleBackColor = true;
            this.btnDrawRoundRect.Click += new System.EventHandler(this.btnDrawRoundRect_Click);
            // 
            // pnlControls
            // 
            this.pnlControls.Controls.Add(this.sbZoom);
            this.pnlControls.Controls.Add(this.lblZoomFactor);
            this.pnlControls.Controls.Add(this.label1);
            this.pnlControls.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlControls.Location = new System.Drawing.Point(147, 0);
            this.pnlControls.Name = "pnlControls";
            this.pnlControls.Size = new System.Drawing.Size(606, 49);
            this.pnlControls.TabIndex = 4;
            // 
            // sbZoom
            // 
            this.sbZoom.Location = new System.Drawing.Point(43, 13);
            this.sbZoom.Minimum = 5;
            this.sbZoom.Name = "sbZoom";
            this.sbZoom.Size = new System.Drawing.Size(192, 17);
            this.sbZoom.TabIndex = 5;
            this.sbZoom.Value = 10;
            this.sbZoom.Scroll += new System.Windows.Forms.ScrollEventHandler(this.sbZoom_Scroll);
            // 
            // lblZoomFactor
            // 
            this.lblZoomFactor.AutoSize = true;
            this.lblZoomFactor.Location = new System.Drawing.Point(247, 17);
            this.lblZoomFactor.Name = "lblZoomFactor";
            this.lblZoomFactor.Size = new System.Drawing.Size(33, 13);
            this.lblZoomFactor.TabIndex = 0;
            this.lblZoomFactor.Text = "100%";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 17);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(34, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "Zoom";
            // 
            // pbView
            // 
            this.pbView.AutoScroll = true;
            this.pbView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pbView.Location = new System.Drawing.Point(147, 49);
            this.pbView.Name = "pbView";
            this.pbView.Size = new System.Drawing.Size(606, 527);
            this.pbView.TabIndex = 5;
            // 
            // fmPrimitiveRendering
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(753, 576);
            this.Controls.Add(this.pbView);
            this.Controls.Add(this.pnlControls);
            this.Controls.Add(this.pnlNav);
            this.Name = "fmPrimitiveRendering";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Drawing with Drawer";
            this.Load += new System.EventHandler(this.fmDrawingWithDrawer_Load);
            this.pnlNav.ResumeLayout(false);
            this.pnlControls.ResumeLayout(false);
            this.pnlControls.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnDrawRectangle;
        private System.Windows.Forms.Button btnDrawEllipse;
        private System.Windows.Forms.Button btnDrawPolygon;
        private System.Windows.Forms.Button btnDrawPath;
        private System.Windows.Forms.Panel pnlNav;
        private System.Windows.Forms.ListBox lstTests;
        private System.Windows.Forms.Button btnDrawTest;
        private System.Windows.Forms.Panel pnlControls;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.HScrollBar sbZoom;
        private System.Windows.Forms.Label lblZoomFactor;
        private Cross.Helpers.ScrollablePictureBox pbView;
        private System.Windows.Forms.Button btnDrawRoundRect;
    }
}