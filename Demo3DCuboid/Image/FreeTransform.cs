using System;
using System.Drawing;

namespace Cross.Drawing.D3.Image
{
    using Point = System.Drawing.Point;

    public class FreeTransform
    {
        PointF[] vertex = new PointF[4];
        Vector AB, BC, CD, DA;
        Rectangle rect = new Rectangle();
        ImageData srcCB = new ImageData();
        int srcW = 0;
        int srcH = 0;

        public Bitmap Bitmap
        {
            set
            {
                try
                {
                    srcCB.FromBitmap(value);
                    srcH = value.Height;
                    srcW = value.Width;
                }
                catch
                {
                    srcW = 0; srcH = 0;
                }
            }
            get
            {
                return getTransformedBitmap();
            }
        }

        public Point ImageLocation
        {
            set { rect.Location = value; }
            get { return rect.Location; }
        }

        bool isBilinear = false;
        public bool IsBilinearInterpolation
        {
            set { isBilinear = value; }
            get { return isBilinear; }
        }

        public int ImageWidth
        {
            get { return rect.Width; }
        }

        public int ImageHeight
        {
            get { return rect.Height; }
        }

        public PointF VertexLeftTop
        {
            set { vertex[0] = value; setVertex(); }
            get { return vertex[0]; }
        }

        public PointF VertexTopRight
        {
            set { vertex[1] = value; setVertex(); }
            get { return vertex[1]; }
        }

        public PointF VertexRightBottom
        {
            set { vertex[2] = value; setVertex(); }
            get { return vertex[2]; }
        }

        public PointF VertexBottomLeft
        {
            set { vertex[3] = value; setVertex(); }
            get { return vertex[3]; }
        }

        public PointF[] FourCorners
        {
            set { vertex = value; setVertex(); }
            get { return vertex; }
        }

        private void setVertex()
        {
            float xmin = float.MaxValue;
            float ymin = float.MaxValue;
            float xmax = float.MinValue;
            float ymax = float.MinValue;

            for (int i = 0; i < 4; i++)
            {
                xmax = Math.Max(xmax, vertex[i].X);
                ymax = Math.Max(ymax, vertex[i].Y);
                xmin = Math.Min(xmin, vertex[i].X);
                ymin = Math.Min(ymin, vertex[i].Y);
            }

            rect = new Rectangle((int)xmin, (int)ymin, (int)(xmax - xmin + 2), (int)(ymax - ymin + 2));

            AB = new Vector(vertex[1].X - vertex[0].X, vertex[1].Y - vertex[0].Y);
            BC = new Vector(vertex[2].X - vertex[1].X, vertex[2].Y - vertex[1].Y);
            CD = new Vector(vertex[3].X - vertex[2].X, vertex[3].Y - vertex[2].Y);
            DA = new Vector(vertex[0].X - vertex[3].X, vertex[0].Y - vertex[3].Y);

            // get unit vector
            AB.Normalize();
            BC.Normalize();
            CD.Normalize();
            DA.Normalize();
        }

        private Cross.Drawing.D3.Point ToPoint(ref PointF pt)
        {
            return new D3.Point(pt.X, pt.Y);
        }

        private bool isOnPlaneABCD(PointF pt) //  including point on border
        {
            if (!Vector.IsCCW(ToPoint(ref pt), ToPoint(ref vertex[0]), ToPoint(ref vertex[1])))
            {
                if (!Vector.IsCCW(ToPoint(ref pt), ToPoint(ref vertex[1]), ToPoint(ref vertex[2])))
                {
                    if (!Vector.IsCCW(ToPoint(ref pt), ToPoint(ref vertex[2]), ToPoint(ref vertex[3])))
                    {
                        if (!Vector.IsCCW(ToPoint(ref pt), ToPoint(ref vertex[3]), ToPoint(ref vertex[0])))
                            return true;
                    }
                }
            }
            return false;
        }

