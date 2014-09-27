
namespace Cross.Drawing
{
    /// <summary>
    /// A drawer is responsible for rendering 2D graphics primitives.
    /// This interface maybe used by both immediate drawer and retained drawer.
    /// </summary>
    /// <remarks>
    /// <para>Immediate mode drawer executes drawing commands immediately by rendering to drawing buffer</para>
    /// <para>Retained mode drawer serialize drawing commands to instructions for later rendering</para>
    /// </remarks>
    public interface IDrawer
    {
        #region State save and load
        /// <summary>
        /// Save the current state of this drawer.
        /// Typical implementation store transformation matrix, clipping, and other information to this state object
        /// </summary>
        object Save();

        /// <summary>
        /// Restore the state of this drawer
        /// </summary>
        void Load(object state);
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
        void SetClip(double x1, double y1, double x2, double y2);

        //TO BE REVISED
        /// <summary>
        /// Set clipping region to a boundary
        /// </summary>
        /// <param name="boundary">The boundary to clip to</param>
        //void SetClip(IBoundary boundary);

        /// <summary>
        /// Gets/Sets the opacity mask used for clipping based on opacity masking.
        /// <para>Default is null (no opacity mask operation is required)</para>
        /// </summary>
        MaskBuffer Mask { get; set; }
        #endregion

        #region Gamma Correction
        /// <summary>
        /// Gets/Sets whether gamma correction is in use
        /// </summary>
        bool GammaCorrected { get; set; }

        #region Set Gamma
        /// <summary>
        /// Set gamma correction factor for all color components
        /// </summary>
        /// <param name="factor">The value used for all 3 color components</param>
        void SetGamma(double factor);

        /// <summary>
        /// Set gamma correction factors for individual color components
        /// </summary>
        /// <param name="red">Red component factor</param>
        /// <param name="green">Green component factor</param>
        /// <param name="blue">Blue component factor</param>
        void SetGamma(double red, double green, double blue);
        #endregion

        #endregion

        #region Matrix operations
        /// <summary>
        /// Create a new transformation matrix and make it active
        /// </summary>
        void PushMatrix();

        /// <summary>
        /// Remove the currently active transformation matrix, make the previous one in matrix stack active
        /// </summary>
        void PopMatrix();

        /// <summary>
        /// Reset the current transformation matrix by making it an identity matrix
        /// </summary>
        void ResetMatrix();
        #endregion

        #region Transformations

        #region Translate
        /// <summary>
        /// Push translate transformation to matrix
        /// </summary>
        /// <param name="x">horizontal coordinate value to transform by</param>
        /// <param name="y">vertical coordinate value to transform by</param>
        void Translate(double x, double y);
        #endregion

        #region Rotate
        /// <summary>
        /// Push rotate transformation to matrix. The rotation origin is assumed at (0, 0)
        /// </summary>
        /// <param name="angle">The angle (in degree) to rotate by</param>
        void Rotate(double angle);

        /// <summary>
        /// Push rotate transformation to matrix
        /// </summary>
        /// <param name="angle">The angle (in degree) to rotate by</param>
        /// <param name="centerX">X-coordinate of rotation origin</param>
        /// <param name="centerY">Y-coordinate of rotation origin</param>
        void Rotate(double angle, double centerX, double centerY);
        #endregion

        #region Scale
        /// <summary>
        /// Push scale transformation to matrix. The scaling origin is assumed at (0, 0)
        /// </summary>
        /// <param name="scaleX">The horizontal factor to scale by</param>
        /// <param name="scaleY">The vertical factor to scale by</param>
        void Scale(double scaleX, double scaleY);

        /// <summary>
        /// Push scale transformation to matrix.
        /// </summary>
        /// <param name="scaleX">The horizontal factor to scale by</param>
        /// <param name="scaleY">The vertical factor to scale by</param>
        /// <param name="centerX">X-coordinate of scaling origin</param>
        /// <param name="centerY">Y-coordinate of scaling origin</param>
        void Scale(double scaleX, double scaleY, double centerX, double centerY);
        #endregion

        #region Skew
        /// <summary>
        /// Push skew (shear) transformation to matrix. The skewing origin is assumed at (0, 0)
        /// </summary>
        /// <param name="angleX">X-axis skew angle (in degree)</param>
        /// <param name="angleY">Y-axis skew angle (in degree)</param>
        void Skew(double angleX, double angleY);

        /// <summary>
        /// Push skew (shear) transformation to matrix
        /// </summary>
        /// <param name="angleX">X-axis skew angle (in degree)</param>
        /// <param name="angleY">Y-axis skew angle (in degree)</param>
        /// <param name="centerX">X-coordinate of skewing origin</param>
        /// <param name="centerY">Y-coordinate of skewing origin</param>
        void Skew(double angleX, double angleY, double centerX, double centerY);
        #endregion

        #endregion

        #region Drawing operations

        /// <summary>
        /// Empty buffer
        /// </summary>
        void Clear();

        /// <summary>
        /// Fill the buffer with a specified color
        /// </summary>
        void Clear(Color color);

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
        void DrawRectangle(Fill fill, double x, double y, double width, double height);

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
        void DrawRoundedRectangle(Fill fill, double x, double y, double width, double height, double radiusX, double radiusY);

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
        void DrawEllipse(Fill fill, double centerX, double centerY, double radiusX, double radiusY);

        /// <summary>
        /// Render a polygon.
        /// </summary>
        /// <param name="stroke">The stroke style to draw border</param>
        /// <param name="fill">The fill style to render inner region</param>
        /// <param name="coordinates">Coordinate data. Must be in format [x1, y1, x2, y2, ...]</param>
        /// <remarks>The stroke and the fill can both be a null reference (Nothing in Visual Basic). If the stroke is null, no stroke is performed. If the fill is null, no fill is performed.</remarks>
        void DrawPolygon(Fill fill, double[] coordinates);

        /// <summary>
        /// Render a path
        /// </summary>
        /// <param name="stroke">The stroke style to draw border</param>
        /// <param name="fill">The fill style to render inner region</param>
        /// <param name="path">The path geometry</param>
        /// <remarks>The stroke and the fill can both be a null reference (Nothing in Visual Basic). If the stroke is null, no stroke is performed. If the fill is null, no fill is performed.</remarks>
        void DrawPath(Fill fill, DrawingPath path);

        #endregion
    }
}

