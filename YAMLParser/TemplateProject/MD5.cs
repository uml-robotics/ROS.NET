#region USINGZ

using System;
using System.Reflection;
using System.Text;
using Messages;
using String = Messages.std_msgs.String;
using System.Collections.Generic;

#endregion

namespace Messages
{
    public static class MD5
    {
        public static string Sum(MsgTypes m)
        {
            string hashme = IRosMessage.generate(m).MessageDefinition.Trim('\n', '\t', '\r', ' ');
            while (hashme.Contains("  "))
                hashme = hashme.Replace("  ", " ");
            while (hashme.Contains("\r\n"))
                hashme = hashme.Replace("\r\n", "\n");
            hashme = hashme.Trim();
            string[] lines = hashme.Split('\n');

            //this shit is bananas.
            Queue<string> haves = new Queue<string>(), havenots = new Queue<string>();
            for (int i=0;i<lines.Length;i++)
            {
                string l = lines[i];
                if (l.Contains("=")) haves.Enqueue(l); else havenots.Enqueue(l);
            } hashme = "";            
            while(haves.Count + havenots.Count > 0) hashme += (haves.Count > 0 ? haves.Dequeue() : havenots.Dequeue()) + (haves.Count + havenots.Count >= 1 ? "\n" : "");
            IRosMessage irm = IRosMessage.generate(m);
            if (irm.IsMetaType)
            {
                Type t = irm.GetType();
                object o = irm;
                FieldInfo[] fields = SerializationHelper.GetFields(t, ref o, out irm); ;
                for (int i = 0; i < fields.Length; i++)
                {
                    Type FieldType = fields[i].FieldType;
                    if (!FieldType.Namespace.Contains("Messages")) continue;
                    while (FieldType.IsArray) FieldType = FieldType.GetElementType();
                    MsgTypes T =
                        (MsgTypes)
                        Enum.Parse(typeof (MsgTypes), FieldType.FullName.Replace("Messages.", "").Replace(".", "__"));
                    string[] BLADAMN = hashme.Replace(FieldType.Name, Sum(T)).Split('\n');
                    hashme = "";
                    for (int x = 0; x < BLADAMN.Length; x++)
                    {
                        if (BLADAMN[x].Contains("/"))
                        {
                            BLADAMN[x] = BLADAMN[x].Split('/')[1];
                            BLADAMN[x] = BLADAMN[x].Replace("[]", "");
                        }
                        
                        hashme += BLADAMN[x];
                        if (x < BLADAMN.Length - 1)
                            hashme += "\n";
                    }
                }
                Console.WriteLine("\t"+hashme);
                return Sum(hashme);
            }
            return Sum(hashme);
        }

        public static string Sum(string str)
        {
            return Sum(Encoding.ASCII.GetBytes(str));
        }

        public static string Sum(byte[] data)
        {
            string s = "";
            byte[] sum = System.Security.Cryptography.MD5.Create().ComputeHash(data);
            foreach (byte b in sum)
            {
                if (b < 16)
                    s += "0";
                s += b.ToString("x");
            }
            return s.TrimEnd(' ', '\t', '\n');
        }
    }
}