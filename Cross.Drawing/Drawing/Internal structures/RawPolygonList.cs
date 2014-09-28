using System;

namespace Cross.Drawing
{
    /// <summary>
    /// Storing raw data in many polygon to many double array
    /// Each array is one polygon.
    /// </summary>
    /// <remarks>
    /// NOTE: must call at least one MoveTo before call line-to When finish must call Finish to clean up
    /// </remarks>
    internal class RawPolygonList
    {
        #region constant
        /// <summary>
        /// default polygon capacity when move to new polygon
        /// </summary>
        const int DefaultPolygonCapacity = 10;
        #endregion

        #region Fields
        #region public field
        ///// <summary>
        ///// indicate that when move to, this will be auto close all
        ///// </summary>
        //public bool IsAutoClose = true;

        /// <summary>
        /// internal raw data in format [x1,y1, x2,y2, ...]
        /// but all element in array is not exactly all point
        /// number of element can get by property Count
        /// </summary>
        internal double[][] RawDatas = null;

        /// <summary>
        /// Number of polygon
        /// </summary>
        internal int PolygonCount = 0;
        #endregion

        #region private fields
        /// <summary>
        /// Number of items currently in list
        /// </summary>
        internal int CurrentPolygonCount = 0;

        /// <summary>
        /// current polygon capacity
        /// </summary>
        internal int CurrentPolygonCapacity = 0;

        /// <summary>
        /// saving current polygon
        /// </summary>
        internal double[] CurrentPolygonRawData = null;
        #endregion

        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor.
        /// <para>Default capacity is 50</para>
        /// </summary>
        public RawPolygonList()
        {
            PolygonCount = 0;
        }
        #endregion

        #region line to ,move to
        /// <summary>
        /// Add a point to point collection
        /// </summary>
        /// <param name="x">x</param>
        /// <param name="y">y</param>
        public void LineTo(double x, double y)
        {
            // when exceed limit
            if (CurrentPolygonCount + 2 > CurrentPolygonCapacity)
            {
                if (RawDatas == null)
                {
                    MoveTo(x, y);
                    return;
                }
                int newCapacity;
                if (CurrentPolygonCapacity < 1000) newCapacity = (int)(CurrentPolygonCapacity * 2);
                else if (CurrentPolygonCapacity < 5000) newCapacity = (int)(CurrentPolygonCapacity * 1.5);
                else newCapacity = (int)(CurrentPolygonCapacity * 1.2);

                //allocate new buffer
                double[] newItems = new double[newCapacity];

                //copy old values to new buffer
                RawDatas[PolygonCount - 1].CopyTo(newItems, 0);

                //assign the new bufer to current buffer
                RawDatas[PolygonCount - 1] = newItems;
                CurrentPolygonCapacity = newCapacity;
                // reassign current raw data
                CurrentPolygonRawData = newItems;
            }
            if (CurrentPolygonCount > 0)
            {
                if (!((CurrentPolygonRawData[CurrentPolygonCount - 2] == x)
                && (CurrentPolygonRawData[CurrentPolygonCount - 1] == y)))
                {
                    CurrentPolygonRawData[CurrentPolygonCount++] = x;
                    CurrentPolygonRawData[CurrentPolygonCount++] = y;
                }
            }
            else
            {
                CurrentPolygonRawData[CurrentPolygonCount++] = x;
                CurrentPolygonRawData[CurrentPolygonCount++] = y;
            }
        }

        /// <summary>
        /// Move to new position and open new polygon
        /// </summary>
        /// <param name="x">x</param>
        /// <param name="y">y</param>
        public void MoveTo(double x, double y)
        {
            // check if current polygon
            if (CurrentPolygonCount == 2)
            {
                // when current polygon include one moveto only.
                // skip it, and change new polygon to this
                CurrentPolygonRawData[0] = x;
                CurrentPolygonRawData[1] = y;
            }
            else
            {
                // create new polygon
                if (PolygonCount == 0)
                {
                    // new
                    PolygonCount++;
                    RawDatas = new double[PolygonCount][];
                    CurrentPolygonCapacity = DefaultPolygonCapacity;
                    RawDatas[0] = new double[CurrentPolygonCapacity];
                    CurrentPolygonCount = 0;
                    CurrentPolygonRawData = RawDatas[0];
                }
                else
                {
                    // clean up current polygon
                    if (CurrentPolygonCount < CurrentPolygonCapacity)
                    {
                        double[] tempPolygon = new double[CurrentPolygonCount];
                        Array.Copy(RawDatas[PolygonCount - 1], tempPolygon, CurrentPolygonCount);
                        RawDatas[PolygonCount - 1] = tempPolygon;
                    }

                    PolygonCount++;
                    double[][] tempRawData = new double[PolygonCount][];
                    Array.Copy(RawDatas, tempRawData, PolygonCount - 1);
                    CurrentPolygonCapacity = DefaultPolygonCapacity;
                    tempRawData[PolygonCount - 1] = new double[CurrentPolygonCapacity];

                    //re assign to current raw data
                    RawDatas = tempRawData;
                    CurrentPolygonRawData = RawDatas[PolygonCount - 1];
                    CurrentPolygonCount = 0;
                }

                CurrentPolygonRawData[CurrentPolygonCount++] = x;
                CurrentPolygonRawData[CurrentPolygonCount++] = y;
            }
        }
        #endregion

