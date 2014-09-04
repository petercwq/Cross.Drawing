#region Using directives
using System;
#endregion

namespace Cross.Drawing
{
    /// <summary>
    /// Matrix 3x3 using for transformation and internal using only
    /// <para>Affine transformation are linear transformations in Cartesian coordinates
    ///(strictly speaking not only in Cartesian, but for the beginning we will 
    /// think so). They are rotation, scaling, translation and skewing.  
    /// After any affine transformation a line segment remains a line segment 
    /// and it will never become a curve.</para>
    /// <para>There are two group of methods to use matrix:
    /// _ Normal method, just build a matrix without apply current transformation
    /// _ xxxStack method, build matrix and apply current transformation
    /// </para>
    /// </summary>
    /// <remarks>
    /// Matrix will be:
    ///     [sx]    [shx]   [tx]
    ///     [shy]   [xy]    [ty]
    ///     [0]     [0]     [1]
    /// When want to transform we just multiply matrix to 
    /// [x] [y] [1]. as following
    /// 
    /// tmp = x
    /// x = tmp * sx + y * shx + tx;
    /// y = tmp * shy + y * sy + ty;
    /// 
    /// Notice that, when scale x, or y to 0, this matrix can not be converted.
    /// </remarks>
    public class Matrix3x3
    {
        #region Const
        /// <summary>
        /// Epsilon using to compare
        /// </summary>
        const double AffineEpsilon = 1e-14;
        /// <summary>
        /// Degree to radian factor
        /// </summary>
        const double DegreeToRadianFactor = Math.PI / 180;
        #endregion

        #region JIT property
        #region IsTransformed
        private bool mIsTransformed;
        /// <summary>
        /// This boolean indicate that this matrix is transformed or just like normal.
        /// This property is calculated each type access it, please careful using.
        /// </summary>
        public bool IsTransformed
        {
            get
            {
                if (isChanged)
                {
                    CalculateJustInTime();
                }
                return mIsTransformed;
            }
        }
        #endregion

        #region InvertedMatrix
        private Matrix3x3 mInvertedMatrix;
        /// <summary>
        /// Gets Inverted matrix of current matrix.
        /// Null when current matrix can't be inverted
        /// </summary>
        public Matrix3x3 InvertedMatrix
        {
            get
            {
                if (isChanged)
                {
                    CalculateJustInTime();
                }
                return mInvertedMatrix;
            }
        }
        #endregion

        #endregion

        #region Fields
        /// <summary>
        /// Indicate that scale and translate only.
        /// This is correct when using for normal matrix.
        /// Not applied for inverted matrix, or matrix
        /// modified or constructed direct 6 parameters
        /// </summary>
        internal bool SimpleScaleAndTranslateOnly = true;

        /// <summary>
        /// Indicate that current matrix include scale and transform only
        /// This is difference to above, because of, when there are negative scale.
        /// Simple scale and translate is not true
        /// </summary>
        private bool ScaleAndTransformOnly = true;


        /// <summary>
        /// Scale x factor
        /// 
        /// This is correct when using for normal matrix.
        /// Not applied for inverted matrix, or matrix
        /// modified or constructed direct 6 parameters
        /// </summary>
        internal double ScaleXFactor = 1.0;

        /// <summary>
        /// Scale y factor
        /// 
        /// This is correct when using for normal matrix.
        /// Not applied for inverted matrix, or matrix
        /// modified or constructed direct 6 parameters
        /// </summary>
        internal double ScaleYFactor = 1.0;

        /// <summary>
        /// The value in the first row, first column of matrix.
        /// </summary>
        public double Sx;
        /// <summary>
        /// The value in the first row, second column of matrix.
        /// </summary>
        public double Shy;
        /// <summary>
        /// The value in the second row, first column of matrix.
        /// </summary>
        public double Shx;
        /// <summary>
        /// The value in the second row, second column of matrix.
        /// </summary>
        public double Sy;
        /// <summary>
        /// The value in the third row and first column of matrix
        /// </summary>
        public double Tx;
        /// <summary>
        /// The value in the third row and second column of matrix
        /// </summary>
        public double Ty;

        /// <summary>
        /// These value is used for multiply transform
        /// </summary>
        double t0, t2, t4;
        double t1, t3, t5;
        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor.
        /// Identity matrix.
        /// </summary>
        public Matrix3x3()
        {
            Sx = 1.0;
            Shy = 0.0;
            Shx = 0.0;
            Sy = 1.0;
            Tx = 0.0;
            Ty = 0.0;
        }

        /// <summary>
        /// New matrix from other
        /// Create a new instance with information from the provided source matrix
        /// </summary>
        /// <param name="matrix">The source matrix to copy values from</param>
        public Matrix3x3(Matrix3x3 source)
        {
            Sx = source.Sx;
            Shy = source.Shy;
            Shx = source.Shx;
            Sy = source.Sy;
            Tx = source.Tx;
            Ty = source.Ty;
            //IsTransformed = matrix.IsTransformed;
            ScaleXFactor = source.ScaleXFactor;
            ScaleYFactor = source.ScaleYFactor;

            SimpleScaleAndTranslateOnly = source.SimpleScaleAndTranslateOnly;
            ScaleAndTransformOnly = source.ScaleAndTransformOnly;

            isChanged = true;
        }
        #endregion

