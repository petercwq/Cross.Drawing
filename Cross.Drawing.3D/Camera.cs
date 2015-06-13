using System;
using System.Drawing;

namespace Cross.Drawing.D3
{
    //orintation vector (0,0,-1)
    public class Camera
    {
        public Camera()
        {
            Location = new Point3d(0, 0, 0);
            FocalDistance = -500;
            Quaternion = new Quaternion(1, 0, 0, 0);
        }

        public Point3d Location { get; set; }

        public double FocalDistance { get; set; }

        public Quaternion Quaternion { get; set; }

        public void MoveRight(double d)
        {
            Location.Offset(d, 0, 0);
        }

        public void MoveLeft(double d)
        {
            Location.Offset(-d, 0, 0);
        }

        public void MoveUp(double d)
        {
            Location.Offset(0, -d, 0);
        }

        public void MoveDown(double d)
        {
            Location.Offset(0, d, 0);
        }

        public void MoveIn(double d)
        {
            Location.Offset(0, 0, d);
        }

        public void MoveOut(double d)
        {
            Location.Offset(0, 0, -d);
        }

        public void Roll(int degree) // rotate around Z axis
        {
            var q = new Quaternion(new Vector3d(0, 0, 1), degree * Math.PI / 180.0);
            Quaternion = q * Quaternion;
        }

        public void Yaw(int degree)  // rotate around Y axis
        {
            var q = new Quaternion(new Vector3d(0, 1, 0), degree * Math.PI / 180.0);
            Quaternion = q * Quaternion;
        }

        public void Pitch(int degree) // rotate around X axis
        {
            var q = new Quaternion(new Vector3d(1, 0, 0), degree * Math.PI / 180.0);
            Quaternion = q * Quaternion;
        }

        public void TurnUp(int degree)
        {
            Pitch(-degree);
        }

        public void TurnDown(int degree)
        {
            Pitch(degree);
        }

        public void TurnLeft(int degree)
        {
            Yaw(degree);
        }

        public void TurnRight(int degree)
        {
            Yaw(-degree);
        }

        public PointF[] GetProjection(Point3d[] pts)
        {
            PointF[] pt2ds = new PointF[pts.Length];

            // transform to new coordinates system which origin is camera location
            Point3d[] pts1 = Point3d.Copy(pts);
            Point3d.Offset(pts1, -Location.X, -Location.Y, -Location.Z);

            // rotate
            Quaternion.Rotate(pts1);

            //project
            for (int i = 0; i < pts.Length; i++)
            {
                if (pts1[i].Z > 0.1)
                {
                    pt2ds[i] = new PointF((float)(Location.X + pts1[i].X * FocalDistance / pts1[i].Z),
                        (float)(Location.Y + pts1[i].Y * FocalDistance / pts1[i].Z));
                }
                else
                {
                    pt2ds[i] = new PointF(float.MaxValue, float.MaxValue);
                }
            }
            return pt2ds;
        }
    }
}
