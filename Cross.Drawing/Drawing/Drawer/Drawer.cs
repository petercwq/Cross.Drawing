using System;
using Cross.Drawing.Rasterizers.Analytical;

namespace Cross.Drawing
{
    /// <summary>
    /// Default implementation of <see cref="IDrawer"/>.
    /// Notes: property Buffer must be assigned before using this drawer
    /// </summary>    
    public class Drawer : IDrawer, IBufferDrawer
    {
        #region Fields
        IPolygonRasterizer mColorRasterizer = null;
        LinearGradientRasterizer mLinearGradientRasterizer = null;
        RadialGradientRasterizer mRadialGradientRasterizer = null;
        DrawingPathGenerator mPathGenerator = null;

        IPolygonRasterizer rasterizer = null;
        bool preparationRequired = true;
        bool transformRequired = false;
        bool gammaPreparationRequired = true;
        bool opacityPreparationRequired = true;
        Matrix3x3Stack matrixStack = null;

        /// <summary>
        /// Current transformation matrix.
        /// When null, no transformation is required
        /// </summary>
        Matrix3x3 currentTransform = new Matrix3x3();
        double[] transformedCoordinates = null;

        double clipX1;
        double clipY1;
        double clipX2;
        double clipY2;

        double gammaRed = 1.2;
        double gammaGreen = 1.2;
        double gammaBlue = 1.2;

        #endregion

        #region Buffer
        private PixelsBuffer mBuffer;
        /// <summary>
        /// Gets/Sets the buffer where results will be rendered to
        /// </summary>
        public PixelsBuffer Buffer
        {
            get { return mBuffer; }
            set
            {
                mBuffer = value;
                SetClip(0, 0, mBuffer.Width, mBuffer.Height);
                preparationRequired = true;
            }
        }
        #endregion

        #region Prepare
        /// <summary>
        /// Preparations before drawing
        /// </summary>
        void Prepare()
        {
            if (mBuffer == null) 
            NullArgumentException.Publish(typeof(PixelsBuffer), "Buffer");

            #region Initialize rasterizers

            if (mPathGenerator == null) mPathGenerator = new DrawingPathGenerator();

            if (mColorRasterizer == null) mColorRasterizer = new ColorRasterizer();
            mColorRasterizer.Buffer = mBuffer;

            if (mLinearGradientRasterizer == null) mLinearGradientRasterizer = new LinearGradientRasterizer();
            mLinearGradientRasterizer.Buffer = mBuffer;

            if (mRadialGradientRasterizer == null) mRadialGradientRasterizer = new RadialGradientRasterizer();
            mRadialGradientRasterizer.Buffer = mBuffer;

            preparationRequired = false;
            #endregion

            #region Prepare gamma correction
            if (gammaPreparationRequired)
            {
                if (mGammaCorrected)
                {
                    IGammaCorrector corrector = new PowerGammaCorrector(gammaRed, gammaGreen, gammaBlue);
                    mColorRasterizer.Gamma = corrector;
                    mLinearGradientRasterizer.Gamma = corrector;
                    mRadialGradientRasterizer.Gamma = corrector;
                }
                else
                {
                    mColorRasterizer.Gamma = null;
                    mLinearGradientRasterizer.Gamma = null;
                    mRadialGradientRasterizer.Gamma = null;
                }

                gammaPreparationRequired = false;
            }
            #endregion

            #region Prepare opacity masking
            if (opacityPreparationRequired)
            {
                mColorRasterizer.OpacityMask = mOpacityMask;
                mLinearGradientRasterizer.OpacityMask = mOpacityMask;
                mRadialGradientRasterizer.OpacityMask = mOpacityMask;

                opacityPreparationRequired = false;
            }
            #endregion

        }
        #endregion

        #region Get Rasterizer
        /// <summary>
        /// Gets the corresponding rasterizer based on the input paint
        /// </summary>
        IPolygonRasterizer GetRasterizer(Paint paint)
        {
            if (paint is ColorPaint) return mColorRasterizer;
            else if (paint is LinearGradient) return mLinearGradientRasterizer;
            else if (paint is RadialGradient) return mRadialGradientRasterizer;

            else UnsupportedException.Publish(paint.GetType());

            return null;
        }
        #endregion

