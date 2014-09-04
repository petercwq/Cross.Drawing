#region Using directives

#endregion

namespace Cross.Drawing.ColorSpaces
{
    /// <summary>
    /// A single color in CIE LAB color space
    /// <para>Lab color space is a color-opponent space with dimension L for luminance and a and b for the color-opponent dimensions, based on nonlinearly-compressed CIE XYZ color space coordinates.</para>
    /// </summary>
    public class CieLab
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

        #region L
        private double mL;
        /// <summary>
        /// Gets/Sets L component
        /// </summary>
        public double L
        {
            get { return mL; }
            set { mL = value; }
        }
        #endregion

        #region A
        private double mA;
        /// <summary>
        /// Gets/Sets A
        /// </summary>
        public double A
        {
            get { return mA; }
            set { mA = value; }
        }
        #endregion

        #region B
        private double mB;
        /// <summary>
        /// Gets/Sets B component 
        /// </summary>
        public double B
        {
            get { return mB; }
            set { mB = value; }
        }
        #endregion

        #endregion

        #region Operators

        public static bool operator ==(CieLab a, CieLab b)
        {
            return (a.mL == b.mL && a.mA == b.mA && a.mB == b.mB && a.mAlpha == b.mAlpha);
        }

        public static bool operator !=(CieLab a, CieLab b)
        {
            return (a.mL != b.mL || a.mA != b.mA || a.mB != b.mB || a.mAlpha != b.mAlpha);
        }
        #endregion

        #region Equality

        /// <summary>
        /// Check quality of the object against this instance
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is CieLab) return this == (CieLab)obj;
            else return false;
        }

        /// <summary>
        /// Returns the hash code for this instance
        /// </summary>
        public override int GetHashCode()
        {
            return mL.GetHashCode() ^ mA.GetHashCode() ^ mB.GetHashCode() ^ mAlpha.GetHashCode();
        }
        #endregion

        #region To String
        /// <summary>
        /// Converts to display text
        /// </summary>
        public override string ToString()
        {
            return string.Format("L*: {0} A*: {1} B*: {2} Alpha: {3}", mL, mA, mB, mAlpha);
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor
        /// </summary>
        public CieLab()
        { }

        /// <summary>
        /// Create new instance
        /// </summary>
        public CieLab(double l, double a, double b)
        {
            mL = l;
            mA = a;
            mB = b;
        }
        #endregion
    }
}
