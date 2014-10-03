/*
	HuffmanCompression.cs
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
using Adrian.Framework.DataStructure;
using System.Collections;
using System.Runtime.Serialization;
using System.Diagnostics;

namespace Adrian.Compression
{
	public class HuffmanCompression : Compression
	{
		#region Internal Helper Class

		[DataContract]
		private class ByteNode : IComparable
		{
			public static class Serializer
			{
				public static void Serialize(Stream stream, ByteNode[] bns)
				{
					foreach (ByteNode bn in bns)
					{
						stream.WriteByte(bn.ByteValue);
						byte[] bts = BitConverter.GetBytes(bn.Times);
						stream.WriteByte(bts[0]);
						stream.WriteByte(bts[1]);
					}
				}

				public static ByteNode[] Deserialize(Stream stream)
				{
					ByteNode[] bns = new ByteNode[256];
					for (int i = 0; i < 256; i++)
					{
						bns[i] = new ByteNode();
						bns[i].ByteValue = (byte)stream.ReadByte();
						byte[] buff = new byte[2];
						stream.Read(buff, 0, 2);
						bns[i].Times = BitConverter.ToUInt16(buff, 0);
					}
					return bns;
				}
			}

			[IgnoreDataMember]
			public ByteNode Left;
			[IgnoreDataMember]
			public ByteNode Right;
			[IgnoreDataMember]
			public ByteNode Parent;

			public byte ByteValue;
			public ulong Times;

			public ByteNode(ByteNode lhs, ByteNode rhs)
			{
				Left = lhs;
				Right = rhs;
				lhs.Parent = this;
				rhs.Parent = this;
				Times = lhs.Times + rhs.Times;
			}

			public ByteNode(byte byteValue)
			{
				ByteValue = byteValue;
			}

			internal ByteNode() { }

			public bool IsLeaf
			{
				get { return Left == null && Right == null && Parent != null; }
			}

			public bool IsLeftChild
			{
				get { return Parent.Left == this; }
			}

			public bool IsRightChild
			{
				get { return Parent.Right == this; }
			}

			#region IComparable Members

			public int CompareTo(object obj)
			{
				if (obj == null || obj.GetType() != GetType())
				{
					throw new ArgumentException("obj can't be null or the other type");
				}
				ByteNode bi = (ByteNode)obj;
				return Times.CompareTo(bi.Times);
			}

			#endregion
		}

		private class CodeItem : IComparable
		{
			public byte ByteValue;
			public ushort CodeValue;
			[IgnoreDataMember]
			public IList<bool> CodeBits;

			[IgnoreDataMember]
			public int BitCount;

			public CodeItem() { }

			public int CompareTo(object obj)
			{
				if (obj == null || obj.GetType() != GetType())
				{
					throw new ArgumentException("obj can't be null or the other type");
				}
				CodeItem bi = (CodeItem)obj;
				return CodeValue.CompareTo(bi.CodeValue);
			}
		}

		private class BitReader
		{
			private BinaryReader _byteReader;
			private byte _currentByte;
			private int _bitIdx = 8;
			private byte _mask = 0x80;
			private byte _complementaryBitsLength;

			public byte ComplementaryBitsLength
			{
				get { return _complementaryBitsLength; }
				set { _complementaryBitsLength = value; }
			}

			public BitReader(BinaryReader binReader)
			{
				_byteReader = binReader;
			}

			public bool PeekBit()
			{
				EnsureByte();
				return Convert.ToBoolean((_currentByte << _bitIdx) & _mask);
			}

			public bool ReadBit()
			{
				bool b = PeekBit();
				_bitIdx++;
				return b;
			}

			private void EnsureByte()
			{
				if (_bitIdx == 8)
				{
					_currentByte = _byteReader.ReadByte();
					_bitIdx = 0;
				}
			}

			public bool HasMore
			{
				get
				{
					if (_byteReader.BaseStream.Position < _byteReader.BaseStream.Length)
					{
						return true;
					}
					else if (_byteReader.BaseStream.Position == _byteReader.BaseStream.Length)
					{
						return _bitIdx < 8 - _complementaryBitsLength;
					}
					return false;
				}
			}
		}

		#endregion

		//private static BinaryFormatter _serializer = new BinaryFormatter();

		public override void Compress(BinaryReader reader, BinaryWriter writer)
		{
			ByteNode[] items = new ByteNode[256];
			for (int i = 0; i < 256; i++)
			{
				items[i] = new ByteNode((byte)i);
			}
			while (reader.BaseStream.Position < reader.BaseStream.Length)
			{
				items[reader.ReadByte()].Times++;
			}
			// save frequency table
			ByteNode.Serializer.Serialize(writer.BaseStream, items);

			CodeItem[] codes = GetCodes(items);
			List<CodeItem> notEmpty = new List<CodeItem>();
			Dictionary<byte, IList<bool>> byteDic = new Dictionary<byte, IList<bool>>();
			foreach (CodeItem ci in codes)
			{
				if (ci != null)
				{
					notEmpty.Add(ci);
					byteDic[ci.ByteValue] = ci.CodeBits;
				}
			}

			List<bool> allBits = new List<bool>();
			// convert original byte to our bit code
			reader.BaseStream.Seek(0, SeekOrigin.Begin);
			int lastPercent = 0;
			while (reader.BaseStream.Position < reader.BaseStream.Length)
			{
				lastPercent = RaiseEvent(reader, lastPercent);
				allBits.AddRange(byteDic[reader.ReadByte()]);
			}
			// complementary bits
			byte length = 0;
			if (allBits.Count % 8 != 0)
			{
				length = (byte)(8 - allBits.Count % 8);
				allBits.AddRange(new bool[length]);
			}
			writer.Write(length);
			// write all bits
			for (int i = 0; i < allBits.Count - 7; i += 8)
			{
				writer.Write(ToByte(allBits, i));
			}
			RaiseFinishEvent();
		}

		#region Compress Helper Methods

		private CodeItem[] GetCodes(ByteNode[] items)
		{
			Heap<ByteNode> heap = new Heap<ByteNode>(items);
			while (heap.Count > 1)
			{
				ByteNode lhs = heap.DeleteMin(); // left node is smaller / 0
				ByteNode rhs = heap.DeleteMin(); // right node is bigger / 1
				ByteNode parent = new ByteNode(lhs, rhs); // give them a parent
				heap.Add(parent);
			}
			CodeItem[] codes = new CodeItem[256];
			AddCode(codes, heap.DeleteMin(), 0, new List<bool>());
			return codes;
		}

		private byte ToByte(IList<bool> allBits, int i)
		{
			byte value = 0;
			// convert bits to byte
			// e.g.
			// 0101 1010
			// value = 0 << 7 + 1 << 6 + ... + 1 << 1 + 0 << 0
			for (int k = i; k < i + 8; k++)
			{
				value += (byte)(Convert.ToByte(allBits[k]) << (7 - (k - i)));
			}
			return value;
		}

		private IList<bool> ToBits(ushort p)
		{
			if (p == 0)
			{
				return new bool[] { false };
			}
			// convert ushort to bits
			// high bits first
			// e.g.
			// ushort: 00000000 00001010  =  10
			// bin: 1010
			// shift = 3;
			int shift = 15;
			ushort mask = 0x0001;
			while (p >> shift != 1)
			{
				shift--;
			}
			bool[] list = new bool[shift + 1];
			for (int i = shift; i > -1; i--)
			{
				list[shift - i] = Convert.ToBoolean((p >> i) & mask);
			}
			return list;
		}

		private void AddCode(CodeItem[] codes, ByteNode treeNode, int p, List<bool> list)
		{
			if (treeNode.Parent != null)
			{
				if (treeNode.IsLeftChild)
				{
					list.Add(false); // false stands for 0
				}
				else
				{
					list.Add(true); // true stands for 1
				}
			}
			if (treeNode.IsLeaf)
			{
				if (treeNode.Times > 0) // if the code never happen, we won't waste time on it
				{
					CodeItem codeItem = new CodeItem();
					codeItem.ByteValue = treeNode.ByteValue;
					codeItem.CodeValue = GetCodeValue(list);
					codeItem.CodeBits = list.ToArray();
					codes[treeNode.ByteValue] = codeItem;
				}
			}
			else
			{
				AddCode(codes, treeNode.Left, p + 1, list); // traverse the tree recursively
				AddCode(codes, treeNode.Right, p + 1, list);
			}
			if (list.Count > 0)
			{
				list.RemoveAt(list.Count - 1);
			}
		}

		private ushort GetCodeValue(List<bool> list)
		{
			// because we're using ushort to store code value
			if (list.Count > 16)
			{
				throw new Exception("length of code is too long, try some bigger data type to store code");
			}
			ushort value = 0;
			// convert bits to ushort
			// high bits first
			// e.g.
			// 101
			// ushortValue = 1 << 2 + 0 << 1 + 1 << 0
			for (int i = 0; i < list.Count; i++)
			{
				value += (ushort)((Convert.ToUInt16(list[i]) << (list.Count - 1 - i)));
			}
			return value;
		}

		#endregion

		public override void Decompress(BinaryReader reader, BinaryWriter writer)
		{
			ByteNode[] table = ByteNode.Serializer.Deserialize(reader.BaseStream);
			CodeItem[] codes = GetCodes(table);
			foreach (CodeItem ci in codes)
			{
				if (ci != null)
				{
					ci.BitCount = ci.CodeBits.Count;
				}
			}
			byte complementaryBitsLength = reader.ReadByte();
			BitReader bitReader = new BitReader(reader);
			bitReader.ComplementaryBitsLength = complementaryBitsLength;
			List<bool> buffer = new List<bool>();
			byte byteValue;
			int lastPercent = 0;
			buffer.Add(bitReader.ReadBit());
			while (bitReader.HasMore)
			{
				lastPercent = RaiseEvent(reader, lastPercent);
				if (Decode(buffer, codes, out byteValue))
				{
					writer.Write(byteValue);
					buffer.Clear();
				}
				buffer.Add(bitReader.ReadBit());
			}
			// decode the last bits
			if (Decode(buffer, codes, out byteValue))
			{
				writer.Write(byteValue);
				buffer.Clear();
			}
			RaiseFinishEvent();
		}

		#region Decompress Helper Method

		private bool Decode(IList<bool> buffer, CodeItem[] codes, out byte byteValue)
		{
			ushort bufferValue = ToUInt16(buffer);
			int bufferBitCount = buffer.Count;
			foreach (CodeItem ci in codes)
			{
				if (ci != null && ci.CodeValue == bufferValue && ci.BitCount == bufferBitCount)
				{
					for (int i = 0; i < ci.BitCount; i++)
					{
						if (ci.CodeBits[i] != buffer[i])
						{
							break;
						}
						if (i == ci.BitCount - 1)
						{
							byteValue = ci.ByteValue;
							return true;
						}
					}
				}
			}
			byteValue = 0;
			return false;
		}

		private ushort ToUInt16(IList<bool> allBits)
		{
			ushort value = 0;
			// convert bits to byte
			// e.g.
			// 0101 1010
			// value = 0 << 7 + 1 << 6 + ... + 1 << 1 + 0 << 0
			for (int k = 0; k < allBits.Count; k++)
			{
				value += (ushort)(Convert.ToUInt16(allBits[k]) << (allBits.Count - k - 1));
			}
			return value;
		}

		#endregion
	}
}
