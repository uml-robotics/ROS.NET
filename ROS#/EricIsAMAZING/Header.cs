using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using SerializationHelper = Messages.SerializationHelper;

namespace EricIsAMAZING
{
    public class Header
    {
        public IDictionary Values = new Hashtable();

        public bool Parse(byte[] buffer, ref string error_msg)
        {
            int i = 0;
            while (i < buffer.Length)
            {
                UInt32 size = BitConverter.ToUInt32(buffer, i);
                i += 4;
                byte[] line = new byte[size];
                Array.Copy(buffer, i, line, 0, size);
                string[] chunks = Encoding.ASCII.GetString(line).Split('=');
                if (chunks.Length != 2)
                    return false;
                Values[chunks[0].Trim()] = chunks[1].Trim();
                i += (int)size;
            }
            return true;
        }
        private static byte[] concat(byte[] a, byte[] b)
        {
            byte[] result;
            result = new byte[a.Length + b.Length];
            Array.Copy(a, result, a.Length);
            Array.Copy(b, 0, result, a.Length, b.Length);
            return result;
        }
        public void Write(IDictionary dict, ref byte[] buffer, ref int totallength)
        {
            buffer = new byte[0];
            totallength = 0;
            foreach (object k in dict.Keys)
            {
                int linelength = 0;
                byte[] key = Encoding.ASCII.GetBytes((string)k);
                byte[] val = Encoding.ASCII.GetBytes((string)dict[k]);
                totallength += val.Length + key.Length + 1 + 4;
                linelength = val.Length + key.Length + 1;
                buffer = concat(buffer, ByteLength(linelength));
                buffer = concat(buffer, key);
                buffer = concat(buffer, Encoding.ASCII.GetBytes("="));
                buffer = concat(buffer, val);
            }
            if (totallength != buffer.Length)
                throw new Exception("FUCKING EXCEPTION!");
        }

        private static byte[] ByteLength(int num)
        {
            return ByteLength((uint)num);
        }

        private static byte[] ByteLength(uint num)
        {
            return BitConverter.GetBytes(num);
        }
    }
}
