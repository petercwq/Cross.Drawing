#region Using directives

#endregion

namespace Cross.Drawing.ColorSpaces
{
    /// <summary>
    /// A single color in CYMK color space.
    /// <para>CMYK (short for cyan, magenta, yellow, and key (black), and often referred to as process color or four color) is a subtractive color model, used in color printing, also used to describe the printing process itself.</para>
    /// <para>The CMYK model works by partially or entirely masking certain colors on the typically white background (that is, absorbing particular wavelengths of light). Such a model is called subtractive because inks “subtract” brightness from white.</para>
    /// </summary>
    public class Cymk
    {
        #region Properties

        #region Alpha
        private byte mAlpha = 255;
        /// <summary>
        /// Gets/Sets the alpha property
        /// <para>Value must be in range [0, 255]</para>
        /// <para>Notes: this colorspace actually does not have Alpha component. The component is kept for conversion between ARGB and this colorspace</para>
        /// </summary>
        public byte Alpha
        {
            get { return mAlpha; }
            set { mAlpha = value; }
        }
        #endregion

        #region Cyan
        double mCyan;
        /// <summary>
        /// Gets/Sets the Cyan component. Range: [0, 1]
        /// </summary>
        public double Cyan
        {
            get { return mCyan; }
            set
            {
                mCyan = value;
                mCyan = mCyan > 1 ? 1 : mCyan < 0 ? 0 : mCyan;
            }
        }
        #endregion

        #region Magenta
        double mMagenta;
        /// <summary>
        /// Gets/Sets the Magenta component. Range: [0, 1]
        /// </summary>
        public double Magenta
        {
            get { return mMagenta; }
            set
            {
                mMagenta = value;
                mMagenta = mMagenta > 1 ? 1 : mMagenta < 0 ? 0 : mMagenta;
            }
        }
        #endregion

        #region Yellow
        double mYellow;
        /// <summary>
        /// Gets/Sets the yellow component. Range: [0, 1]
        /// </summary>
        public double Yellow
        {
            get { return mYellow; }
            set
            {
                mYellow = value;
                mYellow = mYellow > 1 ? 1 : mYellow < 0 ? 0 : mYellow;
            }
        }
        #endregion

        #region Black
        double mBlack;
        /// <summary>
        /// Gets/Sets the black component. Range: [0, 1]
        /// </summary>
        public double Black
        {
            get { return mBlack; }
            set
            {
                mBlack = value;
                mBlack = mBlack > 1 ? 1 : mBlack < 0 ? 0 : mBlack;
            }
        }
        #endregion

        #endregion

        #region To String
        /// <summary>
        /// Converts to display text
        /// </summary>
        public override string ToString()
        {
            return string.Format("C: {0} Y: {1} M: {2} K: {3} Alpha: {4}", mCyan, mYellow, mMagenta, mBlack, mAlpha);
        }
        #endregion

        #region Constructor
        /// <summary>
        /// Default constructor
        /// </summary>
        public Cymk()
        {
        }

        /// <summary>
        /// Normal constructor
        /// </summary>
        public Cymk(double cyan, double magenta, double yellow, double black)
        {
            mCyan = cyan;
            mMagenta = magenta;
            mYellow = yellow;
            mBlack = black;
        }
        #endregion

        #region Casting
        #region CYMK <-> Argb
        /// <summary>
        /// Implicit casting from CYMK to System.Drawing.Color
        /// </summary>
        public static implicit operator Color(Cymk value)
        {
            return ToColor(value);
        }

        /// <summary>
        /// Implicit casting from System.Drawing.Color to CYMK
        /// </summary>
        public static implicit operator Cymk(Color value)
        {
            return ToCymk(value);
        }
        #endregion

        #region CYMK -> HSL
        /// <summary>
        /// Implicit casting from CYMK to HSL
        /// </summary>
        public static implicit operator Hsl(Cymk value)
        {
            Color c = ToColor(value);
            return Hsl.ToHsl(c);
        }
        #endregion
        #endregion

        #region CYMK to Argb
        /// <summary>
        /// Converts from a CYMK color to Argb (system) color
        /// </summary>
        public static Color ToColor(Cymk value)
        {
            byte red, green, blue;

            red = Round(255 - (255 * value.Cyan));
            green = Round(255 - (255 * value.Magenta));
            blue = Round(255 - (255 * value.Yellow));

            return new Color(value.Alpha, red, green, blue);
        }

        /// <summary>
        /// Custom rounding function.
        /// </summary>
        /// <param name="value">Value to round</param>
        /// <returns>Rounded value</returns>
        private static byte Round(double value)
        {
            byte result = (byte)value;

            byte temp = (byte)(value * 100);

            if ((temp % 100) >= 50)
                result += 1;

            return result;
        }
        #endregion

        #region Argb to CYMK
        /// <summary>
        /// Converts from a Argb(system) color to CYMK
        /// </summary>
        public static Cymk ToCymk(Color value)
        {
            Cymk cmyk = new Cymk();
            cmyk.Alpha = value.Alpha;
            double low = 1f;

            cmyk.Cyan = (double)(255 - value.Red) / 255;
            if (low > cmyk.Cyan)
                low = cmyk.Cyan;

            cmyk.Magenta = (double)(255 - value.Green) / 255;
            if (low > cmyk.Magenta)
                low = cmyk.Magenta;

            cmyk.Yellow = (double)(255 - value.Blue) / 255;
            if (low > cmyk.Yellow)
                low = cmyk.Yellow;

            if (low > 0.0)
            {
                cmyk.Black = low;
            }

            return cmyk;
        }
        #endregion
    }
}
