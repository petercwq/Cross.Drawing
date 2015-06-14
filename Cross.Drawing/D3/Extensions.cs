
namespace Cross.Drawing.D3
{
    public static class Extensions
    {
        public static Point3D[] Copy(this Point3D[] pts)
        {
            var copy = new Point3D[pts.Length];
            for (int i = 0; i < pts.Length; i++)
            {
                copy[i] = pts[i];
            }
            return copy;
        }

        public static void Offset(this Point3D[] pts, double offsetX, double offsetY, double offsetZ)
        {
            for (int i = 0; i < pts.Length; i++)
            {
                pts[i].Offset(offsetX, offsetY, offsetZ);
            }
        }

        //public static Point GetProjectedPoint(this Point3D pt, double d /* project distance: from eye to screen*/)
        //{
        //    return new Point(pt.X * d / (d + pt.Z), pt.Y * d / (d + pt.Z));
        //}

        //public static Point[] Project(this Point3D[] pts, double d /* project distance: from eye to screen*/)
        //{
        //    var pt2ds = new Point[pts.Length];
        //    for (int i = 0; i < pts.Length; i++)
        //    {
        //        pt2ds[i] = pts[i].GetProjectedPoint(d);
        //    }
        //    return pt2ds;
        //}

        // V'=q*V*q^-1
        public static void Rotate(this Quaternion q, ref Point3D pt)
        {
            q.Normalize();
            var q1 = q;
            q1.Conjugate();

            var qNode = new Quaternion(pt.X, pt.Y, pt.Z, 0);
            qNode = q * qNode * q1;
            pt.X = qNode.X;
            pt.Y = qNode.Y;
            pt.Z = qNode.Z;
        }

        public static void Rotate(this Quaternion q, Point3D[] nodes)
        {
            q.Normalize();
            var q1 = q;
            q1.Conjugate();
            for (int i = 0; i < nodes.Length; i++)
            {
                var qNode = new Quaternion(nodes[i].X, nodes[i].Y, nodes[i].Z, 0);
                qNode = q * qNode * q1;
                nodes[i].X = qNode.X;
                nodes[i].Y = qNode.Y;
                nodes[i].Z = qNode.Z;
            }
        }

        public static Point[] GetProjection(this Camera camera, Point3D[] pts)
        {
            var pt2ds = new Point[pts.Length];

            // transform to new coordinates system which origin is camera location
            var pts1 = pts.Copy();
            pts1.Offset(-camera.Location.X, -camera.Location.Y, -camera.Location.Z);

            // rotate
            camera.Quaternion.Rotate(pts1);

            //project
            for (int i = 0; i < pts.Length; i++)
            {
                if (pts1[i].Z > 0.1)
                {
                    pt2ds[i] = new Point(camera.Location.X + pts1[i].X * camera.FocalDistance / pts1[i].Z,
                        camera.Location.Y + pts1[i].Y * camera.FocalDistance / pts1[i].Z);
                }
                else
                {
                    pt2ds[i] = new Point(float.MaxValue, float.MaxValue);
                }
            }
            return pt2ds;
        }
    }
}
