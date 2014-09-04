#region Using directives

#endregion

namespace Cross.Drawing
{
    /// <summary>
    /// Repeat color mode while filling gradient that large than range of gradient
    /// </summary>
    public enum GradientStyle
    {
        /// <summary>
        /// Use start and end color for out-of-range gradient
        /// </summary>
        Pad,
        /// <summary>
        /// Repeat gradient range
        /// </summary>
        Repeat,
        /// <summary>
        /// Repeat gradient range inversely
        /// </summary>
        Reflect
    }
}