        #region calculate just in time property
        /// <summary>
        /// boolean indicate that current matrix have any changes from previous calculation
        /// </summary>
        bool isChanged = true;

        /// <summary>
        /// Calculate just in time property
        /// </summary>
        void CalculateJustInTime()
        {
            // when there are changed
            mIsTransformed = ((Sx != 1.0 || Sy != 1 || Shx != 0.0 || Shy != 0.0 || Tx != 0 || Ty != 0));
            if (CanInvert())
            {
                //mInvertedMatrix = CloneInverted();
                mInvertedMatrix = Clone();
                mInvertedMatrix.Invert();
            }
            else
            {
                mInvertedMatrix = null;
            }
            // turn of the flag
            isChanged = false;
        }
        #endregion

        #region transform matrix, using for drawer ( this this do not need to know current transform )
        #region Translate
        /// <summary>
        /// Apply current transformation for (x, y) then tranlate coordinate to x,y
        /// </summary>
        /// <param name="x">x</param>
        /// <param name="y">y</param>
        public void Translate(double x, double y)
        {
            Tx += x;
            Ty += y;
            isChanged = true;
        }

        /// <summary>
        /// Apply current transformation for (x, y) then tranlate coordinate to x,y
        /// </summary>
        /// <param name="x">x</param>
        /// <param name="y">y</param>
        public void TranslatePrepend(double x, double y)
        {
            PrependSelfMultiply(1.0, 1.0, 0.0, 0.0, x, y);
            isChanged = true;
        }
        #endregion

        #region Rotate
        /// <summary>
        /// Apply current transformation for (0,0) then rotate an angle (in degree)
        /// </summary>
        /// <param name="angle">An angle, measured in degree</param>
        /// <returns></returns>
        public void Rotate(double angle)
        {
            Rotate(angle, 0.0, 0.0);
        }

        /// <summary>
        /// Apply current transformation for (0,0) then rotate an angle ( in radian).
        /// </summary>
        /// <param name="angleRad">angle in radian</param>
        public void RotateRad(double angleRad)
        {
            RotateRad(angleRad, 0.0, 0.0);
        }
        #endregion

        #region Rotate at
        /// <summary>
        /// Apply current transformation for (centerX, centerY) then rotate by angle (in degree)
        /// </summary>
        /// <param name="angle">angle in degree</param>
        /// <param name="centerX">x position</param>
        /// <param name="centerY">y position</param>
        public void Rotate(double angle, double centerX, double centerY)
        {
            if (angle != 0)
            {

                //angle = (angle % 360) * DegreeToRadianFactor;
                angle *= DegreeToRadianFactor;
                double num = Math.Sin(angle);
                double num2 = Math.Cos(angle);
                double offsetX = (centerX * (1.0 - num2)) + (centerY * num);
                double offsetY = (centerY * (1.0 - num2)) - (centerX * num);

                //SelfMultiply(num2, num, -num, num2, offsetX, offsetY);
                SelfMultiply(num2, num2, -num, num, offsetX, offsetY);

                isChanged = true;
                SimpleScaleAndTranslateOnly = false;
                ScaleAndTransformOnly = false;
            }
        }

        /// <summary>
        /// Apply current transformation for (x, y) then rotate by angle (in radian)
        /// </summary>
        /// <param name="angleRad">angle in radian</param>
        /// <param name="x">x position</param>
        /// <param name="y">y position</param>
        public void RotateRad(double angleRad, double centerX, double centerY)
        {
            if (angleRad != 0)
            {
                //angleRad = (angleRad % 360);
                double num = Math.Sin(angleRad);
                double num2 = Math.Cos(angleRad);
                double offsetX = (centerX * (1.0 - num2)) + (centerY * num);
                double offsetY = (centerY * (1.0 - num2)) - (centerX * num);

                SelfMultiply(num2, num2, -num, num, offsetX, offsetY);

                //IsTransformed = ((sx != 1.0 || sy != 1 || shx != 0.0 || shy != 0.0 || tx != 0 || ty != 0));
                isChanged = true;
                SimpleScaleAndTranslateOnly = false;
            }
        }
        #endregion

        #region Rotate prepend
        /// <summary>
        /// Apply current transformation for (0,0) then rotate an angle (in degree)
        /// </summary>
        /// <param name="angle">An angle, measured in degree</param>
        /// <returns></returns>
        public void RotatePrepend(double angle)
        {
            RotatePrepend(angle, 0.0, 0.0);
        }

        /// <summary>
        /// Apply current transformation for (0,0) then rotate an angle ( in radian).
        /// </summary>
        /// <param name="angleRad">angle in radian</param>
        public void RotateRadPrepend(double angleRad)
        {
            RotateRadPrepend(angleRad, 0.0, 0.0);
        }
        #endregion