        #region Transform Coordinates
        /// <summary>
        /// Transform the coordinates according to current transformation matrix
        /// </summary>
        double[] TransformCoordinates(double[] coordinates)
        {
            //allocate space
            if ((transformedCoordinates == null) || (coordinates.Length > transformedCoordinates.Length))
            {
                transformedCoordinates = new double[coordinates.Length];
            }

            //transform
            Matrix3x3 m = currentTransform;
            double sx = m.Sx;
            double sy = m.Sy;
            double tx = m.Tx;
            double shx = m.Shx;
            double shy = m.Shy;
            double ty = m.Ty;
            double x, y;
            for (int coordinate = 0; coordinate < coordinates.Length; coordinate += 2)
            {
                x = coordinates[coordinate];
                y = coordinates[coordinate + 1];
                transformedCoordinates[coordinate] = x * sx + y * shx + tx;
                transformedCoordinates[coordinate + 1] = x * shy + y * sy + ty;
            }

            return transformedCoordinates;
        }
        #endregion

        #region IDrawer Members

        #region State management
        /// <summary>
        /// Save the current state of this drawer
        /// Typical implementation store transformation matrix, clipping, and other information to this state object
        /// </summary>
        public object Save()
        {
            DrawerState result = new DrawerState();
            if (matrixStack != null) result.MatrixStack = matrixStack.Clone();
            if (currentTransform != null) result.CurrentTransform = currentTransform.Clone();

            return result;
        }

        /// <summary>
        /// Restore the state of this drawer 
        /// </summary>
        public void Load(object state)
        {
            if (state is DrawerState)
            {
                DrawerState ds = (DrawerState)state;
                currentTransform = ds.CurrentTransform;
                matrixStack = ds.MatrixStack;
                transformRequired = currentTransform != null;
            }
            else Cross.Drawing.IncompatibleTypeException.Publish(state, typeof(DrawerState));
        }
        #endregion

        #region Clipping
        bool isEmptyClip = true;

        /// <summary>
        /// Set clipping region to be a rectangular region
        /// </summary>
        /// <param name="x1">X-axis coordinate of first point</param>
        /// <param name="y1">Y-axis coordinate of first point</param>
        /// <param name="x2">X-axis coordinate of second point</param>
        /// <param name="y2">Y-axis coordinate of second point</param>
        /// <remarks>The coordinates are logical (based on the current coordinate system), not absolute (based on pixels)</remarks>
        public void SetClip(double x1, double y1, double x2, double y2)
        {
            clipX1 = x1;
            clipX2 = x2;
            clipY1 = y1;
            clipY2 = y2;
            isEmptyClip = ((x1 == x2) && (y1 == y2));
        }

        #region Opacity Mask
        private MaskBuffer mOpacityMask;
        /// <summary>
        /// Gets/Sets the opacity mask used for clipping based on opacity masking.
        /// <para>Default is null (no opacity mask operation is required)</para>
        /// </summary>
        public MaskBuffer Mask
        {
            get { return mOpacityMask; }
            set
            {
                if (mOpacityMask != value)
                {
                    mOpacityMask = value;
                    opacityPreparationRequired = true;
                    preparationRequired = true;
                }
            }
        }
        #endregion

        #endregion

        #region Gamma Correction

        #region Gamma Corrected
        private bool mGammaCorrected = true;
        /// <summary>
        /// Gets/Sets whether gamma correction is in use.
        /// <para>Default is true</para>
        /// </summary>
        public bool GammaCorrected
        {
            get { return mGammaCorrected; }
            set
            {
                if (mGammaCorrected != value)
                {
                    mGammaCorrected = value;
                    preparationRequired = true;
                    gammaPreparationRequired = true;
                }
            }
        }
        #endregion

        #region Set Gamma
        /// <summary>
        /// Set gamma correction factor for all color components
        /// </summary>
        /// <param name="factor">The value used for all 3 color components</param>
        public void SetGamma(double factor)
        {
            gammaRed = factor;
            gammaGreen = factor;
            gammaBlue = factor;
            preparationRequired = true;
            gammaPreparationRequired = true;
        }

        /// <summary>
        /// Set gamma correction factors for individual color components
        /// </summary>
        /// <param name="red">Red component factor</param>
        /// <param name="green">Green component factor</param>
        /// <param name="blue">Blue component factor</param>
        public void SetGamma(double red, double green, double blue)
        {
            gammaRed = red;
            gammaGreen = green;
            gammaBlue = blue;
            preparationRequired = true;
            gammaPreparationRequired = true;
        }
        #endregion

