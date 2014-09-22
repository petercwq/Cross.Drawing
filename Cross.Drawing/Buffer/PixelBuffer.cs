using System;

namespace Cross.Drawing
{
    /// <summary>
    /// Holds a memory buffer for pixel rendering purposes. Each pixel is encoded in a 4-bytes uint with the same format of <see cref="Color"/> (A, R, G, B)
    /// </summary>
    public class PixelBuffer
    {
        #region Fields
        /// <summary>
        /// The memory buffer. Each pixel is encoded in a 4-bytes uint with the same format of <see cref="Color"/> (A, R, G, B)
        /// </summary>
        public uint[] Data = null;

        /// <summary>
        /// Horizontal size of buffer (in pixels)
        /// </summary>
        public int Width = 0;

        /// <summary>
        /// Vertical size of buffer (in pixels)
        /// </summary>
        public int Height = 0;

        /// <summary>
        /// The number of pixels a row contains
        /// </summary>
        public int Stride = 0;

        /// <summary>
        /// Starting index offset
        /// </summary>
        public int StartOffset = 0;

        #endregion

        #region Absolute Stride
        //bool recalculateAbsStride = true;
        //private int mAbsoluteStride;
        ///// <summary>
        ///// Gets the absolute size of stride
        ///// </summary>
        //public int AbsoluteStride
        //{
        //    get
        //    {
        //        if (recalculateAbsStride)
        //        {
        //            if (Stride < 0) mAbsoluteStride = -Stride;
        //            else mAbsoluteStride = Stride;
        //            recalculateAbsStride = false;
        //        }
        //        return mAbsoluteStride;
        //    }
        //}
        #endregion

        #region Create View
        /// <summary>
        /// Create a new buffer that attaches to this buffer as a sub-view.
        /// </summary>
        /// <param name="left">Horizontal (column) offset</param>
        /// <param name="top">Vertical (row) offset</param>
        /// <param name="width">New view's horizontal width</param>
        /// <param name="height">New view's vertical height</param>
        public PixelBuffer CreateView(int left, int top, int width, int height)
        {
            return CreateView(left, top, width, height, false);
        }

        /// <summary>
        /// Create a new buffer that attaches to this buffer as a sub-view.
        /// </summary>
        /// <param name="left">Horizontal (column) offset</param>
        /// <param name="top">Vertical (row) offset</param>
        /// <param name="width">New view's horizontal width</param>
        /// <param name="height">New view's vertical height</param>
        /// <param name="inversed">When true, the y-axis is reversed</param>
        public PixelBuffer CreateView(int left, int top, int width, int height, bool inversed)
        {
            PixelBuffer result = new PixelBuffer();

            int stride = this.Stride;
            if (inversed) stride = -stride;
            int offset = this.GetPixelIndex(left, top);

            result.Attach(this.Data, width, height, stride, offset);

            return result;
        }
        #endregion

        #region Attach
        /// <summary>
        /// Setup this buffer by using an existing memory buffer
        /// </summary>
        /// <param name="data">The memory buffer space to attach to</param>
        /// <param name="width">Horizontal size of buffer (in pixels)</param>
        /// <param name="height">Vertical size of buffer (in pixels)</param>
        /// <param name="stride">The number of bytes a row contains</param>
        /// <param name="startOffset">Index of the starting pixel</param>
        public void Attach(uint[] data, int width, int height, int stride, int startOffset)
        {
            Data = data;
            Width = width;
            Height = height;
            Stride = stride;
            //recalculateAbsStride = true;
            if (stride < 0)
            {
                StartOffset = startOffset - (height - 1) * stride;
            }
            else StartOffset = startOffset;
        }

        /// <summary>
        /// Setup this buffer by using an existing pixel buffer
        /// </summary>
        /// <param name="buffer">The memory buffer space to attach to</param>
        /// <param name="width">Horizontal size of buffer (in pixels)</param>
        /// <param name="height">Vertical size of buffer (in pixels)</param>
        /// <param name="stride">The number of bytes a row contains</param>
        /// <param name="startOffset">Index of the starting pixel</param>
        public void Attach(PixelBuffer buffer, int width, int height, int stride, int startOffset)
        {
            Attach(buffer.Data, width, height, stride, startOffset);
        }
        #endregion