        #region Rotate at
        /// <summary>
        /// Apply current transformation for (centerX, centerY) then rotate by angle (in degree)
        /// </summary>
        /// <param name="angle">angle in degree</param>
        /// <param name="centerX">x position</param>
        /// <param name="centerY">y position</param>
        public void RotatePrepend(double angle, double centerX, double centerY)
        {
            if (angle != 0)
            {
                //angle = (angle % 360) * DegreeToRadianFactor;
                angle = DegreeToRadianFactor;
                double num = Math.Sin(angle);
                double num2 = Math.Cos(angle);
                double offsetX = (centerX * (1.0 - num2)) + (centerY * num);
                double offsetY = (centerY * (1.0 - num2)) - (centerX * num);

                PrependSelfMultiply(num2, num2, -num, num, offsetX, offsetY);

                isChanged = true;
                SimpleScaleAndTranslateOnly = false;
                ScaleAndTransformOnly = false;
            }
        }

        /// <summary>
        /// Apply current transformation for (x, y) then rotate by angle (in radian)
        /// </summary>
        /// <param name="angleRad">angle in radian</param>
        /// <param name="x">x position</param>
        /// <param name="y">y position</param>
        public void RotateRadPrepend(double angleRad, double centerX, double centerY)
        {
            if (angleRad != 0)
            {

                double num = Math.Sin(angleRad);
                double num2 = Math.Cos(angleRad);
                double offsetX = (centerX * (1.0 - num2)) + (centerY * num);
                double offsetY = (centerY * (1.0 - num2)) - (centerX * num);

                PrependSelfMultiply(num2, num2, -num, num, offsetX, offsetY);

                //IsTransformed = ((sx != 1.0 || sy != 1 || shx != 0.0 || shy != 0.0 || tx != 0 || ty != 0));
                isChanged = true;
                SimpleScaleAndTranslateOnly = false;
                ScaleAndTransformOnly = false;
            }
        }
        #endregion

        #region Scale
        /// <summary>
        /// Apply current transformation for (0,0) then scale by both x and y with the same scale ratio
        /// </summary>
        /// <param name="scale"></param>
        /// <returns></returns>
        public void Scale(double scale)
        {
            if ((scale != 1.0))
            {
                SelfMultiply(
                    scale, scale,
                    0.0, 0.0,
                    0.0, 0.0);

                isChanged = true;
                ScaleXFactor *= scale;
                ScaleYFactor *= scale;
                ValidateScale();
            }
        }

        /// <summary>
        /// Apply current transformation for (0,0) then scale (by x and y).
        /// Basically used to calculate the approximation_scale when
        /// decomposinting curves into line segments.
        /// </summary>
        /// <param name="xScale">horizontal scale</param>
        /// <param name="yScale">vertical scale</param>
        /// <returns></returns>
        public void Scale(double xScale, double yScale)
        {
            //Scale(xScale, yScale, 0.0, 0.0);
            if ((xScale != 1.0) || (yScale != 1.0))
            {
                SelfMultiply(
                    xScale, yScale,
                    0.0, 0.0,
                    0.0, 0.0);

                #region scale
                //Sx *= xScale; Shx *= xScale; Tx *= xScale;
                //Shy *= yScale; Sy *= yScale; Ty *= yScale;
                #endregion

                isChanged = true;
                ScaleXFactor *= xScale;
                ScaleYFactor *= yScale;
                ValidateScale();
            }
        }

        /// <summary>
        /// Apply current transformation for (centerX, centerY) then scale at a center point
        /// </summary>
        /// <param name="xScale">x scale</param>
        /// <param name="yScale">y scale</param>
        /// <param name="centerX">center x-coordinate</param>
        /// <param name="centerY">center y-coordinate</param>
        public void Scale(double xScale, double yScale, double centerX, double centerY)
        {
            if ((xScale != 1.0) || (yScale != 1.0))
            {
                #region origin code
                //#region transform x,y
                //double tmp = centerX;
                //centerX = tmp * Sx + centerY * Shx + Tx;
                //centerY = tmp * Shy + centerY * Sy + Ty;
                //#endregion

                //#region translate -x,-y
                //Tx -= centerX;
                //Ty -= centerY;
                //#endregion

                //#region scale
                //Sx *= xScale; Shx *= xScale; Tx *= xScale;
                //Shy *= yScale; Sy *= yScale; Ty *= yScale;
                //#endregion

                //#region translate x,y
                //Tx += centerX;
                //Ty += centerY;
                //#endregion
                #endregion

                SelfMultiply(
                    xScale, yScale,
                    0.0, 0.0,
                    centerX - (centerX * xScale),
                    centerY - (centerY * yScale));

                isChanged = true;
                ScaleXFactor *= xScale;
                ScaleYFactor *= yScale;
                ValidateScale();
            }
        }
        #endregion

        #region Scale Prepend
        /// <summary>
        /// Apply current transformation for (0,0) then scale by both x and y with the same scale ratio
        /// </summary>
        /// <param name="scale"></param>
        /// <returns></returns>
        public void ScalePrepend(double scale)
        {
            if ((scale != 1.0))
            {
                PrependSelfMultiply(
                    scale, scale,
                    0.0, 0.0,
                    0.0, 0.0);

                isChanged = true;
                ScaleXFactor *= scale;
                ScaleYFactor *= scale;
                ValidateScale();
            }
        }

