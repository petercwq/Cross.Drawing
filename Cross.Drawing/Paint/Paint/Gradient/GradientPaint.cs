#region Using directives

#endregion

namespace Cross.Drawing
{
    /// <summary>
    /// Gradient paint supports gradient colors for fill and stroke
    /// </summary>
    public abstract class GradientPaint : Paint
    {
        /// <summary>
        /// Gets/Sets the gradient's repeat mode
        /// </summary>
        public GradientStyle Style = GradientStyle.Reflect;

        /// <summary>
        /// Gets/Sets the color ramp used for gradient interpolation
        /// </summary>
        public ColorRamp Ramp = null;

        //#region Ramp
        //ColorRamp mRamp;
        ///// <summary>
        ///// Gets/Sets Ramp
        ///// </summary>
        //public ColorRamp Ramp
        //{
        //    get { return mRamp; }
        //    set { mRamp = value; }
        //}
        //#endregion

        #region GetLinearColors
        /// <summary>
        /// Get linear colors
        /// </summary>
        /// <param name="opacity">apacity of paint (value in range 0-256)</param>
        /// <returns>512 element array of colors. When ramp is not set, array include 512 zero element</returns>
        internal uint[] GetLinearColors(uint opacity)
        {
            if (this.Ramp != null)
            {
                return this.Ramp.Build(Style, opacity);
            }
            return ColorRamp.EmptyColors;
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Default constructor
        /// </summary>
        public GradientPaint()
        {
            this.Ramp = new ColorRamp();
        }
        #endregion
    }
}
