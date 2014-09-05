#region Using directives
using System;
using Cross.Drawing.Rasterizers.Analytical;
#endregion

namespace Cross.Drawing
{
    /// <summary>
    /// This drawer is similar to <see cref="Drawer"/> except that it works with a <see cref="MaskBuffer"/> instead of <see cref="MaskBuffer"/>
    /// Notes: property Buffer must be assigned before using this drawer
    /// </summary>
    /// <remarks>This drawer does not support gamma correction, gradient, and image (yet)</remarks>
    public class MaskDrawer : IDrawer
    {
        #region Fields
        MaskRasterizer mMaskRasterizer = null;
        IPolygonRasterizer rasterizer = null;
        bool preparationRequired = true;
        bool transformRequired = false;
        Matrix3x3Stack matrixStack = null;

        /// <summary>
        /// Current transformation matrix.
        /// When null, no transformation is required
        /// </summary>
        Matrix3x3 currentTransform = null;
        double[] transformedCoordinates = null;
        #endregion

        #region Buffer
        private MaskBuffer mBuffer;
        /// <summary>
        /// Gets/Sets the buffer where results will be rendered to
        /// </summary>
        public MaskBuffer Buffer
        {
            get { return mBuffer; }
            set { mBuffer = value; }
        }
        #endregion

        #region Prepare
        /// <summary>
        /// Preparations before drawing
        /// </summary>
        void Prepare()
        {
            if (mBuffer == null) Cross.Drawing.NullArgumentException.Publish(typeof(MaskBuffer), "Buffer");

            if (mMaskRasterizer == null) mMaskRasterizer = new MaskRasterizer();
            //throw new Exception("How to use the mask rasterizer correctly ?");
            mMaskRasterizer.ResultMask = mBuffer;

            preparationRequired = false;
        }
        #endregion