        /// <summary>
        /// Apply current transformation for (0,0) then scale (by x and y).
        /// Basically used to calculate the approximation_scale when
        /// decomposinting curves into line segments.
        /// </summary>
        /// <param name="xScale">horizontal scale</param>
        /// <param name="yScale">vertical scale</param>
        /// <returns></returns>
        public void ScalePrepend(double xScale, double yScale)
        {
            //Scale(xScale, yScale, 0.0, 0.0);
            if ((xScale != 1.0) || (yScale != 1.0))
            {
                PrependSelfMultiply(
                    xScale, yScale,
                    0.0, 0.0,
                    0.0, 0.0);

                isChanged = true;
                ScaleXFactor *= xScale;
                ScaleYFactor *= yScale;
                ValidateScale();
            }
        }

        /// <summary>
        /// Apply current transformation for (centerX, centerY) then scale at a center point
        /// </summary>
        /// <param name="xScale">x scale</param>
        /// <param name="yScale">y scale</param>
        /// <param name="centerX">center x-coordinate</param>
        /// <param name="centerY">center y-coordinate</param>
        public void ScalePrepend(double xScale, double yScale, double centerX, double centerY)
        {
            if ((xScale != 1.0) || (yScale != 1.0))
            {

                PrependSelfMultiply(
                    xScale, yScale,
                    0.0, 0.0,
                    centerX - (centerX * xScale),
                    centerY - (centerY * yScale));

                isChanged = true;
                ScaleXFactor *= xScale;
                ScaleYFactor *= yScale;
                ValidateScale();
            }
        }
        #endregion

        #region Skew
        /// <summary>
        /// Skew ( shear)
        /// </summary>
        /// <param name="xSkewAngle">skew x angle ( in degree)</param>
        /// <param name="ySkewAngle">skew y angle ( in degree)</param>
        /// <remarks>When change from ratio to angle using
        /// following fomula : ratio = Math.Tan(angle)</remarks>
        public void Skew(double xSkewAngle, double ySkewAngle)
        {
            if ((xSkewAngle != 0) || (ySkewAngle != 0))
            {
                SelfMultiply(1.0, 1.0,
                    Math.Tan(xSkewAngle * DegreeToRadianFactor),
                    Math.Tan(ySkewAngle * DegreeToRadianFactor),
                    0.0, 0.0);

                isChanged = true;

                SimpleScaleAndTranslateOnly = false;
                ScaleAndTransformOnly = false;
            }
        }
        /// <summary>
        /// Apply current transformation for (0,0) then skew ( shear)
        /// </summary>
        /// <param name="skewAngle">skew angle</param>
        public void Skew(double skewAngle)
        {
            if ((skewAngle != 0))
            {
                SelfMultiply(1.0, 1.0,
                    Math.Tan(skewAngle * DegreeToRadianFactor),
                    Math.Tan(skewAngle * DegreeToRadianFactor),
                    0.0, 0.0);

                isChanged = true;

                SimpleScaleAndTranslateOnly = false;
                ScaleAndTransformOnly = false;
            }
        }

        /// <summary>
        /// Apply current transformation for (centerX, centerY) then skew at center point
        /// </summary>
        /// <param name="xSkewAngle">x skew angle (in degree)</param>
        /// <param name="ySkewAngle">y skew angle (in degree)</param>
        /// <param name="centerX">center point x coordinate</param>
        /// <param name="centerY">center point y coordinate</param>
        public void Skew(double xSkewAngle, double ySkewAngle, double centerX, double centerY)
        {
            if ((xSkewAngle != 0) || (ySkewAngle != 0))
            {
                #region OLD HUYHM DEC 24 2008
                //#region transform x,y
                //double tmp = centerX;
                //centerX = tmp * Sx + centerY * Shx + Tx;
                //centerY = tmp * Shy + centerY * Sy + Ty;
                //#endregion
                //#region translate -x,-y
                //Tx -= centerX;
                //Ty -= centerY;
                //#endregion
                //#region skew
                //// change from degree to radian
                ////xSkewAngle = xSkewAngle * DegreeToRadianFactor;
                ////ySkewAngle = ySkewAngle * DegreeToRadianFactor;
                //xSkewAngle = Math.Tan(xSkewAngle * DegreeToRadianFactor);
                //ySkewAngle = Math.Tan(ySkewAngle * DegreeToRadianFactor);

                //// multiply with matrix that have
                //t0 = Sx + Shy * xSkewAngle;
                //t2 = Shx + Sy * xSkewAngle;
                //t4 = Tx + Ty * xSkewAngle;

                //Shy = Sx * ySkewAngle + Shy;
                //Sy = Shx * ySkewAngle + Sy;
                //Ty = Tx * ySkewAngle + Ty;

                //Sx = t0;
                //Shx = t2;
                //Tx = t4;
                //#endregion
                //#region translate x,y
                //Tx += centerX;
                //Ty += centerY;
                //#endregion
                #endregion

                Tx -= centerX;
                Ty -= centerY;
                SelfMultiply(1.0, 1.0,
                    Math.Tan(xSkewAngle * DegreeToRadianFactor),
                    Math.Tan(ySkewAngle * DegreeToRadianFactor),
                    0.0, 0.0);
                Tx += centerX;
                Ty += centerY;
                isChanged = true;

                SimpleScaleAndTranslateOnly = false;
                ScaleAndTransformOnly = false;
            }
        }

        #endregion

