using System;
using System.Windows.Forms;
using Cross.Drawing;
using Demo.Helpers;

namespace DemoWinForm
{
    public partial class fmFill : Form
    {
        /// <summary>
        /// width of viewing region
        /// </summary>
        int w = 600;
        /// <summary>
        /// height of viewing region
        /// </summary>
        int h = 500;
        PixelBuffer buffer = null;
        IDrawer drawer = null;
        double[] coordinates = null;
        FillType fillType = FillType.Uniform;
        GradientStyle gradientStyle = GradientStyle.Reflect;
        //ColorRamp ramp = null;

        #region Initialize
        public fmFill()
        {
            InitializeComponent();
            lstFills.SelectedIndex = 0;
            lstGradientStyle.SelectedIndex = 0;
            lstShapes.SelectedIndex = 0;
        }

        private void fmFill_Load(object sender, EventArgs e)
        {
            if (!DesignMode)
            {
                buffer = new PixelBuffer(w, h);
                drawer = new Drawer(buffer);
                Draw();
            }
        }
        #endregion

        #region Draw
        private void Draw()
        {
            //clear background
            drawer.Clear(Colors.White);

            //draw test
            coordinates = GetPath();
            Fill fill = GetFill();
            drawer.DrawPolygon(fill, coordinates);

            //show to form
            DisplayBuffer(buffer);
        }
        #endregion

        #region Get Fills

        #region Get Fill
        /// <summary>
        /// Gets the appropriate fill based on current selection
        /// </summary>
        Fill GetFill()
        {
            Fill result = null;

            switch (fillType)
            {
                case FillType.Uniform:
                    result = GetUniformFill();
                    break;

                case FillType.RadialCircle:
                    result = GetRadialCircleFill();
                    break;

                case FillType.RadialEllipse:
                    result = GetRadialEllipseFill();
                    break;

                case FillType.RadialFocal:
                    result = GetRadialFocalFill();
                    break;

                case FillType.LinearVertical:
                    result = GetLinearVerticalFill();
                    break;

                case FillType.LinearHorizontal:
                    result = GetLinearHorizontalFill();
                    break;

                case FillType.LinearForward:
                    result = GetLinearForwardFill();
                    break;

                case FillType.LinearBackward:
                    result = GetLinearBackwardFill();
                    break;
            }

            return result;
        }
        #endregion

        #region Get Uniform Fill
        Fill GetUniformFill()
        {
            Fill result = null;

            //there are several ways to create a solid fills.
            //The following methods give the same result

            //method 1
            result = Fills.Red;

            //method 2
            result = new Fill(Colors.Red);

            //method 3
            ColorPaint paint = new ColorPaint(Colors.Red);
            result = new Fill(paint);

            return result;
        }
        #endregion

        #region Get Radial Circle Fill
        Fill GetRadialCircleFill()
        {
            //Create a radial gradient paint with
            // + Center point and Focal point is at the center of viewing region
            // + Radius X = Radius Y
            RadialGradient paint = new RadialGradient();
            paint.CenterX = w / 2;
            paint.CenterY = h / 2;
            paint.FocusX = w / 2;
            paint.FocusY = h / 2;
            paint.Radius = Math.Min(w, h) / 3;
            paint.Style = gradientStyle;
            paint.Ramp = GetColorRamp();

            //create fill and return result
            Fill result = new Fill(paint);
            return result;
        }
        #endregion

        #region Get Radial Ellipse Fill
        Fill GetRadialEllipseFill()
        {
            //Create a radial gradient paint with
            // + Center point and Focal point is at the center of viewing region
            // + Different radius X, radius Y
            RadialGradient paint = new RadialGradient();
            paint.CenterX = w / 2;
            paint.CenterY = h / 2;
            paint.FocusX = w / 2;
            paint.FocusY = h / 2;
            paint.RadiusX = w / 2;
            paint.RadiusY = h / 3;
            paint.Style = gradientStyle;
            paint.Ramp = GetColorRamp();

            //create fill and return result
            Fill result = new Fill(paint);
            return result;
        }
        #endregion

        #region Get Radial Focal Fill
        Fill GetRadialFocalFill()
        {
            //Create a radial gradient paint with
            // + Center point is at the center of viewing region
            // + Different radius X, radius Y
            // + Focal point is near the top-left of viewing region
            RadialGradient paint = new RadialGradient();
            paint.CenterX = w / 2;
            paint.CenterY = h / 2;
            paint.FocusX = w / 4;
            paint.FocusY = h / 2.5;
            paint.RadiusX = w / 3;
            paint.RadiusY = h / 3;
            paint.Style = gradientStyle;
            paint.Ramp = GetColorRamp();

            //create fill and return result
            Fill result = new Fill(paint);
            return result;
        }
        #endregion

        #region Get Linear Vertical Fill
        Fill GetLinearVerticalFill()
        {
            //Create a linear gradient paint with
            // + mode = vertical
            // + fill from top to 1/2 of height (middle)
            LinearGradient paint = new LinearGradient();
            paint.StartY = 0;
            paint.EndY = h / 2;
            paint.Ramp = GetColorRamp();
            paint.Mode = LinearGradientMode.Vertical;
            paint.Style = gradientStyle;

            Fill result = new Fill(paint);
            return result;
        }
        #endregion

