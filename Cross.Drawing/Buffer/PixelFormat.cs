


namespace Cross.Drawing
{
    /// <summary>
    /// Pixel order information class
    /// </summary>
    public class PixelFormats
    {
        /// <summary>
        /// Red component offset
        /// </summary>
        public int RedOffset = 0;

        /// <summary>
        /// Blue component offset
        /// </summary>
        public int BlueOffset = -1;

        /// <summary>
        /// Green component offset
        /// </summary>
        public int GreenOffset = -1;

        /// <summary>
        /// Alpha component offset
        /// </summary>
        public int AlphaOffset = -1;

        /// <summary>
        /// The number of bytes each pixel occupies
        /// </summary>
        public int BytesPerPixel;

        #region Supports Alpha
        /// <summary>
        /// Checks whether this format support alpha channel
        /// </summary>
        public bool SupportsAlpha
        { get { return AlphaOffset >= 0; } }
        #endregion

        #region Rgb
        static PixelFormats mRgb = new PixelFormats(0, 1, 1, -1, 3);
        /// <summary>
        /// Gets 24-bit RGB format
        /// </summary>
        public static PixelFormats Rgb
        { get { return mRgb; } }
        #endregion

        #region Bgr
        static PixelFormats mBgr = new PixelFormats(2, 1, 0, -1, 3);
        /// <summary>
        /// Gets 24-bit BGR format
        /// </summary>
        public static PixelFormats Bgr
        { get { return mBgr; } }
        #endregion

        #region Rgba
        static PixelFormats mRgba = new PixelFormats(0, 1, 2, 3, 4);
        /// <summary>
        /// Gets 32-bit RGBA format
        /// </summary>
        public static PixelFormats Rgba
        { get { return mRgba; } }
        #endregion

        #region Bgra
        static PixelFormats mBgra = new PixelFormats(2, 1, 0, 3, 4);
        /// <summary>
        /// Gets 32-bit BGRA format
        /// </summary>
        public static PixelFormats Bgra
        { get { return mBgra; } }
        #endregion

        #region Argb
        static PixelFormats mArgb = new PixelFormats(1, 2, 3, 0, 4);
        /// <summary>
        /// Gets 32-bit ARGB format
        /// </summary>
        public static PixelFormats Argb
        { get { return mArgb; } }
        #endregion

        #region Abgr
        static PixelFormats mAbgr = new PixelFormats(3, 2, 1, 0, 4);
        /// <summary>
        /// Gets 32-bit ABGR format
        /// </summary>
        public static PixelFormats Abgr
        { get { return mAbgr; } }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new instance
        /// </summary>
        public PixelFormats() { }

        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="red">red component offset</param>
        /// <param name="green">green component offset</param>
        /// <param name="blue">blue component offset</param>
        public PixelFormats(int red, int green, int blue)
        {
            RedOffset = red;
            GreenOffset = green;
            BlueOffset = blue;
        }

        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="red">red component offset</param>
        /// <param name="green">green component offset</param>
        /// <param name="blue">blue component offset</param>
        /// <param name="alpha">alpha component offset</param>
        public PixelFormats(int red, int green, int blue, int alpha)
        {
            RedOffset = red;
            GreenOffset = green;
            BlueOffset = blue;
            AlphaOffset = alpha;
        }

        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="red">red component offset</param>
        /// <param name="green">green component offset</param>
        /// <param name="blue">blue component offset</param>
        /// <param name="alpha">alpha component offset</param>
        /// <param name="bytes">number of bytes per pixel</param>
        public PixelFormats(int red, int green, int blue, int alpha, int bytes)
        {
            RedOffset = red;
            GreenOffset = green;
            BlueOffset = blue;
            AlphaOffset = alpha;
            BytesPerPixel = bytes;
        }
        #endregion

        #region Equals
        /// <summary>
        /// Equality comparision
        /// </summary>
        public override bool Equals(object obj)
        {
            bool result = false;

            if (obj is PixelFormats)
            {
                PixelFormats dst = (PixelFormats)obj;
                result = (dst.AlphaOffset == AlphaOffset) && (dst.BlueOffset == BlueOffset) && (dst.GreenOffset == GreenOffset) && (dst.RedOffset == RedOffset);
            }

            return result;
        }
        #endregion

        #region Get Hash Code
        /// <summary>
        /// Calculate the hashcode of this instance
        /// </summary>
        public override int GetHashCode()
        {
            return AlphaOffset ^ RedOffset ^ GreenOffset ^ BlueOffset;
        }
        #endregion
    }
}