        #endregion

        #region Matrix operations
        /// <summary>
        /// Create a new transformation matrix and make it active
        /// </summary>
        public void PushMatrix()
        {
            if (currentTransform == null)
            {
                currentTransform = new Matrix3x3();
            }
            else
            {
                //store current transform to stack
                if (matrixStack == null) matrixStack = new Matrix3x3Stack();
                matrixStack.Push(currentTransform);

                //create a new matrix
                currentTransform = currentTransform.Clone();
            }

            transformRequired = true;
        }

        /// <summary>
        /// Remove the currently active transformation matrix, make the previous one in matrix stack active
        /// </summary>
        public void PopMatrix()
        {
            if (matrixStack != null)
            {
                if (matrixStack.Count > 0)
                {
                    Matrix3x3 prev = matrixStack.Pop();
                    currentTransform = prev;
                    transformRequired = true;
                }
                else
                {
                    currentTransform = null;
                    transformRequired = false;
                }
            }
        }

        /// <summary>
        /// Reset the current transformation matrix by making it an identity matrix
        /// </summary>
        public void ResetMatrix()
        {
            if (currentTransform != null) currentTransform.Reset();
        }
        #endregion

        #region Transformations

        #region Translate
        /// <summary>
        /// Push translate transformation to matrix
        /// </summary>
        /// <param name="x">horizontal coordinate value to transform by</param>
        /// <param name="y">vertical coordinate value to transform by</param>
        public void Translate(double x, double y)
        {
            //if (currentTransform == null) PushMatrix();
            //currentTransform.Translate(x, y);

            if (currentTransform == null) currentTransform = new Matrix3x3();
            currentTransform.TranslateRelative(x, y);
            transformRequired = true;
        }
        #endregion

        #region Rotate
        /// <summary>
        /// Push rotate transformation to matrix. The rotation origin is assumed at (0, 0)
        /// </summary>
        /// <param name="angle">The angle (in degree) to rotate by</param>
        public void Rotate(double angle)
        {
            //if (currentTransform == null) PushMatrix();

            if (currentTransform == null) currentTransform = new Matrix3x3();
            currentTransform.RotateRelative(angle);
            transformRequired = true;
        }

        /// <summary>
        /// Push rotate transformation to matrix
        /// </summary>
        /// <param name="angle">The angle (in degree) to rotate by</param>
        /// <param name="centerX">X-coordinate of rotation origin</param>
        /// <param name="centerY">Y-coordinate of rotation origin</param>        
        public void Rotate(double angle, double centerX, double centerY)
        {
            //if (currentTransform == null) PushMatrix();
            if (currentTransform == null) currentTransform = new Matrix3x3();
            currentTransform.RotateRelative(angle, centerX, centerY);
            transformRequired = true;
        }
        #endregion

        #region Scale
        /// <summary>
        /// Push scale transformation to matrix. The scaling origin is assumed at (0, 0)
        /// </summary>
        /// <param name="scaleX">The horizontal factor to scale by</param>
        /// <param name="scaleY">The vertical factor to scale by</param>
        public void Scale(double scaleX, double scaleY)
        {
            //if (currentTransform == null) PushMatrix();
            if (currentTransform == null) currentTransform = new Matrix3x3();
            currentTransform.ScaleRelative(scaleX, scaleY);
            transformRequired = true;
        }

        /// <summary>
        /// Push scale transformation to matrix.
        /// </summary>        
        /// <param name="scaleX">The horizontal factor to scale by</param>
        /// <param name="scaleY">The vertical factor to scale by</param>
        /// <param name="centerX">X-coordinate of scaling origin</param>
        /// <param name="centerY">Y-coordinate of scaling origin</param>
        public void Scale(double scaleX, double scaleY, double centerX, double centerY)
        {
            if (currentTransform == null) currentTransform = new Matrix3x3();
            //if (currentTransform == null) PushMatrix();
            //currentTransform.Translate(centerX, centerY);
            //currentTransform.Translate(-centerX, -centerY);
            //currentTransform.Scale(scaleX, scaleY);
            //currentTransform.Translate(centerX, centerY);
            currentTransform.ScaleRelative(scaleX, scaleY, centerX, centerY);
            transformRequired = true;
        }
        #endregion

