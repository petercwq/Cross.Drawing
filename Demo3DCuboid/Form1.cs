using System;
using System.Drawing;
using System.Windows.Forms;
using Cross.Drawing.D3;

namespace Demo3DCuboid
{
    public partial class Form1 : Form
    {
        private Timer timer;

        public Form1()
        {
            InitializeComponent();
        }

        //orientation
        int cameraX = 0, cameraY = 0, cameraZ = 0, cubeX = 0, cubeY = 0, cubeZ = 0;

        Cuboid cub = new Cuboid(150, 150, 150);
        Camera cam = new Camera();
        Bitmap buffer;

        int count = 0;
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // buffer = new Bitmap(Width, Height);
            cub.Center = new Point3d(400, 240, 0);
            cam.Location = new Point3d(400, 240, -500);
            ReDraw();

            timer = new Timer();
            timer.Interval = 20;
            timer.Tick += (s, ee) =>
            {
                //var rand = new Random(DateTime.Now.Millisecond);
                //var x = 10;// rand.Next(10);
                //var y = 10;// rand.Next(10);
                //var z = 10;// rand.Next(10);
                int x = 0, y = 0, z = 0;
                switch (count++ % 30 / 10)
                {
                    case 0:
                        cubeX += 10;
                        labelCrX.Text = cubeX.ToString();
                        x = 1;
                        break;
                    case 1:
                        cubeZ += 10;
                        labelCrZ.Text = cubeZ.ToString();
                        y = 1;
                        break;
                    case 2:
                        cubeY += 10;
                        labelCrY.Text = cubeY.ToString();
                        z = 1;
                        break;
                    default:
                        break;
                }

                //var q = new Quaternion(new Vector3d(1, 0, 0), x * Math.PI / 180.0) *
                //             new Quaternion(new Vector3d(0, 0, 1), y * Math.PI / 180.0) *
                //            new Quaternion(new Vector3d(0, 1, 0), z * Math.PI / 180.0);
                var q = new Quaternion(new Vector3d(x, y, z), 10 * Math.PI / 180.0);
                cub.RotateAt(cub.Center, q);
                ReDraw();
            };
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (buffer != null)
                buffer.Dispose();
            buffer = new Bitmap(Width, Height);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            cub.Draw(e.Graphics, cam);
            //e.Graphics.DrawImage(buffer, 0, 0);
        }

        public void ReDraw()
        {
            Invalidate();
        }

        //bool drawing = false;
        //public void ReDraw()
        //{
        //    if (!drawing)
        //    {
        //        drawing = true;
        //        ////start an async task
        //        //Task.Factory.StartNew(() =>
        //        //{
        //            using (Graphics g = Graphics.FromImage(buffer))
        //            {
        //                g.Clear(Color.Transparent);
        //                g.CompositingQuality = CompositingQuality.HighQuality;
        //                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        //                cub.Draw(g, cam);
        //            }
        //            this.Invalidate();
        //        //});
        //        drawing = false;
        //    }
        //}

        private void button12_Click(object sender, EventArgs e)
        {
            cubeX += 5;
            labelCrX.Text = cubeX.ToString();
            Quaternion q = new Quaternion();
            q.FromAxisAngle(new Vector3d(1, 0, 0), 5 * Math.PI / 180.0);
            cub.RotateAt(cub.Center, q);
            ReDraw();
        }

        private void button11_Click(object sender, EventArgs e)
        {
            cubeX -= 5;
            labelCrX.Text = cubeX.ToString();
            Quaternion q = new Quaternion();
            q.FromAxisAngle(new Vector3d(1, 0, 0), -5 * Math.PI / 180.0);
            cub.RotateAt(cub.Center, q);
            ReDraw();
        }

        private void button10_Click(object sender, EventArgs e)
        {
            cubeY += 5;
            labelCrY.Text = cubeY.ToString();
            Quaternion q = new Quaternion();
            q.FromAxisAngle(new Vector3d(0, 1, 0), 5 * Math.PI / 180.0);
            cub.RotateAt(cub.Center, q);
            ReDraw();
        }

        private void button9_Click(object sender, EventArgs e)
        {
            cubeY -= 5;
            labelCrY.Text = cubeY.ToString();
            Quaternion q = new Quaternion();
            q.FromAxisAngle(new Vector3d(0, 1, 0), -5 * Math.PI / 180.0);
            cub.RotateAt(cub.Center, q);
            ReDraw();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            cubeZ += 5;
            labelCrZ.Text = cubeZ.ToString();
            Quaternion q = new Quaternion();
            q.FromAxisAngle(new Vector3d(0, 0, 1), 5 * Math.PI / 180.0);
            cub.RotateAt(cub.Center, q);
            ReDraw();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            cubeZ -= 5;
            labelCrZ.Text = cubeZ.ToString();
            Quaternion q = new Quaternion();
            q.FromAxisAngle(new Vector3d(0, 0, 1), -5 * Math.PI / 180.0);
            cub.RotateAt(cub.Center, q);
            ReDraw();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            cam.MoveLeft(10);
            ReDraw();
            labelMx.Text = cam.Location.X.ToString();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            cam.MoveRight(10);
            ReDraw();
            labelMx.Text = cam.Location.X.ToString();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            cam.MoveUp(10);
            ReDraw();
            labelMy.Text = cam.Location.Y.ToString();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            cam.MoveDown(10);
            ReDraw();
            labelMy.Text = cam.Location.Y.ToString();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            cam.MoveIn(10);
            ReDraw();
            labelMz.Text = cam.Location.Z.ToString();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            cam.MoveOut(10);
            ReDraw();
            labelMz.Text = cam.Location.Z.ToString();
        }

