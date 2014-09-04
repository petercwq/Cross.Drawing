#region Using directives

#endregion

namespace Cross.Drawing
{
    /// <summary>
    /// Base class for fill or stroke
    /// </summary>
    public abstract class PaintMaterial //: TransformableElement
    {
        #region Paint
        /// <summary>
        /// Paint style
        /// </summary>
        protected Paint mPaint;
        /// <summary>
        ///Gets/Sets the paint style of this material
        /// </summary>
        public Paint Paint
        {
            get { return mPaint; }
            set
            {
                mPaint = value;
                mPaintAssigned = true;
            }
        }
        #endregion

        #region Filling Rule
        /// <summary>
        /// Filling rule
        /// </summary>
        protected FillingRule mFillingRule = FillingRule.Default;
        /// <summary>
        /// Gets/Sets filling rule (non-zero or even-odd)
        /// </summary>
        public FillingRule FillingRule
        {
            get { return mFillingRule; }
            set
            {
                mFillingRule = value;
                mFillingRuleAssigned = true;
            }
        }
        #endregion

        #region Opacity
        /// <summary>
        /// Precomputed translucency level. Values are in range [0, 256]
        /// </summary>
        internal uint ScaledOpacity = 256;

        /// <summary>
        /// Translucency level
        /// </summary>
        protected double mOpacity = 1.0;
        /// <summary>
        /// Gets/Sets the translucency level.
        /// <para>Valid values are in range [0, 1]</para>
        /// <para>Default is 1</para>
        /// <para>When Opacity is 0, the material is ignored while rendering an object</para>
        /// </summary>
        public double Opacity
        {
            get { return mOpacity; }
            set
            {
                if (value < 0) mOpacity = 0;
                else if (value > 1.0) mOpacity = 1.0;
                else mOpacity = value;

                ScaledOpacity = (uint)(mOpacity * 256);
                mOpacityAssigned = true;
            }
        }
        #endregion

        #region Transformations

        #region Translate
        /// <summary>
        /// Push translate transformation to matrix
        /// </summary>
        /// <param name="x">horizontal coordinate value to transform by</param>
        /// <param name="y">vertical coordinate value to transform by</param>
        public void Translate(double x, double y)
        {
            if (mTransformMatrix == null)
            {
                mTransformMatrix = new Matrix3x3();
                mTransformMatrixAssigned = true;
            }
            mTransformMatrix.Translate(x, y);
        }
        #endregion

        #region Rotate
        /// <summary>
        /// Push rotate transformation to matrix. The rotation origin is assumed at (0, 0)
        /// </summary>
        /// <param name="angle">The angle (in degree) to rotate by</param>
        public void Rotate(double angle)
        {
            if (mTransformMatrix == null)
            {
                mTransformMatrix = new Matrix3x3();
                mTransformMatrixAssigned = true;
            }
            mTransformMatrix.Rotate(angle);
        }

        /// <summary>
        /// Push rotate transformation to matrix
        /// </summary>
        /// <param name="angle">The angle (in degree) to rotate by</param>
        /// <param name="centerX">X-coordinate of rotation origin</param>
        /// <param name="centerY">Y-coordinate of rotation origin</param>        
        public void Rotate(double angle, double centerX, double centerY)
        {
            if (mTransformMatrix == null)
            {
                mTransformMatrix = new Matrix3x3();
                mTransformMatrixAssigned = true;
            }
            mTransformMatrix.Rotate(angle, centerX, centerY);
        }
        #endregion

        #region Scale
        /// <summary>
        /// Push scale transformation to matrix. The scaling origin is assumed at (0, 0)
        /// </summary>
        /// <param name="scaleX">The horizontal factor to scale by</param>
        /// <param name="scaleY">The vertical factor to scale by</param>
        public void Scale(double scaleX, double scaleY)
        {
            if (mTransformMatrix == null)
            {
                mTransformMatrix = new Matrix3x3();
                mTransformMatrixAssigned = true;
            }
            mTransformMatrix.Scale(scaleX, scaleY);
        }

        /// <summary>
        /// Push scale transformation to matrix.
        /// </summary>        
        /// <param name="scaleX">The horizontal factor to scale by</param>
        /// <param name="scaleY">The vertical factor to scale by</param>
        /// <param name="centerX">X-coordinate of scaling origin</param>
        /// <param name="centerY">Y-coordinate of scaling origin</param>
        public void Scale(double scaleX, double scaleY, double centerX, double centerY)
        {
            if (mTransformMatrix == null)
            {
                mTransformMatrix = new Matrix3x3();
                mTransformMatrixAssigned = true;
            }
            mTransformMatrix.Scale(scaleX, scaleY, centerX, centerY);
        }
        #endregion

        #region Skew
        /// <summary>
        /// Push skew (shear) transformation to matrix. The skewing origin is assumed at (0, 0)
        /// </summary>
        /// <param name="angleX">X-axis skew angle (in degree)</param>
        /// <param name="angleY">Y-axis skew angle (in degree)</param>
        public void Skew(double angleX, double angleY)
        {
            if (mTransformMatrix == null)
            {
                mTransformMatrix = new Matrix3x3();
                mTransformMatrixAssigned = true;
            }
            mTransformMatrix.Skew(angleX, angleY);
        }

        /// <summary>
        /// Push skew (shear) transformation to matrix
        /// </summary>
        /// <param name="angleX">X-axis skew angle (in degree)</param>
        /// <param name="angleY">Y-axis skew angle (in degree)</param>
        /// <param name="centerX">X-coordinate of skewing origin</param>
        /// <param name="centerY">Y-coordinate of skewing origin</param>
        public void Skew(double angleX, double angleY, double centerX, double centerY)
        {
            if (mTransformMatrix == null)
            {
                mTransformMatrix = new Matrix3x3();
                mTransformMatrixAssigned = true;
            }
            mTransformMatrix.Skew(angleX, angleY, centerX, centerY);
        }

        #endregion

        #region Transform
        /// <summary>
        /// Current transform
        /// </summary>
        protected Matrix3x3 mTransformMatrix;
        /// <summary>
        /// Gets/Sets current transformation matrix as a standard 3x3 matrix
        /// </summary>
        public Matrix3x3 TransformMatrix
        {
            get { return mTransformMatrix; }
            set
            {
                mTransformMatrix = value;
                mTransformMatrixAssigned = true;
            }
        }
        #endregion

        #endregion

        #region Ambient Object Pattern
        /// <summary>
        /// Whether property Paint has been assigned
        /// </summary>
        protected bool mPaintAssigned;
        /// <summary>
        /// Whether property FillingRule has been assigned
        /// </summary>
        protected bool mFillingRuleAssigned;
        /// <summary>
        /// Whether property Opacity has been assigned 
        /// </summary>
        protected bool mOpacityAssigned;
        /// <summary>
        /// Whether property TransformationMatrix has been assigned
        /// </summary>
        protected bool mTransformMatrixAssigned;
        #endregion

        #region Constructors
        /// <summary>
        /// Empty constructor
        /// </summary>
        public PaintMaterial() { }

        /// <summary>
        /// Create new instance
        /// </summary>
        public PaintMaterial(Paint paint)
        {
            Paint = paint;
        }

        /// <summary>
        /// Create new instance
        /// </summary>
        public PaintMaterial(Paint paint, double opacity)
        {
            Paint = paint;

            if (opacity < 0) Opacity = 0;
            else if (opacity > 100) Opacity = 256;
            else Opacity = opacity;
        }
        #endregion
    }
}
