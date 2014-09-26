using System;

namespace Cross.Drawing
{
    /// <summary>
    /// Provides a memory buffer for masking (opacity, clipping, etc.). 
    /// Each pixel's opacity is represented by a single byte (0-255)
    /// </summary>
    public class MaskBuffer
    {
        #region Fields

        /// <summary>
        /// The memory buffer. Each pixel's opacity is represented by a single byte (0-255)
        /// </summary>
        public byte[] Data = null;

        /// <summary>
        /// x-axis offset (relative to the underlying <see cref="PixelsBuffer"/>
        /// </summary>
        public int Left = 0;

        /// <summary>
        /// y-axis offset (relative to the underlying <see cref="PixelsBuffer"/>
        /// </summary>
        public int Top = 0;

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

        #region Get Index

        /// <summary>
        /// Calculate the index of a specific row
        /// <para>NOTE: for performance optimzation, replace this method with inline code: rowIndex = StartOffset + row*Stride</para>
        /// </summary>
        [Obsolete]
        public int GetRowIndex(int row)
        {
            return StartOffset + row * Stride;
        }

        /// <summary>
        /// Calculate the index of a pixel
        /// <para>NOTE: for performance optimzation, replace this method with inline code: rowIndex = StartOffset + row*Stride + column</para>
        /// </summary>
        [Obsolete]
        public int GetPixelIndex(int column, int row)
        {
            return StartOffset + row * Stride + column;
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
        public void Clear(byte value)
        {
            for (int i = 0; i < Data.Length; i++)
            {
                Data[i] = value;
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor
        /// </summary>
        public MaskBuffer()
        { }

        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="width">Horizontal size of buffer (in pixels)</param>
        /// <param name="height">Vertical size of buffer (in pixels)</param>
        public MaskBuffer(int width, int height)
        {
            Width = width;
            Height = height;
            Stride = width;
            Data = new byte[width * height];
        }

        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="width">Horizontal size of buffer (in pixels)</param>
        /// <param name="height">Vertical size of buffer (in pixels)</param>
        /// <param name="stride">The number of pixels a row contains</param>
        public MaskBuffer(int width, int height, int stride)
        {
            Width = width;
            Height = height;
            Stride = stride;
            if (stride < 0)
            {
                StartOffset = -(height - 1) * stride;
                Data = new byte[-stride * height];
            }
            else Data = new byte[stride * height];
        }

        /// <summary>
        /// Create a new instance
        /// </summary>
        /// <param name="data">The memory buffer to use instead of creating a new one</param>
        /// <param name="width">Horizontal size of buffer (in pixels)</param>
        /// <param name="height">Vertical size of buffer (in pixels)</param>
        /// <param name="stride">The number of pixels a row contains</param>
        public MaskBuffer(byte[] data, int width, int height, int stride)
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
    }
}