        #region Get Linear Horizontal Fill
        Fill GetLinearHorizontalFill()
        {
            //Create a linear gradient paint with
            // + mode = horizontal
            // + fill from left to 1/3 of width
            LinearGradient paint = new LinearGradient();
            paint.StartX = 0;
            paint.EndX = w / 3;
            paint.Ramp = GetColorRamp();
            paint.Mode = LinearGradientMode.Horizontal;
            paint.Style = gradientStyle;

            Fill result = new Fill(paint);
            return result;
        }
        #endregion

        #region Get Linear Forward Fill
        Fill GetLinearForwardFill()
        {
            //Create a linear gradient paint with
            // + mode = forward diagonal
            // + start point: (0,0)
            // + end point: (w/2, h/2)

            LinearGradient paint = new LinearGradient();
            paint.StartX = 0;
            paint.StartY = 0;
            paint.EndX = w / 2;
            paint.EndY = h / 2;
            paint.Ramp = GetColorRamp();
            paint.Mode = LinearGradientMode.ForwardDiagonal;
            paint.Style = gradientStyle;

            Fill result = new Fill(paint);
            return result;
        }
        #endregion

        #region Get Linear Backward Fill
        Fill GetLinearBackwardFill()
        {
            //Create a linear gradient paint with
            // + mode = forward diagonal
            // + start point: (0,0)
            // + end point: (w/2, h/2)

            LinearGradient paint = new LinearGradient();
            paint.StartX = 0;
            paint.StartY = 0;
            paint.EndX = w / 2;
            paint.EndY = h / 2;
            paint.Ramp = GetColorRamp();
            paint.Mode = LinearGradientMode.BackwardDiagonal;
            paint.Style = gradientStyle;

            Fill result = new Fill(paint);
            return result;
        }
        #endregion

        #endregion

        #region Get Path
        /// <summary>
        /// Gets data of the polygon to draw
        /// </summary>
        double[] GetPath()
        {
            double[] result = null;

            switch (lstShapes.SelectedIndex)
            {
                case 0://rectangle
                    result = new double[] { 0, 0, w, 0, w, h, 0, h, 0, 0 };
                    break;

                case 1://triangle
                    result = TestFactory.Triangle();
                    result = TestFactory.Scale(result, 10);
                    break;

                case 2://star
                    result = TestFactory.Star();
                    result = TestFactory.Scale(result, 12);
                    result = TestFactory.Offset(result, 0, -100);
                    break;

                case 3://crown
                    result = TestFactory.Crown();
                    result = TestFactory.Scale(result, 10);
                    result = TestFactory.Offset(result, 0, -70);
                    break;

                case 4://circle pattern 1
                    result = TestFactory.CirclePattern1(w, h);
                    break;

                case 5://circle pattern 2
                    result = TestFactory.CirclePattern2(w, h);
                    break;

                case 6://complex 1
                    result = TestFactory.Complex1();
                    result = TestFactory.Scale(result, 1.3);
                    result = TestFactory.Offset(result, 30, 150);
                    break;

                case 7://complex 2
                    result = TestFactory.Complex2();
                    result = TestFactory.Scale(result, 8);
                    result = TestFactory.Offset(result, -60, -450);
                    break;

                case 8://complex 3
                    result = TestFactory.Complex3();
                    //result = TestFactory.Scale(result, 1);
                    result = TestFactory.Offset(result, 0, -50);
                    break;
            }

            return result;
        }
        #endregion

        #region Get Color Ramp
        ColorRamp GetColorRamp()
        {
            ColorRamp result = new ColorRamp();

            //Red - Green - Blue
            //result.Add(Colors.Red  , 0.0);
            //result.Add(Colors.Green, 0.5);
            //result.Add(Colors.Blue , 1.0);

            //White - Red - Green - Blue
            result.Add(Colors.White, 0.0);
            result.Add(Colors.Red, 1.0 / 3.0);
            result.Add(Colors.Green, 2.0 / 3.0);
            result.Add(Colors.Blue, 1.0);

            return result;
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

        #region UI Events
        private void lstFills_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (lstFills.SelectedIndex)
            {
                case 0:
                    fillType = FillType.Uniform;
                    break;

                case 1:
                    fillType = FillType.RadialCircle;
                    break;

                case 2:
                    fillType = FillType.RadialEllipse;
                    break;

                case 3:
                    fillType = FillType.RadialFocal;
                    break;

                case 4:
                    fillType = FillType.LinearVertical;
                    break;

                case 5:
                    fillType = FillType.LinearHorizontal;
                    break;

                case 6:
                    fillType = FillType.LinearForward;
                    break;

                case 7:
                    fillType = FillType.LinearBackward;
                    break;
            }
            if (drawer != null) Draw();
        }

        private void lstGradientStyle_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (lstGradientStyle.SelectedIndex)
            {
                case 0:
                    gradientStyle = GradientStyle.Reflect;
                    break;

                case 1:
                    gradientStyle = GradientStyle.Repeat;
                    break;

                case 2:
                    gradientStyle = GradientStyle.Pad;
                    break;
            }
            if (drawer != null) Draw();
        }

        private void lstShapes_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (drawer != null) Draw();
        }
        #endregion
    }

    enum FillType
    {
        Uniform,
        RadialCircle,
        RadialEllipse,
        RadialFocal,
        LinearHorizontal,
        LinearVertical,
        LinearForward,
        LinearBackward
    }
}
