/*
	LZWCompression.cs
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
using System.Diagnostics;

namespace Adrian.Compression
{
    public class LZWCompression : Compression
    {
        protected class CodeTable
        {
            private int _codeLength;
            private Dictionary<string, int> _table = new Dictionary<string, int>();

            public CodeTable(int codeLength)
            {
                _codeLength = codeLength;
            }

            public void FillChars()
            {
                for (int a = 0; a < 256; a++)
                {
                    AddString(((char)a).ToString());
                }
            }

            public bool Contains(string str)
            {
                return _table.ContainsKey(str);
            }

            public void AddString(string str)
            {
                if (_table.ContainsKey(str))
                {
                    throw new Exception("Already exists!");
                }
                if (_table.Count >= MaxCode + 1)
                {
                    throw new Exception("Code table is full!");
                }
                _table[str] = _table.Count;
            }

            public int GetCode(string str)
            {
                return _table[str];
            }

            private int MaxCode // legal code range: 0 ~ MaxCode
            {
                get { return (1 << (8 * _codeLength)) - 1; }
            }

            public bool HasSpace
            {
                get { return _table.Count < MaxCode + 1; }
            }
        }

        private int _codeLength = 2;

        public LZWCompression() { }
        public LZWCompression(int codeLength)
        {
            if (_codeLength < 1)
            {
                throw new Exception("codeLength must be greater than 0");
            }
            _codeLength = codeLength;
        }

        public override void Compress(BinaryReader reader, BinaryWriter writer)
        {
            CodeTable tb = new CodeTable(_codeLength);
            tb.FillChars();
            char firstChar = (char)reader.ReadByte();
            string match = firstChar.ToString();
            int lastPercent = 0;
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                lastPercent = RaiseEvent(reader, lastPercent);
                char nextChar = (char)reader.ReadByte();
                string nextMatch = match + nextChar.ToString();
                if (tb.Contains(nextMatch))
                {
                    if (tb.HasSpace)
                    {
                        match = nextMatch;
                    }
                    else
                    {
                        WriteCode(writer, tb.GetCode(nextMatch));
                        if (reader.BaseStream.Position < reader.BaseStream.Length)
                        {
                            match = ((char)reader.ReadByte()).ToString();
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                else
                {
                    WriteCode(writer, tb.GetCode(match));
                    if (tb.HasSpace)
                    {
                        tb.AddString(nextMatch);
                    }
                    match = nextChar.ToString();
                }
                Debug.Assert(writer.BaseStream.Position % 2 == 0);
            }
            WriteCode(writer, tb.GetCode(match));
            RaiseFinishEvent();
        }

        public override void Decompress(BinaryReader reader, BinaryWriter writer)
        {
            List<string> list = new List<string>();
            for (int i = 0; i < 256; i++)
            {
                list.Add(((char)i).ToString());
            }
            byte firstByte = (byte)ReadCode(reader);
            string match = ((char)firstByte).ToString();
            writer.Write(firstByte);
            int lastPercent = 0;
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                lastPercent = RaiseEvent(reader, lastPercent);
                int nextCode = ReadCode(reader);
                string nextMatch = null;
                if (nextCode < list.Count)
                {
                    nextMatch = list[nextCode];
                }
                else
                {
                    nextMatch = match + match[0];
                }
                foreach(char c in nextMatch)
                {
                    writer.Write((byte)c);
                }
                list.Add(match + nextMatch[0]);
                match = nextMatch;
            }
            RaiseFinishEvent();
        }

        private void WriteCode(BinaryWriter writer, int code)
        {
            int shift = 0;
            for (int i = 0; i < _codeLength; i++)
            {
                // write lower byte first
                byte b = (byte)((code >> shift) & 0xFF); 
                writer.Write(b);
                shift += 8;
            }
        }

        private int ReadCode(BinaryReader reader)
        {
            int code = 0;
            int shift = 0;
            for (int i = 0; i < _codeLength; i++)
            {
                // read lower byte first
                code += (reader.ReadByte() << shift);
                shift += 8; 
            }
            return code;
        }

        public int CodeLength
        {
            get { return _codeLength; }
        }
    }
}
