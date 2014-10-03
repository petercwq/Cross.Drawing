/*
	ICompression.cs
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
    public interface ICompression
    {
        byte[] Compress(byte[] input);
        byte[] Decompress(byte[] input);
        void Compress(Stream input, Stream output);
        void Decompress(Stream input, Stream output);
        void Compress(BinaryReader reader, BinaryWriter writer);
        void Decompress(BinaryReader reader, BinaryWriter writer);
    }
}
