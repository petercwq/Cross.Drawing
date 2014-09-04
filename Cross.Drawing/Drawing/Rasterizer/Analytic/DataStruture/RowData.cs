#region Using directives

#endregion

namespace Cross.Drawing.Rasterizers.Analytical
{
    /// <summary>
    /// Linked list of cell information in one row.
    /// <para>Method GoToCell to go to a cell by x position,and set CurrentCell Field to that cell</para>
    /// <para></para>
    /// </summary>
    public /*internal*/ class RowData
    {
        #region Fields
        /// <summary>
        /// Saving current cell
        /// </summary>
        public CellData CurrentCell;

        /// <summary>
        /// First cell in current row, this should get only.
        /// but for better performance this will be field
        /// </summary>
        public CellData First;

        /// <summary>
        /// temp cell using in method Goto cell
        /// </summary>
        CellData TempCell;
        #endregion

        // METHOD WHILE BUILDING
        #region Go to Cell
        /// <summary>
        /// Go to x position, so that it will reserve cell for further using
        /// </summary>
        /// <param name="x">x position that move current cell to</param>
        /// <returns>
        /// Current Row
        /// </returns>
        public CellData GoToCell(int x)
        {
            if (CurrentCell == null)
            {
                CurrentCell = new CellData(x);
                First = CurrentCell;
            }
            else if (CurrentCell.X < x)
            {
                #region When looking forward

                if (CurrentCell.Next == null)
                {
                    // when current cell is last
                    CurrentCell.Next = new CellData(x);
                    CurrentCell = CurrentCell.Next;
                }
                else
                {
                    #region current cell is not last, seek next
                    // sure that all cell have Next already (except the last)
                    do
                    {
                        // move next
                        TempCell = CurrentCell;
                        CurrentCell = CurrentCell.Next;
                        if (CurrentCell.X == x)
                        {
                            // if this exactly the cell that we finding
                            // exit the loop
                            break;
                        }
                        else if (CurrentCell.X > x)
                        {
                            // if this cell is not the last
                            // but next cell is greater than input value
                            // insert a new cell between
                            TempCell.Next = new CellData(x);
                            TempCell.Next.Next = CurrentCell;
                            CurrentCell = TempCell.Next;
                            break;
                        }
                        else if (CurrentCell.Next == null)
                        {
                            // if this cell is the last
                            // construct and return
                            CurrentCell.Next = new CellData(x);
                            CurrentCell = CurrentCell.Next;
                            break;
                        }
                    } while (true);
                    #endregion
                }
                #endregion
            }
            else if (CurrentCell.X > x)
            {
                #region search from begining
                if (First.X > x)
                {
                    // when this value is less than first cell
                    // construct one
                    CurrentCell = new CellData(x);
                    CurrentCell.Next = First;
                    First = CurrentCell;
                }
                else if (First.X == x)
                {
                    // else, it exactly the first
                    // keep it
                    CurrentCell = First;
                }
                else
                {
                    // finding from begining, except first one
                    CurrentCell = First;
                    #region OLD CODE
                    //                    do
                    //                    {
                    //                        TempCell = CurrentCell;
                    //                        CurrentCell = CurrentCell.Next;
                    //#if DEBUG
                    //                        #region Defensive
                    //                        if (CurrentCell == null)
                    //                        {
                    //                            Log.Warning(string.Format("Check 3: Current cell is null. Previous X: {0}, while x is {1}",TempCell.X,x));
                    //                        }
                    //                        #endregion
                    //#endif
                    //                        if (CurrentCell.X > x)
                    //                        {
                    //                            TempCell.Next = new CellData(x);
                    //                            TempCell.Next.Next = CurrentCell;
                    //                            CurrentCell = TempCell.Next;
                    //                            break;
                    //                        }
                    //                    } while (CurrentCell.X < x);
                    #endregion

                    #region Seek
                    // sure that all cell have Next already (except the last)
                    do
                    {
                        // move next
                        TempCell = CurrentCell;
                        CurrentCell = CurrentCell.Next;
#if DEBUG
                        #region Defensive
                        if (CurrentCell == null)
                        {
                            Log.Warning("Check 1: Current cell is null");
                        }
                        #endregion
#endif

                        if (CurrentCell.X == x)
                        {
                            // if this exactly the cell that we finding
                            // exit the loop
                            break;
                        }
                        else if (CurrentCell.X > x)
                        {
                            // if this cell is not the last
                            // but next cell is greater than input value
                            // insert a new cell between
                            TempCell.Next = new CellData(x);
                            TempCell.Next.Next = CurrentCell;
                            CurrentCell = TempCell.Next;
                            break;
                        }
                        else if (CurrentCell.Next == null)
                        {
                            // if this cell is the last
                            // construct and return
                            CurrentCell.Next = new CellData(x);
                            CurrentCell = CurrentCell.Next;
                            break;
                        }
                    } while (true);
                    #endregion
                }
                #endregion
            }

            #region Defensive
#if DEBUG
            if (CurrentCell == null)
            {
                Log.Debug("Unepected result.Can not found correct cell for X ({0}).", x);
                #region Log other cell in current row
                Log.Debug("Test");
                object scope = Log.Start("Datas in row:");
                CellData cell = First;
                while (cell != null)
                {
                    Log.Debug("Cell:X {0}", cell.X);
                    cell = cell.Next;
                }
                Log.End(scope);
                #endregion
            }
#endif
            #endregion
            return CurrentCell;
        }
        #endregion

        #region Go to Cell
        /// <summary>
        /// Go to x position, so that it will reserve cell for further using
        /// </summary>
        /// <param name="x">x position that move current cell to</param>
        /// <returns>
        /// Current Row
        /// </returns>
        public void SetCell(int x, int coverage, int area)
        {
            if (CurrentCell == null)
            {
                CurrentCell = new CellData(x);
                First = CurrentCell;
            }
            else if (CurrentCell.X < x)
            {
                #region When looking forward

                if (CurrentCell.Next == null)
                {
                    // when current cell is last
                    CurrentCell.Next = new CellData(x);
                    CurrentCell = CurrentCell.Next;
                }
                else
                {
                    #region current cell is not last, seek next
                    // sure that all cell have Next already (except the last)
                    do
                    {
                        // move next
                        TempCell = CurrentCell;
                        CurrentCell = CurrentCell.Next;
#if DEBUG
                        #region Defensive
                        if (CurrentCell == null)
                        {
                            Log.Warning("Check 1: Current cell is null");
                        }
                        #endregion
#endif

                        if (CurrentCell.X == x)
                        {
                            // if this exactly the cell that we finding
                            // exit the loop
                            break;
                        }
                        else if (CurrentCell.X > x)
                        {
                            // if this cell is not the last
                            // but next cell is greater than input value
                            // insert a new cell between
                            TempCell.Next = new CellData(x);
                            TempCell.Next.Next = CurrentCell;
                            CurrentCell = TempCell.Next;
                            break;
                        }
                        else if (CurrentCell.Next == null)
                        {
                            // if this cell is the last
                            // construct and return
                            CurrentCell.Next = new CellData(x);
                            CurrentCell = CurrentCell.Next;
                            break;
                        }
                    } while (true);
                    #endregion
                }
                #endregion
            }
            else if (CurrentCell.X > x)
            {
#if DEBUG
                #region Defensive
                if (First == null)
                {
                    Log.Warning("Check 2: First cell is null");
                }
                #endregion
#endif
                #region search from begining
                if (First.X > x)
                {
                    // when this value is less than first cell
                    // construct one
                    CurrentCell = new CellData(x);
                    CurrentCell.Next = First;
                    First = CurrentCell;
                }
                else if (First.X == x)
                {
                    // else, it exactly the first
                    // keep it
                    CurrentCell = First;
                }
                else
                {
                    // finding from begining, except first one
                    CurrentCell = First;
                    #region OLD CODE
                    //                    do
                    //                    {
                    //                        TempCell = CurrentCell;
                    //                        CurrentCell = CurrentCell.Next;
                    //#if DEBUG
                    //                        #region Defensive
                    //                        if (CurrentCell == null)
                    //                        {
                    //                            Log.Warning(string.Format("Check 3: Current cell is null. Previous X: {0}, while x is {1}",TempCell.X,x));
                    //                        }
                    //                        #endregion
                    //#endif
                    //                        if (CurrentCell.X > x)
                    //                        {
                    //                            TempCell.Next = new CellData(x);
                    //                            TempCell.Next.Next = CurrentCell;
                    //                            CurrentCell = TempCell.Next;
                    //                            break;
                    //                        }
                    //                    } while (CurrentCell.X < x);
                    #endregion

                    #region Seek
                    // sure that all cell have Next already (except the last)
                    do
                    {
                        // move next
                        TempCell = CurrentCell;
                        CurrentCell = CurrentCell.Next;
#if DEBUG
                        #region Defensive
                        if (CurrentCell == null)
                        {
                            Log.Warning("Check 1: Current cell is null");
                        }
                        #endregion
#endif

                        if (CurrentCell.X == x)
                        {
                            // if this exactly the cell that we finding
                            // exit the loop
                            break;
                        }
                        else if (CurrentCell.X > x)
                        {
                            // if this cell is not the last
                            // but next cell is greater than input value
                            // insert a new cell between
                            TempCell.Next = new CellData(x);
                            TempCell.Next.Next = CurrentCell;
                            CurrentCell = TempCell.Next;
                            break;
                        }
                        else if (CurrentCell.Next == null)
                        {
                            // if this cell is the last
                            // construct and return
                            CurrentCell.Next = new CellData(x);
                            CurrentCell = CurrentCell.Next;
                            break;
                        }
                    } while (true);
                    #endregion
                }
                #endregion
            }

            // Change from method GoToCell
            CurrentCell.Area += area;
            CurrentCell.Coverage += coverage;
        }
        #endregion
    }
}
