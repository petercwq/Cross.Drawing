#region Using directives
using Cross.Drawing.ColorSpaces;
#endregion

namespace Cross.Drawing
{
    /// <summary>
    /// A single color in 32-bit ARGB format
    /// </summary>
    public class Color
    {
        /// <summary>
        /// The encoded data of this color (A, R, G, B)
        /// </summary>
        /// <remarks>
        /// </remarks>
        public uint Data;
        /// <summary>
        /// A component
        /// </summary>
        internal uint A;
        //public uint A;
        /// <summary>
        /// R and B component
        /// </summary>
        internal uint RB;
        //public uint RB;
        /// <summary>
        /// A and G component
        /// </summary>
        internal uint AG;
        //public uint AG;

        #region Alpha
        /// <summary>
        /// Gets/Sets Alpha component.
        /// <para>Must be in range [0..255]</para>
        /// </summary>
        public byte Alpha
        {
            get
            {
                //decode data
                return (byte)((Data >> 24) & 0xFF);
            }
            set
            {
                //decode, then re-encode data
                Data = (Data & 0x00FFFFFF) | (uint)(value << 24);
                Precalculate();
            }
        }
        #endregion

        #region Red
        /// <summary>
        /// Gets/Sets Red component.
        /// <para>Must be in range [0..255]</para>
        /// </summary>
        public byte Red
        {
            get
            {
                //decode data
                return (byte)((Data >> 16) & 0xFF);
            }
            set
            {
                //decode, then re-encode data
                Data = ((Data & 0xFF00FFFF) | (uint)(value << 16));
                Precalculate();
            }
        }
        #endregion

        #region Green
        /// <summary>
        /// Gets/Sets Green component.
        /// <para>Must be in range [0..255]</para>
        /// </summary>
        public byte Green
        {
            get
            {
                //decode data
                return (byte)((Data >> 8) & 0xFF);
            }
            set
            {
                //decode, then re-encode data
                Data = ((Data & 0xFFFF00FF) | (uint)(value << 8));
                Precalculate();
            }
        }
        #endregion

        #region Blue
        /// <summary>
        /// Gets/Sets Blue component.
        /// <para>Must be in range [0..255]</para>
        /// </summary>
        public byte Blue
        {
            get
            {
                //decode data
                return (byte)(Data & 0xFF);
            }
            set
            {
                //decode, then re-encode data
                Data = ((Data & 0xFFFFFF00) | value);
                Precalculate();
            }
        }
        #endregion

        #region Precalculate Components
        /// <summary>
        /// Precalculate internal components acccording to data for faster retrieval by pixel renderers
        /// </summary>
        protected void Precalculate()
        {
            A = (Data >> 24) & 0xFF;
            RB = Data & 0x00FF00FF;
            AG = (Data >> 8) & 0x00FF00FF;
        }
        #endregion

        #region Creation methods

        #region Create from Color
        /// <summary>
        /// Create a new color from the original color but with different alpha
        /// </summary>
        /// <param name="orginal">The color to copy R, G, B component from</param>
        /// <param name="alpha">The new alpha component. Range: [0, 2555]</param>
        public static Color Create(Color orginal, byte alpha)
        {
            return new Color(orginal, alpha);
        }
        #endregion

        #region Create Rgb

        /// <summary>
        /// Create new color instance
        /// </summary>
        /// <param name="red">Red component. Range: [0, 255]</param>
        /// <param name="blue">Green component. Range: [0, 255]</param>
        /// <param name="green">Blue component. Range: [0, 255]</param>
        public static Color Create(byte red, byte green, byte blue)
        {
            return new Color(red, green, blue);
        }

        /// <summary>
        /// Create new color instance
        /// </summary>
        /// <param name="red">Red component. Range: [0, 255]</param>
        /// <param name="blue">Green component. Range: [0, 255]</param>
        /// <param name="green">Blue component. Range: [0, 255]</param>
        /// <param name="alpha">Alpha component. Range: [0, 255]</param>
        public static Color Create(byte red, byte green, byte blue, byte alpha)
        {
            return new Color(red, green, blue, alpha);
        }

        /// <summary>
        /// Create new color instance
        /// </summary>
        /// <param name="red">Red component. Range: [0, 255]</param>
        /// <param name="blue">Green component. Range: [0, 255]</param>
        /// <param name="green">Blue component. Range: [0, 255]</param>
        public static Color Create(int red, int green, int blue)
        {
            //range checking
            //red
            if (red > 255) red = 255;
            else if (red < 0) red = 0;
            //green
            if (green > 255) green = 255;
            else if (green < 0) green = 0;
            //blue
            if (blue > 255) blue = 255;
            else if (blue < 0) blue = 0;

            return new Color(red, green, blue);
        }

