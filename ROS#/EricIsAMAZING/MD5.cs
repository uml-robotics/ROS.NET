using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace EricIsAMAZING
{
    public class MD5
    {
        public byte[] Hash;
        public MD5(byte[] data)
        {
            Hash = System.Security.Cryptography.MD5.Create().ComputeHash(data);
        }
        public MD5(string data) : this(Encoding.ASCII.GetBytes(data))
        {
        }
        public override string ToString()
        {
            if (Hash == null) return "";
            return (Encoding.ASCII.GetString(Hash).Replace("-",""));
        }
    }
}