        #region Skew
        /// <summary>
        /// Push skew (shear) transformation to matrix. The skewing origin is assumed at (0, 0)
        /// </summary>
        /// <param name="angleX">X-axis skew angle (in degree)</param>
        /// <param name="angleY">Y-axis skew angle (in degree)</param>
        public void Skew(double angleX, double angleY)
        {
            //if (currentTransform == null) PushMatrix();
            if (currentTransform == null) currentTransform = new Matrix3x3();
            currentTransform.SkewRelative(angleX, angleY);
            transformRequired = true;
        }

        /// <summary>
        /// Push skew (shear) transformation to matrix
        /// </summary>
        /// <param name="angleX">X-axis skew angle (in degree)</param>
        /// <param name="angleY">Y-axis skew angle (in degree)</param>
        /// <param name="centerX">X-coordinate of skewing origin</param>
        /// <param name="centerY">Y-coordinate of skewing origin</param>
        public void Skew(double angleX, double angleY, double centerX, double centerY)
        {
            if (currentTransform == null) currentTransform = new Matrix3x3();
            //if (currentTransform == null) PushMatrix();
            //currentTransform.Translate(-centerX, -centerY);
            //currentTransform.Skew(angleX, angleY);
            //currentTransform.Translate(centerX, centerY);

            currentTransform.SkewRelative(angleX, angleY, centerY, centerY);
            transformRequired = true;
        }
        #endregion

        #endregion

        #region Drawing operations

        #region Clear
        /// <summary>
        /// Empty buffer
        /// </summary>
        public void Clear()
        {
            mBuffer.Clear();
        }

        /// <summary>
        /// Fill the buffer with a specified color
        /// </summary>
        public void Clear(Color color)
        {
            mBuffer.Clear(color);
        }
        #endregion

        #region Draw Rectangle

        /// <summary>
        /// Render a rectangle from top-left point (x, y) with the specified width and height
        /// </summary>
        /// <param name="stroke">The stroke style to draw border</param>
        /// <param name="fill">The fill style to render inner region</param>
        /// <param name="x">X-axis of starting point</param>
        /// <param name="y">Y-axis of starting point</param>
        /// <param name="width">vertical size</param>
        /// <param name="height">horizontal size</param>
        /// <remarks>The stroke and the fill can both be a null reference (Nothing in Visual Basic). If the stroke is null, no stroke is performed. If the fill is null, no fill is performed.</remarks>
        public void DrawRectangle(Fill fill, double x, double y, double width, double height)
        {
            //calculate coordinates of a rectangle
            double[] coordinates = new double[]
            {
                x, y,
                x + width, y,
                x + width, y+height,
                x, y+height,
                x, y
            };

            DrawPolygon(fill, coordinates);
        }

        #endregion

        #region Draw Rounded Rectangle

