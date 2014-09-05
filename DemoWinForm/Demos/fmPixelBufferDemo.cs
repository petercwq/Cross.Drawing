using System;
using System.Windows.Forms;
using Cross.Drawing;
using Cross.Helpers;

namespace DemoWinForm
{
    public partial class fmPixelBufferDemo : Form
    {
        int width = 400;
        int height = 400;

        #region Initialize
        public fmPixelBufferDemo()
        {
            InitializeComponent();
        }

        private void fmPixelBufferDemo_Load(object sender, EventArgs e)
        {

        }
        #endregion

        #region Pixel Drawing

        #region Draw Pixels
        private void btnDrawPixels_Click(object sender, EventArgs e)
        {
            //create buffer
            PixelBuffer buffer = new PixelBuffer(width, height);

            //render to buffer
            DrawLine(buffer, Colors.Red);
            DrawFrame(buffer, Colors.Red);

            //show to screen
            DisplayBuffer(buffer);

            #region Description
            string msg = "Create new buffer, then manually set pixels to buffer to render a rectangular frame around the buffer and a diagonal line from point (0,0) to point (width, height)";

            txtDescription.Text = msg;
            #endregion
        }
        #endregion

        #region Inverse Draw
        private void btnInverseDraw_Click(object sender, EventArgs e)
        {
            //intentionally make the stride negative so that
            //the coordinate system is flipped from top-down to bottom-up
            int stride = -height;

            //create buffer
            PixelBuffer buffer = new PixelBuffer(width, height, stride);

            //render to buffer
            DrawLine(buffer, Colors.Blue);
            DrawFrame(buffer, Colors.Blue);

            //show to screen
            DisplayBuffer(buffer);

            #region Description
            string msg = "Exactly similar to Draw Pixel, except that the y-axis coordinate system is inversed (top-down -> bottom-up)";

            txtDescription.Text = msg;
            #endregion
        }
        #endregion

        #region Buffer In Buffer
        private void btnBufferInBuffer_Click(object sender, EventArgs e)
        {
            //create main buffer
            PixelBuffer buffer = new PixelBuffer(width, height);

            //create a sub view (100 pixels margin)
            int margin = 100;

            #region Approach 1
            //calculate new view's parameter by hand
            //then attach the new view to main buffer

            /*
            int viewStride = -buffer.Stride; //inverse coordinate system of view
            int viewOffset = buffer.GetPixelIndex(margin, margin);
            PixelBuffer view = new PixelBuffer();
            view.Attach(buffer, width - margin * 2, height - margin * 2, viewStride, viewOffset);
            */
            #endregion

            #region Approach 2
            //a much easier way to create sub-view

            PixelBuffer view = buffer.CreateView(margin, margin, width - margin * 2, height - margin * 2, true);
            #endregion

            //render to main buffer
            DrawLine(buffer, Colors.Red);
            DrawFrame(buffer, Colors.Red);

            //render to sub view
            DrawLine(view, Colors.Blue);
            DrawFrame(view, Colors.Blue);

            //show to screen
            DisplayBuffer(buffer);

            #region Description
            string msg = "This example creates a main buffer, render pixels to it. Then, create a sub-view with inversed y-axis, and render to this view.";

            txtDescription.Text = msg;
            #endregion
        }
        #endregion

        #region Draw Line
        /// <summary>
        /// Draw a diagonal line with the specified color from (0,0) to (width, height)
        /// </summary>
        void DrawLine(PixelBuffer buffer, Color color)
        {
            int idx = 0; //pixel index

            for (int i = 0; i < buffer.Height; i++)
            {
                idx = buffer.GetPixelIndex(i, i);

                buffer.Data[idx] = color.Data;
            }
        }
        #endregion

