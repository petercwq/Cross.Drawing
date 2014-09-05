


namespace Cross.Drawing.Rasterizers.Analytical
{
    /// <summary>
    /// Structure to saving cell data, coverage,area
    /// This in linked-list of RowData structure
    /// </summary>
    public /*internal*/ class CellData
    {
        #region public fields
        /// <summary>
        /// X position of current cell
        /// </summary>
        public int X;

        /// <summary>
        /// Coverage value
        /// </summary>
        public int Coverage;

        /// <summary>
        /// Area value
        /// </summary>
        public int Area;
        #endregion

        #region linked list field
        /// <summary>
        /// Next cell in current row
        /// </summary>
        public CellData Next = null;
        #endregion

        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="x">x position</param>
        public CellData(int x)
        {
            X = x;
        }
        /// <summary>
        /// Create cell data by using some values
        /// </summary>
        /// <param name="x">x coordinate</param>
        /// <param name="coverage">coverage value</param>
        /// <param name="area">area</param>
        public CellData(int x, int coverage, int area)
        {
            X = x;
            Coverage = coverage;
            Area = area;
        }
        #endregion
    }
}
