#region Using directives
using System;
#endregion

namespace Cross.Drawing.Rasterizers.Analytical
{
    /// <summary>
    /// Rasterizer base using analytical method
    /// <para>This calculate accurate coverage at each cell</para>
    /// <para>This method using <see cref="RowData"/> structure to saving immediate result, 
    /// before fill to buffer.Each<see cref="CellData"></see> include Area,Coverage to determine
    /// a line cross it, and area of divided field. Base on this information, filler can fill and 
    /// calculate exact coverage value</para>
    /// 
    /// <para>CellData include two important information
    ///     + Area: area that seperated by line cross this cell
    ///     + Coverage : delta scaled y across this cell.
    /// This implementation always make sure that coverage for a line
    /// cut one row fully, total coverage of cell in same row is 256
    /// </para>
    /// 
    /// <para>Note that input paint will not check null in this class and sub-class.
    /// PaintMaterial and its Paint property should not be null when calling
    /// FillPolygon or assign to Paint of current rasterizer.
    /// </para>
    /// </summary>
    /*internal*/
    public abstract class AnalyticalRasterizerBase : AnalyticalAlgorithmImplement, IPolygonRasterizer
    {

        #region BUFFER
        #region protected fields for buffer
        /// <summary>
        /// Buffer data array, the main data using for contain filling result
        /// </summary>
        protected uint[] BufferData = null;

        /// <summary>
        /// Start offset of buffer
        /// </summary>
        protected int BufferStartOffset = 0;

        /// <summary>
        /// Stride of buffer
        /// </summary>
        protected int BufferStride = 0;


        #endregion

        #region Buffer
        private PixelBuffer mBuffer;
        /// <summary>
        /// Gets/Sets pixel buffer
        /// </summary>
        public PixelBuffer Buffer
        {
            get { return mBuffer; }
            set
            {
                mBuffer = value;
                PrepareBuffer(mBuffer);
            }
        }
        #endregion
        #endregion


        //#region WindingRule
        //private WindingRule mWindingRule;
        ///// <summary>
        ///// Gets/Sets winding rule
        ///// </summary>
        //public WindingRule WindingRule
        //{
        //    get { return mWindingRule; }
        //    set { mWindingRule = value; }
        //}
        //#endregion  

        #region FillingRule
        //private FillingRule mFillingRule;
        ///// <summary>
        ///// Gets/Sets Desciption
        ///// </summary>
        //public FillingRule FillingRule
        //{
        //    get { return mFillingRule; }
        //    set { mFillingRule = value; }
        //}
        #endregion

        #region Paint
        private PaintMaterial mPaint;
        /// <summary>
        /// Gets/Sets paint using in normal approach
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

        #region OpacityMask
        protected MaskBuffer mOpacityMask;
        /// <summary>
        /// Gets/Sets the opacity mask used for clipping based on opacity masking.
        /// <para>Default is null (no opacity mask operation is required)</para>
        /// </summary>
        public MaskBuffer OpacityMask
        {
            get { return mOpacityMask; }
            set
            {
                mOpacityMask = value;
                if (mOpacityMask != null)
                {
                    MaskStartX = mOpacityMask.Left;
                    MaskStartY = mOpacityMask.Top;

                    MaskEndX = MaskStartX + mOpacityMask.Width;
                    MaskEndY = MaskStartY + mOpacityMask.Height;

                    MaskStride = mOpacityMask.Stride;
                    MaskStartOffset = mOpacityMask.StartOffset;

                    MaskData = mOpacityMask.Data;
                }
            }
        }
        #endregion

        #region private field for opacity mask
        /// <summary>
        /// Saving mask data
        /// </summary>
        protected byte[] MaskData;

        /// <summary>
        /// Saving bounding rect of mask
        /// </summary>
        protected int MaskStartX = 0;
        /// <summary>
        /// Saving bounding rect of mask
        /// </summary>
        protected int MaskEndX = 0;
        /// <summary>
        /// Saving bounding rect of mask
        /// </summary>
        protected int MaskStartY = 0;
        /// <summary>
        /// Saving bounding rect of mask
        /// </summary>
        protected int MaskEndY = 0;
        /// <summary>
        /// mask stride
        /// </summary>
        protected int MaskStride = 0;
        /// <summary>
        /// mask height
        /// </summary>
        protected int MaskHeight = 0;


        /// <summary>
        /// Saving start offset of mask
        /// </summary>
        protected int MaskStartOffset = 0;
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
            Filling(paint, Rows, startRowPosition, endRowPosition);

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
            Filling(paint, Rows, startRowPosition, endRowPosition);

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
            if (IsClipBoxOutSideBound) return;
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

            Filling(mPaint, Rows, CurrentStartYIndex, CurrentEndYIndex);
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

        // ======= VIRTUAL METHODS

        #region PrepareBuffer
        /// <summary>
        /// Preparing buffer and internal data for using
        /// </summary>
        /// <param name="buffer">buffer</param>
        protected virtual void PrepareBuffer(PixelBuffer buffer)
        {
            //automatically create a new boundary to match this buffer
            if (mBuffer == null)
            {
                //mBoundary = Boundary.Empty;
                DestBufferWidth = 0;
                DestBufferHeight = 0;
                BufferStride = 0;
                BufferStartOffset = 0;
                BufferData = null;
            }
            else
            {
                //mBoundary = new Boundary(mBuffer.Width, mBuffer.Height);
                DestBufferWidth = mBuffer.Width;
                DestBufferHeight = mBuffer.Height;
                BufferStride = mBuffer.Stride;
                BufferStartOffset = mBuffer.StartOffset;
                BufferData = mBuffer.Data;
                //mPixelRenderer.PixelBuffer = mBuffer;
            }
            if ((Rows == null) || (Rows.Length < DestBufferHeight + 1))
            {
                Rows = new RowData[DestBufferHeight + 1];
            }
            SetClip(0, 0, DestBufferWidth, DestBufferHeight);

        }
        #endregion

