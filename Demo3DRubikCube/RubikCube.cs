using System;
using System.Drawing;
using Cross.Drawing;
using Cross.Drawing.D3;
using Color = Cross.Drawing.Color;
using Demo3DCuboid;

namespace Demo3DRubikCube
{
    class RubikCube
    {
        // Front, Back, Left, Right, Up, Down
        static readonly Color edgeColor = Colors.DarkGray;
        static readonly Color[] FaceColors = new Color[6] { Colors.Red, Colors.Yellow, Colors.Green, Colors.Cyan, Colors.Blue, Colors.Magenta };
        readonly Cuboid[, ,] cuboids;
        readonly int rank;

        private Point3D center;
        public Point3D Center
        {
            set
            {
                double dx = value.X - center.X;
                double dy = value.Y - center.Y;
                double dz = value.Z - center.Z;
                var vector = new Vector3D(dx, dy, dz);
                foreach (var cuboid in cuboids)
                {
                    cuboid.MoveBy(vector);
                }
                center = value;
            }
            get { return center; }
        }

        public RubikCube(int rank_num = 3, int edge = 150)
        {
            rank = rank_num;
            double temp = rank_num * edge / 2.0;
            // initial cuboids
            cuboids = new Cuboid[rank, rank, rank];
            ForEach((i, j, k) =>
            {
                var cub = new Cuboid(edge, edge, edge);
                cub.MoveBy(new Vector3D(i * edge - temp, j * edge - temp, k * edge - temp));
                cub.DrawingLine = true;
                cub.LineColor = edgeColor;
                cub.FillingFace = false;
                cuboids[i, j, k] = cub;
            });
            // default center is 0,0,0
            center = new Point3D(0, 0, 0);
        }

        public void RotateAt(Point3D pt, Quaternion q)
        {
            foreach (var cuboid in cuboids)
            {
                cuboid.RotateAt(pt, q);
            }
        }

        private void ForEach(Action<int, int, int> action)
        {
            for (int i = 0; i < rank; i++)
                for (int j = 0; j < rank; j++)
                    for (int k = 0; k < rank; k++)
                    {
                        action(i, j, k);
                    }
        }

        public void Draw(Graphics g, Camera cam)
        {
            foreach (var cuboid in cuboids)
            {
                cuboid.Draw(g, cam);
            }
        }
    }
}
