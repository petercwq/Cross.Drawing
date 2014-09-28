
namespace Cross.Drawing.ColorSpaces
{
    /// <summary>
    /// A single color in YUV color space
    /// </summary>
    public class Yuv
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

        #region Y
        private double mY;
        /// <summary>
        /// Gets/Sets Y component
        /// <para>Value must be in range [0, 1]</para>
        /// </summary>
        public double Y
        {
            get { return mY; }
            set
            {
                if (value > 1) mY = 1;
                else if (value < 0) mY = 0;
                else mY = value;
            }
        }
        #endregion

        #region U
        private double mU;
        /// <summary>
        /// Gets/Sets U component
        /// <para>Value must be in range [-0.436, 0.436]</para>
        /// </summary>
        public double U
        {
            get { return mU; }
            set
            {
                if (value > 0.436) mU = 0.436;
                else if (value < -0.436) mU = -0.436;
                else mU = value;
            }
        }
        #endregion

        #region V
        private double mV;
        /// <summary>
        /// Gets/Sets V component
        /// <para>Value must be in range [-0.615, 0.615]</para>
        /// </summary>
        public double V
        {
            get { return mV; }
            set
            {
                if (value > 0.615) mV = 0.615;
                else if (value < -0.615) mV = 0.615;
                else mV = value;
            }
        }
        #endregion

        #endregion

        #region Operators

        public static bool operator ==(Yuv a, Yuv b)
        {
            return (a.mY == b.mY && a.mU == b.mU && a.mV == b.mV && a.mAlpha == b.mAlpha);
        }

        public static bool operator !=(Yuv a, Yuv b)
        {
            return (a.mY != b.mY || a.mU != b.mU || a.mV != b.mV || a.mAlpha != b.mAlpha);
        }
        #endregion

        #region Equality

        /// <summary>
        /// Check quality of the object against this instance
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is Yuv) return this == (Yuv)obj;
            else return false;
        }

        /// <summary>
        /// Returns the hash code for this instance
        /// </summary>
        public override int GetHashCode()
        {
            return mY.GetHashCode() ^ mU.GetHashCode() ^ mV.GetHashCode();
        }
        #endregion

        #region To String
        /// <summary>
        /// Converts to display text
        /// </summary>
        public override string ToString()
        {
            return string.Format("Y: {0} U: {1} V: {2} Alpha: {3}", mY, mU, mV, mAlpha);
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor
        /// </summary>
        public Yuv()
        { }

        /// <summary>
        /// Create new instance
        /// </summary>
        public Yuv(double y, double u, double v)
        {
            //y
            if (y > 1) mY = 1;
            else if (y < 0) mY = 0;
            else mY = y;

            //u
            if (u > 0.436) mU = 0.436;
            else if (u < -0.436) mU = -0.436;
            else mU = u;

            //v
            if (v > 0.615) mV = 0.615;
            else if (v < -0.615) mV = 0.615;
            else mV = v;
        }
        #endregion
    }
}