        #region Skew prepend
        /// <summary>
        /// Skew ( shear) prepend
        /// </summary>
        /// <param name="xSkewAngle">skew x angle ( in degree)</param>
        /// <param name="ySkewAngle">skew y angle ( in degree)</param>
        /// <remarks>When change from ratio to angle using
        /// following fomula : ratio = Math.Tan(angle)</remarks>
        public void SkewPrepend(double xSkewAngle, double ySkewAngle)
        {
            //Skew(xSkewAngle, ySkewAngle, 0.0, 0.0);
            if ((xSkewAngle != 0) || (ySkewAngle != 0))
            {
                PrependSelfMultiply(1.0, 1.0,
                    Math.Tan(xSkewAngle * DegreeToRadianFactor),
                    Math.Tan(ySkewAngle * DegreeToRadianFactor),
                    0.0, 0.0);

                isChanged = true;

                SimpleScaleAndTranslateOnly = false;
                ScaleAndTransformOnly = false;
            }
        }

        /// <summary>
        /// Apply current transformation for (0,0) then skew ( shear)
        /// </summary>
        /// <param name="skewAngle">skew angle</param>
        public void SkewPrepend(double skewAngle)
        {
            //Skew(skewAngle, skewAngle, 0.0, 0.0);
            if ((skewAngle != 0))
            {
                PrependSelfMultiply(1.0, 1.0,
                    Math.Tan(skewAngle * DegreeToRadianFactor),
                    Math.Tan(skewAngle * DegreeToRadianFactor),
                    0.0, 0.0);

                isChanged = true;

                SimpleScaleAndTranslateOnly = false;
                ScaleAndTransformOnly = false;
            }
        }

        /// <summary>
        /// Apply current transformation for (centerX, centerY) then skew at center point
        /// </summary>
        /// <param name="xSkewAngle">x skew angle (in degree)</param>
        /// <param name="ySkewAngle">y skew angle (in degree)</param>
        /// <param name="centerX">center point x coordinate</param>
        /// <param name="centerY">center point y coordinate</param>
        public void SkewPrepend(double xSkewAngle, double ySkewAngle, double centerX, double centerY)
        {
            if ((xSkewAngle != 0) || (ySkewAngle != 0))
            {
                Matrix3x3 matrix = new Matrix3x3();
                matrix.Translate(-centerX, -centerY);
                matrix.Skew(xSkewAngle, ySkewAngle);
                matrix.Translate(centerX, centerY);

                // then prepend multiply
                this.PrependSelfMultiply(matrix.Sx, matrix.Sy, matrix.Shx, matrix.Shy, matrix.Tx, matrix.Ty);
                //PrependSelfMultiply(1.0, 1.0,
                //    Math.Tan(xSkewAngle * DegreeToRadianFactor),
                //    Math.Tan(ySkewAngle * DegreeToRadianFactor),
                //    centerX, centerY);

                isChanged = true;

                SimpleScaleAndTranslateOnly = false;
                ScaleAndTransformOnly = false;
            }
        }

        #endregion

        #endregion

        #region transform with knownledge of current position

        #region Translate
        /// <summary>
        /// Apply current transformation for (x, y) then tranlate coordinate to x,y
        /// </summary>
        /// <param name="x">x</param>
        /// <param name="y">y</param>
        public void TranslateRelative(double x, double y)
        {
            //because of translate is not affected by current transform
            #region transform center
            double tmp = x;
            x = tmp * Sx + y * Shx; // without modify translate
            y = tmp * Shy + y * Sy; // without modify translate
            #endregion

            // so this will not need to transform
            Tx += x;
            Ty += y;
            isChanged = true;
        }
        #endregion

        #region Rotate
        /// <summary>
        /// Apply current transformation for (0,0) then rotate an angle (in degree)
        /// </summary>
        /// <param name="angle">An angle, measured in degree</param>
        /// <returns></returns>
        public void RotateRelative(double angle)
        {
            RotateRelative(angle, 0.0, 0.0);
        }

        /// <summary>
        /// Apply current transformation for (0,0) then rotate an angle ( in radian).
        /// </summary>
        /// <param name="angleRad">angle in radian</param>
        public void RotateRadRelative(double angleRad)
        {
            RotateRadRelative(angleRad, 0.0, 0.0);
        }
        #endregion

        #region Rotate at
        /// <summary>
        /// Apply current transformation for (centerX, centerY) then rotate by angle (in degree)
        /// </summary>
        /// <param name="angle">angle in degree</param>
        /// <param name="centerX">x position</param>
        /// <param name="centerY">y position</param>
        public void RotateRelative(double angle, double centerX, double centerY)
        {
            if (angle != 0)
            {
                #region transform center
                double tmp = centerX;
                centerX = tmp * Sx + centerY * Shx + Tx;
                centerY = tmp * Shy + centerY * Sy + Ty;
                #endregion

                angle *= DegreeToRadianFactor;
                double num = Math.Sin(angle);
                double num2 = Math.Cos(angle);
                double offsetX = (centerX * (1.0 - num2)) + (centerY * num);
                double offsetY = (centerY * (1.0 - num2)) - (centerX * num);

                //SelfMultiply(num2, num, -num, num2, offsetX, offsetY);
                SelfMultiply(num2, num2, -num, num, offsetX, offsetY);

                isChanged = true;
                SimpleScaleAndTranslateOnly = false;
                ScaleAndTransformOnly = false;
            }
        }

