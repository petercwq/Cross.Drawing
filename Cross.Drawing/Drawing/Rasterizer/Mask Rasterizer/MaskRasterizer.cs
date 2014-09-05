using System;


namespace Cross.Drawing.Rasterizers.Analytical
{
    /// <summary>
    /// Implement rasterizer to build up an mask array
    /// <remarks>Mask rasterizer can be used by two different approaches:
    /// <para>   Aprroach 1: call method BuildMaskForPolygon to rasterize polygon and render to mask buffer directly in one pass</para>
    /// <para>   Approach 2: call the following methods sequentially to complete a renderation</para>
    /// <para>        + Begin() to start renderation process</para>
    /// <para>        + LineTo(), MoveTo(), AddPolygon to rasterize the vectors to renderation mask</para>
    /// <para>        + Finish() to end renderation process and render mask to buffer</para>
    /// <para>NOTE: must set Filling rule property to determine the way to build mask for polygon</para>
    /// </summary>
    /// <remarks>
    /// For better maintain code rasterize lines, 
    /// this implement Analytical rasterizer but not use pixel buffer as dest buffer
    /// </remarks>
    /*internal*/
    public class MaskRasterizer : AnalyticalAlgorithmImplement, IPolygonRasterizer
    {
        #region Constructors
        /// <summary>
        /// Default constructor for MaskRasterizer
        /// </summary>
        public MaskRasterizer()
        { }
        #endregion

        #region NOT USE PROPERTY
        #region Buffer
        /// <summary>
        /// Gets/Sets Null buffer for Mask rasterizer
        /// </summary>
        public PixelBuffer Buffer
        {
            get { return null; }
            set { }
        }
        #endregion

        //#region WindingRule
        //private WindingRule mWindingRule;
        ///// <summary>
        ///// Gets/Sets Desciption
        ///// </summary>
        //public WindingRule WindingRule
        //{
        //    get { return mWindingRule; }
        //    set { mWindingRule = value; }
        //}
        //#endregion  

        #region Paint
        private PaintMaterial mPaint;
        /// <summary>
        /// Gets/Sets paint material using for paint. This not implemented in MaskRasterizer
        /// </summary>
        public PaintMaterial Paint
        {
            get { return mPaint; }
            set { mPaint = value; }
        }
        #endregion
        #region Gamma
        /// <summary>
        /// current gamma function
        /// </summary>
        protected IGammaCorrector mGamma;
        /// <summary>
        /// Gets/Sets gamma corrector function. When null, it will not apply gamma corrector
        /// </summary>
        public IGammaCorrector Gamma
        {
            get { return mGamma; }
            set { mGamma = value; }
        }
        #endregion


        #endregion

        #region ResultMask
        private MaskBuffer mResultMask;
        /// <summary>
        /// Gets/Sets result of rasterizer
        /// </summary>
        public MaskBuffer ResultMask
        {
            get { return mResultMask; }
            set
            {
                mResultMask = value;
                PrepareBuffer(mResultMask);
                //if (mResultMask != null)
                //{
                //    //DestBufferWidth = mResultMask.Width;
                //    //DestBufferHeight = mResultMask.Height;
                //    //// get clip box
                //    //SetClipBox(0, 0,
                //    //    mResultMask.Width,
                //    //    mResultMask.Height);
                //}
            }
        }
        #endregion

        #region FillingRule
        private FillingRule mFillingRule;
        /// <summary>
        /// Gets/Sets Desciption
        /// </summary>
        public FillingRule FillingRule
        {
            get { return mFillingRule; }
            set { mFillingRule = value; }
        }
        #endregion

        #region OpacityMask
        private MaskBuffer mOpacityMask;
        /// <summary>
        /// Gets/Sets mask buffer apply for mask builder
        /// </summary>
        public MaskBuffer OpacityMask
        {
            get { return mOpacityMask; }
            set { mOpacityMask = value; }
        }
        #endregion