        #region Clear
        /// <summary>
        /// Resets all pixels to default value
        /// </summary>
        public void Clear()
        {
            Array.Clear(Data, 0, Data.Length);
        }

        /// <summary>
        /// Reset this buffer to a specific value
        /// </summary>
        public void Clear(uint value)
        {
            for (int i = 0; i < Data.Length; i++)
            {
                Data[i] = value;
            }
        }

        /// <summary>
        /// Reset all pixels to a specific color
        /// </summary>
        public void Clear(Color color)
        {
            int pixelOffset = StartOffset;

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    pixelOffset = StartOffset + y * Stride + x;
                    Data[pixelOffset] = color.Data;
                }
            }
        }
        #endregion

        #region Get Row Index
        /// <summary>
        /// Calculate the index of a specific row
        /// <para>NOTE: for performance optimzation, replace this method with inline code: rowIndex = StartOffset + row*Stride</para>
        /// </summary>
        public int GetRowIndex(int row)
        {
            return StartOffset + row * Stride;
        }
        #endregion

        #region Get Pixel Index
        /// <summary>
        /// Calculate the index of a pixel
        /// <para>NOTE: for performance optimzation, replace this method with inline code: rowIndex = StartOffset + row*Stride + column</para>
        /// </summary>
        public int GetPixelIndex(int column, int row)
        {
            return StartOffset + row * Stride + column;
        }
        #endregion

        #region Copy To
        /// <summary>
        /// Copy the content of this buffer to target buffer.
        /// <para>The target's width, height, stride must be the same as this one</para>
        /// </summary>
        public void CopyTo(PixelBuffer target)
        {
            if (target.Data.Length == Data.Length) Array.Copy(Data, target.Data, Data.Length);
            else throw new Exception("Target must have exact width, height and stride as this");
        }
        #endregion

        #region Clone
        /// <summary>
        /// Create a new instance and copy the content of this buffer to the new one
        /// </summary>
        public PixelBuffer Clone()
        {
            PixelBuffer result = new PixelBuffer(Width, Height, Stride);
            Array.Copy(Data, result.Data, Data.Length);
            return result;
        }
        #endregion

        #region From / To byte array

        #region From
        /// <summary>
        /// Copy from a byte array to this buffer
        /// </summary>
        public void FromBytes(byte[] buffer, PixelFormats format)
        {
            int idxBuffer = 0;
            int idxData = 0;
            do
            {
                byte a = buffer[idxBuffer + format.AlphaOffset];//alpha
                byte r = buffer[idxBuffer + format.RedOffset];//red
                byte g = buffer[idxBuffer + format.GreenOffset];//green
                byte b = buffer[idxBuffer + format.BlueOffset];//blue

                Data[idxData] = (uint)(a << 24) | (uint)(r << 16) | (uint)(g << 8) | (uint)b;
                idxBuffer += 4; ;
                idxData++;
            }
            while (idxBuffer < buffer.Length);
        }
        #endregion

        #region To
        /// <summary>
        /// Convert this buffer to byte array. Assuming 32-bit pixel format
        /// </summary>
        public byte[] ToBytes(PixelFormats format)
        {
            byte[] buffer = new byte[Width * Height * 4];

            int pixelOffset = 0;
            int dataOffset = 0;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    uint c = Data[dataOffset];
                    buffer[pixelOffset + format.AlphaOffset] = (byte)((c & 0xFF000000) >> 24); // alpha
                    buffer[pixelOffset + format.RedOffset] = (byte)((c & 0x00FF0000) >> 16); // red
                    buffer[pixelOffset + format.GreenOffset] = (byte)((c & 0x0000FF00) >> 8); // green
                    buffer[pixelOffset + format.BlueOffset] = (byte)((c & 0x000000FF)); // blue

                    dataOffset++;
                    pixelOffset += 4;
                }
            }

            return buffer;
        }
        #endregion

        #region From Bgr
        /// <summary>
        /// Convert this buffer from a byte array. Assuming BGR pixel format
        /// </summary>
        public void FromBgr(byte[] buffer, int width, int height, int stride)
        {
            //allocate new memory if current buffer is not exactly matched the new one
            if ((width != Width) && (height != Height))
            {
                Width = width;
                Height = height;
                Stride = width;
                Data = new uint[width * height];
            }

            int pixelOffset = 0;
            int dataOffset = 0;
            for (int y = 0; y < Height; y++)
            {
                pixelOffset = y * stride;

                for (int x = 0; x < Width; x++)
                {
                    Data[dataOffset] = (uint)(
                                            (0xFF000000)                          //alpha
                                          | (uint)(buffer[pixelOffset + 2] << 16) //red
                                          | (uint)(buffer[pixelOffset + 1] << 8)  //green
                                          | (buffer[pixelOffset])                 //blue
                                        );

                    dataOffset++;
                    pixelOffset += 3;
                }
            }
        }
        #endregion

        #region To Bgr
        /// <summary>
        /// Convert this buffer to a byte array. Assuming BGR pixel format
        /// </summary>
        public byte[] ToBgr(int stride)
        {
            byte[] buffer = new byte[stride * Height];

            int pixelOffset = 0;
            int dataOffset = 0;
            for (int y = 0; y < Height; y++)
            {
                pixelOffset = y * stride;

                for (int x = 0; x < Width; x++)
                {
                    uint c = Data[dataOffset];
                    buffer[pixelOffset] = (byte)((c & 0x000000FF)); // blue
                    buffer[pixelOffset + 1] = (byte)((c & 0x0000FF00) >> 8); // green
                    buffer[pixelOffset + 2] = (byte)((c & 0x00FF0000) >> 16); // red

                    dataOffset++;
                    pixelOffset += 3;
                }
            }

            return buffer;
        }

        /// <summary>
        /// Convert this buffer to a byte array. Assuming BGR pixel format
        /// </summary>
        public void ToBgr(byte[] buffer, int stride)
        {
            int pixelOffset = 0;
            int dataOffset = 0;
            for (int y = 0; y < Height; y++)
            {
                pixelOffset = y * stride;

                for (int x = 0; x < Width; x++)
                {
                    uint c = Data[dataOffset];
                    buffer[pixelOffset] = (byte)((c & 0x000000FF)); // blue
                    buffer[pixelOffset + 1] = (byte)((c & 0x0000FF00) >> 8); // green
                    buffer[pixelOffset + 2] = (byte)((c & 0x00FF0000) >> 16); // red

                    dataOffset++;
                    pixelOffset += 3;
                }
            }
        }
        #endregion

        #region From Bgra
        /// <summary>
        /// Convert this buffer from a byte array. Assuming BGR pixel format
        /// </summary>
        public void FromBgra(byte[] buffer, int width, int height, int stride)
        {
            //allocate new memory if current buffer is not exactly matched the new one
            if ((width != Width) && (height != Height))
            {
                Width = width;
                Height = height;
                Stride = width;
                Data = new uint[width * height];
            }

            int pixelOffset = 0;
            int dataOffset = 0;
            for (int y = 0; y < Height; y++)
            {
                pixelOffset = y * stride;

                for (int x = 0; x < Width; x++)
                {
                    Data[dataOffset] = (uint)(
                                            (uint)(buffer[pixelOffset + 3] << 24) //alpha
                                          | (uint)(buffer[pixelOffset + 2] << 16) //red
                                          | (uint)(buffer[pixelOffset + 1] << 8)  //green
                                          | (buffer[pixelOffset])                 //blue
                                        );

                    dataOffset++;
                    pixelOffset += 3;
                }
            }
        }
        #endregion

        #region To Bgra
        /// <summary>
        /// Convert this buffer to a byte array. Assuming BGRA pixel format.
        /// </summary>
        public byte[] ToBgra(int stride)
        {
            byte[] buffer = new byte[stride * Height];

            int pixelOffset = 0;
            int dataOffset = 0;
            for (int y = 0; y < Height; y++)
            {
                pixelOffset = y * stride;

                for (int x = 0; x < Width; x++)
                {
                    uint c = Data[dataOffset];
                    buffer[pixelOffset] = (byte)((c & 0x000000FF)); // blue
                    buffer[pixelOffset + 1] = (byte)((c & 0x0000FF00) >> 8); // green
                    buffer[pixelOffset + 2] = (byte)((c & 0x00FF0000) >> 16); // red
                    buffer[pixelOffset + 3] = (byte)((c & 0xFF000000) >> 24); // alpha

                    dataOffset++;
                    pixelOffset += 4;
                }
            }

            return buffer;
        }

        /// <summary>
        /// Convert this buffer to an existing byte array. Assuming BGRA pixel format
        /// </summary>
        public void ToBgra(byte[] buffer, int stride)
        {
            int pixelOffset = 0;
            int dataOffset = 0;
            uint c = 0;
            int y, x;
            for (y = 0; y < Height; y++)
            {
                pixelOffset = y * stride;

                for (x = 0; x < Width; x++)
                {
                    c = Data[dataOffset];
                    buffer[pixelOffset] = (byte)((c & 0x000000FF)); // blue
                    buffer[pixelOffset + 1] = (byte)((c & 0x0000FF00) >> 8); // green
                    buffer[pixelOffset + 2] = (byte)((c & 0x00FF0000) >> 16); // red
                    buffer[pixelOffset + 3] = (byte)((c & 0xFF000000) >> 24); // alpha

                    dataOffset++;
                    pixelOffset += 4;
                }
            }
        }

        /// <summary>
        /// Convert this buffer to an existing byte array. Assuming BGRA pixel format
        /// </summary>
        public void ToBgraWithoutA(byte[] buffer, int stride)
        {
            int pixelOffset = 0;
            int dataOffset = 0;
            uint c = 0;
            int y, x;
            for (y = 0; y < Height; y++)
            {
                pixelOffset = y * stride;

                for (x = 0; x < Width; x++)
                {
                    c = Data[dataOffset];
                    buffer[pixelOffset] = (byte)((c & 0x000000FF)); // blue
                    buffer[pixelOffset + 1] = (byte)((c & 0x0000FF00) >> 8); // green
                    buffer[pixelOffset + 2] = (byte)((c & 0x00FF0000) >> 16); // red
                    //buffer[pixelOffset + 3] = (byte)((c & 0xFF000000) >> 24); // alpha

                    dataOffset++;
                    pixelOffset += 4;
                }
            }
        }

        //public void ToBgra2(byte[] buffer, int stride)
        //{
        //    int pixelOffset = 0;
        //    int dataOffset = 0;
        //    uint c = 0;
        //    int y, x;
        //    for (y = 0; y < Height; y++)
        //    {
        //        pixelOffset = y * stride;

        //        for (x = 0; x < Width; x++)
        //        {
        //            c = Data[dataOffset];
        //            buffer[pixelOffset + 3] = (byte)((c & 0x000000FF)); // blue
        //            buffer[pixelOffset + 2] = (byte)((c & 0x0000FF00) >> 8); // green
        //            buffer[pixelOffset + 1] = (byte)((c & 0x00FF0000) >> 16); // red
        //            buffer[pixelOffset + 0] = (byte)((c & 0xFF000000) >> 24); // alpha

        //            dataOffset++;
        //            pixelOffset += 4;
        //        }
        //    }
        //}
        #endregion

        #endregion

        #region Scroll

        #region Scroll Left
        /// <summary>
        /// Scroll content of buffer to the left
        /// </summary>
        /// <param name="pixels">Number of pixels to scroll by</param>
        public void ScrollLeft(int pixels)
        {
            int pixelOffset = 0;
            int length = Width - pixels;
            pixelOffset = StartOffset;

            for (int y = 0; y < Height; y++)
            {
                Array.Copy(Data, pixelOffset + pixels, Data, pixelOffset, length);
                pixelOffset += Stride;
            }
        }
        #endregion

        #region Scroll Right
        /// <summary>
        /// Scroll content of buffer to the right
        /// </summary>
        /// <param name="pixels">Number of pixels to scroll by</param>
        public void ScrollRight(int pixels)
        {
            int pixelOffset = 0;
            int length = Width - pixels;
            pixelOffset = StartOffset;

            for (int y = 0; y < Height; y++)
            {
                Array.Copy(Data, pixelOffset, Data, pixelOffset + pixels, length);
                pixelOffset += Stride;
            }
        }
        #endregion

        #region Scroll Up
        /// <summary>
        /// Scroll content of buffer upward
        /// </summary>
        /// <param name="pixels">Number of pixels to scroll by</param>
        public void ScrollUp(int pixels)
        {
            Array.Copy(Data, Stride * pixels, Data, 0, (Height - pixels) * Stride);
        }
        #endregion

        #region Scroll Down
        /// <summary>
        /// Scroll content of buffer downward
        /// </summary>
        /// <param name="pixels">Number of pixels to scroll by</param>
        public void ScrollDown(int pixels)
        {
            Array.Copy(Data, 0, Data, Stride * pixels, (Height - pixels) * Stride);
        }
        #endregion

        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor
        /// </summary>
        public PixelBuffer()
        { }

        /// <summary>
        /// Create a new instance with exact width, height, stride as source. Then copy the data of source to new instance
        /// </summary>
        public PixelBuffer(PixelBuffer source)
        {
            Width = source.Width;
            Height = source.Height;
            Stride = source.Stride;
            Data = new uint[source.Data.Length];
            Array.Copy(source.Data, Data, source.Data.Length);
        }

        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="width">Horizontal size of buffer (in pixels)</param>
        /// <param name="height">Vertical size of buffer (in pixels)</param>
        public PixelBuffer(int width, int height)
        {
            Width = width;
            Height = height;
            Stride = width;
            Data = new uint[width * height];
        }

        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="width">Horizontal size of buffer (in pixels)</param>
        /// <param name="height">Vertical size of buffer (in pixels)</param>
        /// <param name="stride">The number of pixels a row contains</param>
        public PixelBuffer(int width, int height, int stride)
        {
            Width = width;
            Height = height;
            Stride = stride;
            if (stride < 0)
            {
                StartOffset = -(height - 1) * stride;
                //Data = new int[-stride * height];
                Data = new uint[-stride * height];
            }
            //else Data = new int[stride * height];
            else Data = new uint[stride * height];
        }

        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="data">The memory buffer to use instead of creating a new one</param>
        /// <param name="width">Horizontal size of buffer (in pixels)</param>
        /// <param name="height">Vertical size of buffer (in pixels)</param>
        /// <param name="stride">The number of pixels a row contains</param>
        //public PixelBuffer(int[] data, int width, int height, int stride)
        public PixelBuffer(uint[] data, int width, int height, int stride)
        {
            Data = data;
            Width = width;
            Height = height;
            Stride = stride;
            if (stride < 0)
            {
                StartOffset = -(height - 1) * stride;
            }
        }
        #endregion

        #region OPTIMIZATION INVESTIGATION
        /* NOTE BY HAINM
         * ===========================
         * When have time, check out these tricks
         */

        #region Reverse byte order
        /// <summary>
        /// Reverse byte order of an int. (Useful for converting ARGB to/from BGRA
        /// </summary>
        private static int Reverse(int i)
        {
            int a = (int)(i & 0xFF00FF00);
            int b = i & 0x00FF00FF;
            i = (a >> 8) | (b << 8);
            a = i & 0x0000FFFF;
            b = (int)(i & 0xFFFF0000);
            i = (a << 16) | (b >> 16);

            return i;
        }

        /// <summary>
        /// Shorter version of reverse int.
        /// </summary>
        private static int Reverse2(int i)
        {
            i = ((int)(i & 0xFF00FF00) >> 8) | ((i & 0x00FF00FF) << 8);
            i = ((i & 0x0000FFFF) << 16) | ((int)(i & 0xFFFF0000) >> 16);

            return i;
        }

        /// <summary>
        /// Example of reverse for Uint.
        /// </summary>
        private static uint ReverseU(uint i)
        {
            uint a = i & 0xFF00FF00;
            uint b = i & 0x00FF00FF;
            i = (a >> 8) | (b << 8);
            a = i & 0x0000FFFF;
            b = i & 0xFFFF0000;
            i = (a << 16) | (b >> 16);

            return i;
        }
        #endregion

        #region Convert int[] to byte[]
        private static byte[] ToByteArray(int[] p)
        {
            int len = p.Length << 2;
            byte[] result = new byte[len];
            Buffer.BlockCopy(p, 0, result, 0, len);
            return result;
        }
        #endregion

        #endregion
    }
}
