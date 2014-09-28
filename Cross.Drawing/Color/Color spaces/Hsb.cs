
namespace Cross.Drawing.ColorSpaces
{
    /// <summary>
    /// A single color in HSB (HSV) color space
    /// </summary>
    public class Hsb
    {

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

        private double mHue = 0;
        /// <summary>
        /// Gets/Sets Hue component.
        /// <para>Value must be in range [0, 360]</para>
        /// </summary>
        public double Hue
        {
            get { return mHue; }
            set
            {
                if (value > 360) mHue = 360;
                else if (value < 0) mHue = 0;
                else mHue = value;
            }
        }

        private double mSaturation = 1.0;
        /// <summary>
        /// Gets/Sets Saturation component
        /// <para>Value must be in range [0, 1]</para>
        /// <para>Default value is 1</para>
        /// </summary>
        public double Saturation
        {
            get { return mSaturation; }
            set
            {
                if (value > 1) mSaturation = 1.0;
                else if (value < 0) mSaturation = 0;
                else mSaturation = value;
            }
        }

        private double mBrightness = 1.0;
        /// <summary>
        /// Gets/Sets Brightness component
        /// <para>Value must be in range [0, 1]</para>
        /// <para>Default value is 1</para>
        /// </summary>
        public double Brightness
        {
            get { return mBrightness; }
            set
            {
                if (value > 1) mBrightness = 1;
                else if (value < 0) mBrightness = 0;
                else mBrightness = value;
            }
        }

        public static bool operator ==(Hsb a, Hsb b)
        {
            return (a.mHue == b.mHue && a.mSaturation == b.mSaturation && a.mBrightness == b.mBrightness && a.mAlpha == b.mAlpha);
        }

        public static bool operator !=(Hsb a, Hsb b)
        {
            return (a.mHue != b.mHue || a.mSaturation != b.mSaturation || a.mBrightness != b.mBrightness || a.mAlpha != b.mAlpha);
        }

        /// <summary>
        /// Check quality of the object against this instance
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is Hsb) return this == (Hsb)obj;
            else return false;
        }

        /// <summary>
        /// Returns the hash code for this instance
        /// </summary>
        public override int GetHashCode()
        {
            return mHue.GetHashCode() ^ mSaturation.GetHashCode() ^ mBrightness.GetHashCode() ^ mAlpha.GetHashCode();
        }

        /// <summary>
        /// Converts to display text
        /// </summary>
        public override string ToString()
        {
            return string.Format("H: {0} S: {1} B: {2} Alpha: {3}", mHue, mSaturation, mBrightness, mAlpha);
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public Hsb()
        { }

        /// <summary>
        /// Create a new instance
        /// </summary>
        public Hsb(double hue, double saturation, double brightness)
        {
            //hue
            if (hue > 360) mHue = 360;
            else if (hue < 0) mHue = 0;
            else mHue = hue;

            //saturation
            if (saturation > 1) mSaturation = 1;
            else if (saturation < 0) mSaturation = 0;
            else mSaturation = saturation;

            //brightness
            if (brightness > 1) mBrightness = 1;
            else if (brightness < 0) mBrightness = 0;
            else mBrightness = brightness;
        }
    }
}

