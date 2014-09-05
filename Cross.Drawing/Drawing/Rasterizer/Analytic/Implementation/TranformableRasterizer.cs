


namespace Cross.Drawing.Rasterizers.Analytical
{
    /// <summary>
    /// Rasterizer that need to transform before filling.
    /// This is base class for <see cref="TextureRasterizer"/> and <see cref="GradientRasterizer"/>
    /// </summary>
    public /*internal*/ abstract class TranformableRasterizer : AnalyticalRasterizerBase
    {
        #region protected value
        /// <summary>
        /// Saving current transform matrix
        /// </summary>
        internal Matrix3x3 CurrentTransformMatrix;

        /// <summary>
        /// Boolean to check that current paint is transformed
        /// </summary>
        protected bool IsTransformed = false;

        /// <summary>
        /// saving current inverted matrix sx value
        /// </summary>
        protected double InvertedMatrixSx = 0.0;
        /// <summary>
        /// saving current inverted matrix sy value
        /// </summary>
        protected double InvertedMatrixSy = 0.0;

        /// <summary>
        /// saving current inverted matrix shx value
        /// </summary>
        protected double InvertedMatrixShx = 0.0;

        /// <summary>
        /// saving current inverted matrix shy value
        /// </summary>
        protected double InvertedMatrixShy = 0.0;

        /// <summary>
        /// saving current inverted matrix tx value
        /// </summary>
        protected double InvertedMatrixTx = 0.0;

        /// <summary>
        /// saving current inverted matrix ty value
        /// </summary>
        protected double InvertedMatrixTy = 0.0;
        #endregion

        #region Set transform matrix
        /// <summary>
        /// Set transform matrix
        /// </summary>
        /// <param name="transformMatrix">transform matrix</param>
        public /*internal*/ virtual void SetTransformMatrix(Matrix3x3 transformMatrix)
        {
            if (transformMatrix != null)
            {
                CurrentTransformMatrix = transformMatrix;
                // check if current transform is transformed
                IsTransformed = transformMatrix.IsTransformed;
                if (IsTransformed)
                {
                    #region calculate inverted matrix
                    Matrix3x3 InvertedMatrix = transformMatrix.InvertedMatrix;
                    if (InvertedMatrix != null)
                    {
                        InvertedMatrixSx = InvertedMatrix.Sx;
                        InvertedMatrixSy = InvertedMatrix.Sy;
                        InvertedMatrixShy = InvertedMatrix.Shy;
                        InvertedMatrixShx = InvertedMatrix.Shx;
                        InvertedMatrixTx = InvertedMatrix.Tx;
                        InvertedMatrixTy = InvertedMatrix.Ty;
                    }
                    else
                    {
                        InvertedMatrixSx = 0.0;
                        InvertedMatrixSy = 0.0;
                        InvertedMatrixShy = 0.0;
                        InvertedMatrixShx = 0.0;
                        InvertedMatrixTx = 0.0;
                        InvertedMatrixTy = 0.0;
                    }
                    #endregion
                }
            }
        }
        #endregion

        #region override on filling

        protected override void OnFilling(PaintMaterial paint, RowData[] rows, int startYIndex, int endYIndex)
        {
            //outside of box, no need to fill
            if (IsClipBoxOutSideBound) return;

            if (!IsTransformed)
            {
                base.OnFilling(paint, rows, startYIndex, endYIndex);
            }
            else
            {
                // when gamma function is assigned and gamma function need to apply
                //if ((mGamma != null) && (mGamma.IsAppliedGamma))
                if (mGamma != null)
                {
                    if (paint.FillingRule == FillingRule.NonZero)
                    {
                        // fill non-zero including gamma
                        OnFillingTransformedNonZero(paint, rows, startYIndex, endYIndex, mGamma.GetLookupTableRed(), mGamma.GetLookupTableGreen(), mGamma.GetLookupTableBlue());
                    }
                    else
                    {
                        // fill Even-Odd including gamma
                        OnFillingTransformedEvenOdd(paint, rows, startYIndex, endYIndex, mGamma.GetLookupTableRed(), mGamma.GetLookupTableGreen(), mGamma.GetLookupTableBlue());
                    }
                }
                else
                {
                    //Cross.Log.Debug("Filling without gamma");
                    if (paint.FillingRule == FillingRule.NonZero)
                    {
                        OnFillingTransformedNonZero(paint, rows, startYIndex, endYIndex);
                    }
                    else
                    {
                        OnFillingTransformedEvenOdd(paint, rows, startYIndex, endYIndex);
                    }
                }
            }
        }
        #endregion

        #region abstracted methods
        /// <summary>
        /// Filling row data result from start y index to end y index including transformation
        /// <para>While filling can use CurrentTransformMatrix, or InverterMatrix... to calculate
        /// or access transformation information</para>
        /// </summary>
        /// <param name="paint">paint</param>
        /// <param name="rows">rows</param>
        /// <param name="startYIndex">start y index</param>
        /// <param name="endYIndex">end y index</param>
        protected abstract void OnFillingTransformedEvenOdd(PaintMaterial paint, RowData[] rows, int startYIndex, int endYIndex);


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
        protected abstract void OnFillingTransformedEvenOdd(PaintMaterial paint, RowData[] rows, int startYIndex, int endYIndex, byte[] gammaLutRed, byte[] gammaLutGreen, byte[] gammaLutBlue);


        /// <summary>
        /// Filling row data result from start y index to end y index including transformation
        /// <para>While filling can use CurrentTransformMatrix, or InverterMatrix... to calculate
        /// or access transformation information</para>
        /// </summary>
        /// <param name="paint">paint</param>
        /// <param name="rows">rows</param>
        /// <param name="startYIndex">start y index</param>
        /// <param name="endYIndex">end y index</param>
        protected abstract void OnFillingTransformedNonZero(PaintMaterial paint, RowData[] rows, int startYIndex, int endYIndex);


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
        protected abstract void OnFillingTransformedNonZero(PaintMaterial paint, RowData[] rows, int startYIndex, int endYIndex, byte[] gammaLutRed, byte[] gammaLutGreen, byte[] gammaLutBlue);
        #endregion

    }
}