        #region PrepareBuffer
        /// <summary>
        /// Preparing buffer and internal data for using
        /// </summary>
        /// <param name="buffer">buffer</param>
        protected virtual void PrepareBuffer(MaskBuffer buffer)
        {
            //automatically create a new boundary to match this buffer
            if (buffer == null)
            {
                //mBoundary = Boundary.Empty;
                DestBufferWidth = 0;
                DestBufferHeight = 0;
                //maskst = 0;
                //BufferStartOffset = 0;
                //BufferData = null;
            }
            else
            {
                //mBoundary = new Boundary(mBuffer.Width, mBuffer.Height);
                DestBufferWidth = buffer.Width;
                DestBufferHeight = buffer.Height;
                //BufferStride = mBuffer.Stride;
                //BufferStartOffset = mBuffer.StartOffset;
                //BufferData = mBuffer.Data;
                //mPixelRenderer.PixelBuffer = mBuffer;
            }
            if ((Rows == null) || (Rows.Length < DestBufferHeight + 1))
            {
                Rows = new RowData[DestBufferHeight + 1];
            }
            SetClip(0, 0, DestBufferWidth, DestBufferHeight);

        }
        #endregion

        //=============== RASTERIZE METHODS
        #region Direct approach
        #region Fill polygon
        /// <summary>
        /// Rasterize and fill the polygon directly in one pass. This approach is slightly faster than the normal renderation process (Begin, Addxxx, Finish)
        /// </summary>
        /// <param name="paint">The paint material used for filling</param>
        /// <param name="data">raw data array in format [x1,y1, x2,y2, ...]</param>
        /// <param name="pointCount">Number of points contained within data</param>
        /// <param name="startOffset">Index of the first point in data </param>
        public void FillPolygon(PaintMaterial paint, double[] data, int pointCount, int startOffset)
        {
            if (IsClipBoxOutSideBound)
            {
                return;
            }
            int endIndex = startOffset + pointCount * 2;
            #region determine the startY,endY
            // Start,end y for drawing
            int endRowPosition = int.MinValue;
            int startRowPosition = int.MaxValue;
            CurrentStartXIndex = int.MaxValue;
            CurrentEndXIndex = int.MinValue;
            for (int i = startOffset; i < endIndex; i += 2)
            {
                if (data[i] > CurrentEndXIndex)
                {
                    CurrentEndXIndex = (int)data[i] + 1;
                }
                if (data[i] < CurrentStartXIndex)
                {
                    CurrentStartXIndex = (int)data[i];
                }
                if (data[i + 1] > endRowPosition)
                {
                    endRowPosition = (int)data[i + 1] + 1;
                }
                if (data[i + 1] < startRowPosition)
                {
                    startRowPosition = (int)data[i + 1];
                }
            }
            #endregion
            #region prepare Rows array
            startRowPosition--;
            endRowPosition++;

            if (startRowPosition < ClippingBoxYMin)
            {
                startRowPosition = (int)ClippingBoxYMin;
            }
            if (endRowPosition > ClippingBoxYMax + 1)
            {
                endRowPosition = (int)ClippingBoxYMax + 1;
            }

            for (int rowIndex = startRowPosition; rowIndex <= endRowPosition; rowIndex++)
            {
                Rows[rowIndex] = new RowData();
            }
            #endregion
            #region draw lines

            CurrentXPosition = data[startOffset];
            CurrentYPosition = data[startOffset + 1];
            CurrentPositionFlag =
               ((CurrentXPosition > ClippingBoxXMax) ? XMaxClippingFlag :
               (CurrentXPosition < ClippingBoxXMin) ? XMinClippingFlag : 0)
               |
               ((CurrentYPosition > ClippingBoxYMax) ? YMaxClippingFlag :
               (CurrentYPosition < ClippingBoxYMin) ? YMinClippingFlag : 0);

            for (int i = startOffset + 2; i < endIndex; i += 2)
            {
                if (CurrentYPosition != data[i + 1])
                {
                    DrawAndClippedLine(data[i], data[i + 1]);
                }
                else
                {
                    // just move to and calculate the flag
                    CurrentXPosition = data[i];
                    CurrentYPosition = data[i + 1];
                    CurrentPositionFlag =
                      ((CurrentXPosition > ClippingBoxXMax) ? XMaxClippingFlag :
                      (CurrentXPosition < ClippingBoxXMin) ? XMinClippingFlag : 0)
                      |
                      ((CurrentYPosition > ClippingBoxYMax) ? YMaxClippingFlag :
                      (CurrentYPosition < ClippingBoxYMin) ? YMinClippingFlag : 0);
                }
            }

            if (CurrentYPosition != data[startOffset + 1])
            {
                DrawAndClippedLine(data[startOffset], data[startOffset + 1]);
            }

            #endregion
            #region fill
            BuildMask(Rows, startRowPosition, endRowPosition);

            #endregion

        }
        #endregion
        #region Fill polygon
        /// <summary>
        /// Rasterize and fill the polygon directly in one pass. This approach is slightly faster than the normal renderation process (Begin, Addxxx, Finish)
        /// </summary>
        /// <param name="paint">The paint material used for filling</param>
        /// <param name="data">raw data array in format [x1,y1, x2,y2, ...]</param>
        /// <param name="pointCount">Number of points contained within data</param>
        /// <param name="startOffset">Index of the first point in data </param>
        /// <param name="offsetX">offseted X</param>
        /// <param name="offsetY">offseted Y</param>
        public void FillPolygon(PaintMaterial paint, double[] data, int pointCount, int startOffset, double offsetX, double offsetY)
        {
            if (IsClipBoxOutSideBound)
            {
                return;
            }
            double calculatedX = 0, calculatedY = 0;

            int endIndex = startOffset + pointCount * 2;
            #region determine the startY,endY
            // Start,end y for drawing
            int endRowPosition = int.MinValue;
            int startRowPosition = int.MaxValue;
            CurrentStartXIndex = int.MaxValue;
            CurrentEndXIndex = int.MinValue;
            for (int i = startOffset; i < endIndex; i += 2)
            {
                calculatedX = data[i] + offsetX;
                calculatedY = data[i + 1] + offsetY;
                if (calculatedX > CurrentEndXIndex)
                {
                    CurrentEndXIndex = (int)calculatedX + 1;
                }
                if (calculatedX < CurrentStartXIndex)
                {
                    CurrentStartXIndex = (int)calculatedX;
                }
                if (calculatedY > endRowPosition)
                {
                    endRowPosition = (int)calculatedY + 1;
                }
                if (calculatedY < startRowPosition)
                {
                    startRowPosition = (int)calculatedY;
                }
            }
            #endregion
            #region prepare Rows array
            startRowPosition--;
            endRowPosition++;

            if (startRowPosition < ClippingBoxYMin)
            {
                startRowPosition = (int)ClippingBoxYMin;
            }
            if (endRowPosition > ClippingBoxYMax + 1)
            {
                endRowPosition = (int)ClippingBoxYMax + 1;
            }

            for (int rowIndex = startRowPosition; rowIndex <= endRowPosition; rowIndex++)
            {
                Rows[rowIndex] = new RowData();
            }
            #endregion
            #region draw lines

            CurrentXPosition = data[startOffset] + offsetX;
            CurrentYPosition = data[startOffset + 1] + offsetY;
            CurrentPositionFlag =
               ((CurrentXPosition > ClippingBoxXMax) ? XMaxClippingFlag :
               (CurrentXPosition < ClippingBoxXMin) ? XMinClippingFlag : 0)
               |
               ((CurrentYPosition > ClippingBoxYMax) ? YMaxClippingFlag :
               (CurrentYPosition < ClippingBoxYMin) ? YMinClippingFlag : 0);

            for (int i = startOffset + 2; i < endIndex; i += 2)
            {
                if (CurrentYPosition != data[i + 1] + offsetY)
                {
                    DrawAndClippedLine(data[i] + offsetX, data[i + 1] + offsetY);
                }
                else
                {
                    // just move to and calculate the flag
                    CurrentXPosition = data[i] + offsetX;
                    CurrentYPosition = data[i + 1] + offsetY;
                    CurrentPositionFlag =
                      ((CurrentXPosition > ClippingBoxXMax) ? XMaxClippingFlag :
                      (CurrentXPosition < ClippingBoxXMin) ? XMinClippingFlag : 0)
                      |
                      ((CurrentYPosition > ClippingBoxYMax) ? YMaxClippingFlag :
                      (CurrentYPosition < ClippingBoxYMin) ? YMinClippingFlag : 0);
                }
            }

            if (CurrentYPosition != data[startOffset + 1] + offsetY)
            {
                DrawAndClippedLine(data[startOffset] + offsetX, data[startOffset + 1] + offsetY);
            }

            #endregion
            #region fill
            BuildMask(Rows, startRowPosition, endRowPosition);
            //Filling(paint, Rows, startRowPosition, endRowPosition);

            #endregion

        }
        #endregion
        #region draw polygon
        /// <summary>
        /// Rasterize and draw the polygon using 1-pixel wide pen
        /// </summary>
        /// <param name="data">raw data array in format [x1,y1, x2,y2, ...]</param>
        /// <param name="pointCount">Number of points contained within data</param>
        /// <param name="startOffset">Index of the first point in data </param>
        public void DrawPolygon(Color color, double[] data, int pointCount, int startOffset)
        {
            throw new NotImplementedException("This method need to implement by using WuLine");
        }
        #endregion
        #endregion

