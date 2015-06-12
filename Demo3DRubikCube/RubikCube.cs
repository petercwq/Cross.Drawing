using System;
using System.Drawing;
using Cross.Drawing.D3;

namespace Demo3DRubikCube
{
    class RubikCube
    {
        // Front, Back, Left, Right, Up, Down
        static readonly Color edgeColor = Color.DarkGray;
        static readonly Color[] FaceColors = new Color[6] { Color.Red, Color.Yellow, Color.Green, Color.Cyan, Color.Blue, Color.Magenta };
        readonly Cuboid[, ,] cuboids;
        readonly int rank;

        private Point3d center;
        public Point3d Center
        {
            set
            {
                double dx = value.X - center.X;
                double dy = value.Y - center.Y;
                double dz = value.Z - center.Z;
                var vector = new Vector3d(dx, dy, dz);
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
                cub.MoveBy(new Vector3d(i * edge - temp, j * edge - temp, k * edge - temp));
                cub.DrawingLine = true;
                cub.LineColor = edgeColor;
                cub.FillingFace = false;
                cuboids[i, j, k] = cub;
            });
            // default center is 0,0,0
            center = new Point3d(0, 0, 0);
        }

        public void RotateAt(Point3d pt, Quaternion q)
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
