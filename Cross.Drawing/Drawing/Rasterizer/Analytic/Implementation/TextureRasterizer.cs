#region Using directives
using System;
#endregion

namespace Cross.Drawing.Rasterizers.Analytical
{
    /// <summary>
    /// This implement for texture paint rasterizer.
    /// </summary>
    public /*internal*/ class TextureRasterizer : TranformableRasterizer
    {
        #region Constructors
        /// <summary>
        /// Default constructor for TextureRasterizer
        /// </summary>
        public TextureRasterizer()
        { }
        #endregion

        #region Fill including transform
        #region Even odd, not including Gamma
        /// <summary>
        /// Filling row data result from start y index to end y index including transformation
        /// <para>While filling can use CurrentTransformMatrix, or InverterMatrix... to calculate
        /// or access transformation information</para>
        /// </summary>
        /// <param name="paint">paint</param>
        /// <param name="rows">rows</param>
        /// <param name="startYIndex">start y index</param>
        /// <param name="endYIndex">end y index</param>
        protected override void OnFillingTransformedEvenOdd(PaintMaterial paint, RowData[] rows, int startYIndex, int endYIndex)
        {
            throw new NotImplementedException();
        }
        #endregion
        #region even odd, including gamma
        /// <summary>
        /// Filling row data result from start y index to end y index including transformation and gamma
        /// <para>While filling can use CurrentTransformMatrix, or InverterMatrix... to calculate
        /// or access transformation information</para>
        /// </summary>
        /// <param name="paint">paint</param>
        /// <param name="rows">rows</param>
        /// <param name="startYIndex">start y index</param>
        /// <param name="endYIndex">end y index</param>
        /// <param name="gammaLutRed">gamma look up table for red</param>
        /// <param name="gammaLutGreen">gamma look up table for green</param>
        /// <param name="gammaLutBlue">gamma look up table for blue</param>
        protected override void OnFillingTransformedEvenOdd(PaintMaterial paint, RowData[] rows, int startYIndex, int endYIndex, byte[] gammaLutRed, byte[] gammaLutGreen, byte[] gammaLutBlue)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region non zero,not including gamma
        /// <summary>
        /// Filling row data result from start y index to end y index including transformation
        /// <para>While filling can use CurrentTransformMatrix, or InverterMatrix... to calculate
        /// or access transformation information</para>
        /// </summary>
        /// <param name="paint">paint</param>
        /// <param name="rows">rows</param>
        /// <param name="startYIndex">start y index</param>
        /// <param name="endYIndex">end y index</param>
        protected override void OnFillingTransformedNonZero(PaintMaterial paint, RowData[] rows, int startYIndex, int endYIndex)
        {
            throw new NotImplementedException();
        }
        #endregion
        #region non-zero, including gamma
        /// <summary>
        /// Filling row data result from start y index to end y index including transformation
        /// <para>While filling can use CurrentTransformMatrix, or InverterMatrix... to calculate
        /// or access transformation information</para>
        /// </summary>
        /// <param name="paint">paint</param>
        /// <param name="rows">rows</param>
        /// <param name="startYIndex">start y index</param>
        /// <param name="endYIndex">end y index</param>
        /// <param name="gammaLutRed">gamma look up table for red</param>
        /// <param name="gammaLutGreen">gamma look up table for green</param>
        /// <param name="gammaLutBlue">gamma look up table for blue</param>
        protected override void OnFillingTransformedNonZero(PaintMaterial paint, RowData[] rows, int startYIndex, int endYIndex, byte[] gammaLutRed, byte[] gammaLutGreen, byte[] gammaLutBlue)
        {
            throw new NotImplementedException();
        }
        #endregion
        #endregion

        #region fill not including transform
        #region Filling non zero  without gamma
        /// <summary>
        /// Fill to buffer base rows data information using non zero rule
        /// </summary>
        /// <param name="paint">paint using for fill</param>
        /// <param name="rows">row data information</param>
        /// <param name="startRowIndex">start row index in row array need to draw</param>
        /// <param name="endRowIndex">end row index in end row array need to draw</param>
        protected override void OnFillingNonZero(PaintMaterial paint, RowData[] rows, int startRowIndex, int endRowIndex)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region filling non zero including gamma
        /// <summary>
        /// Fill to buffer base rows data information using non zero rule and using lookup table
        /// </summary>
        /// <param name="paint">paint using for fill</param>
        /// <param name="rows">row data information</param>
        /// <param name="startRowIndex">start row index in row array need to draw</param>
        /// <param name="endRowIndex">end row index in end row array need to draw</param>
        /// <param name="gammaLutRed">gamma look up table for red</param>
        /// <param name="gammaLutGreen">gamma look up table for green</param>
        /// <param name="gammaLutBlue">gamma look up table for blue</param>
        protected override void OnFillingNonZero(PaintMaterial paint, RowData[] rows, int startRowIndex, int endRowIndex, byte[] gammaLutRed, byte[] gammaLutGreen, byte[] gammaLutBlue)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region filling even odd not including gamma
        /// <summary>
        /// Fill to buffer base rows data information using even odd rule
        /// </summary>
        /// <param name="paint">paint using for fill</param>
        /// <param name="rows">row data information</param>
        /// <param name="startRowIndex">start row index in row array need to draw</param>
        /// <param name="endRowIndex">end row index in end row array need to draw</param>
        protected override void OnFillingEvenOdd(PaintMaterial paint, RowData[] rows, int startRowIndex, int endRowIndex)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region filling even odd including gamma
        /// <summary>
        /// Fill to buffer base rows data information using even odd rule
        /// </summary>
        /// <param name="paint">paint using for fill</param>
        /// <param name="rows">row data information</param>
        /// <param name="startRowIndex">start row index in row array need to draw</param>
        /// <param name="endRowIndex">end row index in end row array need to draw</param>
        /// <param name="gammaLutRed">gamma look up table for red</param>
        /// <param name="gammaLutGreen">gamma look up table for green</param>
        /// <param name="gammaLutBlue">gamma look up table for blue</param>
        protected override void OnFillingEvenOdd(PaintMaterial paint, RowData[] rows, int startRowIndex, int endRowIndex, byte[] gammaLutRed, byte[] gammaLutGreen, byte[] gammaLutBlue)
        {
            throw new NotImplementedException();
        }
        #endregion

        #endregion
    }
}
