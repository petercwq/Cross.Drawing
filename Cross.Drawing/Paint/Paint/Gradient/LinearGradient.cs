#region Using directives

#endregion

namespace Cross.Drawing
{
    /// <summary>
    /// Basic gradient, color in color ramp will fill from start to end point, and base on linear mode
    /// </summary>
    public class LinearGradient : GradientPaint
    {
        /// <summary>
        /// X-axis coordinate of starting point.
        /// <para>If value is [0..1], this value is interpreted as percentage</para>
        /// </summary>
        public double StartX;
        /// <summary>
        /// Y-axis coordinate of starting point.
        /// <para>If value is [0..1], this value is interpreted as percentage</para>
        /// </summary>
        public double StartY;
        /// <summary>
        /// X-axis coordinate of ending point.
        /// <para>If value is [0..1], this value is interpreted as percentage</para>
        /// </summary>
        public double EndX;
        /// <summary>
        /// Y-axis coordinate of ending point.
        /// <para>If value is [0..1], this value is interpreted as percentage</para>
        /// </summary>
        public double EndY;

        /// <summary>
        /// Linear mode
        /// </summary>
        public LinearGradientMode Mode = LinearGradientMode.ForwardDiagonal;

        #region Constructors
        /// <summary>
        /// Default constructor
        /// </summary>
        public LinearGradient()
        { }
        #endregion

        #region Horizontal Constructor
        ///// <summary>
        ///// Construct vertical linear gradient from start Y to end Y
        ///// </summary>
        ///// <param name="startY">start y</param>
        ///// <param name="endY">end y</param>
        ///// <param name="startColor">start color</param>
        ///// <param name="endColor">end color</param>
        //public LinearGradient(double startY, double endY, Color startColor, Color endColor)
        //{
        //    Mode = LinearGradientMode.Vertical;
        //    StartY = startY;
        //    EndY = endY;
        //    this.Ramp = new ColorRamp();
        //    this.Ramp.Add(startColor, 0.0);
        //    this.Ramp.Add(endColor, 0.0);
        //}

        ///// <summary>
        ///// Construct vertical linear gradient from start Y to end Y
        ///// </summary>
        ///// <param name="startY">start y</param>
        ///// <param name="endY">end y</param>
        ///// <param name="ramp">ramp</param>
        //public LinearGradient(double startY, double endY, ColorRamp ramp)
        //{
        //    Mode = LinearGradientMode.Vertical;
        //    Start = new Point(0, startY);
        //    End = new Point(0, endY);
        //    this.Ramp = ramp;
        //}
        #endregion
    }


}
