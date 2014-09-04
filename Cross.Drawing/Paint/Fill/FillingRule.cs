#region Using directives

#endregion

namespace Cross.Drawing
{
    /// <summary>
    /// Filling rule for filling polygon
    /// </summary>
    public enum FillingRule
    {
        /// <summary>
        /// Unspecified, use default settings
        /// </summary>
        Default,
        /// <summary>
        /// non zero
        /// </summary>
        NonZero,
        /// <summary>
        ///even odd
        /// </summary>
        EvenOdd
    }
}
