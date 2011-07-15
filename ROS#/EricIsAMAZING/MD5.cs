using System;
using System.Text;

namespace EricIsAMAZING
{
    public class MD5
    {
        public byte[] Sum;
        public MD5(byte[] data)
        {
            Sum = System.Security.Cryptography.MD5.Create().ComputeHash(data);
        }
        public MD5(string data) : this(Encoding.ASCII.GetBytes(data))
        {
        }
        public override string ToString()
        {
            if (Sum == null) return "";
            string s = "";
            foreach (byte b in Sum)
            {
                s += b.ToString("x");
            }
            return s;
        }
    }
}
