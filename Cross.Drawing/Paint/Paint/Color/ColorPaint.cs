


namespace Cross.Drawing
{
    /// <summary>
    /// A solid, uniform paint consisting of single color
    /// </summary>
    public class ColorPaint : Paint
    {
        /// <summary>
        /// Gets/Sets the color of this paint.
        /// <para>Default color is black</para>
        /// </summary>
        public Color Color = Colors.Black;

        #region Constructors
        /// <summary>
        /// Default constructor
        /// </summary>
        public ColorPaint()
        { }

        /// <summary>
        /// Create a new instance with the provided color
        /// </summary>
        public ColorPaint(Color color)
        {
            this.Color = color;
        }
        #endregion
    }
}
