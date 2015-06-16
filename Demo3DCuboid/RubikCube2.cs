using Cross.Drawing;
using Cross.Drawing.D3;

namespace Demo3DCuboid
{
   public class RubikCube2 : Shape3D
    {
        // Front, Back, Right, Left, Up, Down
        enum Faces : byte
        {
            Front = 0,
            Back = 1,
            Right = 2,
            Left = 3,
            Up = 4,
            Down = 5
        }

        enum PieceType : byte
        {
            Center = 1,
            Edge = 2,
            Corner = 3
        }

        static readonly uint[] faceColors = new uint[6] 
        {
            Argbs.Blue,
            Argbs.Green,
            Argbs.Red,
            Argbs.Orange,
            Argbs.Yellow,
            Argbs.White
        };

        public bool DrawingLine { get; set; }
        public bool FillingFace { get; set; }

        public readonly int Rank;

        public uint[] FaceColors
        {
            get { return faceColors; }
        }

        public RubikCube2(double a, double b, double c, int rank)
        {
            DrawingLine = true;
            FillingFace = true;
            Rank = rank;

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
