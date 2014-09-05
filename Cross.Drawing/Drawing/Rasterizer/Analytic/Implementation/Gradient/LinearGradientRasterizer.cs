using System;

namespace Cross.Drawing.Rasterizers.Analytical
{
    /// <summary>
    /// Linear gradient rasterizer
    /// </summary>
    /// <remarks>
    /// Note for gradient including gamma, and colors in color ramp have 
    /// alpha is 255, mean NoBlending is true.
    /// Must check the calculatedCoverage before blending, because gamma may change color
    /// </remarks>
    public /*internal*/ class LinearGradientRasterizer : GradientRasterizer
    {

        #region Constructors
        /// <summary>
        /// Default constructor for LinearGradientRasterizer
        /// </summary>
        public LinearGradientRasterizer()
        { }
        #endregion


        #region FILL (!!!TRANSFORM)

        #region NON-ZERO (!gamma)

        #region On Filling NonZero (!transform, !gamma)
        /// <summary>
        /// Fill to buffer base rows data information using non-zero rule
        /// </summary>
        /// <param name="paint">paint using for fill</param>
        /// <param name="rows">row data information</param>
        /// <param name="startRowIndex">start row index in row array need to draw</param>
        /// <param name="endRowIndex">end row index in end row array need to draw</param>
        protected override void OnFillingNonZero(
            PaintMaterial paint,
            RowData[] rows,
            int startRowIndex,
            int endRowIndex)
        {
            if (!(paint.Paint is LinearGradient))
            {
                NotMatchPaintTypeException.Publish(typeof(LinearGradient), paint.Paint.GetType());
                return;
            }
            LinearGradient linearGradient = paint.Paint as LinearGradient;

            switch (linearGradient.Mode)
            {
                case LinearGradientMode.Horizontal:
                    OnFillingHorizontalNonZero(linearGradient, paint.ScaledOpacity, rows, startRowIndex, endRowIndex);
                    break;
                case LinearGradientMode.Vertical:
                    OnFillingVerticalNonZero(linearGradient, paint.ScaledOpacity, rows, startRowIndex, endRowIndex);
                    break;
                case LinearGradientMode.ForwardDiagonal:
                    OnFillingDiagonalNonZero(linearGradient, paint.ScaledOpacity, rows, startRowIndex, endRowIndex, true);
                    break;
                case LinearGradientMode.BackwardDiagonal:
                    OnFillingDiagonalNonZero(linearGradient, paint.ScaledOpacity, rows, startRowIndex, endRowIndex, false);
                    break;
            }
        }
        #endregion

        #region On Filling Horizontal NonZero (!transform, !gamma)
        /// <summary>
        /// Fill to buffer base rows data information using non-zero rule
        /// </summary>
        /// <param name="paint">linear gradient object</param>
        /// <param name="rows">row data information</param>
        /// <param name="startRowIndex">start row index in row array need to draw</param>
        /// <param name="endRowIndex">end row index in end row array need to draw</param>
        void OnFillingHorizontalNonZero(
            LinearGradient paint,
            uint opacity,
            RowData[] rows,
            int startRowIndex,
            int endRowIndex)
        {
            // now not need to check null or not
            uint[] builtColors = paint.GetLinearColors(opacity);

            /*  Base on startX, endX, we need build fixedColor array
             *  contain width-count elements. So that, at a column,
             *  we can lookup color for that column.    */

            #region Build fixed color
            double startX = paint.StartX;
            double endX = paint.EndX;

            // width of this
            int width = CurrentEndXIndex - CurrentStartXIndex + 1;
            uint[] fixedColor = new uint[width];
            int distanceScaled = (int)(Math.Abs(startX - endX) * DistanceScale);
            if (distanceScaled == 0)
            {
                FillingException.Publish(typeof(LinearGradient), "Start point and end point are too close");
                return;
            }
            #region build fixed-color array
            if (paint.Style == GradientStyle.Pad)
            {
                #region GradientStyle.Pad
                int startXScaled = (int)(startX * DistanceScale);
                int startFixedIndex = (((
                        (((width + CurrentStartXIndex) << DistanceShift) - startXScaled)
                        << ColorIndexShift) / distanceScaled)) << IncrementColorIndexShift;
                int colorIncrement = (DistanceScale * ColorIndexIncludeIncrementScale) / distanceScaled;
                if (endX < startX)
                {
                    colorIncrement = -colorIncrement;
                    startFixedIndex = -startFixedIndex;
                }
                while (width-- > 0)
                {
                    fixedColor[width] = builtColors[
                        startFixedIndex < 0 ?
                            0 :
                        (startFixedIndex > ColorIndexIncludeIncrementScale ?
                            255 :
                            (startFixedIndex >> IncrementColorIndexShift))];
                    startFixedIndex -= colorIncrement;
                }
                #endregion
            }
            else
            {
                #region GradientStyle.Repeat || GradientStyle.Reflect
                int startXScaled = (int)(startX * DistanceScale);
                int startFixedIndex = (((
                    (((width + CurrentStartXIndex) << DistanceShift) - startXScaled)
                    << ColorIndexShift) / distanceScaled)) << IncrementColorIndexShift;
                int colorIncrement = (DistanceScale * ColorIndexIncludeIncrementScale) / distanceScaled;
                if (endX < startX)
                {
                    colorIncrement = -colorIncrement;
                }
                startFixedIndex &= ColorIndexIncludeIncrementDoubleMask;
                while (width-- > 0)
                {
                    fixedColor[width] = builtColors[
                        startFixedIndex < 0 ?
                            (startFixedIndex >> IncrementColorIndexShift) + 512 :
                            (startFixedIndex >> IncrementColorIndexShift)];
                    startFixedIndex -= colorIncrement;
                    startFixedIndex &= ColorIndexIncludeIncrementDoubleMask;
                }
                #endregion
            }
            #endregion

            #endregion

            #region private variable for filling
            int currentCoverage, scLastCoverage, scLastX = 0;
            int tempCover = 0;
            int currentArea = 0;
            int lastXPosition = 0;
            int startXPosition = 0;
            byte calculatedCoverage = 0;
            int currentColorIndexValue = 0;
            CellData currentCellData = null;
            uint colorData = 0;
            uint dst, dstRB, dstG;
            #endregion

            #region FILLING
            if (paint.Ramp.NoBlendingColor)
            {
                #region filling without blend for horizontal lines
                // when no need to blending, when draw a horizontal line
                // do not need check the back color, alway setup
                startRowIndex--;
                while (++startRowIndex <= endRowIndex)
                {
                    currentCoverage = scLastCoverage = scLastX = 0;

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

                                    #region non-zero checking code
                                    if (scLastCoverage > 255) scLastCoverage = 255;
                                    #endregion

                                    if (scLastCoverage != 0)
                                    {
                                        #region BLEND HORIZONTAL LINE
                                        // calculate start and end position
                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                        lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        // get current color index value
                                        currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                        if (scLastCoverage >= 255)
                                        {
                                            while (startXPosition < lastXPosition)
                                            {
                                                BufferData[startXPosition++] = fixedColor[currentColorIndexValue++];
                                            }
                                        }
                                        else
                                        {
                                            while (startXPosition < lastXPosition)
                                            {
                                                colorData = fixedColor[currentColorIndexValue];
                                                calculatedCoverage = (byte)((colorData >> 24));
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);

                                                if (calculatedCoverage >= 255)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    #region blend here
                                                    dst = BufferData[startXPosition];
                                                    dstRB = dst & 0x00FF00FF;
                                                    dstG = (dst >> 8) & 0xFF;

                                                    BufferData[startXPosition] =
                                                        (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                        | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                    #endregion
                                                }
                                                startXPosition++;
                                                currentColorIndexValue++;
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

                                    #region non-zero checking code
                                    if (tempCover > 255) tempCover = 255;
                                    #endregion

                                    // get current color data
                                    colorData = fixedColor[currentCellData.X - CurrentStartXIndex];
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
            }//paint.Ramp.NoBlendingColor
            else
            {//has blending color
                #region perform normal filling
                startRowIndex--;
                while (++startRowIndex <= endRowIndex)
                {
                    currentCoverage = scLastCoverage = scLastX = 0;

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

                                    #region non-zero checking code
                                    if (scLastCoverage > 255) scLastCoverage = 255;
                                    #endregion

                                    if (scLastCoverage != 0)
                                    {
                                        #region BLEND HORIZONTAL LINE
                                        // calculate start and end position
                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                        lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        // get current color index value
                                        currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                        while (startXPosition < lastXPosition)
                                        {
                                            colorData = fixedColor[currentColorIndexValue];
                                            calculatedCoverage = (byte)(colorData >> 24);
                                            calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);

                                            if (calculatedCoverage >= 255)
                                            {
                                                BufferData[startXPosition] = colorData;
                                            }
                                            else
                                            {
                                                #region blend here
                                                dst = BufferData[startXPosition];
                                                dstRB = dst & 0x00FF00FF;
                                                dstG = (dst >> 8) & 0xFF;

                                                BufferData[startXPosition] =
                                                    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                #endregion
                                            }
                                            startXPosition++;
                                            currentColorIndexValue++;
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

                                    #region non-zero checking code
                                    if (tempCover > 255) tempCover = 255;
                                    #endregion

                                    // get current color data
                                    colorData = fixedColor[currentCellData.X - CurrentStartXIndex];
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
            }//has blending color
            #endregion
        }
        #endregion

        #region On Filling Vertical NonZero (!transform, !gamma)
        /// <summary>
        /// Fill to buffer base rows data information using non-zero rule
        /// </summary>
        /// <param name="paint">linear gradient object</param>
        /// <param name="rows">row data information</param>
        /// <param name="startRowIndex">start row index in row array need to draw</param>
        /// <param name="endRowIndex">end row index in end row array need to draw</param>
        void OnFillingVerticalNonZero(
            LinearGradient paint,
            uint opacity,
            RowData[] rows,
            int startRowIndex,
            int endRowIndex)
        {
            // now not need to check null or not
            uint[] builtColors = paint.GetLinearColors(opacity);

            /*  Base on startX,endX, we need build fixedColor array
             *  contain width count elements. So that, at a column, we
             *  can lookup color for that column. */

            #region Build fixed color
            double startY = paint.StartY;
            double endY = paint.EndY;

            // width of this
            int height = endRowIndex - startRowIndex + 1;
            uint[] fixedColor = new uint[height];
            int distanceScaled = (int)(Math.Abs(startY - endY) * DistanceScale);
            if (distanceScaled == 0)
            {
                FillingException.Publish(typeof(LinearGradient), "Start point and end point are too close");
                return;
            }

            #region building fixed color array
            if (paint.Style == GradientStyle.Pad)
            {
                #region GradientStyle.Pad
                int startFixedIndex = (((
                    (((height + startRowIndex) << DistanceShift) - (int)(startY * DistanceScale))
                            << ColorIndexShift) / distanceScaled)) << IncrementColorIndexShift;
                int colorIncrement = (DistanceScale * ColorIndexIncludeIncrementScale) / distanceScaled;
                if (endY < startY)
                {
                    colorIncrement = -colorIncrement;
                    startFixedIndex = -startFixedIndex;
                }
                while (height-- > 0)
                {
                    fixedColor[height] =
                        builtColors[startFixedIndex < 0 ?
                            0 :
                        (startFixedIndex > ColorIndexIncludeIncrementScale ?
                            255 :
                            (startFixedIndex >> IncrementColorIndexShift))];
                    startFixedIndex -= colorIncrement;
                }
                #endregion
            }
            else
            {
                #region GradientStyle.Repeat || GradientStyle.Reflect
                int startFixedIndex = (((
                        (((height + startRowIndex) << DistanceShift) - (int)(startY * DistanceScale))
                        << ColorIndexShift) / distanceScaled)) << IncrementColorIndexShift;
                int colorIncrement = (DistanceScale * ColorIndexIncludeIncrementScale) / distanceScaled;
                if (endY < startY)
                {
                    colorIncrement = -colorIncrement;
                }
                startFixedIndex &= ColorIndexIncludeIncrementDoubleMask;
                while (height-- > 0)
                {
                    fixedColor[height] = builtColors[
                        startFixedIndex < 0 ?
                            (startFixedIndex >> IncrementColorIndexShift) + 512 :
                            (startFixedIndex >> IncrementColorIndexShift)];
                    startFixedIndex -= colorIncrement;
                    startFixedIndex &= ColorIndexIncludeIncrementDoubleMask;
                }
                #endregion
            }
            #endregion

            #endregion

            #region private variable for filling
            int currentCoverage, scLastCoverage, scLastX = 0;
            int tempCover = 0;
            int currentArea = 0;
            int lastXPosition = 0;
            int startXPosition = 0;
            byte calculatedCoverage = 0;
            int currentColorIndexValue = 0;
            CellData currentCellData = null;
            uint colorData = 0;
            uint colorAlpha = 0;
            uint colorG = 0;
            uint colorRB = 0;
            uint dst, dstRB, dstG;
            #endregion

            #region FILLING
            if (paint.Ramp.NoBlendingColor)
            {
                #region filling without blend for horizontal lines
                // when no need to blending, when draw a horizontal line
                // do not need check the back color, alway setup
                startRowIndex--;
                while (++startRowIndex <= endRowIndex)
                {
                    currentCoverage = scLastCoverage = scLastX = 0;

                    if (rows[startRowIndex] != null)
                    {
                        #region calculate and get current color
                        colorData = fixedColor[currentColorIndexValue];
                        colorAlpha = (colorData >> 24);
                        colorG = (colorData & 0x0000FF00) >> 8;
                        colorRB = (colorData & 0x00FF00FF);
                        #endregion
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

                                    #region non-zero checking code
                                    if (scLastCoverage > 255) scLastCoverage = 255;
                                    #endregion

                                    if (scLastCoverage != 0)
                                    {
                                        #region BLEND HORIZONTAL LINE
                                        // calculate start and end position
                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                        lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        // get current color index value
                                        if (scLastCoverage >= 254)
                                        {
                                            while (startXPosition < lastXPosition)
                                            {
                                                BufferData[startXPosition++] = colorData;
                                            }
                                        }
                                        else
                                        {
                                            while (startXPosition < lastXPosition)
                                            {
                                                calculatedCoverage = (byte)((scLastCoverage * colorAlpha) >> 8);
                                                if (calculatedCoverage >= 254)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    #region blend here
                                                    dst = BufferData[startXPosition];
                                                    dstRB = dst & 0x00FF00FF;
                                                    dstG = (dst >> 8) & 0xFF;

                                                    BufferData[startXPosition] =
                                                        (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (uint)((((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                        | (uint)(((((colorRB - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
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

                                    #region non-zero checking code
                                    if (tempCover > 255) tempCover = 255;
                                    #endregion

                                    // get current color data
                                    #region blend pixel
                                    tempCover = (int)((tempCover * colorAlpha) >> 8);
                                    //if (tempCover > 255) tempCover = 255;
                                    calculatedCoverage = (byte)tempCover;

                                    startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;

                                    #region blend here
                                    dst = BufferData[startXPosition];
                                    dstRB = dst & 0x00FF00FF;
                                    dstG = (dst >> 8) & 0xFF;

                                    BufferData[startXPosition] =
                                        (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        | (uint)((((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        | (uint)(((((colorRB - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
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
                    // increase color index
                    currentColorIndexValue++;
                }
                #endregion
            }//paint.Ramp.NoBlendingColor
            else
            {
                #region perform normal filling
                startRowIndex--;
                while (++startRowIndex <= endRowIndex)
                {
                    currentCoverage = scLastCoverage = scLastX = 0;

                    if (rows[startRowIndex] != null)
                    {
                        #region calculate and get current color
                        colorData = fixedColor[currentColorIndexValue];
                        colorAlpha = (colorData >> 24);
                        colorG = (colorData & 0x0000FF00) >> 8;
                        colorRB = (colorData & 0x00FF00FF);
                        #endregion

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

                                    #region non-zero checking code
                                    if (scLastCoverage > 255) scLastCoverage = 255;
                                    #endregion

                                    if (scLastCoverage != 0)
                                    {
                                        #region BLEND HORIZONTAL LINE
                                        // calculate start and end position
                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                        lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;

                                        while (startXPosition < lastXPosition)
                                        {
                                            calculatedCoverage = (byte)((scLastCoverage * colorAlpha) >> 8);
                                            if (calculatedCoverage >= 254)
                                            {
                                                BufferData[startXPosition] = colorData;
                                            }
                                            else
                                            {
                                                #region blend here
                                                dst = BufferData[startXPosition];
                                                dstRB = dst & 0x00FF00FF;
                                                dstG = (dst >> 8) & 0xFF;

                                                BufferData[startXPosition] =
                                                    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                    | (uint)((((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                    | (uint)(((((colorRB - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
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

                                    #region non-zero checking code
                                    if (tempCover > 255) tempCover = 255;
                                    #endregion

                                    #region blend pixel
                                    tempCover = (int)((tempCover * colorAlpha) >> 8);
                                    //if (tempCover > 255) tempCover = 255;
                                    calculatedCoverage = (byte)tempCover;

                                    startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;

                                    #region blend here
                                    dst = BufferData[startXPosition];
                                    dstRB = dst & 0x00FF00FF;
                                    dstG = (dst >> 8) & 0xFF;

                                    BufferData[startXPosition] =
                                        (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        | (uint)((((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        | (uint)(((((colorRB - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
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

                    // increase color index
                    currentColorIndexValue++;
                }
                #endregion
            }
            #endregion
        }
        #endregion

        #region On Filling Diangonal NonZero (!transform, !gamma)
        /// <summary>
        /// Fill to buffer base rows data information using non-zero rule
        /// </summary>
        /// <param name="paint">linear gradient object</param>
        /// <param name="rows">row data information</param>
        /// <param name="startRowIndex">start row index in row array need to draw</param>
        /// <param name="endRowIndex">end row index in end row array need to draw</param>
        /// <param name="isForward">is diagonal gradient is forward</param>
        void OnFillingDiagonalNonZero(
            LinearGradient paint,
            uint opacity,
            RowData[] rows,
            int startRowIndex,
            int endRowIndex,
            bool isForward)
        {

            #region Explain for fomula

            /*
             * CALCULATION NEED FOLLOWING VALUES
             * 1/ INCREMENT
             * increment, when x from n to n+1 , 
             * index of color will increase from
             * f(n) to f(n) + increment
             * 
             * this increment value is calculated by
             * Linear from A to B
             * A              C      B'
             * *  *  *  *   *  *  *
             *    *         *    *
             *       *      *   *
             *          *   *  *
             *              * B
             * AC = w of the rect
             * BB' |_ AB
             * So AB' = (AB * AB)/AC = d * d / w
             * And increment is increment = 256 / AB'
             * it mean when x go from A to B'
             * color index will increase from 0=>255 ( 256 steps)
             * 
             * 
             * 2/ DISTANCE
             *              (x3,y3)
             *                *                  
             *               *
             *              *
             *             *
             *            *
             *           *
             *          *
             *  (x1,y1)*
             *               *
             *                     *
             *                           *
             *                         (x2,y2)
             *                    
             * x3,y3 can be calculated by following fomula
             *      x3 = x1 - height of paint = x1 - ( y2- y1);
             *      y3 = y1 + width of paint = y1 + ( x2 - x1);
             *      
             * to determine color at point(x,y) to line (x1,y1)-(x3,y3)
             * from this distance we can determine the color at this 
             * point by lookup to color array
             * 
             * distance = ((x - x3) * (y3-y1)
             *            - ( y - y3) * (x3 -x1))/(distance from start and end point of paint);
             */
            #endregion

            #region Pre-process
            double x1 = 0;
            double y1 = 0;
            double x2 = 0;
            double y2 = 0;
            if (isForward)
            {
                x1 = paint.StartX;
                y1 = paint.StartY;
                x2 = paint.EndX;
                y2 = paint.EndY;
            }
            else
            {
                x1 = paint.EndX;
                y1 = paint.StartY;

                x2 = paint.StartX;
                y2 = paint.EndY;
            }

            double widthOfPaint = x2 - x1;
            double heightOfPaint = y2 - y1;
            //note: start and end point is random
            // start not always on top-left
            // so width of paint and height of paint may be negative
            if (widthOfPaint == 0)
            {
                // this will change to vertical
                OnFillingVerticalNonZero(paint, opacity, rows, startRowIndex, endRowIndex);
                return;
            }
            else if (heightOfPaint == 0)
            {
                // this will change to horizontal
                OnFillingHorizontalNonZero(paint, opacity, rows, startRowIndex, endRowIndex);
                return;
            }
            #endregion

            #region calculate the increasement

            double x3 = x1 - heightOfPaint;
            double y3 = y1 + widthOfPaint;

            double lengthOfPaint = Math.Sqrt((widthOfPaint * widthOfPaint) + (heightOfPaint * heightOfPaint));
            //int distanceOfPaintScaled = (int)(distanceOfPaint * DistanceScale);
            double incrementColorIndex = (double)(widthOfPaint * ColorIndexScale) / (lengthOfPaint * lengthOfPaint);

            // increment by distance scale
            // increment may be greater than 512, but in reflect,repeat mode, 
            // just modulo it
            // get the remain when divide by 512
            // incrementColorIndex = incrementColorIndex - (((int)incrementColorIndex / ColorIndexDoubleScale) * ColorIndexDoubleScale); 

            //incrementX < 512, calculate incrementIndex  
            // ( that scale by 256 for approxiate calculation )
            int scaledIncrementColorIndex = (int)(incrementColorIndex * IncrementColorIndexScale);

            #endregion

            // now not need to check null or not
            uint[] builtColors = paint.GetLinearColors(opacity);

            #region private variable for filling
            int currentCoverage, scLastCoverage, scLastX = 0;
            int tempCover = 0;
            int currentArea = 0;
            int lastXPosition = 0;
            int startXPosition = 0;
            byte calculatedCoverage = 0;

            // this color index is scaled
            int currentColorIndexScaled = 0;

            CellData currentCellData = null;
            uint colorData = 0;
            uint dst, dstRB, dstG;
            double firstPointDistance = 0;
            #endregion

            #region optimization for color index

            // the ORIGIN fomula for each row, we need to calculate this
            //firstPointDistance = (((x3) * (y3 - y1) - (startRowIndex - y3) * (x3 - x1)) / distanceOfPaint);
            //// color index = (distance from point to line => scaled) * 256/ (distance of paint scaled)
            //currentColorIndexScaled =
            //    (int)((firstPointDistance * ColorIndexIncludeIncrementScale / distanceOfPaint));
            //    currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask; // mod ( 512 << 8)


            // now we need calculate for first time only and after a row, we need to add and small value
            //firstPointDistance  is x value when line cut the horizontal at position startRowIndex
            //firstPointDistance = (((x3) * (y3 - y1) - (startRowIndex - y3) * (x3 - x1)) /(lengthOfPaint));
            // y = slope * x + beta
            //=> slope * x - y + beta = 0
            double slope = (y3 - y1) / (x3 - x1);
            double beta = (y3 - slope * x3);
            // fomula to calculate distance from point to line a*x + b*y + c= 0
            // is d = (a*x1 + b*y1 + c) / sqrt(a*a + b*b)
            // in this case d = (slope * x1 + (-1) * y1 + beta) / sqrt ( slope * slope + (-1) * (-1))
            //firstPointDistance = (-startRowIndex + beta) / Math.Sqrt(slope * slope + 1);


            //http://mathworld.wolfram.com/Point-LineDistance2-Dimensional.html
            firstPointDistance = ((x3 - x1) * (y1 - startRowIndex) - (x1 - 0) * (y3 - y1))
                / lengthOfPaint;

            int startOfRowIndex = (int)((firstPointDistance * ColorIndexIncludeIncrementScale / lengthOfPaint));
            int rowColorIndexIncrementScaled = (int)(((-(x3 - x1) / lengthOfPaint) * ColorIndexIncludeIncrementScale / lengthOfPaint));

            #endregion

            #region FILLING
            if (paint.Ramp.NoBlendingColor)
            {
                if (paint.Style != GradientStyle.Pad)
                {
                    #region GradientStyle.Reflect || GradientStyle.Repeat
                    // in case reflect and repeat, we don't care value that out of range
                    startOfRowIndex &= ColorIndexIncludeIncrementDoubleMask;
                    rowColorIndexIncrementScaled &= ColorIndexIncludeIncrementDoubleMask;
                    scaledIncrementColorIndex &= ColorIndexIncludeIncrementDoubleMask;

                    #region filling without blend for horizontal lines
                    // when no need to blending, when draw a horizontal line
                    // do not need check the back color, alway setup
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;

                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                // calculate the first cell color index
                                #region second way to implement color index
                                currentColorIndexScaled = startOfRowIndex;
                                #endregion

                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;

                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1))
                                    {
                                        if (scLastCoverage != 0)
                                        {
                                            // fast bit absolute
                                            scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                            #region non-zero checking code
                                            if (scLastCoverage > 255) scLastCoverage = 255;
                                            #endregion
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            if (scLastCoverage >= 254)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    BufferData[startXPosition] = builtColors[currentColorIndexScaled < 0 ?
                                                        (currentColorIndexScaled >> IncrementColorIndexShift) + 512 :
                                                        (currentColorIndexScaled >> IncrementColorIndexShift)];
                                                    startXPosition++;
                                                    // increase current color index
                                                    currentColorIndexScaled += scaledIncrementColorIndex;
                                                    currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
                                                }
                                            }
                                            else
                                            {
                                                calculatedCoverage = (byte)scLastCoverage;
                                                while (startXPosition < lastXPosition)
                                                {
                                                    dst = BufferData[startXPosition];
                                                    colorData = builtColors[currentColorIndexScaled < 0 ?
                                                            (currentColorIndexScaled >> IncrementColorIndexShift) + 512 :
                                                            (currentColorIndexScaled >> IncrementColorIndexShift)];
                                                    dstRB = dst & 0x00FF00FF;
                                                    dstG = (dst >> 8) & 0xFF;
                                                    BufferData[startXPosition] =
                                                        (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (uint)((((((((colorData & 0x0000FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                        | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                    startXPosition++;
                                                    // increase the current color index
                                                    currentColorIndexScaled += scaledIncrementColorIndex;
                                                    currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
                                                }
                                            }
                                            #endregion
                                        }
                                        else
                                        {
                                            // not filling but must set and increase the color index
                                            currentColorIndexScaled += (currentCellData.X - scLastX - 1) * scaledIncrementColorIndex;
                                            currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
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

                                        #region non-zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion

                                        // get current color data
                                        #region blend pixel
                                        //tempCover = (int)((tempCover * colorAlpha) >> 8);
                                        ////if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;

                                        #region blend here
                                        dst = BufferData[startXPosition];
                                        colorData = builtColors[currentColorIndexScaled < 0 ?
                                                (currentColorIndexScaled >> IncrementColorIndexShift) + 512 :
                                                (currentColorIndexScaled >> IncrementColorIndexShift)];
                                        dstRB = dst & 0x00FF00FF;
                                        dstG = (dst >> 8) & 0xFF;

                                        BufferData[startXPosition] =
                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (uint)((((((((colorData & 0x0000FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion

                                        #endregion
                                    }
                                    #endregion

                                    // alway increment color index
                                    currentColorIndexScaled += scaledIncrementColorIndex;
                                    currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;

                                    // assign value for next loop
                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                        #region each row we need increase the value of color index
                        startOfRowIndex += rowColorIndexIncrementScaled;
                        startOfRowIndex &= ColorIndexIncludeIncrementDoubleMask;
                        #endregion
                    }
                    #endregion
                    #endregion
                }//Reflect or Repeat mode
                else
                {//Pad mode
                    #region GradientStyle.Pad
                    // when no need to blending, when draw a horizontal line
                    // do not need check the back color, alway setup
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;

                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                // calculate the first cell color index
                                #region second way to implement color index
                                currentColorIndexScaled = startOfRowIndex;
                                #endregion

                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;

                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1))
                                    {
                                        if (scLastCoverage != 0)
                                        {
                                            // fast bit absolute
                                            scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);

                                            #region non-zero checking code
                                            if (scLastCoverage > 255) scLastCoverage = 255;
                                            #endregion

                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            if (scLastCoverage >= 254)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    BufferData[startXPosition] = builtColors[
                                                        currentColorIndexScaled < 0 ?
                                                            0 :
                                                        (currentColorIndexScaled > ColorIndexIncludeIncrementScale ?
                                                            255 :
                                                            (currentColorIndexScaled >> IncrementColorIndexShift))];
                                                    startXPosition++;
                                                    // increase current color index
                                                    currentColorIndexScaled += scaledIncrementColorIndex;
                                                }
                                            }
                                            else
                                            {
                                                calculatedCoverage = (byte)scLastCoverage;
                                                while (startXPosition < lastXPosition)
                                                {
                                                    colorData = builtColors[
                                                        currentColorIndexScaled < 0 ?
                                                            0 :
                                                        (currentColorIndexScaled > ColorIndexIncludeIncrementScale ?
                                                            255 :
                                                            (currentColorIndexScaled >> IncrementColorIndexShift))];
                                                    #region blend here
                                                    dst = BufferData[startXPosition];
                                                    dstRB = dst & 0x00FF00FF;
                                                    dstG = (dst >> 8) & 0xFF;
                                                    BufferData[startXPosition] =
                                                        (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (uint)((((((((colorData & 0x0000FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                        | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                    #endregion
                                                    startXPosition++;
                                                    // increase the current color index
                                                    currentColorIndexScaled += scaledIncrementColorIndex;
                                                }
                                            }
                                            #endregion
                                        }
                                        else
                                        {
                                            // not filling but must set and increase the color index
                                            currentColorIndexScaled += (currentCellData.X - scLastX - 1) * scaledIncrementColorIndex;
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

                                        #region non-zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion
                                        // get current color data
                                        #region blend pixel
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;

                                        #region blend here
                                        dst = BufferData[startXPosition];
                                        colorData = builtColors[
                                            currentColorIndexScaled < 0 ?
                                                0 :
                                            (currentColorIndexScaled > ColorIndexIncludeIncrementScale ?
                                                255 :
                                                (currentColorIndexScaled >> IncrementColorIndexShift))];

                                        dstRB = dst & 0x00FF00FF;
                                        dstG = (dst >> 8) & 0xFF;
                                        BufferData[startXPosition] =
                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (uint)((((((((colorData & 0x0000FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion


                                    }
                                    #endregion

                                    // alway increment color index
                                    currentColorIndexScaled += scaledIncrementColorIndex;

                                    // assign value for next loop
                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                        #region each row we need increase the value of color index
                        startOfRowIndex += rowColorIndexIncrementScaled;
                        #endregion
                    }
                    #endregion
                }//Pad mode
            }//paint.Ramp.NoBlendingColor
            else
            {//has blending color
                // blending include alpha of built color
                if (paint.Style != GradientStyle.Pad)
                {
                    #region GradientStyle.Reflect || GradientStyle.Repeat
                    // in case reflect and repeat, we don't care value that out of range
                    startOfRowIndex &= ColorIndexIncludeIncrementDoubleMask;
                    rowColorIndexIncrementScaled &= ColorIndexIncludeIncrementDoubleMask;
                    // when no need to blending, when draw a horizontal line
                    // do not need check the back color, alway setup
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;

                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                // calculate the first cell color index
                                #region second way to implement color index
                                currentColorIndexScaled = startOfRowIndex;
                                #endregion

                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1))
                                    {
                                        if (scLastCoverage != 0)
                                        {
                                            // fast bit absolute
                                            scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);

                                            #region non-zero checking code
                                            if (scLastCoverage > 255) scLastCoverage = 255;
                                            #endregion

                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;

                                            while (startXPosition < lastXPosition)
                                            {
                                                colorData = builtColors[currentColorIndexScaled < 0 ?
                                                        (currentColorIndexScaled >> IncrementColorIndexShift) + 512 :
                                                        (currentColorIndexScaled >> IncrementColorIndexShift)];

                                                // get current color index value
                                                calculatedCoverage = (byte)(((colorData >> 24) * scLastCoverage) >> 8);

                                                #region blend here
                                                dst = BufferData[startXPosition];
                                                dstRB = dst & 0x00FF00FF;
                                                dstG = (dst >> 8) & 0xFF;
                                                BufferData[startXPosition] =
                                                    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                    | (uint)((((((((colorData & 0x0000FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                    | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                startXPosition++;
                                                #endregion

                                                // increase the current color index
                                                currentColorIndexScaled += scaledIncrementColorIndex;
                                                currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
                                            }
                                            #endregion
                                        }
                                        else
                                        {
                                            // not filling but must set and increase the color index
                                            currentColorIndexScaled += (currentCellData.X - scLastX - 1) * scaledIncrementColorIndex;
                                            currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
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

                                        #region non-zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion

                                        // get current color data
                                        #region blend pixel
                                        //tempCover = (int)((tempCover * colorAlpha) >> 8);
                                        ////if (tempCover > 255) tempCover = 255;
                                        //calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;

                                        #region blend here
                                        colorData = builtColors[currentColorIndexScaled < 0 ?
                                                (currentColorIndexScaled >> IncrementColorIndexShift) + 512 :
                                                (currentColorIndexScaled >> IncrementColorIndexShift)];
                                        calculatedCoverage = (byte)(((colorData >> 24) * tempCover) >> 8);
                                        dst = BufferData[startXPosition];
                                        dstRB = dst & 0x00FF00FF;
                                        dstG = (dst >> 8) & 0xFF;

                                        BufferData[startXPosition] =
                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (uint)((((((((colorData & 0x0000FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion

                                        #endregion
                                    }
                                    #endregion

                                    // alway increment color index
                                    currentColorIndexScaled += scaledIncrementColorIndex;
                                    currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;

                                    // assign value for next loop
                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }

                        #region each row we need increase the value of color index
                        startOfRowIndex += rowColorIndexIncrementScaled;
                        startOfRowIndex &= ColorIndexIncludeIncrementDoubleMask;
                        #endregion
                    }
                    #endregion
                    #endregion
                }//Reflect or Repeat mode
                else
                {//Pad mode
                    #region GradientStyle.Pad
                    // when no need to blending, when draw a horizontal line
                    // do not need check the back color, alway setup
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;

                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                // calculate the first cell color index
                                #region second way to implement color index
                                currentColorIndexScaled = startOfRowIndex;
                                #endregion

                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;

                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1))
                                    {
                                        if (scLastCoverage != 0)
                                        {
                                            // fast bit absolute
                                            scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);

                                            #region non-zero checking code
                                            if (scLastCoverage > 255) scLastCoverage = 255;
                                            #endregion

                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value

                                            while (startXPosition < lastXPosition)
                                            {
                                                colorData = builtColors[currentColorIndexScaled < 0 ?
                                                    0 : (currentColorIndexScaled > ColorIndexIncludeIncrementScale ? 255 :
                                                    (currentColorIndexScaled >> IncrementColorIndexShift))];
                                                calculatedCoverage = (byte)(((colorData >> 24) * scLastCoverage) >> 8);

                                                #region blend here
                                                dst = BufferData[startXPosition];
                                                dstRB = dst & 0x00FF00FF;
                                                dstG = (dst >> 8) & 0xFF;
                                                BufferData[startXPosition] =
                                                    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                    | (uint)((((((((colorData & 0x0000FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                    | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                #endregion

                                                startXPosition++;
                                                // increase the current color index
                                                currentColorIndexScaled += scaledIncrementColorIndex;
                                                //currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
                                            }

                                            #endregion
                                        }
                                        else
                                        {
                                            // not filling but must set and increase the color index
                                            currentColorIndexScaled += (currentCellData.X - scLastX - 1) * scaledIncrementColorIndex;
                                            //currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
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

                                        #region non-zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion

                                        // get current color data
                                        #region blend pixel
                                        //calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;

                                        #region blend here
                                        colorData = builtColors[currentColorIndexScaled < 0 ?
                                                        0 : (currentColorIndexScaled > ColorIndexIncludeIncrementScale ? 255 :
                                                        (currentColorIndexScaled >> IncrementColorIndexShift))];
                                        calculatedCoverage = (byte)(((colorData >> 24) * tempCover) >> 8);
                                        dst = BufferData[startXPosition];
                                        dstRB = dst & 0x00FF00FF;
                                        dstG = (dst >> 8) & 0xFF;

                                        BufferData[startXPosition] =
                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (uint)((((((((colorData & 0x0000FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion

                                        #endregion
                                    }
                                    #endregion

                                    // alway increment color index
                                    currentColorIndexScaled += scaledIncrementColorIndex;

                                    // assign value for next loop
                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }

                        #region each row we need increase the value of color index
                        startOfRowIndex += rowColorIndexIncrementScaled;
                        #endregion
                    }
                    #endregion
                }//Pad mode
            }//has blending color

            #endregion
        }
        #endregion

        #endregion

        #region NON-ZERO (gamma)

        #region On Filling NonZero (!transform, gamma)
        /// <summary>
        /// Fill to buffer base rows data information using non-zero rule and using lookup table
        /// </summary>
        /// <param name="paint">paint using for fill</param>
        /// <param name="rows">row data information</param>
        /// <param name="startRowIndex">start row index in row array need to draw</param>
        /// <param name="endRowIndex">end row index in end row array need to draw</param>
        /// <param name="gammaLutRed">gamma look up table for red</param>
        /// <param name="gammaLutGreen">gamma look up table for green</param>
        /// <param name="gammaLutBlue">gamma look up table for blue</param>
        protected override void OnFillingNonZero(
            PaintMaterial paint,
            RowData[] rows,
            int startRowIndex,
            int endRowIndex,
            byte[] gammaLutRed,
            byte[] gammaLutGreen,
            byte[] gammaLutBlue)
        {
            if (!(paint.Paint is LinearGradient))
            {
                NotMatchPaintTypeException.Publish(typeof(LinearGradient), paint.Paint.GetType());
                return;
            }
            LinearGradient linearGradient = paint.Paint as LinearGradient;
            switch (linearGradient.Mode)
            {
                case LinearGradientMode.Horizontal:
                    OnFillingHorizontalNonZero(linearGradient, paint.ScaledOpacity, rows, startRowIndex, endRowIndex, gammaLutRed, gammaLutGreen, gammaLutBlue);
                    break;
                case LinearGradientMode.Vertical:
                    OnFillingVerticalNonZero(linearGradient, paint.ScaledOpacity, rows, startRowIndex, endRowIndex, gammaLutRed, gammaLutGreen, gammaLutBlue);
                    break;
                case LinearGradientMode.ForwardDiagonal:
                    OnFillingDiagonalNonZero(linearGradient, paint.ScaledOpacity, rows, startRowIndex, endRowIndex, true, gammaLutRed, gammaLutGreen, gammaLutBlue);
                    break;
                case LinearGradientMode.BackwardDiagonal:
                    OnFillingDiagonalNonZero(linearGradient, paint.ScaledOpacity, rows, startRowIndex, endRowIndex, false, gammaLutRed, gammaLutGreen, gammaLutBlue);
                    break;
            }
        }
        #endregion

        #region On Filling Horizontal NonZero (!transform, gamma)
        /// <summary>
        /// Fill to buffer base rows data information using non-zero rule
        /// </summary>
        /// <param name="paint">linear gradient object</param>
        /// <param name="rows">row data information</param>
        /// <param name="startRowIndex">start row index in row array need to draw</param>
        /// <param name="endRowIndex">end row index in end row array need to draw</param>
        /// <param name="gammaLutRed">gamma look up table for red</param>
        /// <param name="gammaLutGreen">gamma look up table for green</param>
        /// <param name="gammaLutBlue">gamma look up table for blue</param>
        void OnFillingHorizontalNonZero(
            LinearGradient paint,
            uint opacity,
            RowData[] rows,
            int startRowIndex,
            int endRowIndex,
            byte[] gammaLutRed,
            byte[] gammaLutGreen,
            byte[] gammaLutBlue)
        {
            // now not need to check null or not
            uint[] builtColors = paint.GetLinearColors(opacity);

            /*Base on startX,endX, we need build fixedColor array
             * contain width count elements. So that, at a column, we
             * can lookup color for that column.
             */

            #region Build fixed color
            double startX = paint.StartX;
            double endX = paint.EndX;

            // width of this
            int width = CurrentEndXIndex - CurrentStartXIndex + 1;
            uint[] fixedColor = new uint[width];
            int distanceScaled = (int)(Math.Abs(startX - endX) * DistanceScale);
            if (distanceScaled == 0)
            {
                FillingException.Publish(typeof(LinearGradient), "Start point and end point are too close");
                return;
            }
            #region building fixed color array
            if (paint.Style == GradientStyle.Pad)
            {
                #region GradientStyle.Pad
                int startXScaled = (int)(startX * DistanceScale);
                int startFixedIndex = (((
                    (((width + CurrentStartXIndex) << DistanceShift) - startXScaled)
                    << ColorIndexShift) / distanceScaled)) << IncrementColorIndexShift;
                int colorIncrement = (DistanceScale * ColorIndexIncludeIncrementScale) / distanceScaled;
                if (endX < startX)
                {
                    colorIncrement = -colorIncrement;
                    startFixedIndex = -startFixedIndex;
                }
                while (width-- > 0)
                {
                    fixedColor[width] =
                            builtColors[startFixedIndex < 0 ?
                                0 :
                            (startFixedIndex > ColorIndexIncludeIncrementScale ?
                                255 :
                                (startFixedIndex >> IncrementColorIndexShift))];
                    startFixedIndex -= colorIncrement;
                }
                #endregion
            }
            else
            {
                #region GradientStyle.Repeat || GradientStyle.Reflect
                int startXScaled = (int)(startX * DistanceScale);
                int startFixedIndex = (((
                        (((width + CurrentStartXIndex) << DistanceShift) - startXScaled)
                        << ColorIndexShift) / distanceScaled)) << IncrementColorIndexShift;
                int colorIncrement = (DistanceScale * ColorIndexIncludeIncrementScale) / distanceScaled;
                if (endX < startX)
                {
                    colorIncrement = -colorIncrement;
                }
                startFixedIndex &= ColorIndexIncludeIncrementDoubleMask;
                while (width-- > 0)
                {
                    fixedColor[width] = builtColors[
                        startFixedIndex < 0 ?
                            (startFixedIndex >> IncrementColorIndexShift) + 512 :
                            (startFixedIndex >> IncrementColorIndexShift)];
                    startFixedIndex -= colorIncrement;
                    startFixedIndex &= ColorIndexIncludeIncrementDoubleMask;
                }
                #endregion
            }
            #endregion

            #endregion

            #region private variable for filling
            int currentCoverage, scLastCoverage, scLastX = 0;
            int tempCover = 0;
            int currentArea = 0;
            int lastXPosition = 0;
            int startXPosition = 0;
            byte calculatedCoverage = 0;

            int currentColorIndexValue = 0;
            CellData currentCellData = null;
            uint colorData = 0;

            uint dst, dstRB, dstG;
            #endregion

            #region FILLING
            if (paint.Ramp.NoBlendingColor)
            {
                #region filling without blend for horizontal lines
                // when no need to blending, when draw a horizontal line
                // do not need check the back color, alway setup
                startRowIndex--;
                while (++startRowIndex <= endRowIndex)
                {
                    currentCoverage = scLastCoverage = scLastX = 0;

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

                                    #region non-zero checking code
                                    if (scLastCoverage > 255) scLastCoverage = 255;
                                    #endregion

                                    if (scLastCoverage != 0)
                                    {
                                        #region BLEND HORIZONTAL LINE
                                        // calculate start and end position
                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                        lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        // get current color index value
                                        currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                        if (scLastCoverage >= 255)
                                        {
                                            while (startXPosition < lastXPosition)
                                            {
                                                colorData = fixedColor[currentColorIndexValue];
                                                // set data to position
                                                BufferData[startXPosition] = colorData;
                                                startXPosition++;
                                                currentColorIndexValue++;
                                            }
                                        }
                                        else
                                        {
                                            while (startXPosition < lastXPosition)
                                            {
                                                colorData = fixedColor[currentColorIndexValue];
                                                calculatedCoverage = (byte)((colorData >> 24) & 0xFF);
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                if (calculatedCoverage >= 255)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    #region blend here
                                                    //dst = BufferData[startXPosition];
                                                    //dstRB = dst & 0x00FF00FF;
                                                    //dstG = (dst >> 8) & 0xFF;

                                                    //BufferData[startXPosition] =
                                                    //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + currentColorAlpha] << 24)
                                                    //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * currentColorAlpha) >> 8) + dstG) << 8) & 0x0000FF00)
                                                    //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * currentColorAlpha) >> 8) + dstRB) & 0x00FF00FF);

                                                    #region gamma apply
                                                    dst = BufferData[startXPosition];
                                                    dstG = (dst >> 8) & 0xFF;
                                                    dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                    BufferData[startXPosition] =
                                                        (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                        | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                        | ((uint)gammaLutBlue[(dstRB & 0x00FF)]));
                                                    #endregion
                                                    #endregion
                                                }
                                                startXPosition++;
                                                currentColorIndexValue++;
                                            }
                                        }
                                        #endregion
                                    }
                                }
                                #endregion

                                currentCoverage += currentCellData.Coverage;

                                #region blend the current cell
                                // calculate coverage
                                tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                if (tempCover != 0)
                                {
                                    // fast bit absolute
                                    tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                    #region non-zero checking code
                                    if (tempCover > 255) tempCover = 255;
                                    #endregion

                                    // get current color data
                                    colorData = fixedColor[currentCellData.X - CurrentStartXIndex];
                                    calculatedCoverage = (byte)(colorData >> 24);

                                    #region blend pixel
                                    tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                    //if (tempCover > 255) tempCover = 255;
                                    calculatedCoverage = (byte)tempCover;
                                    startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                    if (calculatedCoverage >= 254)
                                    {
                                        BufferData[startXPosition] = colorData;
                                    }
                                    else
                                    {
                                        #region blend here
                                        dst = BufferData[startXPosition];
                                        dstG = (dst >> 8) & 0xFF;
                                        dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                        BufferData[startXPosition] =
                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                            | ((uint)gammaLutBlue[(dstRB & 0x00FF)]));
                                        #endregion
                                    }
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
            }//paint.Ramp.NoBlendingColor
            else
            {//has blending color
                #region perform normal filling
                startRowIndex--;
                while (++startRowIndex <= endRowIndex)
                {
                    currentCoverage = scLastCoverage = scLastX = 0;

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

                                    #region non-zero checking code
                                    if (scLastCoverage > 255) scLastCoverage = 255;
                                    #endregion

                                    if (scLastCoverage != 0)
                                    {
                                        #region BLEND HORIZONTAL LINE
                                        // calculate start and end position
                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                        lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        // get current color index value
                                        currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                        while (startXPosition < lastXPosition)
                                        {
                                            colorData = fixedColor[currentColorIndexValue];
                                            calculatedCoverage = (byte)((colorData >> 24) & 0xFF);
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
                                                    | ((uint)gammaLutBlue[(dstRB & 0x00FF)]));
                                                #endregion
                                            }
                                            startXPosition++;
                                            currentColorIndexValue++;
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

                                    #region non-zero checking code
                                    if (tempCover > 255) tempCover = 255;
                                    #endregion

                                    // get current color data
                                    colorData = fixedColor[currentCellData.X - CurrentStartXIndex];
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
                                        | ((uint)gammaLutBlue[(dstRB & 0x00FF)]));
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
            }//has blending color

            #endregion
        }
        #endregion

        #region On Filling Vertical NonZero (!transform, gamma)
        /// <summary>
        /// Fill to buffer base rows data information using non-zero rule
        /// </summary>
        /// <param name="paint">linear gradient object</param>
        /// <param name="rows">row data information</param>
        /// <param name="startRowIndex">start row index in row array need to draw</param>
        /// <param name="endRowIndex">end row index in end row array need to draw</param>
        /// <param name="gammaLutRed">gamma look up table for red</param>
        /// <param name="gammaLutGreen">gamma look up table for green</param>
        /// <param name="gammaLutBlue">gamma look up table for blue</param>
        void OnFillingVerticalNonZero(
            LinearGradient paint,
            uint opacity,
            RowData[] rows,
            int startRowIndex,
            int endRowIndex,
            byte[] gammaLutRed,
            byte[] gammaLutGreen,
            byte[] gammaLutBlue)
        {
            // now not need to check null or not
            uint[] builtColors = paint.GetLinearColors(opacity);

            /*  Base on startX,endX, we need build fixedColor array
             *  contain width count elements. So that, at a column, we
             *  can lookup color for that column. */

            #region Build fixed color
            double startY = paint.StartY;
            double endY = paint.EndY;

            // width of this
            int height = endRowIndex - startRowIndex + 1;
            uint[] fixedColor = new uint[height];
            int distanceScaled = (int)(Math.Abs(startY - endY) * DistanceScale);
            if (distanceScaled == 0)
            {
                FillingException.Publish(typeof(LinearGradient), "Start point and end point are too close");
                return;
            }

            #region building fixed color array
            if (paint.Style == GradientStyle.Pad)
            {
                #region GradientStyle.Pad
                int startFixedIndex = (((
                    (((height + startRowIndex) << DistanceShift) - (int)(startY * DistanceScale))
                    << ColorIndexShift) / distanceScaled)) << IncrementColorIndexShift;
                int colorIncrement = (DistanceScale * ColorIndexIncludeIncrementScale) / distanceScaled;
                if (endY < startY)
                {
                    colorIncrement = -colorIncrement;
                    startFixedIndex = -startFixedIndex;
                }
                while (height-- > 0)
                {
                    fixedColor[height] = builtColors[
                        startFixedIndex < 0 ?
                            0 :
                        (startFixedIndex > ColorIndexIncludeIncrementScale ?
                            255 :
                            (startFixedIndex >> IncrementColorIndexShift))];
                    startFixedIndex -= colorIncrement;
                }
                #endregion
            }
            else
            {
                #region GradientStyle.Reflect || GradientStyle.Repeat
                int startFixedIndex = (((
                        (((height + startRowIndex) << DistanceShift) - (int)(startY * DistanceScale))
                        << ColorIndexShift) / distanceScaled)) << IncrementColorIndexShift;
                int colorIncrement = (DistanceScale * ColorIndexIncludeIncrementScale) / distanceScaled;
                if (endY < startY)
                {
                    colorIncrement = -colorIncrement;
                }
                startFixedIndex &= ColorIndexIncludeIncrementDoubleMask;
                while (height-- > 0)
                {
                    fixedColor[height] = builtColors[
                        startFixedIndex < 0 ?
                            (startFixedIndex >> IncrementColorIndexShift) + 512 :
                            (startFixedIndex >> IncrementColorIndexShift)];
                    startFixedIndex -= colorIncrement;
                    startFixedIndex &= ColorIndexIncludeIncrementDoubleMask;
                }
                #endregion
            }
            #endregion

            #endregion

            #region private variable for filling
            int currentCoverage, scLastCoverage, scLastX = 0;
            int tempCover = 0;
            int currentArea = 0;
            int lastXPosition = 0;
            int startXPosition = 0;
            byte calculatedCoverage = 0;

            int currentColorIndexValue = 0;

            CellData currentCellData = null;
            uint colorData = 0;
            uint colorAlpha = 0;
            uint colorG = 0;
            uint colorRB = 0;

            uint dst, dstRB, dstG;
            #endregion

            #region FILLING

            if (paint.Ramp.NoBlendingColor)
            {//paint.Ramp.NoBlendingColor
                #region filling without blend for horizontal lines
                // when no need to blending, when draw a horizontal line
                // do not need check the back color, alway setup
                startRowIndex--;
                while (++startRowIndex <= endRowIndex)
                {
                    currentCoverage = scLastCoverage = scLastX = 0;

                    if (rows[startRowIndex] != null)
                    {
                        #region calculate and get current color
                        colorData = fixedColor[currentColorIndexValue];
                        colorAlpha = (colorData >> 24);
                        colorG = (colorData & 0x0000FF00) >> 8;
                        colorRB = (colorData & 0x00FF00FF);
                        #endregion

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

                                    #region non-zero checking code
                                    if (scLastCoverage > 255) scLastCoverage = 255;
                                    #endregion

                                    if (scLastCoverage != 0)
                                    {
                                        #region BLEND HORIZONTAL LINE
                                        // calculate start and end position
                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                        lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        // get current color index value
                                        if (scLastCoverage >= 254)
                                        {
                                            while (startXPosition < lastXPosition)
                                            {
                                                BufferData[startXPosition++] = colorData;
                                            }
                                        }
                                        else
                                        {
                                            while (startXPosition < lastXPosition)
                                            {
                                                calculatedCoverage = (byte)((scLastCoverage * colorAlpha) >> 8);
                                                if (calculatedCoverage >= 254)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    #region blend here
                                                    //dst = BufferData[startXPosition];
                                                    //dstRB = dst & 0x00FF00FF;
                                                    //dstG = (dst >> 8) & 0xFF;

                                                    //BufferData[startXPosition] =
                                                    //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                    //    | (uint)((((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                    //    | (uint)(((((colorRB - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                                    #region gamma apply
                                                    dst = BufferData[startXPosition];
                                                    dstG = (dst >> 8) & 0xFF;
                                                    dstRB = ((((colorRB - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                    BufferData[startXPosition] =
                                                        (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (((uint)gammaLutGreen[(((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                        | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                        | (gammaLutBlue[(dstRB & 0x00FF)]));
                                                    #endregion
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

                                    #region non-zero checking code
                                    if (tempCover > 255) tempCover = 255;
                                    #endregion

                                    // get current color data
                                    #region blend pixel
                                    tempCover = (int)((tempCover * colorAlpha) >> 8);
                                    //if (tempCover > 255) tempCover = 255;
                                    calculatedCoverage = (byte)tempCover;

                                    startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;

                                    #region blend here
                                    //dst = BufferData[startXPosition];
                                    //dstRB = dst & 0x00FF00FF;
                                    //dstG = (dst >> 8) & 0xFF;
                                    //BufferData[startXPosition] =
                                    //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                    //    | (uint)((((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                    //    | (uint)(((((colorRB - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                    if (calculatedCoverage >= 254)
                                    {
                                        BufferData[startXPosition] = colorData;
                                    }
                                    else
                                    {
                                        #region gamma apply
                                        dst = BufferData[startXPosition];
                                        dstG = (dst >> 8) & 0xFF;
                                        dstRB = ((((colorRB - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                        BufferData[startXPosition] =
                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (((uint)gammaLutGreen[(((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                            | (gammaLutBlue[(dstRB & 0x00FF)]));
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
                    // increase color index
                    currentColorIndexValue++;
                }
                #endregion
            }//paint.Ramp.NoBlendingColor
            else
            {
                #region perform normal filling
                startRowIndex--;
                while (++startRowIndex <= endRowIndex)
                {
                    currentCoverage = scLastCoverage = scLastX = 0;

                    if (rows[startRowIndex] != null)
                    {
                        #region calculate and get current color
                        colorData = fixedColor[currentColorIndexValue];
                        colorAlpha = (colorData >> 24);
                        colorG = (colorData & 0x0000FF00) >> 8;
                        colorRB = (colorData & 0x00FF00FF);
                        #endregion

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

                                    #region non-zero checking code
                                    if (scLastCoverage > 255) scLastCoverage = 255;
                                    #endregion

                                    if (scLastCoverage != 0)
                                    {
                                        #region BLEND HORIZONTAL LINE
                                        // calculate start and end position
                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                        lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;

                                        while (startXPosition < lastXPosition)
                                        {
                                            calculatedCoverage = (byte)((scLastCoverage * colorAlpha) >> 8);
                                            if (calculatedCoverage >= 254)
                                            {
                                                BufferData[startXPosition] = colorData;
                                            }
                                            else
                                            {
                                                #region blend here
                                                //dst = BufferData[startXPosition];
                                                //dstRB = dst & 0x00FF00FF;
                                                //dstG = (dst >> 8) & 0xFF;

                                                //BufferData[startXPosition] =
                                                //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                //    | (uint)((((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                //    | (uint)(((((colorRB - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                                #region gamma apply
                                                dst = BufferData[startXPosition];
                                                dstG = (dst >> 8) & 0xFF;
                                                dstRB = ((((colorRB - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                BufferData[startXPosition] =
                                                    (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                    | (((uint)gammaLutGreen[(((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                    | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                    | (gammaLutBlue[(dstRB & 0x00FF)]));
                                                #endregion
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

                                    #region non-zero checking code
                                    if (tempCover > 255) tempCover = 255;
                                    #endregion

                                    #region blend pixel
                                    tempCover = (int)((tempCover * colorAlpha) >> 8);
                                    //if (tempCover > 255) tempCover = 255;
                                    calculatedCoverage = (byte)tempCover;

                                    startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                    #region blend here
                                    //dst = BufferData[startXPosition];
                                    //dstRB = dst & 0x00FF00FF;
                                    //dstG = (dst >> 8) & 0xFF;
                                    //BufferData[startXPosition] =
                                    //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                    //    | (uint)((((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                    //    | (uint)(((((colorRB - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                    #region gamma apply
                                    dst = BufferData[startXPosition];
                                    dstG = (dst >> 8) & 0xFF;
                                    dstRB = ((((colorRB - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                    BufferData[startXPosition] =
                                        (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        | (((uint)gammaLutGreen[(((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                        | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                        | (gammaLutBlue[(dstRB & 0x00FF)]));
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

                    // increase color index
                    currentColorIndexValue++;
                }
                #endregion
            }

            #endregion
        }
        #endregion

        #region On Filling Diangonal NonZero (!transform, gamma)
        /// <summary>
        /// Fill to buffer base rows data information using non-zero rule
        /// </summary>
        /// <param name="paint">linear gradient object</param>
        /// <param name="rows">row data information</param>
        /// <param name="startRowIndex">start row index in row array need to draw</param>
        /// <param name="endRowIndex">end row index in end row array need to draw</param>
        /// <param name="isForward">is diagonal gradient is forward</param>
        /// <param name="gammaLutRed">gamma look up table for red</param>
        /// <param name="gammaLutGreen">gamma look up table for green</param>
        /// <param name="gammaLutBlue">gamma look up table for blue</param>
        void OnFillingDiagonalNonZero(
            LinearGradient paint,
            uint opacity,
            RowData[] rows,
            int startRowIndex,
            int endRowIndex,
            bool isForward,
            byte[] gammaLutRed,
            byte[] gammaLutGreen,
            byte[] gammaLutBlue)
        {

            #region Explain for fomula

            /*
             * CALCULATION NEED FOLLOWING VALUES
             * 1/ INCREMENT
             * increment, when x from n to n+1 , 
             * index of color will increase from
             * f(n) to f(n) + increment
             * 
             * this increment value is calculated by
             * Linear from A to B
             * A              C      B'
             * *  *  *  *   *  *  *
             *    *         *    *
             *       *      *   *
             *          *   *  *
             *              * B
             * AC = w of the rect
             * BB' |_ AB
             * So AB' = (AB * AB)/AC = d * d / w
             * And increment is increment = 256 / AB'
             * it mean when x go from A to B'
             * color index will increase from 0=>255 ( 256 steps)
             * 
             * 
             * 2/ DISTANCE
             *              (x3,y3)
             *                *                  
             *               *
             *              *
             *             *
             *            *
             *           *
             *          *
             *  (x1,y1)*
             *               *
             *                     *
             *                           *
             *                         (x2,y2)
             *                    
             * x3,y3 can be calculated by following fomula
             *      x3 = x1 - height of paint = x1 - ( y2- y1);
             *      y3 = y1 + width of paint = y1 + ( x2 - x1);
             *      
             * to determine color at point(x,y) to line (x1,y1)-(x3,y3)
             * from this distance we can determine the color at this 
             * point by lookup to color array
             * 
             * distance = ((x - x3) * (y3-y1)
             *            - ( y - y3) * (x3 -x1))/(distance from start and end point of paint);
             */
            #endregion

            #region Pre-process
            double x1 = 0;
            double y1 = 0;
            double x2 = 0;
            double y2 = 0;
            if (isForward)
            {
                x1 = paint.StartX;
                y1 = paint.StartY;
                x2 = paint.EndX;
                y2 = paint.EndY;
            }
            else
            {
                x1 = paint.EndX;
                y1 = paint.StartY;

                x2 = paint.StartX;
                y2 = paint.EndY;
            }

            double widthOfPaint = x2 - x1;
            double heightOfPaint = y2 - y1;
            //note: start and end point is random
            // start not always on top-left
            // so width of paint and height of paint may be negative
            if (widthOfPaint == 0)
            {
                // this will change to vertical
                OnFillingVerticalNonZero(paint, opacity, rows, startRowIndex, endRowIndex);
                return;
            }
            else if (heightOfPaint == 0)
            {
                // this will change to horizontal
                OnFillingHorizontalNonZero(paint, opacity, rows, startRowIndex, endRowIndex);
                return;
            }
            #endregion

            #region calculate the increasement

            double x3 = x1 - heightOfPaint;
            double y3 = y1 + widthOfPaint;

            double lengthOfPaint = Math.Sqrt((widthOfPaint * widthOfPaint) + (heightOfPaint * heightOfPaint));
            //int distanceOfPaintScaled = (int)(distanceOfPaint * DistanceScale);
            double incrementColorIndex = (double)(widthOfPaint * ColorIndexScale) / (lengthOfPaint * lengthOfPaint);

            // increment by distance scale
            // increment may be greater than 512, but in reflect,repeat mode, 
            // just modulo it
            // get the remain when divide by 512
            // incrementColorIndex = incrementColorIndex - (((int)incrementColorIndex / ColorIndexDoubleScale) * ColorIndexDoubleScale); 

            //incrementX < 512, calculate incrementIndex  
            // ( that scale by 256 for approxiate calculation )
            int scaledIncrementColorIndex = (int)(incrementColorIndex * IncrementColorIndexScale);

            #endregion

            // now not need to check null or not
            uint[] builtColors = paint.GetLinearColors(opacity);

            #region private variable for filling
            int currentCoverage, scLastCoverage, scLastX = 0;
            int tempCover = 0;
            int currentArea = 0;
            int lastXPosition = 0;
            int startXPosition = 0;
            byte calculatedCoverage = 0;

            // this color index is scaled
            int currentColorIndexScaled = 0;

            CellData currentCellData = null;
            uint colorData = 0;
            uint dst, dstRB, dstG;
            double firstPointDistance = 0;
            #endregion

            #region optimization for color index
            // the ORIGIN fomula for each row, we need to calculate this
            //firstPointDistance = (((x3) * (y3 - y1) - (startRowIndex - y3) * (x3 - x1)) / distanceOfPaint);
            //// color index = (distance from point to line => scaled) * 256/ (distance of paint scaled)
            //currentColorIndexScaled =
            //    (int)((firstPointDistance * ColorIndexIncludeIncrementScale / distanceOfPaint));
            //    currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask; // mod ( 512 << 8)


            // now we need calculate for first time only and after a row, we need to add and small value
            //firstPointDistance  is x value when line cut the horizontal at position startRowIndex
            //firstPointDistance = (((x3) * (y3 - y1) - (startRowIndex - y3) * (x3 - x1)) /(lengthOfPaint));
            // y = slope * x + beta
            //=> slope * x - y + beta = 0
            double slope = (y3 - y1) / (x3 - x1);
            double beta = (y3 - slope * x3);
            // fomula to calculate distance from point to line a*x + b*y + c= 0
            // is d = (a*x1 + b*y1 + c) / sqrt(a*a + b*b)
            // in this case d = (slope * x1 + (-1) * y1 + beta) / sqrt ( slope * slope + (-1) * (-1))
            //firstPointDistance = (-startRowIndex + beta) / Math.Sqrt(slope * slope + 1);


            //http://mathworld.wolfram.com/Point-LineDistance2-Dimensional.html
            firstPointDistance = ((x3 - x1) * (y1 - startRowIndex) - (x1 - 0) * (y3 - y1))
                / lengthOfPaint;

            int startOfRowIndex = (int)((firstPointDistance * ColorIndexIncludeIncrementScale / lengthOfPaint));
            int rowColorIndexIncrementScaled = (int)(((-(x3 - x1) / lengthOfPaint) * ColorIndexIncludeIncrementScale / lengthOfPaint));

            #endregion

            #region FILLING
            if (paint.Ramp.NoBlendingColor)
            {
                if (paint.Style != GradientStyle.Pad)
                {
                    #region GradientStyle.Reflect || GradientStyle.Repeat
                    // in case reflect and repeat, we don't care value that out of range
                    startOfRowIndex &= ColorIndexIncludeIncrementDoubleMask;
                    rowColorIndexIncrementScaled &= ColorIndexIncludeIncrementDoubleMask;
                    scaledIncrementColorIndex &= ColorIndexIncludeIncrementDoubleMask;

                    // when no need to blending, when draw a horizontal line
                    // do not need check the back color, alway setup
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;

                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                // calculate the first cell color index
                                #region second way to implement color index
                                currentColorIndexScaled = startOfRowIndex;
                                #endregion

                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;

                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1))
                                    {
                                        if (scLastCoverage != 0)
                                        {
                                            // fast bit absolute
                                            scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);

                                            #region non-zero checking code
                                            if (scLastCoverage > 255) scLastCoverage = 255;
                                            #endregion

                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            if (scLastCoverage >= 254)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    BufferData[startXPosition] = builtColors[currentColorIndexScaled < 0 ?
                                                        (currentColorIndexScaled >> IncrementColorIndexShift) + 512 :
                                                        (currentColorIndexScaled >> IncrementColorIndexShift)];
                                                    startXPosition++;
                                                    // increase current color index
                                                    currentColorIndexScaled += scaledIncrementColorIndex;
                                                    currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
                                                }
                                            }
                                            else
                                            {
                                                calculatedCoverage = (byte)scLastCoverage;
                                                while (startXPosition < lastXPosition)
                                                {
                                                    colorData = builtColors[currentColorIndexScaled < 0 ?
                                                            (currentColorIndexScaled >> IncrementColorIndexShift) + 512 :
                                                            (currentColorIndexScaled >> IncrementColorIndexShift)];

                                                    #region blending
                                                    //dst = BufferData[startXPosition];
                                                    //dstRB = dst & 0x00FF00FF;
                                                    //dstG = (dst >> 8) & 0xFF;

                                                    //BufferData[startXPosition] =
                                                    //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                    //    | (uint)((((((((colorData & 0x0000FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                    //    | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                                    #region gamma apply
                                                    dst = BufferData[startXPosition];
                                                    dstG = (dst >> 8) & 0xFF;
                                                    dstRB = (((((colorData & 0x00FF00FF) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                    BufferData[startXPosition] =
                                                        (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                        | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                        | (gammaLutBlue[(dstRB & 0x00FF)]));
                                                    #endregion
                                                    #endregion

                                                    startXPosition++;
                                                    // increase the current color index
                                                    currentColorIndexScaled += scaledIncrementColorIndex;
                                                    currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
                                                }
                                            }
                                            #endregion
                                        }
                                        else
                                        {
                                            // not filling but must set and increase the color index
                                            currentColorIndexScaled += (currentCellData.X - scLastX - 1) * scaledIncrementColorIndex;
                                            currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
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

                                        #region non-zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion

                                        #region blend pixel
                                        //tempCover = (int)((tempCover * colorAlpha) >> 8);
                                        ////if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        if (calculatedCoverage >= 254)
                                        {
                                            BufferData[startXPosition] = builtColors[currentColorIndexScaled < 0 ?
                                                    (currentColorIndexScaled >> IncrementColorIndexShift) + 512 :
                                                    (currentColorIndexScaled >> IncrementColorIndexShift)];
                                        }
                                        else
                                        {
                                            #region blend here
                                            colorData = builtColors[currentColorIndexScaled < 0 ?
                                                    (currentColorIndexScaled >> IncrementColorIndexShift) + 512 :
                                                    (currentColorIndexScaled >> IncrementColorIndexShift)];

                                            //dst = BufferData[startXPosition];
                                            //dstRB = dst & 0x00FF00FF;
                                            //dstG = (dst >> 8) & 0xFF;
                                            //BufferData[startXPosition] =
                                            //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            //    | (uint)((((((((colorData & 0x0000FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            //    | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                            #region apply gamma
                                            dst = BufferData[startXPosition];
                                            dstG = (dst >> 8) & 0xFF;
                                            dstRB = (((((colorData & 0x00FF00FF) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                            BufferData[startXPosition] =
                                                (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                | (gammaLutBlue[(dstRB & 0x00FF)]));
                                            #endregion
                                            #endregion
                                        }
                                        #endregion
                                    }
                                    #endregion

                                    // alway increment color index
                                    currentColorIndexScaled += scaledIncrementColorIndex;
                                    currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;

                                    // assign value for next loop
                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }

                        #region each row we need increase the value of color index
                        startOfRowIndex += rowColorIndexIncrementScaled;
                        startOfRowIndex &= ColorIndexIncludeIncrementDoubleMask;
                        #endregion
                    }
                    #endregion
                    #endregion
                }//reflect or repeate mode
                else // special case using for pad mode
                {//pad mode
                    #region filling without blend for horizontal lines
                    // when no need to blending, when draw a horizontal line
                    // do not need check the back color, alway setup
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;

                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                // calculate the first cell color index
                                #region second way to implement color index
                                currentColorIndexScaled = startOfRowIndex;
                                #endregion

                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;

                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1))
                                    {
                                        if (scLastCoverage != 0)
                                        {
                                            // fast bit absolute
                                            scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);

                                            #region non-zero checking code
                                            if (scLastCoverage > 255) scLastCoverage = 255;
                                            #endregion

                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            if (scLastCoverage >= 254)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    BufferData[startXPosition] = builtColors[
                                                        currentColorIndexScaled < 0 ?
                                                            0 :
                                                        (currentColorIndexScaled > ColorIndexIncludeIncrementScale ?
                                                            255 :
                                                            (currentColorIndexScaled >> IncrementColorIndexShift))];
                                                    startXPosition++;
                                                    // increase current color index
                                                    currentColorIndexScaled += scaledIncrementColorIndex;
                                                }
                                            }
                                            else
                                            {
                                                calculatedCoverage = (byte)scLastCoverage;
                                                while (startXPosition < lastXPosition)
                                                {
                                                    colorData = builtColors[currentColorIndexScaled < 0 ?
                                                        0 : (currentColorIndexScaled > ColorIndexIncludeIncrementScale ? 255 :
                                                        (currentColorIndexScaled >> IncrementColorIndexShift))];

                                                    #region blend here
                                                    //dst = BufferData[startXPosition];
                                                    //dstRB = dst & 0x00FF00FF;
                                                    //dstG = (dst >> 8) & 0xFF;
                                                    //BufferData[startXPosition] =
                                                    //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                    //    | (uint)((((((((colorData & 0x0000FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                    //    | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                                    #region apply gamma
                                                    dst = BufferData[startXPosition];
                                                    dstG = (dst >> 8) & 0xFF;
                                                    dstRB = (((((colorData & 0x00FF00FF) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                    BufferData[startXPosition] =
                                                        (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                        | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                        | (gammaLutBlue[(dstRB & 0x00FF)]));
                                                    #endregion
                                                    #endregion

                                                    startXPosition++;
                                                    // increase the current color index
                                                    currentColorIndexScaled += scaledIncrementColorIndex;
                                                }
                                            }
                                            #endregion
                                        }
                                        else
                                        {
                                            // not filling but must set and increase the color index
                                            currentColorIndexScaled += (currentCellData.X - scLastX - 1) * scaledIncrementColorIndex;
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

                                        #region non-zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion

                                        // get current color data
                                        #region blend pixel
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;

                                        #region blend here
                                        colorData = builtColors[currentColorIndexScaled < 0 ?
                                                        0 : (currentColorIndexScaled > ColorIndexIncludeIncrementScale ? 255 :
                                                        (currentColorIndexScaled >> IncrementColorIndexShift))];

                                        //dst = BufferData[startXPosition];
                                        //colorData = builtColors[currentColorIndexScaled < 0 ?
                                        //                0 : (currentColorIndexScaled > ColorIndexIncludeIncrementScale ? 255 :
                                        //                (currentColorIndexScaled >> IncrementColorIndexShift))];

                                        //dstRB = dst & 0x00FF00FF;
                                        //dstG = (dst >> 8) & 0xFF;
                                        //BufferData[startXPosition] =
                                        //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        //    | (uint)((((((((colorData & 0x0000FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        //    | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                        #region apply gamma
                                        dst = BufferData[startXPosition];
                                        dstG = (dst >> 8) & 0xFF;
                                        dstRB = (((((colorData & 0x00FF00FF) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                        BufferData[startXPosition] =
                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                            | (gammaLutBlue[(dstRB & 0x00FF)]));
                                        #endregion
                                        #endregion

                                        #endregion
                                    }
                                    #endregion

                                    // alway increment color index
                                    currentColorIndexScaled += scaledIncrementColorIndex;

                                    // assign value for next loop
                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }

                        #region each row we need increase the value of color index
                        startOfRowIndex += rowColorIndexIncrementScaled;
                        #endregion
                    }
                    #endregion
                }//pad mode
            }//no BlendingColor
            else
            {//has blending color
                // blending include alpha of built color
                if (paint.Style != GradientStyle.Pad)
                {
                    #region GradientStyle.Reflect || GradientStyle.Repeat
                    // in case reflect and repeat, we don't care value that out of range
                    startOfRowIndex &= ColorIndexIncludeIncrementDoubleMask;
                    rowColorIndexIncrementScaled &= ColorIndexIncludeIncrementDoubleMask;
                    // when no need to blending, when draw a horizontal line
                    // do not need check the back color, alway setup
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;

                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                // calculate the first cell color index
                                #region second way to implement color index
                                currentColorIndexScaled = startOfRowIndex;
                                #endregion

                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;

                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1))
                                    {
                                        if (scLastCoverage != 0)
                                        {
                                            // fast bit absolute
                                            scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);

                                            #region non-zero checking code
                                            if (scLastCoverage > 255) scLastCoverage = 255;
                                            #endregion

                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;

                                            while (startXPosition < lastXPosition)
                                            {
                                                colorData = builtColors[currentColorIndexScaled < 0 ?
                                                        (currentColorIndexScaled >> IncrementColorIndexShift) + 512 :
                                                        (currentColorIndexScaled >> IncrementColorIndexShift)];

                                                // get current color index value
                                                calculatedCoverage = (byte)(((colorData >> 24) * scLastCoverage) >> 8);

                                                #region blend here
                                                //dst = BufferData[startXPosition];
                                                //dstRB = dst & 0x00FF00FF;
                                                //dstG = (dst >> 8) & 0xFF;
                                                //BufferData[startXPosition] =
                                                //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                //    | (uint)((((((((colorData & 0x0000FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                //    | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                                #region apply gamma
                                                dst = BufferData[startXPosition];
                                                dstG = (dst >> 8) & 0xFF;
                                                dstRB = (((((colorData & 0x00FF00FF) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                BufferData[startXPosition] =
                                                    (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                    | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                    | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                    | (gammaLutBlue[(dstRB & 0x00FF)]));
                                                #endregion
                                                #endregion

                                                startXPosition++;
                                                // increase the current color index
                                                currentColorIndexScaled += scaledIncrementColorIndex;
                                                currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
                                            }
                                            #endregion
                                        }
                                        else
                                        {
                                            // not filling but must set and increase the color index
                                            currentColorIndexScaled += (currentCellData.X - scLastX - 1) * scaledIncrementColorIndex;
                                            currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
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

                                        #region non-zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion

                                        // get current color data
                                        #region blend pixel
                                        //tempCover = (int)((tempCover * colorAlpha) >> 8);
                                        ////if (tempCover > 255) tempCover = 255;
                                        //calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;

                                        #region blend here

                                        colorData = builtColors[currentColorIndexScaled < 0 ?
                                                (currentColorIndexScaled >> IncrementColorIndexShift) + 512 :
                                                (currentColorIndexScaled >> IncrementColorIndexShift)];
                                        calculatedCoverage = (byte)(((colorData >> 24) * tempCover) >> 8);

                                        //dst = BufferData[startXPosition];
                                        //dstRB = dst & 0x00FF00FF;
                                        //dstG = (dst >> 8) & 0xFF;

                                        //BufferData[startXPosition] =
                                        //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        //    | (uint)((((((((colorData & 0x0000FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        //    | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #region apply gamma
                                        dst = BufferData[startXPosition];
                                        dstG = (dst >> 8) & 0xFF;
                                        dstRB = (((((colorData & 0x00FF00FF) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                        BufferData[startXPosition] =
                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                            | (gammaLutBlue[(dstRB & 0x00FF)]));
                                        #endregion
                                        #endregion

                                        #endregion

                                    }
                                    #endregion

                                    // alway increment color index
                                    currentColorIndexScaled += scaledIncrementColorIndex;
                                    currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;

                                    // assign value for next loop
                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }

                        #region each row we need increase the value of color index
                        startOfRowIndex += rowColorIndexIncrementScaled;
                        startOfRowIndex &= ColorIndexIncludeIncrementDoubleMask;
                        #endregion
                    }
                    #endregion
                    #endregion
                }//reflect or repeate mode
                else
                {//pad mode
                    #region filling without blend for horizontal lines
                    // when no need to blending, when draw a horizontal line
                    // do not need check the back color, alway setup
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;

                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                // calculate the first cell color index
                                #region second way to implement color index
                                currentColorIndexScaled = startOfRowIndex;
                                #endregion

                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;

                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1))
                                    {
                                        if (scLastCoverage != 0)
                                        {
                                            // fast bit absolute
                                            scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);

                                            #region non-zero checking code
                                            if (scLastCoverage > 255) scLastCoverage = 255;
                                            #endregion

                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value

                                            while (startXPosition < lastXPosition)
                                            {

                                                colorData = builtColors[currentColorIndexScaled < 0 ?
                                                    0 : (currentColorIndexScaled > ColorIndexIncludeIncrementScale ? 255 :
                                                    (currentColorIndexScaled >> IncrementColorIndexShift))];

                                                calculatedCoverage = (byte)(((colorData >> 24) * scLastCoverage) >> 8);

                                                #region blend here
                                                ////dst = BufferData[startXPosition];
                                                ////dstRB = dst & 0x00FF00FF;
                                                ////dstG = (dst >> 8) & 0xFF;
                                                ////BufferData[startXPosition] =
                                                ////    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                ////    | (uint)((((((((colorData & 0x0000FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                ////    | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                #region apply gamma
                                                dst = BufferData[startXPosition];
                                                dstG = (dst >> 8) & 0xFF;
                                                dstRB = (((((colorData & 0x00FF00FF) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                BufferData[startXPosition] =
                                                    (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                    | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                    | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                    | (gammaLutBlue[(dstRB & 0x00FF)]));
                                                #endregion
                                                #endregion

                                                startXPosition++;
                                                // increase the current color index
                                                currentColorIndexScaled += scaledIncrementColorIndex;
                                                //currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
                                            }

                                            #endregion
                                        }
                                        else
                                        {
                                            // not filling but must set and increase the color index
                                            currentColorIndexScaled += (currentCellData.X - scLastX - 1) * scaledIncrementColorIndex;
                                            //currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
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

                                        #region non-zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion

                                        // get current color data
                                        #region blend pixel
                                        //calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;

                                        #region blend here

                                        colorData = builtColors[currentColorIndexScaled < 0 ?
                                                        0 : (currentColorIndexScaled > ColorIndexIncludeIncrementScale ? 255 :
                                                        (currentColorIndexScaled >> IncrementColorIndexShift))];
                                        calculatedCoverage = (byte)(((colorData >> 24) * tempCover) >> 8);

                                        //dst = BufferData[startXPosition];
                                        //dstRB = dst & 0x00FF00FF;
                                        //dstG = (dst >> 8) & 0xFF;
                                        //BufferData[startXPosition] =
                                        //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        //    | (uint)((((((((colorData & 0x0000FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        //    | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #region apply gamma
                                        dst = BufferData[startXPosition];
                                        dstG = (dst >> 8) & 0xFF;
                                        dstRB = (((((colorData & 0x00FF00FF) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                        BufferData[startXPosition] =
                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                            | (gammaLutBlue[(dstRB & 0x00FF)]));
                                        #endregion
                                        #endregion

                                        #endregion

                                    }
                                    #endregion

                                    // alway increment color index
                                    currentColorIndexScaled += scaledIncrementColorIndex;

                                    // assign value for next loop
                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }

                        #region each row we need increase the value of color index
                        startOfRowIndex += rowColorIndexIncrementScaled;
                        #endregion
                    }
                    #endregion
                }//pad mode
            }//has blending color

            #endregion
        }
        #endregion

        #endregion


        #region EVEN-ODD (!gamma)

        #region On Filling EvenOdd (!transform, !gamma)
        /// <summary>
        /// Fill to buffer base rows data information using even-odd rule
        /// </summary>
        /// <param name="paint">paint using for fill</param>
        /// <param name="rows">row data information</param>
        /// <param name="startRowIndex">start row index in row array need to draw</param>
        /// <param name="endRowIndex">end row index in end row array need to draw</param>
        protected override void OnFillingEvenOdd(
            PaintMaterial paint,
            RowData[] rows,
            int startRowIndex,
            int endRowIndex)
        {
            if (!(paint.Paint is LinearGradient))
            {
                NotMatchPaintTypeException.Publish(typeof(LinearGradient), paint.Paint.GetType());
                return;
            }
            LinearGradient linearGradient = paint.Paint as LinearGradient;

            switch (linearGradient.Mode)
            {
                case LinearGradientMode.Horizontal:
                    OnFillingHorizontalEvenOdd(linearGradient, paint.ScaledOpacity, rows, startRowIndex, endRowIndex);
                    break;
                case LinearGradientMode.Vertical:
                    OnFillingVerticalEvenOdd(linearGradient, paint.ScaledOpacity, rows, startRowIndex, endRowIndex);
                    break;
                case LinearGradientMode.ForwardDiagonal:
                    OnFillingDiagonalEvenOdd(linearGradient, paint.ScaledOpacity, rows, startRowIndex, endRowIndex, true);
                    break;
                case LinearGradientMode.BackwardDiagonal:
                    OnFillingDiagonalEvenOdd(linearGradient, paint.ScaledOpacity, rows, startRowIndex, endRowIndex, false);
                    break;
            }
        }
        #endregion

        #region On Filling Horizontal EvenOdd (!transform, !gamma)
        /// <summary>
        /// Fill to buffer base rows data information using non-zero rule
        /// </summary>
        /// <param name="paint">linear gradient object</param>
        /// <param name="rows">row data information</param>
        /// <param name="startRowIndex">start row index in row array need to draw</param>
        /// <param name="endRowIndex">end row index in end row array need to draw</param>
        void OnFillingHorizontalEvenOdd(
            LinearGradient paint,
            uint opacity,
            RowData[] rows,
            int startRowIndex,
            int endRowIndex)
        {
            // now not need to check null or not
            uint[] builtColors = paint.GetLinearColors(opacity);

            /*Base on startX,endX, we need build fixedColor array
             * contain width count elements. So that, at a column, we
             * can lookup color for that column.
             */

            #region build fixed color
            double startX = paint.StartX;
            double endX = paint.EndX;

            // width of this
            int width = CurrentEndXIndex - CurrentStartXIndex + 1;
            uint[] fixedColor = new uint[width];
            int distanceScaled = (int)(Math.Abs(startX - endX) * DistanceScale);
            if (distanceScaled == 0)
            {
                FillingException.Publish(typeof(LinearGradient), "Start point and end point are too close");
                return;
            }
            #region building fixed color array
            if (paint.Style == GradientStyle.Pad)
            {
                #region GradientStyle.Pad
                int startXScaled = (int)(startX * DistanceScale);
                int startFixedIndex = (((
                    (((width + CurrentStartXIndex) << DistanceShift) - startXScaled)
                    << ColorIndexShift) / distanceScaled)) << IncrementColorIndexShift;

                int colorIncrement = (DistanceScale * ColorIndexIncludeIncrementScale) / distanceScaled;
                if (endX < startX)
                {
                    colorIncrement = -colorIncrement;
                    startFixedIndex = -startFixedIndex;
                }
                while (width-- > 0)
                {
                    fixedColor[width] = builtColors[
                        startFixedIndex < 0 ?
                            0 :
                        (startFixedIndex > ColorIndexIncludeIncrementScale ?
                            255 :
                            (startFixedIndex >> IncrementColorIndexShift))];
                    startFixedIndex -= colorIncrement;
                }
                #endregion
            }
            else
            {
                #region when mode are repeat or reflect
                int startXScaled = (int)(startX * DistanceScale);
                int startFixedIndex = (((
                    (((width + CurrentStartXIndex) << DistanceShift) - startXScaled)
                    << ColorIndexShift) / distanceScaled)) << IncrementColorIndexShift;
                int colorIncrement = (DistanceScale * ColorIndexIncludeIncrementScale) / distanceScaled;
                if (endX < startX)
                {
                    colorIncrement = -colorIncrement;
                }
                startFixedIndex &= ColorIndexIncludeIncrementDoubleMask;
                while (width-- > 0)
                {
                    fixedColor[width] = builtColors[
                        startFixedIndex < 0 ?
                            (startFixedIndex >> IncrementColorIndexShift) + 512 :
                            (startFixedIndex >> IncrementColorIndexShift)];
                    startFixedIndex -= colorIncrement;
                    startFixedIndex &= ColorIndexIncludeIncrementDoubleMask;
                }
                #endregion
            }
            #endregion

            #endregion

            #region private variable for filling
            int currentCoverage, scLastCoverage, scLastX = 0;
            int tempCover = 0;
            int currentArea = 0;
            int lastXPosition = 0;
            int startXPosition = 0;
            byte calculatedCoverage = 0;

            int currentColorIndexValue = 0;
            CellData currentCellData = null;
            uint colorData = 0;
            uint dst, dstRB, dstG;
            #endregion

            #region FILLING
            if (paint.Ramp.NoBlendingColor)
            {
                #region filling without blending
                startRowIndex--;
                while (++startRowIndex <= endRowIndex)
                {
                    currentCoverage = scLastCoverage = scLastX = 0;

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
                                    #region even-odd change
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
                                        currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                        if (scLastCoverage >= 255)
                                        {
                                            while (startXPosition < lastXPosition)
                                            {
                                                //colorData = fixedColor[currentColorIndexValue];
                                                //// just set
                                                //BufferData[startXPosition] = colorData;
                                                //startXPosition++;
                                                //currentColorIndexValue++;

                                                BufferData[startXPosition++] = fixedColor[currentColorIndexValue++];
                                            }
                                        }
                                        else
                                        {
                                            while (startXPosition < lastXPosition)
                                            {
                                                colorData = fixedColor[currentColorIndexValue];
                                                calculatedCoverage = (byte)((colorData >> 24) & 0xFF);
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                if (calculatedCoverage >= 255)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    dst = BufferData[startXPosition];
                                                    dstRB = dst & 0x00FF00FF;
                                                    dstG = (dst >> 8) & 0xFF;
                                                    BufferData[startXPosition] =
                                                        (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                        | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                                    #region apply gamma
                                                    //dst = BufferData[startXPosition];
                                                    //dstG = (dst >> 8) & 0xFF;
                                                    ////dstRB = (dst & 0x00FF00FF);
                                                    //dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                    //BufferData[startXPosition] =
                                                    //    (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                    //    | ((gammaLut[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                    //    | (gammaLut[(dstRB & 0x00FF0000) >> 16] << 16)
                                                    //    | (gammaLut[(dstRB & 0x00FF)]))
                                                    //    ;
                                                    #endregion
                                                }
                                                startXPosition++;
                                                currentColorIndexValue++;
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
                                // fast bit absolute
                                tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                #region even-odd change
                                tempCover &= 511;
                                if (tempCover >= 256)
                                {
                                    tempCover = 512 - tempCover - 1;
                                }
                                #endregion

                                if (tempCover != 0)
                                {
                                    // get current color data
                                    colorData = fixedColor[currentCellData.X - CurrentStartXIndex];
                                    calculatedCoverage = (byte)(colorData >> 24);

                                    #region blend pixel
                                    tempCover = (int)((tempCover * calculatedCoverage) >> 8);
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

                                    #region apply gamma
                                    //dst = BufferData[startXPosition];
                                    //dstG = (dst >> 8) & 0xFF;
                                    ////dstRB = (dst & 0x00FF00FF);
                                    //dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                    //BufferData[startXPosition] =
                                    //    (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                    //    | ((gammaLut[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                    //    | (gammaLut[(dstRB & 0x00FF0000) >> 16] << 16)
                                    //    | (gammaLut[(dstRB & 0x00FF)]))
                                    //    ;
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
                #region filling including blending
                startRowIndex--;
                while (++startRowIndex <= endRowIndex)
                {
                    currentCoverage = scLastCoverage = scLastX = 0;

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
                                    #region even-odd change
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
                                        currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                        while (startXPosition < lastXPosition)
                                        {
                                            colorData = fixedColor[currentColorIndexValue];
                                            calculatedCoverage = (byte)((colorData >> 24) & 0xFF);
                                            calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                            //if (calculatedCoverage >= 255)
                                            //{
                                            //    BufferData[startXPosition] = colorData;
                                            //}
                                            //else
                                            //{
                                            // blend here
                                            dst = BufferData[startXPosition];
                                            dstRB = dst & 0x00FF00FF;
                                            dstG = (dst >> 8) & 0xFF;

                                            BufferData[startXPosition] =
                                                (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                            //}
                                            startXPosition++;
                                            currentColorIndexValue++;
                                        }
                                        #endregion
                                    }
                                }
                                #endregion

                                currentCoverage += currentCellData.Coverage;

                                #region blend the current cell
                                // fast absolute
                                tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                // fast bit absolute
                                tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                #region even-odd change
                                tempCover &= 511;
                                if (tempCover >= 256)
                                {
                                    tempCover = 512 - tempCover - 1;
                                }
                                #endregion

                                if (tempCover != 0)
                                {
                                    // get current color data
                                    colorData = fixedColor[currentCellData.X - CurrentStartXIndex];
                                    calculatedCoverage = (byte)(colorData >> 24);

                                    #region blend pixel
                                    tempCover = (int)((tempCover * calculatedCoverage) >> 8);
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
            #endregion
        }
        #endregion

        #region On Filling Vertical EvenOdd (!transform, !gamma)
        /// <summary>
        /// Fill to buffer base rows data information using non-zero rule
        /// </summary>
        /// <param name="paint">linear gradient object</param>
        /// <param name="rows">row data information</param>
        /// <param name="startRowIndex">start row index in row array need to draw</param>
        /// <param name="endRowIndex">end row index in end row array need to draw</param>
        void OnFillingVerticalEvenOdd(
            LinearGradient paint,
            uint opacity,
            RowData[] rows,
            int startRowIndex,
            int endRowIndex)
        {
            // now not need to check null or not
            uint[] builtColors = paint.GetLinearColors(opacity);

            /*Base on startX,endX, we need build fixedColor array
             * contain width count elements. So that, at a column, we
             * can lookup color for that column.
             */

            #region build fixed color
            double startY = paint.StartY;
            double endY = paint.EndY;

            // width of this
            int height = endRowIndex - startRowIndex + 1;
            uint[] fixedColor = new uint[height];
            int distanceScaled = (int)(Math.Abs(startY - endY) * DistanceScale);
            if (distanceScaled == 0)
            {
                FillingException.Publish(typeof(LinearGradient), "Start point and end point are too close");
                return;
            }
            #region building fixed color array
            if (paint.Style == GradientStyle.Pad)
            {
                #region GradientStyle.Pad
                int startFixedIndex = (((
                        (((height + startRowIndex) << DistanceShift) - (int)(startY * DistanceScale))
                        << ColorIndexShift) / distanceScaled)) << IncrementColorIndexShift;

                int colorIncrement = (DistanceScale * ColorIndexIncludeIncrementScale) / distanceScaled;
                if (endY < startY)
                {
                    colorIncrement = -colorIncrement;
                    startFixedIndex = -startFixedIndex;
                }
                while (height-- > 0)
                {
                    fixedColor[height] =
                        builtColors[startFixedIndex < 0 ?
                            0 :
                        (startFixedIndex > ColorIndexIncludeIncrementScale ?
                            255 :
                            (startFixedIndex >> IncrementColorIndexShift))];
                    startFixedIndex -= colorIncrement;
                }
                #endregion
            }
            else
            {
                #region GradientStyle.Repeat || GradientStyle.Reflect
                int startFixedIndex = (((
                        (((height + startRowIndex) << DistanceShift) - (int)(startY * DistanceScale))
                        << ColorIndexShift) / distanceScaled)) << IncrementColorIndexShift;
                int colorIncrement = (DistanceScale * ColorIndexIncludeIncrementScale) / distanceScaled;
                if (endY < startY)
                {
                    colorIncrement = -colorIncrement;
                }
                startFixedIndex &= ColorIndexIncludeIncrementDoubleMask;
                while (height-- > 0)
                {
                    fixedColor[height] = builtColors[
                        startFixedIndex < 0 ?
                            (startFixedIndex >> IncrementColorIndexShift) + 512 :
                            (startFixedIndex >> IncrementColorIndexShift)];
                    startFixedIndex -= colorIncrement;
                    startFixedIndex &= ColorIndexIncludeIncrementDoubleMask;
                }
                #endregion
            }
            #endregion

            #endregion

            #region private variable for filling
            int currentCoverage, scLastCoverage, scLastX = 0;
            int tempCover = 0;
            int currentArea = 0;
            int lastXPosition = 0;
            int startXPosition = 0;
            byte calculatedCoverage = 0;

            int currentColorIndexValue = 0;

            CellData currentCellData = null;
            uint colorData = 0;
            uint colorAlpha = 0;
            uint colorG = 0;
            uint colorRB = 0;

            uint dst, dstRB, dstG;
            #endregion

            #region FILLING
            if (paint.Ramp.NoBlendingColor)
            {
                // when no need to blending, when draw a horizontal line
                // do not need check the back color, alway setup
                #region filling without blend for horizontal lines
                startRowIndex--;
                while (++startRowIndex <= endRowIndex)
                {
                    currentCoverage = scLastCoverage = scLastX = 0;

                    if (rows[startRowIndex] != null)
                    {
                        #region calculate and get current color
                        colorData = fixedColor[currentColorIndexValue];
                        colorAlpha = (colorData >> 24);
                        colorG = (colorData & 0x0000FF00) >> 8;
                        colorRB = (colorData & 0x00FF00FF);
                        #endregion
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
                                    //#region non-zero checking code
                                    //if (scLastCoverage > 255) scLastCoverage = 255;
                                    //#endregion
                                    #region even-odd change
                                    scLastCoverage &= 511;
                                    if (scLastCoverage >= 256)
                                    {
                                        scLastCoverage = 512 - scLastCoverage - 1;
                                    }
                                    #endregion

                                    #region BLEND HORIZONTAL LINE
                                    // calculate start and end position
                                    startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                    lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                    // get current color index value
                                    if (scLastCoverage >= 254)
                                    {
                                        while (startXPosition < lastXPosition)
                                        {
                                            BufferData[startXPosition++] = colorData;
                                        }
                                    }
                                    else
                                    {
                                        while (startXPosition < lastXPosition)
                                        {
                                            calculatedCoverage = (byte)((scLastCoverage * colorAlpha) >> 8);
                                            if (calculatedCoverage >= 254)
                                            {
                                                BufferData[startXPosition] = colorData;
                                            }
                                            else
                                            {
                                                #region blend here
                                                dst = BufferData[startXPosition];
                                                dstRB = dst & 0x00FF00FF;
                                                dstG = (dst >> 8) & 0xFF;

                                                BufferData[startXPosition] =
                                                    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                    | (uint)((((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                    | (uint)(((((colorRB - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                #endregion
                                            }
                                            startXPosition++;
                                        }
                                    }
                                    #endregion
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

                                    //#region non-zero checking code
                                    //if (tempCover > 255) tempCover = 255;
                                    //#endregion
                                    #region even-odd change
                                    tempCover &= 511;
                                    if (tempCover >= 256)
                                    {
                                        tempCover = 512 - tempCover - 1;
                                    }
                                    #endregion
                                    // get current color data
                                    #region blend pixel
                                    tempCover = (int)((tempCover * colorAlpha) >> 8);
                                    //if (tempCover > 255) tempCover = 255;
                                    calculatedCoverage = (byte)tempCover;

                                    startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;

                                    #region blend here
                                    dst = BufferData[startXPosition];
                                    dstRB = dst & 0x00FF00FF;
                                    dstG = (dst >> 8) & 0xFF;
                                    BufferData[startXPosition] =
                                        (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        | (uint)((((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        | (uint)(((((colorRB - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
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
                    // increase color index
                    currentColorIndexValue++;
                }
                #endregion
            }//paint.Ramp.NoBlendingColor
            else
            {
                #region perform normal filling
                startRowIndex--;
                while (++startRowIndex <= endRowIndex)
                {
                    currentCoverage = scLastCoverage = scLastX = 0;

                    if (rows[startRowIndex] != null)
                    {
                        #region calculate and get current color
                        colorData = fixedColor[currentColorIndexValue];
                        colorAlpha = (colorData >> 24);
                        colorG = (colorData & 0x0000FF00) >> 8;
                        colorRB = (colorData & 0x00FF00FF);
                        #endregion
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
                                    #region even-odd change
                                    scLastCoverage &= 511;
                                    if (scLastCoverage >= 256)
                                    {
                                        scLastCoverage = 512 - scLastCoverage - 1;
                                    }
                                    #endregion

                                    #region BLEND HORIZONTAL LINE
                                    // calculate start and end position
                                    startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                    lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;

                                    while (startXPosition < lastXPosition)
                                    {
                                        calculatedCoverage = (byte)((scLastCoverage * colorAlpha) >> 8);
                                        if (calculatedCoverage >= 254)
                                        {
                                            BufferData[startXPosition] = colorData;
                                        }
                                        else
                                        {
                                            #region blend here
                                            dst = BufferData[startXPosition];
                                            dstRB = dst & 0x00FF00FF;
                                            dstG = (dst >> 8) & 0xFF;

                                            BufferData[startXPosition] =
                                                (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                | (uint)((((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                | (uint)(((((colorRB - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                            #endregion
                                        }
                                        startXPosition++;
                                    }
                                    #endregion
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

                                    #region even-odd change
                                    tempCover &= 511;
                                    if (tempCover >= 256)
                                    {
                                        tempCover = 512 - tempCover - 1;
                                    }
                                    #endregion

                                    #region blend pixel
                                    tempCover = (int)((tempCover * colorAlpha) >> 8);
                                    //if (tempCover > 255) tempCover = 255;
                                    calculatedCoverage = (byte)tempCover;

                                    startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                    #region blend here
                                    dst = BufferData[startXPosition];
                                    dstRB = dst & 0x00FF00FF;
                                    dstG = (dst >> 8) & 0xFF;
                                    BufferData[startXPosition] =
                                        (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        | (uint)((((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        | (uint)(((((colorRB - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
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

                    // increase color index
                    currentColorIndexValue++;
                }
                #endregion
            }// has blending color

            #endregion
        }
        #endregion

        #region On Filling Diangonal EvenOdd (!transform, !gamma)
        /// <summary>
        /// Fill to buffer base rows data information using non-zero rule
        /// </summary>
        /// <param name="paint">linear gradient object</param>
        /// <param name="rows">row data information</param>
        /// <param name="startRowIndex">start row index in row array need to draw</param>
        /// <param name="endRowIndex">end row index in end row array need to draw</param>
        /// <param name="isForward">is diagonal gradient is forward</param>
        void OnFillingDiagonalEvenOdd(
            LinearGradient paint,
            uint opacity,
            RowData[] rows,
            int startRowIndex,
            int endRowIndex,
            bool isForward)
        {

            #region Explain for fomula

            /*
             * CALCULATION NEED FOLLOWING VALUES
             * 1/ INCREMENT
             * increment, when x from n to n+1 , 
             * index of color will increase from
             * f(n) to f(n) + increment
             * 
             * this increment value is calculated by
             * Linear from A to B
             * A              C      B'
             * *  *  *  *   *  *  *
             *    *         *    *
             *       *      *   *
             *          *   *  *
             *              * B
             * AC = w of the rect
             * BB' |_ AB
             * So AB' = (AB * AB)/AC = d * d / w
             * And increment is increment = 256 / AB'
             * it mean when x go from A to B'
             * color index will increase from 0=>255 ( 256 steps)
             * 
             * 
             * 2/ DISTANCE
             *              (x3,y3)
             *                *                  
             *               *
             *              *
             *             *
             *            *
             *           *
             *          *
             *  (x1,y1)*
             *               *
             *                     *
             *                           *
             *                         (x2,y2)
             *                    
             * x3,y3 can be calculated by following fomula
             *      x3 = x1 - height of paint = x1 - ( y2- y1);
             *      y3 = y1 + width of paint = y1 + ( x2 - x1);
             *      
             * to determine color at point(x,y) to line (x1,y1)-(x3,y3)
             * from this distance we can determine the color at this 
             * point by lookup to color array
             * 
             * distance = ((x - x3) * (y3-y1)
             *            - ( y - y3) * (x3 -x1))/(distance from start and end point of paint);
             */
            #endregion

            #region Pre-process
            double x1 = 0;
            double y1 = 0;
            double x2 = 0;
            double y2 = 0;
            if (isForward)
            {
                x1 = paint.StartX;
                y1 = paint.StartY;
                x2 = paint.EndX;
                y2 = paint.EndY;
            }
            else
            {
                x1 = paint.EndX;
                y1 = paint.StartY;

                x2 = paint.StartX;
                y2 = paint.EndY;
            }

            double widthOfPaint = x2 - x1;
            double heightOfPaint = y2 - y1;
            //note: start and end point is random
            // start not always on top-left
            // so width of paint and height of paint may be negative
            if (widthOfPaint == 0)
            {
                // this will change to vertical
                OnFillingVerticalNonZero(paint, opacity, rows, startRowIndex, endRowIndex);
                return;
            }
            else if (heightOfPaint == 0)
            {
                // this will change to horizontal
                OnFillingHorizontalNonZero(paint, opacity, rows, startRowIndex, endRowIndex);
                return;
            }
            #endregion

            #region calculate the increment

            double x3 = x1 - heightOfPaint;
            double y3 = y1 + widthOfPaint;

            double lengthOfPaint = Math.Sqrt((widthOfPaint * widthOfPaint) + (heightOfPaint * heightOfPaint));
            //int distanceOfPaintScaled = (int)(distanceOfPaint * DistanceScale);
            double incrementColorIndex = (double)(widthOfPaint * ColorIndexScale) / (lengthOfPaint * lengthOfPaint);

            // increment by distance scale
            // increment may be greater than 512, but in reflect,repeat mode, 
            // just modulo it
            // get the remain when divide by 512
            // incrementColorIndex = incrementColorIndex - (((int)incrementColorIndex / ColorIndexDoubleScale) * ColorIndexDoubleScale); 

            //incrementX < 512, calculate incrementIndex  
            // ( that scale by 256 for approxiate calculation )
            int scaledIncrementColorIndex = (int)(incrementColorIndex * IncrementColorIndexScale);
            #endregion

            // now not need to check null or not
            uint[] builtColors = paint.GetLinearColors(opacity);

            #region private variable for filling
            int currentCoverage, scLastCoverage, scLastX = 0;
            int tempCover = 0;
            int currentArea = 0;
            int lastXPosition = 0;
            int startXPosition = 0;
            byte calculatedCoverage = 0;

            // this color index is scaled
            int currentColorIndexScaled = 0;

            CellData currentCellData = null;
            uint colorData = 0;
            uint dst, dstRB, dstG;
            double firstPointDistance = 0;
            #endregion

            #region optimization for color index
            // the ORIGIN fomula for each row, we need to calculate this
            //firstPointDistance = (((x3) * (y3 - y1) - (startRowIndex - y3) * (x3 - x1)) / distanceOfPaint);
            //// color index = (distance from point to line => scaled) * 256/ (distance of paint scaled)
            //currentColorIndexScaled =
            //    (int)((firstPointDistance * ColorIndexIncludeIncrementScale / distanceOfPaint));
            //    currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask; // mod ( 512 << 8)


            // now we need calculate for first time only and after a row, we need to add and small value
            //firstPointDistance  is x value when line cut the horizontal at position startRowIndex
            //firstPointDistance = (((x3) * (y3 - y1) - (startRowIndex - y3) * (x3 - x1)) /(lengthOfPaint));
            // y = slope * x + beta
            //=> slope * x - y + beta = 0
            double slope = (y3 - y1) / (x3 - x1);
            double beta = (y3 - slope * x3);
            // fomula to calculate distance from point to line a*x + b*y + c= 0
            // is d = (a*x1 + b*y1 + c) / sqrt(a*a + b*b)
            // in this case d = (slope * x1 + (-1) * y1 + beta) / sqrt ( slope * slope + (-1) * (-1))
            //firstPointDistance = (-startRowIndex + beta) / Math.Sqrt(slope * slope + 1);


            //http://mathworld.wolfram.com/Point-LineDistance2-Dimensional.html
            firstPointDistance = ((x3 - x1) * (y1 - startRowIndex) - (x1 - 0) * (y3 - y1))
                / lengthOfPaint;

            int startOfRowIndex = (int)((firstPointDistance * ColorIndexIncludeIncrementScale / lengthOfPaint));
            int rowColorIndexIncrementScaled = (int)(((-(x3 - x1) / lengthOfPaint) * ColorIndexIncludeIncrementScale / lengthOfPaint));

            #endregion

            #region FILLING
            if (paint.Ramp.NoBlendingColor)
            {
                if (paint.Style != GradientStyle.Pad)
                {
                    // in case reflect and repeat, we don't care value that out of range
                    startOfRowIndex &= ColorIndexIncludeIncrementDoubleMask;
                    rowColorIndexIncrementScaled &= ColorIndexIncludeIncrementDoubleMask;
                    scaledIncrementColorIndex &= ColorIndexIncludeIncrementDoubleMask;

                    // when no need to blending, when draw a horizontal line
                    // do not need check the back color, alway setup
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;

                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                // calculate the first cell color index
                                #region second way to implement color index
                                currentColorIndexScaled = startOfRowIndex;
                                #endregion

                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1))
                                    {
                                        if (scLastCoverage != 0)
                                        {
                                            // fast bit absolute
                                            scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);

                                            //#region non-zero checking code
                                            //if (scLastCoverage > 255) scLastCoverage = 255;
                                            //#endregion

                                            #region even-odd change
                                            scLastCoverage &= 511;
                                            if (scLastCoverage >= 256)
                                            {
                                                scLastCoverage = 512 - scLastCoverage - 1;
                                            }
                                            #endregion

                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            if (scLastCoverage >= 254)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    BufferData[startXPosition] = builtColors[
                                                        currentColorIndexScaled < 0 ?
                                                            (currentColorIndexScaled >> IncrementColorIndexShift) + 512 :
                                                            (currentColorIndexScaled >> IncrementColorIndexShift)];
                                                    startXPosition++;
                                                    // increase current color index
                                                    currentColorIndexScaled += scaledIncrementColorIndex;
                                                    currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
                                                }
                                            }
                                            else
                                            {
                                                calculatedCoverage = (byte)scLastCoverage;
                                                while (startXPosition < lastXPosition)
                                                {
                                                    dst = BufferData[startXPosition];
                                                    colorData = builtColors[currentColorIndexScaled < 0 ?
                                                            (currentColorIndexScaled >> IncrementColorIndexShift) + 512 :
                                                            (currentColorIndexScaled >> IncrementColorIndexShift)];
                                                    dstRB = dst & 0x00FF00FF;
                                                    dstG = (dst >> 8) & 0xFF;
                                                    BufferData[startXPosition] =
                                                        (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (uint)((((((((colorData & 0x0000FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                        | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                    startXPosition++;
                                                    // increase the current color index
                                                    currentColorIndexScaled += scaledIncrementColorIndex;
                                                    currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
                                                }
                                            }
                                            #endregion
                                        }
                                        else
                                        {
                                            // not filling but must set and increase the color index
                                            currentColorIndexScaled += (currentCellData.X - scLastX - 1) * scaledIncrementColorIndex;
                                            currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
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


                                        #region even-odd change
                                        tempCover &= 511;
                                        if (tempCover >= 256)
                                        {
                                            tempCover = 512 - tempCover - 1;
                                        }
                                        #endregion

                                        // get current color data
                                        #region blend pixel
                                        //tempCover = (int)((tempCover * colorAlpha) >> 8);
                                        ////if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;

                                        #region blend here
                                        dst = BufferData[startXPosition];
                                        colorData = builtColors[currentColorIndexScaled < 0 ?
                                                (currentColorIndexScaled >> IncrementColorIndexShift) + 512 :
                                                (currentColorIndexScaled >> IncrementColorIndexShift)];
                                        dstRB = dst & 0x00FF00FF;
                                        dstG = (dst >> 8) & 0xFF;
                                        BufferData[startXPosition] =
                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (uint)((((((((colorData & 0x0000FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion


                                    }
                                    #endregion

                                    // alway increment color index
                                    currentColorIndexScaled += scaledIncrementColorIndex;
                                    currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;

                                    // assign value for next loop
                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                        #region each row we need increase the value of color index
                        startOfRowIndex += rowColorIndexIncrementScaled;
                        startOfRowIndex &= ColorIndexIncludeIncrementDoubleMask;
                        #endregion
                    }
                    #endregion
                }
                else // special case using for pad mode
                {
                    // when no need to blending, when draw a horizontal line
                    // do not need check the back color, alway setup
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;

                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                // calculate the first cell color index
                                #region second way to implement color index
                                currentColorIndexScaled = startOfRowIndex;
                                #endregion

                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1))
                                    {
                                        if (scLastCoverage != 0)
                                        {
                                            // fast bit absolute
                                            scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                            //#region non-zero checking code
                                            //if (scLastCoverage > 255) scLastCoverage = 255;
                                            //#endregion

                                            #region even-odd change
                                            scLastCoverage &= 511;
                                            if (scLastCoverage >= 256)
                                            {
                                                scLastCoverage = 512 - scLastCoverage - 1;
                                            }
                                            #endregion
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            if (scLastCoverage >= 254)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    BufferData[startXPosition] = builtColors[
                                                        currentColorIndexScaled < 0 ?
                                                            0 :
                                                        (currentColorIndexScaled > ColorIndexIncludeIncrementScale ?
                                                            255 :
                                                            (currentColorIndexScaled >> IncrementColorIndexShift))];
                                                    startXPosition++;
                                                    // increase current color index
                                                    currentColorIndexScaled += scaledIncrementColorIndex;
                                                }
                                            }
                                            else
                                            {
                                                calculatedCoverage = (byte)scLastCoverage;
                                                while (startXPosition < lastXPosition)
                                                {
                                                    dst = BufferData[startXPosition];
                                                    colorData = builtColors[currentColorIndexScaled < 0 ?
                                                        0 : (currentColorIndexScaled > ColorIndexIncludeIncrementScale ? 255 :
                                                        (currentColorIndexScaled >> IncrementColorIndexShift))];
                                                    dstRB = dst & 0x00FF00FF;
                                                    dstG = (dst >> 8) & 0xFF;
                                                    BufferData[startXPosition] =
                                                        (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (uint)((((((((colorData & 0x0000FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                        | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                    startXPosition++;
                                                    // increase the current color index
                                                    currentColorIndexScaled += scaledIncrementColorIndex;
                                                }
                                            }
                                            #endregion
                                        }
                                        else
                                        {
                                            // not filling but must set and increase the color index
                                            currentColorIndexScaled += (currentCellData.X - scLastX - 1) * scaledIncrementColorIndex;
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

                                        //#region non-zero checking code
                                        //if (tempCover > 255) tempCover = 255;
                                        //#endregion
                                        #region even-odd change
                                        tempCover &= 511;
                                        if (tempCover >= 256)
                                        {
                                            tempCover = 512 - tempCover - 1;
                                        }
                                        #endregion

                                        // get current color data
                                        #region blend pixel
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;

                                        #region blend here
                                        dst = BufferData[startXPosition];
                                        colorData = builtColors[currentColorIndexScaled < 0 ?
                                                        0 : (currentColorIndexScaled > ColorIndexIncludeIncrementScale ? 255 :
                                                        (currentColorIndexScaled >> IncrementColorIndexShift))];

                                        dstRB = dst & 0x00FF00FF;
                                        dstG = (dst >> 8) & 0xFF;
                                        BufferData[startXPosition] =
                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (uint)((((((((colorData & 0x0000FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion


                                    }
                                    #endregion

                                    // alway increment color index
                                    currentColorIndexScaled += scaledIncrementColorIndex;

                                    // assign value for next loop
                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                        #region each row we need increase the value of color index
                        startOfRowIndex += rowColorIndexIncrementScaled;
                        #endregion
                    }
                    #endregion
                }
            }
            else
            {
                // blending include alpha of built color
                if (paint.Style != GradientStyle.Pad)
                {
                    // in case reflect and repeat, we don't care value that out of range
                    startOfRowIndex &= ColorIndexIncludeIncrementDoubleMask;
                    rowColorIndexIncrementScaled &= ColorIndexIncludeIncrementDoubleMask;
                    // when no need to blending, when draw a horizontal line
                    // do not need check the back color, alway setup
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;

                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                // calculate the first cell color index
                                #region second way to implement color index
                                currentColorIndexScaled = startOfRowIndex;
                                #endregion

                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1))
                                    {
                                        if (scLastCoverage != 0)
                                        {
                                            // fast bit absolute
                                            scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                            //#region non-zero checking code
                                            //if (scLastCoverage > 255) scLastCoverage = 255;
                                            //#endregion

                                            #region even-odd change
                                            scLastCoverage &= 511;
                                            if (scLastCoverage >= 256)
                                            {
                                                scLastCoverage = 512 - scLastCoverage - 1;
                                            }
                                            #endregion
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;

                                            while (startXPosition < lastXPosition)
                                            {
                                                dst = BufferData[startXPosition];
                                                colorData = builtColors[currentColorIndexScaled < 0 ?
                                                        (currentColorIndexScaled >> IncrementColorIndexShift) + 512 :
                                                        (currentColorIndexScaled >> IncrementColorIndexShift)];

                                                // get current color index value
                                                calculatedCoverage = (byte)(((colorData >> 24) * scLastCoverage) >> 8);

                                                dstRB = dst & 0x00FF00FF;
                                                dstG = (dst >> 8) & 0xFF;
                                                BufferData[startXPosition] =
                                                    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                    | (uint)((((((((colorData & 0x0000FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                    | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                startXPosition++;
                                                // increase the current color index
                                                currentColorIndexScaled += scaledIncrementColorIndex;
                                                currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
                                            }
                                            #endregion
                                        }
                                        else
                                        {
                                            // not filling but must set and increase the color index
                                            currentColorIndexScaled += (currentCellData.X - scLastX - 1) * scaledIncrementColorIndex;
                                            currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
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

                                        //#region non-zero checking code
                                        //if (tempCover > 255) tempCover = 255;
                                        //#endregion
                                        #region even-odd change
                                        tempCover &= 511;
                                        if (tempCover >= 256)
                                        {
                                            tempCover = 512 - tempCover - 1;
                                        }
                                        #endregion
                                        // get current color data
                                        #region blend pixel
                                        //tempCover = (int)((tempCover * colorAlpha) >> 8);
                                        ////if (tempCover > 255) tempCover = 255;
                                        //calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;

                                        #region blend here
                                        dst = BufferData[startXPosition];
                                        colorData = builtColors[currentColorIndexScaled < 0 ?
                                                (currentColorIndexScaled >> IncrementColorIndexShift) + 512 :
                                                (currentColorIndexScaled >> IncrementColorIndexShift)];
                                        calculatedCoverage = (byte)(((colorData >> 24) * tempCover) >> 8);
                                        dstRB = dst & 0x00FF00FF;
                                        dstG = (dst >> 8) & 0xFF;
                                        BufferData[startXPosition] =
                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (uint)((((((((colorData & 0x0000FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion


                                    }
                                    #endregion

                                    // alway increment color index
                                    currentColorIndexScaled += scaledIncrementColorIndex;
                                    currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;

                                    // assign value for next loop
                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                        #region each row we need increase the value of color index
                        startOfRowIndex += rowColorIndexIncrementScaled;
                        startOfRowIndex &= ColorIndexIncludeIncrementDoubleMask;
                        #endregion
                    }
                    #endregion
                }
                else // special case using for pad mode
                {
                    // when no need to blending, when draw a horizontal line
                    // do not need check the back color, alway setup
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;

                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                // calculate the first cell color index
                                #region second way to implement color index
                                currentColorIndexScaled = startOfRowIndex;
                                #endregion

                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1))
                                    {
                                        if (scLastCoverage != 0)
                                        {
                                            // fast bit absolute
                                            scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                            //#region non-zero checking code
                                            //if (scLastCoverage > 255) scLastCoverage = 255;
                                            //#endregion
                                            #region even-odd change
                                            scLastCoverage &= 511;
                                            if (scLastCoverage >= 256)
                                            {
                                                scLastCoverage = 512 - scLastCoverage - 1;
                                            }
                                            #endregion

                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value

                                            while (startXPosition < lastXPosition)
                                            {
                                                dst = BufferData[startXPosition];
                                                colorData = builtColors[
                                                    currentColorIndexScaled < 0 ?
                                                        0 :
                                                    (currentColorIndexScaled > ColorIndexIncludeIncrementScale ?
                                                        255 :
                                                        (currentColorIndexScaled >> IncrementColorIndexShift))];

                                                calculatedCoverage = (byte)(((colorData >> 24) * scLastCoverage) >> 8);

                                                dstRB = dst & 0x00FF00FF;
                                                dstG = (dst >> 8) & 0xFF;
                                                BufferData[startXPosition] =
                                                    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                    | (uint)((((((((colorData & 0x0000FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                    | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                startXPosition++;
                                                // increase the current color index
                                                currentColorIndexScaled += scaledIncrementColorIndex;
                                            }

                                            #endregion
                                        }
                                        else
                                        {
                                            // not filling but must set and increase the color index
                                            currentColorIndexScaled += (currentCellData.X - scLastX - 1) * scaledIncrementColorIndex;
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

                                        //#region non-zero checking code
                                        //if (tempCover > 255) tempCover = 255;
                                        //#endregion
                                        #region even-odd change
                                        tempCover &= 511;
                                        if (tempCover >= 256)
                                        {
                                            tempCover = 512 - tempCover - 1;
                                        }
                                        #endregion
                                        // get current color data
                                        #region blend pixel
                                        //calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;

                                        #region blend here
                                        dst = BufferData[startXPosition];
                                        colorData = builtColors[
                                            currentColorIndexScaled < 0 ?
                                                0 :
                                            (currentColorIndexScaled > ColorIndexIncludeIncrementScale ?
                                                255 :
                                                (currentColorIndexScaled >> IncrementColorIndexShift))];
                                        calculatedCoverage = (byte)(((colorData >> 24) * tempCover) >> 8);
                                        dstRB = dst & 0x00FF00FF;
                                        dstG = (dst >> 8) & 0xFF;
                                        BufferData[startXPosition] =
                                            (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (uint)((((((((colorData & 0x0000FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    // alway increment color index
                                    currentColorIndexScaled += scaledIncrementColorIndex;

                                    // assign value for next loop
                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                        #region each row we need increase the value of color index
                        startOfRowIndex += rowColorIndexIncrementScaled;
                        #endregion
                    }
                    #endregion
                }
            }

            #endregion
        }
        #endregion

        #endregion

        #region EVEN-ODD (gamma)

        #region On Filling EvenOdd (!transform, gamma)
        /// <summary>
        /// Fill to buffer base rows data information using even-odd rule
        /// </summary>
        /// <param name="paint">paint using for fill</param>
        /// <param name="rows">row data information</param>
        /// <param name="gammaLutRed">gamma look up table for red</param>
        /// <param name="gammaLutGreen">gamma look up table for green</param>
        /// <param name="gammaLutBlue">gamma look up table for blue</param>
        protected override void OnFillingEvenOdd(
            PaintMaterial paint,
            RowData[] rows,
            int startRowIndex,
            int endRowIndex,
            byte[] gammaLutRed,
            byte[] gammaLutGreen,
            byte[] gammaLutBlue)
        {
            if (!(paint.Paint is LinearGradient))
            {
                NotMatchPaintTypeException.Publish(typeof(LinearGradient), paint.Paint.GetType());
                return;
            }
            LinearGradient linearGradient = paint.Paint as LinearGradient;

            switch (linearGradient.Mode)
            {
                case LinearGradientMode.Horizontal:
                    OnFillingHorizontalEvenOdd(linearGradient, paint.ScaledOpacity, rows, startRowIndex, endRowIndex, gammaLutRed, gammaLutGreen, gammaLutBlue);
                    break;
                case LinearGradientMode.Vertical:
                    OnFillingVerticalEvenOdd(linearGradient, paint.ScaledOpacity, rows, startRowIndex, endRowIndex, gammaLutRed, gammaLutGreen, gammaLutBlue);
                    break;
                case LinearGradientMode.ForwardDiagonal:
                    OnFillingDiagonalEvenOdd(linearGradient, paint.ScaledOpacity, rows, startRowIndex, endRowIndex, true, gammaLutRed, gammaLutGreen, gammaLutBlue);
                    break;
                case LinearGradientMode.BackwardDiagonal:
                    OnFillingDiagonalEvenOdd(linearGradient, paint.ScaledOpacity, rows, startRowIndex, endRowIndex, false, gammaLutRed, gammaLutGreen, gammaLutBlue);
                    break;
            }
        }
        #endregion

        #region On Filling Horizontal EvenOdd (!transform, gamma)
        /// <summary>
        /// Fill to buffer base rows data information using non-zero rule
        /// </summary>
        /// <param name="paint">linear gradient object</param>
        /// <param name="rows">row data information</param>
        /// <param name="startRowIndex">start row index in row array need to draw</param>
        /// <param name="endRowIndex">end row index in end row array need to draw</param>
        /// <param name="gammaLutRed">gamma look up table for red</param>
        /// <param name="gammaLutGreen">gamma look up table for green</param>
        /// <param name="gammaLutBlue">gamma look up table for blue</param>
        void OnFillingHorizontalEvenOdd(
            LinearGradient paint,
            uint opacity,
            RowData[] rows,
            int startRowIndex,
            int endRowIndex,
            byte[] gammaLutRed,
            byte[] gammaLutGreen,
            byte[] gammaLutBlue)
        {
            // now not need to check null or not
            uint[] builtColors = paint.GetLinearColors(opacity);

            /*Base on startX,endX, we need build fixedColor array
             * contain width count elements. So that, at a column, we
             * can lookup color for that column.
             */

            #region build fixed color
            double startX = paint.StartX;
            double endX = paint.EndX;

            // width of this
            int width = CurrentEndXIndex - CurrentStartXIndex + 1;
            uint[] fixedColor = new uint[width];
            int distanceScaled = (int)(Math.Abs(startX - endX) * DistanceScale);
            if (distanceScaled == 0)
            {
                FillingException.Publish(typeof(LinearGradient), "Start point and end point are too close");
                return;
            }
            #region building fixed color array
            if (paint.Style == GradientStyle.Pad)
            {
                // when pad is supported
                #region when mode are repeat or reflect
                int startXScaled = (int)(startX * DistanceScale);
                int startFixedIndex = (((
                    (((width + CurrentStartXIndex) << DistanceShift) - startXScaled)
                    << ColorIndexShift) / distanceScaled)) << IncrementColorIndexShift;

                int colorIncrement = (DistanceScale * ColorIndexIncludeIncrementScale) / distanceScaled;
                if (endX < startX)
                {
                    colorIncrement = -colorIncrement;
                    startFixedIndex = -startFixedIndex;
                }
                while (width-- > 0)
                {
                    fixedColor[width] =
                        builtColors[startFixedIndex < 0 ?
                            0 :
                        (startFixedIndex > ColorIndexIncludeIncrementScale ?
                            255 :
                            (startFixedIndex >> IncrementColorIndexShift))];
                    startFixedIndex -= colorIncrement;
                }
                #endregion
            }
            else
            {
                #region when mode are repeat or reflect
                int startXScaled = (int)(startX * DistanceScale);
                int startFixedIndex = (((
                    (((width + CurrentStartXIndex) << DistanceShift) - startXScaled)
                    << ColorIndexShift) / distanceScaled)) << IncrementColorIndexShift;
                int colorIncrement = (DistanceScale * ColorIndexIncludeIncrementScale) / distanceScaled;
                if (endX < startX)
                {
                    colorIncrement = -colorIncrement;
                }
                startFixedIndex &= ColorIndexIncludeIncrementDoubleMask;
                while (width-- > 0)
                {
                    fixedColor[width] = builtColors[
                        startFixedIndex < 0 ?
                            (startFixedIndex >> IncrementColorIndexShift) + 512 :
                            (startFixedIndex >> IncrementColorIndexShift)];
                    startFixedIndex -= colorIncrement;
                    startFixedIndex &= ColorIndexIncludeIncrementDoubleMask;
                }
                #endregion
            }
            #endregion

            #endregion

            #region private variable for filling
            int currentCoverage, scLastCoverage, scLastX = 0;
            int tempCover = 0;
            int currentArea = 0;
            int lastXPosition = 0;
            int startXPosition = 0;
            byte calculatedCoverage = 0;

            int currentColorIndexValue = 0;
            CellData currentCellData = null;
            uint colorData = 0;
            uint dst, dstRB, dstG;
            #endregion

            #region FILLING
            if (paint.Ramp.NoBlendingColor)
            {
                #region filling without blending
                startRowIndex--;
                while (++startRowIndex <= endRowIndex)
                {
                    currentCoverage = scLastCoverage = scLastX = 0;

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
                                    #region even-odd change
                                    scLastCoverage &= 511;
                                    if (scLastCoverage > 256)
                                    {
                                        scLastCoverage = 512 - scLastCoverage;
                                    }
                                    #endregion

                                    if (scLastCoverage != 0)
                                    {
                                        #region BLEND HORIZONTAL LINE
                                        // calculate start and end position
                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                        lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        // get current color index value
                                        currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                        if (scLastCoverage >= 255)
                                        {
                                            while (startXPosition < lastXPosition)
                                            {
                                                //colorData = fixedColor[currentColorIndexValue];
                                                //// just set
                                                //BufferData[startXPosition] = colorData;
                                                //startXPosition++;
                                                //currentColorIndexValue++;

                                                BufferData[startXPosition++] = fixedColor[currentColorIndexValue++];
                                            }
                                        }
                                        else
                                        {
                                            while (startXPosition < lastXPosition)
                                            {
                                                colorData = fixedColor[currentColorIndexValue];
                                                calculatedCoverage = (byte)((colorData >> 24) & 0xFF);
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                if (calculatedCoverage >= 255)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    #region apply gamma
                                                    dst = BufferData[startXPosition];
                                                    dstG = (dst >> 8) & 0xFF;
                                                    //dstRB = (dst & 0x00FF00FF);
                                                    dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                    BufferData[startXPosition] =
                                                        (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                        | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                        | ((uint)gammaLutBlue[(dstRB & 0x00FF)]));
                                                    #endregion
                                                }
                                                startXPosition++;
                                                currentColorIndexValue++;
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
                                // fast bit absolute
                                tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                #region even-odd change
                                tempCover &= 511;
                                if (tempCover > 256)
                                {
                                    tempCover = 512 - tempCover;
                                }
                                #endregion

                                if (tempCover != 0)
                                {
                                    // get current color data
                                    colorData = fixedColor[currentCellData.X - CurrentStartXIndex];
                                    calculatedCoverage = (byte)(colorData >> 24);

                                    #region blend pixel
                                    tempCover = (int)((tempCover * calculatedCoverage) >> 8);
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

                                    #region apply gamma
                                    dst = BufferData[startXPosition];
                                    dstG = (dst >> 8) & 0xFF;
                                    //dstRB = (dst & 0x00FF00FF);
                                    dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                    BufferData[startXPosition] =
                                        (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                        | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                        | ((uint)gammaLutBlue[(dstRB & 0x00FF)]));
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
                #region filling without blending
                startRowIndex--;
                while (++startRowIndex <= endRowIndex)
                {
                    currentCoverage = scLastCoverage = scLastX = 0;

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
                                    #region even-odd change
                                    scLastCoverage &= 511;
                                    if (scLastCoverage > 256)
                                    {
                                        scLastCoverage = 512 - scLastCoverage;
                                    }
                                    #endregion

                                    if (scLastCoverage != 0)
                                    {
                                        #region BLEND HORIZONTAL LINE
                                        // calculate start and end position
                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                        lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        // get current color index value
                                        currentColorIndexValue = scLastX + 1 - CurrentStartXIndex;
                                        while (startXPosition < lastXPosition)
                                        {
                                            colorData = fixedColor[currentColorIndexValue];
                                            calculatedCoverage = (byte)((colorData >> 24) & 0xFF);
                                            calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                            if (calculatedCoverage >= 255)
                                            {
                                                BufferData[startXPosition] = colorData;
                                            }
                                            else
                                            {
                                                //// blend here
                                                //dst = BufferData[startXPosition];
                                                //dstRB = dst & 0x00FF00FF;
                                                //dstG = (dst >> 8) & 0xFF;

                                                //BufferData[startXPosition++] =
                                                //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + currentColorAlpha] << 24)
                                                //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * currentColorAlpha) >> 8) + dstG) << 8) & 0x0000FF00)
                                                //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * currentColorAlpha) >> 8) + dstRB) & 0x00FF00FF);

                                                #region apply gamma
                                                dst = BufferData[startXPosition];
                                                dstG = (dst >> 8) & 0xFF;
                                                //dstRB = (dst & 0x00FF00FF);
                                                dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                BufferData[startXPosition] =
                                                    (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                    | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                    | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                    | ((uint)gammaLutBlue[(dstRB & 0x00FF)]));
                                                #endregion
                                            }
                                            startXPosition++;
                                            currentColorIndexValue++;
                                        }
                                        #endregion
                                    }
                                }
                                #endregion

                                currentCoverage += currentCellData.Coverage;

                                #region blend the current cell
                                // fast absolute
                                tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                // fast bit absolute
                                tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                #region even-odd change
                                tempCover &= 511;
                                if (tempCover > 256)
                                {
                                    tempCover = 512 - tempCover;
                                }
                                #endregion

                                if (tempCover != 0)
                                {
                                    // get current color data
                                    colorData = fixedColor[currentCellData.X - CurrentStartXIndex];
                                    calculatedCoverage = (byte)(colorData >> 24);

                                    #region blend pixel
                                    tempCover = (int)((tempCover * calculatedCoverage) >> 8);
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

                                    #region apply gamma
                                    dst = BufferData[startXPosition];
                                    dstG = (dst >> 8) & 0xFF;
                                    //dstRB = (dst & 0x00FF00FF);
                                    dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                    BufferData[startXPosition] =
                                        (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                        | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                        | ((uint)gammaLutBlue[(dstRB & 0x00FF)]));
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
            #endregion
        }
        #endregion

        #region On Filling Vertical EvenOdd (!transform, gamma)
        /// <summary>
        /// Fill to buffer base rows data information using non-zero rule
        /// </summary>
        /// <param name="paint">linear gradient object</param>
        /// <param name="rows">row data information</param>
        /// <param name="startRowIndex">start row index in row array need to draw</param>
        /// <param name="endRowIndex">end row index in end row array need to draw</param>
        /// <param name="gammaLutRed">gamma look up table for red</param>
        /// <param name="gammaLutGreen">gamma look up table for green</param>
        /// <param name="gammaLutBlue">gamma look up table for blue</param>
        void OnFillingVerticalEvenOdd(
            LinearGradient paint,
            uint opacity,
            RowData[] rows,
            int startRowIndex,
            int endRowIndex,
            byte[] gammaLutRed,
            byte[] gammaLutGreen,
            byte[] gammaLutBlue)
        {
            // now not need to check null or not
            uint[] builtColors = paint.GetLinearColors(opacity);

            /*Base on startX,endX, we need build fixedColor array
             * contain width count elements. So that, at a column, we
             * can lookup color for that column.
             */

            #region build fixed color
            double startY = paint.StartY;
            double endY = paint.EndY;

            // width of this
            int height = endRowIndex - startRowIndex + 1;
            uint[] fixedColor = new uint[height];
            int distanceScaled = (int)(Math.Abs(startY - endY) * DistanceScale);
            if (distanceScaled == 0)
            {
                FillingException.Publish(typeof(LinearGradient), "Start point and end point are too close");
                return;
            }
            #region building fixed color array
            if (paint.Style == GradientStyle.Pad)
            {
                #region GradientStyle.Pad
                int startFixedIndex = (((
                        (((height + startRowIndex) << DistanceShift) - (int)(startY * DistanceScale))
                        << ColorIndexShift) / distanceScaled)) << IncrementColorIndexShift;

                int colorIncrement = (DistanceScale * ColorIndexIncludeIncrementScale) / distanceScaled;
                if (endY < startY)
                {
                    colorIncrement = -colorIncrement;
                    startFixedIndex = -startFixedIndex;
                }
                while (height-- > 0)
                {
                    fixedColor[height] =
                        builtColors[startFixedIndex < 0 ?
                            0 :
                        (startFixedIndex > ColorIndexIncludeIncrementScale ?
                            255 :
                            (startFixedIndex >> IncrementColorIndexShift))];
                    startFixedIndex -= colorIncrement;
                }
                #endregion
            }
            else
            {
                #region GradientStyle.Repeat || GradientStyle.Reflect
                int startFixedIndex = (((
                        (((height + startRowIndex) << DistanceShift) - (int)(startY * DistanceScale))
                        << ColorIndexShift) / distanceScaled)) << IncrementColorIndexShift;
                int colorIncrement = (DistanceScale * ColorIndexIncludeIncrementScale) / distanceScaled;
                if (endY < startY)
                {
                    colorIncrement = -colorIncrement;
                }
                startFixedIndex &= ColorIndexIncludeIncrementDoubleMask;
                while (height-- > 0)
                {
                    fixedColor[height] = builtColors[
                        startFixedIndex < 0 ?
                            (startFixedIndex >> IncrementColorIndexShift) + 512 :
                            (startFixedIndex >> IncrementColorIndexShift)];
                    startFixedIndex -= colorIncrement;
                    startFixedIndex &= ColorIndexIncludeIncrementDoubleMask;
                }
                #endregion
            }
            #endregion

            #endregion

            #region private variable for filling
            int currentCoverage, scLastCoverage, scLastX = 0;
            int tempCover = 0;
            int currentArea = 0;
            int lastXPosition = 0;
            int startXPosition = 0;
            byte calculatedCoverage = 0;

            int currentColorIndexValue = 0;

            CellData currentCellData = null;
            uint colorData = 0;
            uint colorAlpha = 0;
            uint colorG = 0;
            uint colorRB = 0;

            uint dst, dstRB, dstG;
            #endregion

            #region FILLING
            if (paint.Ramp.NoBlendingColor)
            {
                // when no need to blending, when draw a horizontal line
                // do not need check the back color, alway setup
                #region filling without blend for horizontal lines
                startRowIndex--;
                while (++startRowIndex <= endRowIndex)
                {
                    currentCoverage = scLastCoverage = scLastX = 0;

                    if (rows[startRowIndex] != null)
                    {
                        #region calculate and get current color
                        colorData = fixedColor[currentColorIndexValue];
                        colorAlpha = (colorData >> 24);
                        colorG = (colorData & 0x0000FF00) >> 8;
                        colorRB = (colorData & 0x00FF00FF);
                        #endregion
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
                                    //#region non-zero checking code
                                    //if (scLastCoverage > 255) scLastCoverage = 255;
                                    //#endregion
                                    #region even-odd change
                                    scLastCoverage &= 511;
                                    if (scLastCoverage > 256)
                                    {
                                        scLastCoverage = 512 - scLastCoverage;
                                    }
                                    #endregion

                                    #region BLEND HORIZONTAL LINE
                                    // calculate start and end position
                                    startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                    lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                    // get current color index value
                                    if (scLastCoverage >= 254)
                                    {
                                        while (startXPosition < lastXPosition)
                                        {
                                            BufferData[startXPosition++] = colorData;
                                        }
                                    }
                                    else
                                    {
                                        while (startXPosition < lastXPosition)
                                        {
                                            calculatedCoverage = (byte)((scLastCoverage * colorAlpha) >> 8);
                                            if (calculatedCoverage >= 254)
                                            {
                                                BufferData[startXPosition] = colorData;
                                            }
                                            else
                                            {
                                                #region blend here
                                                //dst = BufferData[startXPosition];
                                                //dstRB = dst & 0x00FF00FF;
                                                //dstG = (dst >> 8) & 0xFF;

                                                //BufferData[startXPosition] =
                                                //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                //    | (uint)((((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                //    | (uint)(((((colorRB - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                                #region gamma apply
                                                dst = BufferData[startXPosition];
                                                dstG = (dst >> 8) & 0xFF;
                                                dstRB = ((((colorRB - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                BufferData[startXPosition] =
                                                    (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                    | (((uint)gammaLutGreen[(((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                    | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                    | (gammaLutBlue[(dstRB & 0x00FF)]));
                                                #endregion
                                                #endregion
                                            }
                                            startXPosition++;
                                        }
                                    }
                                    #endregion
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

                                    //#region non-zero checking code
                                    //if (tempCover > 255) tempCover = 255;
                                    //#endregion
                                    #region even-odd change
                                    tempCover &= 511;
                                    if (tempCover > 256)
                                    {
                                        tempCover = 512 - tempCover;
                                    }
                                    #endregion
                                    // get current color data
                                    #region blend pixel
                                    tempCover = (int)((tempCover * colorAlpha) >> 8);
                                    //if (tempCover > 255) tempCover = 255;
                                    calculatedCoverage = (byte)tempCover;

                                    startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;

                                    #region blend here
                                    //dst = BufferData[startXPosition];
                                    //dstRB = dst & 0x00FF00FF;
                                    //dstG = (dst >> 8) & 0xFF;
                                    //BufferData[startXPosition] =
                                    //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                    //    | (uint)((((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                    //    | (uint)(((((colorRB - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                    #region gamma apply
                                    dst = BufferData[startXPosition];
                                    dstG = (dst >> 8) & 0xFF;
                                    dstRB = ((((colorRB - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                    BufferData[startXPosition] =
                                        (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        | (((uint)gammaLutGreen[(((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                        | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                        | (gammaLutBlue[(dstRB & 0x00FF)]));
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
                    // increase color index
                    currentColorIndexValue++;
                }
                #endregion
            }
            else
            {
                #region perform normal filling
                startRowIndex--;
                while (++startRowIndex <= endRowIndex)
                {
                    currentCoverage = scLastCoverage = scLastX = 0;

                    if (rows[startRowIndex] != null)
                    {
                        #region calculate and get current color
                        colorData = fixedColor[currentColorIndexValue];
                        colorAlpha = (colorData >> 24);
                        colorG = (colorData & 0x0000FF00) >> 8;
                        colorRB = (colorData & 0x00FF00FF);
                        #endregion
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
                                    #region even-odd change
                                    scLastCoverage &= 511;
                                    if (scLastCoverage > 256)
                                    {
                                        scLastCoverage = 512 - scLastCoverage;
                                    }
                                    #endregion

                                    #region BLEND HORIZONTAL LINE
                                    // calculate start and end position
                                    startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                    lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;

                                    while (startXPosition < lastXPosition)
                                    {
                                        calculatedCoverage = (byte)((scLastCoverage * colorAlpha) >> 8);
                                        if (calculatedCoverage >= 254)
                                        {
                                            BufferData[startXPosition] = colorData;
                                        }
                                        else
                                        {
                                            #region blend here
                                            //dst = BufferData[startXPosition];
                                            //dstRB = dst & 0x00FF00FF;
                                            //dstG = (dst >> 8) & 0xFF;

                                            //BufferData[startXPosition] =
                                            //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            //    | (uint)((((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            //    | (uint)(((((colorRB - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                            #region gamma apply
                                            dst = BufferData[startXPosition];
                                            dstG = (dst >> 8) & 0xFF;
                                            dstRB = ((((colorRB - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                            BufferData[startXPosition] =
                                                (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                | (((uint)gammaLutGreen[(((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                | (gammaLutBlue[(dstRB & 0x00FF)]));
                                            #endregion
                                            #endregion
                                        }
                                        startXPosition++;
                                    }
                                    #endregion
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

                                    #region even-odd change
                                    tempCover &= 511;
                                    if (tempCover > 256)
                                    {
                                        tempCover = 512 - tempCover;
                                    }
                                    #endregion

                                    #region blend pixel
                                    tempCover = (int)((tempCover * colorAlpha) >> 8);
                                    //if (tempCover > 255) tempCover = 255;
                                    calculatedCoverage = (byte)tempCover;

                                    startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                    #region blend here
                                    //dst = BufferData[startXPosition];
                                    //dstRB = dst & 0x00FF00FF;
                                    //dstG = (dst >> 8) & 0xFF;
                                    //BufferData[startXPosition] =
                                    //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                    //    | (uint)((((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                    //    | (uint)(((((colorRB - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                    #region gamma apply
                                    dst = BufferData[startXPosition];
                                    dstG = (dst >> 8) & 0xFF;
                                    dstRB = ((((colorRB - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                    BufferData[startXPosition] =
                                        (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        | (((uint)gammaLutGreen[(((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                        | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                        | (gammaLutBlue[(dstRB & 0x00FF)]));
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

                    // increase color index
                    currentColorIndexValue++;
                }
                #endregion
            }

            #endregion
        }
        #endregion

        #region On Filling Diangonal EvenOdd (!transform, gamma)
        /// <summary>
        /// Fill to buffer base rows data information using non-zero rule
        /// </summary>
        /// <param name="paint">linear gradient object</param>
        /// <param name="rows">row data information</param>
        /// <param name="startRowIndex">start row index in row array need to draw</param>
        /// <param name="endRowIndex">end row index in end row array need to draw</param>
        /// <param name="isForward">is diagonal gradient is forward</param>
        /// <param name="gammaLutRed">gamma look up table for red</param>
        /// <param name="gammaLutGreen">gamma look up table for green</param>
        /// <param name="gammaLutBlue">gamma look up table for blue</param>
        void OnFillingDiagonalEvenOdd(
            LinearGradient paint,
            uint opacity,
            RowData[] rows,
            int startRowIndex,
            int endRowIndex,
            bool isForward,
            byte[] gammaLutRed,
            byte[] gammaLutGreen,
            byte[] gammaLutBlue)
        {

            #region Explain for fomula

            /*
             * CALCULATION NEED FOLLOWING VALUES
             * 1/ INCREMENT
             * increment, when x from n to n+1 , 
             * index of color will increase from
             * f(n) to f(n) + increment
             * 
             * this increment value is calculated by
             * Linear from A to B
             * A              C      B'
             * *  *  *  *   *  *  *
             *    *         *    *
             *       *      *   *
             *          *   *  *
             *              * B
             * AC = w of the rect
             * BB' |_ AB
             * So AB' = (AB * AB)/AC = d * d / w
             * And increment is increment = 256 / AB'
             * it mean when x go from A to B'
             * color index will increase from 0=>255 ( 256 steps)
             * 
             * 
             * 2/ DISTANCE
             *              (x3,y3)
             *                *                  
             *               *
             *              *
             *             *
             *            *
             *           *
             *          *
             *  (x1,y1)*
             *               *
             *                     *
             *                           *
             *                         (x2,y2)
             *                    
             * x3,y3 can be calculated by following fomula
             *      x3 = x1 - height of paint = x1 - ( y2- y1);
             *      y3 = y1 + width of paint = y1 + ( x2 - x1);
             *      
             * to determine color at point(x,y) to line (x1,y1)-(x3,y3)
             * from this distance we can determine the color at this 
             * point by lookup to color array
             * 
             * distance = ((x - x3) * (y3-y1)
             *            - ( y - y3) * (x3 -x1))/(distance from start and end point of paint);
             */
            #endregion

            #region Pre-process
            double x1 = 0;
            double y1 = 0;
            double x2 = 0;
            double y2 = 0;
            if (isForward)
            {
                x1 = paint.StartX;
                y1 = paint.StartY;
                x2 = paint.EndX;
                y2 = paint.EndY;
            }
            else
            {
                x1 = paint.EndX;
                y1 = paint.StartY;

                x2 = paint.StartX;
                y2 = paint.EndY;
            }

            double widthOfPaint = x2 - x1;
            double heightOfPaint = y2 - y1;
            //note: start and end point is random
            // start not always on top-left
            // so width of paint and height of paint may be negative
            if (widthOfPaint == 0)
            {
                // this will change to vertical
                OnFillingVerticalNonZero(paint, opacity, rows, startRowIndex, endRowIndex);
                return;
            }
            else if (heightOfPaint == 0)
            {
                // this will change to horizontal
                OnFillingHorizontalNonZero(paint, opacity, rows, startRowIndex, endRowIndex);
                return;
            }
            #endregion

            #region calculate the increasement

            double x3 = x1 - heightOfPaint;
            double y3 = y1 + widthOfPaint;

            double lengthOfPaint = Math.Sqrt((widthOfPaint * widthOfPaint) + (heightOfPaint * heightOfPaint));
            //int distanceOfPaintScaled = (int)(distanceOfPaint * DistanceScale);
            double incrementColorIndex = (double)(widthOfPaint * ColorIndexScale) / (lengthOfPaint * lengthOfPaint);

            // increment by distance scale
            // increment may be greater than 512, but in reflect,repeat mode, 
            // just modulo it
            // get the remain when divide by 512
            // incrementColorIndex = incrementColorIndex - (((int)incrementColorIndex / ColorIndexDoubleScale) * ColorIndexDoubleScale); 

            //incrementX < 512, calculate incrementIndex  
            // ( that scale by 256 for approxiate calculation )
            int scaledIncrementColorIndex = (int)(incrementColorIndex * IncrementColorIndexScale);
            #endregion

            // now not need to check null or not
            uint[] builtColors = paint.GetLinearColors(opacity);

            #region private variable for filling
            int currentCoverage, scLastCoverage, scLastX = 0;
            int tempCover = 0;
            int currentArea = 0;
            int lastXPosition = 0;
            int startXPosition = 0;
            byte calculatedCoverage = 0;

            // this color index is scaled
            int currentColorIndexScaled = 0;

            CellData currentCellData = null;
            uint colorData = 0;
            uint dst, dstRB, dstG;
            double firstPointDistance = 0;
            #endregion

            #region optimization for color index
            // the ORIGIN fomula for each row, we need to calculate this
            //firstPointDistance = (((x3) * (y3 - y1) - (startRowIndex - y3) * (x3 - x1)) / distanceOfPaint);
            //// color index = (distance from point to line => scaled) * 256/ (distance of paint scaled)
            //currentColorIndexScaled =
            //    (int)((firstPointDistance * ColorIndexIncludeIncrementScale / distanceOfPaint));
            //    currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask; // mod ( 512 << 8)


            // now we need calculate for first time only and after a row, we need to add and small value
            //firstPointDistance  is x value when line cut the horizontal at position startRowIndex
            //firstPointDistance = (((x3) * (y3 - y1) - (startRowIndex - y3) * (x3 - x1)) /(lengthOfPaint));
            // y = slope * x + beta
            //=> slope * x - y + beta = 0
            double slope = (y3 - y1) / (x3 - x1);
            double beta = (y3 - slope * x3);
            // fomula to calculate distance from point to line a*x + b*y + c= 0
            // is d = (a*x1 + b*y1 + c) / sqrt(a*a + b*b)
            // in this case d = (slope * x1 + (-1) * y1 + beta) / sqrt ( slope * slope + (-1) * (-1))
            //firstPointDistance = (-startRowIndex + beta) / Math.Sqrt(slope * slope + 1);


            //http://mathworld.wolfram.com/Point-LineDistance2-Dimensional.html
            firstPointDistance = ((x3 - x1) * (y1 - startRowIndex) - (x1 - 0) * (y3 - y1))
                / lengthOfPaint;

            int startOfRowIndex = (int)((firstPointDistance * ColorIndexIncludeIncrementScale / lengthOfPaint));
            int rowColorIndexIncrementScaled = (int)(((-(x3 - x1) / lengthOfPaint) * ColorIndexIncludeIncrementScale / lengthOfPaint));

            #endregion

            #region FILLING
            if (paint.Ramp.NoBlendingColor)
            {
                if (paint.Style != GradientStyle.Pad)
                {
                    // in case reflect and repeat, we don't care value that out of range
                    startOfRowIndex &= ColorIndexIncludeIncrementDoubleMask;
                    rowColorIndexIncrementScaled &= ColorIndexIncludeIncrementDoubleMask;
                    scaledIncrementColorIndex &= ColorIndexIncludeIncrementDoubleMask;

                    // when no need to blending, when draw a horizontal line
                    // do not need check the back color, alway setup
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;

                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                // calculate the first cell color index
                                #region second way to implement color index
                                currentColorIndexScaled = startOfRowIndex;
                                #endregion

                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1))
                                    {
                                        if (scLastCoverage != 0)
                                        {
                                            // fast bit absolute
                                            scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                            //#region non-zero checking code
                                            //if (scLastCoverage > 255) scLastCoverage = 255;
                                            //#endregion

                                            #region even-odd change
                                            scLastCoverage &= 511;
                                            if (scLastCoverage >= 256)
                                            {
                                                scLastCoverage = 512 - scLastCoverage - 1;
                                            }
                                            #endregion
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            if (scLastCoverage >= 254)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    BufferData[startXPosition] = builtColors[currentColorIndexScaled < 0 ?
                                                        (currentColorIndexScaled >> IncrementColorIndexShift) + 512 :
                                                        (currentColorIndexScaled >> IncrementColorIndexShift)];
                                                    startXPosition++;
                                                    // increase current color index
                                                    currentColorIndexScaled += scaledIncrementColorIndex;
                                                    currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
                                                }
                                            }
                                            else
                                            {
                                                calculatedCoverage = (byte)scLastCoverage;
                                                while (startXPosition < lastXPosition)
                                                {
                                                    colorData = builtColors[currentColorIndexScaled < 0 ?
                                                            (currentColorIndexScaled >> IncrementColorIndexShift) + 512 :
                                                            (currentColorIndexScaled >> IncrementColorIndexShift)];
                                                    #region blending
                                                    #region gamma apply
                                                    dst = BufferData[startXPosition];
                                                    dstG = (dst >> 8) & 0xFF;
                                                    dstRB = (((((colorData & 0x00FF00FF) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                    BufferData[startXPosition] =
                                                        (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                        | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                        | (gammaLutBlue[(dstRB & 0x00FF)]));
                                                    #endregion
                                                    //dst = BufferData[startXPosition];
                                                    //dstRB = dst & 0x00FF00FF;
                                                    //dstG = (dst >> 8) & 0xFF;
                                                    //BufferData[startXPosition] =
                                                    //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                    //    | (uint)((((((((colorData & 0x0000FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                    //    | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                    #endregion
                                                    startXPosition++;
                                                    // increase the current color index
                                                    currentColorIndexScaled += scaledIncrementColorIndex;
                                                    currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
                                                }
                                            }
                                            #endregion
                                        }
                                        else
                                        {
                                            // not filling but must set and increase the color index
                                            currentColorIndexScaled += (currentCellData.X - scLastX - 1) * scaledIncrementColorIndex;
                                            currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
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

                                        //#region non-zero checking code
                                        //if (tempCover > 255) tempCover = 255;
                                        //#endregion
                                        #region even-odd change
                                        tempCover &= 511;
                                        if (tempCover >= 256)
                                        {
                                            tempCover = 512 - tempCover - 1;
                                        }
                                        #endregion

                                        // get current color data
                                        #region blend pixel
                                        //tempCover = (int)((tempCover * colorAlpha) >> 8);
                                        ////if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        if (calculatedCoverage >= 254)
                                        {
                                            BufferData[startXPosition] = builtColors[currentColorIndexScaled < 0 ?
                                                    (currentColorIndexScaled >> IncrementColorIndexShift) + 512 :
                                                    (currentColorIndexScaled >> IncrementColorIndexShift)];

                                        }
                                        else
                                        {
                                            #region blend here
                                            colorData = builtColors[currentColorIndexScaled < 0 ?
                                                    (currentColorIndexScaled >> IncrementColorIndexShift) + 512 :
                                                    (currentColorIndexScaled >> IncrementColorIndexShift)];
                                            //dst = BufferData[startXPosition];
                                            //dstRB = dst & 0x00FF00FF;
                                            //dstG = (dst >> 8) & 0xFF;
                                            //BufferData[startXPosition] =
                                            //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            //    | (uint)((((((((colorData & 0x0000FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            //    | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                            #region apply gamma
                                            dst = BufferData[startXPosition];
                                            dstG = (dst >> 8) & 0xFF;
                                            dstRB = (((((colorData & 0x00FF00FF) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                            BufferData[startXPosition] =
                                                (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                    | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                    | (gammaLutBlue[(dstRB & 0x00FF)]));
                                            #endregion
                                            #endregion
                                        }
                                        #endregion


                                    }
                                    #endregion

                                    // alway increment color index
                                    currentColorIndexScaled += scaledIncrementColorIndex;
                                    currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;

                                    // assign value for next loop
                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }

                        #region each row we need increase the value of color index
                        startOfRowIndex += rowColorIndexIncrementScaled;
                        startOfRowIndex &= ColorIndexIncludeIncrementDoubleMask;
                        #endregion
                    }
                    #endregion
                }
                else // special case using for pad mode
                {
                    // when no need to blending, when draw a horizontal line
                    // do not need check the back color, alway setup
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;

                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                // calculate the first cell color index
                                #region second way to implement color index
                                currentColorIndexScaled = startOfRowIndex;
                                #endregion

                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1))
                                    {
                                        if (scLastCoverage != 0)
                                        {
                                            // fast bit absolute
                                            scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                            //#region non-zero checking code
                                            //if (scLastCoverage > 255) scLastCoverage = 255;
                                            //#endregion
                                            #region even-odd change
                                            scLastCoverage &= 511;
                                            if (scLastCoverage >= 256)
                                            {
                                                scLastCoverage = 512 - scLastCoverage - 1;
                                            }
                                            #endregion
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            if (scLastCoverage >= 254)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    BufferData[startXPosition] = builtColors[currentColorIndexScaled < 0 ?
                                                        0 : (currentColorIndexScaled > ColorIndexIncludeIncrementScale ? 255 :
                                                        (currentColorIndexScaled >> IncrementColorIndexShift))];
                                                    startXPosition++;
                                                    // increase current color index
                                                    currentColorIndexScaled += scaledIncrementColorIndex;
                                                    //currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
                                                }
                                            }
                                            else
                                            {
                                                calculatedCoverage = (byte)scLastCoverage;
                                                while (startXPosition < lastXPosition)
                                                {
                                                    colorData = builtColors[currentColorIndexScaled < 0 ?
                                                        0 : (currentColorIndexScaled > ColorIndexIncludeIncrementScale ? 255 :
                                                        (currentColorIndexScaled >> IncrementColorIndexShift))];

                                                    //dst = BufferData[startXPosition];
                                                    //dstRB = dst & 0x00FF00FF;
                                                    //dstG = (dst >> 8) & 0xFF;
                                                    //BufferData[startXPosition] =
                                                    //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                    //    | (uint)((((((((colorData & 0x0000FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                    //    | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                                    #region apply gamma
                                                    dst = BufferData[startXPosition];
                                                    dstG = (dst >> 8) & 0xFF;
                                                    dstRB = (((((colorData & 0x00FF00FF) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                    BufferData[startXPosition] =
                                                        (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                        | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                        | (gammaLutBlue[(dstRB & 0x00FF)]));
                                                    #endregion


                                                    startXPosition++;
                                                    // increase the current color index
                                                    currentColorIndexScaled += scaledIncrementColorIndex;
                                                    //currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
                                                }
                                            }
                                            #endregion
                                        }
                                        else
                                        {
                                            // not filling but must set and increase the color index
                                            currentColorIndexScaled += (currentCellData.X - scLastX - 1) * scaledIncrementColorIndex;
                                            //currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
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

                                        //#region non-zero checking code
                                        //if (tempCover > 255) tempCover = 255;
                                        //#endregion
                                        #region even-odd change
                                        tempCover &= 511;
                                        if (tempCover >= 256)
                                        {
                                            tempCover = 512 - tempCover - 1;
                                        }
                                        #endregion

                                        // get current color data
                                        #region blend pixel
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;

                                        #region blend here
                                        colorData = builtColors[currentColorIndexScaled < 0 ?
                                                        0 : (currentColorIndexScaled > ColorIndexIncludeIncrementScale ? 255 :
                                                        (currentColorIndexScaled >> IncrementColorIndexShift))];

                                        //dst = BufferData[startXPosition];
                                        //colorData = builtColors[currentColorIndexScaled < 0 ?
                                        //                0 : (currentColorIndexScaled > ColorIndexIncludeIncrementScale ? 255 :
                                        //                (currentColorIndexScaled >> IncrementColorIndexShift))];

                                        //dstRB = dst & 0x00FF00FF;
                                        //dstG = (dst >> 8) & 0xFF;
                                        //BufferData[startXPosition] =
                                        //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        //    | (uint)((((((((colorData & 0x0000FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        //    | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                        #region apply gamma
                                        dst = BufferData[startXPosition];
                                        dstG = (dst >> 8) & 0xFF;
                                        dstRB = (((((colorData & 0x00FF00FF) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                        BufferData[startXPosition] =
                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                            | (gammaLutBlue[(dstRB & 0x00FF)]));
                                        #endregion
                                        #endregion
                                        #endregion


                                    }
                                    #endregion

                                    // alway increment color index
                                    currentColorIndexScaled += scaledIncrementColorIndex;

                                    // assign value for next loop
                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                        #region each row we need increase the value of color index
                        startOfRowIndex += rowColorIndexIncrementScaled;
                        #endregion
                    }
                    #endregion
                }
            }
            else
            {
                // blending include alpha of built color
                if (paint.Style != GradientStyle.Pad)
                {
                    // in case reflect and repeat, we don't care value that out of range
                    startOfRowIndex &= ColorIndexIncludeIncrementDoubleMask;
                    rowColorIndexIncrementScaled &= ColorIndexIncludeIncrementDoubleMask;
                    // when no need to blending, when draw a horizontal line
                    // do not need check the back color, alway setup
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;

                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                // calculate the first cell color index
                                #region second way to implement color index
                                currentColorIndexScaled = startOfRowIndex;
                                #endregion

                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1))
                                    {
                                        if (scLastCoverage != 0)
                                        {
                                            // fast bit absolute
                                            scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                            //#region non-zero checking code
                                            //if (scLastCoverage > 255) scLastCoverage = 255;
                                            //#endregion
                                            #region even-odd change
                                            scLastCoverage &= 511;
                                            if (scLastCoverage >= 256)
                                            {
                                                scLastCoverage = 512 - scLastCoverage - 1;
                                            }
                                            #endregion
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;

                                            while (startXPosition < lastXPosition)
                                            {

                                                colorData = builtColors[currentColorIndexScaled < 0 ?
                                                        (currentColorIndexScaled >> IncrementColorIndexShift) + 512 :
                                                        (currentColorIndexScaled >> IncrementColorIndexShift)];

                                                // get current color index value
                                                calculatedCoverage = (byte)(((colorData >> 24) * scLastCoverage) >> 8);

                                                //dst = BufferData[startXPosition];
                                                //dstRB = dst & 0x00FF00FF;
                                                //dstG = (dst >> 8) & 0xFF;
                                                //BufferData[startXPosition] =
                                                //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                //    | (uint)((((((((colorData & 0x0000FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                //    | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                                #region apply gamma
                                                dst = BufferData[startXPosition];
                                                dstG = (dst >> 8) & 0xFF;
                                                dstRB = (((((colorData & 0x00FF00FF) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                BufferData[startXPosition] =
                                                    (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                    | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                    | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                    | (gammaLutBlue[(dstRB & 0x00FF)]));
                                                #endregion

                                                startXPosition++;
                                                // increase the current color index
                                                currentColorIndexScaled += scaledIncrementColorIndex;
                                                currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
                                            }
                                            #endregion
                                        }
                                        else
                                        {
                                            // not filling but must set and increase the color index
                                            currentColorIndexScaled += (currentCellData.X - scLastX - 1) * scaledIncrementColorIndex;
                                            currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
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

                                        //#region non-zero checking code
                                        //if (tempCover > 255) tempCover = 255;
                                        //#endregion

                                        #region even-odd change
                                        tempCover &= 511;
                                        if (tempCover >= 256)
                                        {
                                            tempCover = 512 - tempCover - 1;
                                        }
                                        #endregion

                                        // get current color data
                                        #region blend pixel
                                        //tempCover = (int)((tempCover * colorAlpha) >> 8);
                                        ////if (tempCover > 255) tempCover = 255;
                                        //calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;

                                        #region blend here

                                        colorData = builtColors[currentColorIndexScaled < 0 ?
                                                (currentColorIndexScaled >> IncrementColorIndexShift) + 512 :
                                                (currentColorIndexScaled >> IncrementColorIndexShift)];
                                        calculatedCoverage = (byte)(((colorData >> 24) * tempCover) >> 8);
                                        //dst = BufferData[startXPosition];
                                        //dstRB = dst & 0x00FF00FF;
                                        //dstG = (dst >> 8) & 0xFF;
                                        //BufferData[startXPosition] =
                                        //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        //    | (uint)((((((((colorData & 0x0000FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        //    | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                        #region apply gamma
                                        dst = BufferData[startXPosition];
                                        dstG = (dst >> 8) & 0xFF;
                                        dstRB = (((((colorData & 0x00FF00FF) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                        BufferData[startXPosition] =
                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                    | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                    | (gammaLutBlue[(dstRB & 0x00FF)]));
                                        #endregion
                                        #endregion
                                        #endregion


                                    }
                                    #endregion

                                    // alway increment color index
                                    currentColorIndexScaled += scaledIncrementColorIndex;
                                    currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;

                                    // assign value for next loop
                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                        #region each row we need increase the value of color index
                        startOfRowIndex += rowColorIndexIncrementScaled;
                        startOfRowIndex &= ColorIndexIncludeIncrementDoubleMask;
                        #endregion
                    }
                    #endregion
                }
                else // special case using for pad mode
                {
                    // when no need to blending, when draw a horizontal line
                    // do not need check the back color, alway setup
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;

                        if (rows[startRowIndex] != null)
                        {
                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                // calculate the first cell color index
                                #region second way to implement color index
                                currentColorIndexScaled = startOfRowIndex;
                                #endregion

                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1))
                                    {
                                        if (scLastCoverage != 0)
                                        {
                                            // fast bit absolute
                                            scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                            //#region non-zero checking code
                                            //if (scLastCoverage > 255) scLastCoverage = 255;
                                            //#endregion
                                            #region even-odd change
                                            scLastCoverage &= 511;
                                            if (scLastCoverage >= 256)
                                            {
                                                scLastCoverage = 512 - scLastCoverage - 1;
                                            }
                                            #endregion
                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value

                                            while (startXPosition < lastXPosition)
                                            {
                                                colorData = builtColors[
                                                    currentColorIndexScaled < 0 ?
                                                        0 :
                                                    (currentColorIndexScaled > ColorIndexIncludeIncrementScale ?
                                                        255 :
                                                        (currentColorIndexScaled >> IncrementColorIndexShift))];

                                                calculatedCoverage = (byte)(((colorData >> 24) * scLastCoverage) >> 8);

                                                ////dst = BufferData[startXPosition];
                                                ////dstRB = dst & 0x00FF00FF;
                                                ////dstG = (dst >> 8) & 0xFF;
                                                ////BufferData[startXPosition] =
                                                ////    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                ////    | (uint)((((((((colorData & 0x0000FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                ////    | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                                #region apply gamma
                                                dst = BufferData[startXPosition];
                                                dstG = (dst >> 8) & 0xFF;
                                                dstRB = (((((colorData & 0x00FF00FF) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                BufferData[startXPosition] =
                                                    (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                    | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                    | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                    | (gammaLutBlue[(dstRB & 0x00FF)]));
                                                #endregion

                                                startXPosition++;
                                                // increase the current color index
                                                currentColorIndexScaled += scaledIncrementColorIndex;
                                            }

                                            #endregion
                                        }
                                        else
                                        {
                                            // not filling but must set and increase the color index
                                            currentColorIndexScaled += (currentCellData.X - scLastX - 1) * scaledIncrementColorIndex;
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

                                        //#region non-zero checking code
                                        //if (tempCover > 255) tempCover = 255;
                                        //#endregion

                                        #region even-odd change
                                        tempCover &= 511;
                                        if (tempCover >= 256)
                                        {
                                            tempCover = 512 - tempCover - 1;
                                        }
                                        #endregion

                                        // get current color data
                                        #region blend pixel
                                        //calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;

                                        #region blend here

                                        colorData = builtColors[currentColorIndexScaled < 0 ?
                                                        0 : (currentColorIndexScaled > ColorIndexIncludeIncrementScale ? 255 :
                                                        (currentColorIndexScaled >> IncrementColorIndexShift))];
                                        calculatedCoverage = (byte)(((colorData >> 24) * tempCover) >> 8);

                                        //dst = BufferData[startXPosition];
                                        //dstRB = dst & 0x00FF00FF;
                                        //dstG = (dst >> 8) & 0xFF;
                                        //BufferData[startXPosition] =
                                        //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        //    | (uint)((((((((colorData & 0x0000FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        //    | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                        #region apply gamma
                                        dst = BufferData[startXPosition];
                                        dstG = (dst >> 8) & 0xFF;
                                        dstRB = (((((colorData & 0x00FF00FF) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                        BufferData[startXPosition] =
                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                            | (gammaLutBlue[(dstRB & 0x00FF)]));
                                        #endregion

                                        #endregion
                                        #endregion


                                    }
                                    #endregion

                                    // alway increment color index
                                    currentColorIndexScaled += scaledIncrementColorIndex;

                                    // assign value for next loop
                                    scLastCoverage = currentCoverage;
                                    scLastX = currentCellData.X;

                                    // move to next cell
                                    currentCellData = currentCellData.Next;
                                } while (currentCellData != null);
                                #endregion
                            }
                        }
                        #region each row we need increase the value of color index
                        startOfRowIndex += rowColorIndexIncrementScaled;
                        #endregion
                    }
                    #endregion
                }
            }

            #endregion
        }
        #endregion

        #endregion

        #endregion


        #region FILL (INCLUDING TRANSFORM)

        #region NON-ZERO (!gamma)

        #region On Filling Transformed NonZero (transform, !gamma)
        /// <summary>
        /// Filling row data result from start y index to end y index including transformation
        /// <para>While filling can use CurrentTransformMatrix, or InverterMatrix... to calculate
        /// or access transformation information</para>
        /// </summary>
        /// <param name="paint">paint</param>
        /// <param name="rows">rows</param>
        /// <param name="startYIndex">start y index</param>
        /// <param name="endYIndex">end y index</param>
        protected override void OnFillingTransformedNonZero(
            PaintMaterial paint,
            RowData[] rows,
            int startYIndex,
            int endYIndex)
        {
            if (!(paint.Paint is LinearGradient))
            {
                //throw new NotImplementedException("Support color paint only");
                NotMatchPaintTypeException.Publish(typeof(LinearGradient), paint.Paint.GetType());
                return;
            }
            LinearGradient linearGradient = paint.Paint as LinearGradient;

            switch (linearGradient.Mode)
            {
                case LinearGradientMode.Horizontal:
                    OnFillingTransformedHorizontalNonZero(linearGradient, paint.ScaledOpacity, rows, startYIndex, endYIndex);
                    break;
                case LinearGradientMode.Vertical:
                    OnFillingTransformedVerticalNonZero(linearGradient, paint.ScaledOpacity, rows, startYIndex, endYIndex);
                    break;
                case LinearGradientMode.ForwardDiagonal:
                    OnFillingTransformedDiagonalNonZero(linearGradient, paint.ScaledOpacity, rows, startYIndex, endYIndex, true);
                    break;
                case LinearGradientMode.BackwardDiagonal:
                    OnFillingTransformedDiagonalNonZero(linearGradient, paint.ScaledOpacity, rows, startYIndex, endYIndex, false);
                    break;
            }
        }
        #endregion

        #region On Filling Transformed Horizontal NonZero (transform, !gamma)
        /// <summary>
        /// Fill to buffer base rows data information using non-zero rule
        /// </summary>
        /// <param name="paint">linear gradient object</param>
        /// <param name="rows">row data information</param>
        /// <param name="startRowIndex">start row index in row array need to draw</param>
        /// <param name="endRowIndex">end row index in end row array need to draw</param>
        void OnFillingTransformedHorizontalNonZero(
            LinearGradient paint,
            uint opacity,
            RowData[] rows,
            int startRowIndex,
            int endRowIndex)
        {
            // now not need to check null or not
            uint[] builtColors = paint.GetLinearColors(opacity);

            #region private variable for filling
            int currentCoverage, scLastCoverage, scLastX = 0;
            int tempCover = 0;
            int currentArea = 0;
            int lastXPosition = 0;
            int startXPosition = 0;
            byte calculatedCoverage = 0;
            int currentColorIndexScaled = 0;
            CellData currentCellData = null;
            uint colorData = 0;
            uint dst, dstRB, dstG;
            double startRowIncrement = 0;   //each row color index will increase value.
            #endregion

            #region variable for horizontal
            double startX = paint.StartX;
            double endX = paint.EndX;
            double distance = endX - startX;
            #endregion

            #region variable for transform
            #region transform line 1,1 -> 101,1
            //tmp = x;
            //x = tmp * Sx + y * Shx + Tx;  //transform X
            //y = tmp * Shy + y * Sy + Ty;  //transform Y
            double currentXTransformed = 1 * InvertedMatrixSx + 1 * InvertedMatrixShx + InvertedMatrixTx;
            double destXToTransformed = 101 * InvertedMatrixSx + 1 * InvertedMatrixShx + InvertedMatrixTx;
            #endregion
            // in horizontal we need increment by x after steps
            double transformedRatio = (destXToTransformed - currentXTransformed) / 100;
            // when transformed horizonline increase 1, x will increase by increment.
            int incrementTranformedColorIndexScaled =
                (int)((transformedRatio / distance) * ColorIndexIncludeIncrementScale);
            #endregion

            #region prepare value for rows
            // transform first cell of row
            currentXTransformed = // 0 * InvertedMatrixSx + 
                 startRowIndex * InvertedMatrixShx + InvertedMatrixTx;
            currentXTransformed = ((currentXTransformed - startX) / distance);
            // calculate row increment
            startRowIncrement = ((InvertedMatrixShx / distance));
            #endregion

            #region FILLING
            if (paint.Ramp.NoBlendingColor)
            {
                if (paint.Style != GradientStyle.Pad)
                {
                    #region optimized for reflect and repeat mode
                    incrementTranformedColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
                    if (incrementTranformedColorIndexScaled < 0)
                    {
                        incrementTranformedColorIndexScaled = ColorIndexIncludeIncrementDoubleScale - incrementTranformedColorIndexScaled;
                    }
                    #endregion
                    // when no need to blending, when draw a horizontal line
                    // do not need check the back color, alway setup
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;

                        if (rows[startRowIndex] != null)
                        {
                            #region transform for row
                            currentColorIndexScaled = (int)
                                (currentXTransformed * ColorIndexIncludeIncrementScale);
                            if (currentColorIndexScaled < 0)
                            {
                                currentColorIndexScaled += ColorIndexIncludeIncrementDoubleScale;
                            }
                            currentXTransformed += startRowIncrement;
                            #endregion

                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1))
                                    {
                                        if (scLastCoverage != 0)
                                        {
                                            // fast bit absolute
                                            scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                            #region non-zero checking code
                                            if (scLastCoverage > 255) scLastCoverage = 255;
                                            #endregion

                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            //currentColorIndexScaled = scLastX + 1 - CurrentStartXIndex;
                                            if (scLastCoverage >= 254)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    //BufferData[startXPosition++] = builtColors[(currentColorIndexScaled >> IncrementColorIndexShift) ];
                                                    BufferData[startXPosition++] = builtColors[(currentColorIndexScaled & ColorIndexIncludeIncrementDoubleMask) >> IncrementColorIndexShift];
                                                    // incre color index
                                                    currentColorIndexScaled += incrementTranformedColorIndexScaled;
                                                    //currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
                                                }
                                            }
                                            else
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    colorData = builtColors[(currentColorIndexScaled & ColorIndexIncludeIncrementDoubleMask) >> IncrementColorIndexShift];
                                                    // incre color index
                                                    currentColorIndexScaled += incrementTranformedColorIndexScaled;

                                                    calculatedCoverage = (byte)((colorData >> 24));
                                                    calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                    if (calculatedCoverage >= 255)
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
                                                    currentColorIndexScaled++;
                                                }
                                            }
                                            #endregion
                                        }
                                        else
                                        {
                                            // incre color index
                                            currentColorIndexScaled += (currentCellData.X - scLastX - 1) * incrementTranformedColorIndexScaled;
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non-zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion

                                        colorData = builtColors[(currentColorIndexScaled & ColorIndexIncludeIncrementDoubleMask) >> IncrementColorIndexShift];

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

                                    // incre color index
                                    currentColorIndexScaled += incrementTranformedColorIndexScaled;

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
                {//Pad mode
                    // when no need to blending, when draw a horizontal line
                    // do not need check the back color, alway setup
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;

                        if (rows[startRowIndex] != null)
                        {
                            #region transform for row
                            currentColorIndexScaled = (int)
                                (currentXTransformed * ColorIndexIncludeIncrementScale);
                            //if (currentColorIndexScaled < 0)
                            //{
                            //    currentColorIndexScaled += ColorIndexIncludeIncrementDoubleScale;
                            //    currentColorIndexScaled = -currentColorIndexScaled; //???
                            //}
                            currentXTransformed += startRowIncrement;
                            #endregion

                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1))
                                    {
                                        if (scLastCoverage != 0)
                                        {
                                            // fast bit absolute
                                            scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                            #region non-zero checking code
                                            if (scLastCoverage > 255) scLastCoverage = 255;
                                            #endregion

                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            //currentColorIndexScaled = scLastX + 1 - CurrentStartXIndex;
                                            if (scLastCoverage >= 254)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    //BufferData[startXPosition++] = builtColors[(currentColorIndexScaled & ColorIndexIncludeIncrementDoubleMask) >> IncrementColorIndexShift];
                                                    BufferData[startXPosition++] = builtColors[
                                                        currentColorIndexScaled < 0 ?
                                                        0 :
                                                        (currentColorIndexScaled > ColorIndexIncludeIncrementScale ?
                                                        255 :
                                                        (currentColorIndexScaled) >> IncrementColorIndexShift)];
                                                    // incre color index
                                                    currentColorIndexScaled += incrementTranformedColorIndexScaled;
                                                    //currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
                                                }
                                            }
                                            else
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    //colorData = builtColors[(currentColorIndexScaled & ColorIndexIncludeIncrementDoubleMask) >> IncrementColorIndexShift];
                                                    colorData = builtColors[
                                                        currentColorIndexScaled < 0 ?
                                                        0 :
                                                        (currentColorIndexScaled > ColorIndexIncludeIncrementScale ?
                                                        255 :
                                                        (currentColorIndexScaled) >> IncrementColorIndexShift)];

                                                    // incre color index
                                                    currentColorIndexScaled += incrementTranformedColorIndexScaled;

                                                    calculatedCoverage = (byte)((colorData >> 24));
                                                    calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                    if (calculatedCoverage >= 255)
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
                                                    currentColorIndexScaled++;
                                                }
                                            }
                                            #endregion
                                        }
                                        else
                                        {
                                            // incre color index
                                            currentColorIndexScaled += (currentCellData.X - scLastX - 1) * incrementTranformedColorIndexScaled;
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non-zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion

                                        //colorData = builtColors[(currentColorIndexScaled & ColorIndexIncludeIncrementDoubleMask) >> IncrementColorIndexShift];
                                        colorData = builtColors[
                                            currentColorIndexScaled < 0 ?
                                                0 :
                                            (currentColorIndexScaled > ColorIndexIncludeIncrementScale ?
                                                255 :
                                                (currentColorIndexScaled) >> IncrementColorIndexShift)];

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

                                    // incre color index
                                    currentColorIndexScaled += incrementTranformedColorIndexScaled;

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
                }//Pad mode
            }//paint.Ramp.NoBlendingColor
            else
            {//has blending color
                if (paint.Style != GradientStyle.Pad)
                {
                    #region optimized for reflect and repeat mode
                    incrementTranformedColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
                    if (incrementTranformedColorIndexScaled < 0)
                    {
                        incrementTranformedColorIndexScaled = ColorIndexIncludeIncrementDoubleScale - incrementTranformedColorIndexScaled;
                    }
                    #endregion
                    // when no need to blending, when draw a horizontal line
                    // do not need check the back color, alway setup
                    #region filling with blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;

                        if (rows[startRowIndex] != null)
                        {
                            #region transform for row
                            currentColorIndexScaled = (int)
                                (currentXTransformed * ColorIndexIncludeIncrementScale);
                            if (currentColorIndexScaled < 0)
                            {
                                currentColorIndexScaled += ColorIndexIncludeIncrementDoubleScale;
                            }
                            currentXTransformed += startRowIncrement;
                            #endregion

                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1))
                                    {
                                        if (scLastCoverage != 0)
                                        {
                                            // fast bit absolute
                                            scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                            #region non-zero checking code
                                            if (scLastCoverage > 255) scLastCoverage = 255;
                                            #endregion

                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            while (startXPosition < lastXPosition)
                                            {
                                                colorData = builtColors[(currentColorIndexScaled & ColorIndexIncludeIncrementDoubleMask) >> IncrementColorIndexShift];
                                                currentColorIndexScaled += incrementTranformedColorIndexScaled;
                                                calculatedCoverage = (byte)(colorData >> 24);
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);

                                                if (calculatedCoverage >= 255)
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
                                                currentColorIndexScaled++;
                                            }
                                            #endregion
                                        }
                                        else
                                        {
                                            // incre color index
                                            currentColorIndexScaled += (currentCellData.X - scLastX - 1) * incrementTranformedColorIndexScaled;
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non-zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion

                                        colorData = builtColors[(currentColorIndexScaled & ColorIndexIncludeIncrementDoubleMask) >> IncrementColorIndexShift];

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

                                    // incre color index
                                    currentColorIndexScaled += incrementTranformedColorIndexScaled;

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
                {//Pad mode (& has blending color)

                    // when no need to blending, when draw a horizontal line
                    // do not need check the back color, alway setup
                    #region filling with blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;

                        if (rows[startRowIndex] != null)
                        {
                            #region transform for row
                            currentColorIndexScaled = (int)
                                (currentXTransformed * ColorIndexIncludeIncrementScale);
                            //if (currentColorIndexScaled < 0)
                            //{
                            //    currentColorIndexScaled += ColorIndexIncludeIncrementDoubleScale;
                            //}
                            currentXTransformed += startRowIncrement;
                            #endregion

                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1))
                                    {
                                        if (scLastCoverage != 0)
                                        {
                                            // fast bit absolute
                                            scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                            #region non-zero checking code
                                            if (scLastCoverage > 255) scLastCoverage = 255;
                                            #endregion

                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            while (startXPosition < lastXPosition)
                                            {
                                                colorData = builtColors[
                                                    currentColorIndexScaled < 0 ?
                                                        0 :
                                                    (currentColorIndexScaled > ColorIndexIncludeIncrementScale ?
                                                        255 :
                                                        (currentColorIndexScaled) >> IncrementColorIndexShift)];

                                                currentColorIndexScaled += incrementTranformedColorIndexScaled;
                                                calculatedCoverage = (byte)(colorData >> 24);
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);

                                                if (calculatedCoverage >= 255)
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
                                                currentColorIndexScaled++;
                                            }
                                            #endregion
                                        }
                                        else
                                        {
                                            // incre color index
                                            currentColorIndexScaled += (currentCellData.X - scLastX - 1) * incrementTranformedColorIndexScaled;
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non-zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion

                                        colorData = builtColors[
                                            currentColorIndexScaled < 0 ?
                                                0 :
                                            (currentColorIndexScaled > ColorIndexIncludeIncrementScale ?
                                                255 :
                                                (currentColorIndexScaled) >> IncrementColorIndexShift)];

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

                                    // incre color index
                                    currentColorIndexScaled += incrementTranformedColorIndexScaled;

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
                }//Pad mode (& has blending color)
            }//has blending color

            #endregion
        }
        #endregion

        #region On Filling Transformed Vertical NonZero (transform, !gamma)
        /// <summary>
        /// Fill to buffer base rows data information using non-zero rule
        /// </summary>
        /// <param name="paint">linear gradient object</param>
        /// <param name="rows">row data information</param>
        /// <param name="startRowIndex">start row index in row array need to draw</param>
        /// <param name="endRowIndex">end row index in end row array need to draw</param>
        void OnFillingTransformedVerticalNonZero(
            LinearGradient paint,
            uint opacity,
            RowData[] rows,
            int startRowIndex,
            int endRowIndex)
        {
            // now not need to check null or not
            uint[] builtColors = paint.GetLinearColors(opacity);

            #region private variable for filling
            int currentCoverage, scLastCoverage, scLastX = 0;
            int tempCover = 0;
            int currentArea = 0;
            int lastXPosition = 0;
            int startXPosition = 0;
            byte calculatedCoverage = 0;
            int currentColorIndexScaled = 0;
            CellData currentCellData = null;
            uint colorData = 0;
            uint dst, dstRB, dstG;

            //uint colorG = 0;
            //uint colorRB = 0;

            // each row color index will increase value
            double startRowIncrement = 0;
            #endregion

            #region varialbe for vertical
            double startY = paint.StartY;
            double endY = paint.EndY;
            double distance = endY - startY;
            #endregion

            #region variable for transform
            #region transform line 1,1 => 101,1
            double currentYTransformed = 1 * InvertedMatrixShy + 1 * InvertedMatrixSy + InvertedMatrixTy;
            double destYToTransformed = 101 * InvertedMatrixShy + 1 * InvertedMatrixSy + InvertedMatrixTy;
            #endregion
            // in vertical we need increment by x after steps
            double transformedRatio = (destYToTransformed - currentYTransformed) / 100;
            // when transformed horizonline increase 1, x will increase by increment.
            int incrementTranformedColorIndexScaled =
                (int)((transformedRatio / distance) * ColorIndexIncludeIncrementScale);
            #endregion

            #region prepare value for rows
            //transform first cell of row
            currentYTransformed =
                startRowIndex * InvertedMatrixSy + InvertedMatrixTy;
            currentYTransformed = ((currentYTransformed - startY) / distance);

            //calculate row increment
            startRowIncrement = ((InvertedMatrixSy / distance));
            #endregion

            #region FILLING
            if (paint.Ramp.NoBlendingColor)
            {// no blending color
                if (paint.Style != GradientStyle.Pad)
                {
                    #region optimized for reflect and repeat mode
                    incrementTranformedColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
                    if (incrementTranformedColorIndexScaled < 0)
                    {
                        incrementTranformedColorIndexScaled = ColorIndexIncludeIncrementDoubleScale - incrementTranformedColorIndexScaled;
                    }
                    #endregion
                    // when no need to blending, when draw a horizontal line
                    // do not need check the back color, alway setup
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;

                        if (rows[startRowIndex] != null)
                        {
                            #region transform for row
                            currentColorIndexScaled = (int)
                                (currentYTransformed * ColorIndexIncludeIncrementScale);
                            if (currentColorIndexScaled < 0)
                            {
                                currentColorIndexScaled += ColorIndexIncludeIncrementDoubleScale;
                            }
                            currentYTransformed += startRowIncrement;
                            #endregion

                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1))
                                    {
                                        if (scLastCoverage != 0)
                                        {
                                            // fast bit absolute
                                            scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                            #region non-zero checking code
                                            if (scLastCoverage > 255) scLastCoverage = 255;
                                            #endregion

                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            if (scLastCoverage >= 254)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    //BufferData[startXPosition++] = colorData;
                                                    //BufferData[startXPosition++] = builtColors[(currentColorIndexScaled >> IncrementColorIndexShift) ];
                                                    BufferData[startXPosition++] = builtColors[(currentColorIndexScaled & ColorIndexIncludeIncrementDoubleMask) >> IncrementColorIndexShift];
                                                    // incre color index
                                                    currentColorIndexScaled += incrementTranformedColorIndexScaled;
                                                    //currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
                                                }
                                            }
                                            else
                                            {
                                                while (startXPosition < lastXPosition)
                                                {

                                                    colorData = builtColors[(currentColorIndexScaled & ColorIndexIncludeIncrementDoubleMask) >> IncrementColorIndexShift];
                                                    // incre color index
                                                    currentColorIndexScaled += incrementTranformedColorIndexScaled;

                                                    calculatedCoverage = (byte)((colorData >> 24));
                                                    calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                    if (calculatedCoverage >= 255)
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
                                                            | (uint)((((((((colorData & 0xFF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                            | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                    }
                                                    startXPosition++;
                                                    currentColorIndexScaled++;
                                                }
                                            }
                                            #endregion
                                        }
                                        else
                                        {
                                            // incre color index
                                            currentColorIndexScaled += (currentCellData.X - scLastX - 1) * incrementTranformedColorIndexScaled;
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non-zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion

                                        colorData = builtColors[(currentColorIndexScaled & ColorIndexIncludeIncrementDoubleMask) >> IncrementColorIndexShift];

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
                                            | (uint)((((((((colorData & 0xFF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    // incre color index
                                    currentColorIndexScaled += incrementTranformedColorIndexScaled;

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
                {//GradientStyle.Pad mode
                    #region GradientStyle.Pad

                    // when no need to blending, when draw a horizontal line
                    // do not need check the back color, alway setup
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;

                        if (rows[startRowIndex] != null)
                        {
                            #region transform for row
                            currentColorIndexScaled = (int)
                                (currentYTransformed * ColorIndexIncludeIncrementScale);
                            //if (currentColorIndexScaled < 0)
                            //{
                            //    currentColorIndexScaled += ColorIndexIncludeIncrementDoubleScale;
                            //}
                            currentYTransformed += startRowIncrement;
                            #endregion

                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1))
                                    {
                                        if (scLastCoverage != 0)
                                        {
                                            // fast bit absolute
                                            scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                            #region non-zero checking code
                                            if (scLastCoverage > 255) scLastCoverage = 255;
                                            #endregion

                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            if (scLastCoverage >= 254)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    //BufferData[startXPosition++] = colorData;
                                                    //BufferData[startXPosition++] = builtColors[(currentColorIndexScaled >> IncrementColorIndexShift) ];
                                                    BufferData[startXPosition++] = builtColors[
                                                        currentColorIndexScaled < 0 ?
                                                            0 :
                                                            (currentColorIndexScaled > ColorIndexIncludeIncrementScale ?
                                                                255 :
                                                                currentColorIndexScaled >> IncrementColorIndexShift)];
                                                    // incre color index
                                                    currentColorIndexScaled += incrementTranformedColorIndexScaled;
                                                    //currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
                                                }
                                            }
                                            else
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    //colorData = builtColors[(currentColorIndexScaled & ColorIndexIncludeIncrementDoubleMask) >> IncrementColorIndexShift];
                                                    colorData = builtColors[
                                                        currentColorIndexScaled < 0 ?
                                                            0 :
                                                        (currentColorIndexScaled > ColorIndexIncludeIncrementScale ?
                                                            255 :
                                                            currentColorIndexScaled >> IncrementColorIndexShift)];
                                                    // incre color index
                                                    currentColorIndexScaled += incrementTranformedColorIndexScaled;

                                                    calculatedCoverage = (byte)((colorData >> 24));
                                                    calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                    if (calculatedCoverage >= 255)
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
                                                            | (uint)((((((((colorData & 0xFF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                            | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                    }
                                                    startXPosition++;
                                                    currentColorIndexScaled++;
                                                }
                                            }
                                            #endregion
                                        }
                                        else
                                        {
                                            // incre color index
                                            currentColorIndexScaled += (currentCellData.X - scLastX - 1) * incrementTranformedColorIndexScaled;
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);
                                        #region non-zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion

                                        colorData = builtColors[
                                            currentColorIndexScaled < 0 ?
                                                0 :
                                                (currentColorIndexScaled > ColorIndexIncludeIncrementScale ?
                                                    255 :
                                                    currentColorIndexScaled >> IncrementColorIndexShift)];

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
                                            | (uint)((((((((colorData & 0xFF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    // incre color index
                                    currentColorIndexScaled += incrementTranformedColorIndexScaled;

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
                    #endregion
                }//GradientStyle.Pad mode
            }// no blending color
            else
            {// has blending color
                if (paint.Style != GradientStyle.Pad)
                {
                    #region optimized for reflect and repeat mode
                    incrementTranformedColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
                    if (incrementTranformedColorIndexScaled < 0)
                    {
                        incrementTranformedColorIndexScaled = ColorIndexIncludeIncrementDoubleScale - incrementTranformedColorIndexScaled;
                    }
                    #endregion
                    // when no need to blending, when draw a horizontal line
                    // do not need check the back color, alway setup
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;

                        if (rows[startRowIndex] != null)
                        {
                            #region transform for row
                            currentColorIndexScaled = (int)
                                (currentYTransformed * ColorIndexIncludeIncrementScale);
                            if (currentColorIndexScaled < 0)
                            {
                                currentColorIndexScaled += ColorIndexIncludeIncrementDoubleScale;
                            }
                            currentYTransformed += startRowIncrement;
                            #endregion

                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1))
                                    {
                                        if (scLastCoverage != 0)
                                        {
                                            // fast bit absolute
                                            scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                            #region non-zero checking code
                                            if (scLastCoverage > 255) scLastCoverage = 255;
                                            #endregion

                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;

                                            while (startXPosition < lastXPosition)
                                            {
                                                colorData = builtColors[(currentColorIndexScaled & ColorIndexIncludeIncrementDoubleMask) >> IncrementColorIndexShift];
                                                currentColorIndexScaled += incrementTranformedColorIndexScaled;
                                                calculatedCoverage = (byte)(colorData >> 24);
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);

                                                if (calculatedCoverage >= 254)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    #region blend here
                                                    dst = BufferData[startXPosition];
                                                    dstRB = dst & 0x00FF00FF;
                                                    dstG = (dst >> 8) & 0xFF;

                                                    BufferData[startXPosition] =
                                                        (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                        | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                    #endregion
                                                }
                                                startXPosition++;
                                                currentColorIndexScaled++;
                                            }
                                            #endregion
                                        }
                                        else
                                        {
                                            // incre color index
                                            currentColorIndexScaled += (currentCellData.X - scLastX - 1) * incrementTranformedColorIndexScaled;
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non-zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion

                                        colorData = builtColors[(currentColorIndexScaled & ColorIndexIncludeIncrementDoubleMask) >> IncrementColorIndexShift];

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
                                            | (uint)((((((((colorData & 0xFF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    // incre color index
                                    currentColorIndexScaled += incrementTranformedColorIndexScaled;

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
                    #region GradientStyle.Pad

                    // when no need to blending, when draw a horizontal line
                    // do not need check the back color, alway setup
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;

                        if (rows[startRowIndex] != null)
                        {
                            #region transform for row
                            currentColorIndexScaled = (int)
                                (currentYTransformed * ColorIndexIncludeIncrementScale);
                            //if (currentColorIndexScaled < 0)
                            //{
                            //    currentColorIndexScaled += ColorIndexIncludeIncrementDoubleScale;
                            //}
                            currentYTransformed += startRowIncrement;
                            #endregion

                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1))
                                    {
                                        if (scLastCoverage != 0)
                                        {
                                            // fast bit absolute
                                            scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                            #region non-zero checking code
                                            if (scLastCoverage > 255) scLastCoverage = 255;
                                            #endregion

                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;

                                            while (startXPosition < lastXPosition)
                                            {
                                                //colorData = builtColors[(currentColorIndexScaled & ColorIndexIncludeIncrementDoubleMask) >> IncrementColorIndexShift];
                                                colorData = builtColors[
                                                    currentColorIndexScaled < 0 ?
                                                    0 :
                                                    (currentColorIndexScaled > ColorIndexIncludeIncrementScale ?
                                                    255 :
                                                    currentColorIndexScaled >> IncrementColorIndexShift)];
                                                currentColorIndexScaled += incrementTranformedColorIndexScaled;
                                                calculatedCoverage = (byte)(colorData >> 24);
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);

                                                if (calculatedCoverage >= 254)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    #region blend here
                                                    dst = BufferData[startXPosition];
                                                    dstRB = dst & 0x00FF00FF;
                                                    dstG = (dst >> 8) & 0xFF;

                                                    BufferData[startXPosition] =
                                                        (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                        | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                    #endregion
                                                }
                                                startXPosition++;
                                                currentColorIndexScaled++;
                                            }
                                            #endregion
                                        }
                                        else
                                        {
                                            // incre color index
                                            currentColorIndexScaled += (currentCellData.X - scLastX - 1) * incrementTranformedColorIndexScaled;
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non-zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion

                                        colorData = builtColors[
                                            currentColorIndexScaled < 0 ?
                                                0 :
                                            (currentColorIndexScaled > ColorIndexIncludeIncrementScale ?
                                                255 :
                                                currentColorIndexScaled >> IncrementColorIndexShift)];

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
                                            | (uint)((((((((colorData & 0xFF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    // incre color index
                                    currentColorIndexScaled += incrementTranformedColorIndexScaled;

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
                    #endregion
                }
            }//has blending color

            #endregion
        }
        #endregion

        #region On Filling Transformed Diagonal NonZero (transform, !gamma)
        /// <summary>
        /// Fill to buffer base rows data information using non-zero rule
        /// </summary>
        /// <param name="paint">linear gradient object</param>
        /// <param name="rows">row data information</param>
        /// <param name="startRowIndex">start row index in row array need to draw</param>
        /// <param name="endRowIndex">end row index in end row array need to draw</param>
        /// <param name="isForward">is diagonal gradient is forward</param>
        void OnFillingTransformedDiagonalNonZero(
            LinearGradient paint,
            uint opacity,
            RowData[] rows,
            int startRowIndex,
            int endRowIndex,
            bool isForward)
        {
            throw new NotImplementedException();
        }
        #endregion

        #endregion

        #region NON-ZERO (gamma)

        #region On Filling Transformed Non-Zero (transform, gamma)
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
        protected override void OnFillingTransformedNonZero(PaintMaterial paint, RowData[] rows, int startRowIndex, int endRowIndex, byte[] gammaLutRed, byte[] gammaLutGreen, byte[] gammaLutBlue)
        {
            if (!(paint.Paint is LinearGradient))
            {
                NotMatchPaintTypeException.Publish(typeof(LinearGradient), paint.Paint.GetType());
                return;
            }
            LinearGradient linearGradient = paint.Paint as LinearGradient;
            switch (linearGradient.Mode)
            {
                case LinearGradientMode.Horizontal:
                    OnFillingTransformedHorizontalNonZero(linearGradient, paint.ScaledOpacity, rows, startRowIndex, endRowIndex, gammaLutRed, gammaLutGreen, gammaLutBlue);
                    break;
                case LinearGradientMode.Vertical:
                    OnFillingTransformedVerticalNonZero(linearGradient, paint.ScaledOpacity, rows, startRowIndex, endRowIndex, gammaLutRed, gammaLutGreen, gammaLutBlue);
                    break;
                case LinearGradientMode.ForwardDiagonal:
                    OnFillingTransformedDiagonalNonZero(linearGradient, paint.ScaledOpacity, rows, startRowIndex, endRowIndex, true, gammaLutRed, gammaLutGreen, gammaLutBlue);
                    break;
                case LinearGradientMode.BackwardDiagonal:
                    OnFillingTransformedDiagonalNonZero(linearGradient, paint.ScaledOpacity, rows, startRowIndex, endRowIndex, false, gammaLutRed, gammaLutGreen, gammaLutBlue);
                    break;
            }
        }
        #endregion

        #region On Filling Transformed Horizontal NonZero (transform, gamma)
        /// <summary>
        /// Fill to buffer base rows data information using non-zero rule
        /// </summary>
        /// <param name="paint">linear gradient object</param>
        /// <param name="rows">row data information</param>
        /// <param name="startRowIndex">start row index in row array need to draw</param>
        /// <param name="endRowIndex">end row index in end row array need to draw</param>
        /// <param name="gammaLutRed">gamma look up table for red</param>
        /// <param name="gammaLutGreen">gamma look up table for green</param>
        /// <param name="gammaLutBlue">gamma look up table for blue</param>
        void OnFillingTransformedHorizontalNonZero(LinearGradient paint, uint opacity, RowData[] rows, int startRowIndex, int endRowIndex, byte[] gammaLutRed, byte[] gammaLutGreen, byte[] gammaLutBlue)
        {
            // now not need to check null or not
            uint[] builtColors = paint.GetLinearColors(opacity);

            #region private variable for filling
            int currentCoverage, scLastCoverage, scLastX = 0;
            int tempCover = 0;
            int currentArea = 0;
            int lastXPosition = 0;
            int startXPosition = 0;
            byte calculatedCoverage = 0;
            int currentColorIndexScaled = 0;
            CellData currentCellData = null;
            uint colorData = 0;
            uint dst, dstRB, dstG;
            double startRowIncrement = 0;   //each row color index will increase value.
            #endregion

            #region variable for horizontal
            double startX = paint.StartX;
            double endX = paint.EndX;
            double distance = endX - startX;
            #endregion

            #region variable for transform
            #region transform line 1,1 -> 101,1
            //tmp = x;
            //x = tmp * Sx + y * Shx + Tx;  //transform X
            //y = tmp * Shy + y * Sy + Ty;  //transform Y
            double currentXTransformed = 1 * InvertedMatrixSx + 1 * InvertedMatrixShx + InvertedMatrixTx;
            double destXToTransformed = 101 * InvertedMatrixSx + 1 * InvertedMatrixShx + InvertedMatrixTx;
            #endregion
            // in horizontal we need increment by x after steps
            double transformedRatio = (destXToTransformed - currentXTransformed) / 100;
            // when transformed horizonline increase 1, x will increase by increment.
            int incrementTranformedColorIndexScaled =
                (int)((transformedRatio / distance) * ColorIndexIncludeIncrementScale);
            #endregion

            #region prepare value for rows
            // transform first cell of row
            currentXTransformed = // 0 * InvertedMatrixSx + 
                 startRowIndex * InvertedMatrixShx + InvertedMatrixTx;
            currentXTransformed = ((currentXTransformed - startX) / distance);
            // calculate row increment
            startRowIncrement = ((InvertedMatrixShx / distance));
            #endregion

            #region FILLING
            if (paint.Ramp.NoBlendingColor)
            {
                if (paint.Style != GradientStyle.Pad)
                {
                    #region optimized for reflect and repeat mode
                    incrementTranformedColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
                    if (incrementTranformedColorIndexScaled < 0)
                    {
                        incrementTranformedColorIndexScaled = ColorIndexIncludeIncrementDoubleScale - incrementTranformedColorIndexScaled;
                    }
                    #endregion
                    // when no need to blending, when draw a horizontal line
                    // do not need check the back color, alway setup
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;

                        if (rows[startRowIndex] != null)
                        {
                            #region transform for row
                            currentColorIndexScaled = (int)
                                (currentXTransformed * ColorIndexIncludeIncrementScale);
                            if (currentColorIndexScaled < 0)
                            {
                                currentColorIndexScaled += ColorIndexIncludeIncrementDoubleScale;
                            }
                            currentXTransformed += startRowIncrement;
                            #endregion

                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1))
                                    {
                                        if (scLastCoverage != 0)
                                        {
                                            // fast bit absolute
                                            scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                            #region non-zero checking code
                                            if (scLastCoverage > 255) scLastCoverage = 255;
                                            #endregion

                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            //currentColorIndexScaled = scLastX + 1 - CurrentStartXIndex;
                                            if (scLastCoverage >= 254)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    //BufferData[startXPosition++] = builtColors[(currentColorIndexScaled >> IncrementColorIndexShift) ];
                                                    BufferData[startXPosition++] = builtColors[(currentColorIndexScaled & ColorIndexIncludeIncrementDoubleMask) >> IncrementColorIndexShift];
                                                    // incre color index
                                                    currentColorIndexScaled += incrementTranformedColorIndexScaled;
                                                    //currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
                                                }
                                            }
                                            else
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    colorData = builtColors[(currentColorIndexScaled & ColorIndexIncludeIncrementDoubleMask) >> IncrementColorIndexShift];
                                                    // incre color index
                                                    currentColorIndexScaled += incrementTranformedColorIndexScaled;

                                                    calculatedCoverage = (byte)((colorData >> 24));
                                                    calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                    if (calculatedCoverage >= 255)
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
                                                            | (gammaLutBlue[(dstRB & 0x00FF)]));
                                                        #endregion
                                                    }
                                                    startXPosition++;
                                                    currentColorIndexScaled++;
                                                }
                                            }
                                            #endregion
                                        }
                                        else
                                        {
                                            // incre color index
                                            currentColorIndexScaled += (currentCellData.X - scLastX - 1) * incrementTranformedColorIndexScaled;
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non-zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion

                                        colorData = builtColors[(currentColorIndexScaled & ColorIndexIncludeIncrementDoubleMask) >> IncrementColorIndexShift];

                                        calculatedCoverage = (byte)(colorData >> 24);

                                        #region blend pixel
                                        tempCover = (int)((tempCover * calculatedCoverage) >> 8);
                                        //if (tempCover > 255) tempCover = 255;
                                        calculatedCoverage = (byte)tempCover;

                                        startXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                        //#region blend here
                                        //dst = BufferData[startXPosition];
                                        //dstRB = dst & 0x00FF00FF;
                                        //dstG = (dst >> 8) & 0xFF;
                                        //BufferData[startXPosition] =
                                        //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        //    | (uint)(((((((colorData & 0x00FF00FF)) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        //#endregion
                                        #region gamma apply
                                        dst = BufferData[startXPosition];
                                        dstG = (dst >> 8) & 0xFF;
                                        dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                        BufferData[startXPosition] =
                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                            | (gammaLutBlue[(dstRB & 0x00FF)]));
                                        #endregion

                                        #endregion
                                    }
                                    #endregion

                                    // incre color index
                                    currentColorIndexScaled += incrementTranformedColorIndexScaled;

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
                {//Pad mode
                    // when no need to blending, when draw a horizontal line
                    // do not need check the back color, alway setup
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;

                        if (rows[startRowIndex] != null)
                        {
                            #region transform for row
                            currentColorIndexScaled = (int)
                                (currentXTransformed * ColorIndexIncludeIncrementScale);
                            //if (currentColorIndexScaled < 0)
                            //{
                            //    currentColorIndexScaled += ColorIndexIncludeIncrementDoubleScale;
                            //    currentColorIndexScaled = -currentColorIndexScaled; //???
                            //}
                            currentXTransformed += startRowIncrement;
                            #endregion

                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1))
                                    {
                                        if (scLastCoverage != 0)
                                        {
                                            // fast bit absolute
                                            scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                            #region non-zero checking code
                                            if (scLastCoverage > 255) scLastCoverage = 255;
                                            #endregion

                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            //currentColorIndexScaled = scLastX + 1 - CurrentStartXIndex;
                                            if (scLastCoverage >= 254)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    //BufferData[startXPosition++] = builtColors[(currentColorIndexScaled & ColorIndexIncludeIncrementDoubleMask) >> IncrementColorIndexShift];
                                                    BufferData[startXPosition++] = builtColors[
                                                        currentColorIndexScaled < 0 ?
                                                        0 :
                                                        (currentColorIndexScaled > ColorIndexIncludeIncrementScale ?
                                                        255 :
                                                        (currentColorIndexScaled) >> IncrementColorIndexShift)];
                                                    // incre color index
                                                    currentColorIndexScaled += incrementTranformedColorIndexScaled;
                                                    //currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
                                                }
                                            }
                                            else
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    //colorData = builtColors[(currentColorIndexScaled & ColorIndexIncludeIncrementDoubleMask) >> IncrementColorIndexShift];
                                                    colorData = builtColors[
                                                        currentColorIndexScaled < 0 ?
                                                        0 :
                                                        (currentColorIndexScaled > ColorIndexIncludeIncrementScale ?
                                                        255 :
                                                        (currentColorIndexScaled) >> IncrementColorIndexShift)];

                                                    // incre color index
                                                    currentColorIndexScaled += incrementTranformedColorIndexScaled;

                                                    calculatedCoverage = (byte)((colorData >> 24));
                                                    calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                    if (calculatedCoverage >= 255)
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
                                                            | (gammaLutBlue[(dstRB & 0x00FF)]));
                                                        #endregion

                                                    }
                                                    startXPosition++;
                                                    currentColorIndexScaled++;
                                                }
                                            }
                                            #endregion
                                        }
                                        else
                                        {
                                            // incre color index
                                            currentColorIndexScaled += (currentCellData.X - scLastX - 1) * incrementTranformedColorIndexScaled;
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non-zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion

                                        //colorData = builtColors[(currentColorIndexScaled & ColorIndexIncludeIncrementDoubleMask) >> IncrementColorIndexShift];
                                        colorData = builtColors[
                                            currentColorIndexScaled < 0 ?
                                                0 :
                                            (currentColorIndexScaled > ColorIndexIncludeIncrementScale ?
                                                255 :
                                                (currentColorIndexScaled) >> IncrementColorIndexShift)];

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
                                        #endregion
                                        #region gamma apply
                                        dst = BufferData[startXPosition];
                                        dstG = (dst >> 8) & 0xFF;
                                        dstRB = ((((((colorData & 0x00FF00FF)) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                        BufferData[startXPosition] =
                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (((uint)gammaLutGreen[(((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                            | (gammaLutBlue[(dstRB & 0x00FF)]));
                                        #endregion

                                        #endregion
                                    }
                                    #endregion

                                    // incre color index
                                    currentColorIndexScaled += incrementTranformedColorIndexScaled;

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
                }//Pad mode
            }//paint.Ramp.NoBlendingColor
            else
            {//has blending color
                if (paint.Style != GradientStyle.Pad)
                {
                    #region optimized for reflect and repeat mode
                    incrementTranformedColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
                    if (incrementTranformedColorIndexScaled < 0)
                    {
                        incrementTranformedColorIndexScaled = ColorIndexIncludeIncrementDoubleScale - incrementTranformedColorIndexScaled;
                    }
                    #endregion
                    // when no need to blending, when draw a horizontal line
                    // do not need check the back color, alway setup
                    #region filling with blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;

                        if (rows[startRowIndex] != null)
                        {
                            #region transform for row
                            currentColorIndexScaled = (int)
                                (currentXTransformed * ColorIndexIncludeIncrementScale);
                            if (currentColorIndexScaled < 0)
                            {
                                currentColorIndexScaled += ColorIndexIncludeIncrementDoubleScale;
                            }
                            currentXTransformed += startRowIncrement;
                            #endregion

                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1))
                                    {
                                        if (scLastCoverage != 0)
                                        {
                                            // fast bit absolute
                                            scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                            #region non-zero checking code
                                            if (scLastCoverage > 255) scLastCoverage = 255;
                                            #endregion

                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            while (startXPosition < lastXPosition)
                                            {
                                                colorData = builtColors[(currentColorIndexScaled & ColorIndexIncludeIncrementDoubleMask) >> IncrementColorIndexShift];
                                                currentColorIndexScaled += incrementTranformedColorIndexScaled;
                                                calculatedCoverage = (byte)(colorData >> 24);
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);

                                                if (calculatedCoverage >= 255)
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
                                                        | (gammaLutBlue[(dstRB & 0x00FF)]));
                                                    #endregion
                                                }
                                                startXPosition++;
                                                currentColorIndexScaled++;
                                            }
                                            #endregion
                                        }
                                        else
                                        {
                                            // incre color index
                                            currentColorIndexScaled += (currentCellData.X - scLastX - 1) * incrementTranformedColorIndexScaled;
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non-zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion

                                        colorData = builtColors[(currentColorIndexScaled & ColorIndexIncludeIncrementDoubleMask) >> IncrementColorIndexShift];

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
                                            | (gammaLutBlue[(dstRB & 0x00FF)]));
                                        #endregion
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    // incre color index
                                    currentColorIndexScaled += incrementTranformedColorIndexScaled;

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
                {//Pad mode (& has blending color)

                    // when no need to blending, when draw a horizontal line
                    // do not need check the back color, alway setup
                    #region filling with blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;

                        if (rows[startRowIndex] != null)
                        {
                            #region transform for row
                            currentColorIndexScaled = (int)
                                (currentXTransformed * ColorIndexIncludeIncrementScale);
                            //if (currentColorIndexScaled < 0)
                            //{
                            //    currentColorIndexScaled += ColorIndexIncludeIncrementDoubleScale;
                            //}
                            currentXTransformed += startRowIncrement;
                            #endregion

                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1))
                                    {
                                        if (scLastCoverage != 0)
                                        {
                                            // fast bit absolute
                                            scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                            #region non-zero checking code
                                            if (scLastCoverage > 255) scLastCoverage = 255;
                                            #endregion

                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            while (startXPosition < lastXPosition)
                                            {
                                                colorData = builtColors[
                                                    currentColorIndexScaled < 0 ?
                                                        0 :
                                                    (currentColorIndexScaled > ColorIndexIncludeIncrementScale ?
                                                        255 :
                                                        (currentColorIndexScaled) >> IncrementColorIndexShift)];

                                                currentColorIndexScaled += incrementTranformedColorIndexScaled;
                                                calculatedCoverage = (byte)(colorData >> 24);
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);

                                                if (calculatedCoverage >= 255)
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
                                                        | (gammaLutBlue[(dstRB & 0x00FF)]));
                                                    #endregion

                                                }
                                                startXPosition++;
                                                currentColorIndexScaled++;
                                            }
                                            #endregion
                                        }
                                        else
                                        {
                                            // incre color index
                                            currentColorIndexScaled += (currentCellData.X - scLastX - 1) * incrementTranformedColorIndexScaled;
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non-zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion

                                        colorData = builtColors[
                                            currentColorIndexScaled < 0 ?
                                                0 :
                                            (currentColorIndexScaled > ColorIndexIncludeIncrementScale ?
                                                255 :
                                                (currentColorIndexScaled) >> IncrementColorIndexShift)];

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
                                            | (gammaLutBlue[(dstRB & 0x00FF)]));
                                        #endregion
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    // incre color index
                                    currentColorIndexScaled += incrementTranformedColorIndexScaled;

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
                }//Pad mode (& has blending color)
            }//has blending color

            #endregion

        }
        #endregion

        #region On Filling Transformed Vertical NonZero (transform, gamma)
        /// <summary>
        /// Fill to buffer base rows data information using non-zero rule
        /// </summary>
        /// <param name="paint">linear gradient object</param>
        /// <param name="rows">row data information</param>
        /// <param name="startRowIndex">start row index in row array need to draw</param>
        /// <param name="endRowIndex">end row index in end row array need to draw</param>
        /// <param name="gammaLutRed">gamma look up table for red</param>
        /// <param name="gammaLutGreen">gamma look up table for green</param>
        /// <param name="gammaLutBlue">gamma look up table for blue</param>
        void OnFillingTransformedVerticalNonZero(LinearGradient paint, uint opacity, RowData[] rows, int startRowIndex, int endRowIndex, byte[] gammaLutRed, byte[] gammaLutGreen, byte[] gammaLutBlue)
        {
            // now not need to check null or not
            uint[] builtColors = paint.GetLinearColors(opacity);

            #region private variable for filling
            int currentCoverage, scLastCoverage, scLastX = 0;
            int tempCover = 0;
            int currentArea = 0;
            int lastXPosition = 0;
            int startXPosition = 0;
            byte calculatedCoverage = 0;
            int currentColorIndexScaled = 0;
            CellData currentCellData = null;
            uint colorData = 0;
            uint dst, dstRB, dstG;

            //uint colorG = 0;
            //uint colorRB = 0;

            // each row color index will increase value
            double startRowIncrement = 0;
            #endregion

            #region varialbe for vertical
            double startY = paint.StartY;
            double endY = paint.EndY;
            double distance = endY - startY;
            #endregion

            #region variable for transform
            #region transform line 1,1 => 101,1
            double currentYTransformed = 1 * InvertedMatrixShy + 1 * InvertedMatrixSy + InvertedMatrixTy;
            double destYToTransformed = 101 * InvertedMatrixShy + 1 * InvertedMatrixSy + InvertedMatrixTy;
            #endregion
            // in vertical we need increment by x after steps
            double transformedRatio = (destYToTransformed - currentYTransformed) / 100;
            // when transformed horizonline increase 1, x will increase by increment.
            int incrementTranformedColorIndexScaled =
                (int)((transformedRatio / distance) * ColorIndexIncludeIncrementScale);
            #endregion

            #region prepare value for rows
            //transform first cell of row
            currentYTransformed =
                startRowIndex * InvertedMatrixSy + InvertedMatrixTy;
            currentYTransformed = ((currentYTransformed - startY) / distance);

            //calculate row increment
            startRowIncrement = ((InvertedMatrixSy / distance));
            #endregion

            #region FILLING
            if (paint.Ramp.NoBlendingColor)
            {// no blending color
                if (paint.Style != GradientStyle.Pad)
                {
                    #region optimized for reflect and repeat mode
                    incrementTranformedColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
                    if (incrementTranformedColorIndexScaled < 0)
                    {
                        incrementTranformedColorIndexScaled = ColorIndexIncludeIncrementDoubleScale - incrementTranformedColorIndexScaled;
                    }
                    #endregion
                    // when no need to blending, when draw a horizontal line
                    // do not need check the back color, alway setup
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;

                        if (rows[startRowIndex] != null)
                        {
                            #region transform for row
                            currentColorIndexScaled = (int)
                                (currentYTransformed * ColorIndexIncludeIncrementScale);
                            if (currentColorIndexScaled < 0)
                            {
                                currentColorIndexScaled += ColorIndexIncludeIncrementDoubleScale;
                            }
                            currentYTransformed += startRowIncrement;
                            #endregion

                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1))
                                    {
                                        if (scLastCoverage != 0)
                                        {
                                            // fast bit absolute
                                            scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                            #region non-zero checking code
                                            if (scLastCoverage > 255) scLastCoverage = 255;
                                            #endregion

                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            if (scLastCoverage >= 254)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    //BufferData[startXPosition++] = colorData;
                                                    //BufferData[startXPosition++] = builtColors[(currentColorIndexScaled >> IncrementColorIndexShift) ];
                                                    BufferData[startXPosition++] = builtColors[(currentColorIndexScaled & ColorIndexIncludeIncrementDoubleMask) >> IncrementColorIndexShift];
                                                    // incre color index
                                                    currentColorIndexScaled += incrementTranformedColorIndexScaled;
                                                    //currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
                                                }
                                            }
                                            else
                                            {
                                                while (startXPosition < lastXPosition)
                                                {

                                                    colorData = builtColors[(currentColorIndexScaled & ColorIndexIncludeIncrementDoubleMask) >> IncrementColorIndexShift];
                                                    // incre color index
                                                    currentColorIndexScaled += incrementTranformedColorIndexScaled;

                                                    calculatedCoverage = (byte)((colorData >> 24));
                                                    calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                    if (calculatedCoverage >= 255)
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
                                                        //    | (uint)((((((((colorData & 0xFF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                        //    | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                        #region gamma apply
                                                        dst = BufferData[startXPosition];
                                                        dstG = (dst >> 8) & 0xFF;
                                                        dstRB = (((((colorData & 0x00FF00FF) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                        BufferData[startXPosition] =
                                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                            | (((uint)gammaLutGreen[(((((((colorData & 0xFF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                            | (gammaLutBlue[(dstRB & 0x00FF)]));
                                                        #endregion
                                                    }
                                                    startXPosition++;
                                                    currentColorIndexScaled++;
                                                }
                                            }
                                            #endregion
                                        }
                                        else
                                        {
                                            // incre color index
                                            currentColorIndexScaled += (currentCellData.X - scLastX - 1) * incrementTranformedColorIndexScaled;
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non-zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion

                                        colorData = builtColors[(currentColorIndexScaled & ColorIndexIncludeIncrementDoubleMask) >> IncrementColorIndexShift];

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
                                        //    | (uint)((((((((colorData & 0xFF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        //    | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #region gamma apply
                                        dst = BufferData[startXPosition];
                                        dstG = (dst >> 8) & 0xFF;
                                        dstRB = (((((colorData & 0x00FF00FF) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                        BufferData[startXPosition] =
                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (((uint)gammaLutGreen[(((((((colorData & 0xFF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                            | (gammaLutBlue[(dstRB & 0x00FF)]));
                                        #endregion

                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    // incre color index
                                    currentColorIndexScaled += incrementTranformedColorIndexScaled;

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
                {//GradientStyle.Pad mode
                    #region GradientStyle.Pad

                    // when no need to blending, when draw a horizontal line
                    // do not need check the back color, alway setup
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;

                        if (rows[startRowIndex] != null)
                        {
                            #region transform for row
                            currentColorIndexScaled = (int)
                                (currentYTransformed * ColorIndexIncludeIncrementScale);
                            //if (currentColorIndexScaled < 0)
                            //{
                            //    currentColorIndexScaled += ColorIndexIncludeIncrementDoubleScale;
                            //}
                            currentYTransformed += startRowIncrement;
                            #endregion

                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1))
                                    {
                                        if (scLastCoverage != 0)
                                        {
                                            // fast bit absolute
                                            scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                            #region non-zero checking code
                                            if (scLastCoverage > 255) scLastCoverage = 255;
                                            #endregion

                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;
                                            // get current color index value
                                            if (scLastCoverage >= 254)
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    //BufferData[startXPosition++] = colorData;
                                                    //BufferData[startXPosition++] = builtColors[(currentColorIndexScaled >> IncrementColorIndexShift) ];
                                                    BufferData[startXPosition++] = builtColors[
                                                        currentColorIndexScaled < 0 ?
                                                            0 :
                                                            (currentColorIndexScaled > ColorIndexIncludeIncrementScale ?
                                                                255 :
                                                                currentColorIndexScaled >> IncrementColorIndexShift)];
                                                    // incre color index
                                                    currentColorIndexScaled += incrementTranformedColorIndexScaled;
                                                    //currentColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
                                                }
                                            }
                                            else
                                            {
                                                while (startXPosition < lastXPosition)
                                                {
                                                    //colorData = builtColors[(currentColorIndexScaled & ColorIndexIncludeIncrementDoubleMask) >> IncrementColorIndexShift];
                                                    colorData = builtColors[
                                                        currentColorIndexScaled < 0 ?
                                                            0 :
                                                        (currentColorIndexScaled > ColorIndexIncludeIncrementScale ?
                                                            255 :
                                                            currentColorIndexScaled >> IncrementColorIndexShift)];
                                                    // incre color index
                                                    currentColorIndexScaled += incrementTranformedColorIndexScaled;

                                                    calculatedCoverage = (byte)((colorData >> 24));
                                                    calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);
                                                    if (calculatedCoverage >= 255)
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
                                                        //    | (uint)((((((((colorData & 0xFF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                        //    | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                        #region gamma apply
                                                        dst = BufferData[startXPosition];
                                                        dstG = (dst >> 8) & 0xFF;
                                                        dstRB = (((((colorData & 0x00FF00FF) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                        BufferData[startXPosition] =
                                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                            | (((uint)gammaLutGreen[(((((((colorData & 0xFF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                            | (gammaLutBlue[(dstRB & 0x00FF)]));
                                                        #endregion

                                                    }
                                                    startXPosition++;
                                                    currentColorIndexScaled++;
                                                }
                                            }
                                            #endregion
                                        }
                                        else
                                        {
                                            // incre color index
                                            currentColorIndexScaled += (currentCellData.X - scLastX - 1) * incrementTranformedColorIndexScaled;
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);
                                        #region non-zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion

                                        colorData = builtColors[
                                            currentColorIndexScaled < 0 ?
                                                0 :
                                                (currentColorIndexScaled > ColorIndexIncludeIncrementScale ?
                                                    255 :
                                                    currentColorIndexScaled >> IncrementColorIndexShift)];

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
                                        //    | (uint)((((((((colorData & 0xFF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        //    | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #region gamma apply
                                        dst = BufferData[startXPosition];
                                        dstG = (dst >> 8) & 0xFF;
                                        dstRB = (((((colorData & 0x00FF00FF) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                        BufferData[startXPosition] =
                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (((uint)gammaLutGreen[(((((((colorData & 0xFF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                            | (gammaLutBlue[(dstRB & 0x00FF)]));
                                        #endregion
                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    // incre color index
                                    currentColorIndexScaled += incrementTranformedColorIndexScaled;

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
                    #endregion
                }//GradientStyle.Pad mode
            }// no blending color
            else
            {// has blending color
                if (paint.Style != GradientStyle.Pad)
                {
                    #region optimized for reflect and repeat mode
                    incrementTranformedColorIndexScaled &= ColorIndexIncludeIncrementDoubleMask;
                    if (incrementTranformedColorIndexScaled < 0)
                    {
                        incrementTranformedColorIndexScaled = ColorIndexIncludeIncrementDoubleScale - incrementTranformedColorIndexScaled;
                    }
                    #endregion
                    // when no need to blending, when draw a horizontal line
                    // do not need check the back color, alway setup
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;

                        if (rows[startRowIndex] != null)
                        {
                            #region transform for row
                            currentColorIndexScaled = (int)
                                (currentYTransformed * ColorIndexIncludeIncrementScale);
                            if (currentColorIndexScaled < 0)
                            {
                                currentColorIndexScaled += ColorIndexIncludeIncrementDoubleScale;
                            }
                            currentYTransformed += startRowIncrement;
                            #endregion

                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1))
                                    {
                                        if (scLastCoverage != 0)
                                        {
                                            // fast bit absolute
                                            scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                            #region non-zero checking code
                                            if (scLastCoverage > 255) scLastCoverage = 255;
                                            #endregion

                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;

                                            while (startXPosition < lastXPosition)
                                            {
                                                colorData = builtColors[(currentColorIndexScaled & ColorIndexIncludeIncrementDoubleMask) >> IncrementColorIndexShift];
                                                currentColorIndexScaled += incrementTranformedColorIndexScaled;
                                                calculatedCoverage = (byte)(colorData >> 24);
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);

                                                if (calculatedCoverage >= 254)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    #region blend here
                                                    //dst = BufferData[startXPosition];
                                                    //dstRB = dst & 0x00FF00FF;
                                                    //dstG = (dst >> 8) & 0xFF;

                                                    //BufferData[startXPosition] =
                                                    //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                    //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                    //    | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                    #region gamma apply
                                                    dst = BufferData[startXPosition];
                                                    dstG = (dst >> 8) & 0xFF;
                                                    dstRB = (((((colorData & 0x00FF00FF) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                    BufferData[startXPosition] =
                                                        (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (((uint)gammaLutGreen[(((((((colorData & 0xFF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                        | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                        | ((uint)gammaLutBlue[(dstRB & 0x00FF)]));
                                                    #endregion

                                                    #endregion
                                                }
                                                startXPosition++;
                                                currentColorIndexScaled++;
                                            }
                                            #endregion
                                        }
                                        else
                                        {
                                            // incre color index
                                            currentColorIndexScaled += (currentCellData.X - scLastX - 1) * incrementTranformedColorIndexScaled;
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non-zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion

                                        colorData = builtColors[(currentColorIndexScaled & ColorIndexIncludeIncrementDoubleMask) >> IncrementColorIndexShift];

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
                                        //    | (uint)((((((((colorData & 0xFF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        //    | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #region gamma apply
                                        dst = BufferData[startXPosition];
                                        dstG = (dst >> 8) & 0xFF;
                                        dstRB = (((((colorData & 0x00FF00FF) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                        BufferData[startXPosition] =
                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | (((uint)gammaLutGreen[(((((((colorData & 0xFF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                            | (gammaLutBlue[(dstRB & 0x00FF)]));
                                        #endregion

                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    // incre color index
                                    currentColorIndexScaled += incrementTranformedColorIndexScaled;

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
                    #region GradientStyle.Pad

                    // when no need to blending, when draw a horizontal line
                    // do not need check the back color, alway setup
                    #region filling without blend for horizontal lines
                    startRowIndex--;
                    while (++startRowIndex <= endRowIndex)
                    {
                        currentCoverage = scLastCoverage = scLastX = 0;

                        if (rows[startRowIndex] != null)
                        {
                            #region transform for row
                            currentColorIndexScaled = (int)
                                (currentYTransformed * ColorIndexIncludeIncrementScale);
                            //if (currentColorIndexScaled < 0)
                            //{
                            //    currentColorIndexScaled += ColorIndexIncludeIncrementDoubleScale;
                            //}
                            currentYTransformed += startRowIncrement;
                            #endregion

                            // get first cell in current row
                            currentCellData = rows[startRowIndex].First;
                            if (currentCellData != null)
                            {
                                #region fill current row
                                do
                                {
                                    currentArea = currentCellData.Area;
                                    #region blend horizontal line
                                    if ((currentCellData.X > scLastX + 1))
                                    {
                                        if (scLastCoverage != 0)
                                        {
                                            // fast bit absolute
                                            scLastCoverage = (scLastCoverage ^ (scLastCoverage >> 31)) - (scLastCoverage >> 31);
                                            #region non-zero checking code
                                            if (scLastCoverage > 255) scLastCoverage = 255;
                                            #endregion

                                            #region BLEND HORIZONTAL LINE
                                            // calculate start and end position
                                            startXPosition = BufferStartOffset + startRowIndex * BufferStride + scLastX + 1;
                                            lastXPosition = BufferStartOffset + startRowIndex * BufferStride + currentCellData.X;

                                            while (startXPosition < lastXPosition)
                                            {
                                                //colorData = builtColors[(currentColorIndexScaled & ColorIndexIncludeIncrementDoubleMask) >> IncrementColorIndexShift];
                                                colorData = builtColors[
                                                    currentColorIndexScaled < 0 ?
                                                    0 :
                                                    (currentColorIndexScaled > ColorIndexIncludeIncrementScale ?
                                                    255 :
                                                    currentColorIndexScaled >> IncrementColorIndexShift)];
                                                currentColorIndexScaled += incrementTranformedColorIndexScaled;
                                                calculatedCoverage = (byte)(colorData >> 24);
                                                calculatedCoverage = (byte)((scLastCoverage * calculatedCoverage) >> 8);

                                                if (calculatedCoverage >= 254)
                                                {
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else
                                                {
                                                    #region blend here
                                                    //dst = BufferData[startXPosition];
                                                    //dstRB = dst & 0x00FF00FF;
                                                    //dstG = (dst >> 8) & 0xFF;

                                                    //BufferData[startXPosition] =
                                                    //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                    //    | (uint)((((((((colorData & 0x00FF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                    //    | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                    #region gamma apply
                                                    dst = BufferData[startXPosition];
                                                    dstG = (dst >> 8) & 0xFF;
                                                    dstRB = (((((colorData & 0x00FF00FF) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                    BufferData[startXPosition] =
                                                        (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (((uint)gammaLutGreen[(((((((colorData & 0xFF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                        | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                        | ((uint)gammaLutBlue[(dstRB & 0x00FF)]));
                                                    #endregion

                                                    #endregion
                                                }
                                                startXPosition++;
                                                currentColorIndexScaled++;
                                            }
                                            #endregion
                                        }
                                        else
                                        {
                                            // incre color index
                                            currentColorIndexScaled += (currentCellData.X - scLastX - 1) * incrementTranformedColorIndexScaled;
                                        }
                                    }
                                    #endregion

                                    currentCoverage += currentCellData.Coverage;

                                    #region blend the current cell
                                    tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                    if (tempCover != 0)
                                    {
                                        // fast bit absolute
                                        tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);

                                        #region non-zero checking code
                                        if (tempCover > 255) tempCover = 255;
                                        #endregion

                                        colorData = builtColors[
                                            currentColorIndexScaled < 0 ?
                                                0 :
                                            (currentColorIndexScaled > ColorIndexIncludeIncrementScale ?
                                                255 :
                                                currentColorIndexScaled >> IncrementColorIndexShift)];

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
                                        //    | (uint)((((((((colorData & 0xFF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        //    | (uint)((((((colorData & 0x00FF00FF) - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        #region gamma apply
                                        dst = BufferData[startXPosition];
                                        dstG = (dst >> 8) & 0xFF;
                                        dstRB = (((((colorData & 0x00FF00FF) - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                        BufferData[startXPosition] =
                                            (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                            | ((uint)(gammaLutGreen[(((((((colorData & 0xFF00) >> 8) - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                            | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                            | ((uint)gammaLutBlue[(dstRB & 0x00FF)]));
                                        #endregion

                                        #endregion
                                        #endregion
                                    }
                                    #endregion

                                    // incre color index
                                    currentColorIndexScaled += incrementTranformedColorIndexScaled;

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
                    #endregion
                }
            }//has blending color

            #endregion
        }
        #endregion

        #region On Filling Transformed Diangonal NonZero (transform, gamma)
        /// <summary>
        /// Fill to buffer base rows data information using non-zero rule
        /// </summary>
        /// <param name="paint">linear gradient object</param>
        /// <param name="rows">row data information</param>
        /// <param name="startRowIndex">start row index in row array need to draw</param>
        /// <param name="endRowIndex">end row index in end row array need to draw</param>
        /// <param name="isForward">is diagonal gradient is forward</param>
        /// <param name="gammaLutRed">gamma look up table for red</param>
        /// <param name="gammaLutGreen">gamma look up table for green</param>
        /// <param name="gammaLutBlue">gamma look up table for blue</param>
        void OnFillingTransformedDiagonalNonZero(LinearGradient paint, uint opacity, RowData[] rows, int startRowIndex, int endRowIndex, bool isForward, byte[] gammaLutRed, byte[] gammaLutGreen, byte[] gammaLutBlue)
        {
            throw new NotImplementedException();
        }
        #endregion

        #endregion


        #region EVEN-ODD (!gamma) NotImplementedException
        /// <summary>
        /// Filling row data result from start y index to end y index including transformation
        /// <para>While filling can use CurrentTransformMatrix, or InverterMatrix... to calculate
        /// or access transformation information</para>
        /// </summary>
        /// <param name="paint">paint</param>
        /// <param name="rows">rows</param>
        /// <param name="startYIndex">start y index</param>
        /// <param name="endYIndex">end y index</param>
        protected override void OnFillingTransformedEvenOdd(PaintMaterial paint, RowData[] rows, int startRowIndex, int endRowIndex)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region EVEN-ODD (gamma) NotImplementedException
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

        #endregion

    }
}