        /// <summary>
        /// Render a rounded rectangle from top-left point (x, y) with the specified width and height
        /// </summary>
        /// <param name="stroke">The stroke style to draw border</param>
        /// <param name="fill">The fill style to render inner region</param>
        /// <param name="x">X-axis of starting point</param>
        /// <param name="y">Y-axis of starting point</param>
        /// <param name="width">vertical size</param>
        /// <param name="height">horizontal size</param>
        /// <param name="radiusX">The radius in the x dimension of the rounded corners. This value will be clamped to the range of 0 to width/2</param>
        /// <param name="radiusY">The radius in the y dimension of the rounded corners. This value will be clamped to the range of 0 to width/2</param>
        /// <remarks>The stroke and the fill can both be a null reference (Nothing in Visual Basic). If the stroke is null, no stroke is performed. If the fill is null, no fill is performed.</remarks>
        public void DrawRoundedRectangle(Fill fill, double x, double y, double width, double height, double radiusX, double radiusY)
        {
            double x2 = x + width;
            double y2 = y + height;

            #region calculate number of points which need to draw
            #region calculate Da
            double ra = (radiusX + radiusY);
            double mDa = Math.Acos(ra / (ra + 0.5)) * 2.0;
            #endregion
            double[] coordinates = new double[((int)(Math.PI / (2 * mDa)) << 3) + 18];
            #endregion

            #region change rounded rectangle to coordinate polygon

            double startAngle, endAngle;
            double centerX, centerY;
            int i = 2;

            #region start point
            coordinates[0] = x;
            coordinates[1] = y + radiusY;
            #endregion

            #region top-left arc
            startAngle = 3.14159 - mDa; // Math.PI - mDa
            endAngle = 1.570796;// Math.PI/2
            centerX = x + radiusX;
            centerY = y + radiusY;
            while (startAngle > endAngle)
            {
                coordinates[i] = centerX + Math.Cos(startAngle) * radiusX;
                coordinates[i + 1] = centerY - Math.Sin(startAngle) * radiusY;
                i += 2;
                startAngle -= mDa;
            }
            coordinates[i] = x + radiusX;
            coordinates[i + 1] = y;
            i += 2;
            #endregion

            #region top-right arc
            startAngle = 1.570796;// Math.PI / 2
            centerX = x2 - radiusX;
            centerY = y + radiusY;
            while (startAngle > 0)
            {
                coordinates[i] = centerX + Math.Cos(startAngle) * radiusX;
                coordinates[i + 1] = centerY - Math.Sin(startAngle) * radiusY;
                i += 2;
                startAngle -= mDa;
            }
            coordinates[i] = x2;
            coordinates[i + 1] = y + radiusY;
            i += 2;

            #endregion

            #region bottom-right arc
            startAngle = 0;
            endAngle = -1.570796; // -Math.PI / 2
            centerX = x2 - radiusX;
            centerY = y2 - radiusY;
            while (startAngle > endAngle)
            {
                coordinates[i] = centerX + Math.Cos(startAngle) * radiusX;
                coordinates[i + 1] = centerY - Math.Sin(startAngle) * radiusY;
                i += 2;
                startAngle -= mDa;
            }
            coordinates[i] = x2 - radiusX;
            coordinates[i + 1] = y2;
            i += 2;
            #endregion

            #region bottom-left arc
            startAngle = -1.570796;// - Math.PI/2
            endAngle = -3.14159; // -Math.PI
            centerX = x + radiusX;
            centerY = y2 - radiusY;
            while (startAngle > endAngle)
            {
                coordinates[i] = centerX + Math.Cos(startAngle) * radiusX;
                coordinates[i + 1] = centerY - Math.Sin(startAngle) * radiusY;
                i += 2;
                startAngle -= mDa;
            }
            coordinates[i] = x;
            coordinates[i + 1] = y2 - radiusY;
            #endregion

            #region left edge
            coordinates[i + 2] = x;
            coordinates[i + 3] = y + radiusY;
            #endregion

            #endregion

            //drawing rounded rectangle
            DrawPolygon(fill, coordinates);
        }

        #endregion

        #region Draw Ellipse
        /// <summary>
        /// Render an ellipse
        /// </summary>
        /// <param name="stroke">The stroke style to draw border</param>
        /// <param name="fill">The fill style to render inner region</param>
        /// <param name="centerX">X-axis coordinate of the center point</param>
        /// <param name="centerY">Y-axis coordinate of the center point</param>
        /// <param name="radiusX">horizontal radius of the ellipse</param>
        /// <param name="radiusY">vertical radius of the ellipse</param>
        /// <remarks>The stroke and the fill can both be a null reference (Nothing in Visual Basic). If the stroke is null, no stroke is performed. If the fill is null, no fill is performed.</remarks>
        public void DrawEllipse(Fill fill, double centerX, double centerY, double radiusX, double radiusY)
        {
            #region Generate coordinate from a ellipse data
            //double mDa = Math.Acos((radiusX + radiusY) / (radiusX + radiusY + 0.4)) * 2.0;

            #region HACKED by HaiNM - 2008 Dec 15
            //To fixed the bug where current local space is too small compared to world space,
            //the ellipse increment step is too big -> not smooth
            double mDa;
            if (currentTransform.IsTransformed)
            {
                double scaleX = Math.Abs(currentTransform.ScaleXFactor);
                double scaleY = Math.Abs(currentTransform.ScaleYFactor);
                mDa = Math.Acos((radiusX * scaleX + radiusY * scaleY) / (radiusX * scaleX + radiusY * scaleY + 0.4)) * 1.25;
            }
            else mDa = Math.Acos((radiusX + radiusY) / (radiusX + radiusY + 0.4)) * 2.0;

            #endregion

            double startAngle = mDa, endAngle = 6.28318;//Math.PI * 2.0 - mDa

            double[] coordinates = new double[((int)(endAngle / mDa) + 2) << 1];
            int i = 2;
            coordinates[0] = centerX + radiusX;
            coordinates[1] = centerY;
            while (startAngle < endAngle)
            {
                coordinates[i] = centerX + Math.Cos(startAngle) * radiusX;
                coordinates[i + 1] = centerY + Math.Sin(startAngle) * radiusY;
                i += 2;
                startAngle += mDa;
            }
            coordinates[i] = coordinates[0];
            coordinates[i + 1] = centerY;

            #endregion
            DrawPolygon(fill, coordinates);
        }

