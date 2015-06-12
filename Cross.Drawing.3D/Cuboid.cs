using System;
using System.Drawing;
using Cross.Drawing.D3.Image;

namespace Cross.Drawing.D3
{
    public class Cuboid : Shape3d
    {
        public bool DrawingLine { get; set; }

        public bool FillingFace { get; set; }

        public bool DrawingImage { get; set; }

        Color[] faceColor = new Color[6] { Color.Red, Color.Orange, Color.Yellow, Color.Green, Color.Blue, Color.Purple };
        public Color[] FaceColorArray
        {
            set
            {
                int n = Math.Min(value.Length, faceColor.Length);
                for (int i = 0; i < n; i++)
                    faceColor[i] = value[i];
            }
            get { return faceColor; }
        }

        Bitmap[] bmp = new Bitmap[6];
        public Bitmap[] FaceImageArray
        {
            set
            {
                int n = Math.Min(value.Length, 6);
                for (int i = 0; i < n; i++)
                    bmp[i] = value[i];
                setupFilter();
            }
            get { return bmp; }
        }

        FreeTransform[] filters = new FreeTransform[6];
        private void setupFilter()
        {
            for (int i = 0; i < 6; i++)
            {
                filters[i] = new FreeTransform();
                filters[i].Bitmap = bmp[i];
            }
        }

        public Cuboid(double a, double b, double c)
        {
            DrawingLine = false;
            FillingFace = true;
            DrawingImage = false;

            center = new Point3d(a / 2, b / 2, c / 2);
            pts[0] = new Point3d(0, 0, 0);
            pts[1] = new Point3d(a, 0, 0);
            pts[2] = new Point3d(a, b, 0);
            pts[3] = new Point3d(0, b, 0);
            pts[4] = new Point3d(0, 0, c);
            pts[5] = new Point3d(a, 0, c);
            pts[6] = new Point3d(a, b, c);
            pts[7] = new Point3d(0, b, c);
        }

        public override void Draw(Graphics g, Camera cam)
        {
            PointF[] pts2d = cam.GetProjection(pts);

            PointF[][] face = new PointF[6][];
            face[0] = new PointF[] { pts2d[0], pts2d[1], pts2d[2], pts2d[3] };
            face[1] = new PointF[] { pts2d[5], pts2d[1], pts2d[0], pts2d[4] };
            face[2] = new PointF[] { pts2d[1], pts2d[5], pts2d[6], pts2d[2] };
            face[3] = new PointF[] { pts2d[2], pts2d[6], pts2d[7], pts2d[3] };
            face[4] = new PointF[] { pts2d[3], pts2d[7], pts2d[4], pts2d[0] };
            face[5] = new PointF[] { pts2d[4], pts2d[7], pts2d[6], pts2d[5] };

            for (int i = 0; i < 6; i++)
            {
                bool isout = false;
                for (int j = 0; j < 4; j++)
                {
                    if (face[i][j] == new PointF(float.MaxValue, float.MaxValue))
                    {
                        isout = true;
                        continue;
                    }
                }
                if (!isout)
                {
                    if (DrawingLine)
                        g.DrawPolygon(new Pen(lineColor, 2.0f), face[i]);
                    if (Vector.IsClockwise(face[i][0], face[i][1], face[i][2])) // the face can be seen by camera
                    {
                        if (FillingFace) g.FillPolygon(new SolidBrush(faceColor[i]), face[i]);
                        if (DrawingImage && bmp[i] != null)
                        {
                            filters[i].FourCorners = face[i];
                            g.DrawImage(filters[i].Bitmap, filters[i].ImageLocation);
                        }
                    }
                }
            }
        }
    }
}