#region USINGZ

using System;
using System.Reflection;
using System.Text;
using Messages;
using String = Messages.std_msgs.String;
using System.Collections.Generic;

#endregion

namespace Ros_CSharp
{
    public static class MD5
    {
        public static string Sum(MsgTypes m)
        {
            string hashme = TypeHelper.TypeInformation[m].MessageDefinition.Trim('\n', '\t', '\r', ' ');
            while (hashme.Contains("  "))
                hashme = hashme.Replace("  ", " ");
            while (hashme.Contains("\r\n"))
                hashme = hashme.Replace("\r\n", "\n");
            hashme = hashme.Trim();
            string[] lines = hashme.Split('\n');

            //this shit is bananas.
            Queue<string> haves = new Queue<string>(), havenots = new Queue<string>();
            foreach (string l in lines) if (l.Contains("=")) haves.Enqueue(l); else havenots.Enqueue(l); hashme = "";            
            while(haves.Count + havenots.Count > 0) hashme += (haves.Count > 0 ? haves.Dequeue() : havenots.Dequeue()) + (haves.Count + havenots.Count >= 1 ? "\n" : "");

            IRosMessage irm =
                (IRosMessage)
                Activator.CreateInstance(
                    typeof (TypedMessage<>).MakeGenericType(TypeHelper.TypeInformation[m].Type.GetGenericArguments()));
            if (irm.IsMeta)
            {
                Type t = irm.GetType().GetGenericArguments()[0];
                FieldInfo[] fields = t.GetFields();
                for (int i = 0; i < fields.Length; i++)
                {
                    Type FieldType = fields[i].FieldType;
                    if (!FieldType.Namespace.Contains("Messages")) continue;
                    while (FieldType.IsArray || FieldType.Name.Contains("["))
                    {
                        object[] o;
                        if (FieldType.Name.Contains("String"))
                            FieldType = typeof (String);
                        else
                        {
                            o = (object[]) Activator.CreateInstance(FieldType);
                            FieldType = o.GetType();
                        }
                    }
                    MsgTypes T =
                        (MsgTypes)
                        Enum.Parse(typeof (MsgTypes), FieldType.FullName.Replace("Messages.", "").Replace(".", "__"));
                    if (!TypeHelper.TypeInformation.ContainsKey(T))
                        throw new Exception("SOME SHIT BE FUCKED!");
                    hashme = hashme.Replace(FieldType.Name, Sum(T));
                }
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