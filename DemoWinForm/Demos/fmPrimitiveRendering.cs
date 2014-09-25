using System;
using Cross.Drawing;
using Cross.Helpers;
using Demo.Helpers;

namespace DemoWinForm
{
    public partial class fmPrimitiveRendering : System.Windows.Forms.Form
    {
        #region Initialize
        public fmPrimitiveRendering()
        {
            InitializeComponent();
        }

        private void fmDrawingWithDrawer_Load(object sender, EventArgs e)
        {

        }
        #endregion

        #region Display Buffer
        System.Drawing.Bitmap bmp = null;

        /// <summary>
        /// Helper method to display result from a pixel buffer
        /// </summary>
        void DisplayBuffer(PixelsBuffer buffer)
        {
            if (bmp != null) bmp.Dispose();
            bmp = Cross.Helpers.BufferToBitmap.GetBitmap(buffer, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            pbView.Image = bmp;
        }
        #endregion

        #region Draw Rectangle
        private void btnDrawRectangle_Click(object sender, EventArgs e)
        {
            //create a new drawing context
            PixelsBuffer buffer = new PixelsBuffer(400, 400);
            IDrawer drawer = new Drawer(buffer);
            drawer.Clear(Colors.White);

            //create fill for drawing
            Fill fill = Fills.YellowGreen;

            //draw content
            drawer.DrawRectangle(fill, 50, 50, 300, 200);

            //show to screen
            DisplayBuffer(buffer);
        }
        #endregion

        #region Draw Rounded Rectangle
        private void btnDrawRoundRect_Click(object sender, EventArgs e)
        {
            //create a new drawing context
            PixelsBuffer buffer = new PixelsBuffer(400, 400);
            IDrawer drawer = new Drawer(buffer);
            drawer.Clear(Colors.White);

            //create fill for drawing
            Fill fill = Fills.DarkOrange;

            //draw content
            drawer.DrawRoundedRectangle(fill, 50, 50, 300, 200, 15, 15);

            //show to screen
            DisplayBuffer(buffer);
        }
        #endregion

        #region Draw Ellipse
        private void btnDrawEllipse_Click(object sender, EventArgs e)
        {
            //create a new drawing context
            PixelsBuffer buffer = new PixelsBuffer(400, 400);
            IDrawer drawer = new Drawer(buffer);
            drawer.Clear(Colors.White);

            //create fill for drawing
            Fill fill = Fills.Yellow;

            //draw content
            drawer.DrawEllipse(fill, 200, 200, 180, 100);

            //show to screen
            DisplayBuffer(buffer);
        }
        #endregion

        #region Draw Polygon
        private void btnDrawPolygon_Click(object sender, EventArgs e)
        {
            //create a new drawing context
            PixelsBuffer buffer = new PixelsBuffer(400, 400);
            IDrawer drawer = new Drawer(buffer);
            drawer.Clear(Colors.White);

            //create fill for drawing
            Fill fill = Fills.MistyRose;

            //populate polygon coordinate data
            double[] coordinates = new double[]
            {
                30, 300,
                150, 40,
                300, 260,
                130, 200,
                //30, 300 //the last point is omitted to show that the end point is important
                          //for rendering stroke
            };

            //draw content
            drawer.DrawPolygon(fill, coordinates);

            //show to screen
            DisplayBuffer(buffer);
        }
        #endregion

        #region Draw Path
        private void btnDrawPath_Click(object sender, EventArgs e)
        {
            //create a new drawing context
            PixelsBuffer buffer = new PixelsBuffer(400, 400);
            IDrawer drawer = new Drawer(buffer);
            drawer.Clear(Colors.White);

            //create fill for drawing
            Fill fill = Fills.MistyRose;

            //create path
            DrawingPath path = new DrawingPath();
            path.MoveTo(200, 100);
            path.CurveTo(200, 350, 340, 30, 360, 200);
            path.CurveTo(200, 100, 40, 200, 60, 30);

            //draw content
            drawer.Rotate(15);
            drawer.Scale(0.3, 0.3);
            drawer.DrawPath(fill, path);

            //show to screen
            DisplayBuffer(buffer);
        }
        #endregion

        #region Draw Test
        private void btnDrawTest_Click(object sender, EventArgs e)
        {
            if (lstTests.SelectedIndex < 0) lstTests.SelectedIndex = 0;

            //create a new drawing context
            PixelsBuffer buffer = new PixelsBuffer(600, 600);
            IDrawer drawer = new Drawer(buffer);
            drawer.Clear(Colors.White);

            //create fill for drawing
            Fill fill = Fills.Black;

            //populate polygon coordinate data
            double[] coordinates = null;
            switch (lstTests.SelectedIndex)
            {
                case 0:
                    coordinates = TestFactory.Triangle();
                    TestFactory.Scale(coordinates, 5.0);
                    break;

                case 1:
                    coordinates = TestFactory.Star();
                    TestFactory.Scale(coordinates, 5.0);
                    break;

                case 2:
                    coordinates = TestFactory.Crown();
                    TestFactory.Scale(coordinates, 5.0);
                    break;

                case 3:
                    coordinates = TestFactory.CirclePattern1(buffer.Width, buffer.Height);
                    break;

                case 4:
                    coordinates = TestFactory.CirclePattern2(buffer.Width, buffer.Height);
                    break;

                case 5:
                    coordinates = TestFactory.Complex1();
                    break;

                case 6:
                    coordinates = TestFactory.Complex2();
                    break;

                case 7:
                    coordinates = TestFactory.Complex3();
                    break;

                case 8:
                    DrawLion();
                    break;
            }


            if (coordinates != null)
            {
                //draw content
                drawer.DrawPolygon(fill, coordinates);

                //show to screen
                DisplayBuffer(buffer);
            }
        }

        void DrawLion()
        {
            //create a new drawing context
            PixelsBuffer buffer = new PixelsBuffer(400, 400);
            IDrawer drawer = new Drawer(buffer);
            drawer.Clear(Colors.White);

            //get coordinates and colors
            double[][] polygons = LionPathHelper.GetLionPolygons();
            Color[] colors = LionPathHelper.GetLionColors();

            //iterate all polygons and draw them
            double[] coordinates = null;
            for (int i = 0; i < polygons.Length; i++)
            {
                coordinates = polygons[i];
                Fill fill = new Fill(colors[i]);
                drawer.DrawPolygon(fill, coordinates);
            }

            //show to screen
            DisplayBuffer(buffer);
        }

        private void lstTests_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnDrawTest_Click(this, EventArgs.Empty);
        }
        #endregion

        #region Zoom
        private void sbZoom_Scroll(object sender, System.Windows.Forms.ScrollEventArgs e)
        {
            double zoom = (double)sbZoom.Value / 10.0;
            lblZoomFactor.Text = string.Format("{0:#,##}%", zoom * 100);
            pbView.Zoom = zoom;
        }
        #endregion
    }
}
