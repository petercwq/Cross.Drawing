using System.Drawing;
using Cross.Drawing.D3;
using Demo3DCuboid;
using Point = Cross.Drawing.D3.Point;

namespace Demo3D
{
    public static class Extensions
    {
        //public static System.Drawing.Color ToSystemColor(this Cross.Drawing.Color color)
        //{
        //    return System.Drawing.Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue);
        //}

        public static PointF[] FromD3Points(Point[] pts)
        {
            var npts = new PointF[pts.Length];
            for (int i = 0; i < pts.Length; i++)
            {
                npts[i] = new PointF((float)pts[i].X, (float)pts[i].Y);
            }
            return npts;
        }

        public static void Draw(this Cuboid cube, Graphics g, Camera cam)
        {
            var pts2d = cam.GetProjection(cube.Points);//.Select<Point, PointF>(x => new PointF((float)(x.X), (float)(x.Y))).ToArray();

            Point[][] face = new Point[6][];
            face[0] = new Point[] { pts2d[0], pts2d[1], pts2d[2], pts2d[3] };
            face[1] = new Point[] { pts2d[5], pts2d[1], pts2d[0], pts2d[4] };
            face[2] = new Point[] { pts2d[1], pts2d[5], pts2d[6], pts2d[2] };
            face[3] = new Point[] { pts2d[2], pts2d[6], pts2d[7], pts2d[3] };
            face[4] = new Point[] { pts2d[3], pts2d[7], pts2d[4], pts2d[0] };
            face[5] = new Point[] { pts2d[4], pts2d[7], pts2d[6], pts2d[5] };

            for (int i = 0; i < 6; i++)
            {
                bool isout = false;
                for (int j = 0; j < 4; j++)
                {
                    if (face[i][j] == new Point(float.MaxValue, float.MaxValue))
                    {
                        isout = true;
                        continue;
                    }
                }
                if (!isout)
                {
                    if (cube.DrawingLine)
                        g.DrawPolygon(new Pen(Color.Blue, 2.0f), FromD3Points(face[i]));
                    if (Vector.IsClockwise(face[i][0], face[i][1], face[i][2])) // the face can be seen by camera
                    {
                        if (cube.FillingFace) g.FillPolygon(new SolidBrush(Color.FromArgb((int)(cube.FaceColors[i]))), FromD3Points(face[i]));
                        if (cube.DrawingImage && cube.FaceImages[i] != null)
                        {
                            cube.filters[i].FourCorners = FromD3Points(face[i]);
                            g.DrawImage(cube.filters[i].Bitmap, (float)cube.filters[i].ImageLocation.X, (float)cube.filters[i].ImageLocation.Y);
                        }
                    }
                }
            }
        }

        public static void Draw(this RubikCube2 cube, Graphics g, Camera cam)
        {
            var pts2d = cam.GetProjection(cube.Points);//.Select<Point, PointF>(x => new PointF((float)(x.X), (float)(x.Y))).ToArray();

            Point[][] face = new Point[6][];
            face[0] = new Point[] { pts2d[0], pts2d[1], pts2d[2], pts2d[3] };
            face[1] = new Point[] { pts2d[5], pts2d[1], pts2d[0], pts2d[4] };
            face[2] = new Point[] { pts2d[1], pts2d[5], pts2d[6], pts2d[2] };
            face[3] = new Point[] { pts2d[2], pts2d[6], pts2d[7], pts2d[3] };
            face[4] = new Point[] { pts2d[3], pts2d[7], pts2d[4], pts2d[0] };
            face[5] = new Point[] { pts2d[4], pts2d[7], pts2d[6], pts2d[5] };

            for (int i = 0; i < 6; i++)
            {
                bool isout = false;
                for (int j = 0; j < 4; j++)
                {
                    if (face[i][j] == new Point(float.MaxValue, float.MaxValue))
                    {
                        isout = true;
                        continue;
                    }
                }
                if (!isout)
                {
                    if (cube.DrawingLine)
                        g.DrawPolygon(new Pen(Color.Blue, 2.0f), FromD3Points(face[i]));
                    if (Vector.IsClockwise(face[i][0], face[i][1], face[i][2])) // the face can be seen by camera
                    {
                        if (cube.FillingFace)
                            g.FillPolygon(new SolidBrush(Color.FromArgb((int)(cube.FaceColors[i]))), FromD3Points(face[i]));
                    }
                }
            }
        }
    }
}
