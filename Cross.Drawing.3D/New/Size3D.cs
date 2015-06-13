using System;

namespace Cross.Drawing._3D.New
{
    /// <summary>
    /// Size3D - A value type which defined a size in terms of non-negative width,
    /// length, and height.
    /// </summary>
    public struct Size3D
    {
        /// <summary>
        /// Constructor which sets the size's initial values.  Values must be non-negative.
        /// </summary>
        /// <param name="x">X dimension of the new size.</param>
        /// <param name="y">Y dimension of the new size.</param>
        /// <param name="z">Z dimension of the new size.</param>
        public Size3D(double x, double y, double z)
        {
            if (x < 0 || y < 0 || z < 0)
            {
                throw new System.ArgumentException("Size3D Dimension Cannot Be Negative");
            }
            _x = x;
            _y = y;
            _z = z;
        }

        /// <summary>
        /// Empty - a static property which provides an Empty size.  X, Y, and Z are
        /// negative-infinity.  This is the only situation
        /// where size can be negative.
        /// </summary>
        public static Size3D Empty
        {
            get
            {
                return s_empty;
            }
        }

        /// <summary>
        /// IsEmpty - this returns true if this size is the Empty size.
        /// Note: If size is 0 this Size3D still contains a 0, 1, or 2 dimensional set
        /// of points, so this method should not be used to check for 0 volume.
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return _x < 0;
            }
        }

        /// <summary>
        /// Size in X dimension. Default is 0, must be non-negative.
        /// </summary>
        public double X
        {
            get
            {
                return _x;
            }
            set
            {
                if (IsEmpty)
                {
                    throw new System.InvalidOperationException("Size3D_CannotModifyEmptySize");
                }

                if (value < 0)
                {
                    throw new System.ArgumentException("Size3D_DimensionCannotBeNegative");
                }

                _x = value;
            }
        }

        /// <summary>
        /// Size in Y dimension. Default is 0, must be non-negative.
        /// </summary>
        public double Y
        {
            get
            {
                return _y;
            }
            set
            {
                if (IsEmpty)
                {
                    throw new System.InvalidOperationException("Size3D_CannotModifyEmptySize");
                }

                if (value < 0)
                {
                    throw new System.ArgumentException("Size3D_DimensionCannotBeNegative");
                }

                _y = value;
            }
        }

        /// <summary>
        /// Size in Z dimension. Default is 0, must be non-negative.
        /// </summary>
        public double Z
        {
            get
            {
                return _z;
            }
            set
            {
                if (IsEmpty)
                {
                    throw new System.InvalidOperationException("Size3D_CannotModifyEmptySize");
                }

                if (value < 0)
                {
                    throw new System.ArgumentException("Size3D_DimensionCannotBeNegative");
                }

                _z = value;
            }
        }

        /// <summary>
        /// Explicit conversion to Vector.
        /// </summary>
        /// <param name="size">The size to convert to a vector.</param>
        /// <returns>A vector equal to this size.</returns>
        public static explicit operator Vector3D(Size3D size)
        {
            return new Vector3D(size._x, size._y, size._z);
        }

        /// <summary>
        /// Explicit conversion to point.
        /// </summary>
        /// <param name="size">The size to convert to a point.</param>
        /// <returns>A point equal to this size.</returns>
        public static explicit operator Point3D(Size3D size)
        {
            return new Point3D(size._x, size._y, size._z);
        }

        private static Size3D CreateEmptySize3D()
        {
            Size3D empty = new Size3D();
            // Can't use setters because they throw on negative values
            empty._x = Double.NegativeInfinity;
            empty._y = Double.NegativeInfinity;
            empty._z = Double.NegativeInfinity;
            return empty;
        }

        private readonly static Size3D s_empty = CreateEmptySize3D();

        /// <summary>
        /// Compares two Size3D instances for exact equality.
        /// Note that double values can acquire error when operated upon, such that
        /// an exact comparison between two values which are logically equal may fail.
        /// Furthermore, using this equality operator, Double.NaN is not equal to itself.
        /// </summary>
        /// <returns>
        /// bool - true if the two Size3D instances are exactly equal, false otherwise
        /// </returns>
        /// <param name='size1'>The first Size3D to compare</param>
        /// <param name='size2'>The second Size3D to compare</param>
        public static bool operator ==(Size3D size1, Size3D size2)
        {
            return size1.X == size2.X &&
            size1.Y == size2.Y &&
            size1.Z == size2.Z;
        }

        /// <summary>
        /// Compares two Size3D instances for exact inequality.
        /// Note that double values can acquire error when operated upon, such that
        /// an exact comparison between two values which are logically equal may fail.
        /// Furthermore, using this equality operator, Double.NaN is not equal to itself.
        /// </summary>
        /// <returns>
        /// bool - true if the two Size3D instances are exactly unequal, false otherwise
        /// </returns>
        /// <param name='size1'>The first Size3D to compare</param>
        /// <param name='size2'>The second Size3D to compare</param>
        public static bool operator !=(Size3D size1, Size3D size2)
        {
            return !(size1 == size2);
        }
        /// <summary>
        /// Compares two Size3D instances for object equality.  In this equality
        /// Double.NaN is equal to itself, unlike in numeric equality.
        /// Note that double values can acquire error when operated upon, such that
        /// an exact comparison between two values which
        /// are logically equal may fail.
        /// </summary>
        /// <returns>
        /// bool - true if the two Size3D instances are exactly equal, false otherwise
        /// </returns>
        /// <param name='size1'>The first Size3D to compare</param>
        /// <param name='size2'>The second Size3D to compare</param>
        public static bool Equals(Size3D size1, Size3D size2)
        {
            if (size1.IsEmpty)
            {
                return size2.IsEmpty;
            }
            else
            {
                return size1.X.Equals(size2.X) &&
                size1.Y.Equals(size2.Y) &&
                size1.Z.Equals(size2.Z);
            }
        }

        /// <summary>
        /// Equals - compares this Size3D with the passed in object.  In this equality
        /// Double.NaN is equal to itself, unlike in numeric equality.
        /// Note that double values can acquire error when operated upon, such that
        /// an exact comparison between two values which
        /// are logically equal may fail.
        /// </summary>
        /// <returns>
        /// bool - true if the object is an instance of Size3D and if it's equal to "this".
        /// </returns>
        /// <param name='o'>The object to compare to "this"</param>
        public override bool Equals(object o)
        {
            if ((null == o) || !(o is Size3D))
            {
                return false;
            }

            Size3D value = (Size3D)o;
            return Size3D.Equals(this, value);
        }

        /// <summary>
        /// Equals - compares this Size3D with the passed in object.  In this equality
        /// Double.NaN is equal to itself, unlike in numeric equality.
        /// Note that double values can acquire error when operated upon, such that
        /// an exact comparison between two values which
        /// are logically equal may fail.
        /// </summary>
        /// <returns>
        /// bool - true if "value" is equal to "this".
        /// </returns>
        /// <param name='value'>The Size3D to compare to "this"</param>
        public bool Equals(Size3D value)
        {
            return Size3D.Equals(this, value);
        }
        /// <summary>
        /// Returns the HashCode for this Size3D
        /// </summary>
        /// <returns>
        /// int - the HashCode for this Size3D
        /// </returns>
        public override int GetHashCode()
        {
            if (IsEmpty)
            {
                return 0;
            }
            else
            {
                // Perform field-by-field XOR of HashCodes
                return X.GetHashCode() ^
                Y.GetHashCode() ^
                Z.GetHashCode();
            }
        }

        /// <summary>
        /// Creates a string representation of this object based on the current culture.
        /// </summary>
        /// <returns>
        /// A string representation of this object.
        /// </returns>
        public override string ToString()
        {
            return string.Format("{0},{1},{2}", _x, _y, _z);
        }

        internal double _x;
        internal double _y;
        internal double _z;
    }
}