        #region Normal approach
        #region field to saving the first point of polygon
        bool isTheFirst = true;
        double firstPointX = 0;
        double firstPointY = 0;
        #endregion

        #region begin rasterizer
        /// <summary>
        /// Begin rasterize polygons into same buffer
        /// </summary>
        public void Begin()
        {
            CurrentStartYIndex = int.MaxValue;
            CurrentStartXIndex = int.MaxValue;
            CurrentEndYIndex = int.MinValue;
            CurrentEndYIndex = int.MinValue;
            isTheFirst = true;
        }
        /// <summary>
        /// Begin rasterize polygons into same buffer, including set clipping box
        /// </summary>
        /// <param name="left">left of clip box</param>
        /// <param name="top">top of clip box</param>
        /// <param name="right">right of clip box</param>
        /// <param name="bottom">bottom of clip box</param>
        public void Begin(double left, double top, double right, double bottom)
        {
            SetClip(left, top, right, bottom);
            Begin();
        }
        #endregion

        #region MOVE TO - LINE TO
        #region move to
        /// <summary>
        /// Move to (x,y) , start for a new polygon
        /// </summary>
        /// <param name="x">x coordinate</param>
        /// <param name="y">y coordinate</param>
        public void MoveTo(double x, double y)
        {
            #region calculate first point x,y
            // when not the first
            if (!isTheFirst)
            {
                // close last polygon
                DrawAndClippedLine(firstPointX, firstPointY);
            }
            else
            {
                isTheFirst = false;
            }
            // set first point of polygon
            firstPointX = x;
            firstPointY = y;
            #endregion

            #region determine minx,maxy
            if (x < CurrentStartXIndex)
            {
                CurrentStartXIndex = (int)x;
            }
            if (x > CurrentEndXIndex)
            {
                CurrentEndXIndex = (int)x + 1;
            }
            #endregion

            // change current point and check flag
            CurrentXPosition = x;
            CurrentYPosition = y;
            // calculate current position flag
            CurrentPositionFlag =
               ((CurrentXPosition > ClippingBoxXMax) ? XMaxClippingFlag :
               (CurrentXPosition < ClippingBoxXMin) ? XMinClippingFlag : 0)
               |
               ((CurrentYPosition > ClippingBoxYMax) ? YMaxClippingFlag :
               (CurrentYPosition < ClippingBoxYMin) ? YMinClippingFlag : 0);

            PrepareRows((int)CurrentYPosition);
        }
        #endregion

