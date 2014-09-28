
namespace Cross.Drawing.ColorSpaces
{
    /// <summary>
    /// A single color in HSL (HSI) color space
    /// <para>HSL and HSV (also called HSB) are two related representations of points in an RGB color space, which attempt to describe perceptual color relationships more accurately than RGB, while remaining computationally simple. HSL stands for hue, saturation, lightness, while HSV stands for hue, saturation, value and HSB stands for hue, saturation, brightness.</para>
    /// <para>Both HSL and HSV describe colors as points in a cylinder whose central axis ranges from black at the bottom to white at the top with neutral colors between them, where angle around the axis corresponds to “hue”, distance from the axis corresponds to “saturation”, and distance along the axis corresponds to “lightness”, “value”, or “brightness”.</para>
    /// </summary>
    public class Hsl
    {
        #region Lighten
        /// <summary>
        /// Lightens the colour by the specified amount by modifying
        /// the luminance (for example, 0.2 would lighten the colour by 20%)
        /// </summary>
        public void Lighten(double percent)
        {
            mLuminance *= (1.0f + percent);
            if (mLuminance > 1.0f)
            {
                mLuminance = 1.0f;
            }
        }
        #endregion

        #region Darken
        /// <summary>
        /// Darkens the colour by the specified amount by modifying
        /// the luminance (for example, 0.2 would darken the colour by 20%)
        /// </summary>
        public void Darken(double percent)
        {
            mLuminance *= (1.0f - percent);
        }
        #endregion

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

        #region Hue
        double mHue;
        /// <summary>
        /// Gets/Sets the hue property in range [0, 360]
        /// </summary>
        public double Hue
        {
            get { return mHue; }
            set
            {
                if (value < 0.0) mHue = 360 + value;
                else if (value > 360) mHue = 360 - value;
                else mHue = value;
            }
        }
        #endregion

        #region Saturation
        double mSaturation = 1f;
        /// <summary>
        /// Gets/Sets the saturation property in range [0, 1]
        /// </summary>
        public double Saturation
        {
            get { return mSaturation; }
            set
            {
                mSaturation = value;
                mSaturation = mSaturation > 1 ? 1 : mSaturation < 0 ? 0 : mSaturation;
            }
        }
        #endregion

        #region Luminance
        double mLuminance = 0.5;
        /// <summary>
        /// Gets/Sets the luminance property in range [0, 1]
        /// </summary>
        public double Luminance
        {
            get { return mLuminance; }
            set
            {
                mLuminance = value;
                mLuminance = mLuminance > 1 ? 1 : mLuminance < 0 ? 0 : mLuminance;
            }
        }
        #endregion

        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor
        /// </summary>
        public Hsl()
        {
            mSaturation = 1f;
            mLuminance = 0.5f;
        }

        /// <summary>
        /// Normal constructor
        /// </summary>
        public Hsl(double hue, double saturation, double luminance)
        {
            mHue = hue;
            mSaturation = saturation;
            mLuminance = luminance;
        }
        #endregion

        #region HSL to Argb
        /// <summary>
        /// Converts from an HSL color to Argb (system) color
        /// </summary>
        public static Color ToColor(Hsl value)
        {
            byte red;
            byte green;
            byte blue;
            double Saturation = value.Saturation;
            double Luminance = value.Luminance;
            double Hue = value.Hue;

            if (Saturation == 0.0)
            {
                red = (byte)(Luminance * 255.0F);
                green = red;
                blue = red;
            }
            else
            {
                double rm1;
                double rm2;

                if (Luminance <= 0.5f)
                {
                    rm2 = Luminance + Luminance * Saturation;
                }
                else
                {
                    rm2 = Luminance + Saturation - Luminance * Saturation;
                }
                rm1 = 2.0f * value.Luminance - rm2;
                red = ToRGB1(rm1, rm2, Hue + 120.0f);
                green = ToRGB1(rm1, rm2, Hue);
                blue = ToRGB1(rm1, rm2, Hue - 120.0f);
            }

            return new Color(value.Alpha, red, green, blue);
        }

        static byte ToRGB1(double rm1, double rm2, double rh)
        {
            if (rh > 360.0f)
            {
                rh -= 360.0f;
            }
            else if (rh < 0.0f)
            {
                rh += 360.0f;
            }

            if (rh < 60.0f)
            {
                rm1 = rm1 + (rm2 - rm1) * rh / 60.0f;
            }
            else if (rh < 180.0f)
            {
                rm1 = rm2;
            }
            else if (rh < 240.0f)
            {
                rm1 = rm1 + (rm2 - rm1) * (240.0f - rh) / 60.0f;
            }

            return (byte)(rm1 * 255);
        }
        #endregion

        #region Argb to HSL
        /// <summary>
        /// Converts from an Argb (system) color to HSL
        /// </summary>
        public static Hsl ToHsl(Color value)
        {
            double luminance = 0f;
            double saturation = 0f;
            double hue = 0f;
            byte red = value.Red;
            byte green = value.Green;
            byte blue = value.Blue;

            byte minval = System.Math.Min(red, System.Math.Min(green, blue));
            byte maxval = System.Math.Max(red, System.Math.Max(green, blue));

            double mdiff = (double)(maxval - minval);
            double msum = (double)(maxval + minval);

            luminance = msum / 510.0f;

            if (maxval == minval)
            {
                saturation = 0.0f;
                hue = 0.0f;
            }
            else
            {
                double rnorm = (maxval - red) / mdiff;
                double gnorm = (maxval - green) / mdiff;
                double bnorm = (maxval - blue) / mdiff;

                saturation = (luminance <= 0.5f) ? (mdiff / msum) : (mdiff / (510.0f - msum));

                if (red == maxval)
                {
                    hue = 60.0f * (6.0f + bnorm - gnorm);
                }
                if (green == maxval)
                {
                    hue = 60.0f * (2.0f + rnorm - bnorm);
                }
                if (blue == maxval)
                {
                    hue = 60.0f * (4.0f + gnorm - rnorm);
                }
                if (hue > 360.0f)
                {
                    hue = hue - 360.0f;
                }
            }

            Hsl result = new Hsl(hue, saturation, luminance);
            result.Alpha = value.Alpha;

            return result;
        }
        #endregion

        #region Casting
        #region HSL <-> Argb
        /// <summary>
        /// Implicit casting from HSL to System.Drawing.Color
        /// </summary>
        public static implicit operator Color(Hsl value)
        {
            return ToColor(value);
        }

        /// <summary>
        /// Implicit casting from System.Drawing.Color to HSL
        /// </summary>
        public static implicit operator Hsl(Color value)
        {
            return ToHsl(value);
        }
        #endregion

        #region HSL -> CYMK
        /// <summary>
        /// Implicit casting from HSL to CYMK
        /// </summary>
        public static implicit operator Cymk(Hsl value)
        {
            Color tmp = ToColor(value);
            return Cymk.ToCymk(tmp);
        }
        #endregion
        #endregion

        #region To String
        /// <summary>
        /// Converts to display text
        /// </summary>
        public override string ToString()
        {
            return string.Format("H: {0} S: {1} L: {2} Alpha: {3}", mHue, mSaturation, mLuminance, mAlpha);
        }
        #endregion
    }
}

