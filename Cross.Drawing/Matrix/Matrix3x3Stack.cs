using System.Collections.Generic;

namespace Cross.Drawing
{
    /// <summary>
    /// A stack collection for <see cref="Matrix3x3"/>
    /// </summary>
    public class Matrix3x3Stack : Stack<Matrix3x3>
    {
        /// <summary>
        /// Create an exact duplicate of this stack
        /// </summary>
        public Matrix3x3Stack Clone()
        {
            return new Matrix3x3Stack(this);
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public Matrix3x3Stack()
            : base()
        {
        }

        private Matrix3x3Stack(Matrix3x3Stack source)
            : base(source)
        {
        }
    }
}