        /// <summary>
        /// Apply current transformation for (x, y) then rotate by angle (in radian)
        /// </summary>
        /// <param name="angleRad">angle in radian</param>
        /// <param name="x">x position</param>
        /// <param name="y">y position</param>
        public void RotateRadRelative(double angleRad, double centerX, double centerY)
        {
            if (angleRad != 0)
            {
                #region transform center
                double tmp = centerX;
                centerX = tmp * Sx + centerY * Shx + Tx;
                centerY = tmp * Shy + centerY * Sy + Ty;
                #endregion

                //angleRad = (angleRad % 360);
                double num = Math.Sin(angleRad);
                double num2 = Math.Cos(angleRad);
                double offsetX = (centerX * (1.0 - num2)) + (centerY * num);
                double offsetY = (centerY * (1.0 - num2)) - (centerX * num);

                SelfMultiply(num2, num2, -num, num, offsetX, offsetY);

                //IsTransformed = ((sx != 1.0 || sy != 1 || shx != 0.0 || shy != 0.0 || tx != 0 || ty != 0));
                isChanged = true;
                SimpleScaleAndTranslateOnly = false;
                ScaleAndTransformOnly = false;
            }
        }
        #endregion

        #region Scale
        /// <summary>
        /// Apply current transformation for (0,0) then scale by both x and y with the same scale ratio
        /// </summary>
        /// <param name="scale"></param>
        /// <returns></returns>
        public void ScaleRelative(double scale)
        {
            if ((scale != 1.0))
            {
                #region transform center
                //double centerX = 0.0;
                //double centerY = 0.0;
                //double tmp = centerX;
                //centerX = tmp * Sx + centerY * Shx + Tx;
                //centerY = tmp * Shy + centerY * Sy + Ty;

                // so it will be
                //double centerX = Tx;
                //double centerY = Ty;
                #endregion

                SelfMultiply(
                    scale, scale,
                    0.0, 0.0,
                    Tx - (Tx * scale),
                    Ty - (Ty * scale));

                isChanged = true;
                ScaleXFactor *= scale;
                ScaleYFactor *= scale;
                ValidateScale();
            }
        }

        /// <summary>
        /// Apply current transformation for (0,0) then scale (by x and y).
        /// Basically used to calculate the approximation_scale when
        /// decomposinting curves into line segments.
        /// </summary>
        /// <param name="xScale">horizontal scale</param>
        /// <param name="yScale">vertical scale</param>
        /// <returns></returns>
        public void ScaleRelative(double xScale, double yScale)
        {
            //Scale(xScale, yScale, 0.0, 0.0);
            if ((xScale != 1.0) || (yScale != 1.0))
            {
                //SelfMultiply(
                //    xScale, yScale,
                //    0.0, 0.0,
                //    0.0, 0.0);

                //Same as above method, just need to modify tx,ty
                SelfMultiply(
                   xScale, yScale,
                   0.0, 0.0,
                   Tx - (Tx * xScale),
                   Ty - (Ty * yScale));

                isChanged = true;
                ScaleXFactor *= xScale;
                ScaleYFactor *= yScale;
                ValidateScale();
            }
        }

        /// <summary>
        /// Apply current transformation for (centerX, centerY) then scale at a center point
        /// </summary>
        /// <param name="xScale">x scale</param>
        /// <param name="yScale">y scale</param>
        /// <param name="centerX">center x-coordinate</param>
        /// <param name="centerY">center y-coordinate</param>
        public void ScaleRelative(double xScale, double yScale, double centerX, double centerY)
        {
            if ((xScale != 1.0) || (yScale != 1.0))
            {

                #region transform center
                double tmp = centerX;
                centerX = tmp * Sx + centerY * Shx + Tx;
                centerY = tmp * Shy + centerY * Sy + Ty;
                #endregion

                //Log.Debug("Current scale:"

                SelfMultiply(
                    xScale, yScale,
                    0.0, 0.0,
                    centerX - (centerX * xScale),
                    centerY - (centerY * yScale));

                isChanged = true;
                ScaleXFactor *= xScale;
                ScaleYFactor *= yScale;
                ValidateScale();
            }
        }
        #endregion

