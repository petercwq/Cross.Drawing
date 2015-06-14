using System;
using System.Drawing;
using Cross.Drawing.D3.Image;
using System.Linq;

namespace Cross.Drawing.D3
{
    public class Cuboid : Shape3d
    {
        public bool DrawingLine { get; set; }

        public bool FillingFace { get; set; }

        public bool DrawingImage { get; set; }

        Color[] faceColor = new Color[6] { Colors.Red, Colors.Orange, Colors.Yellow, Colors.Green, Colors.Blue, Colors.Purple };
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
            pts[0] = new Point3D(0, 0, 0);
            pts[1] = new Point3D(a, 0, 0);
            pts[2] = new Point3D(a, b, 0);
            pts[3] = new Point3D(0, b, 0);
            pts[4] = new Point3D(0, 0, c);
            pts[5] = new Point3D(a, 0, c);
            pts[6] = new Point3D(a, b, c);
            pts[7] = new Point3D(0, b, c);
        }
    }
}