        #region line to
        /// <summary>
        /// Rasterize a line
        /// </summary>
        /// <param name="x">x coordinate</param>
        /// <param name="y">y coordinate</param>
        public void LineTo(double x, double y)
        {
            #region determine minx,maxy
            if (x < CurrentStartXIndex)
            {
                CurrentStartXIndex = (int)x;
            }
            if (x > CurrentEndXIndex)
            {
                CurrentEndXIndex = (int)x + 1;
            }
            #endregion

            // when horizontal line, just move to
            if (y == CurrentYPosition)
            {
                // change current point and check flag
                CurrentXPosition = x;
                CurrentYPosition = y;
                // calculate current position flag
                CurrentPositionFlag =
                   ((CurrentXPosition > ClippingBoxXMax) ? XMaxClippingFlag :
                   (CurrentXPosition < ClippingBoxXMin) ? XMinClippingFlag : 0)
                   |
                   ((CurrentYPosition > ClippingBoxYMax) ? YMaxClippingFlag :
                   (CurrentYPosition < ClippingBoxYMin) ? YMinClippingFlag : 0);

                // do not need prepare row for current row
                //PrepareRows((int)currentYPosition);
            }
            else
            {
                // prepare row
                PrepareRows((int)y);
                DrawAndClippedLine(x, y);
            }
        }
        #endregion
        #endregion

