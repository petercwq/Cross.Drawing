#region Using directives
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
#endregion

namespace DemoHelpers
{
    /// <summary>
    /// An extended panel to mimic scrollable picture box
    /// </summary>
    [ToolboxItem(true)]
    public class ScrollablePictureBox : Panel
    {
        public ImageBox Viewer = null;

        #region Image
        private Image mImage;
        /// <summary>
        /// Gets/Sets Image
        /// </summary>
        [Browsable(false), DefaultValue(null)]
        public Image Image
        {
            get { return mImage; }
            set
            {
                mImage = value;
                UpdateViewer();
                Invalidate();
            }
        }
        #endregion

        #region High Quality
        private bool mHighQuality;
        /// <summary>
        /// Gets/Sets whether the image drawing routine use best interpolation mode when stretched
        /// </summary>
        [DefaultValue(false), Browsable(true), Description("When true, the image zooming use best interpolation mode")]
        public bool HighQuality
        {
            get { return mHighQuality; }
            set { mHighQuality = value; }
        }
        #endregion

        #region Zoom
        private double mZoom = 1.0;
        /// <summary>
        /// Gets/Sets zoom factor
        /// </summary>
        [Browsable(true), DefaultValue(1.0), Description("Scaling factor")]
        public double Zoom
        {
            get { return mZoom; }
            set
            {
                if (mZoom != value)
                {
                    mZoom = value;
                    if (mZoom < 0.01) mZoom = 0.01;
                    UpdateViewer();
                }
            }
        }
        #endregion

        #region Draw Checker
        private bool mDrawChecker;
        /// <summary>
        /// Gets/Sets whether a checker-board pattern is drawn as background
        /// </summary>
        [Browsable(true), DefaultValue(false), Category("Visual"), Description("Draw a checker-board pattern as background")]
        public bool DrawChecker
        {
            get { return mDrawChecker; }
            set
            {
                if (mDrawChecker != value)
                {
                    mDrawChecker = value;
                    UpdateViewer();
                }
            }
        }
        #endregion

        #region On Paint
        /// <summary>
        /// 
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            //base.OnPaint(e);
            Graphics g = e.Graphics;
            g.Clear(BackColor);
        }
        #endregion

        #region On Paint Background
        /// <summary>
        /// 
        /// </summary>
        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            //deliberately do nothing
        }
        #endregion

        #region Update Viewer
        void UpdateViewer()
        {
            if (!DesignMode)
            {
                Viewer.HighQuality = mHighQuality;
                Viewer.DrawChecker = mDrawChecker;

                if (mImage == null) Viewer.Image = null;
                else
                {
                    if (mZoom == 1.0)
                    {//no zooming
                        Viewer.Stretched = false;
                        Viewer.Size = mImage.Size;
                        Viewer.Image = mImage;
                    }
                    else
                    {
                        int w = (int)(mImage.Width * mZoom);
                        int h = (int)(mImage.Height * mZoom);
                        if (w < 1) w = 1;
                        if (h < 1) h = 1;
                        Viewer.Stretched = true;
                        Viewer.Size = new Size(w, h);
                        Viewer.Image = mImage;
                    }
                }
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor
        /// </summary>
        public ScrollablePictureBox()
        {
            if (Viewer == null)
            {
                Viewer = new ImageBox();
                Viewer.Location = new Point(0, 0);
                Viewer.Parent = this;
            }
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            this.AutoScroll = true;
        }
        #endregion
    }

    /// <summary>
    /// A custom picture box that is flicker-free
    /// </summary>
    [ToolboxItem(false)]
    public class ImageBox : Control
    {
        #region Stretched
        private bool mStretched;
        /// <summary>
        /// Gets/Sets whether the image is stretched when drawn to canvas
        /// </summary>
        [DefaultValue(false)]
        public bool Stretched
        {
            get { return mStretched; }
            set { mStretched = value; }
        }
        #endregion

        #region High Quality
        private bool mHighQuality;
        /// <summary>
        /// Gets/Sets whether the image drawing routine use best interpolation mode when stretched
        /// </summary>
        [DefaultValue(false)]
        public bool HighQuality
        {
            get { return mHighQuality; }
            set { mHighQuality = value; }
        }
        #endregion

        #region Image
        private Image mImage;
        /// <summary>
        /// Gets/Sets Image
        /// </summary>
        [Browsable(false), DefaultValue(null)]
        public Image Image
        {
            get { return mImage; }
            set
            {
                mImage = value;
                Invalidate();
            }
        }
        #endregion

        #region Draw Checker
        private bool mDrawChecker;
        /// <summary>
        /// Gets/Sets whether a checker-board pattern is drawn as background
        /// </summary>
        [Browsable(true), DefaultValue(false), Category("Visual"), Description("Draw a checker-board pattern as background")]
        public bool DrawChecker
        {
            get { return mDrawChecker; }
            set
            {
                if (mDrawChecker != value)
                {
                    mDrawChecker = value;
                    Invalidate();
                }
            }
        }
        #endregion

        #region On Paint
        /// <summary>
        /// 
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            //base.OnPaint(e);
            Graphics g = e.Graphics;
            if (mDrawChecker) PaintChecker(g);
            else g.Clear(BackColor);

            if (mImage != null)
            {
                if (mStretched)
                {
                    if (mHighQuality)
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    }
                    else
                    {
                        g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    }
                    g.DrawImage(mImage, this.ClientRectangle);
                }
                else g.DrawImageUnscaled(mImage, 0, 0);
            }
        }
        #endregion

        #region On Paint Background
        /// <summary>
        /// 
        /// </summary>
        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            //deliberately do nothing
        }
        #endregion

        #region Paint Checker
        void PaintChecker(Graphics g)
        {
            int w = this.Width;
            int h = this.Height;
            int cw = 20;
            int ch = 20;
            Brush bFirst = Brushes.Silver;
            Brush bSecond = Brushes.Gray;
            int x = 0;
            int y = 0;

            for (int row = 0; row < h / ch; row++)
            {
                x = 0;
                for (int col = 0; col < w / ch; col++)
                {
                    if (col % 2 == 0) g.FillRectangle(bSecond, x, y, cw, ch);
                    else g.FillRectangle(bFirst, x, y, cw, ch);

                    x += cw;
                }

                //prepare for next iteration
                y += ch;
                Brush tmp = bFirst;
                bFirst = bSecond;
                bSecond = tmp;
            }
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Create new instance
        /// </summary>
        public ImageBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        }
        #endregion
    }
}
