#region Using directives
using System;
#endregion

namespace Cross.Drawing.Rasterizers.Analytical
{
    /// <summary>
    /// Description
    /// </summary>
    public /*internal*/ class RadialGradientRasterizer : GradientRasterizer
    {
        #region const
        /// <summary>
        /// This using for adjust when value is zero
        /// </summary>
        public const double GradientAdjustment = 0.1;
        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor for RadialGradientRasterizer
        /// </summary>
        public RadialGradientRasterizer()
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
        /// <param name="gammaLut">gamma lookup table</param>
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
            // this base on paint to filling
            if (!(paint.Paint is RadialGradient))
            {
                //throw new NotImplementedException("Support color paint only");
                NotMatchPaintTypeException.Publish(typeof(RadialGradient), paint.Paint.GetType());
                return;
            }
            RadialGradient radial = paint.Paint as RadialGradient;
            if (radial.RadiusX == radial.RadiusY)
            {
                if ((radial.FocusX == radial.CenterX) && (radial.FocusY == radial.CenterY))
                {
                    // when normal radial gradient
                    FillingRadial(radial, paint.ScaledOpacity, rows, startRowIndex, endRowIndex);
                }
                else
                {
                    // circle and focus gradient
                    FillingRadialFocal(radial, paint.ScaledOpacity, rows, startRowIndex, endRowIndex);
                }
            }
            else
            {
                if ((radial.FocusX == radial.CenterX) && (radial.FocusY == radial.CenterY))
                {
                    // when normal ellipse gradient
                    FillingEllipse(radial, paint.ScaledOpacity, rows, startRowIndex, endRowIndex);
                }
                else
                {
                    // ellipse and focus gradient
                    FillingEllipseFocal(radial, paint.ScaledOpacity, rows, startRowIndex, endRowIndex);
                }
            }
        }