        #region AddPolygon
        /// <summary>
        /// Rasterize a polygon
        /// </summary>
        /// <param name="data">raw data array in format [x1,y1, x2,y2, ...]</param>
        /// <param name="pointCount">Number of points contained within data</param>
        /// <param name="startOffset">Index of the first point in data </param>
        public void AddPolygon(double[] data, int pointCount, int startOffset)
        {
            int endIndex = startOffset + pointCount * 2;
            #region close last polygon
            // when not the first
            if (!isTheFirst)
            {
                // close last polygon
                DrawAndClippedLine(firstPointX, firstPointY);
            }
            else
            {
                isTheFirst = false;
            }
            // set first point of polygon
            firstPointX = data[startOffset];
            firstPointY = data[startOffset + 1];
            #endregion


            CurrentXPosition = data[startOffset];
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
            CurrentYPosition = data[startOffset + 1];
            CurrentPositionFlag =
               ((CurrentXPosition > ClippingBoxXMax) ? XMaxClippingFlag :
               (CurrentXPosition < ClippingBoxXMin) ? XMinClippingFlag : 0)
               |
               ((CurrentYPosition > ClippingBoxYMax) ? YMaxClippingFlag :
               (CurrentYPosition < ClippingBoxYMin) ? YMinClippingFlag : 0);
            PrepareRows((int)CurrentYPosition);
            for (int i = startOffset + 2; i < endIndex; i += 2)
            {
                if (CurrentYPosition != data[i + 1])
                {
                    PrepareRows((int)data[i + 1]);
                    DrawAndClippedLine(data[i], data[i + 1]);
                }
                else
                {
                    // just move to and calculate the flag
                    CurrentXPosition = data[i];
                    CurrentYPosition = data[i + 1];
                    //PrepareRows((int)data[i + 1]);
                    CurrentPositionFlag =
                      ((CurrentXPosition > ClippingBoxXMax) ? XMaxClippingFlag :
                      (CurrentXPosition < ClippingBoxXMin) ? XMinClippingFlag : 0)
                      |
                      ((CurrentYPosition > ClippingBoxYMax) ? YMaxClippingFlag :
                      (CurrentYPosition < ClippingBoxYMin) ? YMinClippingFlag : 0);
                }

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
            }

        }
        #endregion

        #region AddPolygon
        /// <summary>
        /// Rasterize a polygon
        /// </summary>
        /// <param name="data">raw data array in format [x1,y1, x2,y2, ...]</param>
        /// <param name="pointCount">Number of points contained within data</param>
        /// <param name="startOffset">Index of the first point in data </param>
        public void AddPolygon(double[] data, int pointCount, int startOffset, double offsetX, double offsetY)
        {
            int endIndex = startOffset + pointCount * 2;
            #region close last polygon
            // when not the first
            if (!isTheFirst)
            {
                // close last polygon
                DrawAndClippedLine(firstPointX, firstPointY);
            }
            else
            {
                isTheFirst = false;
            }
            // set first point of polygon
            firstPointX = data[startOffset] + offsetX;
            firstPointY = data[startOffset + 1] + offsetY;
            #endregion


            CurrentXPosition = data[startOffset] + offsetX;
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
            CurrentYPosition = data[startOffset + 1] + offsetY;
            CurrentPositionFlag =
               ((CurrentXPosition > ClippingBoxXMax) ? XMaxClippingFlag :
               (CurrentXPosition < ClippingBoxXMin) ? XMinClippingFlag : 0)
               |
               ((CurrentYPosition > ClippingBoxYMax) ? YMaxClippingFlag :
               (CurrentYPosition < ClippingBoxYMin) ? YMinClippingFlag : 0);
            PrepareRows((int)CurrentYPosition);
            double calculatedY = 0;
            double calculatedX = 0;
            for (int i = startOffset + 2; i < endIndex; i += 2)
            {
                calculatedX = data[i] + offsetX;
                calculatedY = data[i + 1] + offsetY;
                if (CurrentYPosition != calculatedY)
                {
                    PrepareRows((int)calculatedY);
                    DrawAndClippedLine(calculatedX, calculatedY);
                }
                else
                {
                    // just move to and calculate the flag
                    CurrentXPosition = calculatedX;
                    CurrentYPosition = calculatedY;
                    //PrepareRows((int)data[i + 1]);
                    CurrentPositionFlag =
                      ((CurrentXPosition > ClippingBoxXMax) ? XMaxClippingFlag :
                      (CurrentXPosition < ClippingBoxXMin) ? XMinClippingFlag : 0)
                      |
                      ((CurrentYPosition > ClippingBoxYMax) ? YMaxClippingFlag :
                      (CurrentYPosition < ClippingBoxYMin) ? YMinClippingFlag : 0);
                }

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
            }

        }
        #endregion

