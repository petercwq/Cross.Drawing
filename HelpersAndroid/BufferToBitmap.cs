using System;
using System.Collections.Generic;
using System.Linq;
using Android.Graphics;
using Cross.Drawing;

namespace Cross.Helpers
{
    /// <summary>
    /// A helper class to obtain a gdi+ System.Drawing.Bitmap from <see cref="RenderingBuffer"/>
    /// </summary>
    public class BufferToBitmap
    {
        /// <summary>
        /// Obtains a rendering buffer from a gdi+ System.Drawing.Bitmap
        /// </summary>
        public static PixelBuffer GetBuffer(Bitmap source)
        {
            PixelBuffer result = new PixelBuffer(source.Width, source.Height, source.Width);
            DrawBitmapToBuffer(source, result);
            return result;
        }

        public static void DrawBitmapToBuffer(Bitmap source, PixelBuffer buffer)
        {
            //make sure we have a ABGR Bitmap, same as PixelFormat.Format32bppArgb
            var bmp = ConvertConfig(source, Bitmap.Config.Argb8888);

            //copy Bitmap's data to buffer
            IntPtr ptr = bmp.LockPixels();
            int size = bmp.RowBytes * bmp.Height;
            byte[] tmpBuffer = new byte[bmp.Width * bmp.Height * 4];
            System.Runtime.InteropServices.Marshal.Copy(ptr, tmpBuffer, 0, size);
            bmp.UnlockPixels();

            //copy to pixel buffer
            buffer.FromBytes(tmpBuffer, PixelFormats.Bgra);
        }

        public static Bitmap ConvertConfig(Bitmap bitmap, Bitmap.Config config)
        {
            if (bitmap.GetConfig().Equals(config))
                return Bitmap.CreateBitmap(bitmap);
            Bitmap convertedBitmap = Bitmap.CreateBitmap(bitmap.Width, bitmap.Height, config);
            Canvas canvas = new Canvas(convertedBitmap);
            Android.Graphics.Paint paint = new Android.Graphics.Paint();
            paint.Color = Android.Graphics.Color.Black;
            canvas.DrawBitmap(bitmap, 0, 0, paint);
            return convertedBitmap;
        }

        public static Bitmap Overlay(IEnumerable<Bitmap> bmps, Bitmap.Config config = null)
        {
            int width = bmps.Max<Bitmap>(x => x.Width);
            int height = bmps.Max<Bitmap>(x => x.Height);
            Bitmap bmOverlay = Bitmap.CreateBitmap(width, height, config == null ? Bitmap.Config.Argb8888 : config);
            Canvas canvas = new Canvas(bmOverlay);
            foreach (var bmp in bmps)
                canvas.DrawBitmap(bmp, 0, 0, null);
            canvas.Dispose();
            return bmOverlay;
        }

        /// <summary>
        /// Obtain a Bitmap from <see cref="RenderingBuffer"/>
        /// </summary>
        public static Bitmap GetBitmap(PixelBuffer source, Bitmap.Config config = null)
        {
            var bmp = Bitmap.CreateBitmap(Array.ConvertAll<uint, int>(source.Data, new Converter<uint, int>(x => (int)x)), source.StartOffset, source.Stride, source.Width, source.Height, Bitmap.Config.Argb8888);

            if (config != null && !config.Equals(Bitmap.Config.Argb8888))
            {
                var tmp = ConvertConfig(bmp, config);
                bmp.Dispose();
                bmp = tmp;
            }
            return bmp;
        }
    }
}