        #region Skew
        /// <summary>
        /// Skew ( shear)
        /// </summary>
        /// <param name="xSkewAngle">skew x angle ( in degree)</param>
        /// <param name="ySkewAngle">skew y angle ( in degree)</param>
        /// <remarks>When change from ratio to angle using
        /// following fomula : ratio = Math.Tan(angle)</remarks>
        public void SkewRelative(double xSkewAngle, double ySkewAngle)
        {
            if ((xSkewAngle != 0) || (ySkewAngle != 0))
            {
                #region transform center
                //double centerX = 0.0;
                //double centerY = 0.0;
                //double tmp = centerX;
                //centerX = tmp * Sx + centerY * Shx + Tx;
                //centerY = tmp * Shy + centerY * Sy + Ty;

                // so it will be
                double centerX = Tx;
                double centerY = Ty;
                #endregion

                Tx -= centerX;
                Ty -= centerY;

                SelfMultiply(1.0, 1.0,
                    Math.Tan(xSkewAngle * DegreeToRadianFactor),
                    Math.Tan(ySkewAngle * DegreeToRadianFactor),
                    0.0, 0.0);
                Tx += centerX;
                Ty += centerY;
                isChanged = true;

                SimpleScaleAndTranslateOnly = false;
                ScaleAndTransformOnly = false;

            }
        }
        /// <summary>
        /// Apply current transformation for (0,0) then skew ( shear)
        /// </summary>
        /// <param name="skewAngle">skew angle</param>
        public void SkewRelative(double skewAngle)
        {
            if ((skewAngle != 0))
            {
                #region transform center
                //double centerX = 0.0;
                //double centerY = 0.0;
                //double tmp = centerX;
                //centerX = tmp * Sx + centerY * Shx + Tx;
                //centerY = tmp * Shy + centerY * Sy + Ty;

                // so it will be
                double centerX = Tx;
                double centerY = Ty;
                #endregion

                Tx -= centerX;
                Ty -= centerY;

                SelfMultiply(1.0, 1.0,
                    Math.Tan(skewAngle * DegreeToRadianFactor),
                    Math.Tan(skewAngle * DegreeToRadianFactor),
                    0.0, 0.0);

                Tx += centerX;
                Ty += centerY;

                isChanged = true;

                SimpleScaleAndTranslateOnly = false;
                ScaleAndTransformOnly = false;
            }
        }

        /// <summary>
        /// Apply current transformation for (centerX, centerY) then skew at center point
        /// </summary>
        /// <param name="xSkewAngle">x skew angle (in degree)</param>
        /// <param name="ySkewAngle">y skew angle (in degree)</param>
        /// <param name="centerX">center point x coordinate</param>
        /// <param name="centerY">center point y coordinate</param>
        public void SkewRelative(double xSkewAngle, double ySkewAngle, double centerX, double centerY)
        {
            if ((xSkewAngle != 0) || (ySkewAngle != 0))
            {

                #region transform center
                double tmp = centerX;
                centerX = tmp * Sx + centerY * Shx + Tx;
                centerY = tmp * Shy + centerY * Sy + Ty;
                #endregion

                Tx -= centerX;
                Ty -= centerY;
                SelfMultiply(1.0, 1.0,
                    Math.Tan(xSkewAngle * DegreeToRadianFactor),
                    Math.Tan(ySkewAngle * DegreeToRadianFactor),
                    0.0, 0.0);
                Tx += centerX;
                Ty += centerY;
                isChanged = true;

                SimpleScaleAndTranslateOnly = false;
                ScaleAndTransformOnly = false;
            }
        }

        #endregion

        #endregion

        #region private methods for transform

        #region multiply for stack
        /// <summary>
        /// Multiply current matrix to anther shear type matrix
        /// </summary>
        /// <param name="matrix">input matrix</param>
        private void SelfMultiply(double sx, double sy, double shx, double shy, double tx, double ty)
        {
            t0 = Sx * sx + Shy * shx;
            t2 = Shx * sx + Sy * shx;
            t4 = Tx * sx + Ty * shx + tx;

            t1 = Sx * shy + Shy * sy;
            t3 = Shx * shy + Sy * sy;
            t5 = Tx * shy + Ty * sy + ty;

            Sx = t0;
            Shx = t2;
            Tx = t4;

            Shy = t1;
            Sy = t3;
            Ty = t5;
        }

        /// <summary>
        /// Multiply current matrix to anther shear type matrix
        /// </summary>
        /// <param name="matrix">input matrix</param>
        private void PrependSelfMultiply(double sx, double sy, double shx, double shy, double tx, double ty)
        {
            t0 = sx * Sx + shy * Shx;
            t2 = shx * Sx + sy * Shx;
            t4 = tx * Sx + ty * Shx + Tx;

            t1 = sx * Shy + shy * Sy;
            t3 = shx * Shy + sy * Sy;
            t5 = tx * Shy + ty * Sy + Ty;

            Sx = t0;
            Shx = t2;
            Tx = t4;

            Shy = t1;
            Sy = t3;
            Ty = t5;
        }
        #endregion

        #region validate scale


        /// <summary>
        /// Check the scale factor and validate scale factor
        /// </summary>
        private void ValidateScale()
        {
            if (ScaleXFactor < 0)
            {
                ScaleXFactor = -ScaleXFactor;
            }
            if (ScaleYFactor < 0)
            {
                ScaleYFactor = -ScaleYFactor;
            }

            // when there are negative scale
            if ((Sx < 0) || (Sy < 0))
            {
                SimpleScaleAndTranslateOnly = false;
            }
            else // both are positive
            {
                if (ScaleAndTransformOnly)
                {
                    SimpleScaleAndTranslateOnly = true;
                }
            }
        }
        #endregion
        #endregion

        #region Reset
        /// <summary>
        /// Reset - load an identity matrix
        /// </summary>
        /// <returns></returns>
        public void Reset()
        {
            Sx = Sy = 1.0;
            Shy = Shx = Tx = Ty = 0.0;
            ScaleXFactor = ScaleYFactor = 1.0;
            //IsTransformed = false;
            isChanged = true;
        }
        #endregion

