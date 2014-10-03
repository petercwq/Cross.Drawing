using System;
using System.Drawing;
using System.Drawing.Imaging;
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
        public static PixelsBuffer GetBuffer(Image source)
        {
            PixelsBuffer result = new PixelsBuffer(source.Width, source.Height, source.Width);
            DrawBitmapToBuffer(source, result);
            return result;
        }

        /// <summary>
        /// Draw a gdi+ System.Drawing.Bitmap to the rendering buffer
        /// </summary>
        public static void DrawBitmapToBuffer(Image source, PixelsBuffer buffer)
        {
            //make sure we have a ABGR System.Drawing.Bitmap
            Bitmap bmp = null;
            bmp = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
            Graphics g = Graphics.FromImage(bmp);
            g.DrawImage(source, new Rectangle(0, 0, source.Width, source.Height));
            g.Dispose();

            //copy System.Drawing.Bitmap's data to buffer
            Rectangle r = new Rectangle(0, 0, source.Width, source.Height);
            BitmapData data = bmp.LockBits(r, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            IntPtr ptr = data.Scan0;
            int size = data.Stride * bmp.Height;
            byte[] tmpBuffer = new byte[bmp.Width * bmp.Height * 4];
            System.Runtime.InteropServices.Marshal.Copy(ptr, tmpBuffer, 0, size);
            bmp.UnlockBits(data);

            //copy to pixel buffer
            buffer.FromBytes(tmpBuffer, PixelFormats.Bgra);
        }

        /// <summary>
        /// Obtain a gdi+ System.Drawing.Bitmap from <see cref="RenderingBuffer"/>
        /// </summary>
        public static Bitmap GetBitmap(PixelsBuffer source, PixelFormat format)
        {
            System.Drawing.Bitmap bmp = null;
            int width = source.Width;
            int height = source.Height;
            Rectangle r = new Rectangle(0, 0, width, height);

            bmp = new System.Drawing.Bitmap(width, height, format);
            BitmapData data = bmp.LockBits(r, ImageLockMode.ReadWrite, format);
            IntPtr ptr = data.Scan0;

            //calculate buffer size
            int size = 0;
            int bpp = 0;
            switch (format)
            {
                case PixelFormat.Format24bppRgb:
                    bpp = 24;
                    break;
                case PixelFormat.Format32bppArgb:
                    bpp = 32;
                    break;
                case PixelFormat.Format32bppPArgb:
                    bpp = 32;
                    break;
                case PixelFormat.Format32bppRgb:
                    bpp = 32;
                    break;
                case PixelFormat.Format48bppRgb:
                    bpp = 48;
                    break;
                case PixelFormat.Format4bppIndexed:
                    bpp = 48;
                    break;
                case PixelFormat.Format64bppArgb:
                    bpp = 64;
                    break;
                case PixelFormat.Format64bppPArgb:
                    bpp = 64;
                    break;
                case PixelFormat.Format8bppIndexed:
                    bpp = 8;
                    break;
            }
            size = width * (bpp / 8) * height;

            //byte[] buffer = new byte[size];
            //System.Runtime.InteropServices.Marshal.Copy(ptr, buffer, 0, size);

            //copy from source to buffer
            byte[] buffer = source.ToBytes(PixelFormats.Bgra);
            //source.Buffer.CopyTo(buffer, 0);

            //copy back to System.Drawing.Bitmap
            System.Runtime.InteropServices.Marshal.Copy(buffer, 0, ptr, size);

            bmp.UnlockBits(data);

            return bmp;
        }
    }
}