        private void button18_Click(object sender, EventArgs e)
        {
            cameraY -= 1;
            labelMrY.Text = cameraY.ToString();
            cam.TurnLeft(1);
            ReDraw();
        }

        private void button17_Click(object sender, EventArgs e)
        {
            cameraY += 1;
            labelMrY.Text = cameraY.ToString();
            cam.TurnRight(1);
            ReDraw();
        }

        private void button16_Click(object sender, EventArgs e)
        {
            cameraX -= 1;
            labelMrX.Text = cameraX.ToString();
            cam.TurnUp(1);
            ReDraw();
        }

        private void button15_Click(object sender, EventArgs e)
        {
            cameraX += 1;
            labelMrX.Text = cameraX.ToString();
            cam.TurnDown(1);
            ReDraw();
        }

        private void button26_Click(object sender, EventArgs e)
        {
            cameraZ += 5;
            labelMrZ.Text = cameraZ.ToString();
            cam.Roll(-5);
            ReDraw();
        }

        private void button25_Click(object sender, EventArgs e)
        {
            cameraZ -= 5;
            labelMrZ.Text = cameraZ.ToString();
            cam.Roll(5);
            ReDraw();
        }

        private void button14_Click(object sender, EventArgs e)
        {
            cub = new Cuboid(150, 150, 150);
            cam = new Camera();
            cub.Center = new Point3d(400, 240, 0);
            cam.Location = new Point3d(400, 240, -500);
            ReDraw();
            i = 0;
            bmp = new Bitmap[6];
            labelMx.Text = cam.Location.X.ToString();
            labelMy.Text = cam.Location.Y.ToString();
            labelMz.Text = cam.Location.Z.ToString();
            labelCx.Text = cub.Center.X.ToString();
            labelCy.Text = cub.Center.Y.ToString();
            labelCz.Text = cub.Center.Z.ToString();
            cameraX = 0; cameraY = 0; cameraZ = 0; cubeX = 0; cubeY = 0; cubeZ = 0;
            labelCrX.Text = "0";
            labelCrY.Text = "0";
            labelCrZ.Text = "0";
            labelMrX.Text = "0";
            labelMrY.Text = "0";
            labelMrZ.Text = "0";
        }

        Bitmap[] bmp = new Bitmap[6];
        int i = 0;
        private void button13_Click(object sender, EventArgs e)
        {
            if (i == 6) return;
            OpenFileDialog o = new OpenFileDialog();
            if (o.ShowDialog() == DialogResult.OK)
            {
                bmp[i] = new Bitmap(o.FileName);
                i++;
            }
            cub.FaceImageArray = bmp;
            cub.DrawingLine = false;
            cub.DrawingImage = true;
            cub.FillingFace = true;
            ReDraw();
        }

        private void button24_Click(object sender, EventArgs e)
        {
            cub.Center = new Point3d(cub.Center.X - 5, cub.Center.Y, cub.Center.Z);
            labelCx.Text = cub.Center.X.ToString();
            ReDraw();
        }

        private void button23_Click(object sender, EventArgs e)
        {
            cub.Center = new Point3d(cub.Center.X + 5, cub.Center.Y, cub.Center.Z);
            labelCx.Text = cub.Center.X.ToString();
            ReDraw();
        }

        private void button22_Click(object sender, EventArgs e)
        {
            cub.Center = new Point3d(cub.Center.X, cub.Center.Y - 5, cub.Center.Z);
            labelCy.Text = cub.Center.Y.ToString();
            ReDraw();
        }

        private void button21_Click(object sender, EventArgs e)
        {
            cub.Center = new Point3d(cub.Center.X, cub.Center.Y + 5, cub.Center.Z);
            labelCy.Text = cub.Center.Y.ToString();
            ReDraw();
        }

        private void button20_Click(object sender, EventArgs e)
        {
            cub.Center = new Point3d(cub.Center.X, cub.Center.Y, cub.Center.Z + 5);
            labelCz.Text = cub.Center.Z.ToString();
            ReDraw();
        }

        private void button19_Click(object sender, EventArgs e)
        {
            cub.Center = new Point3d(cub.Center.X, cub.Center.Y, cub.Center.Z - 5);
            labelCz.Text = cub.Center.Z.ToString();
            ReDraw();
        }

        private void button27_Click(object sender, EventArgs e)
        {
            timer.Enabled = !timer.Enabled;
        }
    }
}