        #region Get Rasterizer
        /// <summary>
        /// Gets the corresponding rasterizer based on the input paint
        /// </summary>
        IPolygonRasterizer GetRasterizer(Paint paint)
        {
            //throw new Exception("Please uncomment the following region and... debug it");


            return mMaskRasterizer;
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
        /// <summary>
        /// Set clipping region to be a rectangular region
        /// </summary>
        /// <param name="x1">X-axis coordinate of first point</param>
        /// <param name="y1">Y-axis coordinate of first point</param>
        /// <param name="x2">X-axis coordinate of second point</param>
        /// <param name="y2">Y-axis coordinate of second point</param>
        /// <remarks>The coordinates are logical (based on the current coordinate system), not absolute (based on pixels)</remarks>
        public void SetClip(double x1, double y1, double x2, double y2) { }

        //TO BE REVISED
        /// <summary>
        /// Set clipping region to a boundary
        /// </summary>
        /// <param name="boundary">The boundary to clip to</param>
        //public void SetClip(IBoundary boundary){}

        #region Opacity Mask
        private MaskBuffer mOpacityMask;
        /// <summary>
        /// Gets/Sets the opacity mask used for clipping based on opacity masking.
        /// <para>Default is null (no opacity mask operation is required)</para>
        /// </summary>
        public MaskBuffer Mask
        {
            get { return mOpacityMask; }
            set { mOpacityMask = value; }
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
                Cross.Drawing.UnsupportedException.Publish("Gamma correction", "MaskDrawer");
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
            throw new NotImplementedException();
        }

        /// <summary>
        /// Set gamma correction factors for individual color components
        /// </summary>
        /// <param name="red">Red component factor</param>
        /// <param name="green">Green component factor</param>
        /// <param name="blue">Blue component factor</param>
        public void SetGamma(double red, double green, double blue)
        {
            throw new NotImplementedException();
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
            if (currentTransform == null) currentTransform = new Matrix3x3();
            transformRequired = true;
            currentTransform.Translate(x, y);
        }
        #endregion

        #region Rotate
        /// <summary>
        /// Push rotate transformation to matrix. The rotation origin is assumed at (0, 0)
        /// </summary>
        /// <param name="angle">The angle (in degree) to rotate by</param>
        public void Rotate(double angle)
        {
            if (currentTransform == null) currentTransform = new Matrix3x3();
            transformRequired = true;
            currentTransform.Rotate(angle);
        }

        /// <summary>
        /// Push rotate transformation to matrix
        /// </summary>
        /// <param name="angle">The angle (in degree) to rotate by</param>
        /// <param name="centerX">X-coordinate of rotation origin</param>
        /// <param name="centerY">Y-coordinate of rotation origin</param>        
        public void Rotate(double angle, double centerX, double centerY)
        {
            if (currentTransform == null) currentTransform = new Matrix3x3();
            transformRequired = true;
            currentTransform.Rotate(angle, centerX, centerY);
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
            if (currentTransform == null) currentTransform = new Matrix3x3();
            transformRequired = true;
            currentTransform.Scale(scaleX, scaleY);
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
            transformRequired = true;
            currentTransform.Scale(scaleX, scaleY, centerX, centerY);
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
            if (currentTransform == null) currentTransform = new Matrix3x3();
            transformRequired = true;
            currentTransform.Skew(angleX, angleY);
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
            transformRequired = true;
            currentTransform.Skew(angleX, angleY, centerX, centerY);
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
        /// Fill the buffer with a specified color's alpha
        /// </summary>
        public void Clear(Color color)
        {
            mBuffer.Clear(color.Alpha);
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
        public void DrawRoundedRectangle(Fill fill, double x, double y, double width, double height, double radiusX, double radiusY) { }

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
        public void DrawEllipse(Fill fill, double centerX, double centerY, double radiusX, double radiusY) { }

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
            throw new NotImplementedException();
        }
        #endregion

        #region Draw Bezier
        /// <summary>
        /// Draws a Bézier spline defined by four points
        /// </summary>
        /// <param name="stroke">The stroke style to draw border</param>
        /// <param name="x1">X-axis coordinate of first point</param>
        /// <param name="y1">Y-axis coordinate of first point</param>
        /// <param name="x2">X-axis coordinate of second point</param>
        /// <param name="y2">Y-axis coordinate of second point</param>
        /// <param name="x3">X-axis coordinate of third point</param>
        /// <param name="y3">Y-axis coordinate of third point</param>
        /// <param name="x4">X-axis coordinate of fourth point</param>
        /// <param name="y4">Y-axis coordinate of fourth point</param>
        public void DrawBezier(double x1, double y1, double x4, double y4, double x2, double y2, double x3, double y3)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Draw Arc
        /// <summary>
        /// Render an arc from point (x1, y1) to (x2, y2)
        /// </summary>
        /// <param name="stroke">The stroke style to draw border</param>
        /// <param name="x1">X-axis coordinate of starting point</param>
        /// <param name="y1">Y-axis coordinate of starting point</param>
        /// <param name="x2">X-axis coordinate of ending point</param>
        /// <param name="y2">Y-axis coordinate of ending point</param>
        /// <param name="isLargeArc">Whether the arc is large or small</param>
        /// <param name="isSweepLeftSide">Whether the arc is right or left</param>
        public void DrawArc(double x1, double y1, double x2, double y2, bool isLargeArc, bool isSweepLeftSide)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Render an arc using angle-based approach
        /// </summary>
        /// <param name="stroke">The stroke style to draw border</param>
        /// <param name="cx">X-axis coordinate of center point</param>
        /// <param name="cy">Y-axis coordinate of center point</param>
        /// <param name="radiusX">X-axis radius</param>
        /// <param name="radiusY">Y-axis radius</param>
        /// <param name="startAngle">Starting angle</param>
        /// <param name="endAngle">Ending angle</param>
        public void DrawArc(double cx, double cy, double radiusX, double radiusY, double startAngle, double endAngle)
        {
            throw new NotImplementedException();
        }
        #endregion

        #endregion

        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor
        /// </summary>
        public MaskDrawer()
        { }

        /// <summary>
        /// Create a new instance for the provided buffer
        /// </summary>
        public MaskDrawer(MaskBuffer buffer)
        {
            mBuffer = buffer;
        }
        #endregion
    }
}
