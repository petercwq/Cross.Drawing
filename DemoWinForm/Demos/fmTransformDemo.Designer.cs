namespace DemoWinForm
{
    partial class fmTransformDemo
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
            this.pnlNav = new System.Windows.Forms.Panel();
            this.btnPushPop = new System.Windows.Forms.Button();
            this.btnSkew = new System.Windows.Forms.Button();
            this.btnScale = new System.Windows.Forms.Button();
            this.btnRotate = new System.Windows.Forms.Button();
            this.btnTranslate = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.pbOriginal = new Cross.Helpers.ScrollablePictureBox();
            this.label2 = new System.Windows.Forms.Label();
            this.pbAfter1 = new Cross.Helpers.ScrollablePictureBox();
            this.pbAfter2 = new Cross.Helpers.ScrollablePictureBox();
            this.pbAfter3 = new Cross.Helpers.ScrollablePictureBox();
            this.txtDesc1 = new System.Windows.Forms.TextBox();
            this.txtDesc2 = new System.Windows.Forms.TextBox();
            this.txtDesc3 = new System.Windows.Forms.TextBox();
            this.pnlNav.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlNav
            // 
            this.pnlNav.Controls.Add(this.btnPushPop);
            this.pnlNav.Controls.Add(this.btnSkew);
            this.pnlNav.Controls.Add(this.btnScale);
            this.pnlNav.Controls.Add(this.btnRotate);
            this.pnlNav.Controls.Add(this.btnTranslate);
            this.pnlNav.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlNav.Location = new System.Drawing.Point(0, 0);
            this.pnlNav.Name = "pnlNav";
            this.pnlNav.Size = new System.Drawing.Size(643, 56);
            this.pnlNav.TabIndex = 0;
            // 
            // btnPushPop
            // 
            this.btnPushPop.Location = new System.Drawing.Point(340, 12);
            this.btnPushPop.Name = "btnPushPop";
            this.btnPushPop.Size = new System.Drawing.Size(136, 23);
            this.btnPushPop.TabIndex = 0;
            this.btnPushPop.Text = "Push, Pop";
            this.btnPushPop.UseVisualStyleBackColor = true;
            this.btnPushPop.Click += new System.EventHandler(this.btnPushPop_Click);
            // 
            // btnSkew
            // 
            this.btnSkew.Location = new System.Drawing.Point(259, 13);
            this.btnSkew.Name = "btnSkew";
            this.btnSkew.Size = new System.Drawing.Size(75, 23);
            this.btnSkew.TabIndex = 0;
            this.btnSkew.Text = "Skew";
            this.btnSkew.UseVisualStyleBackColor = true;
            this.btnSkew.Click += new System.EventHandler(this.btnSkew_Click);
            // 
            // btnScale
            // 
            this.btnScale.Location = new System.Drawing.Point(178, 12);
            this.btnScale.Name = "btnScale";
            this.btnScale.Size = new System.Drawing.Size(75, 23);
            this.btnScale.TabIndex = 0;
            this.btnScale.Text = "Scale";
            this.btnScale.UseVisualStyleBackColor = true;
            this.btnScale.Click += new System.EventHandler(this.btnScale_Click);
            // 
            // btnRotate
            // 
            this.btnRotate.Location = new System.Drawing.Point(97, 12);
            this.btnRotate.Name = "btnRotate";
            this.btnRotate.Size = new System.Drawing.Size(75, 23);
            this.btnRotate.TabIndex = 0;
            this.btnRotate.Text = "Rotate";
            this.btnRotate.UseVisualStyleBackColor = true;
            this.btnRotate.Click += new System.EventHandler(this.btnRotate_Click);
            // 
            // btnTranslate
            // 
            this.btnTranslate.Location = new System.Drawing.Point(16, 13);
            this.btnTranslate.Name = "btnTranslate";
            this.btnTranslate.Size = new System.Drawing.Size(75, 23);
            this.btnTranslate.TabIndex = 0;
            this.btnTranslate.Text = "Translate";
            this.btnTranslate.UseVisualStyleBackColor = true;
            this.btnTranslate.Click += new System.EventHandler(this.btnTranslate_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 78);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(42, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Original";
            // 
            // pbOriginal
            // 
            this.pbOriginal.AutoScroll = true;
            this.pbOriginal.BackColor = System.Drawing.Color.White;
            this.pbOriginal.Location = new System.Drawing.Point(16, 94);
            this.pbOriginal.Name = "pbOriginal";
            this.pbOriginal.Size = new System.Drawing.Size(200, 200);
            this.pbOriginal.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 307);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(98, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "After transformation";
            // 
            // pbAfter1
            // 
            this.pbAfter1.AutoScroll = true;
            this.pbAfter1.BackColor = System.Drawing.Color.White;
            this.pbAfter1.Location = new System.Drawing.Point(16, 323);
            this.pbAfter1.Name = "pbAfter1";
            this.pbAfter1.Size = new System.Drawing.Size(200, 200);
            this.pbAfter1.TabIndex = 2;
            // 
            // pbAfter2
            // 
            this.pbAfter2.AutoScroll = true;
            this.pbAfter2.BackColor = System.Drawing.Color.White;
            this.pbAfter2.Location = new System.Drawing.Point(222, 323);
            this.pbAfter2.Name = "pbAfter2";
            this.pbAfter2.Size = new System.Drawing.Size(200, 200);
            this.pbAfter2.TabIndex = 2;
            // 
            // pbAfter3
            // 
            this.pbAfter3.AutoScroll = true;
            this.pbAfter3.BackColor = System.Drawing.Color.White;
            this.pbAfter3.Location = new System.Drawing.Point(428, 323);
            this.pbAfter3.Name = "pbAfter3";
            this.pbAfter3.Size = new System.Drawing.Size(200, 200);
            this.pbAfter3.TabIndex = 2;
            // 
            // txtDesc1
            // 
            this.txtDesc1.Location = new System.Drawing.Point(16, 530);
            this.txtDesc1.Multiline = true;
            this.txtDesc1.Name = "txtDesc1";
            this.txtDesc1.Size = new System.Drawing.Size(200, 85);
            this.txtDesc1.TabIndex = 3;
            // 
            // txtDesc2
            // 
            this.txtDesc2.Location = new System.Drawing.Point(222, 530);
            this.txtDesc2.Multiline = true;
            this.txtDesc2.Name = "txtDesc2";
            this.txtDesc2.Size = new System.Drawing.Size(200, 85);
            this.txtDesc2.TabIndex = 3;
            // 
            // txtDesc3
            // 
            this.txtDesc3.Location = new System.Drawing.Point(428, 529);
            this.txtDesc3.Multiline = true;
            this.txtDesc3.Name = "txtDesc3";
            this.txtDesc3.Size = new System.Drawing.Size(200, 85);
            this.txtDesc3.TabIndex = 3;
            // 
            // fmTransformDemo
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(643, 627);
            this.Controls.Add(this.txtDesc3);
            this.Controls.Add(this.txtDesc2);
            this.Controls.Add(this.txtDesc1);
            this.Controls.Add(this.pbAfter3);
            this.Controls.Add(this.pbAfter2);
            this.Controls.Add(this.pbAfter1);
            this.Controls.Add(this.pbOriginal);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pnlNav);
            this.Name = "fmTransformDemo";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Transformation demo";
            this.Load += new System.EventHandler(this.fmTransformDemo_Load);
            this.pnlNav.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel pnlNav;
        private System.Windows.Forms.Label label1;
        private Cross.Helpers.ScrollablePictureBox pbOriginal;
        private System.Windows.Forms.Label label2;
        private Cross.Helpers.ScrollablePictureBox pbAfter1;
        private Cross.Helpers.ScrollablePictureBox pbAfter2;
        private Cross.Helpers.ScrollablePictureBox pbAfter3;
        private System.Windows.Forms.TextBox txtDesc1;
        private System.Windows.Forms.TextBox txtDesc2;
        private System.Windows.Forms.TextBox txtDesc3;
        private System.Windows.Forms.Button btnSkew;
        private System.Windows.Forms.Button btnScale;
        private System.Windows.Forms.Button btnRotate;
        private System.Windows.Forms.Button btnTranslate;
        private System.Windows.Forms.Button btnPushPop;
    }
}