        #region On filling
        /// <summary>
        /// Filling method including prepare material and perform filling
        /// </summary>
        /// <param name="paint">paint</param>
        /// <param name="rows">rows</param>
        /// <param name="startYIndex">start y index</param>
        /// <param name="endYIndex">end y index</param>
        protected void Filling(PaintMaterial paint, RowData[] rows, int startYIndex, int endYIndex)
        {
            //this.CurrentPaint = paint;
            PrepareMaterial(paint, CurrentStartXIndex, startYIndex, CurrentEndXIndex, endYIndex);
            OnFilling(paint, rows, startYIndex, endYIndex);
            OnFinishFilling();
        }

        /// <summary>
        /// Prepare material for paint,including define the bounding box of drawing
        /// </summary>
        /// <param name="paint">paint</param>
        /// <param name="startXIndex">start x index</param>
        /// <param name="startYIndex">start y index</param>
        /// <param name="endXIndex">end x index</param>
        /// <param name="endYIndex">end y index</param>
        protected virtual void PrepareMaterial(PaintMaterial paint, int startXIndex, int startYIndex, int endXIndex, int endYIndex)
        {
        }

        /// <summary>
        /// Finishing filling in rasterizer
        /// </summary>
        protected virtual void OnFinishFilling()
        {
        }

        /// <summary>
        /// Perform filling into rasterized result 
        /// </summary>
        /// <param name="paint">paint</param>
        /// <param name="rows">rows</param>
        /// <param name="startYIndex">start y index</param>
        /// <param name="endYIndex">end y index</param>
        protected virtual void OnFilling(PaintMaterial paint, RowData[] rows, int startYIndex, int endYIndex)
        {
            //outside of box, no need to fill
            if (IsClipBoxOutSideBound) return;
            //Cross.Log.Debug("Start row {0}, end rows {1}",startYIndex,endYIndex);
            // when gamma function is assigned and gamma function need to apply
            //if ((mGamma != null) &&(mGamma.IsAppliedGamma))
            if ((mGamma != null))
            {
                if (paint.FillingRule == FillingRule.NonZero)
                {
                    // fill non-zero including gamma
                    OnFillingNonZero(paint, Rows, startYIndex, endYIndex, mGamma.GetLookupTableRed(), mGamma.GetLookupTableGreen(), mGamma.GetLookupTableBlue());
                }
                else
                {
                    // fill Even-Odd including gamma
                    OnFillingEvenOdd(paint, Rows, startYIndex, endYIndex, mGamma.GetLookupTableRed(), mGamma.GetLookupTableGreen(), mGamma.GetLookupTableBlue());
                }
            }
            else
            {
                //Cross.Log.Debug("Filling without gamma");
                if (paint.FillingRule == FillingRule.NonZero)
                {
                    OnFillingNonZero(paint, Rows, startYIndex, endYIndex);
                }
                else
                {
                    OnFillingEvenOdd(paint, Rows, startYIndex, endYIndex);
                }
            }
        }
        #endregion

        // ======= ABSTRACT METHODS
        #region Fill Non Zero
        /// <summary>
        /// Fill to buffer base rows data information using non zero rule
        /// </summary>
        /// <param name="paint">paint using for fill</param>
        /// <param name="rows">row data information</param>
        /// <param name="startRowIndex">start row index in row array need to draw</param>
        /// <param name="endRowIndex">end row index in end row array need to draw</param>
        protected abstract void OnFillingNonZero(PaintMaterial paint, RowData[] rows, int startRowIndex, int endRowIndex);

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
        protected abstract void OnFillingNonZero(PaintMaterial paint, RowData[] rows, int startRowIndex, int endRowIndex, byte[] gammaLutRed, byte[] gammaLutGreen, byte[] gammaLutBlue);
        #endregion

        #region Fill Even Odd
        /// <summary>
        /// Fill to buffer base rows data information using even odd rule
        /// </summary>
        /// <param name="paint">paint using for fill</param>
        /// <param name="rows">row data information</param>
        /// <param name="startRowIndex">start row index in row array need to draw</param>
        /// <param name="endRowIndex">end row index in end row array need to draw</param>
        protected abstract void OnFillingEvenOdd(PaintMaterial paint, RowData[] rows, int startRowIndex, int endRowIndex);


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
        protected abstract void OnFillingEvenOdd(PaintMaterial paint, RowData[] rows, int startRowIndex, int endRowIndex, byte[] gammaLutRed, byte[] gammaLutGreen, byte[] gammaLutBlue);

        #endregion

        #region CONSTRUCTOR
        /// <summary>
        /// Alpha cache for blending
        /// </summary>
        protected uint[] AlphaCache = null;

        /// <summary>
        /// Default constructor and initialize values
        /// </summary>
        public AnalyticalRasterizerBase()
        {
            AlphaCache = Cross.Drawing.AlphaCache.Cache;
            //AlphaCache = new byte[256 * 256];
            //for (int alpha = 0; alpha < 256; alpha++)
            //{
            //    for (int beta = 0; beta < 256; beta++)
            //    {
            //        int alphaIdx = alpha * 256 + beta;
            //        AlphaCache[alphaIdx] = (byte)((alpha + beta) - ((beta * alpha + 255) >> 8));
            //    }
            //}
        }
        #endregion


    }
}