        /// <summary>
        /// Create new color instance
        /// </summary>
        /// <param name="red">Red component. Range: [0, 255]</param>
        /// <param name="blue">Green component. Range: [0, 255]</param>
        /// <param name="green">Blue component. Range: [0, 255]</param>
        /// <param name="alpha">Alpha component. Range: [0, 255]</param>
        public static Color Create(int red, int green, int blue, int alpha)
        {
            //range checking
            //red
            if (red > 255) red = 255;
            else if (red < 0) red = 0;
            //green
            if (green > 255) green = 255;
            else if (green < 0) green = 0;
            //blue
            if (blue > 255) blue = 255;
            else if (blue < 0) blue = 0;
            //alpha
            if (alpha > 255) alpha = 255;
            else if (alpha < 0) alpha = 0;

            return new Color(red, green, blue, alpha);
        }
        #endregion

        #region Create Cymk

        /// <summary>
        /// Create a new color in CYMK colorspace
        /// </summary>
        /// <param name="cyan">cyan component. Range: [0, 1]</param>
        /// <param name="magenta">yellow component. Range: [0, 1]</param>
        /// <param name="yellow">magenta component. Range: [0, 1]</param>
        /// <param name="black">black component. Range: [0, 1]</param>
        public static Cymk CreateCymk(double cyan, double yellow, double magenta, double black)
        {
            //range checking
            //cyan
            if (cyan > 1) cyan = 1;
            else if (cyan < 0) cyan = 0;
            //yellow
            if (yellow > 1) yellow = 1;
            else if (yellow < 0) yellow = 0;
            //magenta
            if (magenta > 1) magenta = 1;
            else if (magenta < 0) magenta = 0;

            return new Cymk(cyan, yellow, magenta, black);
        }

        /// <summary>
        /// Create a new color in CYMK colorspace
        /// </summary>
        /// <param name="cyan">cyan component. Range: [0, 1]</param>
        /// <param name="magenta">yellow component. Range: [0, 1]</param>
        /// <param name="yellow">magenta component. Range: [0, 1]</param>
        /// <param name="black">black component. Range: [0, 1]</param>
        /// <param name="alpha">Alpha component. Range: [0, 255]</param>
        public static Cymk CreateCymk(double cyan, double yellow, double magenta, double black, int alpha)
        {
            //range checking
            //cyan
            if (cyan > 1) cyan = 1;
            else if (cyan < 0) cyan = 0;
            //yellow
            if (yellow > 1) yellow = 1;
            else if (yellow < 0) yellow = 0;
            //magenta
            if (magenta > 1) magenta = 1;
            else if (magenta < 0) magenta = 0;
            //alpha
            if (alpha > 255) alpha = 255;
            else if (alpha < 0) alpha = 0;

            Cymk result = new Cymk(cyan, yellow, magenta, black);
            result.Alpha = (byte)alpha;
            return result;
        }

        #endregion

        #region Create Hsl

        /// <summary>
        /// Create a new color in HSL colorspace
        /// </summary>
        /// <param name="hue">hue component. Range: [0, 360]</param>
        /// <param name="luminance">saturation component. Range: [0, 1]</param>
        /// <param name="saturation">luminance component. Range: [0, 1]</param>
        public static Hsl CreateHsl(double hue, double saturation, double luminance)
        {
            return new Hsl(hue, saturation, luminance);
        }

        /// <summary>
        /// Create a new color in HSL colorspace
        /// </summary>
        /// <param name="hue">hue component. Range: [0, 360]</param>
        /// <param name="luminance">saturation component. Range: [0, 1]</param>
        /// <param name="saturation">luminance component. Range: [0, 1]</param>
        /// <param name="alpha">Alpha component. Range: [0, 255]</param>
        public static Hsl CreateHsl(double hue, double saturation, double luminance, int alpha)
        {
            //range checking
            //alpha
            if (alpha > 255) alpha = 255;
            else if (alpha < 0) alpha = 0;

            Hsl result = new Hsl(hue, saturation, luminance);
            result.Alpha = (byte)alpha;
            return result;
        }

        #endregion

        #region Create Hsb

        /// <summary>
        /// Create a new color in HSB colorspace
        /// </summary>
        /// <param name="hue">hue component. Range: [0, 360]</param>
        /// <param name="brightness">saturation component. Range: [0, 1]</param>
        /// <param name="saturation">brightness component. Range: [0, 1]</param>
        public static Hsb CreateHsb(double hue, double saturation, double brightness)
        {
            return new Hsb(hue, saturation, brightness);
        }

