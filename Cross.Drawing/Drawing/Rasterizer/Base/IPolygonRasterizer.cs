#region Using directives

#endregion

namespace Cross.Drawing
{
    /// <summary>
    /// A polygon rasterizer is used for rasterizing a polygon and render to buffer
    /// </summary>
    /// <remarks>A polygon rasterizer can be used by two different approaches:
    /// <para>   Aprroach 1: call method FillPolygon to rasterize polygon and render to buffer directly in one pass</para>
    /// <para>   Approach 2: call the following methods sequentially to complete a renderation</para>
    /// <para>        + Set current paint to Paint property</para>
    /// <para>        + Begin() to start renderation process</para>
    /// <para>        + LineTo(), MoveTo(), AddPolygon , AddCurve(), etc. to rasterize the vectors to renderation mask</para>
    /// <para>        + Finish() to end renderation process and render mask to buffer</para>
    /// </remarks>
    public interface IPolygonRasterizer
    {
        #region Properties
        /// <summary>
        /// Gets/Sets the buffer to draw to
        /// </summary>
        PixelBuffer Buffer
        { get; set; }

        ///// <summary>
        ///// Gets/Sets the mode of winding
        ///// </summary>
        //WindingRule WindingRule
        //{ get;set;}

        #region Set Clip
        /// <summary>
        /// Set clipping region to be a rectangular region
        /// </summary>
        /// <param name="x1">X-axis coordinate of first point</param>
        /// <param name="y1">Y-axis coordinate of first point</param>
        /// <param name="x2">X-axis coordinate of second point</param>
        /// <param name="y2">Y-axis coordinate of second point</param>
        /// <remarks>The coordinates are logical (based on the current coordinate system), not absolute (based on pixels)</remarks>
        void SetClip(double x1, double y1, double x2, double y2);
        #endregion

        /// <summary>
        /// Gets/Sets the paint material used for filling
        /// </summary>
        PaintMaterial Paint
        { get; set; }

        /// <summary>
        /// Gets/Sets gamma correction function.
        /// <para>Default is null (no gamma correction)</para>
        /// </summary>
        IGammaCorrector Gamma
        { get; set; }

        /// <summary>
        /// Gets/Sets the opacity mask used for clipping based on opacity masking.
        /// <para>Default is null (no opacity mask operation is required)</para>
        /// </summary>
        MaskBuffer OpacityMask
        { get; set; }
        #endregion

        #region Direct approach
        /// <summary>
        /// Rasterize and fill the polygon directly in one pass. This approach is slightly faster than the normal renderation process (Begin, Addxxx, Finish)
        /// </summary>
        /// <param name="paint">The paint material used for filling</param>
        /// <param name="data">
        ///     raw data array in format [x1,y1, x2,y2, ...]
        ///     For more flexible, when x= double.NaN it mean
        ///     next coordinate pair is move to
        /// </param>
        /// <param name="pointCount">Number of points contained within data</param>
        /// <param name="startOffset">Index of the first point in data </param>
        void FillPolygon(PaintMaterial paint, double[] data, int pointCount, int startOffset);

        /// <summary>
        /// Rasterize and fill the polygon directly in one pass. This approach is slightly faster than the normal renderation process (Begin, Addxxx, Finish)
        /// </summary>
        /// <param name="paint">The paint material used for filling</param>
        /// <param name="data">
        ///     raw data array in format [x1,y1, x2,y2, ...]
        ///     For more flexible, when x= double.NaN it mean
        ///     next coordinate pair is move to
        /// </param>
        /// <param name="pointCount">Number of points contained within data</param>
        /// <param name="startOffset">Index of the first point in data </param>
        /// <param name="offsetX">offseted X</param>
        /// <param name="offsetY">offseted Y</param>
        void FillPolygon(PaintMaterial paint, double[] data, int pointCount, int startOffset, double offsetX, double offsetY);

        /// <summary>
        /// Rasterize and draw the polygon using 1-pixel wide pen
        /// </summary>
        /// <param name="data">raw data array in format [x1,y1, x2,y2, ...]</param>
        /// <param name="pointCount">Number of points contained within data</param>
        /// <param name="startOffset">Index of the first point in data </param>
        void DrawPolygon(Color color, double[] data, int pointCount, int startOffset);
        #endregion

        #region Normal approach

        #region Begin
        /// <summary>
        /// Begin rasterize polygons into same buffer
        /// </summary>
        void Begin();

        /// <summary>
        /// Begin rasterize polygons into same buffer, including set clipping box
        /// </summary>
        /// <param name="left">left of clip box</param>
        /// <param name="top">top of clip box</param>
        /// <param name="right">right of clip box</param>
        /// <param name="bottom">bottom of clip box</param>
        void Begin(double left, double top, double right, double bottom);
        #endregion

        #region move to , line to
        /// <summary>
        /// Move to (x,y) , start for a new polygon
        /// </summary>
        /// <param name="x">x coordinate</param>
        /// <param name="y">y coordinate</param>
        void MoveTo(double x, double y);

        /// <summary>
        /// Rasterize a line
        /// </summary>
        /// <param name="x">x coordinate</param>
        /// <param name="y">y coordinate</param>
        void LineTo(double x, double y);
        #endregion

        #region add polygon
        /// <summary>
        /// Rasterize a polygon
        /// </summary>
        /// <param name="data">Raw data array in format [x1,y1, x2,y2, ...]</param>
        /// <param name="pointCount">Number of points contained within data</param>
        /// <param name="startOffset">Index of the first point in data </param>
        void AddPolygon(double[] data, int pointCount, int startOffset);

        /// <summary>
        /// Rasterize a polygon
        /// </summary>
        /// <param name="data">Raw data array in format [x1,y1, x2,y2, ...]</param>
        /// <param name="pointCount">Number of points contained within data</param>
        /// <param name="startOffset">Index of the first point in data </param>
        void AddPolygon(double[] data, int pointCount, int startOffset, double offsetX, double offsetY);
        #endregion

        #region finish rasterize and filling
        /// <summary>
        /// Filling into buffer by using current rasterized result
        /// </summary>
        void Finish();
        #endregion

        #region finish rasterize and filling
        /// <summary>
        /// Filling into buffer by using current rasterized result
        /// </summary>
        void FinishWithoutFilling();
        #endregion
        #endregion
    }
}