        #region Matrix operation
        #region Multiply
        /// <summary>
        /// Multiply current matrix to another one
        /// </summary>
        /// <param name="matrix">input matrix</param>
        public void Multiply(Matrix3x3 matrix)
        {
            t0 = Sx * matrix.Sx + Shy * matrix.Shx;
            t2 = Shx * matrix.Sx + Sy * matrix.Shx;
            t4 = Tx * matrix.Sx + Ty * matrix.Shx + matrix.Tx;

            t1 = Sx * matrix.Shy + Shy * matrix.Sy;
            t3 = Shx * matrix.Shy + Sy * matrix.Sy;
            t5 = Tx * matrix.Shy + Ty * matrix.Sy + matrix.Ty;

            Sx = t0;
            Shx = t2;
            Tx = t4;

            Shy = t1;
            Sy = t3;
            Ty = t5;

            ScaleXFactor *= matrix.ScaleXFactor;
            ScaleYFactor *= matrix.ScaleYFactor;

            //ScaleAndTranslateOnly true when in both matrix is true
            SimpleScaleAndTranslateOnly =
                SimpleScaleAndTranslateOnly && (matrix.SimpleScaleAndTranslateOnly);

            ScaleAndTransformOnly =
                ScaleAndTransformOnly && (matrix.ScaleAndTransformOnly);

            isChanged = true;
        }

        #endregion

        #region Invert
        /// <summary>
        /// Check if current matrix can invert or not
        /// </summary>
        /// <returns>true when can invert</returns>
        public bool CanInvert()
        {
            return ((Sx * Sy - Shy * Shx) != 0);
        }

        /// <summary>
        /// Invert matrix. Do not try to invert degenerate matrices, 
        /// there's no check for validity. If you set scale to 0 and 
        /// then try to invert matrix, expect unpredictable result.
        /// </summary>
        public void Invert()
        {
            double d = 1.0 / (Sx * Sy - Shy * Shx);

            t0 = Sy * d;
            Sy = Sx * d;
            Shy = -Shy * d;
            Shx = -Shx * d;

            t4 = -Tx * t0 - Ty * Shx;
            Ty = -Tx * Shy - Ty * Sy;

            Sx = t0;
            Tx = t4;

            ScaleXFactor = 1 / ScaleXFactor;
            ScaleYFactor = 1 / ScaleYFactor;
        }
        #endregion
        #endregion

        #region Transform
        /// <summary>
        /// Direct transformation of x and y.
        /// </summary>
        /// <param name="x">coordinate x of current point which want to transform</param>
        /// <param name="y">coordinate y of current point which want to transform</param>
        [Obsolete("Using direct code instead. And including /*DIRECT CODE*/ comment", true)]
        public void Transform(ref double x, ref double y)
        {
            double tmp = x;
            x = tmp * Sx + y * Shx + Tx;
            y = tmp * Shy + y * Sy + Ty;
        }

        /// <summary>
        /// Direct transformation of x and y.
        /// </summary>
        /// <param name="x">coordinate x of current point which want to transform</param>
        /// <param name="y">coordinate y of current point which want to transform</param>
        [Obsolete("Using direct code instead. And including /*DIRECT CODE*/ comment", true)]
        internal void TransformX(ref double x, double y)
        {
            x = x * Sx + y * Shx + Tx;
        }

        /// <summary>
        /// Direct transformation of x and y.
        /// </summary>
        /// <param name="x">coordinate x of current point which want to transform</param>
        /// <param name="y">coordinate y of current point which want to transform</param>
        [Obsolete("Using direct code instead. And including /*DIRECT CODE*/ comment", true)]
        internal void TransformY(double x, ref double y)
        {
            y = x * Shy + y * Sy + Ty; /*DIRECT CODE*/
        }
        #endregion

        #region Equals
        /// <summary>
        /// Compare current to other matrix
        /// </summary>
        /// <param name="obj">compared matrix</param>
        /// <returns>true when 2 matrix is equals</returns>
        public override bool Equals(object obj)
        {
            if (obj is Matrix3x3)
            {
                Matrix3x3 matrix = obj as Matrix3x3;
                return
                    (Math.Abs(Sx - matrix.Sx) <= AffineEpsilon) &&
                    (Math.Abs(Shy - matrix.Shy) <= AffineEpsilon) &&
                    (Math.Abs(Shx - matrix.Shx) <= AffineEpsilon) &&
                    (Math.Abs(Sy - matrix.Sy) <= AffineEpsilon) &&
                    (Math.Abs(Tx - matrix.Tx) <= AffineEpsilon) &&
                    (Math.Abs(Ty - matrix.Ty) <= AffineEpsilon);
            }
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion

        #region Product
        /// <summary>
        /// Product maxtrix from two matrix
        /// </summary>
        /// <param name="source">source matrix</param>
        /// <param name="dest">dest matrix</param>
        /// <returns></returns>
        public static Matrix3x3 operator *(Matrix3x3 source, Matrix3x3 dest)
        {
            Matrix3x3 result = new Matrix3x3(source);
            result.Multiply(dest);
            return result;
        }
        #endregion

        #region Clone
        /// <summary>
        /// Create an exact duplicate matrix
        /// </summary>
        public Matrix3x3 Clone()
        {
            return new Matrix3x3(this);
        }
        #endregion
    }
}