        /// <summary>
        /// Create a new color in HSB colorspace
        /// </summary>
        /// <param name="hue">hue component. Range: [0, 360]</param>
        /// <param name="brightness">saturation component. Range: [0, 1]</param>
        /// <param name="saturation">brightness component. Range: [0, 1]</param>
        /// <param name="alpha">Alpha component. Range: [0, 255]</param>
        public static Hsb CreateHsb(double hue, double saturation, double brightness, int alpha)
        {
            //range checking
            //alpha
            if (alpha > 255) alpha = 255;
            else if (alpha < 0) alpha = 0;

            Hsb result = new Hsb(hue, saturation, brightness);
            result.Alpha = (byte)alpha;
            return result;
        }

        #endregion

        #region Create Cie Xyz

        /// <summary>
        /// Create a new color in CIE XYZ colorspace
        /// </summary>
        /// <param name="x">x component. Range: [0, 0.9505]</param>
        /// <param name="z">y component. Range: [0, 1]</param>
        /// <param name="y">z component. Range: [0, 1.089]</param>
        public static CieXyz CreateCieXyz(double x, double y, double z)
        {
            return new CieXyz(x, y, z);
        }

        /// <summary>
        /// Create a new color in CIE XYZ colorspace
        /// </summary>
        /// <param name="x">x component. Range: [0, 0.9505]</param>
        /// <param name="z">y component. Range: [0, 1]</param>
        /// <param name="y">z component. Range: [0, 1.089]</param>
        /// <param name="alpha">Alpha component. Range: [0, 255]</param>
        public static CieXyz CreateCieXyz(double x, double y, double z, int alpha)
        {
            //range checking
            //alpha
            if (alpha > 255) alpha = 255;
            else if (alpha < 0) alpha = 0;

            CieXyz result = new CieXyz(x, y, z);
            result.Alpha = (byte)alpha;
            return result;
        }

        #endregion

        #region Create Cie Lab

        /// <summary>
        /// Create a new color in CIE LAB colorspace
        /// </summary>
        /// <param name="l">l component.</param>
        /// <param name="b">a component.</param>
        /// <param name="a">b component.</param>
        public static CieLab CreateCieLab(double l, double a, double b)
        {
            return new CieLab(l, a, b);
        }

        /// <summary>
        /// Create a new color in CIE LAB colorspace
        /// </summary>
        /// <param name="l">l component.</param>
        /// <param name="b">a component.</param>
        /// <param name="a">b component.</param>
        /// <param name="alpha">Alpha component. Range: [0, 255]</param>
        public static CieLab CreateCieLab(double l, double a, double b, int alpha)
        {
            //range checking
            //alpha
            if (alpha > 255) alpha = 255;
            else if (alpha < 0) alpha = 0;

            CieLab result = new CieLab(l, a, b);
            result.Alpha = (byte)alpha;
            return result;
        }

        #endregion

        #endregion

        #region To String
        /// <summary>
        /// Converts to display text
        /// </summary>
        public override string ToString()
        {
            return string.Format("R: {0} G: {1} B: {2} Alpha: {3}", Red, Green, Blue, Alpha);
        }
        #endregion

        #region Constructors
        private Color() { }

        /// <summary>
        /// Create a new instance
        /// </summary>
        public Color(byte red, byte green, byte blue)
        {
            Data = ((uint)255 << 24) | (uint)(red << 16) | (uint)(green << 8) | blue;
            Precalculate();
        }

        /// <summary>
        /// Create a new instance
        /// </summary>
        public Color(byte red, byte green, byte blue, byte alpha)
        {
            Data = (uint)(alpha << 24) | (uint)(red << 16) | (uint)(green << 8) | (uint)blue;
            Precalculate();
        }

        /// <summary>
        /// Create a new instance
        /// </summary>
        public Color(Color source, byte alpha)
        {
            Data = source.Data | (uint)(alpha << 24);
            Precalculate();
        }

        /// <summary>
        /// Create a new instance
        /// <para>Note: This constructor does not check whether values are within correct range [0, 255]</para>
        /// </summary>
        public Color(int red, int green, int blue)
        {
            Data = ((uint)255 << 24) | (uint)(red << 16) | (uint)(green << 8) | (uint)blue;
            Precalculate();
        }

        /// <summary>
        /// Create a new instance
        /// <para>Note: This constructor does not check whether values are within correct range [0, 255]</para>
        /// </summary>
        public Color(int red, int green, int blue, int alpha)
        {
            Data = (uint)(alpha << 24) | (uint)(red << 16) | (uint)(green << 8) | (uint)blue;
            Precalculate();
        }

        /// <summary>
        /// Create a new instance
        /// <para>Note: This constructor does not check whether values are within correct range [0, 255]</para>
        /// </summary>
        public Color(Color source, int alpha)
        {
            Data = source.Data | (uint)(alpha << 24);
            Precalculate();
        }

        /// <summary>
        /// Create a new instance
        /// </summary>        
        public Color(uint data)
        {
            Data = data;
            Precalculate();
        }
        #endregion
    }
}
