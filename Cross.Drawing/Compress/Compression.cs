/*
	Compression.cs
	Author: Adrian Huang
	FOR RESEARCH OR STUDY ONLY, DO NOT USE THE CODE FOR COMMERCIAL

	I'll appreciate you posting question and bug to me
	email: sanyuexx@hotmail.com
	web	  :	http://www.cnblogs.com/dah
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Adrian.Compression
{
    public abstract class Compression : ICompression
    {
        public event Action<int> Progress;

        #region ICompression Members

        public byte[] Compress(byte[] input)
        {
            MemoryStream stream = new MemoryStream(input);
            MemoryStream output = new MemoryStream();
            Compress(stream, output);
            return output.ToArray();
        }

        public byte[] Decompress(byte[] input)
        {
            MemoryStream stream = new MemoryStream(input);
            MemoryStream output = new MemoryStream();
            Decompress(stream, output);
            return output.ToArray();
        }

        public void Compress(Stream input, Stream output)
        {
            BinaryReader reader = new BinaryReader(input);
            BinaryWriter writer = new BinaryWriter(output);
            Compress(reader, writer);
        }

        public void Decompress(Stream input, Stream output)
        {
            BinaryReader reader = new BinaryReader(input);
            BinaryWriter writer = new BinaryWriter(output);
            Decompress(reader, writer);
        }

        public abstract void Compress(BinaryReader reader, BinaryWriter writer);

        public abstract void Decompress(BinaryReader reader, BinaryWriter writer);

        #endregion

        private Dictionary<BinaryReader, long> LengthDic = new Dictionary<BinaryReader, long>();

        protected int RaiseEvent(BinaryReader reader, int lastPercent)
        {
            if (!LengthDic.ContainsKey(reader))
            {
                LengthDic[reader] = reader.BaseStream.Length;
            }
            int currentPercent = (int)(100.0 * reader.BaseStream.Position / LengthDic[reader]);
            if (this.Progress != null && currentPercent - lastPercent >= 1)
            {
                lastPercent = currentPercent;
                this.Progress(currentPercent);
            }
            return lastPercent;
        }

        protected void RaiseFinishEvent()
        {
            if (Progress != null)
            {
                Progress(100);
            }
        }
    }
}
