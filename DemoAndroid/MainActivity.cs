using System;
using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Widget;
using Cross.Drawing;
using Cross.Helpers;
using Demo.Helpers;

namespace DemoAndroid
{
    [Activity(Label = "AndroidDemo", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        Bitmap bmp = null;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
            var imageView = (ImageView)FindViewById<ImageView>(Resource.Id.imageView1);

            FindViewById<Button>(Resource.Id.button_1000).Click += (sender, e) =>
            {
                Draw(imageView, 1000);
            };
            FindViewById<Button>(Resource.Id.button_10000).Click += (sender, e) =>
            {
                Draw(imageView, 10000);
            };
        }

        private void Draw(ImageView imageView, int times)
        {
            var start = DateTime.Now;
            PixelsBuffer buffer = new PixelsBuffer(600, 600);
            IDrawer drawer = new Drawer(buffer);
            drawer.Clear(Colors.Transparent);
            //create fill for drawing
            Fill fill = new Fill(Colors.Transparent);
            //fill.Opacity = 0.3;
            //draw content
            var coords1 = GetPath(1);
            var coords2 = GetPath(0);
            var coords3 = GetPath(2);

            var path = GetPath();
            var startDraw = DateTime.Now;
            int step = 5;
            for (int i = 0; i < times; i++)
            {
                //drawer.DrawRectangle(fill, 10, 10, 300, 300);
                //drawer.DrawEllipse(fill, 200, 200, 120, 200);
                //drawer.DrawPolygon(fill, coords1);
                //drawer.DrawPolygon(fill, coords2);
                //drawer.DrawPolygon(fill, coords3);

                //draw content
                //drawer.Rotate(15);
                //drawer.Scale(0.3, 0.3);
                //drawer.DrawPath(fill, path);

                var margin = i / 10 * step;

                PixelsBuffer view = buffer.CreateView(margin, margin, buffer.Width - margin * 2, buffer.Height - margin * 2, true);
                DrawFrame(view, Colors.OrangeRed);
                DrawLine(view, Colors.Olive);

            }
            DrawLion(buffer, 200, 200);

            if (bmp != null)
                bmp.Dispose();
            bmp = BufferToBitmap.GetBitmap(buffer);

            //show to screen
            var icon = BitmapFactory.DecodeResource(this.Resources, Resource.Drawable.Icon);

            imageView.SetImageBitmap(BufferToBitmap.Overlay(new Bitmap[] { icon, bmp }));
            Android.Util.Log.Debug("Draw " + times, "Draw: " + (DateTime.Now - startDraw).TotalSeconds.ToString() + " Total: " + (DateTime.Now - start).TotalSeconds.ToString());
        }

        //create fill for drawing
        Fill fill = Fills.MistyRose;

        DrawingPath GetPath()
        {
            //create path
            DrawingPath path = new DrawingPath();
            path.MoveTo(200, 100);
            path.CurveTo(200, 350, 340, 30, 360, 200);
            path.CurveTo(200, 100, 40, 200, 60, 30);
            return path;
        }

        double[] GetPath(int index)
        {
            double[] result = null;

            switch (index)
            {
                case 0://rectangle
                    result = new double[] { 0, 0, 100, 0, 100, 100, 0, 100, 0, 0 };
                    break;

                case 1://triangle
                    result = TestFactory.Triangle();
                    result = TestFactory.Scale(result, 10);
                    break;

                case 2://star
                    result = TestFactory.Star();
                    result = TestFactory.Scale(result, 12);
                    result = TestFactory.Offset(result, 0, -100);
                    break;

                case 3://crown
                    result = TestFactory.Crown();
                    result = TestFactory.Scale(result, 10);
                    result = TestFactory.Offset(result, 0, -70);
                    break;
            }

            return result;
        }

        void DrawLion(PixelsBuffer buffer, int x, int y)
        {
            //create a new drawing context
            //PixelBuffer buffer = new PixelBuffer(400, 400);
            IDrawer drawer = new Drawer(buffer);

            //get coordinates and colors
            double[][] polygons = LionPathHelper.GetLionPolygons();
            Cross.Drawing.Color[] colors = LionPathHelper.GetLionColors();

            //iterate all polygons and draw them
            double[] coordinates = null;
            drawer.Translate(x, y);
            for (int i = 0; i < polygons.Length; i++)
            {
                coordinates = polygons[i];
                Fill fill = new Fill(colors[i]);
                drawer.DrawPolygon(fill, coordinates);
            }
        }

        #region Draw Line
        /// <summary>
        /// Draw a diagonal line with the specified color from (0,0) to (width, height)
        /// </summary>
        void DrawLine(PixelsBuffer buffer, Cross.Drawing.Color color)
        {
            int idx = 0; //pixel index

            for (int i = 0; i < buffer.Height; i++)
            {
                idx = buffer.StartOffset + i * buffer.Stride + i; // buffer.GetPixelIndex(i, i);
                buffer.Data[idx] = color.Data;
            }
        }
        #endregion

        #region Draw Frame
        /// <summary>
        /// Draw a frame around the buffer from (0,0) to (width, height)
        /// </summary>
        void DrawFrame(PixelsBuffer buffer, Cross.Drawing.Color color)
        {
            int idx = 0; //pixel index

            //draw left, right lines
            for (int y = 0; y < buffer.Height; y++)
            {
                //left
                idx = buffer.StartOffset + buffer.Stride * y;// buffer.GetPixelIndex(0, y);
                buffer.Data[idx] = color.Data;

                //right
                idx = buffer.StartOffset + buffer.Stride * y + buffer.Width - 1;// buffer.GetPixelIndex(buffer.Width - 1, y);
                buffer.Data[idx] = color.Data;
            }

            //draw top, bottom lines
            for (int x = 0; x < buffer.Width; x++)
            {
                //top
                idx = buffer.StartOffset + x;// buffer.GetPixelIndex(x, 0);
                buffer.Data[idx] = color.Data;

                //bottom
                idx = buffer.StartOffset + (buffer.Height - 1) * buffer.Stride + x;// buffer.GetPixelIndex(x, buffer.Height - 1);
                buffer.Data[idx] = color.Data;
            }
        }
        #endregion
    }
}

