#region Using directives

#endregion

namespace Cross.Drawing
{
    /// <summary>
    /// Modes supported for linear gradient
    /// </summary>
    public enum LinearGradientMode
    {
        /// <summary>
        /// Blend color from top to bottom
        /// </summary>
        Horizontal,
        /// <summary>
        /// Blend color from left to right
        /// </summary>
        Vertical,
        /// <summary>
        /// Blend color from top-left to bottom-right
        /// </summary>
        BackwardDiagonal,
        /// <summary>
        /// Blend color from bottom-left to top-right
        /// </summary>
        ForwardDiagonal
    }
}
