using System.Drawing;

namespace Cross.Drawing.D3
{
    public class Shape3d
    {
        protected Point3D[] pts = new Point3D[8];
        public Point3D[] Point3DArray
        {
            get { return pts; }
        }

        protected Point3D center = new Point3D(0, 0, 0);
        public Point3D Center
        {
            set
            {
                double dx = value.X - center.X;
                double dy = value.Y - center.Y;
                double dz = value.Z - center.Z;
                pts.Offset(dx, dy, dz);
                center = value;
            }
            get { return center; }
        }

        protected Color lineColor = Colors.Black;
        public Color LineColor
        {
            set { lineColor = value; }
            get { return lineColor; }
        }

        public void MoveBy(Vector3D vector)
        {
            pts.Offset(vector.X, vector.Y, vector.Z);
            center.Offset(vector.X, vector.Y, vector.Z);
        }

        public void RotateAt(Point3D pt, Quaternion q)
        {
            // transform origin to pt
            Point3D[] copy = pts.Copy();
            copy.Offset(-pt.X, -pt.Y, -pt.Z);
            center.Offset(-pt.X, -pt.Y, -pt.Z);

            // rotate
            q.Rotate(copy);
            // TODO:???
            q.Rotate(ref center);

            // transform to original origin
            center.Offset(pt.X, pt.Y, pt.Z);
            copy.Offset(pt.X, pt.Y, pt.Z);
            pts = copy;
        }
    }
}