        private Bitmap getTransformedBitmap()
        {
            if (srcH == 0 || srcW == 0) return null;

            ImageData destCB = new ImageData();
            destCB.A = new byte[rect.Width, rect.Height];
            destCB.B = new byte[rect.Width, rect.Height];
            destCB.G = new byte[rect.Width, rect.Height];
            destCB.R = new byte[rect.Width, rect.Height];


            PointF ptInPlane = new PointF();
            int x1, x2, y1, y2;
            double dab, dbc, dcd, dda;
            float dx1, dx2, dy1, dy2, dx1y1, dx1y2, dx2y1, dx2y2, nbyte;

            for (int y = 0; y < rect.Height; y++)
            {
                for (int x = 0; x < rect.Width; x++)
                {
                    Point srcPt = new Point(x, y);
                    srcPt.Offset(this.rect.Location);

                    if (isOnPlaneABCD(srcPt))
                    {
                        dab = Math.Abs(Vector.CrossProduct(new Vector(srcPt.X - vertex[0].X, srcPt.Y - vertex[0].Y), AB));
                        dbc = Math.Abs(Vector.CrossProduct(new Vector(srcPt.X - vertex[1].X, srcPt.Y - vertex[1].Y), BC));
                        dcd = Math.Abs(Vector.CrossProduct(new Vector(srcPt.X - vertex[2].X, srcPt.Y - vertex[2].Y), CD));
                        dda = Math.Abs(Vector.CrossProduct(new Vector(srcPt.X - vertex[3].X, srcPt.Y - vertex[3].Y), DA));
                        ptInPlane.X = (float)(srcW * (dda / (dda + dbc)));
                        ptInPlane.Y = (float)(srcH * (dab / (dab + dcd)));

                        x1 = (int)ptInPlane.X;
                        y1 = (int)ptInPlane.Y;

                        if (x1 >= 0 && x1 < srcW && y1 >= 0 && y1 < srcH)
                        {
                            if (isBilinear)
                            {
                                x2 = (x1 == srcW - 1) ? x1 : x1 + 1;
                                y2 = (y1 == srcH - 1) ? y1 : y1 + 1;

                                dx1 = ptInPlane.X - (float)x1;
                                if (dx1 < 0) dx1 = 0;
                                dx1 = 1f - dx1;
                                dx2 = 1f - dx1;
                                dy1 = ptInPlane.Y - (float)y1;
                                if (dy1 < 0) dy1 = 0;
                                dy1 = 1f - dy1;
                                dy2 = 1f - dy1;

                                dx1y1 = dx1 * dy1;
                                dx1y2 = dx1 * dy2;
                                dx2y1 = dx2 * dy1;
                                dx2y2 = dx2 * dy2;


                                nbyte = srcCB.A[x1, y1] * dx1y1 + srcCB.A[x2, y1] * dx2y1 + srcCB.A[x1, y2] * dx1y2 + srcCB.A[x2, y2] * dx2y2;
                                destCB.A[x, y] = (byte)nbyte;
                                nbyte = srcCB.B[x1, y1] * dx1y1 + srcCB.B[x2, y1] * dx2y1 + srcCB.B[x1, y2] * dx1y2 + srcCB.B[x2, y2] * dx2y2;
                                destCB.B[x, y] = (byte)nbyte;
                                nbyte = srcCB.G[x1, y1] * dx1y1 + srcCB.G[x2, y1] * dx2y1 + srcCB.G[x1, y2] * dx1y2 + srcCB.G[x2, y2] * dx2y2;
                                destCB.G[x, y] = (byte)nbyte;
                                nbyte = srcCB.R[x1, y1] * dx1y1 + srcCB.R[x2, y1] * dx2y1 + srcCB.R[x1, y2] * dx1y2 + srcCB.R[x2, y2] * dx2y2;
                                destCB.R[x, y] = (byte)nbyte;
                            }
                            else
                            {
                                destCB.A[x, y] = srcCB.A[x1, y1];
                                destCB.B[x, y] = srcCB.B[x1, y1];
                                destCB.G[x, y] = srcCB.G[x1, y1];
                                destCB.R[x, y] = srcCB.R[x1, y1];
                            }
                        }
                    }
                }
            }
            return destCB.ToBitmap();
        }
    }
}