        #region Draw Frame
        /// <summary>
        /// Draw a frame around the buffer from (0,0) to (width, height)
        /// </summary>
        void DrawFrame(PixelBuffer buffer, Color color)
        {
            int idx = 0; //pixel index

            //draw left, right lines
            for (int y = 0; y < buffer.Height; y++)
            {
                //left
                idx = buffer.GetPixelIndex(0, y);
                buffer.Data[idx] = color.Data;

                //right
                idx = buffer.GetPixelIndex(buffer.Width - 1, y);
                buffer.Data[idx] = color.Data;
            }

            //draw top, bottom lines
            for (int x = 0; x < buffer.Width; x++)
            {
                //top
                idx = buffer.GetPixelIndex(x, 0);
                buffer.Data[idx] = color.Data;

                //bottom
                idx = buffer.GetPixelIndex(x, buffer.Height - 1);
                buffer.Data[idx] = color.Data;
            }
        }
        #endregion

        #endregion

        #region Polygon drawing

        #region Draw Star
        private void btnDrawStar_Click(object sender, EventArgs e)
        {
            //create buffer
            PixelBuffer buffer = new PixelBuffer(width, height);

            //render
            DrawStar(buffer, Colors.Red, Colors.White);

            //show to screen
            DisplayBuffer(buffer);

            #region Description
            string msg = "Create new buffer, then draw a star";

            txtDescription.Text = msg;
            #endregion
        }
        #endregion

        #region Inverse Draw Star
        private void btnInverseDrawStar_Click(object sender, EventArgs e)
        {
            //intentionally make the stride negative so that
            //the coordinate system is flipped from top-down to bottom-up
            int stride = -height;

            //create buffer
            PixelBuffer buffer = new PixelBuffer(width, height, stride);

            //render
            DrawStar(buffer, Colors.Blue, Colors.White);

            //show to screen
            DisplayBuffer(buffer);

            #region Description
            string msg = "Similar to Draw Star example, except that the y-axis is inversed";

            txtDescription.Text = msg;
            #endregion
        }
        #endregion

        #region Buffer in Buffer Draw
        private void btnBufferInBufferDraw_Click(object sender, EventArgs e)
        {
            //create & render to main buffer
            PixelBuffer buffer = new PixelBuffer(width, height);
            DrawStar(buffer, Colors.Red, Colors.White);

            //create a sub view (50, 50, 100, 100) - inversed
            PixelBuffer view = buffer.CreateView(50, 250, 100, 100, true);
            DrawStar(view, Colors.Blue, Colors.YellowGreen);

            //create a sub view (50, 50, 100, 100)
            view = buffer.CreateView(250, 150, 100, 100);
            DrawStar(view, Colors.Goldenrod, Colors.LemonChiffon);

            //show to screen
            DisplayBuffer(buffer);

            #region Description
            string msg = "This example demonstrates that logical views with different coordinate systems can be attached to the same pixel buffer";

            txtDescription.Text = msg;
            #endregion
        }
        #endregion

        #region Draw Star
        /// <summary>
        /// Render a star shape to buffer
        /// </summary>
        void DrawStar(PixelBuffer buffer, Color color, Color background)
        {
            IDrawer drawer = new Drawer(buffer);
            drawer.Clear(background);

            Fill fill = new Fill(color);

            double[] coordinates = TestFactory.Star();
            TestFactory.Scale(coordinates, 2.0);

            //drawer.DrawRectangle(stroke, 0, 0, buffer.Width, buffer.Height);
            drawer.DrawPolygon(fill, coordinates);
        }
        #endregion

        #endregion

        #region Display Buffer
        System.Drawing.Bitmap bmp = null;

        /// <summary>
        /// Helper method to display result from a pixel buffer
        /// </summary>
        void DisplayBuffer(PixelBuffer buffer)
        {
            if (bmp != null) bmp.Dispose();
            bmp = Cross.Helpers.BufferToBitmap.GetBitmap(buffer, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            pbView.Image = bmp;
        }
        #endregion

        #region Zoom
        private void sbZoom_Scroll(object sender, ScrollEventArgs e)
        {
            double zoom = (double)sbZoom.Value / 10.0;
            lblZoomFactor.Text = string.Format("{0:#,##}%", zoom * 100);
            pbView.Zoom = zoom;
        }
        #endregion
    }
}
