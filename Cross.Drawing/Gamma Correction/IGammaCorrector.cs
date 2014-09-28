
namespace Cross.Drawing
{
    /// <summary>
    ///
    /// </summary>
    public interface IGammaCorrector
    {
        /// <summary>
        /// Prepare a lookup table for red
        /// </summary>
        /// <returns>Result must be a uint[256] array</returns>
        byte[] GetLookupTableRed();

        /// <summary>
        /// Prepare a lookup table for green
        /// </summary>
        /// <returns>result must be a uint[256] array</returns>
        byte[] GetLookupTableGreen();

        /// <summary>
        /// Prepare a lookup table for blue
        /// </summary>
        /// <returns>result must be a uint[256] array</returns>
        byte[] GetLookupTableBlue();
    }
}