        #region Finish
        /// <summary>
        /// Filling into buffer by using current rasterized result
        /// </summary>
        public void Finish()
        {
            #region close last polygon
            // when not the first
            // close last
            if (!isTheFirst)
            {
                // close last polygon
                DrawAndClippedLine(firstPointX, firstPointY);
            }
            #endregion

            //Filling(mPaint, Rows, CurrentStartYIndex, CurrentEndYIndex);
            BuildMask(Rows, CurrentStartYIndex, CurrentEndYIndex);
        }
        #endregion


        #region Finish
        /// <summary>
        /// Filling into buffer by using current rasterized result
        /// </summary>
        public void FinishWithoutFilling()
        {
            #region close last polygon
            // when not the first
            // close last
            if (!isTheFirst)
            {
                // close last polygon
                DrawAndClippedLine(firstPointX, firstPointY);
            }
            #endregion
        }
        #endregion

        #endregion

        #region BuildMask
        /// <summary>
        /// Build mask base on rows result of rasterizer
        /// </summary>
        /// <param name="rows">rasterized result</param>
        /// <param name="startRowPosition">start row</param>
        /// <param name="endRowPosition">end row</param>
        protected void BuildMask(RowData[] rows, int startRowPosition, int endRowPosition)
        {
            if (mFillingRule == FillingRule.NonZero)
            {
                OnBuildingNonZero(rows, startRowPosition, endRowPosition);
            }
            else
            {
                OnBuildingEvenOdd(rows, startRowPosition, endRowPosition);
            }
        }
        #endregion

        #region Non zero
        /// <summary>
        /// Fill to buffer base rows data information using non zero rule
        /// </summary>
        /// <param name="paint">paint using for fill</param>
        /// <param name="rows">row data information</param>
        /// <param name="startYIndex">start row index in row array need to draw</param>
        /// <param name="endYIndex">end row index in end row array need to draw</param>
        protected void OnBuildingNonZero(RowData[] rows, int startYIndex, int endYIndex)
        {
            #region private variable for filling
            int currentCoverage, scLastCoverage, scLastX = 0;
            int tempCover = 0;
            int currentArea = 0;
            int lastXPosition = 0;
            int startXPosition = 0;
            CellData currentCellData = null;
            byte calculatedCoverage = 0;

            byte[] MaskData = mResultMask.Data;

            int maskStartOffset = mResultMask.StartOffset;
            int maskStride = mResultMask.Stride;
            #endregion
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
                                //scLastCoverage = (byte)((scLastCoverage * colorAlpha) >> 8);

                                #region BLEND HORIZONTAL LINE
                                // calculate start and end position
                                startXPosition = maskStartOffset + startYIndex * maskStride + scLastX + 1;
                                lastXPosition = maskStartOffset + startYIndex * maskStride + currentCellData.X;
                                calculatedCoverage = (byte)scLastCoverage;
                                while (startXPosition < lastXPosition)
                                {
                                    MaskData[startXPosition++] = calculatedCoverage;
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

                                startXPosition = maskStartOffset + startYIndex * maskStride + currentCellData.X;
                                MaskData[startXPosition] = (byte)tempCover;
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
        #endregion

        #region Even odd
        /// <summary>
        /// Fill to buffer base rows data information using EvenOdd rule
        /// </summary>
        /// <param name="paint">paint using for fill</param>
        /// <param name="rows">row data information</param>
        /// <param name="startYIndex">start row index in row array need to draw</param>
        /// <param name="endYIndex">end row index in end row array need to draw</param>
        protected void OnBuildingEvenOdd(RowData[] rows, int startYIndex, int endYIndex)
        {

            #region private variable for filling
            int currentCoverage, scLastCoverage, scLastX = 0;
            int tempCover = 0;
            int currentArea = 0;
            int lastXPosition = 0;
            int startXPosition = 0;
            CellData currentCellData = null;
            byte calculatedCoverage = 0;

            byte[] MaskData = mResultMask.Data;

            int maskStartOffset = mResultMask.StartOffset;
            int maskStride = mResultMask.Stride;
            #endregion

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
                                    startXPosition = maskStartOffset + startYIndex * maskStride + scLastX + 1;
                                    lastXPosition = maskStartOffset + startYIndex * maskStride + currentCellData.X;
                                    calculatedCoverage = (byte)scLastCoverage;
                                    while (startXPosition < lastXPosition)
                                    {
                                        MaskData[startXPosition++] = calculatedCoverage;
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
                                startXPosition = maskStartOffset + startYIndex * maskStride + currentCellData.X;
                                MaskData[startXPosition] = (byte)tempCover;
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
        #endregion
    }
}
