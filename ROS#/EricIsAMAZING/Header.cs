#region USINGZ

using System;
using System.Collections;
using System.Text;

#endregion

namespace EricIsAMAZING
{
    public class Header
    {
        public IDictionary Values = new Hashtable();

        public bool Parse(byte[] buffer, int size, ref string error_msg)
        {
            int i = 0;
            while (i < buffer.Length)
            {
                int thispiece = BitConverter.ToInt32(buffer, i);
                i += 4;
                byte[] line = new byte[thispiece];
                Array.Copy(buffer, i, line, 0, thispiece);
                string[] chunks = Encoding.ASCII.GetString(line).Split('=');
                if (chunks.Length != 2)
                {
                    error_msg = "A LINE DOES NOT CONTAIN TWO CHUNKS!";
                    return false;
                }
                Values[chunks[0].Trim()] = chunks[1].Trim();
                i += thispiece;
            }
            return (i == size);
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
                byte[] key = Encoding.ASCII.GetBytes((string) k);
                byte[] val = Encoding.ASCII.GetBytes(dict[k].ToString());
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

        public static byte[] ByteLength(int num)
        {
            return ByteLength((uint) num);
        }

        public static byte[] ByteLength(uint num)
        {
            return BitConverter.GetBytes(num);
        }
    }
}