        #region close current polygon
        /// <summary>
        /// Close current polygon
        /// </summary>
        public void CloseCurrentPolygon()
        {
            // close current polygon
            LineTo(CurrentPolygonRawData[0], CurrentPolygonRawData[1]);
        }
        #endregion

        #region append to current polygon
        /// <summary>
        /// Append coordinates to current polygon
        /// </summary>
        /// <param name="data">data in format [x1,y1,x2,y2,...]</param>
        public void AppendToCurrentPolygon(double[] data)
        {
            // create new polygon
            if (PolygonCount == 0)
            {
                // new
                PolygonCount++;
                RawDatas = new double[PolygonCount][];
                RawDatas[0] = data;
                CurrentPolygonCapacity = data.Length;
                CurrentPolygonCount = data.Length;
                CurrentPolygonRawData = RawDatas[0];
            }
            else
            {
                if (CurrentPolygonCount + data.Length > CurrentPolygonCapacity)
                {

                    int newCapacity = CurrentPolygonCapacity + data.Length + DefaultPolygonCapacity;
                    //allocate new buffer
                    double[] newItems = new double[newCapacity];

                    //copy old values to new buffer
                    RawDatas[PolygonCount - 1].CopyTo(newItems, 0);

                    //assign the new bufer to current buffer
                    RawDatas[PolygonCount - 1] = newItems;
                    CurrentPolygonCapacity = newCapacity;
                    // reassign current raw data
                    CurrentPolygonRawData = newItems;
                }

                Array.Copy(data, 0, CurrentPolygonRawData, CurrentPolygonCount, data.Length);
                CurrentPolygonCount += data.Length;
            }

        }
        #endregion

        #region Add new polygon
        /// <summary>
        /// add new polygon to current path
        /// </summary>
        /// <param name="data">data</param>
        public void AddNewPolygon(double[] data)
        {
            if (PolygonCount == 0)
            {
                PolygonCount++;
                RawDatas = new double[PolygonCount][];
                RawDatas[0] = data;
                CurrentPolygonCapacity = data.Length;
                CurrentPolygonCount = data.Length;
                CurrentPolygonRawData = data;
            }
            else
            {
                PolygonCount++;
                double[][] tempRawData = new double[PolygonCount][];
                Array.Copy(RawDatas, tempRawData, PolygonCount - 1);
                tempRawData[PolygonCount - 1] = data;
                RawDatas = tempRawData;
                CurrentPolygonCapacity = data.Length;
                CurrentPolygonCount = data.Length;
                CurrentPolygonRawData = data;
            }
        }
        #endregion

        #region finish and clean up
        /// <summary>
        /// Finish and Clean up polygons that have build
        /// </summary>
        public void Finish()
        {
            if (PolygonCount > 0)
            {
                // check if there is a move to command only.
                if (CurrentPolygonCount == 2)
                {
                    // remove it
                    double[][] temp = new double[PolygonCount - 1][];
                    Array.Copy(RawDatas, temp, PolygonCount - 1);
                    RawDatas = temp;
                    PolygonCount--;
                }
                else
                {
                    // clean up current polygon, cut not used lots.
                    if (CurrentPolygonCount < CurrentPolygonCapacity)
                    {
                        double[] tempPolygon = new double[CurrentPolygonCount];
                        Array.Copy(RawDatas[PolygonCount - 1], tempPolygon, CurrentPolygonCount);
                        RawDatas[PolygonCount - 1] = tempPolygon;
                    }
                }
            }
            else
            {
                // construct an empty stoke return.
                RawDatas = new double[][] { };
            }
        }
        #endregion
    }
}

