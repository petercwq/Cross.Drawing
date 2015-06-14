using System;
using System.Drawing;
using Cross.Drawing.D3.Image;

namespace Cross.Drawing.D3
{
    public class Cuboid : Shape3D
    {
        public bool DrawingLine { get; set; }
        public bool FillingFace { get; set; }
        public bool DrawingImage { get; set; }

        uint[] faceColor = new uint[6] { Argbs.Red, Argbs.Orange, Argbs.Yellow, Argbs.Green, Argbs.Blue, Argbs.Purple };
        public uint[] FaceColors
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
        public Bitmap[] FaceImages
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

        internal FreeTransform[] filters = new FreeTransform[6];
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

            center = new Point3D(a / 2, b / 2, c / 2);
            points[0] = new Point3D(0, 0, 0);
            points[1] = new Point3D(a, 0, 0);
            points[2] = new Point3D(a, b, 0);
            points[3] = new Point3D(0, b, 0);
            points[4] = new Point3D(0, 0, c);
            points[5] = new Point3D(a, 0, c);
            points[6] = new Point3D(a, b, c);
            points[7] = new Point3D(0, b, c);
        }

        private Point3D[] points = new Point3D[8];
        public override Point3D[] Points
        {
            get
            {
                return points;
            }
            protected set
            {
                points = value;
            }
        }
    }
}