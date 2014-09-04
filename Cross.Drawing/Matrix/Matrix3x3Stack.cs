#region Using directives
using System.Collections;
#endregion

namespace Cross.Drawing
{
    /// <summary>
    /// A stack collection for <see cref="Matrix3x3"/>
    /// </summary>
    public class Matrix3x3Stack
    {
        #region Fields
        Stack mStack = null;
        #endregion

        #region Count
        /// <summary>
        /// Gets the number of elements contained in this stack
        /// </summary>
        public int Count
        {
            get { return mStack.Count; }
        }
        #endregion

        #region Push
        /// <summary>
        /// Insert an item at the top of stack
        /// </summary>
        public void Push(Matrix3x3 item)
        {
            mStack.Push(item);
        }
        #endregion

        #region Pop
        /// <summary>
        /// Remove and return the item that the top of stack
        /// </summary>
        public Matrix3x3 Pop()
        {
            return (Matrix3x3)mStack.Pop();
        }
        #endregion

        #region Clone
        /// <summary>
        /// Create an exact duplicate of this stack
        /// </summary>
        public Matrix3x3Stack Clone()
        {
            return new Matrix3x3Stack(this);
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor
        /// </summary>
        public Matrix3x3Stack()
        {
            mStack = new Stack();
        }

        private Matrix3x3Stack(Matrix3x3Stack source)
        {
            mStack = new Stack(source.mStack);
        }
        #endregion
    }
}
