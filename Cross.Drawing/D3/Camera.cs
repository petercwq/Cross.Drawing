
namespace Cross.Drawing.D3
{
    //orintation vector (0,0,-1)
    public class Camera
    {
        public Camera()
        {
            Location = new Point3D(0, 0, 0);
            FocalDistance = -500;
            Quaternion = new Quaternion(0, 0, 0, 1);
        }

        public Point3D Location { get; set; }

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
            var q = new Quaternion(new Vector3D(0, 0, 1), degree);
            Quaternion = q * Quaternion;
        }

        public void Yaw(int degree)  // rotate around Y axis
        {
            var q = new Quaternion(new Vector3D(0, 1, 0), degree);
            Quaternion = q * Quaternion;
        }

        public void Pitch(int degree) // rotate around X axis
        {
            var q = new Quaternion(new Vector3D(1, 0, 0), degree);
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
    }
}