        #endregion

        #region Draw Polygon
        /// <summary>
        /// Render a polygon.
        /// </summary>
        /// <param name="stroke">The stroke style to draw border</param>
        /// <param name="fill">The fill style to render inner region</param>
        /// <param name="coordinates">Coordinate data. Must be in format [x1, y1, x2, y2, ...]</param>
        /// <remarks>The stroke and the fill can both be a null reference (Nothing in Visual Basic). If the stroke is null, no stroke is performed. If the fill is null, no fill is performed.</remarks>
        public void DrawPolygon(Fill fill, double[] coordinates)
        {
            double[] data = null;

            if (preparationRequired) Prepare();

            //transform input coordinates
            if (transformRequired) data = TransformCoordinates(coordinates);
            else data = coordinates;

            //render the inner region
            if (fill != null)
            {
                rasterizer = GetRasterizer(fill.Paint);
                if ((rasterizer is TranformableRasterizer) && (transformRequired))
                {
                    // set transform matrix to current transformable matrix
                    ((TranformableRasterizer)rasterizer).SetTransformMatrix(currentTransform);
                }
                rasterizer.FillPolygon(fill, data, coordinates.Length / 2, 0);
            }
        }
        #endregion

        #region Draw Path
        /// <summary>
        /// Render a path
        /// </summary>
        /// <param name="stroke">The stroke style to draw border</param>
        /// <param name="fill">The fill style to render inner region</param>
        /// <param name="path">The path geometry</param>
        /// <remarks>The stroke and the fill can both be a null reference (Nothing in Visual Basic). If the stroke is null, no stroke is performed. If the fill is null, no fill is performed.</remarks>
        public void DrawPath(Fill fill, DrawingPath path)
        {
            if (preparationRequired) Prepare();

            #region detemin current scale factor
            double sizeScale = 1.0;
            if (currentTransform != null)
            {
                sizeScale = (currentTransform.ScaleXFactor > currentTransform.ScaleYFactor) ?
                    currentTransform.ScaleXFactor : currentTransform.ScaleYFactor;
            }
            #endregion

            // this need scale factor before generate
            double[][] coordinates = mPathGenerator.Generate(path, 1.0);
            //transform input coordinates

            //render the inner region
            if (fill != null)
            {
                rasterizer = GetRasterizer(fill.Paint);
                rasterizer.Paint = fill;
                rasterizer.Begin();
                if ((transformRequired))
                {
                    if ((rasterizer is TranformableRasterizer))
                    {
                        // set transform matrix to current transformable matrix
                        ((TranformableRasterizer)rasterizer).SetTransformMatrix(currentTransform);
                    }
                    for (int coordinateIndex = 0; coordinateIndex < coordinates.Length; coordinateIndex++)
                    {
                        rasterizer.AddPolygon(
                            TransformCoordinates(coordinates[coordinateIndex]),
                            coordinates[coordinateIndex].Length / 2, 0);
                    }
                }
                else
                {
                    // set paint before using approach 2

                    for (int coordinateIndex = 0; coordinateIndex < coordinates.Length; coordinateIndex++)
                    {
                        rasterizer.AddPolygon(coordinates[coordinateIndex], coordinates[coordinateIndex].Length / 2, 0);
                    }
                }
                rasterizer.Finish();
            }
        }
        #endregion

        #endregion

        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor
        /// </summary>
        public Drawer()
        {
        }

        /// <summary>
        /// Create a new instance for the provided buffer
        /// </summary>
        public Drawer(PixelsBuffer buffer)
        {
            Buffer = buffer;
        }
        #endregion
    }
}
