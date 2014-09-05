using System;
using System.Windows.Forms;
using Cross.Drawing;
using Cross.Helpers;
using Demo.Helpers;

namespace DemoWinForm
{
    public partial class fmGammaCorrection : Form
    {
        double gammaFactorRed = 1.2;
        double gammaFactorGreen = 1.2;
        double gammaFactorBlue = 1.2;

        IDrawer drawer = null;
        PixelBuffer buffer = null;

        #region Initialize
        public fmGammaCorrection()
        {
            InitializeComponent();
        }

        private void fmGammaCorrection_Load(object sender, EventArgs e)
        {
            buffer = new PixelBuffer(400, 400);
            drawer = new Drawer(buffer);
            DrawLion();
        }
        #endregion

        #region Use Gamma
        private void chkUseGamma_CheckedChanged(object sender, EventArgs e)
        {
            DrawLion();
        }
        #endregion

        #region Gamma Factor Scroll
        private void sbFactor_Scroll(object sender, ScrollEventArgs e)
        {
            gammaFactorRed = (double)sbFactorRed.Value / 10.0;
            lblGammaFactorRed.Text = string.Format("{0}", gammaFactorRed);
            DrawLion();
        }

        private void hScrollBar2_Scroll(object sender, ScrollEventArgs e)
        {
            gammaFactorBlue = (double)sbFactorBlue.Value / 10.0;
            lblGammaFactorBlue.Text = string.Format("{0}", gammaFactorBlue);
            DrawLion();

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void sbFactorGreen_Scroll(object sender, ScrollEventArgs e)
        {
            gammaFactorGreen = (double)sbFactorGreen.Value / 10.0;
            lblGammaFactorGreen.Text = string.Format("{0}", gammaFactorGreen);
            DrawLion();
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

        #region Draw Lion
        void DrawLion()
        {
            //set gamma settings
            drawer.GammaCorrected = chkUseGamma.Checked;
            drawer.SetGamma(gammaFactorRed, gammaFactorGreen, gammaFactorBlue);

            //clear background
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
        #endregion
    }
}
