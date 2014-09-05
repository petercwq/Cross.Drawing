


namespace Cross.Drawing.Rasterizers.Analytical
{
    /// <summary>
    /// Rasterizer and filling by solid color
    /// </summary>
    public sealed class ColorRasterizer : AnalyticalRasterizerBase
    {
        #region fill non zero

        #region Without gamma
        /// <summary>
        /// Fill to buffer base rows data information using non zero rule
        /// </summary>
        /// <param name="paint">paint using for fill</param>
        /// <param name="rows">row data information</param>
        /// <param name="startYIndex">start row index in row array need to draw</param>
        /// <param name="endYIndex">end row index in end row array need to draw</param>
        protected override void OnFillingNonZero(PaintMaterial paint, RowData[] rows, int startYIndex, int endYIndex)
        {

            if (!(paint.Paint is ColorPaint))
            {
                //throw new NotImplementedException("Support color paint only");
                NotMatchPaintTypeException.Publish(typeof(ColorPaint), paint.Paint.GetType());
                return;
            }

            Color currentColor = ((ColorPaint)paint.Paint).Color;
            #region private variable for filling
            int currentCoverage, scLastCoverage, scLastX = 0;
            int tempCover = 0;
            int currentArea = 0;
            int lastXPosition = 0;
            int startXPosition = 0;
            CellData currentCellData = null;
            byte calculatedCoverage = 0;

            uint colorData = currentColor.Data;
            uint colorAlpha = (currentColor.A * paint.ScaledOpacity) >> 8;
            if (paint.ScaledOpacity < 256)
            {
                colorData = (colorAlpha << 24) | (colorAlpha & 0x00FFFFFF);
            }

            uint colorG = currentColor.Green;
            uint colorRB = currentColor.RB;

            uint dst, dstRB, dstG;
            #endregion

            if (mOpacityMask != null)
            {
                #region check the y of mask
                if (startYIndex < MaskStartY) startYIndex = MaskStartY;
                if (endYIndex > MaskEndY - 1) endYIndex = MaskEndY - 1;
                #endregion

                #region variable using opacity mask
                int currentMaskY = startYIndex - MaskStartY;
                int currentMaskIndex = 0;
                #endregion

                #region PERFORM FILLING
                startYIndex--;
                while (++startYIndex <= endYIndex)
                {
                    currentCoverage = scLastCoverage = scLastX = 0;

                    if (rows[startYIndex] != null)
                    {
                        // get first cell in current row
                        currentCellData = rows[startYIndex].First;
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
                                    #region this check for non zero case
                                    if (scLastCoverage > 255) scLastCoverage = 255;
                                    #endregion
                                    //fill from currentX position to last x position
                                    scLastCoverage = (byte)((scLastCoverage * colorAlpha) >> 8);

                                    #region BLEND HORIZONTAL LINE
                                    // calculate start and end position

                                    #region modifi for start ,end X position
                                    if (scLastX + 1 < MaskStartX)
                                    {
                                        startXPosition = BufferStartOffset + startYIndex * BufferStride + MaskStartX;
                                        currentMaskIndex = currentMaskY * MaskStride + MaskStartOffset;
                                    }
                                    else
                                    {
                                        startXPosition = BufferStartOffset + startYIndex * BufferStride + scLastX + 1;
                                        currentMaskIndex = currentMaskY * MaskStride + MaskStartOffset + scLastX + 1 - MaskStartX;
                                    }

                                    if (currentCellData.X > MaskEndX)
                                    {
                                        lastXPosition = BufferStartOffset + startYIndex * BufferStride + MaskEndX;
                                    }
                                    else
                                    {
                                        lastXPosition = BufferStartOffset + startYIndex * BufferStride + currentCellData.X;
                                    }

                                    #endregion


                                    while (startXPosition < lastXPosition)
                                    {
                                        if (MaskData[currentMaskIndex] > 0)
                                        {
                                            calculatedCoverage = (byte)((scLastCoverage * (MaskData[currentMaskIndex] + 1)) >> 8);

                                            #region blend here
                                            // because scLastCoverage about 255 when multiply with 255 will have value about 254
                                            if (calculatedCoverage >= 254)
                                            {
                                                // draw only
                                                BufferData[startXPosition] = colorData;
                                            }
                                            else // != 255
                                            {
                                                //calculatedCoverage = (byte)scLastCoverage;
                                                //blending here
                                                dst = BufferData[startXPosition];
                                                dstRB = dst & 0x00FF00FF;
                                                dstG = (dst >> 8) & 0xFF;

                                                BufferData[startXPosition] =
                                                    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                    | (uint)((((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                    | (uint)(((((colorRB - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                            }
                                            #endregion
                                        }
                                        // go next cell
                                        startXPosition++;
                                        currentMaskIndex++;
                                    }

                                    #endregion
                                }
                                #endregion

                                currentCoverage += currentCellData.Coverage;

                                #region blend the current cell
                                // calculate tempcover
                                tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                if (tempCover != 0)
                                {
                                    // fast bit absolute
                                    tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);
                                    #region this check using for non zero
                                    if (tempCover > 255) tempCover = 255;
                                    #endregion
                                    tempCover = (int)((tempCover * colorAlpha) >> 8);
                                    #region blend pixel

                                    if ((currentCellData.X >= MaskStartX) && (currentCellData.X < MaskEndX))
                                    {
                                        #region mask chage
                                        currentMaskIndex = currentMaskY * MaskStride + MaskStartOffset + currentCellData.X - MaskStartX;
                                        #endregion
                                        if (MaskData[currentMaskIndex] > 0)
                                        {
                                            calculatedCoverage = (byte)((tempCover * (MaskData[currentMaskIndex] + 1)) >> 8);
                                            startXPosition = BufferStartOffset + startYIndex * BufferStride + currentCellData.X;

                                            dst = BufferData[startXPosition];
                                            dstRB = dst & 0x00FF00FF;
                                            dstG = (dst >> 8) & 0xFF;
                                            BufferData[startXPosition] =
                                                (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                | (uint)((((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                | (uint)(((((colorRB - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        }
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

                    // increase current mask y
                    currentMaskY++;
                }
                #endregion
            }
            else
            {
                #region  normal filling
                startYIndex--;
                while (++startYIndex <= endYIndex)
                {
                    currentCoverage = scLastCoverage = scLastX = 0;

                    if (rows[startYIndex] != null)
                    {
                        // get first cell in current row
                        currentCellData = rows[startYIndex].First;
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
                                    #region this check for non zero case
                                    if (scLastCoverage > 255) scLastCoverage = 255;
                                    #endregion
                                    //fill from currentX position to last x position
                                    scLastCoverage = (byte)((scLastCoverage * colorAlpha) >> 8);

                                    #region BLEND HORIZONTAL LINE
                                    // calculate start and end position
                                    startXPosition = BufferStartOffset + startYIndex * BufferStride + scLastX + 1;
                                    lastXPosition = BufferStartOffset + startYIndex * BufferStride + currentCellData.X;

                                    // because scLastCoverage about 255 when multiply with 255 will have value about 254
                                    if (scLastCoverage >= 254)
                                    {
                                        // draw only
                                        while (startXPosition < lastXPosition)
                                        {
                                            BufferData[startXPosition++] = colorData;
                                        }
                                    }
                                    else // != 255
                                    {
                                        calculatedCoverage = (byte)scLastCoverage;
                                        //blending here
                                        while (startXPosition < lastXPosition)
                                        {
                                            dst = BufferData[startXPosition];
                                            dstRB = dst & 0x00FF00FF;
                                            dstG = (dst >> 8) & 0xFF;

                                            BufferData[startXPosition] =
                                                (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                | (uint)((((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                | (uint)(((((colorRB - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                            startXPosition++;
                                        }
                                    }
                                    #endregion
                                }
                                #endregion

                                currentCoverage += currentCellData.Coverage;

                                #region blend the current cell
                                // calculate tempcover
                                tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                if (tempCover != 0)
                                {
                                    // fast bit absolute
                                    tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);
                                    #region this check using for non zero
                                    if (tempCover > 255) tempCover = 255;
                                    #endregion
                                    tempCover = (int)((tempCover * colorAlpha) >> 8);
                                    #region blend pixel
                                    //if (tempCover > 255) tempCover = 255;
                                    calculatedCoverage = (byte)tempCover;
                                    startXPosition = BufferStartOffset + startYIndex * BufferStride + currentCellData.X;

                                    dst = BufferData[startXPosition];
                                    dstRB = dst & 0x00FF00FF;
                                    dstG = (dst >> 8) & 0xFF;
                                    BufferData[startXPosition] =
                                        (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        | (uint)((((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        | (uint)(((((colorRB - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
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

        #region include gamma
        /// <summary>
        /// Fill to buffer base rows data information using non zero rule
        /// </summary>
        /// <param name="paint">paint using for fill</param>
        /// <param name="rows">row data information</param>
        /// <param name="startYIndex">start row index in row array need to draw</param>
        /// <param name="endYIndex">end row index in end row array need to draw</param>
        /// <param name="gammaLutRed">gamma look up table for red</param>
        /// <param name="gammaLutGreen">gamma look up table for green</param>
        /// <param name="gammaLutBlue">gamma look up table for blue</param>
        protected override void OnFillingNonZero(PaintMaterial paint, RowData[] rows, int startYIndex, int endYIndex, byte[] gammaLutRed, byte[] gammaLutGreen, byte[] gammaLutBlue)
        {
            if (!(paint.Paint is ColorPaint))
            {
                //throw new NotImplementedException("Support color paint only");
                NotMatchPaintTypeException.Publish(typeof(ColorPaint), paint.Paint.GetType());
                return;
            }
            Color currentColor = ((ColorPaint)paint.Paint).Color;
            #region private variable for filling
            int currentCoverage, scLastCoverage, scLastX = 0;
            int tempCover = 0;
            int currentArea = 0;
            int lastXPosition = 0;
            int startXPosition = 0;
            CellData currentCellData = null;
            byte calculatedCoverage = 0;

            uint colorData = currentColor.Data;
            uint colorAlpha = (currentColor.A * paint.ScaledOpacity) >> 8;
            if (paint.ScaledOpacity < 256)
            {
                colorData = (colorAlpha << 24) | (colorAlpha & 0x00FFFFFF);
            }

            uint colorG = currentColor.Green;
            uint colorRB = currentColor.RB;

            uint dst, dstRB, dstG;
            #endregion
            if (mOpacityMask != null)
            {
                #region check the y of mask
                if (startYIndex < MaskStartY) startYIndex = MaskStartY;
                if (endYIndex > MaskEndY - 1) endYIndex = MaskEndY - 1;
                #endregion

                #region variable using opacity mask
                int currentMaskY = startYIndex - MaskStartY;
                int currentMaskIndex = 0;
                #endregion

                #region PERFORM FILLING
                startYIndex--;
                while (++startYIndex <= endYIndex)
                {
                    currentCoverage = scLastCoverage = scLastX = 0;

                    if (rows[startYIndex] != null)
                    {
                        // get first cell in current row
                        currentCellData = rows[startYIndex].First;
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
                                    #region this check for non zero case
                                    if (scLastCoverage > 255) scLastCoverage = 255;
                                    #endregion
                                    //fill from currentX position to last x position
                                    scLastCoverage = (byte)((scLastCoverage * colorAlpha) >> 8);

                                    #region BLEND HORIZONTAL LINE
                                    // calculate start and end position

                                    #region modifi for start ,end X position
                                    if (scLastX + 1 < MaskStartX)
                                    {
                                        startXPosition = BufferStartOffset + startYIndex * BufferStride + MaskStartX;
                                        currentMaskIndex = currentMaskY * MaskStride + MaskStartOffset;
                                    }
                                    else
                                    {
                                        startXPosition = BufferStartOffset + startYIndex * BufferStride + scLastX + 1;
                                        currentMaskIndex = currentMaskY * MaskStride + MaskStartOffset + scLastX + 1 - MaskStartX;
                                    }

                                    if (currentCellData.X > MaskEndX)
                                    {
                                        lastXPosition = BufferStartOffset + startYIndex * BufferStride + MaskEndX;
                                    }
                                    else
                                    {
                                        lastXPosition = BufferStartOffset + startYIndex * BufferStride + currentCellData.X;
                                    }

                                    #endregion


                                    while (startXPosition < lastXPosition)
                                    {
                                        if (MaskData[currentMaskIndex] > 0)
                                        {
                                            calculatedCoverage = (byte)((scLastCoverage * (MaskData[currentMaskIndex] + 1)) >> 8);

                                            #region blend here
                                            // because scLastCoverage about 255 when multiply with 255 will have value about 254
                                            if (calculatedCoverage >= 254)
                                            {
                                                // draw only
                                                BufferData[startXPosition] = colorData;
                                            }
                                            else // != 255
                                            {
                                                #region blending here
                                                //dst = BufferData[startXPosition];
                                                //dstRB = dst & 0x00FF00FF;
                                                //dstG = (dst >> 8) & 0xFF;

                                                //BufferData[startXPosition] =
                                                //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage] << 24)
                                                //    | (uint)((((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                //    | (uint)(((((colorRB - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                                #region apply gamma here
                                                dst = BufferData[startXPosition];
                                                dstG = (dst >> 8) & 0xFF;
                                                //dstRB = (dst & 0x00FF00FF);
                                                dstRB = ((((colorRB - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                BufferData[startXPosition] =
                                                    (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                    | (((uint)gammaLutGreen[(((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                    | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                    | ((uint)gammaLutBlue[(dstRB & 0x00FF)]))
                                                    ;
                                                #endregion
                                                #endregion
                                            }
                                            #endregion
                                        }
                                        // go next cell
                                        startXPosition++;
                                        currentMaskIndex++;
                                    }

                                    #endregion
                                }
                                #endregion

                                currentCoverage += currentCellData.Coverage;

                                #region blend the current cell
                                // calculate tempcover
                                tempCover = ((currentCoverage << 9) - currentArea) >> 9;
                                if (tempCover != 0)
                                {
                                    // fast bit absolute
                                    tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);
                                    #region this check using for non zero
                                    if (tempCover > 255) tempCover = 255;
                                    #endregion
                                    tempCover = (int)((tempCover * colorAlpha) >> 8);
                                    #region blend pixel

                                    if ((currentCellData.X >= MaskStartX) && (currentCellData.X < MaskEndX))
                                    {
                                        #region mask chage
                                        currentMaskIndex = currentMaskY * MaskStride + MaskStartOffset + currentCellData.X - MaskStartX;
                                        #endregion
                                        if (MaskData[currentMaskIndex] > 0)
                                        {
                                            calculatedCoverage = (byte)((tempCover * (MaskData[currentMaskIndex] + 1)) >> 8);
                                            startXPosition = BufferStartOffset + startYIndex * BufferStride + currentCellData.X;
                                            #region bleding
                                            dst = BufferData[startXPosition];
                                            //dstRB = dst & 0x00FF00FF;
                                            //dstG = (dst >> 8) & 0xFF;
                                            //BufferData[startXPosition] =
                                            //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage] << 24)
                                            //    | (uint)((((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            //    | (uint)(((((colorRB - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                            #region apply gamma here
                                            dst = BufferData[startXPosition];
                                            dstG = (dst >> 8) & 0xFF;
                                            //dstRB = (dst & 0x00FF00FF);
                                            dstRB = ((((colorRB - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                            BufferData[startXPosition] =
                                                (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                | (((uint)gammaLutGreen[(((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                | ((uint)gammaLutBlue[(dstRB & 0x00FF)]))
                                                ;
                                            #endregion
                                            #endregion
                                        }
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

                    // increase current mask y
                    currentMaskY++;
                }
                #endregion
            }
            else
            {
                startYIndex--;
                while (++startYIndex <= endYIndex)
                {
                    currentCoverage = scLastCoverage = scLastX = 0;

                    if (rows[startYIndex] != null)
                    {
                        // get first cell in current row
                        currentCellData = rows[startYIndex].First;
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
                                    #region this check for non zero case
                                    if (scLastCoverage > 255) scLastCoverage = 255;
                                    #endregion
                                    //fill from currentX position to last x position
                                    scLastCoverage = (byte)((scLastCoverage * colorAlpha) >> 8);

                                    #region BLEND HORIZONTAL LINE
                                    // calculate start and end position
                                    startXPosition = BufferStartOffset + startYIndex * BufferStride + scLastX + 1;
                                    lastXPosition = BufferStartOffset + startYIndex * BufferStride + currentCellData.X;
                                    if (scLastCoverage >= 254)
                                    {
                                        // draw only
                                        while (startXPosition < lastXPosition)
                                        {
                                            BufferData[startXPosition++] = colorData;
                                        }
                                    }
                                    else // != 255
                                    {
                                        calculatedCoverage = (byte)scLastCoverage;
                                        //blending here
                                        while (startXPosition < lastXPosition)
                                        {
                                            //dst = BufferData[startXPosition];
                                            //dstRB = dst & 0x00FF00FF;
                                            //dstG = (dst >> 8) & 0xFF;
                                            //BufferData[startXPosition++] =
                                            //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage] << 24)
                                            //    | (uint)((((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            //    | (uint)(((((colorRB - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                            #region apply gamma here
                                            dst = BufferData[startXPosition];
                                            dstG = (dst >> 8) & 0xFF;
                                            //dstRB = (dst & 0x00FF00FF);
                                            dstRB = ((((colorRB - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                            BufferData[startXPosition] =
                                                (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                | (((uint)gammaLutGreen[(((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                | ((uint)gammaLutBlue[(dstRB & 0x00FF)]))
                                                ;
                                            #endregion
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
                                    #region this check for non zero case
                                    if (tempCover > 255) tempCover = 255;
                                    #endregion

                                    if (tempCover > 255) tempCover = 255;
                                    tempCover = (int)((tempCover * colorAlpha) >> 8);
                                    #region blend pixel

                                    calculatedCoverage = (byte)tempCover;
                                    startXPosition = BufferStartOffset + startYIndex * BufferStride + currentCellData.X;

                                    #region apply gamma
                                    dst = BufferData[startXPosition];
                                    dstG = (dst >> 8) & 0xFF;
                                    //dstRB = (dst & 0x00FF00FF);
                                    dstRB = ((((colorRB - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                    BufferData[startXPosition] =
                                        (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        | (((uint)gammaLutGreen[(((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                        | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                        | ((uint)gammaLutBlue[(dstRB & 0x00FF)]))
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
            }
        }
        #endregion
        #endregion

        #region Fill EVEN ODD

        #region without gamma
        /// <summary>
        /// Fill to buffer base rows data information using EvenOdd rule
        /// </summary>
        /// <param name="paint">paint using for fill</param>
        /// <param name="rows">row data information</param>
        /// <param name="startYIndex">start row index in row array need to draw</param>
        /// <param name="endYIndex">end row index in end row array need to draw</param>
        protected override void OnFillingEvenOdd(PaintMaterial paint, RowData[] rows, int startYIndex, int endYIndex)
        {

            if (!(paint.Paint is ColorPaint))
            {
                //throw new NotImplementedException("Support color paint only");
                NotMatchPaintTypeException.Publish(typeof(ColorPaint), paint.Paint.GetType());
                return;
            }
            Color currentColor = ((ColorPaint)paint.Paint).Color;

            #region private variable for filling
            int currentCoverage, scLastCoverage, scLastX = 0;
            int tempCover = 0;
            int currentArea = 0;
            int lastXPosition = 0;
            int startXPosition = 0;
            CellData currentCellData = null;
            byte calculatedCoverage = 0;

            uint colorData = currentColor.Data;
            uint colorAlpha = (currentColor.A * paint.ScaledOpacity) >> 8;
            if (paint.ScaledOpacity < 256)
            {
                colorData = (colorAlpha << 24) | (colorAlpha & 0x00FFFFFF);
            }

            uint colorG = currentColor.Green;
            uint colorRB = currentColor.RB;

            uint dst, dstRB, dstG;
            #endregion

            if (mOpacityMask != null)
            {
                #region check the y of mask
                if (startYIndex < MaskStartY) startYIndex = MaskStartY;
                if (endYIndex > MaskEndY - 1) endYIndex = MaskEndY - 1;
                #endregion

                #region variable using opacity mask
                int currentMaskY = startYIndex - MaskStartY;
                int currentMaskIndex = 0;
                #endregion

                #region PERFORM FILLING
                startYIndex--;
                while (++startYIndex <= endYIndex)
                {
                    currentCoverage = scLastCoverage = scLastX = 0;

                    if (rows[startYIndex] != null)
                    {
                        // get first cell in current row
                        currentCellData = rows[startYIndex].First;
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
                                    //fill from currentX position to last x position
                                    scLastCoverage = (byte)((scLastCoverage * colorAlpha) >> 8);
                                    if (scLastCoverage > 0)
                                    {
                                        #region BLEND HORIZONTAL LINE
                                        // calculate start and end position

                                        #region modifi for start ,end X position
                                        if (scLastX + 1 < MaskStartX)
                                        {
                                            startXPosition = BufferStartOffset + startYIndex * BufferStride + MaskStartX;
                                            currentMaskIndex = currentMaskY * MaskStride + MaskStartOffset;
                                        }
                                        else
                                        {
                                            startXPosition = BufferStartOffset + startYIndex * BufferStride + scLastX + 1;
                                            currentMaskIndex = currentMaskY * MaskStride + MaskStartOffset + scLastX + 1 - MaskStartX;
                                        }

                                        if (currentCellData.X > MaskEndX)
                                        {
                                            lastXPosition = BufferStartOffset + startYIndex * BufferStride + MaskEndX;
                                        }
                                        else
                                        {
                                            lastXPosition = BufferStartOffset + startYIndex * BufferStride + currentCellData.X;
                                        }

                                        #endregion


                                        while (startXPosition < lastXPosition)
                                        {
                                            if (MaskData[currentMaskIndex] > 0)
                                            {
                                                calculatedCoverage = (byte)((scLastCoverage * (MaskData[currentMaskIndex] + 1)) >> 8);

                                                #region blend here
                                                // because scLastCoverage about 255 when multiply with 255 will have value about 254
                                                if (calculatedCoverage >= 254)
                                                {
                                                    // draw only
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else // != 255
                                                {
                                                    //calculatedCoverage = (byte)scLastCoverage;
                                                    //blending here
                                                    dst = BufferData[startXPosition];
                                                    dstRB = dst & 0x00FF00FF;
                                                    dstG = (dst >> 8) & 0xFF;

                                                    BufferData[startXPosition] =
                                                        (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (uint)((((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                        | (uint)(((((colorRB - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                                }
                                                #endregion
                                            }
                                            // go next cell
                                            startXPosition++;
                                            currentMaskIndex++;
                                        }

                                        #endregion
                                    }
                                }
                                #endregion

                                currentCoverage += currentCellData.Coverage;

                                #region blend the current cell
                                // calculate tempcover
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
                                    tempCover = (int)((tempCover * colorAlpha) >> 8);
                                    #region blend pixel

                                    if ((currentCellData.X >= MaskStartX) && (currentCellData.X < MaskEndX))
                                    {
                                        #region mask chage
                                        currentMaskIndex = currentMaskY * MaskStride + MaskStartOffset + currentCellData.X - MaskStartX;
                                        #endregion
                                        if (MaskData[currentMaskIndex] > 0)
                                        {
                                            calculatedCoverage = (byte)((tempCover * (MaskData[currentMaskIndex] + 1)) >> 8);
                                            startXPosition = BufferStartOffset + startYIndex * BufferStride + currentCellData.X;

                                            dst = BufferData[startXPosition];
                                            dstRB = dst & 0x00FF00FF;
                                            dstG = (dst >> 8) & 0xFF;
                                            BufferData[startXPosition] =
                                                (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                | (uint)((((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                | (uint)(((((colorRB - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
                                        }
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

                    // increase current mask y
                    currentMaskY++;
                }
                #endregion
            }
            else
            {
                startYIndex--;
                while (++startYIndex <= endYIndex)
                {
                    currentCoverage = scLastCoverage = scLastX = 0;

                    if (rows[startYIndex] != null)
                    {
                        // get first cell in current row
                        currentCellData = rows[startYIndex].First;
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

                                    //scLastCoverage &= 511;
                                    //if (scLastCoverage > 256)
                                    //{
                                    //    scLastCoverage = 512 - scLastCoverage;
                                    //}

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
                                        startXPosition = BufferStartOffset + startYIndex * BufferStride + scLastX + 1;
                                        lastXPosition = BufferStartOffset + startYIndex * BufferStride + currentCellData.X;

                                        //fill from currentX position to last x position
                                        scLastCoverage = (byte)((scLastCoverage * colorAlpha) >> 8);
                                        if (scLastCoverage >= 254)
                                        {
                                            // draw only
                                            while (startXPosition < lastXPosition)
                                            {
                                                BufferData[startXPosition++] = colorData;
                                            }
                                        }
                                        else // != 255
                                        {
                                            calculatedCoverage = (byte)scLastCoverage;
                                            //blending here
                                            while (startXPosition < lastXPosition)
                                            {
                                                dst = BufferData[startXPosition];
                                                dstRB = dst & 0x00FF00FF;
                                                dstG = (dst >> 8) & 0xFF;

                                                BufferData[startXPosition++] =
                                                    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                    | (uint)((((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                    | (uint)(((((colorRB - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
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
                                //tempCover &= 511;
                                //if (tempCover >= 256)
                                //{
                                //    tempCover = 512 - tempCover;
                                //}

                                #region even odd change
                                tempCover &= 511;
                                if (tempCover >= 256)
                                {
                                    tempCover = 512 - tempCover - 1;
                                }
                                #endregion

                                if (tempCover != 0)
                                {
                                    #region blend pixel
                                    tempCover = (int)((tempCover * colorAlpha) >> 8);

                                    //if (tempCover > 255) tempCover = 255;
                                    calculatedCoverage = (byte)tempCover;
                                    startXPosition = BufferStartOffset + startYIndex * BufferStride + currentCellData.X;

                                    dst = BufferData[startXPosition];
                                    dstRB = dst & 0x00FF00FF;
                                    dstG = (dst >> 8) & 0xFF;
                                    BufferData[startXPosition] =
                                        (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        | (uint)((((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                        | (uint)(((((colorRB - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);
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
            }

        }
        #endregion

        #region including gamma
        /// <summary>
        /// Fill to buffer base rows data information using EvenOdd rule
        /// </summary>
        /// <param name="paint">paint using for fill</param>
        /// <param name="rows">row data information</param>
        /// <param name="startRowIndex">start row index in row array need to draw</param>
        /// <param name="endRowIndex">end row index in end row array need to draw</param>
        /// <param name="gammaLutRed">gamma look up table for red</param>
        /// <param name="gammaLutGreen">gamma look up table for green</param>
        /// <param name="gammaLutBlue">gamma look up table for blue</param>
        protected override void OnFillingEvenOdd(PaintMaterial paint, RowData[] rows, int startYIndex, int endYIndex, byte[] gammaLutRed, byte[] gammaLutGreen, byte[] gammaLutBlue)
        {

            if (!(paint.Paint is ColorPaint))
            {
                //throw new NotImplementedException("Support color paint only");
                NotMatchPaintTypeException.Publish(typeof(ColorPaint), paint.Paint.GetType());
                return;
            }

            Color currentColor = ((ColorPaint)paint.Paint).Color;

            #region private variable for filling
            int currentCoverage, scLastCoverage, scLastX = 0;
            int tempCover = 0;
            int currentArea = 0;
            int lastXPosition = 0;
            int startXPosition = 0;
            CellData currentCellData = null;
            byte calculatedCoverage = 0;

            uint colorData = currentColor.Data;
            uint colorAlpha = (currentColor.A * paint.ScaledOpacity) >> 8;
            if (paint.ScaledOpacity < 256)
            {
                colorData = (colorAlpha << 24) | (colorAlpha & 0x00FFFFFF);
            }

            uint colorG = currentColor.Green;
            uint colorRB = currentColor.RB;

            uint dst, dstRB, dstG;
            #endregion
            if (mOpacityMask != null)
            {
                #region check the y of mask
                if (startYIndex < MaskStartY) startYIndex = MaskStartY;
                if (endYIndex > MaskEndY - 1) endYIndex = MaskEndY - 1;
                #endregion

                #region variable using opacity mask
                int currentMaskY = startYIndex - MaskStartY;
                int currentMaskIndex = 0;
                #endregion

                #region PERFORM FILLING
                startYIndex--;
                while (++startYIndex <= endYIndex)
                {
                    currentCoverage = scLastCoverage = scLastX = 0;

                    if (rows[startYIndex] != null)
                    {
                        // get first cell in current row
                        currentCellData = rows[startYIndex].First;
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
                                    //fill from currentX position to last x position
                                    scLastCoverage = (byte)((scLastCoverage * colorAlpha) >> 8);
                                    if (scLastCoverage > 0)
                                    {
                                        #region BLEND HORIZONTAL LINE
                                        // calculate start and end position

                                        #region modifi for start ,end X position
                                        if (scLastX + 1 < MaskStartX)
                                        {
                                            startXPosition = BufferStartOffset + startYIndex * BufferStride + MaskStartX;
                                            currentMaskIndex = currentMaskY * MaskStride + MaskStartOffset;
                                        }
                                        else
                                        {
                                            startXPosition = BufferStartOffset + startYIndex * BufferStride + scLastX + 1;
                                            currentMaskIndex = currentMaskY * MaskStride + MaskStartOffset + scLastX + 1 - MaskStartX;
                                        }

                                        if (currentCellData.X > MaskEndX)
                                        {
                                            lastXPosition = BufferStartOffset + startYIndex * BufferStride + MaskEndX;
                                        }
                                        else
                                        {
                                            lastXPosition = BufferStartOffset + startYIndex * BufferStride + currentCellData.X;
                                        }

                                        #endregion


                                        while (startXPosition < lastXPosition)
                                        {
                                            if (MaskData[currentMaskIndex] > 0)
                                            {
                                                calculatedCoverage = (byte)((scLastCoverage * (MaskData[currentMaskIndex] + 1)) >> 8);

                                                #region blend here
                                                // because scLastCoverage about 255 when multiply with 255 will have value about 254
                                                if (calculatedCoverage >= 254)
                                                {
                                                    // draw only
                                                    BufferData[startXPosition] = colorData;
                                                }
                                                else // != 255
                                                {
                                                    //calculatedCoverage = (byte)scLastCoverage;
                                                    #region blending here
                                                    //dst = BufferData[startXPosition];
                                                    //dstRB = dst & 0x00FF00FF;
                                                    //dstG = (dst >> 8) & 0xFF;

                                                    //BufferData[startXPosition] =
                                                    //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage] << 24)
                                                    //    | (uint)((((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                    //    | (uint)(((((colorRB - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                                    #region apply gamma
                                                    dst = BufferData[startXPosition];
                                                    dstG = (dst >> 8) & 0xFF;
                                                    //dstRB = (dst & 0x00FF00FF);
                                                    dstRB = ((((colorRB - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                    BufferData[startXPosition] =
                                                        (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                        | (((uint)gammaLutGreen[(((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                        | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                        | ((uint)gammaLutBlue[(dstRB & 0x00FF)]))
                                                        ;
                                                    #endregion

                                                    #endregion
                                                }
                                                #endregion
                                            }
                                            // go next cell
                                            startXPosition++;
                                            currentMaskIndex++;
                                        }

                                        #endregion
                                    }
                                }
                                #endregion

                                currentCoverage += currentCellData.Coverage;

                                #region blend the current cell
                                // calculate tempcover
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
                                    tempCover = (int)((tempCover * colorAlpha) >> 8);
                                    #region blend pixel

                                    if ((currentCellData.X >= MaskStartX) && (currentCellData.X < MaskEndX))
                                    {
                                        #region mask chage
                                        currentMaskIndex = currentMaskY * MaskStride + MaskStartOffset + currentCellData.X - MaskStartX;
                                        #endregion
                                        if (MaskData[currentMaskIndex] > 0)
                                        {
                                            calculatedCoverage = (byte)((tempCover * (MaskData[currentMaskIndex] + 1)) >> 8);
                                            startXPosition = BufferStartOffset + startYIndex * BufferStride + currentCellData.X;
                                            #region blending
                                            //dst = BufferData[startXPosition];
                                            //dstRB = dst & 0x00FF00FF;
                                            //dstG = (dst >> 8) & 0xFF;
                                            //BufferData[startXPosition] =
                                            //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage] << 24)
                                            //    | (uint)((((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                            //    | (uint)(((((colorRB - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                            #region apply gamma
                                            dst = BufferData[startXPosition];
                                            dstG = (dst >> 8) & 0xFF;
                                            //dstRB = (dst & 0x00FF00FF);
                                            dstRB = ((((colorRB - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                            BufferData[startXPosition] =
                                                (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                | (((uint)gammaLutGreen[(((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                | ((uint)gammaLutBlue[(dstRB & 0x00FF)]))
                                                ;
                                            #endregion
                                            #endregion
                                        }
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

                    // increase current mask y
                    currentMaskY++;
                }
                #endregion
            }
            else
            {
                startYIndex--;
                while (++startYIndex <= endYIndex)
                {
                    currentCoverage = scLastCoverage = scLastX = 0;

                    if (rows[startYIndex] != null)
                    {
                        // get first cell in current row
                        currentCellData = rows[startYIndex].First;
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
                                    //scLastCoverage &= 511;
                                    //if (scLastCoverage > 256)
                                    //{
                                    //    scLastCoverage = 512 - scLastCoverage;
                                    //}
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
                                        startXPosition = BufferStartOffset + startYIndex * BufferStride + scLastX + 1;
                                        lastXPosition = BufferStartOffset + startYIndex * BufferStride + currentCellData.X;

                                        //fill from currentX position to last x position
                                        scLastCoverage = (byte)((scLastCoverage * colorAlpha) >> 8);
                                        if (scLastCoverage >= 254)
                                        {
                                            // draw only
                                            while (startXPosition < lastXPosition)
                                            {
                                                BufferData[startXPosition++] = colorData;
                                            }
                                        }
                                        else // != 255
                                        {
                                            calculatedCoverage = (byte)scLastCoverage;
                                            //blending here
                                            while (startXPosition < lastXPosition)
                                            {
                                                //dst = BufferData[startXPosition];
                                                //dstRB = dst & 0x00FF00FF;
                                                //dstG = (dst >> 8) & 0xFF;

                                                //BufferData[startXPosition++] =
                                                //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage] << 24)
                                                //    | (uint)((((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                                //    | (uint)(((((colorRB - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                                dst = BufferData[startXPosition];
                                                dstG = (dst >> 8) & 0xFF;
                                                //dstRB = (dst & 0x00FF00FF);
                                                dstRB = ((((colorRB - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                                BufferData[startXPosition] =
                                                    (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                                    | (((uint)gammaLutGreen[(((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                                    | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                                    | ((uint)gammaLutBlue[(dstRB & 0x00FF)]))
                                                    ;
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
                                // fast bit absolute
                                tempCover = (tempCover ^ (tempCover >> 31)) - (tempCover >> 31);
                                //tempCover &= 511;
                                //if (tempCover > 256)
                                //{
                                //    tempCover = 512 - tempCover;
                                //}

                                #region even odd change
                                tempCover &= 511;
                                if (tempCover >= 256)
                                {
                                    tempCover = 512 - tempCover - 1;
                                }
                                #endregion

                                if (tempCover != 0)
                                {
                                    #region blend pixel
                                    tempCover = (int)((tempCover * colorAlpha) >> 8);

                                    //if (tempCover > 255) tempCover = 255;
                                    calculatedCoverage = (byte)tempCover;
                                    startXPosition = BufferStartOffset + startYIndex * BufferStride + currentCellData.X;

                                    //dst = BufferData[startXPosition];
                                    //dstRB = dst & 0x00FF00FF;
                                    //dstG = (dst >> 8) & 0xFF;
                                    //BufferData[startXPosition] =
                                    //    (uint)(AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage] << 24)
                                    //    | (uint)((((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) << 8) & 0x0000FF00)
                                    //    | (uint)(((((colorRB - dstRB) * calculatedCoverage) >> 8) + dstRB) & 0x00FF00FF);

                                    dst = BufferData[startXPosition];
                                    dstG = (dst >> 8) & 0xFF;
                                    //dstRB = ((((colorRB - (dst & 0x00FF00FF)) * calculatedCoverage) >> 8) + (dst & 0x00FF00FF));

                                    dstRB = (dst & 0x00FF00FF);
                                    dstRB = ((((colorRB - dstRB) * calculatedCoverage) >> 8) + dstRB);

                                    BufferData[startXPosition] =
                                        (uint)((AlphaCache[(((dst >> 24) & 0xFF) << 8) + calculatedCoverage])
                                        | (((uint)gammaLutGreen[(((((colorG - dstG) * calculatedCoverage) >> 8) + dstG) & 0xFF)] << 8))
                                        | ((uint)gammaLutRed[(dstRB & 0x00FF0000) >> 16] << 16)
                                        | (gammaLutBlue[(dstRB & 0x00FF)]))
                                        ;
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
            }
        }
        #endregion
        #endregion
    }
}
