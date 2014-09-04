namespace Cross.Drawing
{
    /// <summary>
    /// Radial gradient paint
    /// </summary>
    public class RadialGradient : GradientPaint
    {
        #region RadiusX
        /// <summary>
        /// Gets/Sets radius for horizontal
        /// </summary>
        public double RadiusX;
        #endregion

        #region RadiusY
        /// <summary>
        /// Gets/Sets radius for vertical
        /// </summary>
        public double RadiusY;
        #endregion

        #region Radius
        /// <summary>
        /// Sets both RadiusX and RadiusY to the same value
        /// </summary>
        public double Radius
        {
            set
            {
                RadiusX = value;
                RadiusY = value;
            }
        }
        #endregion

        #region CenterX
        /// <summary>
        /// Gets/Sets center point x
        /// </summary>
        public double CenterX;
        #endregion

        #region CenterY
        /// <summary>
        /// Gets/Sets center point y value
        /// </summary>
        public double CenterY;
        #endregion

        #region FocusX
        /// <summary>
        /// Gets/Sets x coordinate that the gradient focus to
        /// </summary>
        public double FocusX;
        #endregion

        #region FocusY
        /// <summary>
        /// Gets/Sets y coordinate that the gradient focus to
        /// </summary>
        public double FocusY;
        #endregion
    }
}
