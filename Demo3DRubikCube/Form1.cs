using System;
using System.Windows.Forms;
using Cross.Drawing.D3;

namespace Demo3DRubikCube
{
    public partial class Form1 : Form
    {
        //orientation
        //int cameraX = 0, cameraY = 0, cameraZ = 0, cubeX = 0, cubeY = 0, cubeZ = 0;

        RubikCube cub = new RubikCube(5, 100);
        Camera cam = new Camera();

        public Form1()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
        }

        Timer timer;
        int count = 0;
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // buffer = new Bitmap(Width, Height);
            cub.Center = new Point3D(400, 400, 0);
            cam.Location = new Point3D(400, 400, -1000);

            timer = new Timer();
            timer.Interval = 50;
            timer.Tick += (s, ee) =>
            {
                var index = count++ % 30 / 10;
                var q = new Quaternion(new Vector3D(index == 0 ? 1 : 0, index == 1 ? 1 : 0, index == 2 ? 1 : 0), 10);
                cub.RotateAt(cub.Center, q);
                Invalidate();
            };
            timer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            cub.Draw(e.Graphics, cam);
        }
    }
}
