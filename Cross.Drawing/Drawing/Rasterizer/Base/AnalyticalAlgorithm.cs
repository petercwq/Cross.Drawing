


namespace Cross.Drawing.Rasterizers.Analytical
{
    /// <summary>
    /// Class to implement analytical algorithm and clipping for rasterizer
    /// </summary>
    public /*internal*/ class AnalyticalAlgorithmImplement
    {
        #region Constructors
        /// <summary>
        /// Default constructor for AnalyticalAlgorithm
        /// </summary>
        public AnalyticalAlgorithmImplement()
        { }
        #endregion

        #region const for clipping
        ///// -------+--------+--------
        /////        |        |
        /////  0110  |  0010  | 0011
        /////    6   |    2   |   3
        ///// -------+--------+-------- yMax
        /////        |        |
        /////  0100  |  0000  | 0001
        /////    4   |    0   |   1
        ///// -------+--------+-------- yMin
        /////        |        |
        /////  1100  |  1000  | 1001
        /////  12(C) |  8 (8) | 9 (9)
        ///// -------+--------+--------
        /////       xMin    xMax
        ///// 
        ///// NOTE THAT , Y IS RESERVED IN THIS CASE

        /// <summary>
        /// when X in less than X Min
        /// 4 (0100)
        /// </summary>
        public const int XMinClippingFlag = 4;

        /// <summary>
        /// When X greater than X Max
        /// 1 (0001)
        /// </summary>
        public const int XMaxClippingFlag = 1;

        /// <summary>
        /// When y less than Y Min
        /// 8 (1000)
        /// </summary>
        public const int YMinClippingFlag = 8;

        /// <summary>
        /// When y greater than Y Max
        /// 2 (0010)
        /// </summary>
        public const int YMaxClippingFlag = 2;

        /// <summary>
        /// Clipping modified to width and heigt
        /// </summary>
        public const double ClippingEpsilon = 1.0 / 256.0;
        #endregion

        #region CONST USING FOR DRAWING
        /// <summary>
        /// Pixel shift
        /// </summary>
        public const int PixelShift = 8;
        public const int PixelScale = 1 << PixelShift;
        public const int PixelMask = PixelScale - 1;
        #endregion

        #region field for drawing
        /// <summary>
        /// Saving min x index
        /// </summary>
        protected int CurrentStartXIndex = 0;

        /// <summary>
        /// saving max x index
        /// </summary>
        protected int CurrentEndXIndex = 0;

        /// <summary>
        /// Saving current start y, this using when approach 2
        /// </summary>
        public int CurrentStartYIndex = 0;

        /// <summary>
        /// Saving current end y position,
        /// this using when approach 2
        /// </summary>
        public int CurrentEndYIndex = 0;

        /// <summary>
        /// saving current x position
        /// </summary>
        protected double CurrentXPosition = 0;

        /// <summary>
        /// saving current y position
        /// </summary>
        protected double CurrentYPosition = 0;

        /// <summary>
        /// saving current position flag
        /// </summary>
        protected int CurrentPositionFlag;
        #endregion

        #region buffer width, height
        /// <summary> 
        /// Width of buffer data
        /// </summary>
        public int DestBufferWidth = 0;

        /// <summary>
        /// Height of buffer data
        /// </summary>
        public int DestBufferHeight = 0;
        #endregion

        #region Prepare rows
        /// <summary>
        /// Prepare row for rasterize, in range start Y to end Y.
        /// </summary>
        /// <param name="startYIndex">start y index</param>
        /// <param name="endYIndex">end row index</param>
        public void PrepareRows(int startYIndex, int endYIndex)
        {
            PrepareRows(startYIndex);
            PrepareRows(endYIndex + 2);
        }

        /// <summary>
        /// Prepare row for rasterize, so that current row always ready to use
        /// </summary>
        /// <param name="startYIndex"></param>
        /// <param name="endYIndex"></param>
        public void PrepareRows(int row)
        {
            if ((Rows == null) || (Rows.Length < DestBufferHeight + 1))
            {
                Rows = new RowData[DestBufferHeight + 1];
            }

            // clip row first
            if (row > ClippingBoxYMax) row = (int)ClippingBoxYMax;
            else if (row < ClippingBoxYMin) row = (int)ClippingBoxYMin;

            // determine min,max y
            if (row < CurrentStartYIndex)
            {
                int oldStartY = CurrentStartYIndex;
                CurrentStartYIndex = row;

                if (row > CurrentEndYIndex)
                {
                    // when the first time drawing, row > current end rowYIndex
                    CurrentEndYIndex = row;
                    Rows[CurrentStartYIndex] = new RowData();
                }
                else
                {
                    // create new from start to old start y
                    for (; row < oldStartY; row++)
                    {
                        Rows[row] = new RowData();
                    }
                }
            }
            else if (row + 1 > CurrentEndYIndex)
            {
                int olfEndY = CurrentEndYIndex;
                CurrentEndYIndex = row + 1;
                if (row < Rows.Length - 1) row++;
                for (; row > olfEndY; row--)
                {
                    Rows[row] = new RowData();
                }
            }
        }
        #endregion

        #region DRAW LINE TO TEMP CANVAS ( in this case, drawing to Rows array)
        /// <summary>
        /// Contain all data that rasterized
        /// </summary>
        public RowData[] Rows = null;

        /// <summary>
        /// Add a line to result row array.
        /// This method need all coordinate has been scaled ( mean multiply for Pixel Scale)
        /// </summary>
        /// <param name="x1Scaled">x coordinate of point 1 ( x * PixelScale)</param>
        /// <param name="y1Scaled">y coordinate of point 1 ( x * PixelScale)</param>
        /// <param name="x2Scaled">x coordinate of point 2 ( x * PixelScale)</param>
        /// <param name="y2Scaled">y coordinate of point 2 ( x * PixelScale)</param>
        /// <remarks>
        /// double coordinate will be convert by using
        /// method upscale() to convert to int before add to rasterizer
        /// Not implement horizontal lines
        /// </remarks>
        protected virtual void DrawScaledLine(int x1Scaled, int y1Scaled, int x2Scaled, int y2Scaled)
        {
#if DEBUG
            int min = -1;
            if ((x1Scaled < min) || (y1Scaled < min) || (x2Scaled < min) || (y2Scaled < min))
            {
                Log.Warning(string.Format("x1: {0} x2: {1} y1: {2} y2: {3}", x1Scaled, x2Scaled, y1Scaled, y2Scaled));
            }
#endif
            int CurrentRowIndex = 0;
            int currentRasterizeX = 0;
            CellData CurrentCell = null, TempCell = null;

            // calculate some property of line
            int slopeByX = (x2Scaled == x1Scaled) ? 0 : (x2Scaled - x1Scaled) < 0 ? -1 : 1;

            #region make sure always draw from left to right ( equal is kept)
            if (slopeByX < 0)
            {
                // switch x1,y1 with x2,y2
                int tempSwitch = x1Scaled;
                x1Scaled = x2Scaled;
                x2Scaled = tempSwitch;

                tempSwitch = y1Scaled;
                y1Scaled = y2Scaled;
                y2Scaled = tempSwitch;
            }
            #endregion

            #region calculate some know value
            int exactlyX1 = x1Scaled >> PixelShift;
            int exactlyX2 = x2Scaled >> PixelShift;
            int exactlyY1 = y1Scaled >> PixelShift;
            int exactlyY2 = y2Scaled >> PixelShift;
            int firstY1 = y1Scaled & PixelMask;
            int firstY2 = y2Scaled & PixelMask;
            int firstX1 = x1Scaled & PixelMask;
            int firstX2 = x2Scaled & PixelMask;
            #endregion

            #region init value for drawing line
            int slopeByY = 0;
            CurrentRowIndex = exactlyY1;
            currentRasterizeX = exactlyX1;
            // special case for start point, may be out side of prepare range
            if (Rows[CurrentRowIndex] == null) Rows[CurrentRowIndex] = new RowData();
            CurrentCell = Rows[CurrentRowIndex].GoToCell(exactlyX1);
            if (y2Scaled > y1Scaled)
            {
                slopeByY = 1;
            }
            else if (y2Scaled < y1Scaled)
            {
                slopeByY = -1;
            }
            #endregion

            //int slope = slopeByX * slopeByY;
            if (slopeByX * slopeByY == 0) // vertical or horizontal
            {
                #region when vertical line
                // when in same cell
                if (exactlyY1 == exactlyY2)
                {
                    CurrentCell.Coverage += y2Scaled - y1Scaled;
                    CurrentCell.Area += (y2Scaled - y1Scaled) * 2 * firstX1;
                }
                else
                {
                    #region  when top -> bottom
                    if (y2Scaled > y1Scaled)
                    {
                        if (firstX1 == 0) // at the right side
                        {
                            firstX1 = 1;
                        }
                        #region draw first
                        CurrentCell.Coverage += PixelScale - firstY1;
                        CurrentCell.Area += (PixelScale - firstY1) * 2 * firstX1;
                        #endregion
                        #region drawing cells
                        // calculate number of cell
                        int totalRows = exactlyY2 - exactlyY1 - 1;
                        while (totalRows-- > 0)
                        {
                            CurrentCell = Rows[++CurrentRowIndex].GoToCell(currentRasterizeX);
                            CurrentCell.Coverage += PixelScale;
                            CurrentCell.Area += firstX1 << 9;

                        }
                        #endregion
                        #region draw last
                        if (firstY2 > 0)
                        {
                            CurrentCell = Rows[++CurrentRowIndex].GoToCell(currentRasterizeX);
                            CurrentCell.Coverage += firstY2;
                            CurrentCell.Area += firstY2 * 2 * firstX1;
                        }
                        #endregion
                    }
                    #endregion
                    #region bottom ->top
                    else
                    {
                        firstX1 = PixelMask - firstX1;
                        if (firstX1 == 0) // at the right side
                        {
                            firstX1 = 1;
                        }
                        #region draw first
                        if (firstY1 != 0)
                        {
                            CurrentCell.Coverage -= firstY1;
                            CurrentCell.Area -= firstY1 * 2 * (PixelScale - firstX1);
                        }
                        #endregion
                        #region drawing cells
                        // calculate number of cell
                        int totalRows = exactlyY1 - exactlyY2 - 1;
                        while (totalRows-- > 0)
                        {
                            CurrentCell = Rows[--CurrentRowIndex].GoToCell(currentRasterizeX);
                            CurrentCell.Coverage -= PixelScale;
                            CurrentCell.Area -= ((PixelScale - firstX1) << 9);
                        }
                        #endregion
                        #region draw last
                        if (firstY2 < PixelScale)
                        {
                            CurrentCell = Rows[--CurrentRowIndex].GoToCell(currentRasterizeX);
                            CurrentCell.Coverage += firstY2 - PixelScale;
                            CurrentCell.Area += (firstY2 - PixelScale) * 2 * (PixelScale - firstX1);
                        }
                        #endregion
                    }
                    #endregion
                }
                // exist function
                return;
                #endregion
            }
            else
            {
                // draw horizontal lines
                #region when in single cell
                if (exactlyX1 == exactlyX2)
                {
                    if (exactlyY1 == exactlyY2)
                    {
                        CurrentCell.Coverage += slopeByX * (y2Scaled - y1Scaled);
                        CurrentCell.Area += slopeByX * (y2Scaled - y1Scaled) * (firstX1 + firstX2);
                        return;
                    }
                }
                #endregion

                #region else ( NORMAL CASE)
                int xFrom = 0, xTo = 0, yFrom = 0, yTo = 0, xCurrentMax = 0, yCurrentMax = 0;

                #region normal line calculation here
                // using x = ( invAlpha * (y-beta) ) >>LineApproxiateScale;
                double invAlpha = (double)(x2Scaled - x1Scaled) / (y2Scaled - y1Scaled);
                // using y = ((alpha * x ) >> LineApproxiateScale) + beta;
                double alpha = (double)(y2Scaled - y1Scaled) / (x2Scaled - x1Scaled);
                // beta
                double beta = ((double)y1Scaled - ((alpha) * x1Scaled));
                #endregion

                #region draw horizontal lines
                #region when slopeY>0
                if (slopeByY > 0)
                {
                    // from left to right
                    // first cell at all
                    xFrom = x1Scaled;
                    yFrom = y1Scaled;
                    // from the first row
                    yCurrentMax = (exactlyY1) << PixelShift;
                    //while (true)
                    while (true)
                    {
                        #region rendering in row
                        // calculate max X  that current line can be draw to int way
                        yCurrentMax += PixelScale;
                        #region check if end
                        // when current Max Y is greater than last y2=> change it
                        if ((yCurrentMax >= y2Scaled) || (xCurrentMax >= x2Scaled))
                        {
                            // all in one row
                            yCurrentMax = y2Scaled;
                            xCurrentMax = x2Scaled;
                        }
                        else
                        {
                            xCurrentMax = (int)(invAlpha * (yCurrentMax - beta));
                        }
                        #endregion
                        xTo = (exactlyX1) << PixelShift;
                        while (true)
                        {
                            #region calculate xTo,yTo
                            // draw horizontal line from x1,y1 to xCurrentMax,yCurrentMax
                            xTo += PixelScale;
                            // check if max x, calculate need value and break
                            if (xTo >= xCurrentMax)
                            {
                                xTo = xCurrentMax;
                                yTo = yCurrentMax;
                                #region calculate and set coverage + area
                                CurrentCell.Coverage += slopeByX * (yTo - yFrom);
                                CurrentCell.Area += slopeByX * (yTo - yFrom) *
                                    (((xFrom & PixelMask) + ((xTo - 1) & PixelMask)) + 1);
                                #endregion
                                // continue to next row
                                break;
                            }
                            else
                            {
                                yTo = (int)((alpha * xTo) + beta);
                            }
                            #endregion

                            #region calculate and set coverage + area
                            CurrentCell.Coverage +=
                                slopeByX * (yTo - yFrom);
                            CurrentCell.Area +=
                                slopeByX * (yTo - yFrom) * ((((xFrom) & PixelMask) + ((xTo - 1) & PixelMask)) + 1);
                            #endregion

                            xFrom = xTo;
                            yFrom = yTo;
                            exactlyX1++;
                            #region increase cell
                            ++currentRasterizeX;
                            if (CurrentCell.Next == null)
                            {
                                CurrentCell.Next = new CellData(currentRasterizeX);
                            }
                            else if (CurrentCell.Next.X > currentRasterizeX)
                            {
                                TempCell = CurrentCell.Next;
                                CurrentCell.Next = new CellData(currentRasterizeX);
                                CurrentCell.Next.Next = TempCell;
                            }
                            CurrentCell = CurrentCell.Next;
                            #endregion
                        }
                        #endregion

                        #region check if end and increase row
                        if (++exactlyY1 > exactlyY2)
                        {
                            break;
                        }
                        //increase yFrom ( in case calculation not correct )
                        yFrom = yCurrentMax;
                        xFrom = xCurrentMax;

                        // when cell is not same as expect
                        if ((xFrom >> PixelShift) - exactlyX1 != 0)
                        {
                            // go to next cell
                            CurrentCell = Rows[++CurrentRowIndex].GoToCell(++currentRasterizeX);
                            exactlyX1++;
                        }
                        else
                        {
                            // go to next cell
                            CurrentCell = Rows[++CurrentRowIndex].GoToCell(currentRasterizeX);
                        }
                        #endregion
                    }

                }
                #endregion
                #region when slopeByY<0 ( bottom to top)
                else
                {
                    // from left to right
                    // first cell at all
                    xFrom = x1Scaled;
                    yFrom = y1Scaled;
                    if (firstY1 == 0)
                    {
                        exactlyY1--;
                        CurrentCell = Rows[--CurrentRowIndex].GoToCell(currentRasterizeX);
                    }
                    yCurrentMax = (exactlyY1 + 1) * PixelScale;
                    while (true)
                    {
                        #region rendering rows
                        // calculate max X  that current line can be draw to int way
                        yCurrentMax -= PixelScale;
                        xCurrentMax = (int)(invAlpha * (yCurrentMax - beta));

                        #region check if end
                        // when current Max Y is greater than last y2=> change it
                        if (xCurrentMax >= x2Scaled)
                        {
                            // all in one row
                            yCurrentMax = y2Scaled;
                            xCurrentMax = x2Scaled;
                        }
                        #endregion

                        xTo = (exactlyX1) * PixelScale;

                        while (true)
                        {
                            #region calculate xTo,yTo
                            // draw horizontal line from x1,y1 to xCurrentMax,yCurrentMax
                            xTo += PixelScale;

                            // get max only
                            if (xTo >= xCurrentMax)
                            {
                                xTo = xCurrentMax;
                                yTo = yCurrentMax;
                                #region calculate and set coverage + area
                                CurrentCell.Coverage += slopeByX * (yTo - yFrom);
                                CurrentCell.Area += slopeByX * (yTo - yFrom) * (((xFrom & PixelMask) + ((xTo - 1) & PixelMask)) + 1);
                                #endregion
                                break;
                            }
                            else
                            {
                                // caculate yTo from xTo
                                yTo = (int)((alpha * xTo) + beta);
                            }
                            #endregion
                            #region calculate and set coverage + area
                            CurrentCell.Coverage += slopeByX * (yTo - yFrom);
                            CurrentCell.Area += slopeByX * (yTo - yFrom) * (((xFrom & PixelMask) + ((xTo - 1) & PixelMask)) + 1);
                            #endregion
                            xFrom = xTo;
                            yFrom = yTo;
                            exactlyX1++;

                            #region increase cell
                            ++currentRasterizeX;
                            if (CurrentCell.Next == null)
                            {
                                CurrentCell.Next = new CellData(currentRasterizeX);
                            }
                            else if (CurrentCell.Next.X > currentRasterizeX)
                            {
                                TempCell = CurrentCell.Next;
                                CurrentCell.Next = new CellData(currentRasterizeX);
                                CurrentCell.Next.Next = TempCell;
                            }
                            CurrentCell = CurrentCell.Next;
                            #endregion
                        }

                        #endregion
                        #region check if end
                        if (--exactlyY1 < exactlyY2)
                        {
                            break;
                        }
                        yFrom = yCurrentMax;
                        xFrom = xCurrentMax;
                        if ((xFrom >> PixelShift) - exactlyX1 != 0)
                        {
                            CurrentCell = Rows[--CurrentRowIndex].GoToCell(++currentRasterizeX);
                            exactlyX1++;
                        }
                        else
                        {
                            CurrentCell = Rows[--CurrentRowIndex].GoToCell(currentRasterizeX);
                        }
                        #endregion
                    }
                }
                #endregion
                #endregion

                #endregion
            }
        }

        /// <summary>
        /// Draw line without scaled
        /// </summary>
        /// <param name="x1">x1</param>
        /// <param name="y1">y1</param>
        /// <param name="x2">x2</param>
        /// <param name="y2">y2</param>
        protected virtual void DrawLine(double x1, double y1, double x2, double y2)
        {
            DrawScaledLine(
                (int)(x1 * PixelScale + 0.5),
                (int)(y1 * PixelScale + 0.5),
                (int)(x2 * PixelScale + 0.5),
                (int)(y2 * PixelScale + 0.5));

            //Cross.Log.Debug("Draw line ({0:0.##},{1:0.##}) to ({2:0.##},{3:0.##})", x1, y1, x2, y2);
        }
        #endregion

        #region Set clip box


        /// <summary>
        /// Check if clip box out side bound
        /// </summary>
        internal bool IsClipBoxOutSideBound = false;

        /// <summary>
        /// Clipping box x min value
        /// </summary>
        protected double ClippingBoxXMin = 0;
        /// <summary>
        /// Clipping box x max value
        /// </summary>
        protected double ClippingBoxXMax = 0;

        /// <summary>
        /// Clipping box y min value
        /// </summary>
        protected double ClippingBoxYMin = 0;

        /// <summary>
        /// Clipping box y max value
        /// </summary>
        protected double ClippingBoxYMax = 0;

        /// <summary>
        /// Set clipping box for current drawer.
        /// </summary>
        /// <param name="left">start x of box</param>
        /// <param name="top">start y of box</param>
        /// <param name="right">right of box</param>
        /// <param name="bottom">height of box</param>
        public void SetClip(double left, double top, double right, double bottom)
        {
            ClippingBoxXMin = left;
            if (ClippingBoxXMin < 0)
            {
                ClippingBoxXMin = 0;
            }
            else if (ClippingBoxXMin >= DestBufferWidth)
            {
                IsClipBoxOutSideBound = true;
                return;
            }

            ClippingBoxYMin = top;
            if (ClippingBoxYMin < 0)
            {
                ClippingBoxYMin = 0;
            }
            else if (ClippingBoxYMin >= DestBufferHeight)
            {
                IsClipBoxOutSideBound = true;
                return;
            }

            ClippingBoxXMax = right;
            if (ClippingBoxXMax >= DestBufferWidth)
            {
                ClippingBoxXMax = DestBufferWidth - ClippingEpsilon;
            }
            ClippingBoxYMax = bottom;
            if (ClippingBoxYMax >= DestBufferHeight)
            {
                ClippingBoxYMax = DestBufferHeight - ClippingEpsilon;
            }

            IsClipBoxOutSideBound = false;
        }
        #endregion

        #region Draw clipped line


        /// <summary>
        /// draw and clip lines from (currentXPosition,currentYPosition) to (xTo,yTo)
        /// coordinate in this case is not scaled coordinate.
        /// </summary>
        /// <param name="xTo">end point of line</param>
        /// <param name="yTo">end point of line</param>
        /// <remarks>
        /// Before the first time call this method, calculate and assign value for
        /// currentXPosition,currentYPosition,currentPositionFlag for the first point of polygon
        /// </remarks>
        protected void DrawAndClippedLine(double xTo, double yTo)
        {
            // when same point, not draw
            if ((xTo == CurrentXPosition) && (yTo == CurrentYPosition)) return;

            #region check flag for end point of line
            int endPointClipping =
                ((xTo > ClippingBoxXMax) ? XMaxClippingFlag :
                (xTo < ClippingBoxXMin) ? XMinClippingFlag : 0)

                |

                ((yTo > ClippingBoxYMax) ? YMaxClippingFlag :
                (yTo < ClippingBoxYMin) ? YMinClippingFlag : 0)
                ;
            #endregion

            #region base on position of start and end, draw needed rendering lines
            /*
             * This flag include flag 
             *      _ start point at 4 last bits
             *      _ end point at 4 first bits
             */
            double lastDrawingX, lastDrawingY;
            double dx = xTo - CurrentXPosition;
            double dy = yTo - CurrentYPosition;

            int currentLinePosition = (((int)CurrentPositionFlag) << 4) | (int)endPointClipping;
            switch (currentLinePosition)
            {
                #region when both are in visible range
                case 0:
                    DrawLine(CurrentXPosition, CurrentYPosition, xTo, yTo);
                    break;
                #endregion

                #region when first point is in view port( first 4 bit is 0)
                #region end point in region 1
                case 0x01:
                    //      yTo  > yMIN, yTo <yMAX
                    //      xTo > xMAX
                    // first draw from currentXPos,currentYPos to the cut of line end right bound box
                    // then draw a line along the right bound of box

                    lastDrawingY = yTo + dy * (ClippingBoxXMax - xTo) / dx;
                    //lastDrawingX = boxXMax;
                    //DrawLine(currentXPosition, currentYPosition, lastDrawingX, lastDrawingY);
                    DrawLine(CurrentXPosition, CurrentYPosition, ClippingBoxXMax, lastDrawingY);
                    // draw vertical line
                    DrawLine(ClippingBoxXMax, lastDrawingY, ClippingBoxXMax, yTo);
                    break;
                #endregion

                #region end point in region 2
                case 0x02:
                    /* xTo > xmin, xto<xmax
                     * yto > yMax
                     */
                    // find the cut to top of box
                    lastDrawingX = xTo + dx * (ClippingBoxYMax - yTo) / dy;
                    //lastDrawingY = boxYMax;
                    //DrawLine(currentXPosition, currentYPosition, lastDrawingX, lastDrawingY);
                    DrawLine(CurrentXPosition, CurrentYPosition, lastDrawingX, ClippingBoxYMax);
                    break;
                #endregion

                #region end point in region 3
                case 0x03:
                    /*yto > yMax
                     * xto >xmax
                     * top-right
                     */
                    // clip right first
                    lastDrawingY = yTo + dy * (ClippingBoxXMax - xTo) / dx;
                    if (lastDrawingY > ClippingBoxYMax)
                    {
                        // clip top and draw 1 line only
                        // now last drawing Y is ymax
                        // last drawing x is the cut to the top
                        lastDrawingX = xTo + dx * (ClippingBoxYMax - yTo) / dy;
                        DrawLine(CurrentXPosition, CurrentYPosition, lastDrawingX, ClippingBoxYMax);
                    }
                    else
                    {
                        // not need clip but draw 2 line
                        // draw from first point to right side of box
                        DrawLine(CurrentXPosition, CurrentYPosition, ClippingBoxXMax, lastDrawingY);
                        // draw line along right side of box to y max
                        DrawLine(ClippingBoxXMax, lastDrawingY, ClippingBoxXMax, ClippingBoxYMax);
                    }
                    break;
                #endregion

                #region end point in region 0x04
                case 0x04:
                    /* yTo >yMin,yTo<yMax
                     * xTo <xMin
                     */
                    // clip end point on left
                    // drawing X is bix min X
                    // drawing Y calculated
                    lastDrawingY = yTo + dy * (ClippingBoxXMin - xTo) / dx;
                    DrawLine(CurrentXPosition, CurrentYPosition, ClippingBoxXMin, lastDrawingY);
                    // draw vertical line
                    DrawLine(ClippingBoxXMin, lastDrawingY, ClippingBoxXMin, yTo);
                    break;
                #endregion

                #region end point in region 0x06
                case 0x06:
                    /*yTo > yMax
                     * xTo < xMin
                     */
                    // clip left first
                    lastDrawingY = yTo + dy * (ClippingBoxXMin - xTo) / dx;
                    if (lastDrawingY > ClippingBoxYMax)
                    {
                        // clip top
                        // draw line to box YMax only, and recalculate last drawing X
                        lastDrawingX = xTo + dx * (ClippingBoxYMax - yTo) / dy;
                        DrawLine(CurrentXPosition, CurrentYPosition, lastDrawingX, ClippingBoxYMax);
                    }
                    else
                    {
                        // not need clip but draw 2 line
                        // draw from first point to right side of box
                        DrawLine(CurrentXPosition, CurrentYPosition, ClippingBoxXMin, lastDrawingY);
                        // draw line along right side of box to yTO
                        DrawLine(ClippingBoxXMin, lastDrawingY, ClippingBoxXMin, ClippingBoxYMax);
                    }
                    break;
                #endregion

                #region end point in region 0x08
                case 0x08:
                    /* xTo>xMin , xTo<xMax
                     * yTo < yMin
                     */
                    // recalculate the drawing x at ymin
                    lastDrawingX = xTo + dx * (ClippingBoxYMin - yTo) / dy;
                    DrawLine(CurrentXPosition, CurrentYPosition, lastDrawingX, ClippingBoxYMin);
                    break;
                #endregion

                #region end point in region 0x09
                case 0x09:
                    /* x > xMax
                     * y < yMin
                     */
                    // clip right first
                    lastDrawingY = yTo + dy * (ClippingBoxXMax - xTo) / dx;
                    // check if last drawing Y is less than yMIN
                    if (lastDrawingY < ClippingBoxYMin)
                    {
                        // draw one line from current point to the bottom border of box
                        lastDrawingX = xTo + dx * (ClippingBoxYMin - yTo) / dy;
                        DrawLine(CurrentXPosition, CurrentYPosition, lastDrawingX, ClippingBoxYMin);
                    }
                    else
                    {
                        // draw 2 seperated line
                        DrawLine(CurrentXPosition, CurrentYPosition, ClippingBoxXMax, lastDrawingY);
                        // draw second line along to right side of box
                        DrawLine(ClippingBoxXMax, lastDrawingY, ClippingBoxXMax, ClippingBoxYMin);
                    }
                    break;
                #endregion

                #region end point in region 0x0C
                case 0x0C:
                    /*x < xMin
                     * y< yMin
                     */
                    // clip left first
                    lastDrawingY = yTo + dy * (ClippingBoxXMin - xTo) / dx;
                    // check if last drawing Y is less than yMIN
                    if (lastDrawingY < ClippingBoxYMin)
                    {
                        // draw one line from current point to the bottom border of box
                        lastDrawingX = xTo + dx * (ClippingBoxYMin - yTo) / dy;
                        DrawLine(CurrentXPosition, CurrentYPosition, lastDrawingX, ClippingBoxYMin);
                    }
                    else
                    {
                        // draw 2 seperated line
                        DrawLine(CurrentXPosition, CurrentYPosition, ClippingBoxXMin, lastDrawingY);
                        // draw second line along to left side of box
                        DrawLine(ClippingBoxXMin, lastDrawingY, ClippingBoxXMin, ClippingBoxYMin);
                    }
                    break;
                #endregion

                #endregion

                #region when first point is in region 1
                /*
                 * xFrom > XMax
                 * yFrom > yMin, yFrom >yMax
                 * In this case, we need process 9 cases
                 */
                #region when end point in viewport region
                case 0x10:
                    /*
                     * draw from region 1 to view port ( visible area)
                     */
                    // draw the line along the right size of line
                    lastDrawingY = CurrentYPosition + dy * (ClippingBoxXMax - CurrentXPosition) / dx;
                    DrawLine(ClippingBoxXMax, CurrentYPosition, ClippingBoxXMax, lastDrawingY);
                    // draw the rest as normal
                    DrawLine(ClippingBoxXMax, lastDrawingY, xTo, yTo);
                    break;
                #endregion

                #region when end point in region 0x01
                case 0x11:
                    // draw a line along the right border of box
                    DrawLine(ClippingBoxXMax, CurrentYPosition, ClippingBoxXMax, yTo);
                    break;
                #endregion

                #region when end point in region 0x02
                case 0x12:
                    /*
                     * xTo > xMin,<xMax
                     * yTo > yMax
                     * Divide in two case
                     */
                    // first must clipping top of line
                    lastDrawingX = CurrentXPosition + dx * (ClippingBoxYMax - CurrentYPosition) / dy;
                    if (lastDrawingX > ClippingBoxXMax)
                    {
                        // we need draw 1 line along the right border of the box
                        DrawLine(ClippingBoxXMax, CurrentYPosition, ClippingBoxXMax, ClippingBoxYMax);
                    }
                    else
                    {
                        // draw into 2 line, first line along the right border of box
                        lastDrawingY = CurrentYPosition + dy * (ClippingBoxXMax - CurrentXPosition) / dx;
                        DrawLine(ClippingBoxXMax, CurrentYPosition, ClippingBoxXMax, lastDrawingY);

                        // draw from right side of box to top side of box
                        DrawLine(ClippingBoxXMax, lastDrawingY, lastDrawingX, ClippingBoxYMax);
                    }
                    break;
                #endregion

                #region when end point in region 0x03
                case 0x13:
                    /*xTo  > xMAX
                     * yTo > yMAX
                     */
                    // need draw 1 line only along the right side of box
                    DrawLine(ClippingBoxXMax, CurrentYPosition, ClippingBoxXMax, ClippingBoxYMax);
                    break;
                #endregion

                #region when end point in region 0x04
                case 0x14:
                    // drawing 3 lines
                    // first line from start point to right bound of box, 
                    // but this line vertical along the right side of box
                    lastDrawingY = CurrentYPosition + dy * (ClippingBoxXMax - CurrentXPosition) / dx;
                    DrawLine(ClippingBoxXMax, CurrentYPosition, ClippingBoxXMax, lastDrawingY);
                    // draw from right side to left side of box
                    // NOTE:in this case we use lastDrawingX to saving left y position
                    lastDrawingX = CurrentYPosition + dy * (ClippingBoxXMin - CurrentXPosition) / dx;
                    DrawLine(ClippingBoxXMax, lastDrawingY, ClippingBoxXMin, lastDrawingX);
                    // draw the rest, vertical line along left side of box
                    DrawLine(ClippingBoxXMin, lastDrawingX, ClippingBoxXMin, yTo);
                    break;
                #endregion

                #region when end point in region 0x06
                case 0x16:
                    // we may draw 2 or 3 lines
                    // first left clipping it
                    // finding y position that cut the left side
                    lastDrawingY = CurrentYPosition + dy * (ClippingBoxXMin - CurrentXPosition) / dx;
                    if (lastDrawingY < ClippingBoxYMax)
                    {
                        // when it cut the left of box
                        // first draw from start point to right side of box
                        // Note: using lastDrawingX saving y cut position to right side of box
                        lastDrawingX = CurrentYPosition + dy * (ClippingBoxXMax - CurrentXPosition) / dx;
                        DrawLine(ClippingBoxXMax, CurrentYPosition, ClippingBoxXMax, lastDrawingX);

                        // draw line from right to left side
                        DrawLine(ClippingBoxXMax, lastDrawingX, ClippingBoxXMin, lastDrawingY);
                        // draw the rest to top box
                        DrawLine(ClippingBoxXMin, lastDrawingY, ClippingBoxXMin, ClippingBoxYMax);
                    }
                    else
                    {
                        // draw 2 line.

                        // first draw from start point to right side of box
                        lastDrawingY = CurrentYPosition + dy * (ClippingBoxXMax - CurrentXPosition) / dx;
                        DrawLine(ClippingBoxXMax, CurrentYPosition, ClippingBoxXMax, lastDrawingY);

                        lastDrawingX = CurrentXPosition + dx * (ClippingBoxYMax - CurrentYPosition) / dy;
                        // draw rest line from right side of box to the top border
                        DrawLine(ClippingBoxXMax, lastDrawingY, lastDrawingX, ClippingBoxYMax);
                    }
                    break;
                #endregion

                #region when end point in region 0x08
                case 0x18:
                    // first clip bottom of line
                    lastDrawingX = CurrentXPosition + dx * (ClippingBoxYMin - CurrentYPosition) / dy;
                    if (lastDrawingX > ClippingBoxXMax)
                    {
                        // draw vertical line only
                        DrawLine(ClippingBoxXMax, CurrentYPosition, ClippingBoxXMax, ClippingBoxYMin);
                    }
                    else
                    {
                        // calculate the cut to right of box
                        lastDrawingY = CurrentYPosition + dy * (ClippingBoxXMax - CurrentXPosition) / dx;
                        DrawLine(ClippingBoxXMax, CurrentYPosition, ClippingBoxXMax, lastDrawingY);

                        // draw in view port line
                        DrawLine(ClippingBoxXMax, lastDrawingY, lastDrawingX, ClippingBoxYMin);
                    }
                    break;
                #endregion

                #region when end point in region 0x09
                case 0x19:
                    // draw along the right border of box to bottom
                    DrawLine(ClippingBoxXMax, CurrentYPosition, ClippingBoxXMax, ClippingBoxYMin);
                    break;
                #endregion

                #region when end point in region 0x0C
                case 0x1C:
                    /*This similar to case 0x16
                     */
                    // first left clipping it
                    // finding y position that cut the left side
                    lastDrawingY = CurrentYPosition + dy * (ClippingBoxXMin - CurrentXPosition) / dx;
                    if (lastDrawingY < ClippingBoxYMin)
                    {
                        // draw 2 line.

                        // first draw from start point to right side of box
                        lastDrawingY = CurrentYPosition + dy * (ClippingBoxXMax - CurrentXPosition) / dx;
                        DrawLine(ClippingBoxXMax, CurrentYPosition, ClippingBoxXMax, lastDrawingY);

                        lastDrawingX = CurrentXPosition + dx * (ClippingBoxYMin - CurrentYPosition) / dy;
                        // draw rest line from right side of box to the bottom border
                        DrawLine(ClippingBoxXMax, lastDrawingY, lastDrawingX, ClippingBoxYMin);
                    }
                    else
                    {
                        // when it cut the left of box
                        // first draw from start point to right side of box
                        // Note: using lastDrawingX saving y cut position to right side of box
                        lastDrawingX = CurrentYPosition + dy * (ClippingBoxXMax - CurrentXPosition) / dx;
                        DrawLine(ClippingBoxXMax, CurrentYPosition, ClippingBoxXMax, lastDrawingX);
                        // draw line from right to left side
                        DrawLine(ClippingBoxXMax, lastDrawingX, ClippingBoxXMin, lastDrawingY);
                        // draw the rest to bottom box
                        DrawLine(ClippingBoxXMin, lastDrawingY, ClippingBoxXMin, ClippingBoxYMin);
                    }
                    break;
                #endregion
                #endregion

                #region when first point in region 2
                /*
                     * in this region, y >YMax
                     * when endpoint in region 2,3,6, don't need to draw it
                     */
                #region when end point in region 0x00
                case 0x20:
                    // in this case , draw a line from top to the cut position
                    lastDrawingX = xTo + dx * (ClippingBoxYMax - yTo) / dy;
                    DrawLine(lastDrawingX, ClippingBoxYMax, xTo, yTo);
                    break;
                #endregion
                #region when end point in region 0x01
                case 0x21:
                    // similar to case 12 but reversed
                    // clip top
                    lastDrawingX = xTo + dx * (ClippingBoxYMax - yTo) / dy;

                    if (lastDrawingX > ClippingBoxXMax)
                    {
                        // draw 1 line from top-right corner along the right side
                        DrawLine(ClippingBoxXMax, ClippingBoxYMax, ClippingBoxXMax, yTo);
                    }
                    else
                    {
                        // draw 2 line
                        // first find the cut to the right border of box
                        lastDrawingY = yTo + dy * (ClippingBoxXMax - xTo) / dx;
                        DrawLine(lastDrawingX, ClippingBoxYMax, ClippingBoxXMax, lastDrawingY);
                        // second line along the right border
                        DrawLine(ClippingBoxXMax, lastDrawingY, ClippingBoxXMax, yTo);
                    }
                    break;
                #endregion
                #region when end point in region 0x04
                case 0x24:
                    // clip top
                    lastDrawingX = xTo + dx * (ClippingBoxYMax - yTo) / dy;
                    if (lastDrawingX < ClippingBoxXMin)
                    {
                        // draw 1 line a long the left of box
                        DrawLine(ClippingBoxXMin, ClippingBoxYMax, ClippingBoxXMin, yTo);
                    }
                    else
                    {
                        // find the cut to left border of box
                        lastDrawingY = yTo + dy * (ClippingBoxXMin - xTo) / dx;
                        DrawLine(lastDrawingX, ClippingBoxYMax, ClippingBoxXMin, lastDrawingY);
                        // draw vertical line along the left border
                        DrawLine(ClippingBoxXMin, lastDrawingY, ClippingBoxXMin, yTo);
                    }
                    break;
                #endregion
                #region when end point in region 0x8
                case 0x28:
                    // draw in the box only, clip top and bottom
                    lastDrawingX = xTo + dx * (ClippingBoxYMax - yTo) / dy;

                    // using last drawing Y to saving the x of cut at bottom border
                    lastDrawingY = xTo + dx * (ClippingBoxYMin - yTo) / dy;

                    DrawLine(lastDrawingX, ClippingBoxYMax, lastDrawingY, ClippingBoxYMin);

                    break;
                #endregion
                #region when end point in region 9
                case 0x29:
                    // clip top first
                    lastDrawingX = xTo + dx * (ClippingBoxYMax - yTo) / dy;
                    if (lastDrawingX > ClippingBoxXMax)
                    {
                        // draw one line along whole right border of box
                        DrawLine(ClippingBoxXMax, ClippingBoxYMax, ClippingBoxXMax, ClippingBoxYMin);
                    }
                    else
                    {
                        // clip right
                        lastDrawingY = yTo + dy * (ClippingBoxXMax - xTo) / dx;
                        if (lastDrawingY < ClippingBoxYMin)
                        {
                            // in this case we need drawing 1 line only
                            // find the cut of line to the bottom and saving x to lastDrawingY
                            lastDrawingY = xTo + dx * (ClippingBoxYMin - yTo) / dy;
                            DrawLine(lastDrawingX, ClippingBoxYMax, lastDrawingY, ClippingBoxYMin);
                        }
                        else
                        {

                            // in this case, we need drawing 2 line
                            DrawLine(lastDrawingX, ClippingBoxYMax, ClippingBoxXMax, lastDrawingY);
                            //
                            DrawLine(ClippingBoxXMax, lastDrawingY, ClippingBoxXMax, ClippingBoxYMin);
                        }
                    }
                    break;
                #endregion
                #region end point in region 0x0C
                case 0x2C:
                    // clip top first
                    lastDrawingX = xTo + dx * (ClippingBoxYMax - yTo) / dy;
                    if (lastDrawingX < ClippingBoxXMin)
                    {
                        // draw one line along whole right border of box
                        DrawLine(ClippingBoxXMin, ClippingBoxYMax, ClippingBoxXMin, ClippingBoxYMin);
                    }
                    else
                    {
                        // clip left
                        lastDrawingY = yTo + dy * (ClippingBoxXMin - xTo) / dx;
                        if (lastDrawingY < ClippingBoxYMin)
                        {
                            // in this case we need drawing 1 line only
                            // find the cut of line to the bottom and saving x to lastDrawingY
                            lastDrawingY = xTo + dx * (ClippingBoxYMin - yTo) / dy;
                            DrawLine(lastDrawingX, ClippingBoxYMax, lastDrawingY, ClippingBoxYMin);
                        }
                        else
                        {
                            // in this case, we need drawing 2 line
                            DrawLine(lastDrawingX, ClippingBoxYMax, ClippingBoxXMin, lastDrawingY);
                            //
                            DrawLine(ClippingBoxXMin, lastDrawingY, ClippingBoxXMin, ClippingBoxYMin);
                        }
                    }
                    break;
                #endregion
                #endregion

                #region when first point in region 3
                /*In this case, we not need to draw anymore
                     * when end point in region 2,3,6
                     */
                #region WHen end point in view port
                case 0x30:
                    //right clipping
                    lastDrawingY = yTo + dy * (ClippingBoxXMax - xTo) / dx;
                    if (lastDrawingY > ClippingBoxYMax)
                    {
                        // draw 1 line only
                        // find the cut to top box
                        lastDrawingX = xTo + dx * (ClippingBoxYMax - yTo) / dy;

                        DrawLine(lastDrawingX, ClippingBoxYMax, xTo, yTo);
                    }
                    else
                    {
                        // draw 2 line
                        // first line from top,left of box to lastDrawingY
                        DrawLine(ClippingBoxXMax, ClippingBoxYMax, ClippingBoxXMax, lastDrawingY);
                        // second from right side to xto , yto
                        DrawLine(ClippingBoxXMax, lastDrawingY, xTo, yTo);
                    }
                    break;
                #endregion

                #region when point in region 0x01
                case 0x31:
                    // draw line along the right side of box
                    // from top - right of box to yto 
                    DrawLine(ClippingBoxXMax, ClippingBoxYMax, ClippingBoxXMax, yTo);
                    break;
                #endregion

                #region when point in region 0x04
                case 0x34:
                    // this is complex case
                    // first clip right of start
                    lastDrawingY = yTo + dy * (ClippingBoxXMax - xTo) / dx;
                    if (lastDrawingY > ClippingBoxYMax)
                    {
                        #region similar to draw from 2 to 4
                        // clip top, and draw 
                        lastDrawingX = xTo + dx * (ClippingBoxYMax - yTo) / dy;
                        if (lastDrawingX < ClippingBoxXMin)
                        {
                            DrawLine(ClippingBoxXMin, ClippingBoxYMax, ClippingBoxXMin, yTo);
                        }
                        else
                        {
                            // clip left first
                            lastDrawingY = yTo + dy * (ClippingBoxXMin - xTo) / dx;
                            DrawLine(lastDrawingX, ClippingBoxYMax, ClippingBoxXMin, lastDrawingY);
                            DrawLine(ClippingBoxXMin, lastDrawingY, ClippingBoxXMin, yTo);
                        }
                        #endregion
                    }
                    else
                    {
                        #region continue to draw 3 lines
                        // draw from top-right of box
                        DrawLine(ClippingBoxXMax, ClippingBoxYMax, ClippingBoxXMax, lastDrawingY);
                        // left clipping
                        // using lastdrawingX to saving y pos of cut at left box
                        lastDrawingX = yTo + dy * (ClippingBoxXMin - xTo) / dx;
                        DrawLine(ClippingBoxXMax, lastDrawingY, ClippingBoxXMin, lastDrawingX);
                        // draw last line vertical along left border
                        DrawLine(ClippingBoxXMin, lastDrawingX, ClippingBoxXMin, yTo);
                        #endregion
                    }
                    break;
                #endregion

                #region when point in region 0x08
                case 0x38:
                    // clip right first
                    lastDrawingY = yTo + dy * (ClippingBoxXMax - xTo) / dx;

                    if (lastDrawingY > ClippingBoxYMax)
                    {
                        #region continue clip top
                        lastDrawingX = xTo + dx * (ClippingBoxYMax - yTo) / dy;
                        // find the cut at bottom and saving x to lastDrawingY
                        lastDrawingY = xTo + dx * (ClippingBoxYMin - yTo) / dy;

                        DrawLine(lastDrawingX, ClippingBoxYMax, lastDrawingY, ClippingBoxYMin);
                        #endregion
                    }
                    else if (lastDrawingY < ClippingBoxYMin)
                    {
                        #region not cut the box at all
                        // just draw a line along whole right side border
                        DrawLine(ClippingBoxXMax, ClippingBoxYMax, ClippingBoxXMax, ClippingBoxYMin);
                        #endregion
                    }
                    else
                    {
                        #region cut in range from boxYMin to boxYMax
                        // draw a line from top-right to current y pos
                        DrawLine(ClippingBoxXMax, ClippingBoxYMax, ClippingBoxXMax, lastDrawingY);
                        // clip bottom
                        lastDrawingX = xTo + dx * (ClippingBoxYMin - yTo) / dy;
                        DrawLine(ClippingBoxXMax, lastDrawingY, lastDrawingX, ClippingBoxYMin);
                        #endregion
                    }
                    break;
                #endregion

                #region when point in region 0x09
                case 0x39:
                    // just draw a line along whole right side border
                    DrawLine(ClippingBoxXMax, ClippingBoxYMax, ClippingBoxXMax, ClippingBoxYMin);
                    break;
                #endregion

                #region when point in region 0x0C
                case 0x3C:
                    // similar to 38 but more complex case
                    // clip right first
                    lastDrawingY = yTo + dy * (ClippingBoxXMax - xTo) / dx;

                    if (lastDrawingY > ClippingBoxYMax)
                    {
                        #region continue clip top, similar to 2C
                        // this will similar to 2C
                        lastDrawingX = xTo + dx * (ClippingBoxYMax - yTo) / dy;
                        if (lastDrawingX < ClippingBoxXMin)
                        {
                            // draw 1 line along left border
                            DrawLine(ClippingBoxXMin, ClippingBoxYMax, ClippingBoxXMin, ClippingBoxYMin);
                        }
                        else
                        {
                            //clip left
                            lastDrawingY = yTo + dy * (ClippingBoxXMin - xTo) / dx;
                            if (lastDrawingY < ClippingBoxYMin)
                            {
                                // draw 1 line
                                // clip bottom ,and saving x pos to lastDrawingY
                                lastDrawingY = xTo + dx * (ClippingBoxYMin - yTo) / dy;
                                DrawLine(lastDrawingX, ClippingBoxYMax, lastDrawingY, ClippingBoxYMin);
                            }
                            else
                            {
                                // draw two line
                                DrawLine(lastDrawingX, ClippingBoxYMax, ClippingBoxXMin, lastDrawingY);
                                // draw vertical line
                                DrawLine(ClippingBoxXMin, lastDrawingY, ClippingBoxXMin, ClippingBoxYMin);
                            }
                        }

                        #endregion
                    }
                    else if (lastDrawingY < ClippingBoxYMin)
                    {
                        #region not cut the box at all
                        // just draw a line along whole right side border
                        DrawLine(ClippingBoxXMax, ClippingBoxYMax, ClippingBoxXMax, ClippingBoxYMin);
                        #endregion
                    }
                    else
                    {
                        #region cut in range from boxYMin to boxYMax
                        // this similar to 1C
                        // draw a line from top-right to current y pos
                        DrawLine(ClippingBoxXMax, ClippingBoxYMax, ClippingBoxXMax, lastDrawingY);

                        // clipping left, saving y pos to lastDrawingX
                        lastDrawingX = yTo + dy * (ClippingBoxXMin - xTo) / dx;
                        if (lastDrawingX < ClippingBoxXMin)
                        {
                            // draw into 2 line
                            DrawLine(ClippingBoxXMax, lastDrawingY, ClippingBoxXMin, lastDrawingX);
                            // draw along the left border
                            DrawLine(ClippingBoxXMin, lastDrawingX, ClippingBoxXMin, ClippingBoxYMin);
                        }
                        else
                        {
                            // draw 1 line
                            // clip bottom
                            lastDrawingX = xTo + dx * (ClippingBoxYMin - yTo) / dy;
                            DrawLine(ClippingBoxXMax, lastDrawingY, lastDrawingX, ClippingBoxYMin);
                        }
                        #endregion
                    }
                    break;
                #endregion
                #endregion

                #region when first point in region 4
                /*This is similar to region 1
                     * So that need 9 case
                     */

                #region when end point in region 0
                case 0x40:
                    // clip left
                    lastDrawingY = yTo + dy * (ClippingBoxXMin - xTo) / dx;
                    DrawLine(ClippingBoxXMin, CurrentYPosition, ClippingBoxXMin, lastDrawingY);
                    DrawLine(ClippingBoxXMin, lastDrawingY, xTo, yTo);
                    break;
                #endregion
                #region when end point in region 1
                case 0x41:
                    // clip left
                    lastDrawingY = yTo + dy * (ClippingBoxXMin - xTo) / dx;
                    //clip right but saving y to lastDrawingX
                    lastDrawingX = yTo + dy * (ClippingBoxXMax - xTo) / dx;
                    DrawLine(ClippingBoxXMin, CurrentYPosition, ClippingBoxXMin, lastDrawingY);
                    DrawLine(ClippingBoxXMin, lastDrawingY, ClippingBoxXMax, lastDrawingX);
                    DrawLine(ClippingBoxXMax, lastDrawingX, ClippingBoxXMax, yTo);
                    break;
                #endregion
                #region when end point in region 2
                case 0x42:
                    // clip left
                    lastDrawingY = yTo + dy * (ClippingBoxXMin - xTo) / dx;
                    if (lastDrawingY > ClippingBoxYMax)
                    {
                        // draw 1 line along the left border
                        DrawLine(ClippingBoxXMin, CurrentYPosition, ClippingBoxXMin, ClippingBoxYMax);
                    }
                    else
                    {
                        DrawLine(ClippingBoxXMin, CurrentYPosition, ClippingBoxXMin, lastDrawingY);
                        // clipping top
                        lastDrawingX = xTo + dx * (ClippingBoxYMax - yTo) / dy;
                        DrawLine(ClippingBoxXMin, lastDrawingY, lastDrawingX, ClippingBoxYMax);
                    }
                    break;
                #endregion
                #region when end point in region 3
                case 0x43:
                    // clip left
                    lastDrawingY = yTo + dy * (ClippingBoxXMin - xTo) / dx;
                    if (lastDrawingY > ClippingBoxYMax)
                    {
                        // draw 1 line along the left border
                        DrawLine(ClippingBoxXMin, CurrentYPosition, ClippingBoxXMin, ClippingBoxYMax);
                    }
                    else
                    {
                        DrawLine(ClippingBoxXMin, CurrentYPosition, ClippingBoxXMin, lastDrawingY);

                        // clipping top
                        lastDrawingX = xTo + dx * (ClippingBoxYMax - yTo) / dy;
                        if (lastDrawingX > ClippingBoxXMax)
                        {
                            // continue to clipping right, saving y pos to lastDrawingX
                            lastDrawingX = yTo + dy * (ClippingBoxXMax - xTo) / dx;
                            DrawLine(ClippingBoxXMin, lastDrawingY, ClippingBoxXMax, lastDrawingX);
                            // draw a line to top
                            DrawLine(ClippingBoxXMax, lastDrawingX, ClippingBoxXMax, ClippingBoxYMax);
                        }
                        else
                        {
                            // else, draw 1 line only
                            DrawLine(ClippingBoxXMin, lastDrawingY, lastDrawingX, ClippingBoxYMax);
                        }
                    }
                    break;
                #endregion

                #region when end point in region 4
                case 0x44:
                    DrawLine(ClippingBoxXMin, CurrentYPosition, ClippingBoxXMin, yTo);
                    break;
                #endregion
                #region when end point in region 6
                case 0x46:
                    DrawLine(ClippingBoxXMin, CurrentYPosition, ClippingBoxXMin, ClippingBoxYMax);
                    break;
                #endregion

                #region when end point in region 8
                case 0x48:
                    // clip left
                    lastDrawingY = yTo + dy * (ClippingBoxXMin - xTo) / dx;
                    if (lastDrawingY < ClippingBoxYMin)
                    {
                        // draw 1 line
                        DrawLine(ClippingBoxXMin, CurrentYPosition, ClippingBoxXMin, ClippingBoxYMin);
                    }
                    else
                    {
                        DrawLine(ClippingBoxXMin, CurrentYPosition, ClippingBoxXMin, lastDrawingY);

                        // clip bottom
                        lastDrawingX = xTo + dx * (ClippingBoxYMin - yTo) / dy;

                        DrawLine(ClippingBoxXMin, lastDrawingY, lastDrawingX, ClippingBoxYMin);
                    }
                    break;
                #endregion
                #region when end point in region 9
                case 0x49:
                    // clip left
                    lastDrawingY = yTo + dy * (ClippingBoxXMin - xTo) / dx;
                    if (lastDrawingY < ClippingBoxYMin)
                    {
                        // draw 1 line
                        DrawLine(ClippingBoxXMin, CurrentYPosition, ClippingBoxXMin, ClippingBoxYMin);
                    }
                    else
                    {
                        DrawLine(ClippingBoxXMin, CurrentYPosition, ClippingBoxXMin, lastDrawingY);
                        // clip bottom
                        lastDrawingX = xTo + dx * (ClippingBoxYMin - yTo) / dy;
                        if (lastDrawingX > ClippingBoxXMax)
                        {
                            // clip right and saving to lastDrawingX
                            lastDrawingX = yTo + dy * (ClippingBoxXMax - xTo) / dx;
                            DrawLine(ClippingBoxXMin, lastDrawingY, ClippingBoxXMax, lastDrawingX);
                            DrawLine(ClippingBoxXMax, lastDrawingX, ClippingBoxXMax, ClippingBoxYMin);
                        }
                        else
                        {
                            DrawLine(ClippingBoxXMin, lastDrawingY, lastDrawingX, ClippingBoxYMin);
                        }
                    }
                    break;
                #endregion

                #region when end point in region C
                case 0x4C:
                    DrawLine(ClippingBoxXMin, CurrentYPosition, ClippingBoxXMin, ClippingBoxYMin);
                    break;
                #endregion
                #endregion

                #region when first point in region 6
                /*not need draw when end point in 2,3,6
                     */
                #region when end point in view port
                case 0x60:
                    // clip left
                    lastDrawingY = yTo + dy * (ClippingBoxXMin - xTo) / dx;
                    if (lastDrawingY > ClippingBoxYMax)
                    {
                        // clip top
                        lastDrawingX = xTo + dx * (ClippingBoxYMax - yTo) / dy;
                        DrawLine(lastDrawingX, ClippingBoxYMax, xTo, yTo);
                    }
                    else
                    {
                        DrawLine(ClippingBoxXMin, ClippingBoxYMax, ClippingBoxXMin, lastDrawingY);
                        DrawLine(ClippingBoxXMin, lastDrawingY, xTo, yTo);
                    }
                    break;
                #endregion
                #region when end point in region 1
                case 0x61:
                    // clip left
                    lastDrawingY = yTo + dy * (ClippingBoxXMin - xTo) / dx;
                    if (lastDrawingY > ClippingBoxYMax)
                    {
                        // clip top
                        lastDrawingX = xTo + dx * (ClippingBoxYMax - yTo) / dy;
                        if (lastDrawingX > ClippingBoxXMax)
                        {
                            // draw 1 vertical line on right side of box
                            DrawLine(ClippingBoxXMax, ClippingBoxYMax, ClippingBoxXMax, yTo);
                        }
                        else
                        {
                            // clip right
                            lastDrawingY = yTo + dy * (ClippingBoxXMax - xTo) / dx;
                            DrawLine(lastDrawingX, ClippingBoxYMax, ClippingBoxXMax, lastDrawingY);
                            DrawLine(ClippingBoxXMax, lastDrawingY, ClippingBoxXMax, yTo);
                        }
                    }
                    else
                    {
                        // draw from top-left corner to cut
                        DrawLine(ClippingBoxXMin, ClippingBoxYMax, ClippingBoxXMin, lastDrawingY);
                        // clip right, and saving Y pos to lastDrawingX
                        lastDrawingX = yTo + dy * (ClippingBoxXMax - xTo) / dx;
                        DrawLine(ClippingBoxXMin, lastDrawingY, ClippingBoxXMax, lastDrawingX);
                        // draw rest
                        DrawLine(ClippingBoxXMax, lastDrawingX, ClippingBoxXMax, yTo);
                    }
                    break;
                #endregion
                #region when end point in region 4
                case 0x64:
                    DrawLine(ClippingBoxXMin, ClippingBoxYMax, ClippingBoxXMin, yTo);
                    break;
                #endregion
                #region when end point in region 8
                case 0x68:
                    // clip left
                    lastDrawingY = yTo + dy * (ClippingBoxXMin - xTo) / dx;
                    if (lastDrawingY > ClippingBoxYMax)
                    {
                        // clip top
                        lastDrawingX = xTo + dx * (ClippingBoxYMax - yTo) / dy;
                        // clip bottom,but using lastDrawingY to saving x pos
                        lastDrawingY = xTo + dx * (ClippingBoxYMin - yTo) / dy;
                        DrawLine(lastDrawingX, ClippingBoxYMax, lastDrawingY, ClippingBoxYMin);
                    }
                    else if (lastDrawingY < ClippingBoxYMin)
                    {
                        // draw full left side border
                        DrawLine(ClippingBoxXMin, ClippingBoxYMax, ClippingBoxXMin, ClippingBoxYMin);
                    }
                    else
                    {
                        // cut left of box at pos in range Y Min - Y Max
                        DrawLine(ClippingBoxXMin, ClippingBoxYMax, ClippingBoxXMin, lastDrawingY);
                        // clip bottom
                        lastDrawingX = xTo + dx * (ClippingBoxYMin - yTo) / dy;
                        DrawLine(ClippingBoxXMin, lastDrawingY, lastDrawingX, ClippingBoxYMin);
                    }
                    break;
                #endregion
                #region when end point in region 9
                case 0x69:
                    // clip left
                    lastDrawingY = yTo + dy * (ClippingBoxXMin - xTo) / dx;
                    if (lastDrawingY > ClippingBoxYMax)
                    {
                        // clip top
                        lastDrawingX = xTo + dx * (ClippingBoxYMax - yTo) / dy;
                        if (lastDrawingX > ClippingBoxXMax)
                        {
                            // draw full right border
                            DrawLine(ClippingBoxXMax, ClippingBoxYMax, ClippingBoxXMax, ClippingBoxYMin);
                        }
                        else
                        {
                            // clip right
                            lastDrawingY = yTo + dy * (ClippingBoxXMax - xTo) / dx;
                            if (lastDrawingY < ClippingBoxYMin)
                            {
                                // clip bottom,but saving to lastDrawing Y
                                lastDrawingY = xTo + dx * (ClippingBoxYMin - yTo) / dy;

                                DrawLine(lastDrawingX, ClippingBoxYMax, lastDrawingY, ClippingBoxYMin);
                            }
                            else
                            {
                                DrawLine(lastDrawingX, ClippingBoxYMax, ClippingBoxXMax, lastDrawingY);
                                DrawLine(ClippingBoxXMax, lastDrawingY, ClippingBoxXMax, ClippingBoxYMin);
                            }
                        }
                    }
                    else if (lastDrawingY < ClippingBoxYMin)
                    {
                        // draw full left border
                        DrawLine(ClippingBoxXMin, ClippingBoxYMax, ClippingBoxXMin, ClippingBoxYMin);
                    }
                    else
                    {
                        DrawLine(ClippingBoxXMin, ClippingBoxYMax, ClippingBoxXMin, lastDrawingY);
                        // clip bottom
                        lastDrawingX = xTo + dx * (ClippingBoxYMin - yTo) / dy;
                        if (lastDrawingX > ClippingBoxXMax)
                        {
                            // clip right
                            lastDrawingX = yTo + dy * (ClippingBoxXMax - xTo) / dx;
                            DrawLine(ClippingBoxXMin, lastDrawingY, ClippingBoxXMax, lastDrawingX);
                            DrawLine(ClippingBoxXMax, lastDrawingX, ClippingBoxXMax, ClippingBoxYMin);
                        }
                        else
                        {
                            DrawLine(ClippingBoxXMin, lastDrawingY, lastDrawingX, ClippingBoxYMin);
                        }
                    }
                    break;
                #endregion
                #region when end point in region C
                case 0x6C:
                    // draw full left border
                    DrawLine(ClippingBoxXMin, ClippingBoxYMax, ClippingBoxXMin, ClippingBoxYMin);
                    break;
                #endregion

                #endregion

                #region when first point in region 8
                /* Do not need to draw when end point in region 8,9,C
                 */
                #region when end point in view port
                case 0x80:
                    // draw from the bottom
                    lastDrawingX = xTo + dx * (ClippingBoxYMin - yTo) / dy;
                    DrawLine(lastDrawingX, ClippingBoxYMin, xTo, yTo);
                    break;
                #endregion

                #region when end point in region 1
                case 0x81:
                    // draw from the bottom
                    lastDrawingX = xTo + dx * (ClippingBoxYMin - yTo) / dy;
                    if (lastDrawingX > ClippingBoxXMax)
                    {
                        DrawLine(ClippingBoxXMax, ClippingBoxYMin, ClippingBoxXMax, yTo);
                    }
                    else
                    {
                        // clip right
                        lastDrawingY = yTo + dy * (ClippingBoxXMax - xTo) / dx;
                        DrawLine(lastDrawingX, ClippingBoxYMin, ClippingBoxXMax, lastDrawingY);
                        DrawLine(ClippingBoxXMax, lastDrawingY, ClippingBoxXMax, yTo);
                    }
                    break;
                #endregion

                #region when end point in region 2
                case 0x82:
                    // clip bottom 
                    lastDrawingX = xTo + dx * (ClippingBoxYMin - yTo) / dy;
                    // clip top, but saving x to lastDrawingY
                    lastDrawingY = xTo + dx * (ClippingBoxYMax - yTo) / dy;
                    DrawLine(lastDrawingX, ClippingBoxYMin, lastDrawingY, ClippingBoxYMax);
                    break;
                #endregion

                #region when end point in region 3
                case 0x83:
                    // clip bottom 
                    lastDrawingX = xTo + dx * (ClippingBoxYMin - yTo) / dy;
                    if (lastDrawingX > ClippingBoxXMax)
                    {
                        // draw full right border
                        DrawLine(ClippingBoxXMax, ClippingBoxYMin, ClippingBoxXMax, ClippingBoxYMax);
                    }
                    else
                    {
                        // clip right
                        lastDrawingY = yTo + dy * (ClippingBoxXMax - xTo) / dx;
                        if (lastDrawingY > ClippingBoxYMax)
                        {
                            // clip top
                            lastDrawingY = xTo + dx * (ClippingBoxYMax - yTo) / dy;
                            DrawLine(lastDrawingX, ClippingBoxYMin, lastDrawingY, ClippingBoxYMax);
                        }
                        else
                        {
                            DrawLine(lastDrawingX, ClippingBoxYMin, ClippingBoxXMax, lastDrawingY);
                            // draw vertical line to top-right corner
                            DrawLine(ClippingBoxXMax, lastDrawingY, ClippingBoxXMax, ClippingBoxYMax);
                        }
                    }
                    break;
                #endregion

                #region when end point in region 4
                case 0x84:
                    // clip bottom
                    lastDrawingX = xTo + dx * (ClippingBoxYMin - yTo) / dy;
                    if (lastDrawingX < ClippingBoxXMin)
                    {
                        // draw 1 line only
                        DrawLine(ClippingBoxXMin, ClippingBoxYMin, ClippingBoxXMin, yTo);
                    }
                    else
                    {
                        // clip left
                        lastDrawingY = yTo + dy * (ClippingBoxXMin - xTo) / dx;

                        DrawLine(lastDrawingX, ClippingBoxYMin, ClippingBoxXMin, lastDrawingY);

                        DrawLine(ClippingBoxXMin, lastDrawingY, ClippingBoxXMin, yTo);
                    }
                    break;
                #endregion

                #region when end point in region 6
                case 0x86:
                    // clip bottom
                    lastDrawingX = xTo + dx * (ClippingBoxYMin - yTo) / dy;
                    if (lastDrawingX < ClippingBoxXMin)
                    {
                        // draw full left side border
                        DrawLine(ClippingBoxXMin, ClippingBoxYMin, ClippingBoxXMin, ClippingBoxYMax);
                    }
                    else
                    {
                        // clip left
                        lastDrawingY = yTo + dy * (ClippingBoxXMin - xTo) / dx;
                        if (lastDrawingY > ClippingBoxYMax)
                        {
                            // clip top, saving to lastDrawingY
                            lastDrawingY = xTo + dx * (ClippingBoxYMax - yTo) / dy;
                            DrawLine(lastDrawingX, ClippingBoxYMin, lastDrawingY, ClippingBoxYMax);
                        }
                        else
                        {
                            DrawLine(lastDrawingX, ClippingBoxYMin, ClippingBoxXMin, lastDrawingY);
                            DrawLine(ClippingBoxXMin, lastDrawingY, ClippingBoxXMin, ClippingBoxYMax);
                        }
                    }
                    break;
                #endregion
                #endregion

                #region when first point in region 9
                /*Do not need draw when end point region 8,9,C
                 */
                #region when end point in view port
                case 0x90:
                    // clip bottom
                    lastDrawingX = xTo + dx * (ClippingBoxYMin - yTo) / dy;
                    if (lastDrawingX > ClippingBoxXMax)
                    {
                        // clip right
                        lastDrawingY = yTo + dy * (ClippingBoxXMax - xTo) / dx;
                        DrawLine(ClippingBoxXMax, ClippingBoxYMin, ClippingBoxXMax, lastDrawingY);
                        DrawLine(ClippingBoxXMax, lastDrawingY, xTo, yTo);
                    }
                    else
                    {
                        DrawLine(lastDrawingX, ClippingBoxYMin, xTo, yTo);
                    }
                    break;
                #endregion
                #region when end point in region 1
                case 0x91:
                    DrawLine(ClippingBoxXMax, ClippingBoxYMin, ClippingBoxXMax, yTo);
                    break;
                #endregion
                #region when end point in region 2
                case 0x92:
                    // clip bottom
                    lastDrawingX = xTo + dx * (ClippingBoxYMin - yTo) / dy;
                    if (lastDrawingX > ClippingBoxXMax)
                    {
                        // clip right
                        lastDrawingY = yTo + dy * (ClippingBoxXMax - xTo) / dx;
                        if (lastDrawingY > ClippingBoxYMax)
                        {
                            // draw 1 full right border
                            DrawLine(ClippingBoxXMax, ClippingBoxYMin, ClippingBoxXMax, ClippingBoxYMax);
                        }
                        else
                        {
                            DrawLine(ClippingBoxXMax, ClippingBoxYMin, ClippingBoxXMax, lastDrawingY);
                            // clip top
                            lastDrawingX = xTo + dx * (ClippingBoxYMax - yTo) / dy;
                            DrawLine(ClippingBoxXMax, lastDrawingY, lastDrawingX, ClippingBoxYMax);
                        }
                    }
                    else
                    {
                        // clip top, but saving to lastDrawingY
                        lastDrawingY = xTo + dx * (ClippingBoxYMax - yTo) / dy;
                        DrawLine(lastDrawingX, ClippingBoxYMin, lastDrawingY, ClippingBoxYMax);
                    }
                    break;
                #endregion
                #region when end point in region 3
                case 0x93:
                    // draw full right border
                    DrawLine(ClippingBoxXMax, ClippingBoxYMin, ClippingBoxXMax, ClippingBoxYMax);
                    break;
                #endregion
                #region when end point in region 4
                case 0x94:
                    // clip bottom
                    lastDrawingX = xTo + dx * (ClippingBoxYMin - yTo) / dy;
                    if (lastDrawingX > ClippingBoxXMax)
                    {
                        // clip right
                        lastDrawingY = yTo + dy * (ClippingBoxXMax - xTo) / dx;
                        DrawLine(ClippingBoxXMax, ClippingBoxYMin, ClippingBoxXMax, lastDrawingY);
                        // clip left, and saving y to lastDrawingX
                        lastDrawingX = yTo + dy * (ClippingBoxXMin - xTo) / dx;
                        DrawLine(ClippingBoxXMax, lastDrawingY, ClippingBoxXMin, lastDrawingX);
                        DrawLine(ClippingBoxXMin, lastDrawingX, ClippingBoxXMin, yTo);
                    }
                    else if (lastDrawingX < ClippingBoxXMin)
                    {
                        DrawLine(ClippingBoxXMin, ClippingBoxYMin, ClippingBoxXMin, yTo);
                    }
                    else
                    {
                        // clip left
                        lastDrawingY = yTo + dy * (ClippingBoxXMin - xTo) / dx;
                        DrawLine(lastDrawingX, ClippingBoxYMin, ClippingBoxXMin, lastDrawingY);

                        DrawLine(ClippingBoxXMin, lastDrawingY, ClippingBoxXMin, yTo);
                    }
                    break;
                #endregion
                #region when end point in region 6
                case 0x96:
                    // clip bottom
                    lastDrawingX = xTo + dx * (ClippingBoxYMin - yTo) / dy;
                    if (lastDrawingX > ClippingBoxXMax)
                    {
                        // clip right
                        lastDrawingY = yTo + dy * (ClippingBoxXMax - xTo) / dx;
                        if (lastDrawingY > ClippingBoxYMax)
                        {
                            // draw full right border
                            DrawLine(ClippingBoxXMax, ClippingBoxYMin, ClippingBoxXMax, ClippingBoxYMax);
                        }
                        else
                        {
                            DrawLine(ClippingBoxXMax, ClippingBoxYMin, ClippingBoxXMax, lastDrawingY);
                            // clip top
                            lastDrawingX = xTo + dx * (ClippingBoxYMax - yTo) / dy;
                            if (lastDrawingX < ClippingBoxXMin)
                            {
                                // clip left, saving to lastDrawingX
                                lastDrawingX = yTo + dy * (ClippingBoxXMin - xTo) / dx;
                                DrawLine(ClippingBoxXMax, lastDrawingY, ClippingBoxXMin, lastDrawingX);
                                DrawLine(ClippingBoxXMin, lastDrawingX, ClippingBoxXMin, ClippingBoxYMax);
                            }
                            else
                            {
                                DrawLine(ClippingBoxXMax, lastDrawingY, lastDrawingX, ClippingBoxYMax);
                            }
                        }
                    }
                    else if (lastDrawingX < ClippingBoxXMin)
                    {
                        // draw full left side of box
                        DrawLine(ClippingBoxXMin, ClippingBoxYMin, ClippingBoxXMin, ClippingBoxYMax);
                    }
                    else
                    {
                        // clip left
                        lastDrawingY = yTo + dy * (ClippingBoxXMin - xTo) / dx;
                        if (lastDrawingY > ClippingBoxYMax)
                        {
                            // clip top, but saving X to lastDrawingY
                            lastDrawingY = xTo + dx * (ClippingBoxYMax - yTo) / dy;
                            DrawLine(lastDrawingX, ClippingBoxYMin, lastDrawingY, ClippingBoxYMax);
                        }
                        else
                        {
                            DrawLine(lastDrawingX, ClippingBoxYMin, ClippingBoxXMin, lastDrawingY);
                            DrawLine(ClippingBoxXMin, lastDrawingY, ClippingBoxXMin, ClippingBoxYMax);
                        }
                    }
                    break;
                #endregion
                #endregion

                #region when first point in region C
                /*This case do not need to draw when end point in 8,9,C
                 */
                #region when end point in view port
                case 0xC0:
                    // clip bottom
                    lastDrawingX = xTo + dx * (ClippingBoxYMin - yTo) / dy;
                    if (lastDrawingX < ClippingBoxXMin)
                    {
                        // clip left
                        lastDrawingY = yTo + dy * (ClippingBoxXMin - xTo) / dx;
                        DrawLine(ClippingBoxXMin, ClippingBoxYMin, ClippingBoxXMin, lastDrawingY);
                        DrawLine(ClippingBoxXMin, lastDrawingY, xTo, yTo);
                    }
                    else
                    {
                        DrawLine(lastDrawingX, ClippingBoxYMin, xTo, yTo);
                    }
                    break;
                #endregion

                #region when end point in region 1
                case 0xC1:
                    // clip bottom
                    lastDrawingX = xTo + dx * (ClippingBoxYMin - yTo) / dy;
                    if (lastDrawingX < ClippingBoxXMin)
                    {
                        // clip left
                        lastDrawingY = yTo + dy * (ClippingBoxXMin - xTo) / dx;
                        DrawLine(ClippingBoxXMin, ClippingBoxYMin, ClippingBoxXMin, lastDrawingY);
                        // clip right, but saving to lastDrawingX
                        lastDrawingX = yTo + dy * (ClippingBoxXMax - xTo) / dx;
                        DrawLine(ClippingBoxXMin, lastDrawingY, ClippingBoxXMax, lastDrawingX);
                        DrawLine(ClippingBoxXMax, lastDrawingX, ClippingBoxXMax, yTo);
                    }
                    else if (lastDrawingX > ClippingBoxXMax)
                    {
                        DrawLine(ClippingBoxXMax, ClippingBoxYMin, ClippingBoxXMax, yTo);
                    }
                    else
                    {
                        // clip right
                        lastDrawingY = yTo + dy * (ClippingBoxXMax - xTo) / dx;
                        DrawLine(lastDrawingX, ClippingBoxYMin, ClippingBoxXMax, lastDrawingY);
                        DrawLine(ClippingBoxXMax, lastDrawingY, ClippingBoxXMax, yTo);
                    }
                    break;
                #endregion

                #region when end point in region 2
                case 0xC2:
                    // clip bottom
                    lastDrawingX = xTo + dx * (ClippingBoxYMin - yTo) / dy;
                    if (lastDrawingX < ClippingBoxXMin)
                    {
                        // clip right
                        lastDrawingY = yTo + dy * (ClippingBoxXMin - xTo) / dx;
                        if (lastDrawingY > ClippingBoxYMax)
                        {
                            // draw full left side of box
                            DrawLine(ClippingBoxXMin, ClippingBoxYMin, ClippingBoxXMin, ClippingBoxYMax);
                        }
                        else
                        {
                            DrawLine(ClippingBoxXMin, ClippingBoxYMin, ClippingBoxXMin, lastDrawingY);
                            // clip top
                            lastDrawingX = xTo + dx * (ClippingBoxYMax - yTo) / dy;
                            DrawLine(ClippingBoxXMin, lastDrawingY, lastDrawingX, ClippingBoxYMax);
                        }
                    }
                    else
                    {
                        // clip top, but saving to lastDrawingY
                        lastDrawingY = xTo + dx * (ClippingBoxYMax - yTo) / dy;
                        DrawLine(lastDrawingX, ClippingBoxYMin, lastDrawingY, ClippingBoxYMax);
                    }
                    break;
                #endregion

                #region when end point in region 3
                case 0xC3:
                    // clip bottom
                    lastDrawingX = xTo + dx * (ClippingBoxYMin - yTo) / dy;
                    if (lastDrawingX > ClippingBoxXMax)
                    {
                        // draw a vertical along the right side of box
                        DrawLine(ClippingBoxXMax, ClippingBoxYMin, ClippingBoxXMax, ClippingBoxYMax);
                    }
                    else if (lastDrawingX < ClippingBoxXMin)
                    {
                        //clip left
                        lastDrawingY = yTo + dy * (ClippingBoxXMin - xTo) / dx;
                        if (lastDrawingY > ClippingBoxYMax)
                        {
                            //draw a line along the left side of box
                            DrawLine(ClippingBoxXMin, ClippingBoxYMin, ClippingBoxXMin, ClippingBoxYMax);
                        }
                        else
                        {
                            DrawLine(ClippingBoxXMin, ClippingBoxYMin, ClippingBoxXMin, lastDrawingY);

                            // clip right , but saving y to lastDrawingX
                            lastDrawingX = yTo + dy * (ClippingBoxXMax - xTo) / dx;
                            if (lastDrawingX > ClippingBoxYMax)
                            {
                                // clip top
                                lastDrawingX = xTo + dx * (ClippingBoxYMax - yTo) / dy;
                                DrawLine(ClippingBoxXMin, lastDrawingY, lastDrawingX, ClippingBoxYMax);
                            }
                            else
                            {
                                DrawLine(ClippingBoxXMin, lastDrawingY, ClippingBoxXMax, lastDrawingX);
                                DrawLine(ClippingBoxXMax, lastDrawingX, ClippingBoxXMax, ClippingBoxYMax);
                            }
                        }
                    }
                    else
                    {
                        // clip right
                        lastDrawingY = yTo + dy * (ClippingBoxXMax - xTo) / dx;
                        if (lastDrawingY > ClippingBoxYMax)
                        {
                            // clip top, but saving x to lastdrawingY
                            lastDrawingY = xTo + dx * (ClippingBoxYMax - yTo) / dy;
                            DrawLine(lastDrawingX, ClippingBoxYMin, lastDrawingY, ClippingBoxYMax);
                        }
                        else
                        {
                            DrawLine(lastDrawingX, ClippingBoxYMin, ClippingBoxXMax, lastDrawingY);
                            DrawLine(ClippingBoxXMax, lastDrawingY, ClippingBoxXMax, ClippingBoxYMax);
                        }
                    }
                    break;
                #endregion
                #region when end point in region 4
                case 0xC4:
                    DrawLine(ClippingBoxXMin, ClippingBoxYMin, ClippingBoxXMin, yTo);
                    break;
                #endregion
                #region when end point in region 6
                case 0xC6:
                    DrawLine(ClippingBoxXMin, ClippingBoxYMin, ClippingBoxXMin, ClippingBoxYMax);
                    break;
                #endregion

                #endregion
            }
            #endregion

            #region after finish must reassign for next line
            CurrentXPosition = xTo;
            CurrentYPosition = yTo;
            CurrentPositionFlag = endPointClipping;
            #endregion
        }

        #endregion

        #region Set Current Point
        /// <summary>
        /// Set current point and calculate flag
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        protected void SetCurrentPoint(double x, double y)
        {
            CurrentXPosition = x;
            CurrentYPosition = y;
            // calculate current position flag
            CurrentPositionFlag =
               ((CurrentXPosition > ClippingBoxXMax) ? XMaxClippingFlag :
               (CurrentXPosition < ClippingBoxXMin) ? XMinClippingFlag : 0)
               |
               ((CurrentYPosition > ClippingBoxYMax) ? YMaxClippingFlag :
               (CurrentYPosition < ClippingBoxYMin) ? YMinClippingFlag : 0);
        }
        #endregion

        #region append row data
        /// <summary>
        /// Append rows into current row data. This method should be call as second approach.
        /// ( mean using Begin and Finish function)
        /// But before using this method must call Prepare row
        /// </summary>
        /// <param name="rows">rows</param>
        /// <param name="offsetX">offset x</param>
        /// <param name="offsetY">offset y</param>
        public void AppendRowData(RowData[] rows, double offsetX, double offsetY)
        {
            // first this will cast to integer value, 
            // need more implementation to make sure that these value will change the coverage
            // when out side clipping box
            if ((offsetY < ClippingBoxYMax)
                && (offsetX < ClippingBoxXMax))
            {
                int roundedOffsetY = (int)offsetY;
                int roundedOffsetX = (int)offsetX;
                #region start and end row
                double startY =
                     roundedOffsetY < ClippingBoxYMin ?
                     ClippingBoxYMin : roundedOffsetY; // max of two values
                int startRowIndex = (int)startY - (int)roundedOffsetY;

                double endY = roundedOffsetY + rows.Length > ClippingBoxYMax ?
                    ClippingBoxYMax : roundedOffsetY + rows.Length;
                int endRowIndex = (int)endY - roundedOffsetY;
                #endregion

                int startClippingX = (int)ClippingBoxXMin;
                int endClippingX = (int)ClippingBoxXMax;

                int startArea = (PixelScale - (int)(ClippingBoxXMin * PixelScale) & PixelMask) << PixelShift;
                int endArea = ((int)(ClippingBoxXMax * PixelScale) & PixelMask) << PixelShift;

                RowData currentRow = null;
                CellData cellData = null;


                int calculatedX = 0;
                int currentDestIndex = (int)startY;
                for (int row = startRowIndex; row < endRowIndex; row++)
                {
                    if (rows[row] != null)
                    {
                        currentRow = Rows[currentDestIndex];
                        cellData = rows[row].First;
                        while (cellData != null)
                        {
                            calculatedX = cellData.X + roundedOffsetX;
                            CurrentXPosition = calculatedX;
                            #region determine minx,maxy
                            if (CurrentXPosition < CurrentStartXIndex)
                            {
                                CurrentStartXIndex = (int)CurrentXPosition;
                            }
                            if (CurrentXPosition > CurrentEndXIndex)
                            {
                                CurrentEndXIndex = (int)CurrentXPosition + 1;
                            }
                            #endregion
                            if (calculatedX < startClippingX)
                            {
                                currentRow.SetCell(startClippingX, cellData.Coverage, 0);
                            }
                            else if (calculatedX > endClippingX)
                            {
                                currentRow.SetCell(endClippingX, cellData.Coverage, 0);
                            }
                            else
                            {
                                currentRow.SetCell(calculatedX, cellData.Coverage, cellData.Area);
                            }
                            cellData = cellData.Next;
                        }
                    }
                    currentDestIndex++;
                }
            }
        }

        /// <summary>
        /// Append rows into current row data. This method should be call as second approach.
        /// ( mean using Begin and Finish function)
        /// But before using this method must call Prepare row
        /// 
        /// This optimize for append text from left to right,
        /// so that row data ALWAYS append to end of current row data
        /// </summary>
        /// <param name="rows">rows</param>
        /// <param name="offsetX">offset x</param>
        /// <param name="offsetY">offset y</param>
        public void AppendRowDataAfter(RowData[] rows, double offsetX, double offsetY)
        {
            // first this will cast to integer value, 
            // need more implementation to make sure that these value will change the coverage
            // when out side clipping box
            if ((offsetY < ClippingBoxYMax)
                && (offsetX < ClippingBoxXMax))
            {
                int roundedOffsetY = (int)offsetY;
                int roundedOffsetX = (int)offsetX;
                #region start and end row
                double startY =
                     roundedOffsetY < ClippingBoxYMin ?
                     ClippingBoxYMin : roundedOffsetY; // max of two values
                int startRowIndex = (int)startY - (int)roundedOffsetY;

                double endY = roundedOffsetY + rows.Length > ClippingBoxYMax ?
                    ClippingBoxYMax : roundedOffsetY + rows.Length;
                int endRowIndex = (int)endY - roundedOffsetY;
                #endregion

                int startClippingX = (int)ClippingBoxXMin;
                int endClippingX = (int)ClippingBoxXMax;

                int startArea = (PixelScale - (int)(ClippingBoxXMin * PixelScale) & PixelMask) << PixelShift;
                int endArea = ((int)(ClippingBoxXMax * PixelScale) & PixelMask) << PixelShift;

                RowData currentRow = null;
                CellData cellData = null;


                int calculatedX = 0;
                int currentDestIndex = (int)startY;
                for (int row = startRowIndex; row < endRowIndex; row++)
                {
                    if (rows[row] != null)
                    {
                        currentRow = Rows[currentDestIndex];
                        cellData = rows[row].First;
                        while (cellData != null)
                        {
                            calculatedX = cellData.X + roundedOffsetX;
                            CurrentXPosition = calculatedX;
                            #region determine minx,maxy
                            if (CurrentXPosition < CurrentStartXIndex)
                            {
                                CurrentStartXIndex = (int)CurrentXPosition;
                            }
                            if (CurrentXPosition > CurrentEndXIndex)
                            {
                                CurrentEndXIndex = (int)CurrentXPosition + 1;
                            }
                            #endregion
                            if (calculatedX < startClippingX)
                            {
                                currentRow.SetCell(startClippingX, cellData.Coverage, 0);
                            }
                            else if (calculatedX > endClippingX)
                            {
                                currentRow.SetCell(endClippingX, cellData.Coverage, 0);
                            }
                            else
                            {
                                //currentRow.SetCell(calculatedX, cellData.Coverage, cellData.Area);
                                if (currentRow.First == null)
                                {
                                    currentRow.First = new CellData(calculatedX, cellData.Coverage, cellData.Area);
                                    currentRow.CurrentCell = currentRow.First;
                                }
                                else
                                {
                                    currentRow.CurrentCell.Next = new CellData(calculatedX, cellData.Coverage, cellData.Area);
                                    currentRow.CurrentCell = currentRow.CurrentCell.Next;
                                }
                            }
                            cellData = cellData.Next;
                        }
                    }
                    currentDestIndex++;
                }
            }
        }
        #endregion
    }
}