        #region Fill normal, radial
        /// <summary>
        /// Filling using radial gradient for circle gradient only
        /// </summary>
        /// <param name="radial">radial</param>
        /// <param name="rows">rows</param>
        /// <param name="startRowIndex">start y index</param>
        /// <param name="endRowIndex">end y index</param>
        void FillingRadial(RadialGradient radial, uint opacity, RowData[] rows, int startRowIndex, int endRowIndex)
        {
            // now not need to check null or not
            uint[] builtColors = radial.GetLinearColors(opacity);
            #region private variable for filling
            int currentCoverage, scLastCoverage, scLastX = 0;
            int tempCover = 0;
            int currentArea = 0;
            int lastXPosition = 0;
            int startXPosition = 0;
            byte calculatedCoverage = 0;

            double centerX = radial.CenterX;
            double centerY = radial.CenterY;
            // in this case radius x = radius y
            double radius = radial.RadiusX;

            // saving precompute value for rows
            /* Normal calculation to get the color index
             * currentColorIndexValue =
                (int)(Math.Sqrt(
                    (startRowIndex - centerY) * (startRowIndex - centerY) +
                    (currentXPosition - centerX) * (currentXPosition - centerX)) * ColorIndexScale / radius );
             * but
             *  preComputeForRow= (startRowIndex - centerY) * (startRowIndex - centerY)
             *  so that
             *    currentColorIndexValue = 
             *    (int)(Math.Sqrt(
                    (preComputeForRow) +
                    (currentXPosition - centerX) * (currentXPosition - centerX)) * ColorIndexScale / radius );
             */
            double preComputeForRow = 0;

            // this is precompute value so that (* ColorIndexScale / radius) now just ( * preComputeRadiusLookup )
            double preComputeRadiusLookup = ColorIndexScale / radius;

            CellData currentCellData = null;
            uint colorData = 0;
            //uint colorG = 0;
            //uint colorRB = 0;


            int currentColorIndexValue = 0;
            int currentXPosition = 0;

            uint dst, dstRB, dstG;
            #endregion

            #region FILLING
            if (radial.Ramp.NoBlendingColor)
            {
                // when no need to blending, when draw a horizontal line
                // do not need check the back color, alway setup
                if (radial.Style != GradientStyle.Pad)
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        preComputeForRow = (startRowIndex - centerY) * (startRowIndex - centerY);
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region non zero checking code
                                        if (scLastCoverage > 255) scLastCoverage = 255;
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            //currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                            currentXPosition = scLastX + 1;
                                            if (scLastCoverage >= 255)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)(Math.Sqrt(
                                                            preComputeForRow +
                                                            (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                                    #endregion
                                                    BufferData[startXPosition] = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                    startXPosition++;
                                                    currentXPosition++;
                                                }
                                            }
                                            else
                                            {
                                                calculatedCoverage = (byte)scLastCoverage;
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)(Math.Sqrt(
                                                            preComputeForRow +
                                                            (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                                    #endregion

                                                    colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                    //calculatedCoverage = (byte)((colorData >> 24));
                                                    //calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                    if (calculatedCoverage >= 254)
                                                    {
                                                        BufferData[startXPosition] = colorData;
                                                    }
                                                    else
                                                    {
                                                        // blend here
                                                        dst = BufferData[startXPosition];
                                                        dstRB = dst & 0x00FF00FF;
                                                        dstG = (dst >> 8) & 0xFF;

                                                        BufferData[startXPosition] =
                                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                    }
                                                    startXPosition++;
                                                    currentXPosition++;
                                                }
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        currentXPosition = currentCellData.X;
                                        currentColorIndexValue =
                                            (int)(Math.Sqrt(preComputeForRow +
                                                (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        //calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        //tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        dst = BufferData[startXPosition];
                                        dstRB = dst & 0x00FF00FF;
                                        dstG = (dst >> 8) & 0xFF;
                                        BufferData[startXPosition] =
                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        preComputeForRow = (startRowIndex - centerY) * (startRowIndex - centerY);
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region non zero checking code
                                        if (scLastCoverage > 255) scLastCoverage = 255;
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            //currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                            currentXPosition = scLastX + 1;
                                            if (scLastCoverage >= 255)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)(Math.Sqrt(
                                                            preComputeForRow +
                                                            (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                                    #endregion
                                                    BufferData[startXPosition] = builtColors[currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                    startXPosition++;
                                                    currentXPosition++;
                                                }
                                            }
                                            else
                                            {
                                                calculatedCoverage = (byte)(scLastCoverage);
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)(Math.Sqrt(
                                                            preComputeForRow +
                                                            (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                                    #endregion

                                                    colorData = builtColors[currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                    //calculatedCoverage = (byte)((colorData >> 24));
                                                    //calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                    if (calculatedCoverage >= 254)
                                                    {
                                                        BufferData[startXPosition] = colorData;
                                                    }
                                                    else
                                                    {
                                                        // blend here
                                                        dst = BufferData[startXPosition];
                                                        dstRB = dst & 0x00FF00FF;
                                                        dstG = (dst >> 8) & 0xFF;

                                                        BufferData[startXPosition] =
                                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                    }
                                                    startXPosition++;
                                                    currentXPosition++;
                                                }
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        currentXPosition = currentCellData.X;
                                        currentColorIndexValue =
                                            (int)(Math.Sqrt(preComputeForRow +
                                                (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue > 254 ? 255 : currentColorIndexValue];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        //calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        //tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        dst = BufferData[startXPosition];
                                        dstRB = dst & 0x00FF00FF;
                                        dstG = (dst >> 8) & 0xFF;
                                        BufferData[startXPosition] =
                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
            }
            else
            {
                // when no need to blending, when draw a horizontal line
                // do not need check the back color, alway setup
                if (radial.Style != GradientStyle.Pad)
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        preComputeForRow = (startRowIndex - centerY) * (startRowIndex - centerY);
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region non zero checking code
                                        if (scLastCoverage > 255) scLastCoverage = 255;
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            //currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                            currentXPosition = scLastX + 1;

                                            while (startXPosition < lastXPosition)
                                            {
                                                #region calculate color index
                                                currentColorIndexValue =
                                                    (int)(Math.Sqrt(
                                                        preComputeForRow +
                                                        (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                                #endregion

                                                colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                calculatedCoverage = (byte)((colorData >> 24));
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                if (calculatedCoverage >= 254)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    // blend here
                                                    dst = BufferData[startXPosition];
                                                    dstRB = dst & 0x00FF00FF;
                                                    dstG = (dst >> 8) & 0xFF;

                                                    BufferData[startXPosition] =
                                                        (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                        | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                }
                                                startXPosition++;
                                                currentXPosition++;
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        currentXPosition = currentCellData.X;
                                        currentColorIndexValue =
                                            (int)(Math.Sqrt(preComputeForRow +
                                                (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        //if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        dst = BufferData[startXPosition];
                                        dstRB = dst & 0x00FF00FF;
                                        dstG = (dst >> 8) & 0xFF;
                                        BufferData[startXPosition] =
                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        preComputeForRow = (startRowIndex - centerY) * (startRowIndex - centerY);
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region non zero checking code
                                        if (scLastCoverage > 255) scLastCoverage = 255;
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            //currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                            currentXPosition = scLastX + 1;

                                            while (startXPosition < lastXPosition)
                                            {
                                                #region calculate color index
                                                currentColorIndexValue =
                                                    (int)(Math.Sqrt(
                                                        preComputeForRow +
                                                        (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                                #endregion

                                                colorData = builtColors[currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                calculatedCoverage = (byte)((colorData >> 24));
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                if (calculatedCoverage >= 254)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    // blend here
                                                    dst = BufferData[startXPosition];
                                                    dstRB = dst & 0x00FF00FF;
                                                    dstG = (dst >> 8) & 0xFF;

                                                    BufferData[startXPosition] =
                                                        (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                        | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                }
                                                startXPosition++;
                                                currentXPosition++;
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        currentXPosition = currentCellData.X;
                                        currentColorIndexValue =
                                            (int)(Math.Sqrt(preComputeForRow +
                                                (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue > 254 ? 255 : currentColorIndexValue];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        //if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        dst = BufferData[startXPosition];
                                        dstRB = dst & 0x00FF00FF;
                                        dstG = (dst >> 8) & 0xFF;
                                        BufferData[startXPosition] =
                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
            }

            #endregion
        }
        #endregion

        #region fill ellipse
        /// <summary>
        /// Filling using ellipse gradient for circle gradient only
        /// </summary>
        /// <param name="radial">radial</param>
        /// <param name="rows">rows</param>
        /// <param name="startRowIndex">start y index</param>
        /// <param name="endRowIndex">end y index</param>
        void FillingEllipse(RadialGradient radial, uint opacity, RowData[] rows, int startRowIndex, int endRowIndex)
        {
            // now not need to check null or not
            uint[] builtColors = radial.GetLinearColors(opacity);
            #region private variable for filling
            int currentCoverage, scLastCoverage, scLastX = 0;
            int tempCover = 0;
            int currentArea = 0;
            int lastXPosition = 0;
            int startXPosition = 0;
            byte calculatedCoverage = 0;

            double centerX = radial.CenterX;
            double centerY = radial.CenterY;
            // in this case radius x = radius y
            double radius = radial.RadiusX;

            // saving precompute value for rows
            /* Normal calculation to get the color index
             *  currentColorIndexValue =
                    (int)(Math.Sqrt(  
                            ((currentXPosition-centerX) * (currentXPosition-centerX) /(radial.RadiusX * radial.RadiusX))
                            +
                            ((startRowIndex - centerY) * (startRowIndex - centerY) )/(radial.RadiusY * radial.RadiusY))
                        * 256);
             * but
             *  preComputeForRow= (startRowIndex - centerY) * (startRowIndex - centerY)
             *  so that
             *    currentColorIndexValue = 
             *    (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX)/rx*rx + (preComputeForRow) +));
             */
            double preComputeForRow = 0;

            // this is precompute value so that (* ColorIndexScale / radius) now just ( * preComputeRadiusLookup )
            //double preComputeRadiusLookup = ColorIndexScale / radius;
            double radiusY = radial.RadiusY;
            double radiusX = radial.RadiusX;
            double radiusYSquared = 1 / (radiusY * radiusY);
            double radiusXSquared = 1 / (radiusX * radiusX);

            CellData currentCellData = null;
            uint colorData = 0;
            //uint colorG = 0;
            //uint colorRB = 0;


            int currentColorIndexValue = 0;
            int currentXPosition = 0;

            uint dst, dstRB, dstG;
            #endregion

            #region FILLING
            if (radial.Ramp.NoBlendingColor)
            {
                // when no need to blending, when draw a horizontal line
                // do not need check the back color, alway setup
                if (radial.Style != GradientStyle.Pad)
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        #region precompute for row
                        preComputeForRow = ((startRowIndex - centerY) * (startRowIndex - centerY)) * radiusYSquared;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region non zero checking code
                                        if (scLastCoverage > 255) scLastCoverage = 255;
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            //currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                            currentXPosition = scLastX + 1;
                                            if (scLastCoverage >= 255)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue = (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX) * radiusXSquared) + preComputeForRow) * ColorIndexScale);
                                                    #endregion
                                                    BufferData[startXPosition] = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                    startXPosition++;
                                                    currentXPosition++;
                                                }
                                            }
                                            else
                                            {
                                                calculatedCoverage = (byte)scLastCoverage;
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue = (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX) * radiusXSquared) + preComputeForRow) * ColorIndexScale);
                                                    #endregion

                                                    colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                    //calculatedCoverage = (byte)((colorData >> 24));
                                                    //calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                    if (calculatedCoverage >= 254)
                                                    {
                                                        BufferData[startXPosition] = colorData;
                                                    }
                                                    else
                                                    {
                                                        // blend here
                                                        dst = BufferData[startXPosition];
                                                        dstRB = dst & 0x00FF00FF;
                                                        dstG = (dst >> 8) & 0xFF;

                                                        BufferData[startXPosition] =
                                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                    }
                                                    startXPosition++;
                                                    currentXPosition++;
                                                }
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        currentXPosition = currentCellData.X;
                                        currentColorIndexValue = (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX) * radiusXSquared) + preComputeForRow) * ColorIndexScale);
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        //calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        //tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        dst = BufferData[startXPosition];
                                        dstRB = dst & 0x00FF00FF;
                                        dstG = (dst >> 8) & 0xFF;
                                        BufferData[startXPosition] =
                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        #region precompute for row
                        preComputeForRow = ((startRowIndex - centerY) * (startRowIndex - centerY)) * radiusYSquared;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region non zero checking code
                                        if (scLastCoverage > 255) scLastCoverage = 255;
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            //currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                            currentXPosition = scLastX + 1;
                                            if (scLastCoverage >= 255)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue = (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX) * radiusXSquared) + preComputeForRow) * ColorIndexScale);
                                                    #endregion
                                                    BufferData[startXPosition] = builtColors[currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                    startXPosition++;
                                                    currentXPosition++;
                                                }
                                            }
                                            else
                                            {
                                                calculatedCoverage = (byte)(scLastCoverage);
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue = (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX) * radiusXSquared) + preComputeForRow) * ColorIndexScale);
                                                    #endregion

                                                    colorData = builtColors[currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                    //calculatedCoverage = (byte)((colorData >> 24));
                                                    //calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                    if (calculatedCoverage >= 254)
                                                    {
                                                        BufferData[startXPosition] = colorData;
                                                    }
                                                    else
                                                    {
                                                        // blend here
                                                        dst = BufferData[startXPosition];
                                                        dstRB = dst & 0x00FF00FF;
                                                        dstG = (dst >> 8) & 0xFF;

                                                        BufferData[startXPosition] =
                                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                    }
                                                    startXPosition++;
                                                    currentXPosition++;
                                                }
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        currentXPosition = currentCellData.X;
                                        currentColorIndexValue = (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX) * radiusXSquared) + preComputeForRow) * ColorIndexScale);
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue > 254 ? 255 : currentColorIndexValue];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        //calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        //tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        dst = BufferData[startXPosition];
                                        dstRB = dst & 0x00FF00FF;
                                        dstG = (dst >> 8) & 0xFF;
                                        BufferData[startXPosition] =
                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
            }
            else
            {
                // when no need to blending, when draw a horizontal line
                // do not need check the back color, alway setup
                if (radial.Style != GradientStyle.Pad)
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        #region precompute for row
                        preComputeForRow = ((startRowIndex - centerY) * (startRowIndex - centerY)) * radiusYSquared;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region non zero checking code
                                        if (scLastCoverage > 255) scLastCoverage = 255;
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            //currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                            currentXPosition = scLastX + 1;

                                            while (startXPosition < lastXPosition)
                                            {
                                                #region calculate color index
                                                currentColorIndexValue = (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX) * radiusXSquared) + preComputeForRow) * ColorIndexScale);
                                                #endregion

                                                colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                calculatedCoverage = (byte)((colorData >> 24));
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                if (calculatedCoverage >= 254)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    // blend here
                                                    dst = BufferData[startXPosition];
                                                    dstRB = dst & 0x00FF00FF;
                                                    dstG = (dst >> 8) & 0xFF;

                                                    BufferData[startXPosition] =
                                                        (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                        | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                }
                                                startXPosition++;
                                                currentXPosition++;
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        currentXPosition = currentCellData.X;
                                        currentColorIndexValue = (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX) * radiusXSquared) + preComputeForRow) * ColorIndexScale);
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        //if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        dst = BufferData[startXPosition];
                                        dstRB = dst & 0x00FF00FF;
                                        dstG = (dst >> 8) & 0xFF;
                                        BufferData[startXPosition] =
                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        #region precompute for row
                        preComputeForRow = ((startRowIndex - centerY) * (startRowIndex - centerY)) * radiusYSquared;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region non zero checking code
                                        if (scLastCoverage > 255) scLastCoverage = 255;
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            //currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                            currentXPosition = scLastX + 1;

                                            while (startXPosition < lastXPosition)
                                            {
                                                #region calculate color index
                                                currentColorIndexValue = (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX) * radiusXSquared) + preComputeForRow) * ColorIndexScale);
                                                #endregion

                                                colorData = builtColors[currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                calculatedCoverage = (byte)((colorData >> 24));
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                if (calculatedCoverage >= 254)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    // blend here
                                                    dst = BufferData[startXPosition];
                                                    dstRB = dst & 0x00FF00FF;
                                                    dstG = (dst >> 8) & 0xFF;

                                                    BufferData[startXPosition] =
                                                        (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                        | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                }
                                                startXPosition++;
                                                currentXPosition++;
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        currentXPosition = currentCellData.X;
                                        currentColorIndexValue = (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX) * radiusXSquared) + preComputeForRow) * ColorIndexScale);
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue > 254 ? 255 : currentColorIndexValue];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        //if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        dst = BufferData[startXPosition];
                                        dstRB = dst & 0x00FF00FF;
                                        dstG = (dst >> 8) & 0xFF;
                                        BufferData[startXPosition] =
                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
            }

            #endregion
        }
        #endregion

        #region Fill circle, Focus
        /// <summary>
        /// Filling using radial gradient for circle gradient only
        /// </summary>
        /// <param name="radial">radial</param>
        /// <param name="rows">rows</param>
        /// <param name="startRowIndex">start y index</param>
        /// <param name="endRowIndex">end y index</param>
        void FillingRadialFocal(RadialGradient radial, uint opacity, RowData[] rows, int startRowIndex, int endRowIndex)
        {
            // now not need to check null or not
            uint[] builtColors = radial.GetLinearColors(opacity);
            #region private variable for filling
            int currentCoverage, scLastCoverage, scLastX = 0;
            int tempCover = 0;
            int currentArea = 0;
            int lastXPosition = 0;
            int startXPosition = 0;
            byte calculatedCoverage = 0;


            double centerX = radial.CenterX;
            double centerY = radial.CenterY;
            // in this case radius x = radius y
            double radius = radial.RadiusX;

            // this is precompute value so that (* ColorIndexScale / radius) now just ( * preComputeRadiusLookup )
            double preComputeRadiusLookup = ColorIndexScale / radius;

            CellData currentCellData = null;
            uint colorData = 0;

            double dx = 0, dy = 0;

            double dySquared = 0; // saving dy * dy
            // focus is changed to relative from the center
            double absoluteFocusX = radial.FocusX;
            double absoluteFocusY = radial.FocusY;

            double focusX = radial.FocusX - centerX;
            double focusY = radial.FocusY - centerY;

            // note that dx,dy need to move center
            /*
             *  dx = (currentXPosition - absoluteFocusX);
             *  dy = (startRowIndex - absoluteFocusY);
             *  currentColorIndexValue =
                    (int)
                    (
                        (
                            (
                            (dx * focusX) + (dy * focusY)
                            + Math.Sqrt
                            (
                                Math.Abs
                                (
                                    radius * radius * (dx * dx + dy * dy) - (dx * focusY - dy * focusX) * (dx * focusY - dy * focusX)      
                                )
                            )
                        ) * (radius /
                        ((radius * radius) - ((focusX * focusX )+ (focusY * focusY))))
                    ) * 256 /radius
                );
             */

            //note that  ( radius / (( radius * radius) - ((focusX * focusX) + (focusY * focusY))) is const
            // so that need to pre compute
            double preComputeMultiply = radius / ((radius * radius) - ((focusX * focusX) + (focusY * focusY)));

            #region modify when pre compute for multiply is zero
            if (preComputeMultiply == 0)
            {
                if (focusX != 0)
                {
                    if (focusX < 0)
                    {
                        focusX += GradientAdjustment;
                    }
                    else
                    {
                        focusX -= GradientAdjustment;
                    }
                }
                if (focusY != 0)
                {
                    if (focusY < 0)
                    {
                        focusY += GradientAdjustment;
                    }
                    else
                    {
                        focusY -= GradientAdjustment;
                    }
                }
                preComputeMultiply = radius / ((radius * radius) - ((focusX * focusX) + (focusY * focusY)));
            }
            #endregion

            double preComputeMultiplyIncludeLookup = preComputeRadiusLookup * preComputeMultiply;

            // saving dy * focusY
            double dyFocusY = 0;
            double dyFocusX = 0;
            double dxFocusYIncrement = 0; // saving dx * focusY - dyFocusX
            double radiusSquared = radius * radius;


            int currentColorIndexValue = 0;
            //int currentXPosition = 0;
            uint dst, dstRB, dstG;
            #endregion

            #region FILLING
            if (radial.Ramp.NoBlendingColor)
            {
                // when no need to blending, when draw a horizontal line
                // do not need check the back color, alway setup
                if (radial.Style != GradientStyle.Pad)
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;

                        #region cumpute value for row
                        //dyFocusY = (startRowIndex - centerY) * (startRowIndex - centerY);
                        dy = (startRowIndex - absoluteFocusY);
                        dySquared = dy * dy;
                        dyFocusX = dy * focusX;
                        dyFocusY = dy * focusY;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region non zero checking code
                                        if (scLastCoverage > 255) scLastCoverage = 255;
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;

                                            #region prepare for row color index calculation
                                            // get current color index value
                                            //currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                            //currentXPosition = scLastX + 1;
                                            dx = (scLastX + 1 - absoluteFocusX);
                                            dxFocusYIncrement = (dx * focusY - dyFocusX);
                                            #endregion
                                            if (scLastCoverage >= 255)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)
                                                        ((((dx * focusX) + dyFocusY +
                                                            Math.Sqrt(Math.Abs(
                                                                radiusSquared *
                                                                (dx * dx + dySquared) -
                                                                dxFocusYIncrement * dxFocusYIncrement))
                                                                ) * preComputeMultiplyIncludeLookup)
                                                        );

                                                    // change for color index calculation
                                                    dx++;
                                                    dxFocusYIncrement += focusY;
                                                    #endregion
                                                    BufferData[startXPosition] = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                    startXPosition++;
                                                }
                                            }
                                            else
                                            {
                                                calculatedCoverage = (byte)scLastCoverage;
                                                while (startXPosition < lastXPosition)
                                                {

                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)
                                                        ((((dx * focusX) + dyFocusY +
                                                            Math.Sqrt(Math.Abs(
                                                                radiusSquared *
                                                                (dx * dx + dySquared) -
                                                                dxFocusYIncrement * dxFocusYIncrement))
                                                                ) * preComputeMultiplyIncludeLookup)
                                                        );

                                                    // change for color index calculation
                                                    dx++;
                                                    dxFocusYIncrement += focusY;
                                                    #endregion

                                                    colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                    //calculatedCoverage = (byte)((colorData >> 24));
                                                    //calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                    if (calculatedCoverage >= 254)
                                                    {
                                                        BufferData[startXPosition] = colorData;
                                                    }
                                                    else
                                                    {
                                                        // blend here
                                                        dst = BufferData[startXPosition];
                                                        dstRB = dst & 0x00FF00FF;
                                                        dstG = (dst >> 8) & 0xFF;

                                                        BufferData[startXPosition] =
                                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                    }
                                                    startXPosition++;

                                                }
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        //currentXPosition = currentCellData.X;
                                        //currentColorIndexValue =
                                        //    (int)(Math.Sqrt(dyFocusY +
                                        //        (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                        #region prepare for row color index calculation
                                        // get current color index value
                                        dx = (currentCellData.X - absoluteFocusX);
                                        dxFocusYIncrement = (dx * focusY - dyFocusX);
                                        #endregion

                                        #region calculate color index
                                        currentColorIndexValue =
                                            (int)
                                            ((((dx * focusX) + dyFocusY +
                                                Math.Sqrt(Math.Abs(
                                                    radiusSquared *
                                                    (dx * dx + dySquared) -
                                                    dxFocusYIncrement * dxFocusYIncrement))
                                                    ) * preComputeMultiplyIncludeLookup)
                                            );
                                        #endregion
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        //calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        //tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        dst = BufferData[startXPosition];
                                        dstRB = dst & 0x00FF00FF;
                                        dstG = (dst >> 8) & 0xFF;
                                        BufferData[startXPosition] =
                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        #region cumpute value for row
                        //dyFocusY = (startRowIndex - centerY) * (startRowIndex - centerY);
                        dy = (startRowIndex - absoluteFocusY);
                        dySquared = dy * dy;
                        dyFocusX = dy * focusX;
                        dyFocusY = dy * focusY;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region non zero checking code
                                        if (scLastCoverage > 255) scLastCoverage = 255;
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            #region prepare for row color index calculation
                                            // get current color index value
                                            dx = (scLastX + 1 - absoluteFocusX);
                                            dxFocusYIncrement = (dx * focusY - dyFocusX);
                                            #endregion

                                            if (scLastCoverage >= 255)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)
                                                        ((((dx * focusX) + dyFocusY +
                                                            Math.Sqrt(Math.Abs(
                                                                radiusSquared *
                                                                (dx * dx + dySquared) -
                                                                dxFocusYIncrement * dxFocusYIncrement))
                                                                ) * preComputeMultiplyIncludeLookup)
                                                        );

                                                    // change for color index calculation
                                                    dx++;
                                                    dxFocusYIncrement += focusY;
                                                    #endregion

                                                    BufferData[startXPosition] = builtColors[currentColorIndexValue < 0 ? 0 : currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                    startXPosition++;
                                                }
                                            }
                                            else
                                            {
                                                calculatedCoverage = (byte)(scLastCoverage);
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)
                                                        ((((dx * focusX) + dyFocusY +
                                                            Math.Sqrt(Math.Abs(
                                                                radiusSquared *
                                                                (dx * dx + dySquared) -
                                                                dxFocusYIncrement * dxFocusYIncrement))
                                                                ) * preComputeMultiplyIncludeLookup)
                                                        );

                                                    // change for color index calculation
                                                    dx++;
                                                    dxFocusYIncrement += focusY;
                                                    #endregion
                                                    colorData = builtColors[currentColorIndexValue < 0 ? 0 : currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                    //calculatedCoverage = (byte)((colorData >> 24));
                                                    //calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                    if (calculatedCoverage >= 254)
                                                    {
                                                        BufferData[startXPosition] = colorData;
                                                    }
                                                    else
                                                    {
                                                        // blend here
                                                        dst = BufferData[startXPosition];
                                                        dstRB = dst & 0x00FF00FF;
                                                        dstG = (dst >> 8) & 0xFF;

                                                        BufferData[startXPosition] =
                                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                    }
                                                    startXPosition++;
                                                }
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        #region prepare for row color index calculation
                                        // get current color index value
                                        dx = (currentCellData.X - absoluteFocusX);
                                        dxFocusYIncrement = (dx * focusY - dyFocusX);
                                        #endregion

                                        #region calculate color index
                                        currentColorIndexValue =
                                            (int)
                                            ((((dx * focusX) + dyFocusY +
                                                Math.Sqrt(Math.Abs(
                                                    radiusSquared *
                                                    (dx * dx + dySquared) -
                                                    dxFocusYIncrement * dxFocusYIncrement))
                                                    ) * preComputeMultiplyIncludeLookup)
                                            );
                                        #endregion
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue < 0 ? 0 : currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                        //calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        //tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        dst = BufferData[startXPosition];
                                        dstRB = dst & 0x00FF00FF;
                                        dstG = (dst >> 8) & 0xFF;
                                        BufferData[startXPosition] =
                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
            }
            else
            {
                // when no need to blending, when draw a horizontal line
                // do not need check the back color, alway setup
                if (radial.Style != GradientStyle.Pad)
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        #region cumpute value for row
                        //dyFocusY = (startRowIndex - centerY) * (startRowIndex - centerY);
                        dy = (startRowIndex - absoluteFocusY);
                        dySquared = dy * dy;
                        dyFocusX = dy * focusX;
                        dyFocusY = dy * focusY;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region non zero checking code
                                        if (scLastCoverage > 255) scLastCoverage = 255;
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            #region prepare for row color index calculation
                                            // get current color index value
                                            dx = (scLastX + 1 - absoluteFocusX);
                                            dxFocusYIncrement = (dx * focusY - dyFocusX);
                                            #endregion


                                            while (startXPosition < lastXPosition)
                                            {
                                                #region calculate color index
                                                currentColorIndexValue =
                                                    (int)
                                                    ((((dx * focusX) + dyFocusY +
                                                        Math.Sqrt(Math.Abs(
                                                            radiusSquared *
                                                            (dx * dx + dySquared) -
                                                            dxFocusYIncrement * dxFocusYIncrement))
                                                            ) * preComputeMultiplyIncludeLookup)
                                                    );

                                                // change for color index calculation
                                                dx++;
                                                dxFocusYIncrement += focusY;
                                                #endregion
                                                colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                calculatedCoverage = (byte)((colorData >> 24));
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                if (calculatedCoverage >= 254)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    // blend here
                                                    dst = BufferData[startXPosition];
                                                    dstRB = dst & 0x00FF00FF;
                                                    dstG = (dst >> 8) & 0xFF;

                                                    BufferData[startXPosition] =
                                                        (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                        | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                }
                                                startXPosition++;
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion
                                        // get current color data
                                        #region prepare for row color index calculation
                                        // get current color index value
                                        dx = (currentCellData.X - absoluteFocusX);
                                        dxFocusYIncrement = (dx * focusY - dyFocusX);
                                        #endregion

                                        #region calculate color index
                                        currentColorIndexValue =
                                            (int)
                                            ((((dx * focusX) + dyFocusY +
                                                Math.Sqrt(Math.Abs(
                                                    radiusSquared *
                                                    (dx * dx + dySquared) -
                                                    dxFocusYIncrement * dxFocusYIncrement))
                                                    ) * preComputeMultiplyIncludeLookup)
                                            );
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        //if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        dst = BufferData[startXPosition];
                                        dstRB = dst & 0x00FF00FF;
                                        dstG = (dst >> 8) & 0xFF;
                                        BufferData[startXPosition] =
                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        #region cumpute value for row
                        //dyFocusY = (startRowIndex - centerY) * (startRowIndex - centerY);
                        dy = (startRowIndex - absoluteFocusY);
                        dySquared = dy * dy;
                        dyFocusX = dy * focusX;
                        dyFocusY = dy * focusY;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region non zero checking code
                                        if (scLastCoverage > 255) scLastCoverage = 255;
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            #region prepare for row color index calculation
                                            // get current color index value
                                            dx = (scLastX + 1 - absoluteFocusX);
                                            dxFocusYIncrement = (dx * focusY - dyFocusX);
                                            #endregion


                                            while (startXPosition < lastXPosition)
                                            {
                                                #region calculate color index
                                                currentColorIndexValue =
                                                    (int)
                                                    ((((dx * focusX) + dyFocusY +
                                                        Math.Sqrt(Math.Abs(
                                                            radiusSquared *
                                                            (dx * dx + dySquared) -
                                                            dxFocusYIncrement * dxFocusYIncrement))
                                                            ) * preComputeMultiplyIncludeLookup)
                                                    );

                                                // change for color index calculation
                                                dx++;
                                                dxFocusYIncrement += focusY;
                                                #endregion

                                                colorData = builtColors[currentColorIndexValue < 0 ? 0 : currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                calculatedCoverage = (byte)((colorData >> 24));
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                if (calculatedCoverage >= 254)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    // blend here
                                                    dst = BufferData[startXPosition];
                                                    dstRB = dst & 0x00FF00FF;
                                                    dstG = (dst >> 8) & 0xFF;

                                                    BufferData[startXPosition] =
                                                        (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                        | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                }
                                                startXPosition++;
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion
                                        // get current color data
                                        #region prepare for row color index calculation
                                        // get current color index value
                                        dx = (currentCellData.X - absoluteFocusX);
                                        dxFocusYIncrement = (dx * focusY - dyFocusX);
                                        #endregion

                                        #region calculate color index
                                        currentColorIndexValue =
                                            (int)
                                            ((((dx * focusX) + dyFocusY +
                                                Math.Sqrt(Math.Abs(
                                                    radiusSquared *
                                                    (dx * dx + dySquared) -
                                                    dxFocusYIncrement * dxFocusYIncrement))
                                                    ) * preComputeMultiplyIncludeLookup)
                                            );
                                        #endregion
                                        colorData = builtColors[currentColorIndexValue < 0 ? 0 : currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                        calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        //if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        dst = BufferData[startXPosition];
                                        dstRB = dst & 0x00FF00FF;
                                        dstG = (dst >> 8) & 0xFF;
                                        BufferData[startXPosition] =
                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
            }

            #endregion
        }
        #endregion

        #region Fill ellipse, Focus
        /// <summary>
        /// Filling using radial gradient for circle gradient only
        /// </summary>
        /// <param name="radial">radial</param>
        /// <param name="rows">rows</param>
        /// <param name="startRowIndex">start y index</param>
        /// <param name="endRowIndex">end y index</param>
        void FillingEllipseFocal(RadialGradient radial, uint opacity, RowData[] rows, int startRowIndex, int endRowIndex)
        {
            // now not need to check null or not
            uint[] builtColors = radial.GetLinearColors(opacity);
            #region private variable for filling
            int currentCoverage, scLastCoverage, scLastX = 0;
            int tempCover = 0;
            int currentArea = 0;
            int lastXPosition = 0;
            int startXPosition = 0;
            byte calculatedCoverage = 0;


            double centerX = radial.CenterX;
            double centerY = radial.CenterY;
            // in this case radius x = radius y
            double radius = radial.RadiusX;
            double radiusYForX = radial.RadiusY / radial.RadiusX;


            // this is precompute value so that (* ColorIndexScale / radius) now just ( * preComputeRadiusLookup )
            double preComputeRadiusLookup = ColorIndexScale / radius;

            CellData currentCellData = null;
            uint colorData = 0;

            double dx = 0, dy = 0;

            double dySquared = 0; // saving dy * dy
            // focus is changed to relative from the center
            double absoluteFocusX = radial.FocusX;
            double absoluteFocusY = radial.FocusY;

            double focusX = radial.FocusX - centerX;
            double focusY = radial.FocusY - centerY;
            focusY = focusY / radiusYForX;

            // note that dx,dy need to move center
            /*
             *  dx = (currentXPosition - absoluteFocusX);
             *  dy = (startRowIndex - absoluteFocusY);
             *  currentColorIndexValue =
                    (int)
                    (
                        (
                            (
                            (dx * focusX) + (dy * focusY)
                            + Math.Sqrt
                            (
                                Math.Abs
                                (
                                    radius * radius * (dx * dx + dy * dy) - (dx * focusY - dy * focusX) * (dx * focusY - dy * focusX)      
                                )
                            )
                        ) * (radius /
                        ((radius * radius) - ((focusX * focusX )+ (focusY * focusY))))
                    ) * 256 /radius
                );
             */

            //note that  ( radius / (( radius * radius) - ((focusX * focusX) + (focusY * focusY))) is const
            // so that need to pre compute
            double preComputeMultiply = radius / ((radius * radius) - ((focusX * focusX) + (focusY * focusY)));

            #region modify when pre compute for multiply is zero
            if (preComputeMultiply == 0)
            {
                if (focusX != 0)
                {
                    if (focusX < 0)
                    {
                        focusX += GradientAdjustment;
                    }
                    else
                    {
                        focusX -= GradientAdjustment;
                    }
                }
                if (focusY != 0)
                {
                    if (focusY < 0)
                    {
                        focusY += GradientAdjustment;
                    }
                    else
                    {
                        focusY -= GradientAdjustment;
                    }
                }
                preComputeMultiply = radius / ((radius * radius) - ((focusX * focusX) + (focusY * focusY)));
            }
            #endregion

            double preComputeMultiplyIncludeLookup = preComputeRadiusLookup * preComputeMultiply;

            // saving dy * focusY
            double dyFocusY = 0;
            double dyFocusX = 0;
            double dxFocusYIncrement = 0; // saving dx * focusY - dyFocusX
            double radiusSquared = radius * radius;


            int currentColorIndexValue = 0;
            //int currentXPosition = 0;
            uint dst, dstRB, dstG;
            #endregion

            #region FILLING
            if (radial.Ramp.NoBlendingColor)
            {
                // when no need to blending, when draw a horizontal line
                // do not need check the back color, alway setup
                if (radial.Style != GradientStyle.Pad)
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;

                        #region cumpute value for row
                        //dyFocusY = (startRowIndex - centerY) * (startRowIndex - centerY);
                        dy = ((startRowIndex - centerY) / radiusYForX) - focusY;
                        dySquared = dy * dy;
                        dyFocusX = dy * focusX;
                        dyFocusY = dy * focusY;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region non zero checking code
                                        if (scLastCoverage > 255) scLastCoverage = 255;
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;

                                            #region prepare for row color index calculation
                                            // get current color index value
                                            //currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                            //currentXPosition = scLastX + 1;
                                            dx = (scLastX + 1 - absoluteFocusX);
                                            dxFocusYIncrement = (dx * focusY - dyFocusX);
                                            #endregion
                                            if (scLastCoverage >= 255)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)
                                                        ((((dx * focusX) + dyFocusY +
                                                            Math.Sqrt(Math.Abs(
                                                                radiusSquared *
                                                                (dx * dx + dySquared) -
                                                                dxFocusYIncrement * dxFocusYIncrement))
                                                                ) * preComputeMultiplyIncludeLookup)
                                                        );

                                                    //currentColorIndexValue =
                                                    //    (int)
                                                    //    (
                                                    //        (
                                                    //            (
                                                    //            (dx * focusX) + (dy * focusY)
                                                    //            + Math.Sqrt
                                                    //            (
                                                    //                Math.Abs
                                                    //                (
                                                    //                    radius * radius 
                                                    //                    * (dx * dx + dy * dy) 
                                                    //                    - (dx * focusY - dy * focusX) 
                                                    //                    * (dx * focusY - dy * focusX)
                                                    //                )
                                                    //            )
                                                    //        ) * (radius /
                                                    //        ((radius * radius) - ((focusX * focusX) + (focusY * focusY))))
                                                    //        ) * 256 / radius
                                                    //    );

                                                    // change for color index calculation
                                                    dx++;
                                                    dxFocusYIncrement += focusY;
                                                    #endregion
                                                    BufferData[startXPosition] = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                    startXPosition++;
                                                }
                                            }
                                            else
                                            {
                                                calculatedCoverage = (byte)scLastCoverage;
                                                while (startXPosition < lastXPosition)
                                                {

                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)
                                                        ((((dx * focusX) + dyFocusY +
                                                            Math.Sqrt(Math.Abs(
                                                                radiusSquared *
                                                                (dx * dx + dySquared) -
                                                                dxFocusYIncrement * dxFocusYIncrement))
                                                                ) * preComputeMultiplyIncludeLookup)
                                                        );

                                                    // change for color index calculation
                                                    dx++;
                                                    dxFocusYIncrement += focusY;
                                                    #endregion

                                                    colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                    //calculatedCoverage = (byte)((colorData >> 24));
                                                    //calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                    if (calculatedCoverage >= 254)
                                                    {
                                                        BufferData[startXPosition] = colorData;
                                                    }
                                                    else
                                                    {
                                                        // blend here
                                                        dst = BufferData[startXPosition];
                                                        dstRB = dst & 0x00FF00FF;
                                                        dstG = (dst >> 8) & 0xFF;

                                                        BufferData[startXPosition] =
                                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                    }
                                                    startXPosition++;

                                                }
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        //currentXPosition = currentCellData.X;
                                        //currentColorIndexValue =
                                        //    (int)(Math.Sqrt(dyFocusY +
                                        //        (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                        #region prepare for row color index calculation
                                        // get current color index value
                                        dx = (currentCellData.X - absoluteFocusX);
                                        dxFocusYIncrement = (dx * focusY - dyFocusX);
                                        #endregion

                                        #region calculate color index
                                        currentColorIndexValue =
                                            (int)
                                            ((((dx * focusX) + dyFocusY +
                                                Math.Sqrt(Math.Abs(
                                                    radiusSquared *
                                                    (dx * dx + dySquared) -
                                                    dxFocusYIncrement * dxFocusYIncrement))
                                                    ) * preComputeMultiplyIncludeLookup)
                                            );
                                        #endregion
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        //calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        //tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        dst = BufferData[startXPosition];
                                        dstRB = dst & 0x00FF00FF;
                                        dstG = (dst >> 8) & 0xFF;
                                        BufferData[startXPosition] =
                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        #region cumpute value for row
                        //dyFocusY = (startRowIndex - centerY) * (startRowIndex - centerY);
                        dy = ((startRowIndex - centerY) / radiusYForX) - focusY;
                        dySquared = dy * dy;
                        dyFocusX = dy * focusX;
                        dyFocusY = dy * focusY;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region non zero checking code
                                        if (scLastCoverage > 255) scLastCoverage = 255;
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            #region prepare for row color index calculation
                                            // get current color index value
                                            dx = (scLastX + 1 - absoluteFocusX);
                                            dxFocusYIncrement = (dx * focusY - dyFocusX);
                                            #endregion

                                            if (scLastCoverage >= 255)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)
                                                        ((((dx * focusX) + dyFocusY +
                                                            Math.Sqrt(Math.Abs(
                                                                radiusSquared *
                                                                (dx * dx + dySquared) -
                                                                dxFocusYIncrement * dxFocusYIncrement))
                                                                ) * preComputeMultiplyIncludeLookup)
                                                        );

                                                    // change for color index calculation
                                                    dx++;
                                                    dxFocusYIncrement += focusY;
                                                    #endregion

                                                    BufferData[startXPosition] = builtColors[currentColorIndexValue < 0 ? 0 : currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                    startXPosition++;
                                                }
                                            }
                                            else
                                            {
                                                calculatedCoverage = (byte)(scLastCoverage);
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)
                                                        ((((dx * focusX) + dyFocusY +
                                                            Math.Sqrt(Math.Abs(
                                                                radiusSquared *
                                                                (dx * dx + dySquared) -
                                                                dxFocusYIncrement * dxFocusYIncrement))
                                                                ) * preComputeMultiplyIncludeLookup)
                                                        );

                                                    // change for color index calculation
                                                    dx++;
                                                    dxFocusYIncrement += focusY;
                                                    #endregion
                                                    colorData = builtColors[currentColorIndexValue < 0 ? 0 : currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                    //calculatedCoverage = (byte)((colorData >> 24));
                                                    //calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                    if (calculatedCoverage >= 254)
                                                    {
                                                        BufferData[startXPosition] = colorData;
                                                    }
                                                    else
                                                    {
                                                        // blend here
                                                        dst = BufferData[startXPosition];
                                                        dstRB = dst & 0x00FF00FF;
                                                        dstG = (dst >> 8) & 0xFF;

                                                        BufferData[startXPosition] =
                                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                    }
                                                    startXPosition++;
                                                }
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        #region prepare for row color index calculation
                                        // get current color index value
                                        dx = (currentCellData.X - absoluteFocusX);
                                        dxFocusYIncrement = (dx * focusY - dyFocusX);
                                        #endregion

                                        #region calculate color index
                                        currentColorIndexValue =
                                            (int)
                                            ((((dx * focusX) + dyFocusY +
                                                Math.Sqrt(Math.Abs(
                                                    radiusSquared *
                                                    (dx * dx + dySquared) -
                                                    dxFocusYIncrement * dxFocusYIncrement))
                                                    ) * preComputeMultiplyIncludeLookup)
                                            );
                                        #endregion
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue < 0 ? 0 : currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                        //calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        //tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        dst = BufferData[startXPosition];
                                        dstRB = dst & 0x00FF00FF;
                                        dstG = (dst >> 8) & 0xFF;
                                        BufferData[startXPosition] =
                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
            }
            else
            {
                // when no need to blending, when draw a horizontal line
                // do not need check the back color, alway setup
                if (radial.Style != GradientStyle.Pad)
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        #region cumpute value for row
                        //dyFocusY = (startRowIndex - centerY) * (startRowIndex - centerY);
                        dy = ((startRowIndex - centerY) / radiusYForX) - focusY;
                        dySquared = dy * dy;
                        dyFocusX = dy * focusX;
                        dyFocusY = dy * focusY;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region non zero checking code
                                        if (scLastCoverage > 255) scLastCoverage = 255;
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            #region prepare for row color index calculation
                                            // get current color index value
                                            dx = (scLastX + 1 - absoluteFocusX);
                                            dxFocusYIncrement = (dx * focusY - dyFocusX);
                                            #endregion


                                            while (startXPosition < lastXPosition)
                                            {
                                                #region calculate color index
                                                currentColorIndexValue =
                                                    (int)
                                                    ((((dx * focusX) + dyFocusY +
                                                        Math.Sqrt(Math.Abs(
                                                            radiusSquared *
                                                            (dx * dx + dySquared) -
                                                            dxFocusYIncrement * dxFocusYIncrement))
                                                            ) * preComputeMultiplyIncludeLookup)
                                                    );

                                                // change for color index calculation
                                                dx++;
                                                dxFocusYIncrement += focusY;
                                                #endregion
                                                colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                calculatedCoverage = (byte)((colorData >> 24));
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                if (calculatedCoverage >= 254)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    // blend here
                                                    dst = BufferData[startXPosition];
                                                    dstRB = dst & 0x00FF00FF;
                                                    dstG = (dst >> 8) & 0xFF;

                                                    BufferData[startXPosition] =
                                                        (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                        | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                }
                                                startXPosition++;
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion
                                        // get current color data
                                        #region prepare for row color index calculation
                                        // get current color index value
                                        dx = (currentCellData.X - absoluteFocusX);
                                        dxFocusYIncrement = (dx * focusY - dyFocusX);
                                        #endregion

                                        #region calculate color index
                                        currentColorIndexValue =
                                            (int)
                                            ((((dx * focusX) + dyFocusY +
                                                Math.Sqrt(Math.Abs(
                                                    radiusSquared *
                                                    (dx * dx + dySquared) -
                                                    dxFocusYIncrement * dxFocusYIncrement))
                                                    ) * preComputeMultiplyIncludeLookup)
                                            );
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        //if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        dst = BufferData[startXPosition];
                                        dstRB = dst & 0x00FF00FF;
                                        dstG = (dst >> 8) & 0xFF;
                                        BufferData[startXPosition] =
                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        #region cumpute value for row
                        //dyFocusY = (startRowIndex - centerY) * (startRowIndex - centerY);
                        dy = ((startRowIndex - centerY) / radiusYForX) - focusY;
                        dySquared = dy * dy;
                        dyFocusX = dy * focusX;
                        dyFocusY = dy * focusY;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region non zero checking code
                                        if (scLastCoverage > 255) scLastCoverage = 255;
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            #region prepare for row color index calculation
                                            // get current color index value
                                            dx = (scLastX + 1 - absoluteFocusX);
                                            dxFocusYIncrement = (dx * focusY - dyFocusX);
                                            #endregion


                                            while (startXPosition < lastXPosition)
                                            {
                                                #region calculate color index
                                                currentColorIndexValue =
                                                    (int)
                                                    ((((dx * focusX) + dyFocusY +
                                                        Math.Sqrt(Math.Abs(
                                                            radiusSquared *
                                                            (dx * dx + dySquared) -
                                                            dxFocusYIncrement * dxFocusYIncrement))
                                                            ) * preComputeMultiplyIncludeLookup)
                                                    );

                                                // change for color index calculation
                                                dx++;
                                                dxFocusYIncrement += focusY;
                                                #endregion

                                                colorData = builtColors[currentColorIndexValue < 0 ? 0 : currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                calculatedCoverage = (byte)((colorData >> 24));
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                if (calculatedCoverage >= 254)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    // blend here
                                                    dst = BufferData[startXPosition];
                                                    dstRB = dst & 0x00FF00FF;
                                                    dstG = (dst >> 8) & 0xFF;

                                                    BufferData[startXPosition] =
                                                        (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                        | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                }
                                                startXPosition++;
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion
                                        // get current color data
                                        #region prepare for row color index calculation
                                        // get current color index value
                                        dx = (currentCellData.X - absoluteFocusX);
                                        dxFocusYIncrement = (dx * focusY - dyFocusX);
                                        #endregion

                                        #region calculate color index
                                        currentColorIndexValue =
                                            (int)
                                            ((((dx * focusX) + dyFocusY +
                                                Math.Sqrt(Math.Abs(
                                                    radiusSquared *
                                                    (dx * dx + dySquared) -
                                                    dxFocusYIncrement * dxFocusYIncrement))
                                                    ) * preComputeMultiplyIncludeLookup)
                                            );
                                        #endregion
                                        colorData = builtColors[currentColorIndexValue < 0 ? 0 : currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                        calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        //if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        dst = BufferData[startXPosition];
                                        dstRB = dst & 0x00FF00FF;
                                        dstG = (dst >> 8) & 0xFF;
                                        BufferData[startXPosition] =
                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
            }

            #endregion
        }
        #endregion
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
            // this base on paint to filling
            if (!(paint.Paint is RadialGradient))
            {
                //throw new NotImplementedException("Support color paint only");
                NotMatchPaintTypeException.Publish(typeof(RadialGradient), paint.Paint.GetType());
                return;
            }
            RadialGradient radial = paint.Paint as RadialGradient;
            if (radial.RadiusX == radial.RadiusY)
            {
                if ((radial.FocusX == radial.CenterX) && (radial.FocusY == radial.CenterY))
                {
                    // when normal radial gradient
                    FillingRadial(radial, paint.ScaledOpacity, rows, startRowIndex, endRowIndex, gammaLutRed, gammaLutGreen, gammaLutBlue);
                }
                else
                {
                    // circle and focus gradient
                    FillingRadialFocal(radial, paint.ScaledOpacity, rows, startRowIndex, endRowIndex, gammaLutRed, gammaLutGreen, gammaLutBlue);
                }
            }
            else
            {
                if ((radial.FocusX == radial.CenterX) && (radial.FocusY == radial.CenterY))
                {
                    // when normal ellipse gradient
                    FillingEllipse(radial, paint.ScaledOpacity, rows, startRowIndex, endRowIndex, gammaLutRed, gammaLutGreen, gammaLutBlue);
                }
                else
                {
                    // ellipse and focus gradient
                    FillingEllipseFocal(radial, paint.ScaledOpacity, rows, startRowIndex, endRowIndex, gammaLutRed, gammaLutGreen, gammaLutBlue);
                }
            }
        }


        #region Fill normal, radial
        /// <summary>
        /// Filling using radial gradient for circle gradient only
        /// </summary>
        /// <param name="radial">radial</param>
        /// <param name="rows">rows</param>
        /// <param name="startRowIndex">start y index</param>
        /// <param name="endRowIndex">end y index</param>
        /// <param name="gammaLutRed">gamma look up table for red</param>
        /// <param name="gammaLutGreen">gamma look up table for green</param>
        /// <param name="gammaLutBlue">gamma look up table for blue</param>
        void FillingRadial(RadialGradient radial, uint opacity, RowData[] rows, int startRowIndex, int endRowIndex, byte[] gammaLutRed, byte[] gammaLutGreen, byte[] gammaLutBlue)
        {
            // now not need to check null or not
            uint[] builtColors = radial.GetLinearColors(opacity);
            #region private variable for filling
            int currentCoverage, scLastCoverage, scLastX = 0;
            int tempCover = 0;
            int currentArea = 0;
            int lastXPosition = 0;
            int startXPosition = 0;
            byte calculatedCoverage = 0;

            double centerX = radial.CenterX;
            double centerY = radial.CenterY;
            // in this case radius x = radius y
            double radius = radial.RadiusX;

            // saving precompute value for rows
            /* Normal calculation to get the color index
             * currentColorIndexValue =
                (int)(Math.Sqrt(
                    (startRowIndex - centerY) * (startRowIndex - centerY) +
                    (currentXPosition - centerX) * (currentXPosition - centerX)) * ColorIndexScale / radius );
             * but
             *  preComputeForRow= (startRowIndex - centerY) * (startRowIndex - centerY)
             *  so that
             *    currentColorIndexValue = 
             *    (int)(Math.Sqrt(
                    (preComputeForRow) +
                    (currentXPosition - centerX) * (currentXPosition - centerX)) * ColorIndexScale / radius );
             */
            double preComputeForRow = 0;

            // this is precompute value so that (* ColorIndexScale / radius) now just ( * preComputeRadiusLookup )
            double preComputeRadiusLookup = ColorIndexScale / radius;

            CellData currentCellData = null;
            uint colorData = 0;
            //uint colorG = 0;
            //uint colorRB = 0;


            int currentColorIndexValue = 0;
            int currentXPosition = 0;

            uint dst, dstRB, dstG;
            #endregion

            #region FILLING
            if (radial.Ramp.NoBlendingColor)
            {
                // when no need to blending, when draw a horizontal line
                // do not need check the back color, alway setup
                if (radial.Style != GradientStyle.Pad)
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        preComputeForRow = (startRowIndex - centerY) * (startRowIndex - centerY);
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region non zero checking code
                                        if (scLastCoverage > 255) scLastCoverage = 255;
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            //currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                            currentXPosition = scLastX + 1;
                                            if (scLastCoverage >= 255)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)(Math.Sqrt(
                                                            preComputeForRow +
                                                            (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                                    #endregion
                                                    BufferData[startXPosition] = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                    startXPosition++;
                                                    currentXPosition++;
                                                }
                                            }
                                            else
                                            {
                                                calculatedCoverage = (byte)scLastCoverage;
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)(Math.Sqrt(
                                                            preComputeForRow +
                                                            (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                                    #endregion

                                                    colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                    //calculatedCoverage = (byte)((colorData >> 24));
                                                    //calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                    if (calculatedCoverage >= 254)
                                                    {
                                                        BufferData[startXPosition] = colorData;
                                                    }
                                                    else
                                                    {
                                                        #region gamma apply
                                                        dst = BufferData[startXPosition];
                                                        dstG = (dst >> 8) & 0xFF;
                                                        dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                        BufferData[startXPosition] =
                                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                            | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                            | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                            ;
                                                        #endregion
                                                    }
                                                    startXPosition++;
                                                    currentXPosition++;
                                                }
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        currentXPosition = currentCellData.X;
                                        currentColorIndexValue =
                                            (int)(Math.Sqrt(preComputeForRow +
                                                (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        //calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        //tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        //dst = BufferData[startXPosition];
                                        //dstRB = dst & 0x00FF00FF;
                                        //dstG = (dst >> 8) & 0xFF;
                                        //BufferData[startXPosition] =
                                        //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        if (calculatedCoverage >= 254)
                                        {
                                            BufferData[startXPosition] = colorData;
                                        }
                                        else
                                        {
                                            #region gamma apply
                                            dst = BufferData[startXPosition];
                                            dstG = (dst >> 8) & 0xFF;
                                            dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                            BufferData[startXPosition] =
                                                (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                ;
                                            #endregion
                                        }
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        preComputeForRow = (startRowIndex - centerY) * (startRowIndex - centerY);
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region non zero checking code
                                        if (scLastCoverage > 255) scLastCoverage = 255;
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            //currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                            currentXPosition = scLastX + 1;
                                            if (scLastCoverage >= 255)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)(Math.Sqrt(
                                                            preComputeForRow +
                                                            (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                                    #endregion
                                                    BufferData[startXPosition] = builtColors[currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                    startXPosition++;
                                                    currentXPosition++;
                                                }
                                            }
                                            else
                                            {
                                                calculatedCoverage = (byte)(scLastCoverage);
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)(Math.Sqrt(
                                                            preComputeForRow +
                                                            (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                                    #endregion

                                                    colorData = builtColors[currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                    //calculatedCoverage = (byte)((colorData >> 24));
                                                    //calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                    if (calculatedCoverage >= 254)
                                                    {
                                                        BufferData[startXPosition] = colorData;
                                                    }
                                                    else
                                                    {
                                                        //// blend here
                                                        //dst = BufferData[startXPosition];
                                                        //dstRB = dst & 0x00FF00FF;
                                                        //dstG = (dst >> 8) & 0xFF;

                                                        //BufferData[startXPosition] =
                                                        //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                        //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                        #region gamma apply
                                                        dst = BufferData[startXPosition];
                                                        dstG = (dst >> 8) & 0xFF;
                                                        dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                        BufferData[startXPosition] =
                                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                            | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                            | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                            ;
                                                        #endregion
                                                    }
                                                    startXPosition++;
                                                    currentXPosition++;
                                                }
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        currentXPosition = currentCellData.X;
                                        currentColorIndexValue =
                                            (int)(Math.Sqrt(preComputeForRow +
                                                (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue > 254 ? 255 : currentColorIndexValue];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        //calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        //tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        //dst = BufferData[startXPosition];
                                        //dstRB = dst & 0x00FF00FF;
                                        //dstG = (dst >> 8) & 0xFF;
                                        //BufferData[startXPosition] =
                                        //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        if (calculatedCoverage >= 254)
                                        {
                                            BufferData[startXPosition] = colorData;
                                        }
                                        else
                                        {
                                            #region gamma apply
                                            dst = BufferData[startXPosition];
                                            dstG = (dst >> 8) & 0xFF;
                                            dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                            BufferData[startXPosition] =
                                                (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                ;
                                            #endregion
                                        }
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
            }
            else
            {
                // when no need to blending, when draw a horizontal line
                // do not need check the back color, alway setup
                if (radial.Style != GradientStyle.Pad)
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        preComputeForRow = (startRowIndex - centerY) * (startRowIndex - centerY);
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region non zero checking code
                                        if (scLastCoverage > 255) scLastCoverage = 255;
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            //currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                            currentXPosition = scLastX + 1;

                                            while (startXPosition < lastXPosition)
                                            {
                                                #region calculate color index
                                                currentColorIndexValue =
                                                    (int)(Math.Sqrt(
                                                        preComputeForRow +
                                                        (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                                #endregion

                                                colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                calculatedCoverage = (byte)((colorData >> 24));
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                if (calculatedCoverage >= 254)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    #region gamma apply
                                                    dst = BufferData[startXPosition];
                                                    dstG = (dst >> 8) & 0xFF;
                                                    dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                    BufferData[startXPosition] =
                                                        (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                        | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                        | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                        ;
                                                    #endregion
                                                }
                                                startXPosition++;
                                                currentXPosition++;
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        currentXPosition = currentCellData.X;
                                        currentColorIndexValue =
                                            (int)(Math.Sqrt(preComputeForRow +
                                                (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        //if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        //dst = BufferData[startXPosition];
                                        //dstRB = dst & 0x00FF00FF;
                                        //dstG = (dst >> 8) & 0xFF;
                                        //BufferData[startXPosition] =
                                        //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #region gamma apply
                                        dst = BufferData[startXPosition];
                                        dstG = (dst >> 8) & 0xFF;
                                        dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                        BufferData[startXPosition] =
                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                            | (gammaLutBlue[(dstRB & 0x00FF)]))
                                            ;
                                        #endregion
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        preComputeForRow = (startRowIndex - centerY) * (startRowIndex - centerY);
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region non zero checking code
                                        if (scLastCoverage > 255) scLastCoverage = 255;
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            //currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                            currentXPosition = scLastX + 1;

                                            while (startXPosition < lastXPosition)
                                            {
                                                #region calculate color index
                                                currentColorIndexValue =
                                                    (int)(Math.Sqrt(
                                                        preComputeForRow +
                                                        (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                                #endregion

                                                colorData = builtColors[currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                calculatedCoverage = (byte)((colorData >> 24));
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                if (calculatedCoverage >= 254)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    //// blend here
                                                    //dst = BufferData[startXPosition];
                                                    //dstRB = dst & 0x00FF00FF;
                                                    //dstG = (dst >> 8) & 0xFF;

                                                    //BufferData[startXPosition] =
                                                    //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                    //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                    //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                                    #region gamma apply
                                                    dst = BufferData[startXPosition];
                                                    dstG = (dst >> 8) & 0xFF;
                                                    dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                    BufferData[startXPosition] =
                                                        (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                        | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                        | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                        ;
                                                    #endregion
                                                }
                                                startXPosition++;
                                                currentXPosition++;
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        currentXPosition = currentCellData.X;
                                        currentColorIndexValue =
                                            (int)(Math.Sqrt(preComputeForRow +
                                                (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue > 254 ? 255 : currentColorIndexValue];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        //if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;
                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        //dst = BufferData[startXPosition];
                                        //dstRB = dst & 0x00FF00FF;
                                        //dstG = (dst >> 8) & 0xFF;
                                        //BufferData[startXPosition] =
                                        //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #region gamma apply
                                        dst = BufferData[startXPosition];
                                        dstG = (dst >> 8) & 0xFF;
                                        dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                        BufferData[startXPosition] =
                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                            | (gammaLutBlue[(dstRB & 0x00FF)]))
                                            ;
                                        #endregion
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
            }

            #endregion
        }
        #endregion

        #region fill ellipse
        /// <summary>
        /// Filling using ellipse gradient for circle gradient only
        /// </summary>
        /// <param name="radial">radial</param>
        /// <param name="rows">rows</param>
        /// <param name="startRowIndex">start y index</param>
        /// <param name="endRowIndex">end y index</param>
        /// <param name="gammaLutRed">gamma look up table for red</param>
        /// <param name="gammaLutGreen">gamma look up table for green</param>
        /// <param name="gammaLutBlue">gamma look up table for blue</param>
        void FillingEllipse(RadialGradient radial, uint opacity, RowData[] rows, int startRowIndex, int endRowIndex, byte[] gammaLutRed, byte[] gammaLutGreen, byte[] gammaLutBlue)
        {
            // now not need to check null or not
            uint[] builtColors = radial.GetLinearColors(opacity);
            #region private variable for filling
            int currentCoverage, scLastCoverage, scLastX = 0;
            int tempCover = 0;
            int currentArea = 0;
            int lastXPosition = 0;
            int startXPosition = 0;
            byte calculatedCoverage = 0;

            double centerX = radial.CenterX;
            double centerY = radial.CenterY;
            // in this case radius x = radius y
            double radius = radial.RadiusX;

            // saving precompute value for rows
            /* Normal calculation to get the color index
             *  currentColorIndexValue =
                    (int)(Math.Sqrt(  
                            ((currentXPosition-centerX) * (currentXPosition-centerX) /(radial.RadiusX * radial.RadiusX))
                            +
                            ((startRowIndex - centerY) * (startRowIndex - centerY) )/(radial.RadiusY * radial.RadiusY))
                        * 256);
             * but
             *  preComputeForRow= (startRowIndex - centerY) * (startRowIndex - centerY)
             *  so that
             *    currentColorIndexValue = 
             *    (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX)/rx*rx + (preComputeForRow) +));
             */
            double preComputeForRow = 0;

            // this is precompute value so that (* ColorIndexScale / radius) now just ( * preComputeRadiusLookup )
            //double preComputeRadiusLookup = ColorIndexScale / radius;
            double radiusY = radial.RadiusY;
            double radiusX = radial.RadiusX;
            double radiusYSquared = 1 / (radiusY * radiusY);
            double radiusXSquared = 1 / (radiusX * radiusX);

            CellData currentCellData = null;
            uint colorData = 0;
            //uint colorG = 0;
            //uint colorRB = 0;


            int currentColorIndexValue = 0;
            int currentXPosition = 0;

            uint dst, dstRB, dstG;
            #endregion

            #region FILLING
            if (radial.Ramp.NoBlendingColor)
            {
                // when no need to blending, when draw a horizontal line
                // do not need check the back color, alway setup
                if (radial.Style != GradientStyle.Pad)
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        #region precompute for row
                        preComputeForRow = ((startRowIndex - centerY) * (startRowIndex - centerY)) * radiusYSquared;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region non zero checking code
                                        if (scLastCoverage > 255) scLastCoverage = 255;
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            //currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                            currentXPosition = scLastX + 1;
                                            if (scLastCoverage >= 255)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue = (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX) * radiusXSquared) + preComputeForRow) * ColorIndexScale);
                                                    #endregion
                                                    BufferData[startXPosition] = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                    startXPosition++;
                                                    currentXPosition++;
                                                }
                                            }
                                            else
                                            {
                                                calculatedCoverage = (byte)scLastCoverage;
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue = (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX) * radiusXSquared) + preComputeForRow) * ColorIndexScale);
                                                    #endregion

                                                    colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                    //calculatedCoverage = (byte)((colorData >> 24));
                                                    //calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                    if (calculatedCoverage >= 254)
                                                    {
                                                        BufferData[startXPosition] = colorData;
                                                    }
                                                    else
                                                    {
                                                        // blend here
                                                        //dst = BufferData[startXPosition];
                                                        //dstRB = dst & 0x00FF00FF;
                                                        //dstG = (dst >> 8) & 0xFF;

                                                        //BufferData[startXPosition] =
                                                        //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                        //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                        #region gamma apply
                                                        dst = BufferData[startXPosition];
                                                        dstG = (dst >> 8) & 0xFF;
                                                        dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                        BufferData[startXPosition] =
                                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                            | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                            | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                            ;
                                                        #endregion
                                                    }
                                                    startXPosition++;
                                                    currentXPosition++;
                                                }
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        currentXPosition = currentCellData.X;
                                        currentColorIndexValue = (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX) * radiusXSquared) + preComputeForRow) * ColorIndexScale);
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        //calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        //tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        //dst = BufferData[startXPosition];
                                        //dstRB = dst & 0x00FF00FF;
                                        //dstG = (dst >> 8) & 0xFF;
                                        //BufferData[startXPosition] =
                                        //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                        if (calculatedCoverage >= 254)
                                        {
                                            BufferData[startXPosition] = colorData;
                                        }
                                        else
                                        {
                                            #region gamma apply
                                            dst = BufferData[startXPosition];
                                            dstG = (dst >> 8) & 0xFF;
                                            dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                            BufferData[startXPosition] =
                                                (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                ;
                                            #endregion
                                        }
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        #region precompute for row
                        preComputeForRow = ((startRowIndex - centerY) * (startRowIndex - centerY)) * radiusYSquared;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region non zero checking code
                                        if (scLastCoverage > 255) scLastCoverage = 255;
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            //currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                            currentXPosition = scLastX + 1;
                                            if (scLastCoverage >= 255)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue = (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX) * radiusXSquared) + preComputeForRow) * ColorIndexScale);
                                                    #endregion
                                                    BufferData[startXPosition] = builtColors[currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                    startXPosition++;
                                                    currentXPosition++;
                                                }
                                            }
                                            else
                                            {
                                                calculatedCoverage = (byte)(scLastCoverage);
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue = (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX) * radiusXSquared) + preComputeForRow) * ColorIndexScale);
                                                    #endregion

                                                    colorData = builtColors[currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                    //calculatedCoverage = (byte)((colorData >> 24));
                                                    //calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                    if (calculatedCoverage >= 254)
                                                    {
                                                        BufferData[startXPosition] = colorData;
                                                    }
                                                    else
                                                    {
                                                        // blend here
                                                        //dst = BufferData[startXPosition];
                                                        //dstRB = dst & 0x00FF00FF;
                                                        //dstG = (dst >> 8) & 0xFF;

                                                        //BufferData[startXPosition] =
                                                        //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                        //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                        #region gamma apply
                                                        dst = BufferData[startXPosition];
                                                        dstG = (dst >> 8) & 0xFF;
                                                        dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                        BufferData[startXPosition] =
                                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                            | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                            | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                            ;
                                                        #endregion
                                                    }
                                                    startXPosition++;
                                                    currentXPosition++;
                                                }
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        currentXPosition = currentCellData.X;
                                        currentColorIndexValue = (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX) * radiusXSquared) + preComputeForRow) * ColorIndexScale);
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue > 254 ? 255 : currentColorIndexValue];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        //calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        //tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        //dst = BufferData[startXPosition];
                                        //dstRB = dst & 0x00FF00FF;
                                        //dstG = (dst >> 8) & 0xFF;
                                        //BufferData[startXPosition] =
                                        //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                        if (calculatedCoverage >= 254)
                                        {
                                            BufferData[startXPosition] = colorData;
                                        }
                                        else
                                        {
                                            #region gamma apply
                                            dst = BufferData[startXPosition];
                                            dstG = (dst >> 8) & 0xFF;
                                            dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                            BufferData[startXPosition] =
                                                (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                ;
                                            #endregion
                                        }
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
            }
            else
            {
                // when no need to blending, when draw a horizontal line
                // do not need check the back color, alway setup
                if (radial.Style != GradientStyle.Pad)
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        #region precompute for row
                        preComputeForRow = ((startRowIndex - centerY) * (startRowIndex - centerY)) * radiusYSquared;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region non zero checking code
                                        if (scLastCoverage > 255) scLastCoverage = 255;
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            //currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                            currentXPosition = scLastX + 1;

                                            while (startXPosition < lastXPosition)
                                            {
                                                #region calculate color index
                                                currentColorIndexValue = (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX) * radiusXSquared) + preComputeForRow) * ColorIndexScale);
                                                #endregion

                                                colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                calculatedCoverage = (byte)((colorData >> 24));
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                if (calculatedCoverage >= 254)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    //// blend here
                                                    //dst = BufferData[startXPosition];
                                                    //dstRB = dst & 0x00FF00FF;
                                                    //dstG = (dst >> 8) & 0xFF;

                                                    //BufferData[startXPosition] =
                                                    //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                    //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                    //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                                    #region gamma apply
                                                    dst = BufferData[startXPosition];
                                                    dstG = (dst >> 8) & 0xFF;
                                                    dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                    BufferData[startXPosition] =
                                                        (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                        | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                        | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                        ;
                                                    #endregion
                                                }
                                                startXPosition++;
                                                currentXPosition++;
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        currentXPosition = currentCellData.X;
                                        currentColorIndexValue = (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX) * radiusXSquared) + preComputeForRow) * ColorIndexScale);
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        //if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        //dst = BufferData[startXPosition];
                                        //dstRB = dst & 0x00FF00FF;
                                        //dstG = (dst >> 8) & 0xFF;
                                        //BufferData[startXPosition] =
                                        //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #region gamma apply
                                        dst = BufferData[startXPosition];
                                        dstG = (dst >> 8) & 0xFF;
                                        dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                        BufferData[startXPosition] =
                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                            | (gammaLutBlue[(dstRB & 0x00FF)]))
                                            ;
                                        #endregion
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        #region precompute for row
                        preComputeForRow = ((startRowIndex - centerY) * (startRowIndex - centerY)) * radiusYSquared;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region non zero checking code
                                        if (scLastCoverage > 255) scLastCoverage = 255;
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            //currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                            currentXPosition = scLastX + 1;

                                            while (startXPosition < lastXPosition)
                                            {
                                                #region calculate color index
                                                currentColorIndexValue = (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX) * radiusXSquared) + preComputeForRow) * ColorIndexScale);
                                                #endregion

                                                colorData = builtColors[currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                calculatedCoverage = (byte)((colorData >> 24));
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                if (calculatedCoverage >= 254)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    //// blend here
                                                    //dst = BufferData[startXPosition];
                                                    //dstRB = dst & 0x00FF00FF;
                                                    //dstG = (dst >> 8) & 0xFF;

                                                    //BufferData[startXPosition] =
                                                    //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                    //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                    //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                    #region gamma apply
                                                    dst = BufferData[startXPosition];
                                                    dstG = (dst >> 8) & 0xFF;
                                                    dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                    BufferData[startXPosition] =
                                                        (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                        | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                        | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                        ;
                                                    #endregion
                                                }
                                                startXPosition++;
                                                currentXPosition++;
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        currentXPosition = currentCellData.X;
                                        currentColorIndexValue = (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX) * radiusXSquared) + preComputeForRow) * ColorIndexScale);
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue > 254 ? 255 : currentColorIndexValue];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        //if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        //dst = BufferData[startXPosition];
                                        //dstRB = dst & 0x00FF00FF;
                                        //dstG = (dst >> 8) & 0xFF;
                                        //BufferData[startXPosition] =
                                        //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #region gamma apply
                                        dst = BufferData[startXPosition];
                                        dstG = (dst >> 8) & 0xFF;
                                        dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                        BufferData[startXPosition] =
                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                            | (gammaLutBlue[(dstRB & 0x00FF)]))
                                            ;
                                        #endregion
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
            }

            #endregion
        }
        #endregion

        #region Fill circle, Focus
        /// <summary>
        /// Filling using radial gradient for circle gradient only
        /// </summary>
        /// <param name="radial">radial</param>
        /// <param name="rows">rows</param>
        /// <param name="startRowIndex">start y index</param>
        /// <param name="endRowIndex">end y index</param>
        /// <param name="gammaLutRed">gamma look up table for red</param>
        /// <param name="gammaLutGreen">gamma look up table for green</param>
        /// <param name="gammaLutBlue">gamma look up table for blue</param>
        void FillingRadialFocal(RadialGradient radial, uint opacity, RowData[] rows, int startRowIndex, int endRowIndex, byte[] gammaLutRed, byte[] gammaLutGreen, byte[] gammaLutBlue)
        {
            // now not need to check null or not
            uint[] builtColors = radial.GetLinearColors(opacity);
            #region private variable for filling
            int currentCoverage, scLastCoverage, scLastX = 0;
            int tempCover = 0;
            int currentArea = 0;
            int lastXPosition = 0;
            int startXPosition = 0;
            byte calculatedCoverage = 0;


            double centerX = radial.CenterX;
            double centerY = radial.CenterY;
            // in this case radius x = radius y
            double radius = radial.RadiusX;

            // this is precompute value so that (* ColorIndexScale / radius) now just ( * preComputeRadiusLookup )
            double preComputeRadiusLookup = ColorIndexScale / radius;

            CellData currentCellData = null;
            uint colorData = 0;

            double dx = 0, dy = 0;

            double dySquared = 0; // saving dy * dy
            // focus is changed to relative from the center
            double absoluteFocusX = radial.FocusX;
            double absoluteFocusY = radial.FocusY;

            double focusX = radial.FocusX - centerX;
            double focusY = radial.FocusY - centerY;

            // note that dx,dy need to move center
            /*
             *  dx = (currentXPosition - absoluteFocusX);
             *  dy = (startRowIndex - absoluteFocusY);
             *  currentColorIndexValue =
                    (int)
                    (
                        (
                            (
                            (dx * focusX) + (dy * focusY)
                            + Math.Sqrt
                            (
                                Math.Abs
                                (
                                    radius * radius * (dx * dx + dy * dy) - (dx * focusY - dy * focusX) * (dx * focusY - dy * focusX)      
                                )
                            )
                        ) * (radius /
                        ((radius * radius) - ((focusX * focusX )+ (focusY * focusY))))
                    ) * 256 /radius
                );
             */

            //note that  ( radius / (( radius * radius) - ((focusX * focusX) + (focusY * focusY))) is const
            // so that need to pre compute
            double preComputeMultiply = radius / ((radius * radius) - ((focusX * focusX) + (focusY * focusY)));

            #region modify when pre compute for multiply is zero
            if (preComputeMultiply == 0)
            {
                if (focusX != 0)
                {
                    if (focusX < 0)
                    {
                        focusX += GradientAdjustment;
                    }
                    else
                    {
                        focusX -= GradientAdjustment;
                    }
                }
                if (focusY != 0)
                {
                    if (focusY < 0)
                    {
                        focusY += GradientAdjustment;
                    }
                    else
                    {
                        focusY -= GradientAdjustment;
                    }
                }
                preComputeMultiply = radius / ((radius * radius) - ((focusX * focusX) + (focusY * focusY)));
            }
            #endregion

            double preComputeMultiplyIncludeLookup = preComputeRadiusLookup * preComputeMultiply;

            // saving dy * focusY
            double dyFocusY = 0;
            double dyFocusX = 0;
            double dxFocusYIncrement = 0; // saving dx * focusY - dyFocusX
            double radiusSquared = radius * radius;


            int currentColorIndexValue = 0;
            //int currentXPosition = 0;
            uint dst, dstRB, dstG;
            #endregion

            #region FILLING
            if (radial.Ramp.NoBlendingColor)
            {
                // when no need to blending, when draw a horizontal line
                // do not need check the back color, alway setup
                if (radial.Style != GradientStyle.Pad)
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;

                        #region cumpute value for row
                        //dyFocusY = (startRowIndex - centerY) * (startRowIndex - centerY);
                        dy = (startRowIndex - absoluteFocusY);
                        dySquared = dy * dy;
                        dyFocusX = dy * focusX;
                        dyFocusY = dy * focusY;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region non zero checking code
                                        if (scLastCoverage > 255) scLastCoverage = 255;
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;

                                            #region prepare for row color index calculation
                                            // get current color index value
                                            //currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                            //currentXPosition = scLastX + 1;
                                            dx = (scLastX + 1 - absoluteFocusX);
                                            dxFocusYIncrement = (dx * focusY - dyFocusX);
                                            #endregion
                                            if (scLastCoverage >= 255)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)
                                                        ((((dx * focusX) + dyFocusY +
                                                            Math.Sqrt(Math.Abs(
                                                                radiusSquared *
                                                                (dx * dx + dySquared) -
                                                                dxFocusYIncrement * dxFocusYIncrement))
                                                                ) * preComputeMultiplyIncludeLookup)
                                                        );

                                                    // change for color index calculation
                                                    dx++;
                                                    dxFocusYIncrement += focusY;
                                                    #endregion
                                                    BufferData[startXPosition] = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                    startXPosition++;
                                                }
                                            }
                                            else
                                            {
                                                calculatedCoverage = (byte)scLastCoverage;
                                                while (startXPosition < lastXPosition)
                                                {

                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)
                                                        ((((dx * focusX) + dyFocusY +
                                                            Math.Sqrt(Math.Abs(
                                                                radiusSquared *
                                                                (dx * dx + dySquared) -
                                                                dxFocusYIncrement * dxFocusYIncrement))
                                                                ) * preComputeMultiplyIncludeLookup)
                                                        );

                                                    // change for color index calculation
                                                    dx++;
                                                    dxFocusYIncrement += focusY;
                                                    #endregion

                                                    colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                    //calculatedCoverage = (byte)((colorData >> 24));
                                                    //calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                    if (calculatedCoverage >= 254)
                                                    {
                                                        BufferData[startXPosition] = colorData;
                                                    }
                                                    else
                                                    {
                                                        //// blend here
                                                        //dst = BufferData[startXPosition];
                                                        //dstRB = dst & 0x00FF00FF;
                                                        //dstG = (dst >> 8) & 0xFF;

                                                        //BufferData[startXPosition] =
                                                        //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                        //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                                        #region gamma apply
                                                        dst = BufferData[startXPosition];
                                                        dstG = (dst >> 8) & 0xFF;
                                                        dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                        BufferData[startXPosition] =
                                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                            | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                            | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                            ;
                                                        #endregion
                                                    }
                                                    startXPosition++;

                                                }
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        //currentXPosition = currentCellData.X;
                                        //currentColorIndexValue =
                                        //    (int)(Math.Sqrt(dyFocusY +
                                        //        (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                        #region prepare for row color index calculation
                                        // get current color index value
                                        dx = (currentCellData.X - absoluteFocusX);
                                        dxFocusYIncrement = (dx * focusY - dyFocusX);
                                        #endregion

                                        #region calculate color index
                                        currentColorIndexValue =
                                            (int)
                                            ((((dx * focusX) + dyFocusY +
                                                Math.Sqrt(Math.Abs(
                                                    radiusSquared *
                                                    (dx * dx + dySquared) -
                                                    dxFocusYIncrement * dxFocusYIncrement))
                                                    ) * preComputeMultiplyIncludeLookup)
                                            );
                                        #endregion
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        //calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        //tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        //dst = BufferData[startXPosition];
                                        //dstRB = dst & 0x00FF00FF;
                                        //dstG = (dst >> 8) & 0xFF;
                                        //BufferData[startXPosition] =
                                        //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        if (calculatedCoverage >= 254)
                                        {
                                            BufferData[startXPosition] = colorData;
                                        }
                                        else
                                        {
                                            #region gamma apply
                                            dst = BufferData[startXPosition];
                                            dstG = (dst >> 8) & 0xFF;
                                            dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                            BufferData[startXPosition] =
                                                (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                ;
                                            #endregion
                                        }
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        #region cumpute value for row
                        //dyFocusY = (startRowIndex - centerY) * (startRowIndex - centerY);
                        dy = (startRowIndex - absoluteFocusY);
                        dySquared = dy * dy;
                        dyFocusX = dy * focusX;
                        dyFocusY = dy * focusY;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region non zero checking code
                                        if (scLastCoverage > 255) scLastCoverage = 255;
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            #region prepare for row color index calculation
                                            // get current color index value
                                            dx = (scLastX + 1 - absoluteFocusX);
                                            dxFocusYIncrement = (dx * focusY - dyFocusX);
                                            #endregion

                                            if (scLastCoverage >= 255)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)
                                                        ((((dx * focusX) + dyFocusY +
                                                            Math.Sqrt(Math.Abs(
                                                                radiusSquared *
                                                                (dx * dx + dySquared) -
                                                                dxFocusYIncrement * dxFocusYIncrement))
                                                                ) * preComputeMultiplyIncludeLookup)
                                                        );

                                                    // change for color index calculation
                                                    dx++;
                                                    dxFocusYIncrement += focusY;
                                                    #endregion

                                                    BufferData[startXPosition] = builtColors[currentColorIndexValue < 0 ? 0 : currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                    startXPosition++;
                                                }
                                            }
                                            else
                                            {
                                                calculatedCoverage = (byte)(scLastCoverage);
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)
                                                        ((((dx * focusX) + dyFocusY +
                                                            Math.Sqrt(Math.Abs(
                                                                radiusSquared *
                                                                (dx * dx + dySquared) -
                                                                dxFocusYIncrement * dxFocusYIncrement))
                                                                ) * preComputeMultiplyIncludeLookup)
                                                        );

                                                    // change for color index calculation
                                                    dx++;
                                                    dxFocusYIncrement += focusY;
                                                    #endregion
                                                    colorData = builtColors[currentColorIndexValue < 0 ? 0 : currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                    //calculatedCoverage = (byte)((colorData >> 24));
                                                    //calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                    if (calculatedCoverage >= 254)
                                                    {
                                                        BufferData[startXPosition] = colorData;
                                                    }
                                                    else
                                                    {
                                                        // blend here
                                                        #region gamma apply
                                                        dst = BufferData[startXPosition];
                                                        dstG = (dst >> 8) & 0xFF;
                                                        dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                        BufferData[startXPosition] =
                                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                            | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                            | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                            ;
                                                        #endregion
                                                    }
                                                    startXPosition++;
                                                }
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        #region prepare for row color index calculation
                                        // get current color index value
                                        dx = (currentCellData.X - absoluteFocusX);
                                        dxFocusYIncrement = (dx * focusY - dyFocusX);
                                        #endregion

                                        #region calculate color index
                                        currentColorIndexValue =
                                            (int)
                                            ((((dx * focusX) + dyFocusY +
                                                Math.Sqrt(Math.Abs(
                                                    radiusSquared *
                                                    (dx * dx + dySquared) -
                                                    dxFocusYIncrement * dxFocusYIncrement))
                                                    ) * preComputeMultiplyIncludeLookup)
                                            );
                                        #endregion
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue < 0 ? 0 : currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                        //calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        //tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        //dst = BufferData[startXPosition];
                                        //dstRB = dst & 0x00FF00FF;
                                        //dstG = (dst >> 8) & 0xFF;
                                        //BufferData[startXPosition] =
                                        //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                        if (calculatedCoverage >= 254)
                                        {
                                            BufferData[startXPosition] = colorData;
                                        }
                                        else
                                        {
                                            #region gamma apply
                                            dst = BufferData[startXPosition];
                                            dstG = (dst >> 8) & 0xFF;
                                            dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                            BufferData[startXPosition] =
                                                (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                ;
                                            #endregion
                                        }
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
            }
            else
            {
                // when no need to blending, when draw a horizontal line
                // do not need check the back color, alway setup
                if (radial.Style != GradientStyle.Pad)
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        #region cumpute value for row
                        //dyFocusY = (startRowIndex - centerY) * (startRowIndex - centerY);
                        dy = (startRowIndex - absoluteFocusY);
                        dySquared = dy * dy;
                        dyFocusX = dy * focusX;
                        dyFocusY = dy * focusY;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region non zero checking code
                                        if (scLastCoverage > 255) scLastCoverage = 255;
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            #region prepare for row color index calculation
                                            // get current color index value
                                            dx = (scLastX + 1 - absoluteFocusX);
                                            dxFocusYIncrement = (dx * focusY - dyFocusX);
                                            #endregion


                                            while (startXPosition < lastXPosition)
                                            {
                                                #region calculate color index
                                                currentColorIndexValue =
                                                    (int)
                                                    ((((dx * focusX) + dyFocusY +
                                                        Math.Sqrt(Math.Abs(
                                                            radiusSquared *
                                                            (dx * dx + dySquared) -
                                                            dxFocusYIncrement * dxFocusYIncrement))
                                                            ) * preComputeMultiplyIncludeLookup)
                                                    );

                                                // change for color index calculation
                                                dx++;
                                                dxFocusYIncrement += focusY;
                                                #endregion
                                                colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                calculatedCoverage = (byte)((colorData >> 24));
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                if (calculatedCoverage >= 254)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    #region gamma apply
                                                    dst = BufferData[startXPosition];
                                                    dstG = (dst >> 8) & 0xFF;
                                                    dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                    BufferData[startXPosition] =
                                                        (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                        | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                        | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                        ;
                                                    #endregion
                                                }
                                                startXPosition++;
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion
                                        // get current color data
                                        #region prepare for row color index calculation
                                        // get current color index value
                                        dx = (currentCellData.X - absoluteFocusX);
                                        dxFocusYIncrement = (dx * focusY - dyFocusX);
                                        #endregion

                                        #region calculate color index
                                        currentColorIndexValue =
                                            (int)
                                            ((((dx * focusX) + dyFocusY +
                                                Math.Sqrt(Math.Abs(
                                                    radiusSquared *
                                                    (dx * dx + dySquared) -
                                                    dxFocusYIncrement * dxFocusYIncrement))
                                                    ) * preComputeMultiplyIncludeLookup)
                                            );
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        //if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        #region gamma apply
                                        dst = BufferData[startXPosition];
                                        dstG = (dst >> 8) & 0xFF;
                                        dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                        BufferData[startXPosition] =
                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                            | (gammaLutBlue[(dstRB & 0x00FF)]))
                                            ;
                                        #endregion
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        #region cumpute value for row
                        //dyFocusY = (startRowIndex - centerY) * (startRowIndex - centerY);
                        dy = (startRowIndex - absoluteFocusY);
                        dySquared = dy * dy;
                        dyFocusX = dy * focusX;
                        dyFocusY = dy * focusY;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region non zero checking code
                                        if (scLastCoverage > 255) scLastCoverage = 255;
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            #region prepare for row color index calculation
                                            // get current color index value
                                            dx = (scLastX + 1 - absoluteFocusX);
                                            dxFocusYIncrement = (dx * focusY - dyFocusX);
                                            #endregion


                                            while (startXPosition < lastXPosition)
                                            {
                                                #region calculate color index
                                                currentColorIndexValue =
                                                    (int)
                                                    ((((dx * focusX) + dyFocusY +
                                                        Math.Sqrt(Math.Abs(
                                                            radiusSquared *
                                                            (dx * dx + dySquared) -
                                                            dxFocusYIncrement * dxFocusYIncrement))
                                                            ) * preComputeMultiplyIncludeLookup)
                                                    );

                                                // change for color index calculation
                                                dx++;
                                                dxFocusYIncrement += focusY;
                                                #endregion

                                                colorData = builtColors[currentColorIndexValue < 0 ? 0 : currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                calculatedCoverage = (byte)((colorData >> 24));
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                if (calculatedCoverage >= 254)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    #region gamma apply
                                                    dst = BufferData[startXPosition];
                                                    dstG = (dst >> 8) & 0xFF;
                                                    dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                    BufferData[startXPosition] =
                                                        (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (((uint)(uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                        | ((uint)(uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                        | ((uint)gammaLutBlue[(dstRB & 0x00FF)]))
                                                        ;
                                                    #endregion
                                                }
                                                startXPosition++;
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion
                                        // get current color data
                                        #region prepare for row color index calculation
                                        // get current color index value
                                        dx = (currentCellData.X - absoluteFocusX);
                                        dxFocusYIncrement = (dx * focusY - dyFocusX);
                                        #endregion

                                        #region calculate color index
                                        currentColorIndexValue =
                                            (int)
                                            ((((dx * focusX) + dyFocusY +
                                                Math.Sqrt(Math.Abs(
                                                    radiusSquared *
                                                    (dx * dx + dySquared) -
                                                    dxFocusYIncrement * dxFocusYIncrement))
                                                    ) * preComputeMultiplyIncludeLookup)
                                            );
                                        #endregion
                                        colorData = builtColors[currentColorIndexValue < 0 ? 0 : currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                        calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        //if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        #region gamma apply
                                        dst = BufferData[startXPosition];
                                        dstG = (dst >> 8) & 0xFF;
                                        dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                        BufferData[startXPosition] =
                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                            | (gammaLutBlue[(dstRB & 0x00FF)]))
                                            ;
                                        #endregion
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
            }

            #endregion
        }
        #endregion

        #region Fill ellipse, Focus
        /// <summary>
        /// Filling using radial gradient for circle gradient only
        /// </summary>
        /// <param name="radial">radial</param>
        /// <param name="rows">rows</param>
        /// <param name="startRowIndex">start y index</param>
        /// <param name="endRowIndex">end y index</param>
        /// <param name="gammaLutRed">gamma look up table for red</param>
        /// <param name="gammaLutGreen">gamma look up table for green</param>
        /// <param name="gammaLutBlue">gamma look up table for blue</param>
        void FillingEllipseFocal(RadialGradient radial, uint opacity, RowData[] rows, int startRowIndex, int endRowIndex, byte[] gammaLutRed, byte[] gammaLutGreen, byte[] gammaLutBlue)
        {
            // now not need to check null or not
            uint[] builtColors = radial.GetLinearColors(opacity);
            #region private variable for filling
            int currentCoverage, scLastCoverage, scLastX = 0;
            int tempCover = 0;
            int currentArea = 0;
            int lastXPosition = 0;
            int startXPosition = 0;
            byte calculatedCoverage = 0;


            double centerX = radial.CenterX;
            double centerY = radial.CenterY;
            // in this case radius x = radius y
            double radius = radial.RadiusX;
            double radiusYForX = radial.RadiusY / radial.RadiusX;


            // this is precompute value so that (* ColorIndexScale / radius) now just ( * preComputeRadiusLookup )
            double preComputeRadiusLookup = ColorIndexScale / radius;

            CellData currentCellData = null;
            uint colorData = 0;

            double dx = 0, dy = 0;

            double dySquared = 0; // saving dy * dy
            // focus is changed to relative from the center
            double absoluteFocusX = radial.FocusX;
            double absoluteFocusY = radial.FocusY;

            double focusX = radial.FocusX - centerX;
            double focusY = radial.FocusY - centerY;
            focusY = focusY / radiusYForX;

            // note that dx,dy need to move center
            /*
             *  dx = (currentXPosition - absoluteFocusX);
             *  dy = (startRowIndex - absoluteFocusY);
             *  currentColorIndexValue =
                    (int)
                    (
                        (
                            (
                            (dx * focusX) + (dy * focusY)
                            + Math.Sqrt
                            (
                                Math.Abs
                                (
                                    radius * radius * (dx * dx + dy * dy) - (dx * focusY - dy * focusX) * (dx * focusY - dy * focusX)      
                                )
                            )
                        ) * (radius /
                        ((radius * radius) - ((focusX * focusX )+ (focusY * focusY))))
                    ) * 256 /radius
                );
             */

            //note that  ( radius / (( radius * radius) - ((focusX * focusX) + (focusY * focusY))) is const
            // so that need to pre compute
            double preComputeMultiply = radius / ((radius * radius) - ((focusX * focusX) + (focusY * focusY)));

            #region modify when pre compute for multiply is zero
            if (preComputeMultiply == 0)
            {
                if (focusX != 0)
                {
                    if (focusX < 0)
                    {
                        focusX += GradientAdjustment;
                    }
                    else
                    {
                        focusX -= GradientAdjustment;
                    }
                }
                if (focusY != 0)
                {
                    if (focusY < 0)
                    {
                        focusY += GradientAdjustment;
                    }
                    else
                    {
                        focusY -= GradientAdjustment;
                    }
                }
                preComputeMultiply = radius / ((radius * radius) - ((focusX * focusX) + (focusY * focusY)));
            }
            #endregion

            double preComputeMultiplyIncludeLookup = preComputeRadiusLookup * preComputeMultiply;

            // saving dy * focusY
            double dyFocusY = 0;
            double dyFocusX = 0;
            double dxFocusYIncrement = 0; // saving dx * focusY - dyFocusX
            double radiusSquared = radius * radius;


            int currentColorIndexValue = 0;
            //int currentXPosition = 0;
            uint dst, dstRB, dstG;
            #endregion

            #region FILLING
            if (radial.Ramp.NoBlendingColor)
            {
                // when no need to blending, when draw a horizontal line
                // do not need check the back color, alway setup
                if (radial.Style != GradientStyle.Pad)
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;

                        #region cumpute value for row
                        //dyFocusY = (startRowIndex - centerY) * (startRowIndex - centerY);
                        dy = ((startRowIndex - centerY) / radiusYForX) - focusY;
                        dySquared = dy * dy;
                        dyFocusX = dy * focusX;
                        dyFocusY = dy * focusY;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region non zero checking code
                                        if (scLastCoverage > 255) scLastCoverage = 255;
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;

                                            #region prepare for row color index calculation
                                            // get current color index value
                                            //currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                            //currentXPosition = scLastX + 1;
                                            dx = (scLastX + 1 - absoluteFocusX);
                                            dxFocusYIncrement = (dx * focusY - dyFocusX);
                                            #endregion
                                            if (scLastCoverage >= 255)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)
                                                        ((((dx * focusX) + dyFocusY +
                                                            Math.Sqrt(Math.Abs(
                                                                radiusSquared *
                                                                (dx * dx + dySquared) -
                                                                dxFocusYIncrement * dxFocusYIncrement))
                                                                ) * preComputeMultiplyIncludeLookup)
                                                        );

                                                    //currentColorIndexValue =
                                                    //    (int)
                                                    //    (
                                                    //        (
                                                    //            (
                                                    //            (dx * focusX) + (dy * focusY)
                                                    //            + Math.Sqrt
                                                    //            (
                                                    //                Math.Abs
                                                    //                (
                                                    //                    radius * radius 
                                                    //                    * (dx * dx + dy * dy) 
                                                    //                    - (dx * focusY - dy * focusX) 
                                                    //                    * (dx * focusY - dy * focusX)
                                                    //                )
                                                    //            )
                                                    //        ) * (radius /
                                                    //        ((radius * radius) - ((focusX * focusX) + (focusY * focusY))))
                                                    //        ) * 256 / radius
                                                    //    );

                                                    // change for color index calculation
                                                    dx++;
                                                    dxFocusYIncrement += focusY;
                                                    #endregion
                                                    BufferData[startXPosition] = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                    startXPosition++;
                                                }
                                            }
                                            else
                                            {
                                                calculatedCoverage = (byte)scLastCoverage;
                                                while (startXPosition < lastXPosition)
                                                {

                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)
                                                        ((((dx * focusX) + dyFocusY +
                                                            Math.Sqrt(Math.Abs(
                                                                radiusSquared *
                                                                (dx * dx + dySquared) -
                                                                dxFocusYIncrement * dxFocusYIncrement))
                                                                ) * preComputeMultiplyIncludeLookup)
                                                        );

                                                    // change for color index calculation
                                                    dx++;
                                                    dxFocusYIncrement += focusY;
                                                    #endregion

                                                    colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                    //calculatedCoverage = (byte)((colorData >> 24));
                                                    //calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                    if (calculatedCoverage >= 254)
                                                    {
                                                        BufferData[startXPosition] = colorData;
                                                    }
                                                    else
                                                    {
                                                        #region gamma apply
                                                        dst = BufferData[startXPosition];
                                                        dstG = (dst >> 8) & 0xFF;
                                                        dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                        BufferData[startXPosition] =
                                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                            | (((uint)(uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                            | ((uint)(uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                            | ((uint)gammaLutBlue[(dstRB & 0x00FF)]))
                                                            ;
                                                        #endregion
                                                    }
                                                    startXPosition++;

                                                }
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        //currentXPosition = currentCellData.X;
                                        //currentColorIndexValue =
                                        //    (int)(Math.Sqrt(dyFocusY +
                                        //        (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                        #region prepare for row color index calculation
                                        // get current color index value
                                        dx = (currentCellData.X - absoluteFocusX);
                                        dxFocusYIncrement = (dx * focusY - dyFocusX);
                                        #endregion

                                        #region calculate color index
                                        currentColorIndexValue =
                                            (int)
                                            ((((dx * focusX) + dyFocusY +
                                                Math.Sqrt(Math.Abs(
                                                    radiusSquared *
                                                    (dx * dx + dySquared) -
                                                    dxFocusYIncrement * dxFocusYIncrement))
                                                    ) * preComputeMultiplyIncludeLookup)
                                            );
                                        #endregion
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        //calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        //tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        //dst = BufferData[startXPosition];
                                        //dstRB = dst & 0x00FF00FF;
                                        //dstG = (dst >> 8) & 0xFF;
                                        //BufferData[startXPosition] =
                                        //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                        if (calculatedCoverage >= 254)
                                        {
                                            BufferData[startXPosition] = colorData;
                                        }
                                        else
                                        {
                                            #region gamma apply
                                            dst = BufferData[startXPosition];
                                            dstG = (dst >> 8) & 0xFF;
                                            dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                            BufferData[startXPosition] =
                                                (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                ;
                                            #endregion
                                        }
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        #region cumpute value for row
                        //dyFocusY = (startRowIndex - centerY) * (startRowIndex - centerY);
                        dy = ((startRowIndex - centerY) / radiusYForX) - focusY;
                        dySquared = dy * dy;
                        dyFocusX = dy * focusX;
                        dyFocusY = dy * focusY;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region non zero checking code
                                        if (scLastCoverage > 255) scLastCoverage = 255;
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            #region prepare for row color index calculation
                                            // get current color index value
                                            dx = (scLastX + 1 - absoluteFocusX);
                                            dxFocusYIncrement = (dx * focusY - dyFocusX);
                                            #endregion

                                            if (scLastCoverage >= 255)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)
                                                        ((((dx * focusX) + dyFocusY +
                                                            Math.Sqrt(Math.Abs(
                                                                radiusSquared *
                                                                (dx * dx + dySquared) -
                                                                dxFocusYIncrement * dxFocusYIncrement))
                                                                ) * preComputeMultiplyIncludeLookup)
                                                        );

                                                    // change for color index calculation
                                                    dx++;
                                                    dxFocusYIncrement += focusY;
                                                    #endregion

                                                    BufferData[startXPosition] = builtColors[currentColorIndexValue < 0 ? 0 : currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                    startXPosition++;
                                                }
                                            }
                                            else
                                            {
                                                calculatedCoverage = (byte)(scLastCoverage);
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)
                                                        ((((dx * focusX) + dyFocusY +
                                                            Math.Sqrt(Math.Abs(
                                                                radiusSquared *
                                                                (dx * dx + dySquared) -
                                                                dxFocusYIncrement * dxFocusYIncrement))
                                                                ) * preComputeMultiplyIncludeLookup)
                                                        );

                                                    // change for color index calculation
                                                    dx++;
                                                    dxFocusYIncrement += focusY;
                                                    #endregion
                                                    colorData = builtColors[currentColorIndexValue < 0 ? 0 : currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                    //calculatedCoverage = (byte)((colorData >> 24));
                                                    //calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                    if (calculatedCoverage >= 254)
                                                    {
                                                        BufferData[startXPosition] = colorData;
                                                    }
                                                    else
                                                    {
                                                        #region gamma apply
                                                        dst = BufferData[startXPosition];
                                                        dstG = (dst >> 8) & 0xFF;
                                                        dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                        BufferData[startXPosition] =
                                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                            | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                            | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                            ;
                                                        #endregion
                                                    }
                                                    startXPosition++;
                                                }
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        #region prepare for row color index calculation
                                        // get current color index value
                                        dx = (currentCellData.X - absoluteFocusX);
                                        dxFocusYIncrement = (dx * focusY - dyFocusX);
                                        #endregion

                                        #region calculate color index
                                        currentColorIndexValue =
                                            (int)
                                            ((((dx * focusX) + dyFocusY +
                                                Math.Sqrt(Math.Abs(
                                                    radiusSquared *
                                                    (dx * dx + dySquared) -
                                                    dxFocusYIncrement * dxFocusYIncrement))
                                                    ) * preComputeMultiplyIncludeLookup)
                                            );
                                        #endregion
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue < 0 ? 0 : currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                        //calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        //tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        //dst = BufferData[startXPosition];
                                        //dstRB = dst & 0x00FF00FF;
                                        //dstG = (dst >> 8) & 0xFF;
                                        //BufferData[startXPosition] =
                                        //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                        if (calculatedCoverage >= 254)
                                        {
                                            BufferData[startXPosition] = colorData;
                                        }
                                        else
                                        {
                                            #region gamma apply
                                            dst = BufferData[startXPosition];
                                            dstG = (dst >> 8) & 0xFF;
                                            dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                            BufferData[startXPosition] =
                                                (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                ;
                                            #endregion
                                        }
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
            }
            else
            {
                // when no need to blending, when draw a horizontal line
                // do not need check the back color, alway setup
                if (radial.Style != GradientStyle.Pad)
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        #region cumpute value for row
                        //dyFocusY = (startRowIndex - centerY) * (startRowIndex - centerY);
                        dy = ((startRowIndex - centerY) / radiusYForX) - focusY;
                        dySquared = dy * dy;
                        dyFocusX = dy * focusX;
                        dyFocusY = dy * focusY;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region non zero checking code
                                        if (scLastCoverage > 255) scLastCoverage = 255;
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            #region prepare for row color index calculation
                                            // get current color index value
                                            dx = (scLastX + 1 - absoluteFocusX);
                                            dxFocusYIncrement = (dx * focusY - dyFocusX);
                                            #endregion


                                            while (startXPosition < lastXPosition)
                                            {
                                                #region calculate color index
                                                currentColorIndexValue =
                                                    (int)
                                                    ((((dx * focusX) + dyFocusY +
                                                        Math.Sqrt(Math.Abs(
                                                            radiusSquared *
                                                            (dx * dx + dySquared) -
                                                            dxFocusYIncrement * dxFocusYIncrement))
                                                            ) * preComputeMultiplyIncludeLookup)
                                                    );

                                                // change for color index calculation
                                                dx++;
                                                dxFocusYIncrement += focusY;
                                                #endregion
                                                colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                calculatedCoverage = (byte)((colorData >> 24));
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                if (calculatedCoverage >= 254)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    #region gamma apply
                                                    dst = BufferData[startXPosition];
                                                    dstG = (dst >> 8) & 0xFF;
                                                    dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                    BufferData[startXPosition] =
                                                        (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                        | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                        | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                        ;
                                                    #endregion
                                                }
                                                startXPosition++;
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion
                                        // get current color data
                                        #region prepare for row color index calculation
                                        // get current color index value
                                        dx = (currentCellData.X - absoluteFocusX);
                                        dxFocusYIncrement = (dx * focusY - dyFocusX);
                                        #endregion

                                        #region calculate color index
                                        currentColorIndexValue =
                                            (int)
                                            ((((dx * focusX) + dyFocusY +
                                                Math.Sqrt(Math.Abs(
                                                    radiusSquared *
                                                    (dx * dx + dySquared) -
                                                    dxFocusYIncrement * dxFocusYIncrement))
                                                    ) * preComputeMultiplyIncludeLookup)
                                            );
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        //if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region gamma apply
                                        dst = BufferData[startXPosition];
                                        dstG = (dst >> 8) & 0xFF;
                                        dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                        BufferData[startXPosition] =
                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                            | (gammaLutBlue[(dstRB & 0x00FF)]))
                                            ;
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        #region cumpute value for row
                        //dyFocusY = (startRowIndex - centerY) * (startRowIndex - centerY);
                        dy = ((startRowIndex - centerY) / radiusYForX) - focusY;
                        dySquared = dy * dy;
                        dyFocusX = dy * focusX;
                        dyFocusY = dy * focusY;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region non zero checking code
                                        if (scLastCoverage > 255) scLastCoverage = 255;
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            #region prepare for row color index calculation
                                            // get current color index value
                                            dx = (scLastX + 1 - absoluteFocusX);
                                            dxFocusYIncrement = (dx * focusY - dyFocusX);
                                            #endregion


                                            while (startXPosition < lastXPosition)
                                            {
                                                #region calculate color index
                                                currentColorIndexValue =
                                                    (int)
                                                    ((((dx * focusX) + dyFocusY +
                                                        Math.Sqrt(Math.Abs(
                                                            radiusSquared *
                                                            (dx * dx + dySquared) -
                                                            dxFocusYIncrement * dxFocusYIncrement))
                                                            ) * preComputeMultiplyIncludeLookup)
                                                    );

                                                // change for color index calculation
                                                dx++;
                                                dxFocusYIncrement += focusY;
                                                #endregion

                                                colorData = builtColors[currentColorIndexValue < 0 ? 0 : currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                calculatedCoverage = (byte)((colorData >> 24));
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                if (calculatedCoverage >= 254)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    #region gamma apply
                                                    dst = BufferData[startXPosition];
                                                    dstG = (dst >> 8) & 0xFF;
                                                    dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                    BufferData[startXPosition] =
                                                        (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                        | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                        | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                        ;
                                                    #endregion
                                                }
                                                startXPosition++;
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion
                                        // get current color data
                                        #region prepare for row color index calculation
                                        // get current color index value
                                        dx = (currentCellData.X - absoluteFocusX);
                                        dxFocusYIncrement = (dx * focusY - dyFocusX);
                                        #endregion

                                        #region calculate color index
                                        currentColorIndexValue =
                                            (int)
                                            ((((dx * focusX) + dyFocusY +
                                                Math.Sqrt(Math.Abs(
                                                    radiusSquared *
                                                    (dx * dx + dySquared) -
                                                    dxFocusYIncrement * dxFocusYIncrement))
                                                    ) * preComputeMultiplyIncludeLookup)
                                            );
                                        #endregion
                                        colorData = builtColors[currentColorIndexValue < 0 ? 0 : currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                        calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        //if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region gamma apply
                                        dst = BufferData[startXPosition];
                                        dstG = (dst >> 8) & 0xFF;
                                        dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                        BufferData[startXPosition] =
                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                            | (gammaLutBlue[(dstRB & 0x00FF)]))
                                            ;
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
            }

            #endregion
        }
        #endregion
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
            // this base on paint to filling
            if (!(paint.Paint is RadialGradient))
            {
                //throw new NotImplementedException("Support color paint only");
                NotMatchPaintTypeException.Publish(typeof(RadialGradient), paint.Paint.GetType());
                return;
            }
            RadialGradient radial = paint.Paint as RadialGradient;
            if (radial.RadiusX == radial.RadiusY)
            {
                if ((radial.FocusX == radial.CenterX) && (radial.FocusY == radial.CenterY))
                {
                    // when normal radial gradient
                    FillingRadialEvenOdd(radial, paint.ScaledOpacity, rows, startRowIndex, endRowIndex);
                }
                else
                {
                    // circle and focus gradient
                    FillingRadialFocalEvenOdd(radial, paint.ScaledOpacity, rows, startRowIndex, endRowIndex);
                }
            }
            else
            {
                if ((radial.FocusX == radial.CenterX) && (radial.FocusY == radial.CenterY))
                {
                    // when normal ellipse gradient
                    FillingEllipseEvenOdd(radial, paint.ScaledOpacity, rows, startRowIndex, endRowIndex);
                }
                else
                {
                    // ellipse and focus gradient
                    FillingEllipseFocalEvenOdd(radial, paint.ScaledOpacity, rows, startRowIndex, endRowIndex);
                }
            }
        }


        #region Fill normal, radial
        /// <summary>
        /// Filling using radial gradient for circle gradient only
        /// </summary>
        /// <param name="radial">radial</param>
        /// <param name="rows">rows</param>
        /// <param name="startRowIndex">start y index</param>
        /// <param name="endRowIndex">end y index</param>
        void FillingRadialEvenOdd(RadialGradient radial, uint opacity, RowData[] rows, int startRowIndex, int endRowIndex)
        {
            // now not need to check null or not
            uint[] builtColors = radial.GetLinearColors(opacity);
            #region private variable for filling
            int currentCoverage, scLastCoverage, scLastX = 0;
            int tempCover = 0;
            int currentArea = 0;
            int lastXPosition = 0;
            int startXPosition = 0;
            byte calculatedCoverage = 0;

            double centerX = radial.CenterX;
            double centerY = radial.CenterY;
            // in this case radius x = radius y
            double radius = radial.RadiusX;

            // saving precompute value for rows
            /* Normal calculation to get the color index
             * currentColorIndexValue =
                (int)(Math.Sqrt(
                    (startRowIndex - centerY) * (startRowIndex - centerY) +
                    (currentXPosition - centerX) * (currentXPosition - centerX)) * ColorIndexScale / radius );
             * but
             *  preComputeForRow= (startRowIndex - centerY) * (startRowIndex - centerY)
             *  so that
             *    currentColorIndexValue = 
             *    (int)(Math.Sqrt(
                    (preComputeForRow) +
                    (currentXPosition - centerX) * (currentXPosition - centerX)) * ColorIndexScale / radius );
             */
            double preComputeForRow = 0;

            // this is precompute value so that (* ColorIndexScale / radius) now just ( * preComputeRadiusLookup )
            double preComputeRadiusLookup = ColorIndexScale / radius;

            CellData currentCellData = null;
            uint colorData = 0;
            //uint colorG = 0;
            //uint colorRB = 0;


            int currentColorIndexValue = 0;
            int currentXPosition = 0;

            uint dst, dstRB, dstG;
            #endregion

            #region FILLING
            if (radial.Ramp.NoBlendingColor)
            {
                // when no need to blending, when draw a horizontal line
                // do not need check the back color, alway setup
                if (radial.Style != GradientStyle.Pad)
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        preComputeForRow = (startRowIndex - centerY) * (startRowIndex - centerY);
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region even odd change
                                        scLastCoverage &= 511;
                                        if (scLastCoverage >= 256)
                                        {
                                            scLastCoverage = 512 - scLastCoverage - 1;
                                        }
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            //currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                            currentXPosition = scLastX + 1;
                                            if (scLastCoverage >= 255)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)(Math.Sqrt(
                                                            preComputeForRow +
                                                            (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                                    #endregion
                                                    BufferData[startXPosition] = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                    startXPosition++;
                                                    currentXPosition++;
                                                }
                                            }
                                            else
                                            {
                                                calculatedCoverage = (byte)scLastCoverage;
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)(Math.Sqrt(
                                                            preComputeForRow +
                                                            (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                                    #endregion

                                                    colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                    //calculatedCoverage = (byte)((colorData >> 24));
                                                    //calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                    if (calculatedCoverage >= 254)
                                                    {
                                                        BufferData[startXPosition] = colorData;
                                                    }
                                                    else
                                                    {
                                                        // blend here
                                                        dst = BufferData[startXPosition];
                                                        dstRB = dst & 0x00FF00FF;
                                                        dstG = (dst >> 8) & 0xFF;

                                                        BufferData[startXPosition] =
                                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                    }
                                                    startXPosition++;
                                                    currentXPosition++;
                                                }
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region even odd change
                                        tempCover &= 511;
                                        if (tempCover >= 256)
                                        {
                                            tempCover = 512 - tempCover - 1;
                                        }
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        currentXPosition = currentCellData.X;
                                        currentColorIndexValue =
                                            (int)(Math.Sqrt(preComputeForRow +
                                                (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        //calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        //tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        dst = BufferData[startXPosition];
                                        dstRB = dst & 0x00FF00FF;
                                        dstG = (dst >> 8) & 0xFF;
                                        BufferData[startXPosition] =
                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        preComputeForRow = (startRowIndex - centerY) * (startRowIndex - centerY);
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region even odd change
                                        scLastCoverage &= 511;
                                        if (scLastCoverage >= 256)
                                        {
                                            scLastCoverage = 512 - scLastCoverage - 1;
                                        }
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            //currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                            currentXPosition = scLastX + 1;
                                            if (scLastCoverage >= 255)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)(Math.Sqrt(
                                                            preComputeForRow +
                                                            (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                                    #endregion
                                                    BufferData[startXPosition] = builtColors[currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                    startXPosition++;
                                                    currentXPosition++;
                                                }
                                            }
                                            else
                                            {
                                                calculatedCoverage = (byte)(scLastCoverage);
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)(Math.Sqrt(
                                                            preComputeForRow +
                                                            (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                                    #endregion

                                                    colorData = builtColors[currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                    //calculatedCoverage = (byte)((colorData >> 24));
                                                    //calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                    if (calculatedCoverage >= 254)
                                                    {
                                                        BufferData[startXPosition] = colorData;
                                                    }
                                                    else
                                                    {
                                                        // blend here
                                                        dst = BufferData[startXPosition];
                                                        dstRB = dst & 0x00FF00FF;
                                                        dstG = (dst >> 8) & 0xFF;

                                                        BufferData[startXPosition] =
                                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                    }
                                                    startXPosition++;
                                                    currentXPosition++;
                                                }
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region even odd change
                                        tempCover &= 511;
                                        if (tempCover >= 256)
                                        {
                                            tempCover = 512 - tempCover - 1;
                                        }
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        currentXPosition = currentCellData.X;
                                        currentColorIndexValue =
                                            (int)(Math.Sqrt(preComputeForRow +
                                                (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue > 254 ? 255 : currentColorIndexValue];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        //calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        //tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        dst = BufferData[startXPosition];
                                        dstRB = dst & 0x00FF00FF;
                                        dstG = (dst >> 8) & 0xFF;
                                        BufferData[startXPosition] =
                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
            }
            else
            {
                // when no need to blending, when draw a horizontal line
                // do not need check the back color, alway setup
                if (radial.Style != GradientStyle.Pad)
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        preComputeForRow = (startRowIndex - centerY) * (startRowIndex - centerY);
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region even odd change
                                        scLastCoverage &= 511;
                                        if (scLastCoverage >= 256)
                                        {
                                            scLastCoverage = 512 - scLastCoverage - 1;
                                        }
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            //currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                            currentXPosition = scLastX + 1;

                                            while (startXPosition < lastXPosition)
                                            {
                                                #region calculate color index
                                                currentColorIndexValue =
                                                    (int)(Math.Sqrt(
                                                        preComputeForRow +
                                                        (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                                #endregion

                                                colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                calculatedCoverage = (byte)((colorData >> 24));
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                if (calculatedCoverage >= 254)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    // blend here
                                                    dst = BufferData[startXPosition];
                                                    dstRB = dst & 0x00FF00FF;
                                                    dstG = (dst >> 8) & 0xFF;

                                                    BufferData[startXPosition] =
                                                        (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                        | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                }
                                                startXPosition++;
                                                currentXPosition++;
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region even odd change
                                        tempCover &= 511;
                                        if (tempCover >= 256)
                                        {
                                            tempCover = 512 - tempCover - 1;
                                        }
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        currentXPosition = currentCellData.X;
                                        currentColorIndexValue =
                                            (int)(Math.Sqrt(preComputeForRow +
                                                (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        //if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        dst = BufferData[startXPosition];
                                        dstRB = dst & 0x00FF00FF;
                                        dstG = (dst >> 8) & 0xFF;
                                        BufferData[startXPosition] =
                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        preComputeForRow = (startRowIndex - centerY) * (startRowIndex - centerY);
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region even odd change
                                        scLastCoverage &= 511;
                                        if (scLastCoverage >= 256)
                                        {
                                            scLastCoverage = 512 - scLastCoverage - 1;
                                        }
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            //currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                            currentXPosition = scLastX + 1;

                                            while (startXPosition < lastXPosition)
                                            {
                                                #region calculate color index
                                                currentColorIndexValue =
                                                    (int)(Math.Sqrt(
                                                        preComputeForRow +
                                                        (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                                #endregion

                                                colorData = builtColors[currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                calculatedCoverage = (byte)((colorData >> 24));
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                if (calculatedCoverage >= 254)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    // blend here
                                                    dst = BufferData[startXPosition];
                                                    dstRB = dst & 0x00FF00FF;
                                                    dstG = (dst >> 8) & 0xFF;

                                                    BufferData[startXPosition] =
                                                        (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                        | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                }
                                                startXPosition++;
                                                currentXPosition++;
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region even odd change
                                        tempCover &= 511;
                                        if (tempCover >= 256)
                                        {
                                            tempCover = 512 - tempCover - 1;
                                        }
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        currentXPosition = currentCellData.X;
                                        currentColorIndexValue =
                                            (int)(Math.Sqrt(preComputeForRow +
                                                (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue > 254 ? 255 : currentColorIndexValue];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        //if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        dst = BufferData[startXPosition];
                                        dstRB = dst & 0x00FF00FF;
                                        dstG = (dst >> 8) & 0xFF;
                                        BufferData[startXPosition] =
                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
            }

            #endregion
        }
        #endregion

        #region fill ellipse
        /// <summary>
        /// Filling using ellipse gradient for circle gradient only
        /// </summary>
        /// <param name="radial">radial</param>
        /// <param name="rows">rows</param>
        /// <param name="startRowIndex">start y index</param>
        /// <param name="endRowIndex">end y index</param>
        void FillingEllipseEvenOdd(RadialGradient radial, uint opacity, RowData[] rows, int startRowIndex, int endRowIndex)
        {
            // now not need to check null or not
            uint[] builtColors = radial.GetLinearColors(opacity);
            #region private variable for filling
            int currentCoverage, scLastCoverage, scLastX = 0;
            int tempCover = 0;
            int currentArea = 0;
            int lastXPosition = 0;
            int startXPosition = 0;
            byte calculatedCoverage = 0;

            double centerX = radial.CenterX;
            double centerY = radial.CenterY;
            // in this case radius x = radius y
            double radius = radial.RadiusX;

            // saving precompute value for rows
            /* Normal calculation to get the color index
             *  currentColorIndexValue =
                    (int)(Math.Sqrt(  
                            ((currentXPosition-centerX) * (currentXPosition-centerX) /(radial.RadiusX * radial.RadiusX))
                            +
                            ((startRowIndex - centerY) * (startRowIndex - centerY) )/(radial.RadiusY * radial.RadiusY))
                        * 256);
             * but
             *  preComputeForRow= (startRowIndex - centerY) * (startRowIndex - centerY)
             *  so that
             *    currentColorIndexValue = 
             *    (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX)/rx*rx + (preComputeForRow) +));
             */
            double preComputeForRow = 0;

            // this is precompute value so that (* ColorIndexScale / radius) now just ( * preComputeRadiusLookup )
            //double preComputeRadiusLookup = ColorIndexScale / radius;
            double radiusY = radial.RadiusY;
            double radiusX = radial.RadiusX;
            double radiusYSquared = 1 / (radiusY * radiusY);
            double radiusXSquared = 1 / (radiusX * radiusX);

            CellData currentCellData = null;
            uint colorData = 0;
            //uint colorG = 0;
            //uint colorRB = 0;


            int currentColorIndexValue = 0;
            int currentXPosition = 0;

            uint dst, dstRB, dstG;
            #endregion

            #region FILLING
            if (radial.Ramp.NoBlendingColor)
            {
                // when no need to blending, when draw a horizontal line
                // do not need check the back color, alway setup
                if (radial.Style != GradientStyle.Pad)
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        #region precompute for row
                        preComputeForRow = ((startRowIndex - centerY) * (startRowIndex - centerY)) * radiusYSquared;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region even odd change
                                        scLastCoverage &= 511;
                                        if (scLastCoverage >= 256)
                                        {
                                            scLastCoverage = 512 - scLastCoverage - 1;
                                        }
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            //currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                            currentXPosition = scLastX + 1;
                                            if (scLastCoverage >= 255)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue = (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX) * radiusXSquared) + preComputeForRow) * ColorIndexScale);
                                                    #endregion
                                                    BufferData[startXPosition] = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                    startXPosition++;
                                                    currentXPosition++;
                                                }
                                            }
                                            else
                                            {
                                                calculatedCoverage = (byte)scLastCoverage;
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue = (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX) * radiusXSquared) + preComputeForRow) * ColorIndexScale);
                                                    #endregion

                                                    colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                    //calculatedCoverage = (byte)((colorData >> 24));
                                                    //calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                    if (calculatedCoverage >= 254)
                                                    {
                                                        BufferData[startXPosition] = colorData;
                                                    }
                                                    else
                                                    {
                                                        // blend here
                                                        dst = BufferData[startXPosition];
                                                        dstRB = dst & 0x00FF00FF;
                                                        dstG = (dst >> 8) & 0xFF;

                                                        BufferData[startXPosition] =
                                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                    }
                                                    startXPosition++;
                                                    currentXPosition++;
                                                }
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region even odd change
                                        tempCover &= 511;
                                        if (tempCover >= 256)
                                        {
                                            tempCover = 512 - tempCover - 1;
                                        }
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        currentXPosition = currentCellData.X;
                                        currentColorIndexValue = (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX) * radiusXSquared) + preComputeForRow) * ColorIndexScale);
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        //calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        //tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        dst = BufferData[startXPosition];
                                        dstRB = dst & 0x00FF00FF;
                                        dstG = (dst >> 8) & 0xFF;
                                        BufferData[startXPosition] =
                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        #region precompute for row
                        preComputeForRow = ((startRowIndex - centerY) * (startRowIndex - centerY)) * radiusYSquared;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region even odd change
                                        scLastCoverage &= 511;
                                        if (scLastCoverage >= 256)
                                        {
                                            scLastCoverage = 512 - scLastCoverage - 1;
                                        }
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            //currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                            currentXPosition = scLastX + 1;
                                            if (scLastCoverage >= 255)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue = (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX) * radiusXSquared) + preComputeForRow) * ColorIndexScale);
                                                    #endregion
                                                    BufferData[startXPosition] = builtColors[currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                    startXPosition++;
                                                    currentXPosition++;
                                                }
                                            }
                                            else
                                            {
                                                calculatedCoverage = (byte)(scLastCoverage);
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue = (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX) * radiusXSquared) + preComputeForRow) * ColorIndexScale);
                                                    #endregion

                                                    colorData = builtColors[currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                    //calculatedCoverage = (byte)((colorData >> 24));
                                                    //calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                    if (calculatedCoverage >= 254)
                                                    {
                                                        BufferData[startXPosition] = colorData;
                                                    }
                                                    else
                                                    {
                                                        // blend here
                                                        dst = BufferData[startXPosition];
                                                        dstRB = dst & 0x00FF00FF;
                                                        dstG = (dst >> 8) & 0xFF;

                                                        BufferData[startXPosition] =
                                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                    }
                                                    startXPosition++;
                                                    currentXPosition++;
                                                }
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region even odd change
                                        tempCover &= 511;
                                        if (tempCover >= 256)
                                        {
                                            tempCover = 512 - tempCover - 1;
                                        }
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        currentXPosition = currentCellData.X;
                                        currentColorIndexValue = (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX) * radiusXSquared) + preComputeForRow) * ColorIndexScale);
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue > 254 ? 255 : currentColorIndexValue];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        //calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        //tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        dst = BufferData[startXPosition];
                                        dstRB = dst & 0x00FF00FF;
                                        dstG = (dst >> 8) & 0xFF;
                                        BufferData[startXPosition] =
                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
            }
            else
            {
                // when no need to blending, when draw a horizontal line
                // do not need check the back color, alway setup
                if (radial.Style != GradientStyle.Pad)
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        #region precompute for row
                        preComputeForRow = ((startRowIndex - centerY) * (startRowIndex - centerY)) * radiusYSquared;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region even odd change
                                        scLastCoverage &= 511;
                                        if (scLastCoverage >= 256)
                                        {
                                            scLastCoverage = 512 - scLastCoverage - 1;
                                        }
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            //currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                            currentXPosition = scLastX + 1;

                                            while (startXPosition < lastXPosition)
                                            {
                                                #region calculate color index
                                                currentColorIndexValue = (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX) * radiusXSquared) + preComputeForRow) * ColorIndexScale);
                                                #endregion

                                                colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                calculatedCoverage = (byte)((colorData >> 24));
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                if (calculatedCoverage >= 254)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    // blend here
                                                    dst = BufferData[startXPosition];
                                                    dstRB = dst & 0x00FF00FF;
                                                    dstG = (dst >> 8) & 0xFF;

                                                    BufferData[startXPosition] =
                                                        (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                        | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                }
                                                startXPosition++;
                                                currentXPosition++;
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region even odd change
                                        tempCover &= 511;
                                        if (tempCover >= 256)
                                        {
                                            tempCover = 512 - tempCover - 1;
                                        }
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        currentXPosition = currentCellData.X;
                                        currentColorIndexValue = (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX) * radiusXSquared) + preComputeForRow) * ColorIndexScale);
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        //if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        dst = BufferData[startXPosition];
                                        dstRB = dst & 0x00FF00FF;
                                        dstG = (dst >> 8) & 0xFF;
                                        BufferData[startXPosition] =
                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        #region precompute for row
                        preComputeForRow = ((startRowIndex - centerY) * (startRowIndex - centerY)) * radiusYSquared;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region even odd change
                                        scLastCoverage &= 511;
                                        if (scLastCoverage >= 256)
                                        {
                                            scLastCoverage = 512 - scLastCoverage - 1;
                                        }
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            //currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                            currentXPosition = scLastX + 1;

                                            while (startXPosition < lastXPosition)
                                            {
                                                #region calculate color index
                                                currentColorIndexValue = (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX) * radiusXSquared) + preComputeForRow) * ColorIndexScale);
                                                #endregion

                                                colorData = builtColors[currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                calculatedCoverage = (byte)((colorData >> 24));
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                if (calculatedCoverage >= 254)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    // blend here
                                                    dst = BufferData[startXPosition];
                                                    dstRB = dst & 0x00FF00FF;
                                                    dstG = (dst >> 8) & 0xFF;

                                                    BufferData[startXPosition] =
                                                        (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                        | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                }
                                                startXPosition++;
                                                currentXPosition++;
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region even odd change
                                        tempCover &= 511;
                                        if (tempCover >= 256)
                                        {
                                            tempCover = 512 - tempCover - 1;
                                        }
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        currentXPosition = currentCellData.X;
                                        currentColorIndexValue = (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX) * radiusXSquared) + preComputeForRow) * ColorIndexScale);
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue > 254 ? 255 : currentColorIndexValue];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        //if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        dst = BufferData[startXPosition];
                                        dstRB = dst & 0x00FF00FF;
                                        dstG = (dst >> 8) & 0xFF;
                                        BufferData[startXPosition] =
                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
            }

            #endregion
        }
        #endregion

        #region Fill circle, Focus
        /// <summary>
        /// Filling using radial gradient for circle gradient only
        /// </summary>
        /// <param name="radial">radial</param>
        /// <param name="rows">rows</param>
        /// <param name="startRowIndex">start y index</param>
        /// <param name="endRowIndex">end y index</param>
        void FillingRadialFocalEvenOdd(RadialGradient radial, uint opacity, RowData[] rows, int startRowIndex, int endRowIndex)
        {
            // now not need to check null or not
            uint[] builtColors = radial.GetLinearColors(opacity);
            #region private variable for filling
            int currentCoverage, scLastCoverage, scLastX = 0;
            int tempCover = 0;
            int currentArea = 0;
            int lastXPosition = 0;
            int startXPosition = 0;
            byte calculatedCoverage = 0;


            double centerX = radial.CenterX;
            double centerY = radial.CenterY;
            // in this case radius x = radius y
            double radius = radial.RadiusX;

            // this is precompute value so that (* ColorIndexScale / radius) now just ( * preComputeRadiusLookup )
            double preComputeRadiusLookup = ColorIndexScale / radius;

            CellData currentCellData = null;
            uint colorData = 0;

            double dx = 0, dy = 0;

            double dySquared = 0; // saving dy * dy
            // focus is changed to relative from the center
            double absoluteFocusX = radial.FocusX;
            double absoluteFocusY = radial.FocusY;

            double focusX = radial.FocusX - centerX;
            double focusY = radial.FocusY - centerY;

            // note that dx,dy need to move center
            /*
             *  dx = (currentXPosition - absoluteFocusX);
             *  dy = (startRowIndex - absoluteFocusY);
             *  currentColorIndexValue =
                    (int)
                    (
                        (
                            (
                            (dx * focusX) + (dy * focusY)
                            + Math.Sqrt
                            (
                                Math.Abs
                                (
                                    radius * radius * (dx * dx + dy * dy) - (dx * focusY - dy * focusX) * (dx * focusY - dy * focusX)      
                                )
                            )
                        ) * (radius /
                        ((radius * radius) - ((focusX * focusX )+ (focusY * focusY))))
                    ) * 256 /radius
                );
             */

            //note that  ( radius / (( radius * radius) - ((focusX * focusX) + (focusY * focusY))) is const
            // so that need to pre compute
            double preComputeMultiply = radius / ((radius * radius) - ((focusX * focusX) + (focusY * focusY)));

            #region modify when pre compute for multiply is zero
            if (preComputeMultiply == 0)
            {
                if (focusX != 0)
                {
                    if (focusX < 0)
                    {
                        focusX += GradientAdjustment;
                    }
                    else
                    {
                        focusX -= GradientAdjustment;
                    }
                }
                if (focusY != 0)
                {
                    if (focusY < 0)
                    {
                        focusY += GradientAdjustment;
                    }
                    else
                    {
                        focusY -= GradientAdjustment;
                    }
                }
                preComputeMultiply = radius / ((radius * radius) - ((focusX * focusX) + (focusY * focusY)));
            }
            #endregion

            double preComputeMultiplyIncludeLookup = preComputeRadiusLookup * preComputeMultiply;

            // saving dy * focusY
            double dyFocusY = 0;
            double dyFocusX = 0;
            double dxFocusYIncrement = 0; // saving dx * focusY - dyFocusX
            double radiusSquared = radius * radius;


            int currentColorIndexValue = 0;
            //int currentXPosition = 0;
            uint dst, dstRB, dstG;
            #endregion

            #region FILLING
            if (radial.Ramp.NoBlendingColor)
            {
                // when no need to blending, when draw a horizontal line
                // do not need check the back color, alway setup
                if (radial.Style != GradientStyle.Pad)
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;

                        #region cumpute value for row
                        //dyFocusY = (startRowIndex - centerY) * (startRowIndex - centerY);
                        dy = (startRowIndex - absoluteFocusY);
                        dySquared = dy * dy;
                        dyFocusX = dy * focusX;
                        dyFocusY = dy * focusY;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region even odd change
                                        scLastCoverage &= 511;
                                        if (scLastCoverage >= 256)
                                        {
                                            scLastCoverage = 512 - scLastCoverage - 1;
                                        }
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;

                                            #region prepare for row color index calculation
                                            // get current color index value
                                            //currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                            //currentXPosition = scLastX + 1;
                                            dx = (scLastX + 1 - absoluteFocusX);
                                            dxFocusYIncrement = (dx * focusY - dyFocusX);
                                            #endregion
                                            if (scLastCoverage >= 255)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)
                                                        ((((dx * focusX) + dyFocusY +
                                                            Math.Sqrt(Math.Abs(
                                                                radiusSquared *
                                                                (dx * dx + dySquared) -
                                                                dxFocusYIncrement * dxFocusYIncrement))
                                                                ) * preComputeMultiplyIncludeLookup)
                                                        );

                                                    // change for color index calculation
                                                    dx++;
                                                    dxFocusYIncrement += focusY;
                                                    #endregion
                                                    BufferData[startXPosition] = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                    startXPosition++;
                                                }
                                            }
                                            else
                                            {
                                                calculatedCoverage = (byte)scLastCoverage;
                                                while (startXPosition < lastXPosition)
                                                {

                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)
                                                        ((((dx * focusX) + dyFocusY +
                                                            Math.Sqrt(Math.Abs(
                                                                radiusSquared *
                                                                (dx * dx + dySquared) -
                                                                dxFocusYIncrement * dxFocusYIncrement))
                                                                ) * preComputeMultiplyIncludeLookup)
                                                        );

                                                    // change for color index calculation
                                                    dx++;
                                                    dxFocusYIncrement += focusY;
                                                    #endregion

                                                    colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                    //calculatedCoverage = (byte)((colorData >> 24));
                                                    //calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                    if (calculatedCoverage >= 254)
                                                    {
                                                        BufferData[startXPosition] = colorData;
                                                    }
                                                    else
                                                    {
                                                        // blend here
                                                        dst = BufferData[startXPosition];
                                                        dstRB = dst & 0x00FF00FF;
                                                        dstG = (dst >> 8) & 0xFF;

                                                        BufferData[startXPosition] =
                                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                    }
                                                    startXPosition++;

                                                }
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region even odd change
                                        tempCover &= 511;
                                        if (tempCover >= 256)
                                        {
                                            tempCover = 512 - tempCover - 1;
                                        }
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        //currentXPosition = currentCellData.X;
                                        //currentColorIndexValue =
                                        //    (int)(Math.Sqrt(dyFocusY +
                                        //        (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                        #region prepare for row color index calculation
                                        // get current color index value
                                        dx = (currentCellData.X - absoluteFocusX);
                                        dxFocusYIncrement = (dx * focusY - dyFocusX);
                                        #endregion

                                        #region calculate color index
                                        currentColorIndexValue =
                                            (int)
                                            ((((dx * focusX) + dyFocusY +
                                                Math.Sqrt(Math.Abs(
                                                    radiusSquared *
                                                    (dx * dx + dySquared) -
                                                    dxFocusYIncrement * dxFocusYIncrement))
                                                    ) * preComputeMultiplyIncludeLookup)
                                            );
                                        #endregion
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        //calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        //tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        dst = BufferData[startXPosition];
                                        dstRB = dst & 0x00FF00FF;
                                        dstG = (dst >> 8) & 0xFF;
                                        BufferData[startXPosition] =
                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        #region cumpute value for row
                        //dyFocusY = (startRowIndex - centerY) * (startRowIndex - centerY);
                        dy = (startRowIndex - absoluteFocusY);
                        dySquared = dy * dy;
                        dyFocusX = dy * focusX;
                        dyFocusY = dy * focusY;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region even odd change
                                        scLastCoverage &= 511;
                                        if (scLastCoverage >= 256)
                                        {
                                            scLastCoverage = 512 - scLastCoverage - 1;
                                        }
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            #region prepare for row color index calculation
                                            // get current color index value
                                            dx = (scLastX + 1 - absoluteFocusX);
                                            dxFocusYIncrement = (dx * focusY - dyFocusX);
                                            #endregion

                                            if (scLastCoverage >= 255)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)
                                                        ((((dx * focusX) + dyFocusY +
                                                            Math.Sqrt(Math.Abs(
                                                                radiusSquared *
                                                                (dx * dx + dySquared) -
                                                                dxFocusYIncrement * dxFocusYIncrement))
                                                                ) * preComputeMultiplyIncludeLookup)
                                                        );

                                                    // change for color index calculation
                                                    dx++;
                                                    dxFocusYIncrement += focusY;
                                                    #endregion

                                                    BufferData[startXPosition] = builtColors[currentColorIndexValue < 0 ? 0 : currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                    startXPosition++;
                                                }
                                            }
                                            else
                                            {
                                                calculatedCoverage = (byte)(scLastCoverage);
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)
                                                        ((((dx * focusX) + dyFocusY +
                                                            Math.Sqrt(Math.Abs(
                                                                radiusSquared *
                                                                (dx * dx + dySquared) -
                                                                dxFocusYIncrement * dxFocusYIncrement))
                                                                ) * preComputeMultiplyIncludeLookup)
                                                        );

                                                    // change for color index calculation
                                                    dx++;
                                                    dxFocusYIncrement += focusY;
                                                    #endregion
                                                    colorData = builtColors[currentColorIndexValue < 0 ? 0 : currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                    //calculatedCoverage = (byte)((colorData >> 24));
                                                    //calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                    if (calculatedCoverage >= 254)
                                                    {
                                                        BufferData[startXPosition] = colorData;
                                                    }
                                                    else
                                                    {
                                                        // blend here
                                                        dst = BufferData[startXPosition];
                                                        dstRB = dst & 0x00FF00FF;
                                                        dstG = (dst >> 8) & 0xFF;

                                                        BufferData[startXPosition] =
                                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                    }
                                                    startXPosition++;
                                                }
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region even odd change
                                        tempCover &= 511;
                                        if (tempCover >= 256)
                                        {
                                            tempCover = 512 - tempCover - 1;
                                        }
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        #region prepare for row color index calculation
                                        // get current color index value
                                        dx = (currentCellData.X - absoluteFocusX);
                                        dxFocusYIncrement = (dx * focusY - dyFocusX);
                                        #endregion

                                        #region calculate color index
                                        currentColorIndexValue =
                                            (int)
                                            ((((dx * focusX) + dyFocusY +
                                                Math.Sqrt(Math.Abs(
                                                    radiusSquared *
                                                    (dx * dx + dySquared) -
                                                    dxFocusYIncrement * dxFocusYIncrement))
                                                    ) * preComputeMultiplyIncludeLookup)
                                            );
                                        #endregion
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue < 0 ? 0 : currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                        //calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        //tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        dst = BufferData[startXPosition];
                                        dstRB = dst & 0x00FF00FF;
                                        dstG = (dst >> 8) & 0xFF;
                                        BufferData[startXPosition] =
                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
            }
            else
            {
                // when no need to blending, when draw a horizontal line
                // do not need check the back color, alway setup
                if (radial.Style != GradientStyle.Pad)
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        #region cumpute value for row
                        //dyFocusY = (startRowIndex - centerY) * (startRowIndex - centerY);
                        dy = (startRowIndex - absoluteFocusY);
                        dySquared = dy * dy;
                        dyFocusX = dy * focusX;
                        dyFocusY = dy * focusY;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region even odd change
                                        scLastCoverage &= 511;
                                        if (scLastCoverage >= 256)
                                        {
                                            scLastCoverage = 512 - scLastCoverage - 1;
                                        }
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            #region prepare for row color index calculation
                                            // get current color index value
                                            dx = (scLastX + 1 - absoluteFocusX);
                                            dxFocusYIncrement = (dx * focusY - dyFocusX);
                                            #endregion


                                            while (startXPosition < lastXPosition)
                                            {
                                                #region calculate color index
                                                currentColorIndexValue =
                                                    (int)
                                                    ((((dx * focusX) + dyFocusY +
                                                        Math.Sqrt(Math.Abs(
                                                            radiusSquared *
                                                            (dx * dx + dySquared) -
                                                            dxFocusYIncrement * dxFocusYIncrement))
                                                            ) * preComputeMultiplyIncludeLookup)
                                                    );

                                                // change for color index calculation
                                                dx++;
                                                dxFocusYIncrement += focusY;
                                                #endregion
                                                colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                calculatedCoverage = (byte)((colorData >> 24));
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                if (calculatedCoverage >= 254)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    // blend here
                                                    dst = BufferData[startXPosition];
                                                    dstRB = dst & 0x00FF00FF;
                                                    dstG = (dst >> 8) & 0xFF;

                                                    BufferData[startXPosition] =
                                                        (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                        | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                }
                                                startXPosition++;
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region even odd change
                                        tempCover &= 511;
                                        if (tempCover >= 256)
                                        {
                                            tempCover = 512 - tempCover - 1;
                                        }
                                        #endregion
                                        // get current color data
                                        #region prepare for row color index calculation
                                        // get current color index value
                                        dx = (currentCellData.X - absoluteFocusX);
                                        dxFocusYIncrement = (dx * focusY - dyFocusX);
                                        #endregion

                                        #region calculate color index
                                        currentColorIndexValue =
                                            (int)
                                            ((((dx * focusX) + dyFocusY +
                                                Math.Sqrt(Math.Abs(
                                                    radiusSquared *
                                                    (dx * dx + dySquared) -
                                                    dxFocusYIncrement * dxFocusYIncrement))
                                                    ) * preComputeMultiplyIncludeLookup)
                                            );
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        //if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        dst = BufferData[startXPosition];
                                        dstRB = dst & 0x00FF00FF;
                                        dstG = (dst >> 8) & 0xFF;
                                        BufferData[startXPosition] =
                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        #region cumpute value for row
                        //dyFocusY = (startRowIndex - centerY) * (startRowIndex - centerY);
                        dy = (startRowIndex - absoluteFocusY);
                        dySquared = dy * dy;
                        dyFocusX = dy * focusX;
                        dyFocusY = dy * focusY;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region even odd change
                                        scLastCoverage &= 511;
                                        if (scLastCoverage >= 256)
                                        {
                                            scLastCoverage = 512 - scLastCoverage - 1;
                                        }
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            #region prepare for row color index calculation
                                            // get current color index value
                                            dx = (scLastX + 1 - absoluteFocusX);
                                            dxFocusYIncrement = (dx * focusY - dyFocusX);
                                            #endregion


                                            while (startXPosition < lastXPosition)
                                            {
                                                #region calculate color index
                                                currentColorIndexValue =
                                                    (int)
                                                    ((((dx * focusX) + dyFocusY +
                                                        Math.Sqrt(Math.Abs(
                                                            radiusSquared *
                                                            (dx * dx + dySquared) -
                                                            dxFocusYIncrement * dxFocusYIncrement))
                                                            ) * preComputeMultiplyIncludeLookup)
                                                    );

                                                // change for color index calculation
                                                dx++;
                                                dxFocusYIncrement += focusY;
                                                #endregion

                                                colorData = builtColors[currentColorIndexValue < 0 ? 0 : currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                calculatedCoverage = (byte)((colorData >> 24));
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                if (calculatedCoverage >= 254)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    // blend here
                                                    dst = BufferData[startXPosition];
                                                    dstRB = dst & 0x00FF00FF;
                                                    dstG = (dst >> 8) & 0xFF;

                                                    BufferData[startXPosition] =
                                                        (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                        | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                }
                                                startXPosition++;
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region even odd change
                                        tempCover &= 511;
                                        if (tempCover >= 256)
                                        {
                                            tempCover = 512 - tempCover - 1;
                                        }
                                        #endregion
                                        // get current color data
                                        #region prepare for row color index calculation
                                        // get current color index value
                                        dx = (currentCellData.X - absoluteFocusX);
                                        dxFocusYIncrement = (dx * focusY - dyFocusX);
                                        #endregion

                                        #region calculate color index
                                        currentColorIndexValue =
                                            (int)
                                            ((((dx * focusX) + dyFocusY +
                                                Math.Sqrt(Math.Abs(
                                                    radiusSquared *
                                                    (dx * dx + dySquared) -
                                                    dxFocusYIncrement * dxFocusYIncrement))
                                                    ) * preComputeMultiplyIncludeLookup)
                                            );
                                        #endregion
                                        colorData = builtColors[currentColorIndexValue < 0 ? 0 : currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                        calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        //if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        dst = BufferData[startXPosition];
                                        dstRB = dst & 0x00FF00FF;
                                        dstG = (dst >> 8) & 0xFF;
                                        BufferData[startXPosition] =
                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
            }

            #endregion
        }
        #endregion

        #region Fill ellipse, Focus
        /// <summary>
        /// Filling using radial gradient for circle gradient only
        /// </summary>
        /// <param name="radial">radial</param>
        /// <param name="rows">rows</param>
        /// <param name="startRowIndex">start y index</param>
        /// <param name="endRowIndex">end y index</param>
        void FillingEllipseFocalEvenOdd(RadialGradient radial, uint opacity, RowData[] rows, int startRowIndex, int endRowIndex)
        {
            // now not need to check null or not
            uint[] builtColors = radial.GetLinearColors(opacity);
            #region private variable for filling
            int currentCoverage, scLastCoverage, scLastX = 0;
            int tempCover = 0;
            int currentArea = 0;
            int lastXPosition = 0;
            int startXPosition = 0;
            byte calculatedCoverage = 0;


            double centerX = radial.CenterX;
            double centerY = radial.CenterY;
            // in this case radius x = radius y
            double radius = radial.RadiusX;
            double radiusYForX = radial.RadiusY / radial.RadiusX;


            // this is precompute value so that (* ColorIndexScale / radius) now just ( * preComputeRadiusLookup )
            double preComputeRadiusLookup = ColorIndexScale / radius;

            CellData currentCellData = null;
            uint colorData = 0;

            double dx = 0, dy = 0;

            double dySquared = 0; // saving dy * dy
            // focus is changed to relative from the center
            double absoluteFocusX = radial.FocusX;
            double absoluteFocusY = radial.FocusY;

            double focusX = radial.FocusX - centerX;
            double focusY = radial.FocusY - centerY;
            focusY = focusY / radiusYForX;

            // note that dx,dy need to move center
            /*
             *  dx = (currentXPosition - absoluteFocusX);
             *  dy = (startRowIndex - absoluteFocusY);
             *  currentColorIndexValue =
                    (int)
                    (
                        (
                            (
                            (dx * focusX) + (dy * focusY)
                            + Math.Sqrt
                            (
                                Math.Abs
                                (
                                    radius * radius * (dx * dx + dy * dy) - (dx * focusY - dy * focusX) * (dx * focusY - dy * focusX)      
                                )
                            )
                        ) * (radius /
                        ((radius * radius) - ((focusX * focusX )+ (focusY * focusY))))
                    ) * 256 /radius
                );
             */

            //note that  ( radius / (( radius * radius) - ((focusX * focusX) + (focusY * focusY))) is const
            // so that need to pre compute
            double preComputeMultiply = radius / ((radius * radius) - ((focusX * focusX) + (focusY * focusY)));

            #region modify when pre compute for multiply is zero
            if (preComputeMultiply == 0)
            {
                if (focusX != 0)
                {
                    if (focusX < 0)
                    {
                        focusX += GradientAdjustment;
                    }
                    else
                    {
                        focusX -= GradientAdjustment;
                    }
                }
                if (focusY != 0)
                {
                    if (focusY < 0)
                    {
                        focusY += GradientAdjustment;
                    }
                    else
                    {
                        focusY -= GradientAdjustment;
                    }
                }
                preComputeMultiply = radius / ((radius * radius) - ((focusX * focusX) + (focusY * focusY)));
            }
            #endregion

            double preComputeMultiplyIncludeLookup = preComputeRadiusLookup * preComputeMultiply;

            // saving dy * focusY
            double dyFocusY = 0;
            double dyFocusX = 0;
            double dxFocusYIncrement = 0; // saving dx * focusY - dyFocusX
            double radiusSquared = radius * radius;


            int currentColorIndexValue = 0;
            //int currentXPosition = 0;
            uint dst, dstRB, dstG;
            #endregion

            #region FILLING
            if (radial.Ramp.NoBlendingColor)
            {
                // when no need to blending, when draw a horizontal line
                // do not need check the back color, alway setup
                if (radial.Style != GradientStyle.Pad)
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;

                        #region cumpute value for row
                        //dyFocusY = (startRowIndex - centerY) * (startRowIndex - centerY);
                        dy = ((startRowIndex - centerY) / radiusYForX) - focusY;
                        dySquared = dy * dy;
                        dyFocusX = dy * focusX;
                        dyFocusY = dy * focusY;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region even odd change
                                        scLastCoverage &= 511;
                                        if (scLastCoverage >= 256)
                                        {
                                            scLastCoverage = 512 - scLastCoverage - 1;
                                        }
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;

                                            #region prepare for row color index calculation
                                            // get current color index value
                                            //currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                            //currentXPosition = scLastX + 1;
                                            dx = (scLastX + 1 - absoluteFocusX);
                                            dxFocusYIncrement = (dx * focusY - dyFocusX);
                                            #endregion
                                            if (scLastCoverage >= 255)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)
                                                        ((((dx * focusX) + dyFocusY +
                                                            Math.Sqrt(Math.Abs(
                                                                radiusSquared *
                                                                (dx * dx + dySquared) -
                                                                dxFocusYIncrement * dxFocusYIncrement))
                                                                ) * preComputeMultiplyIncludeLookup)
                                                        );

                                                    //currentColorIndexValue =
                                                    //    (int)
                                                    //    (
                                                    //        (
                                                    //            (
                                                    //            (dx * focusX) + (dy * focusY)
                                                    //            + Math.Sqrt
                                                    //            (
                                                    //                Math.Abs
                                                    //                (
                                                    //                    radius * radius 
                                                    //                    * (dx * dx + dy * dy) 
                                                    //                    - (dx * focusY - dy * focusX) 
                                                    //                    * (dx * focusY - dy * focusX)
                                                    //                )
                                                    //            )
                                                    //        ) * (radius /
                                                    //        ((radius * radius) - ((focusX * focusX) + (focusY * focusY))))
                                                    //        ) * 256 / radius
                                                    //    );

                                                    // change for color index calculation
                                                    dx++;
                                                    dxFocusYIncrement += focusY;
                                                    #endregion
                                                    BufferData[startXPosition] = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                    startXPosition++;
                                                }
                                            }
                                            else
                                            {
                                                calculatedCoverage = (byte)scLastCoverage;
                                                while (startXPosition < lastXPosition)
                                                {

                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)
                                                        ((((dx * focusX) + dyFocusY +
                                                            Math.Sqrt(Math.Abs(
                                                                radiusSquared *
                                                                (dx * dx + dySquared) -
                                                                dxFocusYIncrement * dxFocusYIncrement))
                                                                ) * preComputeMultiplyIncludeLookup)
                                                        );

                                                    // change for color index calculation
                                                    dx++;
                                                    dxFocusYIncrement += focusY;
                                                    #endregion

                                                    colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                    //calculatedCoverage = (byte)((colorData >> 24));
                                                    //calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                    if (calculatedCoverage >= 254)
                                                    {
                                                        BufferData[startXPosition] = colorData;
                                                    }
                                                    else
                                                    {
                                                        // blend here
                                                        dst = BufferData[startXPosition];
                                                        dstRB = dst & 0x00FF00FF;
                                                        dstG = (dst >> 8) & 0xFF;

                                                        BufferData[startXPosition] =
                                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                    }
                                                    startXPosition++;

                                                }
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region even odd change
                                        tempCover &= 511;
                                        if (tempCover >= 256)
                                        {
                                            tempCover = 512 - tempCover - 1;
                                        }
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        //currentXPosition = currentCellData.X;
                                        //currentColorIndexValue =
                                        //    (int)(Math.Sqrt(dyFocusY +
                                        //        (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                        #region prepare for row color index calculation
                                        // get current color index value
                                        dx = (currentCellData.X - absoluteFocusX);
                                        dxFocusYIncrement = (dx * focusY - dyFocusX);
                                        #endregion

                                        #region calculate color index
                                        currentColorIndexValue =
                                            (int)
                                            ((((dx * focusX) + dyFocusY +
                                                Math.Sqrt(Math.Abs(
                                                    radiusSquared *
                                                    (dx * dx + dySquared) -
                                                    dxFocusYIncrement * dxFocusYIncrement))
                                                    ) * preComputeMultiplyIncludeLookup)
                                            );
                                        #endregion
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        //calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        //tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        dst = BufferData[startXPosition];
                                        dstRB = dst & 0x00FF00FF;
                                        dstG = (dst >> 8) & 0xFF;
                                        BufferData[startXPosition] =
                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        #region cumpute value for row
                        //dyFocusY = (startRowIndex - centerY) * (startRowIndex - centerY);
                        dy = ((startRowIndex - centerY) / radiusYForX) - focusY;
                        dySquared = dy * dy;
                        dyFocusX = dy * focusX;
                        dyFocusY = dy * focusY;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region even odd change
                                        scLastCoverage &= 511;
                                        if (scLastCoverage >= 256)
                                        {
                                            scLastCoverage = 512 - scLastCoverage - 1;
                                        }
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            #region prepare for row color index calculation
                                            // get current color index value
                                            dx = (scLastX + 1 - absoluteFocusX);
                                            dxFocusYIncrement = (dx * focusY - dyFocusX);
                                            #endregion

                                            if (scLastCoverage >= 255)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)
                                                        ((((dx * focusX) + dyFocusY +
                                                            Math.Sqrt(Math.Abs(
                                                                radiusSquared *
                                                                (dx * dx + dySquared) -
                                                                dxFocusYIncrement * dxFocusYIncrement))
                                                                ) * preComputeMultiplyIncludeLookup)
                                                        );

                                                    // change for color index calculation
                                                    dx++;
                                                    dxFocusYIncrement += focusY;
                                                    #endregion

                                                    BufferData[startXPosition] = builtColors[currentColorIndexValue < 0 ? 0 : currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                    startXPosition++;
                                                }
                                            }
                                            else
                                            {
                                                calculatedCoverage = (byte)(scLastCoverage);
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)
                                                        ((((dx * focusX) + dyFocusY +
                                                            Math.Sqrt(Math.Abs(
                                                                radiusSquared *
                                                                (dx * dx + dySquared) -
                                                                dxFocusYIncrement * dxFocusYIncrement))
                                                                ) * preComputeMultiplyIncludeLookup)
                                                        );

                                                    // change for color index calculation
                                                    dx++;
                                                    dxFocusYIncrement += focusY;
                                                    #endregion
                                                    colorData = builtColors[currentColorIndexValue < 0 ? 0 : currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                    //calculatedCoverage = (byte)((colorData >> 24));
                                                    //calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                    if (calculatedCoverage >= 254)
                                                    {
                                                        BufferData[startXPosition] = colorData;
                                                    }
                                                    else
                                                    {
                                                        // blend here
                                                        dst = BufferData[startXPosition];
                                                        dstRB = dst & 0x00FF00FF;
                                                        dstG = (dst >> 8) & 0xFF;

                                                        BufferData[startXPosition] =
                                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                    }
                                                    startXPosition++;
                                                }
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region even odd change
                                        tempCover &= 511;
                                        if (tempCover >= 256)
                                        {
                                            tempCover = 512 - tempCover - 1;
                                        }
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        #region prepare for row color index calculation
                                        // get current color index value
                                        dx = (currentCellData.X - absoluteFocusX);
                                        dxFocusYIncrement = (dx * focusY - dyFocusX);
                                        #endregion

                                        #region calculate color index
                                        currentColorIndexValue =
                                            (int)
                                            ((((dx * focusX) + dyFocusY +
                                                Math.Sqrt(Math.Abs(
                                                    radiusSquared *
                                                    (dx * dx + dySquared) -
                                                    dxFocusYIncrement * dxFocusYIncrement))
                                                    ) * preComputeMultiplyIncludeLookup)
                                            );
                                        #endregion
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue < 0 ? 0 : currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                        //calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        //tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        dst = BufferData[startXPosition];
                                        dstRB = dst & 0x00FF00FF;
                                        dstG = (dst >> 8) & 0xFF;
                                        BufferData[startXPosition] =
                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
            }
            else
            {
                // when no need to blending, when draw a horizontal line
                // do not need check the back color, alway setup
                if (radial.Style != GradientStyle.Pad)
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        #region cumpute value for row
                        //dyFocusY = (startRowIndex - centerY) * (startRowIndex - centerY);
                        dy = ((startRowIndex - centerY) / radiusYForX) - focusY;
                        dySquared = dy * dy;
                        dyFocusX = dy * focusX;
                        dyFocusY = dy * focusY;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region even odd change
                                        scLastCoverage &= 511;
                                        if (scLastCoverage >= 256)
                                        {
                                            scLastCoverage = 512 - scLastCoverage - 1;
                                        }
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            #region prepare for row color index calculation
                                            // get current color index value
                                            dx = (scLastX + 1 - absoluteFocusX);
                                            dxFocusYIncrement = (dx * focusY - dyFocusX);
                                            #endregion


                                            while (startXPosition < lastXPosition)
                                            {
                                                #region calculate color index
                                                currentColorIndexValue =
                                                    (int)
                                                    ((((dx * focusX) + dyFocusY +
                                                        Math.Sqrt(Math.Abs(
                                                            radiusSquared *
                                                            (dx * dx + dySquared) -
                                                            dxFocusYIncrement * dxFocusYIncrement))
                                                            ) * preComputeMultiplyIncludeLookup)
                                                    );

                                                // change for color index calculation
                                                dx++;
                                                dxFocusYIncrement += focusY;
                                                #endregion
                                                colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                calculatedCoverage = (byte)((colorData >> 24));
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                if (calculatedCoverage >= 254)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    // blend here
                                                    dst = BufferData[startXPosition];
                                                    dstRB = dst & 0x00FF00FF;
                                                    dstG = (dst >> 8) & 0xFF;

                                                    BufferData[startXPosition] =
                                                        (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                        | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                }
                                                startXPosition++;
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region even odd change
                                        tempCover &= 511;
                                        if (tempCover >= 256)
                                        {
                                            tempCover = 512 - tempCover - 1;
                                        }
                                        #endregion
                                        // get current color data
                                        #region prepare for row color index calculation
                                        // get current color index value
                                        dx = (currentCellData.X - absoluteFocusX);
                                        dxFocusYIncrement = (dx * focusY - dyFocusX);
                                        #endregion

                                        #region calculate color index
                                        currentColorIndexValue =
                                            (int)
                                            ((((dx * focusX) + dyFocusY +
                                                Math.Sqrt(Math.Abs(
                                                    radiusSquared *
                                                    (dx * dx + dySquared) -
                                                    dxFocusYIncrement * dxFocusYIncrement))
                                                    ) * preComputeMultiplyIncludeLookup)
                                            );
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        //if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        dst = BufferData[startXPosition];
                                        dstRB = dst & 0x00FF00FF;
                                        dstG = (dst >> 8) & 0xFF;
                                        BufferData[startXPosition] =
                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        #region cumpute value for row
                        //dyFocusY = (startRowIndex - centerY) * (startRowIndex - centerY);
                        dy = ((startRowIndex - centerY) / radiusYForX) - focusY;
                        dySquared = dy * dy;
                        dyFocusX = dy * focusX;
                        dyFocusY = dy * focusY;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region even odd change
                                        scLastCoverage &= 511;
                                        if (scLastCoverage >= 256)
                                        {
                                            scLastCoverage = 512 - scLastCoverage - 1;
                                        }
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            #region prepare for row color index calculation
                                            // get current color index value
                                            dx = (scLastX + 1 - absoluteFocusX);
                                            dxFocusYIncrement = (dx * focusY - dyFocusX);
                                            #endregion


                                            while (startXPosition < lastXPosition)
                                            {
                                                #region calculate color index
                                                currentColorIndexValue =
                                                    (int)
                                                    ((((dx * focusX) + dyFocusY +
                                                        Math.Sqrt(Math.Abs(
                                                            radiusSquared *
                                                            (dx * dx + dySquared) -
                                                            dxFocusYIncrement * dxFocusYIncrement))
                                                            ) * preComputeMultiplyIncludeLookup)
                                                    );

                                                // change for color index calculation
                                                dx++;
                                                dxFocusYIncrement += focusY;
                                                #endregion

                                                colorData = builtColors[currentColorIndexValue < 0 ? 0 : currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                calculatedCoverage = (byte)((colorData >> 24));
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                if (calculatedCoverage >= 254)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    // blend here
                                                    dst = BufferData[startXPosition];
                                                    dstRB = dst & 0x00FF00FF;
                                                    dstG = (dst >> 8) & 0xFF;

                                                    BufferData[startXPosition] =
                                                        (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                        | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                }
                                                startXPosition++;
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region even odd change
                                        tempCover &= 511;
                                        if (tempCover >= 256)
                                        {
                                            tempCover = 512 - tempCover - 1;
                                        }
                                        #endregion
                                        // get current color data
                                        #region prepare for row color index calculation
                                        // get current color index value
                                        dx = (currentCellData.X - absoluteFocusX);
                                        dxFocusYIncrement = (dx * focusY - dyFocusX);
                                        #endregion

                                        #region calculate color index
                                        currentColorIndexValue =
                                            (int)
                                            ((((dx * focusX) + dyFocusY +
                                                Math.Sqrt(Math.Abs(
                                                    radiusSquared *
                                                    (dx * dx + dySquared) -
                                                    dxFocusYIncrement * dxFocusYIncrement))
                                                    ) * preComputeMultiplyIncludeLookup)
                                            );
                                        #endregion
                                        colorData = builtColors[currentColorIndexValue < 0 ? 0 : currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                        calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        //if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        dst = BufferData[startXPosition];
                                        dstRB = dst & 0x00FF00FF;
                                        dstG = (dst >> 8) & 0xFF;
                                        BufferData[startXPosition] =
                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
            }

            #endregion
        }
        #endregion
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
            // this base on paint to filling
            if (!(paint.Paint is RadialGradient))
            {
                //throw new NotImplementedException("Support color paint only");
                NotMatchPaintTypeException.Publish(typeof(RadialGradient), paint.Paint.GetType());
                return;
            }
            RadialGradient radial = paint.Paint as RadialGradient;
            if (radial.RadiusX == radial.RadiusY)
            {
                if ((radial.FocusX == radial.CenterX) && (radial.FocusY == radial.CenterY))
                {
                    // when normal radial gradient
                    FillingRadialEvenOdd(radial, paint.ScaledOpacity, rows, startRowIndex, endRowIndex, gammaLutRed, gammaLutGreen, gammaLutBlue);
                }
                else
                {
                    // circle and focus gradient
                    FillingRadialFocalEvenOdd(radial, paint.ScaledOpacity, rows, startRowIndex, endRowIndex, gammaLutRed, gammaLutGreen, gammaLutBlue);
                }
            }
            else
            {
                if ((radial.FocusX == radial.CenterX) && (radial.FocusY == radial.CenterY))
                {
                    // when normal ellipse gradient
                    FillingEllipseEvenOdd(radial, paint.ScaledOpacity, rows, startRowIndex, endRowIndex, gammaLutRed, gammaLutGreen, gammaLutBlue);
                }
                else
                {
                    // ellipse and focus gradient
                    FillingEllipseFocalEvenOdd(radial, paint.ScaledOpacity, rows, startRowIndex, endRowIndex, gammaLutRed, gammaLutGreen, gammaLutBlue);
                }
            }
        }


        #region Fill normal, radial
        /// <summary>
        /// Filling using radial gradient for circle gradient only
        /// </summary>
        /// <param name="radial">radial</param>
        /// <param name="rows">rows</param>
        /// <param name="startRowIndex">start y index</param>
        /// <param name="endRowIndex">end y index</param>
        /// <param name="gammaLutRed">gamma look up table for red</param>
        /// <param name="gammaLutGreen">gamma look up table for green</param>
        /// <param name="gammaLutBlue">gamma look up table for blue</param>
        void FillingRadialEvenOdd(RadialGradient radial, uint opacity, RowData[] rows, int startRowIndex, int endRowIndex, byte[] gammaLutRed, byte[] gammaLutGreen, byte[] gammaLutBlue)
        {
            // now not need to check null or not
            uint[] builtColors = radial.GetLinearColors(opacity);
            #region private variable for filling
            int currentCoverage, scLastCoverage, scLastX = 0;
            int tempCover = 0;
            int currentArea = 0;
            int lastXPosition = 0;
            int startXPosition = 0;
            byte calculatedCoverage = 0;

            double centerX = radial.CenterX;
            double centerY = radial.CenterY;
            // in this case radius x = radius y
            double radius = radial.RadiusX;

            // saving precompute value for rows
            /* Normal calculation to get the color index
             * currentColorIndexValue =
                (int)(Math.Sqrt(
                    (startRowIndex - centerY) * (startRowIndex - centerY) +
                    (currentXPosition - centerX) * (currentXPosition - centerX)) * ColorIndexScale / radius );
             * but
             *  preComputeForRow= (startRowIndex - centerY) * (startRowIndex - centerY)
             *  so that
             *    currentColorIndexValue = 
             *    (int)(Math.Sqrt(
                    (preComputeForRow) +
                    (currentXPosition - centerX) * (currentXPosition - centerX)) * ColorIndexScale / radius );
             */
            double preComputeForRow = 0;

            // this is precompute value so that (* ColorIndexScale / radius) now just ( * preComputeRadiusLookup )
            double preComputeRadiusLookup = ColorIndexScale / radius;

            CellData currentCellData = null;
            uint colorData = 0;
            //uint colorG = 0;
            //uint colorRB = 0;


            int currentColorIndexValue = 0;
            int currentXPosition = 0;

            uint dst, dstRB, dstG;
            #endregion

            #region FILLING
            if (radial.Ramp.NoBlendingColor)
            {
                // when no need to blending, when draw a horizontal line
                // do not need check the back color, alway setup
                if (radial.Style != GradientStyle.Pad)
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        preComputeForRow = (startRowIndex - centerY) * (startRowIndex - centerY);
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region even odd change
                                        scLastCoverage &= 511;
                                        if (scLastCoverage >= 256)
                                        {
                                            scLastCoverage = 512 - scLastCoverage - 1;
                                        }
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            //currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                            currentXPosition = scLastX + 1;
                                            if (scLastCoverage >= 255)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)(Math.Sqrt(
                                                            preComputeForRow +
                                                            (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                                    #endregion
                                                    BufferData[startXPosition] = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                    startXPosition++;
                                                    currentXPosition++;
                                                }
                                            }
                                            else
                                            {
                                                calculatedCoverage = (byte)scLastCoverage;
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)(Math.Sqrt(
                                                            preComputeForRow +
                                                            (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                                    #endregion

                                                    colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                    //calculatedCoverage = (byte)((colorData >> 24));
                                                    //calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                    if (calculatedCoverage >= 254)
                                                    {
                                                        BufferData[startXPosition] = colorData;
                                                    }
                                                    else
                                                    {
                                                        #region gamma apply
                                                        dst = BufferData[startXPosition];
                                                        dstG = (dst >> 8) & 0xFF;
                                                        dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                        BufferData[startXPosition] =
                                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                            | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                            | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                            ;
                                                        #endregion
                                                    }
                                                    startXPosition++;
                                                    currentXPosition++;
                                                }
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region even odd change
                                        tempCover &= 511;
                                        if (tempCover >= 256)
                                        {
                                            tempCover = 512 - tempCover - 1;
                                        }
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        currentXPosition = currentCellData.X;
                                        currentColorIndexValue =
                                            (int)(Math.Sqrt(preComputeForRow +
                                                (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        //calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        //tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        //dst = BufferData[startXPosition];
                                        //dstRB = dst & 0x00FF00FF;
                                        //dstG = (dst >> 8) & 0xFF;
                                        //BufferData[startXPosition] =
                                        //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        if (calculatedCoverage >= 254)
                                        {
                                            BufferData[startXPosition] = colorData;
                                        }
                                        else
                                        {
                                            #region gamma apply
                                            dst = BufferData[startXPosition];
                                            dstG = (dst >> 8) & 0xFF;
                                            dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                            BufferData[startXPosition] =
                                                (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                ;
                                            #endregion
                                        }
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        preComputeForRow = (startRowIndex - centerY) * (startRowIndex - centerY);
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region even odd change
                                        scLastCoverage &= 511;
                                        if (scLastCoverage >= 256)
                                        {
                                            scLastCoverage = 512 - scLastCoverage - 1;
                                        }
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            //currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                            currentXPosition = scLastX + 1;
                                            if (scLastCoverage >= 255)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)(Math.Sqrt(
                                                            preComputeForRow +
                                                            (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                                    #endregion
                                                    BufferData[startXPosition] = builtColors[currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                    startXPosition++;
                                                    currentXPosition++;
                                                }
                                            }
                                            else
                                            {
                                                calculatedCoverage = (byte)(scLastCoverage);
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)(Math.Sqrt(
                                                            preComputeForRow +
                                                            (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                                    #endregion

                                                    colorData = builtColors[currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                    //calculatedCoverage = (byte)((colorData >> 24));
                                                    //calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                    if (calculatedCoverage >= 254)
                                                    {
                                                        BufferData[startXPosition] = colorData;
                                                    }
                                                    else
                                                    {
                                                        //// blend here
                                                        //dst = BufferData[startXPosition];
                                                        //dstRB = dst & 0x00FF00FF;
                                                        //dstG = (dst >> 8) & 0xFF;

                                                        //BufferData[startXPosition] =
                                                        //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                        //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                        #region gamma apply
                                                        dst = BufferData[startXPosition];
                                                        dstG = (dst >> 8) & 0xFF;
                                                        dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                        BufferData[startXPosition] =
                                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                            | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                            | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                            ;
                                                        #endregion
                                                    }
                                                    startXPosition++;
                                                    currentXPosition++;
                                                }
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region even odd change
                                        tempCover &= 511;
                                        if (tempCover >= 256)
                                        {
                                            tempCover = 512 - tempCover - 1;
                                        }
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        currentXPosition = currentCellData.X;
                                        currentColorIndexValue =
                                            (int)(Math.Sqrt(preComputeForRow +
                                                (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue > 254 ? 255 : currentColorIndexValue];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        //calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        //tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        //dst = BufferData[startXPosition];
                                        //dstRB = dst & 0x00FF00FF;
                                        //dstG = (dst >> 8) & 0xFF;
                                        //BufferData[startXPosition] =
                                        //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        if (calculatedCoverage >= 254)
                                        {
                                            BufferData[startXPosition] = colorData;
                                        }
                                        else
                                        {
                                            #region gamma apply
                                            dst = BufferData[startXPosition];
                                            dstG = (dst >> 8) & 0xFF;
                                            dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                            BufferData[startXPosition] =
                                                (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                ;
                                            #endregion
                                        }
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
            }
            else
            {
                // when no need to blending, when draw a horizontal line
                // do not need check the back color, alway setup
                if (radial.Style != GradientStyle.Pad)
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        preComputeForRow = (startRowIndex - centerY) * (startRowIndex - centerY);
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region even odd change
                                        scLastCoverage &= 511;
                                        if (scLastCoverage >= 256)
                                        {
                                            scLastCoverage = 512 - scLastCoverage - 1;
                                        }
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            //currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                            currentXPosition = scLastX + 1;

                                            while (startXPosition < lastXPosition)
                                            {
                                                #region calculate color index
                                                currentColorIndexValue =
                                                    (int)(Math.Sqrt(
                                                        preComputeForRow +
                                                        (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                                #endregion

                                                colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                calculatedCoverage = (byte)((colorData >> 24));
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                if (calculatedCoverage >= 254)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    #region gamma apply
                                                    dst = BufferData[startXPosition];
                                                    dstG = (dst >> 8) & 0xFF;
                                                    dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                    BufferData[startXPosition] =
                                                        (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                        | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                        | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                        ;
                                                    #endregion
                                                }
                                                startXPosition++;
                                                currentXPosition++;
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region even odd change
                                        tempCover &= 511;
                                        if (tempCover >= 256)
                                        {
                                            tempCover = 512 - tempCover - 1;
                                        }
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        currentXPosition = currentCellData.X;
                                        currentColorIndexValue =
                                            (int)(Math.Sqrt(preComputeForRow +
                                                (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        //if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        //dst = BufferData[startXPosition];
                                        //dstRB = dst & 0x00FF00FF;
                                        //dstG = (dst >> 8) & 0xFF;
                                        //BufferData[startXPosition] =
                                        //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #region gamma apply
                                        dst = BufferData[startXPosition];
                                        dstG = (dst >> 8) & 0xFF;
                                        dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                        BufferData[startXPosition] =
                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                            | (gammaLutBlue[(dstRB & 0x00FF)]))
                                            ;
                                        #endregion
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        preComputeForRow = (startRowIndex - centerY) * (startRowIndex - centerY);
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region even odd change
                                        scLastCoverage &= 511;
                                        if (scLastCoverage >= 256)
                                        {
                                            scLastCoverage = 512 - scLastCoverage - 1;
                                        }
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            //currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                            currentXPosition = scLastX + 1;

                                            while (startXPosition < lastXPosition)
                                            {
                                                #region calculate color index
                                                currentColorIndexValue =
                                                    (int)(Math.Sqrt(
                                                        preComputeForRow +
                                                        (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                                #endregion

                                                colorData = builtColors[currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                calculatedCoverage = (byte)((colorData >> 24));
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                if (calculatedCoverage >= 254)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    //// blend here
                                                    //dst = BufferData[startXPosition];
                                                    //dstRB = dst & 0x00FF00FF;
                                                    //dstG = (dst >> 8) & 0xFF;

                                                    //BufferData[startXPosition] =
                                                    //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                    //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                    //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                                    #region gamma apply
                                                    dst = BufferData[startXPosition];
                                                    dstG = (dst >> 8) & 0xFF;
                                                    dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                    BufferData[startXPosition] =
                                                        (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                        | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                        | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                        ;
                                                    #endregion
                                                }
                                                startXPosition++;
                                                currentXPosition++;
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region even odd change
                                        tempCover &= 511;
                                        if (tempCover >= 256)
                                        {
                                            tempCover = 512 - tempCover - 1;
                                        }
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        currentXPosition = currentCellData.X;
                                        currentColorIndexValue =
                                            (int)(Math.Sqrt(preComputeForRow +
                                                (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue > 254 ? 255 : currentColorIndexValue];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        //if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;
                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        //dst = BufferData[startXPosition];
                                        //dstRB = dst & 0x00FF00FF;
                                        //dstG = (dst >> 8) & 0xFF;
                                        //BufferData[startXPosition] =
                                        //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #region gamma apply
                                        dst = BufferData[startXPosition];
                                        dstG = (dst >> 8) & 0xFF;
                                        dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                        BufferData[startXPosition] =
                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                            | (gammaLutBlue[(dstRB & 0x00FF)]))
                                            ;
                                        #endregion
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
            }

            #endregion
        }
        #endregion

        #region fill ellipse
        /// <summary>
        /// Filling using ellipse gradient for circle gradient only
        /// </summary>
        /// <param name="radial">radial</param>
        /// <param name="rows">rows</param>
        /// <param name="startRowIndex">start y index</param>
        /// <param name="endRowIndex">end y index</param>
        /// <param name="gammaLutRed">gamma look up table for red</param>
        /// <param name="gammaLutGreen">gamma look up table for green</param>
        /// <param name="gammaLutBlue">gamma look up table for blue</param>
        void FillingEllipseEvenOdd(RadialGradient radial, uint opacity, RowData[] rows, int startRowIndex, int endRowIndex, byte[] gammaLutRed, byte[] gammaLutGreen, byte[] gammaLutBlue)
        {
            // now not need to check null or not
            uint[] builtColors = radial.GetLinearColors(opacity);
            #region private variable for filling
            int currentCoverage, scLastCoverage, scLastX = 0;
            int tempCover = 0;
            int currentArea = 0;
            int lastXPosition = 0;
            int startXPosition = 0;
            byte calculatedCoverage = 0;

            double centerX = radial.CenterX;
            double centerY = radial.CenterY;
            // in this case radius x = radius y
            double radius = radial.RadiusX;

            // saving precompute value for rows
            /* Normal calculation to get the color index
             *  currentColorIndexValue =
                    (int)(Math.Sqrt(  
                            ((currentXPosition-centerX) * (currentXPosition-centerX) /(radial.RadiusX * radial.RadiusX))
                            +
                            ((startRowIndex - centerY) * (startRowIndex - centerY) )/(radial.RadiusY * radial.RadiusY))
                        * 256);
             * but
             *  preComputeForRow= (startRowIndex - centerY) * (startRowIndex - centerY)
             *  so that
             *    currentColorIndexValue = 
             *    (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX)/rx*rx + (preComputeForRow) +));
             */
            double preComputeForRow = 0;

            // this is precompute value so that (* ColorIndexScale / radius) now just ( * preComputeRadiusLookup )
            //double preComputeRadiusLookup = ColorIndexScale / radius;
            double radiusY = radial.RadiusY;
            double radiusX = radial.RadiusX;
            double radiusYSquared = 1 / (radiusY * radiusY);
            double radiusXSquared = 1 / (radiusX * radiusX);

            CellData currentCellData = null;
            uint colorData = 0;
            //uint colorG = 0;
            //uint colorRB = 0;


            int currentColorIndexValue = 0;
            int currentXPosition = 0;

            uint dst, dstRB, dstG;
            #endregion

            #region FILLING
            if (radial.Ramp.NoBlendingColor)
            {
                // when no need to blending, when draw a horizontal line
                // do not need check the back color, alway setup
                if (radial.Style != GradientStyle.Pad)
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        #region precompute for row
                        preComputeForRow = ((startRowIndex - centerY) * (startRowIndex - centerY)) * radiusYSquared;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region even odd change
                                        scLastCoverage &= 511;
                                        if (scLastCoverage >= 256)
                                        {
                                            scLastCoverage = 512 - scLastCoverage - 1;
                                        }
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            //currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                            currentXPosition = scLastX + 1;
                                            if (scLastCoverage >= 255)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue = (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX) * radiusXSquared) + preComputeForRow) * ColorIndexScale);
                                                    #endregion
                                                    BufferData[startXPosition] = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                    startXPosition++;
                                                    currentXPosition++;
                                                }
                                            }
                                            else
                                            {
                                                calculatedCoverage = (byte)scLastCoverage;
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue = (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX) * radiusXSquared) + preComputeForRow) * ColorIndexScale);
                                                    #endregion

                                                    colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                    //calculatedCoverage = (byte)((colorData >> 24));
                                                    //calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                    if (calculatedCoverage >= 254)
                                                    {
                                                        BufferData[startXPosition] = colorData;
                                                    }
                                                    else
                                                    {
                                                        // blend here
                                                        //dst = BufferData[startXPosition];
                                                        //dstRB = dst & 0x00FF00FF;
                                                        //dstG = (dst >> 8) & 0xFF;

                                                        //BufferData[startXPosition] =
                                                        //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                        //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                        #region gamma apply
                                                        dst = BufferData[startXPosition];
                                                        dstG = (dst >> 8) & 0xFF;
                                                        dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                        BufferData[startXPosition] =
                                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                            | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                            | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                            ;
                                                        #endregion
                                                    }
                                                    startXPosition++;
                                                    currentXPosition++;
                                                }
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region even odd change
                                        tempCover &= 511;
                                        if (tempCover >= 256)
                                        {
                                            tempCover = 512 - tempCover - 1;
                                        }
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        currentXPosition = currentCellData.X;
                                        currentColorIndexValue = (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX) * radiusXSquared) + preComputeForRow) * ColorIndexScale);
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        //calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        //tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        //dst = BufferData[startXPosition];
                                        //dstRB = dst & 0x00FF00FF;
                                        //dstG = (dst >> 8) & 0xFF;
                                        //BufferData[startXPosition] =
                                        //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                        if (calculatedCoverage >= 254)
                                        {
                                            BufferData[startXPosition] = colorData;
                                        }
                                        else
                                        {
                                            #region gamma apply
                                            dst = BufferData[startXPosition];
                                            dstG = (dst >> 8) & 0xFF;
                                            dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                            BufferData[startXPosition] =
                                                (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                ;
                                            #endregion
                                        }
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        #region precompute for row
                        preComputeForRow = ((startRowIndex - centerY) * (startRowIndex - centerY)) * radiusYSquared;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region even odd change
                                        scLastCoverage &= 511;
                                        if (scLastCoverage >= 256)
                                        {
                                            scLastCoverage = 512 - scLastCoverage - 1;
                                        }
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            //currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                            currentXPosition = scLastX + 1;
                                            if (scLastCoverage >= 255)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue = (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX) * radiusXSquared) + preComputeForRow) * ColorIndexScale);
                                                    #endregion
                                                    BufferData[startXPosition] = builtColors[currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                    startXPosition++;
                                                    currentXPosition++;
                                                }
                                            }
                                            else
                                            {
                                                calculatedCoverage = (byte)(scLastCoverage);
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue = (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX) * radiusXSquared) + preComputeForRow) * ColorIndexScale);
                                                    #endregion

                                                    colorData = builtColors[currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                    //calculatedCoverage = (byte)((colorData >> 24));
                                                    //calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                    if (calculatedCoverage >= 254)
                                                    {
                                                        BufferData[startXPosition] = colorData;
                                                    }
                                                    else
                                                    {
                                                        // blend here
                                                        //dst = BufferData[startXPosition];
                                                        //dstRB = dst & 0x00FF00FF;
                                                        //dstG = (dst >> 8) & 0xFF;

                                                        //BufferData[startXPosition] =
                                                        //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                        //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                        #region gamma apply
                                                        dst = BufferData[startXPosition];
                                                        dstG = (dst >> 8) & 0xFF;
                                                        dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                        BufferData[startXPosition] =
                                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                            | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                            | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                            ;
                                                        #endregion
                                                    }
                                                    startXPosition++;
                                                    currentXPosition++;
                                                }
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region even odd change
                                        tempCover &= 511;
                                        if (tempCover >= 256)
                                        {
                                            tempCover = 512 - tempCover - 1;
                                        }
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        currentXPosition = currentCellData.X;
                                        currentColorIndexValue = (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX) * radiusXSquared) + preComputeForRow) * ColorIndexScale);
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue > 254 ? 255 : currentColorIndexValue];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        //calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        //tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        //dst = BufferData[startXPosition];
                                        //dstRB = dst & 0x00FF00FF;
                                        //dstG = (dst >> 8) & 0xFF;
                                        //BufferData[startXPosition] =
                                        //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                        if (calculatedCoverage >= 254)
                                        {
                                            BufferData[startXPosition] = colorData;
                                        }
                                        else
                                        {
                                            #region gamma apply
                                            dst = BufferData[startXPosition];
                                            dstG = (dst >> 8) & 0xFF;
                                            dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                            BufferData[startXPosition] =
                                                (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                ;
                                            #endregion
                                        }
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
            }
            else
            {
                // when no need to blending, when draw a horizontal line
                // do not need check the back color, alway setup
                if (radial.Style != GradientStyle.Pad)
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        #region precompute for row
                        preComputeForRow = ((startRowIndex - centerY) * (startRowIndex - centerY)) * radiusYSquared;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region even odd change
                                        scLastCoverage &= 511;
                                        if (scLastCoverage >= 256)
                                        {
                                            scLastCoverage = 512 - scLastCoverage - 1;
                                        }
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            //currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                            currentXPosition = scLastX + 1;

                                            while (startXPosition < lastXPosition)
                                            {
                                                #region calculate color index
                                                currentColorIndexValue = (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX) * radiusXSquared) + preComputeForRow) * ColorIndexScale);
                                                #endregion

                                                colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                calculatedCoverage = (byte)((colorData >> 24));
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                if (calculatedCoverage >= 254)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    //// blend here
                                                    //dst = BufferData[startXPosition];
                                                    //dstRB = dst & 0x00FF00FF;
                                                    //dstG = (dst >> 8) & 0xFF;

                                                    //BufferData[startXPosition] =
                                                    //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                    //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                    //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                                    #region gamma apply
                                                    dst = BufferData[startXPosition];
                                                    dstG = (dst >> 8) & 0xFF;
                                                    dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                    BufferData[startXPosition] =
                                                        (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                        | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                        | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                        ;
                                                    #endregion
                                                }
                                                startXPosition++;
                                                currentXPosition++;
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region even odd change
                                        tempCover &= 511;
                                        if (tempCover >= 256)
                                        {
                                            tempCover = 512 - tempCover - 1;
                                        }
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        currentXPosition = currentCellData.X;
                                        currentColorIndexValue = (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX) * radiusXSquared) + preComputeForRow) * ColorIndexScale);
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        //if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        //dst = BufferData[startXPosition];
                                        //dstRB = dst & 0x00FF00FF;
                                        //dstG = (dst >> 8) & 0xFF;
                                        //BufferData[startXPosition] =
                                        //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #region gamma apply
                                        dst = BufferData[startXPosition];
                                        dstG = (dst >> 8) & 0xFF;
                                        dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                        BufferData[startXPosition] =
                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                            | (gammaLutBlue[(dstRB & 0x00FF)]))
                                            ;
                                        #endregion
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        #region precompute for row
                        preComputeForRow = ((startRowIndex - centerY) * (startRowIndex - centerY)) * radiusYSquared;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region even odd change
                                        scLastCoverage &= 511;
                                        if (scLastCoverage >= 256)
                                        {
                                            scLastCoverage = 512 - scLastCoverage - 1;
                                        }
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            //currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                            currentXPosition = scLastX + 1;

                                            while (startXPosition < lastXPosition)
                                            {
                                                #region calculate color index
                                                currentColorIndexValue = (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX) * radiusXSquared) + preComputeForRow) * ColorIndexScale);
                                                #endregion

                                                colorData = builtColors[currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                calculatedCoverage = (byte)((colorData >> 24));
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                if (calculatedCoverage >= 254)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    //// blend here
                                                    //dst = BufferData[startXPosition];
                                                    //dstRB = dst & 0x00FF00FF;
                                                    //dstG = (dst >> 8) & 0xFF;

                                                    //BufferData[startXPosition] =
                                                    //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                    //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                    //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                    #region gamma apply
                                                    dst = BufferData[startXPosition];
                                                    dstG = (dst >> 8) & 0xFF;
                                                    dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                    BufferData[startXPosition] =
                                                        (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                        | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                        | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                        ;
                                                    #endregion
                                                }
                                                startXPosition++;
                                                currentXPosition++;
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region even odd change
                                        tempCover &= 511;
                                        if (tempCover >= 256)
                                        {
                                            tempCover = 512 - tempCover - 1;
                                        }
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        currentXPosition = currentCellData.X;
                                        currentColorIndexValue = (int)(Math.Sqrt(((currentXPosition - centerX) * (currentXPosition - centerX) * radiusXSquared) + preComputeForRow) * ColorIndexScale);
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue > 254 ? 255 : currentColorIndexValue];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        //if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        //dst = BufferData[startXPosition];
                                        //dstRB = dst & 0x00FF00FF;
                                        //dstG = (dst >> 8) & 0xFF;
                                        //BufferData[startXPosition] =
                                        //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #region gamma apply
                                        dst = BufferData[startXPosition];
                                        dstG = (dst >> 8) & 0xFF;
                                        dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                        BufferData[startXPosition] =
                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                            | (gammaLutBlue[(dstRB & 0x00FF)]))
                                            ;
                                        #endregion
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
            }

            #endregion
        }
        #endregion

        #region Fill circle, Focus
        /// <summary>
        /// Filling using radial gradient for circle gradient only
        /// </summary>
        /// <param name="radial">radial</param>
        /// <param name="rows">rows</param>
        /// <param name="startRowIndex">start y index</param>
        /// <param name="endRowIndex">end y index</param>
        /// <param name="gammaLutRed">gamma look up table for red</param>
        /// <param name="gammaLutGreen">gamma look up table for green</param>
        /// <param name="gammaLutBlue">gamma look up table for blue</param>
        void FillingRadialFocalEvenOdd(RadialGradient radial, uint opacity, RowData[] rows, int startRowIndex, int endRowIndex, byte[] gammaLutRed, byte[] gammaLutGreen, byte[] gammaLutBlue)
        {
            // now not need to check null or not
            uint[] builtColors = radial.GetLinearColors(opacity);
            #region private variable for filling
            int currentCoverage, scLastCoverage, scLastX = 0;
            int tempCover = 0;
            int currentArea = 0;
            int lastXPosition = 0;
            int startXPosition = 0;
            byte calculatedCoverage = 0;


            double centerX = radial.CenterX;
            double centerY = radial.CenterY;
            // in this case radius x = radius y
            double radius = radial.RadiusX;

            // this is precompute value so that (* ColorIndexScale / radius) now just ( * preComputeRadiusLookup )
            double preComputeRadiusLookup = ColorIndexScale / radius;

            CellData currentCellData = null;
            uint colorData = 0;

            double dx = 0, dy = 0;

            double dySquared = 0; // saving dy * dy
            // focus is changed to relative from the center
            double absoluteFocusX = radial.FocusX;
            double absoluteFocusY = radial.FocusY;

            double focusX = radial.FocusX - centerX;
            double focusY = radial.FocusY - centerY;

            // note that dx,dy need to move center
            /*
             *  dx = (currentXPosition - absoluteFocusX);
             *  dy = (startRowIndex - absoluteFocusY);
             *  currentColorIndexValue =
                    (int)
                    (
                        (
                            (
                            (dx * focusX) + (dy * focusY)
                            + Math.Sqrt
                            (
                                Math.Abs
                                (
                                    radius * radius * (dx * dx + dy * dy) - (dx * focusY - dy * focusX) * (dx * focusY - dy * focusX)      
                                )
                            )
                        ) * (radius /
                        ((radius * radius) - ((focusX * focusX )+ (focusY * focusY))))
                    ) * 256 /radius
                );
             */

            //note that  ( radius / (( radius * radius) - ((focusX * focusX) + (focusY * focusY))) is const
            // so that need to pre compute
            double preComputeMultiply = radius / ((radius * radius) - ((focusX * focusX) + (focusY * focusY)));

            #region modify when pre compute for multiply is zero
            if (preComputeMultiply == 0)
            {
                if (focusX != 0)
                {
                    if (focusX < 0)
                    {
                        focusX += GradientAdjustment;
                    }
                    else
                    {
                        focusX -= GradientAdjustment;
                    }
                }
                if (focusY != 0)
                {
                    if (focusY < 0)
                    {
                        focusY += GradientAdjustment;
                    }
                    else
                    {
                        focusY -= GradientAdjustment;
                    }
                }
                preComputeMultiply = radius / ((radius * radius) - ((focusX * focusX) + (focusY * focusY)));
            }
            #endregion

            double preComputeMultiplyIncludeLookup = preComputeRadiusLookup * preComputeMultiply;

            // saving dy * focusY
            double dyFocusY = 0;
            double dyFocusX = 0;
            double dxFocusYIncrement = 0; // saving dx * focusY - dyFocusX
            double radiusSquared = radius * radius;


            int currentColorIndexValue = 0;
            //int currentXPosition = 0;
            uint dst, dstRB, dstG;
            #endregion

            #region FILLING
            if (radial.Ramp.NoBlendingColor)
            {
                // when no need to blending, when draw a horizontal line
                // do not need check the back color, alway setup
                if (radial.Style != GradientStyle.Pad)
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;

                        #region cumpute value for row
                        //dyFocusY = (startRowIndex - centerY) * (startRowIndex - centerY);
                        dy = (startRowIndex - absoluteFocusY);
                        dySquared = dy * dy;
                        dyFocusX = dy * focusX;
                        dyFocusY = dy * focusY;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region even odd change
                                        scLastCoverage &= 511;
                                        if (scLastCoverage >= 256)
                                        {
                                            scLastCoverage = 512 - scLastCoverage - 1;
                                        }
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;

                                            #region prepare for row color index calculation
                                            // get current color index value
                                            //currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                            //currentXPosition = scLastX + 1;
                                            dx = (scLastX + 1 - absoluteFocusX);
                                            dxFocusYIncrement = (dx * focusY - dyFocusX);
                                            #endregion
                                            if (scLastCoverage >= 255)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)
                                                        ((((dx * focusX) + dyFocusY +
                                                            Math.Sqrt(Math.Abs(
                                                                radiusSquared *
                                                                (dx * dx + dySquared) -
                                                                dxFocusYIncrement * dxFocusYIncrement))
                                                                ) * preComputeMultiplyIncludeLookup)
                                                        );

                                                    // change for color index calculation
                                                    dx++;
                                                    dxFocusYIncrement += focusY;
                                                    #endregion
                                                    BufferData[startXPosition] = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                    startXPosition++;
                                                }
                                            }
                                            else
                                            {
                                                calculatedCoverage = (byte)scLastCoverage;
                                                while (startXPosition < lastXPosition)
                                                {

                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)
                                                        ((((dx * focusX) + dyFocusY +
                                                            Math.Sqrt(Math.Abs(
                                                                radiusSquared *
                                                                (dx * dx + dySquared) -
                                                                dxFocusYIncrement * dxFocusYIncrement))
                                                                ) * preComputeMultiplyIncludeLookup)
                                                        );

                                                    // change for color index calculation
                                                    dx++;
                                                    dxFocusYIncrement += focusY;
                                                    #endregion

                                                    colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                    //calculatedCoverage = (byte)((colorData >> 24));
                                                    //calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                    if (calculatedCoverage >= 254)
                                                    {
                                                        BufferData[startXPosition] = colorData;
                                                    }
                                                    else
                                                    {
                                                        //// blend here
                                                        //dst = BufferData[startXPosition];
                                                        //dstRB = dst & 0x00FF00FF;
                                                        //dstG = (dst >> 8) & 0xFF;

                                                        //BufferData[startXPosition] =
                                                        //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                        //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                                        #region gamma apply
                                                        dst = BufferData[startXPosition];
                                                        dstG = (dst >> 8) & 0xFF;
                                                        dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                        BufferData[startXPosition] =
                                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                            | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                            | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                            ;
                                                        #endregion
                                                    }
                                                    startXPosition++;

                                                }
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region even odd change
                                        tempCover &= 511;
                                        if (tempCover >= 256)
                                        {
                                            tempCover = 512 - tempCover - 1;
                                        }
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        //currentXPosition = currentCellData.X;
                                        //currentColorIndexValue =
                                        //    (int)(Math.Sqrt(dyFocusY +
                                        //        (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                        #region prepare for row color index calculation
                                        // get current color index value
                                        dx = (currentCellData.X - absoluteFocusX);
                                        dxFocusYIncrement = (dx * focusY - dyFocusX);
                                        #endregion

                                        #region calculate color index
                                        currentColorIndexValue =
                                            (int)
                                            ((((dx * focusX) + dyFocusY +
                                                Math.Sqrt(Math.Abs(
                                                    radiusSquared *
                                                    (dx * dx + dySquared) -
                                                    dxFocusYIncrement * dxFocusYIncrement))
                                                    ) * preComputeMultiplyIncludeLookup)
                                            );
                                        #endregion
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        //calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        //tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        //dst = BufferData[startXPosition];
                                        //dstRB = dst & 0x00FF00FF;
                                        //dstG = (dst >> 8) & 0xFF;
                                        //BufferData[startXPosition] =
                                        //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        if (calculatedCoverage >= 254)
                                        {
                                            BufferData[startXPosition] = colorData;
                                        }
                                        else
                                        {
                                            #region gamma apply
                                            dst = BufferData[startXPosition];
                                            dstG = (dst >> 8) & 0xFF;
                                            dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                            BufferData[startXPosition] =
                                                (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                ;
                                            #endregion
                                        }
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        #region cumpute value for row
                        //dyFocusY = (startRowIndex - centerY) * (startRowIndex - centerY);
                        dy = (startRowIndex - absoluteFocusY);
                        dySquared = dy * dy;
                        dyFocusX = dy * focusX;
                        dyFocusY = dy * focusY;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region even odd change
                                        scLastCoverage &= 511;
                                        if (scLastCoverage >= 256)
                                        {
                                            scLastCoverage = 512 - scLastCoverage - 1;
                                        }
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            #region prepare for row color index calculation
                                            // get current color index value
                                            dx = (scLastX + 1 - absoluteFocusX);
                                            dxFocusYIncrement = (dx * focusY - dyFocusX);
                                            #endregion

                                            if (scLastCoverage >= 255)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)
                                                        ((((dx * focusX) + dyFocusY +
                                                            Math.Sqrt(Math.Abs(
                                                                radiusSquared *
                                                                (dx * dx + dySquared) -
                                                                dxFocusYIncrement * dxFocusYIncrement))
                                                                ) * preComputeMultiplyIncludeLookup)
                                                        );

                                                    // change for color index calculation
                                                    dx++;
                                                    dxFocusYIncrement += focusY;
                                                    #endregion

                                                    BufferData[startXPosition] = builtColors[currentColorIndexValue < 0 ? 0 : currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                    startXPosition++;
                                                }
                                            }
                                            else
                                            {
                                                calculatedCoverage = (byte)(scLastCoverage);
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)
                                                        ((((dx * focusX) + dyFocusY +
                                                            Math.Sqrt(Math.Abs(
                                                                radiusSquared *
                                                                (dx * dx + dySquared) -
                                                                dxFocusYIncrement * dxFocusYIncrement))
                                                                ) * preComputeMultiplyIncludeLookup)
                                                        );

                                                    // change for color index calculation
                                                    dx++;
                                                    dxFocusYIncrement += focusY;
                                                    #endregion
                                                    colorData = builtColors[currentColorIndexValue < 0 ? 0 : currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                    //calculatedCoverage = (byte)((colorData >> 24));
                                                    //calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                    if (calculatedCoverage >= 254)
                                                    {
                                                        BufferData[startXPosition] = colorData;
                                                    }
                                                    else
                                                    {
                                                        // blend here
                                                        #region gamma apply
                                                        dst = BufferData[startXPosition];
                                                        dstG = (dst >> 8) & 0xFF;
                                                        dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                        BufferData[startXPosition] =
                                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                            | (((uint)(uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                            | ((uint)(uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                            | ((uint)gammaLutBlue[(dstRB & 0x00FF)]))
                                                            ;
                                                        #endregion
                                                    }
                                                    startXPosition++;
                                                }
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region even odd change
                                        tempCover &= 511;
                                        if (tempCover >= 256)
                                        {
                                            tempCover = 512 - tempCover - 1;
                                        }
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        #region prepare for row color index calculation
                                        // get current color index value
                                        dx = (currentCellData.X - absoluteFocusX);
                                        dxFocusYIncrement = (dx * focusY - dyFocusX);
                                        #endregion

                                        #region calculate color index
                                        currentColorIndexValue =
                                            (int)
                                            ((((dx * focusX) + dyFocusY +
                                                Math.Sqrt(Math.Abs(
                                                    radiusSquared *
                                                    (dx * dx + dySquared) -
                                                    dxFocusYIncrement * dxFocusYIncrement))
                                                    ) * preComputeMultiplyIncludeLookup)
                                            );
                                        #endregion
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue < 0 ? 0 : currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                        //calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        //tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        //dst = BufferData[startXPosition];
                                        //dstRB = dst & 0x00FF00FF;
                                        //dstG = (dst >> 8) & 0xFF;
                                        //BufferData[startXPosition] =
                                        //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                        if (calculatedCoverage >= 254)
                                        {
                                            BufferData[startXPosition] = colorData;
                                        }
                                        else
                                        {
                                            #region gamma apply
                                            dst = BufferData[startXPosition];
                                            dstG = (dst >> 8) & 0xFF;
                                            dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                            BufferData[startXPosition] =
                                                (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                ;
                                            #endregion
                                        }
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
            }
            else
            {
                // when no need to blending, when draw a horizontal line
                // do not need check the back color, alway setup
                if (radial.Style != GradientStyle.Pad)
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        #region cumpute value for row
                        //dyFocusY = (startRowIndex - centerY) * (startRowIndex - centerY);
                        dy = (startRowIndex - absoluteFocusY);
                        dySquared = dy * dy;
                        dyFocusX = dy * focusX;
                        dyFocusY = dy * focusY;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region even odd change
                                        scLastCoverage &= 511;
                                        if (scLastCoverage >= 256)
                                        {
                                            scLastCoverage = 512 - scLastCoverage - 1;
                                        }
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            #region prepare for row color index calculation
                                            // get current color index value
                                            dx = (scLastX + 1 - absoluteFocusX);
                                            dxFocusYIncrement = (dx * focusY - dyFocusX);
                                            #endregion


                                            while (startXPosition < lastXPosition)
                                            {
                                                #region calculate color index
                                                currentColorIndexValue =
                                                    (int)
                                                    ((((dx * focusX) + dyFocusY +
                                                        Math.Sqrt(Math.Abs(
                                                            radiusSquared *
                                                            (dx * dx + dySquared) -
                                                            dxFocusYIncrement * dxFocusYIncrement))
                                                            ) * preComputeMultiplyIncludeLookup)
                                                    );

                                                // change for color index calculation
                                                dx++;
                                                dxFocusYIncrement += focusY;
                                                #endregion
                                                colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                calculatedCoverage = (byte)((colorData >> 24));
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                if (calculatedCoverage >= 254)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    #region gamma apply
                                                    dst = BufferData[startXPosition];
                                                    dstG = (dst >> 8) & 0xFF;
                                                    dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                    BufferData[startXPosition] =
                                                        (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                        | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                        | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                        ;
                                                    #endregion
                                                }
                                                startXPosition++;
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region even odd change
                                        tempCover &= 511;
                                        if (tempCover >= 256)
                                        {
                                            tempCover = 512 - tempCover - 1;
                                        }
                                        #endregion
                                        // get current color data
                                        #region prepare for row color index calculation
                                        // get current color index value
                                        dx = (currentCellData.X - absoluteFocusX);
                                        dxFocusYIncrement = (dx * focusY - dyFocusX);
                                        #endregion

                                        #region calculate color index
                                        currentColorIndexValue =
                                            (int)
                                            ((((dx * focusX) + dyFocusY +
                                                Math.Sqrt(Math.Abs(
                                                    radiusSquared *
                                                    (dx * dx + dySquared) -
                                                    dxFocusYIncrement * dxFocusYIncrement))
                                                    ) * preComputeMultiplyIncludeLookup)
                                            );
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        //if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        #region gamma apply
                                        dst = BufferData[startXPosition];
                                        dstG = (dst >> 8) & 0xFF;
                                        dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                        BufferData[startXPosition] =
                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (((uint)(uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                            | ((uint)(uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                            | ((uint)gammaLutBlue[(dstRB & 0x00FF)]))
                                            ;
                                        #endregion
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        #region cumpute value for row
                        //dyFocusY = (startRowIndex - centerY) * (startRowIndex - centerY);
                        dy = (startRowIndex - absoluteFocusY);
                        dySquared = dy * dy;
                        dyFocusX = dy * focusX;
                        dyFocusY = dy * focusY;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region even odd change
                                        scLastCoverage &= 511;
                                        if (scLastCoverage >= 256)
                                        {
                                            scLastCoverage = 512 - scLastCoverage - 1;
                                        }
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            #region prepare for row color index calculation
                                            // get current color index value
                                            dx = (scLastX + 1 - absoluteFocusX);
                                            dxFocusYIncrement = (dx * focusY - dyFocusX);
                                            #endregion


                                            while (startXPosition < lastXPosition)
                                            {
                                                #region calculate color index
                                                currentColorIndexValue =
                                                    (int)
                                                    ((((dx * focusX) + dyFocusY +
                                                        Math.Sqrt(Math.Abs(
                                                            radiusSquared *
                                                            (dx * dx + dySquared) -
                                                            dxFocusYIncrement * dxFocusYIncrement))
                                                            ) * preComputeMultiplyIncludeLookup)
                                                    );

                                                // change for color index calculation
                                                dx++;
                                                dxFocusYIncrement += focusY;
                                                #endregion

                                                colorData = builtColors[currentColorIndexValue < 0 ? 0 : currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                calculatedCoverage = (byte)((colorData >> 24));
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                if (calculatedCoverage >= 254)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    #region gamma apply
                                                    dst = BufferData[startXPosition];
                                                    dstG = (dst >> 8) & 0xFF;
                                                    dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                    BufferData[startXPosition] =
                                                        (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                        | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                        | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                        ;
                                                    #endregion
                                                }
                                                startXPosition++;
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region even odd change
                                        tempCover &= 511;
                                        if (tempCover >= 256)
                                        {
                                            tempCover = 512 - tempCover - 1;
                                        }
                                        #endregion
                                        // get current color data
                                        #region prepare for row color index calculation
                                        // get current color index value
                                        dx = (currentCellData.X - absoluteFocusX);
                                        dxFocusYIncrement = (dx * focusY - dyFocusX);
                                        #endregion

                                        #region calculate color index
                                        currentColorIndexValue =
                                            (int)
                                            ((((dx * focusX) + dyFocusY +
                                                Math.Sqrt(Math.Abs(
                                                    radiusSquared *
                                                    (dx * dx + dySquared) -
                                                    dxFocusYIncrement * dxFocusYIncrement))
                                                    ) * preComputeMultiplyIncludeLookup)
                                            );
                                        #endregion
                                        colorData = builtColors[currentColorIndexValue < 0 ? 0 : currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                        calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        //if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        #region gamma apply
                                        dst = BufferData[startXPosition];
                                        dstG = (dst >> 8) & 0xFF;
                                        dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                        BufferData[startXPosition] =
                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                            | (gammaLutBlue[(dstRB & 0x00FF)]))
                                            ;
                                        #endregion
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
            }

            #endregion
        }
        #endregion

        #region Fill ellipse, Focus
        /// <summary>
        /// Filling using radial gradient for circle gradient only
        /// </summary>
        /// <param name="radial">radial</param>
        /// <param name="rows">rows</param>
        /// <param name="startRowIndex">start y index</param>
        /// <param name="endRowIndex">end y index</param>
        /// <param name="gammaLutRed">gamma look up table for red</param>
        /// <param name="gammaLutGreen">gamma look up table for green</param>
        /// <param name="gammaLutBlue">gamma look up table for blue</param>
        void FillingEllipseFocalEvenOdd(RadialGradient radial, uint opacity, RowData[] rows, int startRowIndex, int endRowIndex, byte[] gammaLutRed, byte[] gammaLutGreen, byte[] gammaLutBlue)
        {
            // now not need to check null or not
            uint[] builtColors = radial.GetLinearColors(opacity);
            #region private variable for filling
            int currentCoverage, scLastCoverage, scLastX = 0;
            int tempCover = 0;
            int currentArea = 0;
            int lastXPosition = 0;
            int startXPosition = 0;
            byte calculatedCoverage = 0;


            double centerX = radial.CenterX;
            double centerY = radial.CenterY;
            // in this case radius x = radius y
            double radius = radial.RadiusX;
            double radiusYForX = radial.RadiusY / radial.RadiusX;


            // this is precompute value so that (* ColorIndexScale / radius) now just ( * preComputeRadiusLookup )
            double preComputeRadiusLookup = ColorIndexScale / radius;

            CellData currentCellData = null;
            uint colorData = 0;

            double dx = 0, dy = 0;

            double dySquared = 0; // saving dy * dy
            // focus is changed to relative from the center
            double absoluteFocusX = radial.FocusX;
            double absoluteFocusY = radial.FocusY;

            double focusX = radial.FocusX - centerX;
            double focusY = radial.FocusY - centerY;
            focusY = focusY / radiusYForX;

            // note that dx,dy need to move center
            /*
             *  dx = (currentXPosition - absoluteFocusX);
             *  dy = (startRowIndex - absoluteFocusY);
             *  currentColorIndexValue =
                    (int)
                    (
                        (
                            (
                            (dx * focusX) + (dy * focusY)
                            + Math.Sqrt
                            (
                                Math.Abs
                                (
                                    radius * radius * (dx * dx + dy * dy) - (dx * focusY - dy * focusX) * (dx * focusY - dy * focusX)      
                                )
                            )
                        ) * (radius /
                        ((radius * radius) - ((focusX * focusX )+ (focusY * focusY))))
                    ) * 256 /radius
                );
             */

            //note that  ( radius / (( radius * radius) - ((focusX * focusX) + (focusY * focusY))) is const
            // so that need to pre compute
            double preComputeMultiply = radius / ((radius * radius) - ((focusX * focusX) + (focusY * focusY)));

            #region modify when pre compute for multiply is zero
            if (preComputeMultiply == 0)
            {
                if (focusX != 0)
                {
                    if (focusX < 0)
                    {
                        focusX += GradientAdjustment;
                    }
                    else
                    {
                        focusX -= GradientAdjustment;
                    }
                }
                if (focusY != 0)
                {
                    if (focusY < 0)
                    {
                        focusY += GradientAdjustment;
                    }
                    else
                    {
                        focusY -= GradientAdjustment;
                    }
                }
                preComputeMultiply = radius / ((radius * radius) - ((focusX * focusX) + (focusY * focusY)));
            }
            #endregion

            double preComputeMultiplyIncludeLookup = preComputeRadiusLookup * preComputeMultiply;

            // saving dy * focusY
            double dyFocusY = 0;
            double dyFocusX = 0;
            double dxFocusYIncrement = 0; // saving dx * focusY - dyFocusX
            double radiusSquared = radius * radius;


            int currentColorIndexValue = 0;
            //int currentXPosition = 0;
            uint dst, dstRB, dstG;
            #endregion

            #region FILLING
            if (radial.Ramp.NoBlendingColor)
            {
                // when no need to blending, when draw a horizontal line
                // do not need check the back color, alway setup
                if (radial.Style != GradientStyle.Pad)
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;

                        #region cumpute value for row
                        //dyFocusY = (startRowIndex - centerY) * (startRowIndex - centerY);
                        dy = ((startRowIndex - centerY) / radiusYForX) - focusY;
                        dySquared = dy * dy;
                        dyFocusX = dy * focusX;
                        dyFocusY = dy * focusY;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region even odd change
                                        scLastCoverage &= 511;
                                        if (scLastCoverage >= 256)
                                        {
                                            scLastCoverage = 512 - scLastCoverage - 1;
                                        }
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;

                                            #region prepare for row color index calculation
                                            // get current color index value
                                            //currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                            //currentXPosition = scLastX + 1;
                                            dx = (scLastX + 1 - absoluteFocusX);
                                            dxFocusYIncrement = (dx * focusY - dyFocusX);
                                            #endregion
                                            if (scLastCoverage >= 255)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)
                                                        ((((dx * focusX) + dyFocusY +
                                                            Math.Sqrt(Math.Abs(
                                                                radiusSquared *
                                                                (dx * dx + dySquared) -
                                                                dxFocusYIncrement * dxFocusYIncrement))
                                                                ) * preComputeMultiplyIncludeLookup)
                                                        );

                                                    //currentColorIndexValue =
                                                    //    (int)
                                                    //    (
                                                    //        (
                                                    //            (
                                                    //            (dx * focusX) + (dy * focusY)
                                                    //            + Math.Sqrt
                                                    //            (
                                                    //                Math.Abs
                                                    //                (
                                                    //                    radius * radius 
                                                    //                    * (dx * dx + dy * dy) 
                                                    //                    - (dx * focusY - dy * focusX) 
                                                    //                    * (dx * focusY - dy * focusX)
                                                    //                )
                                                    //            )
                                                    //        ) * (radius /
                                                    //        ((radius * radius) - ((focusX * focusX) + (focusY * focusY))))
                                                    //        ) * 256 / radius
                                                    //    );

                                                    // change for color index calculation
                                                    dx++;
                                                    dxFocusYIncrement += focusY;
                                                    #endregion
                                                    BufferData[startXPosition] = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                    startXPosition++;
                                                }
                                            }
                                            else
                                            {
                                                calculatedCoverage = (byte)scLastCoverage;
                                                while (startXPosition < lastXPosition)
                                                {

                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)
                                                        ((((dx * focusX) + dyFocusY +
                                                            Math.Sqrt(Math.Abs(
                                                                radiusSquared *
                                                                (dx * dx + dySquared) -
                                                                dxFocusYIncrement * dxFocusYIncrement))
                                                                ) * preComputeMultiplyIncludeLookup)
                                                        );

                                                    // change for color index calculation
                                                    dx++;
                                                    dxFocusYIncrement += focusY;
                                                    #endregion

                                                    colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                    //calculatedCoverage = (byte)((colorData >> 24));
                                                    //calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                    if (calculatedCoverage >= 254)
                                                    {
                                                        BufferData[startXPosition] = colorData;
                                                    }
                                                    else
                                                    {
                                                        #region gamma apply
                                                        dst = BufferData[startXPosition];
                                                        dstG = (dst >> 8) & 0xFF;
                                                        dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                        BufferData[startXPosition] =
                                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                            | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                            | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                            ;
                                                        #endregion
                                                    }
                                                    startXPosition++;

                                                }
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region even odd change
                                        tempCover &= 511;
                                        if (tempCover >= 256)
                                        {
                                            tempCover = 512 - tempCover - 1;
                                        }
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        //currentXPosition = currentCellData.X;
                                        //currentColorIndexValue =
                                        //    (int)(Math.Sqrt(dyFocusY +
                                        //        (currentXPosition - centerX) * (currentXPosition - centerX)) * preComputeRadiusLookup);
                                        #region prepare for row color index calculation
                                        // get current color index value
                                        dx = (currentCellData.X - absoluteFocusX);
                                        dxFocusYIncrement = (dx * focusY - dyFocusX);
                                        #endregion

                                        #region calculate color index
                                        currentColorIndexValue =
                                            (int)
                                            ((((dx * focusX) + dyFocusY +
                                                Math.Sqrt(Math.Abs(
                                                    radiusSquared *
                                                    (dx * dx + dySquared) -
                                                    dxFocusYIncrement * dxFocusYIncrement))
                                                    ) * preComputeMultiplyIncludeLookup)
                                            );
                                        #endregion
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        //calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        //tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        //dst = BufferData[startXPosition];
                                        //dstRB = dst & 0x00FF00FF;
                                        //dstG = (dst >> 8) & 0xFF;
                                        //BufferData[startXPosition] =
                                        //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                        if (calculatedCoverage >= 254)
                                        {
                                            BufferData[startXPosition] = colorData;
                                        }
                                        else
                                        {
                                            #region gamma apply
                                            dst = BufferData[startXPosition];
                                            dstG = (dst >> 8) & 0xFF;
                                            dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                            BufferData[startXPosition] =
                                                (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                ;
                                            #endregion
                                        }
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        #region cumpute value for row
                        //dyFocusY = (startRowIndex - centerY) * (startRowIndex - centerY);
                        dy = ((startRowIndex - centerY) / radiusYForX) - focusY;
                        dySquared = dy * dy;
                        dyFocusX = dy * focusX;
                        dyFocusY = dy * focusY;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region even odd change
                                        scLastCoverage &= 511;
                                        if (scLastCoverage >= 256)
                                        {
                                            scLastCoverage = 512 - scLastCoverage - 1;
                                        }
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            #region prepare for row color index calculation
                                            // get current color index value
                                            dx = (scLastX + 1 - absoluteFocusX);
                                            dxFocusYIncrement = (dx * focusY - dyFocusX);
                                            #endregion

                                            if (scLastCoverage >= 255)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)
                                                        ((((dx * focusX) + dyFocusY +
                                                            Math.Sqrt(Math.Abs(
                                                                radiusSquared *
                                                                (dx * dx + dySquared) -
                                                                dxFocusYIncrement * dxFocusYIncrement))
                                                                ) * preComputeMultiplyIncludeLookup)
                                                        );

                                                    // change for color index calculation
                                                    dx++;
                                                    dxFocusYIncrement += focusY;
                                                    #endregion

                                                    BufferData[startXPosition] = builtColors[currentColorIndexValue < 0 ? 0 : currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                    startXPosition++;
                                                }
                                            }
                                            else
                                            {
                                                calculatedCoverage = (byte)(scLastCoverage);
                                                while (startXPosition < lastXPosition)
                                                {
                                                    #region calculate color index
                                                    currentColorIndexValue =
                                                        (int)
                                                        ((((dx * focusX) + dyFocusY +
                                                            Math.Sqrt(Math.Abs(
                                                                radiusSquared *
                                                                (dx * dx + dySquared) -
                                                                dxFocusYIncrement * dxFocusYIncrement))
                                                                ) * preComputeMultiplyIncludeLookup)
                                                        );

                                                    // change for color index calculation
                                                    dx++;
                                                    dxFocusYIncrement += focusY;
                                                    #endregion
                                                    colorData = builtColors[currentColorIndexValue < 0 ? 0 : currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                    //calculatedCoverage = (byte)((colorData >> 24));
                                                    //calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                    if (calculatedCoverage >= 254)
                                                    {
                                                        BufferData[startXPosition] = colorData;
                                                    }
                                                    else
                                                    {
                                                        #region gamma apply
                                                        dst = BufferData[startXPosition];
                                                        dstG = (dst >> 8) & 0xFF;
                                                        dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                        BufferData[startXPosition] =
                                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                            | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                            | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                            ;
                                                        #endregion
                                                    }
                                                    startXPosition++;
                                                }
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region even odd change
                                        tempCover &= 511;
                                        if (tempCover >= 256)
                                        {
                                            tempCover = 512 - tempCover - 1;
                                        }
                                        #endregion
                                        // get current color data
                                        #region calculate color index
                                        #region prepare for row color index calculation
                                        // get current color index value
                                        dx = (currentCellData.X - absoluteFocusX);
                                        dxFocusYIncrement = (dx * focusY - dyFocusX);
                                        #endregion

                                        #region calculate color index
                                        currentColorIndexValue =
                                            (int)
                                            ((((dx * focusX) + dyFocusY +
                                                Math.Sqrt(Math.Abs(
                                                    radiusSquared *
                                                    (dx * dx + dySquared) -
                                                    dxFocusYIncrement * dxFocusYIncrement))
                                                    ) * preComputeMultiplyIncludeLookup)
                                            );
                                        #endregion
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue < 0 ? 0 : currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                        //calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        //tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region blend here
                                        //dst = BufferData[startXPosition];
                                        //dstRB = dst & 0x00FF00FF;
                                        //dstG = (dst >> 8) & 0xFF;
                                        //BufferData[startXPosition] =
                                        //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                        if (calculatedCoverage >= 254)
                                        {
                                            BufferData[startXPosition] = colorData;
                                        }
                                        else
                                        {
                                            #region gamma apply
                                            dst = BufferData[startXPosition];
                                            dstG = (dst >> 8) & 0xFF;
                                            dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                            BufferData[startXPosition] =
                                                (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                ;
                                            #endregion
                                        }
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
            }
            else
            {
                // when no need to blending, when draw a horizontal line
                // do not need check the back color, alway setup
                if (radial.Style != GradientStyle.Pad)
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        #region cumpute value for row
                        //dyFocusY = (startRowIndex - centerY) * (startRowIndex - centerY);
                        dy = ((startRowIndex - centerY) / radiusYForX) - focusY;
                        dySquared = dy * dy;
                        dyFocusX = dy * focusX;
                        dyFocusY = dy * focusY;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region even odd change
                                        scLastCoverage &= 511;
                                        if (scLastCoverage >= 256)
                                        {
                                            scLastCoverage = 512 - scLastCoverage - 1;
                                        }
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            #region prepare for row color index calculation
                                            // get current color index value
                                            dx = (scLastX + 1 - absoluteFocusX);
                                            dxFocusYIncrement = (dx * focusY - dyFocusX);
                                            #endregion


                                            while (startXPosition < lastXPosition)
                                            {
                                                #region calculate color index
                                                currentColorIndexValue =
                                                    (int)
                                                    ((((dx * focusX) + dyFocusY +
                                                        Math.Sqrt(Math.Abs(
                                                            radiusSquared *
                                                            (dx * dx + dySquared) -
                                                            dxFocusYIncrement * dxFocusYIncrement))
                                                            ) * preComputeMultiplyIncludeLookup)
                                                    );

                                                // change for color index calculation
                                                dx++;
                                                dxFocusYIncrement += focusY;
                                                #endregion
                                                colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];
                                                calculatedCoverage = (byte)((colorData >> 24));
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                if (calculatedCoverage >= 254)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    #region gamma apply
                                                    dst = BufferData[startXPosition];
                                                    dstG = (dst >> 8) & 0xFF;
                                                    dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                    BufferData[startXPosition] =
                                                        (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                        | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                        | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                        ;
                                                    #endregion
                                                }
                                                startXPosition++;
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region even odd change
                                        tempCover &= 511;
                                        if (tempCover >= 256)
                                        {
                                            tempCover = 512 - tempCover - 1;
                                        }
                                        #endregion
                                        // get current color data
                                        #region prepare for row color index calculation
                                        // get current color index value
                                        dx = (currentCellData.X - absoluteFocusX);
                                        dxFocusYIncrement = (dx * focusY - dyFocusX);
                                        #endregion

                                        #region calculate color index
                                        currentColorIndexValue =
                                            (int)
                                            ((((dx * focusX) + dyFocusY +
                                                Math.Sqrt(Math.Abs(
                                                    radiusSquared *
                                                    (dx * dx + dySquared) -
                                                    dxFocusYIncrement * dxFocusYIncrement))
                                                    ) * preComputeMultiplyIncludeLookup)
                                            );
                                        #endregion

                                        colorData = builtColors[currentColorIndexValue & ColorIndexDoubleMask];//fixedColor[currentCellData.X - CurrentStartXIndex];
                                        calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        //if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region gamma apply
                                        dst = BufferData[startXPosition];
                                        dstG = (dst >> 8) & 0xFF;
                                        dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                        BufferData[startXPosition] =
                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                            | (gammaLutBlue[(dstRB & 0x00FF)]))
                                            ;
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;
                        #region cumpute value for row
                        //dyFocusY = (startRowIndex - centerY) * (startRowIndex - centerY);
                        dy = ((startRowIndex - centerY) / radiusYForX) - focusY;
                        dySquared = dy * dy;
                        dyFocusX = dy * focusX;
                        dyFocusY = dy * focusY;
                        #endregion
                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1) && (scLastCoverage != 0))
                                    {
                                        // fast bit absolute
                                        scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                        #region even odd change
                                        scLastCoverage &= 511;
                                        if (scLastCoverage >= 256)
                                        {
                                            scLastCoverage = 512 - scLastCoverage - 1;
                                        }
                                        #endregion
                                        if (scLastCoverage != 0)
                                        {
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            #region prepare for row color index calculation
                                            // get current color index value
                                            dx = (scLastX + 1 - absoluteFocusX);
                                            dxFocusYIncrement = (dx * focusY - dyFocusX);
                                            #endregion


                                            while (startXPosition < lastXPosition)
                                            {
                                                #region calculate color index
                                                currentColorIndexValue =
                                                    (int)
                                                    ((((dx * focusX) + dyFocusY +
                                                        Math.Sqrt(Math.Abs(
                                                            radiusSquared *
                                                            (dx * dx + dySquared) -
                                                            dxFocusYIncrement * dxFocusYIncrement))
                                                            ) * preComputeMultiplyIncludeLookup)
                                                    );

                                                // change for color index calculation
                                                dx++;
                                                dxFocusYIncrement += focusY;
                                                #endregion

                                                colorData = builtColors[currentColorIndexValue < 0 ? 0 : currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                                calculatedCoverage = (byte)((colorData >> 24));
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                if (calculatedCoverage >= 254)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    #region gamma apply
                                                    dst = BufferData[startXPosition];
                                                    dstG = (dst >> 8) & 0xFF;
                                                    dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                    BufferData[startXPosition] =
                                                        (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                        | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                        | (gammaLutBlue[(dstRB & 0x00FF)]))
                                                        ;
                                                    #endregion
                                                }
                                                startXPosition++;
                                            }
                                            #endregion
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    // fast absolute
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region even odd change
                                        tempCover &= 511;
                                        if (tempCover >= 256)
                                        {
                                            tempCover = 512 - tempCover - 1;
                                        }
                                        #endregion
                                        // get current color data
                                        #region prepare for row color index calculation
                                        // get current color index value
                                        dx = (currentCellData.X - absoluteFocusX);
                                        dxFocusYIncrement = (dx * focusY - dyFocusX);
                                        #endregion

                                        #region calculate color index
                                        currentColorIndexValue =
                                            (int)
                                            ((((dx * focusX) + dyFocusY +
                                                Math.Sqrt(Math.Abs(
                                                    radiusSquared *
                                                    (dx * dx + dySquared) -
                                                    dxFocusYIncrement * dxFocusYIncrement))
                                                    ) * preComputeMultiplyIncludeLookup)
                                            );
                                        #endregion
                                        colorData = builtColors[currentColorIndexValue < 0 ? 0 : currentColorIndexValue > 254 ? 255 : currentColorIndexValue];
                                        calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        //if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        #region gamma apply
                                        dst = BufferData[startXPosition];
                                        dstG = (dst >> 8) & 0xFF;
                                        dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                        BufferData[startXPosition] =
                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                            | (gammaLutBlue[(dstRB & 0x00FF)]))
                                            ;
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                    }
                    #endregion
                }
            }

            #endregion
        }
        #endregion
        #endregion

        #endregion

    }
}
