
namespace Cross.Drawing.D3
{
    public abstract class Shape3D
    {
        public abstract Point3D[] Points { get; protected set; }

        protected Point3D center = new Point3D(0, 0, 0);
        public Point3D Center
        {
            set
            {
                double dx = value.X - center.X;
                double dy = value.Y - center.Y;
                double dz = value.Z - center.Z;
                Points.Offset(dx, dy, dz);
                center = value;
            }
            get { return center; }
        }

        public void MoveBy(double x, double y, double z)
        {
            Points.Offset(x, y, z);
            center.Offset(x, y, z);
        }

        public void RotateAt(Point3D pt, Quaternion q)
        {
            // transform origin to pt
            var copy = Points.Copy();
            copy.Offset(-pt.X, -pt.Y, -pt.Z);
            center.Offset(-pt.X, -pt.Y, -pt.Z);

            // rotate
            q.Rotate(copy);
            // TODO:???
            q.Rotate(ref center);

            // transform to original origin
            center.Offset(pt.X, pt.Y, pt.Z);
            copy.Offset(pt.X, pt.Y, pt.Z);
            Points = copy;
        }
    }
}
