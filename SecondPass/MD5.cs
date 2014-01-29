#region USINGZ

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using Messages;
using String = Messages.std_msgs.String;
using System.Collections.Generic;

#endregion

namespace Messages
{
    public static class MD5
    {
        public static Dictionary<MsgTypes, string> md5memo = new Dictionary<MsgTypes, string>();
        public static Dictionary<SrvTypes, string> srvmd5memo = new Dictionary<SrvTypes, string>();

        public static string Sum(SrvTypes m)
        {
            if (!srvmd5memo.ContainsKey(m))
            {
                IRosService irm = IRosService.generate(m);
                string req = PrepareToHash(irm.RequestMessage);
                string res = PrepareToHash(irm.ResponseMessage);
                srvmd5memo.Add(m,Sum(req, res));
            }
            return srvmd5memo[m];
        }
        public static string Sum(MsgTypes m)
        {
            if (!md5memo.ContainsKey(m))
            {
                IRosMessage irm = IRosMessage.generate(m);
                string hashme = PrepareToHash(irm);
                md5memo.Add(m, Sum(hashme));
            }
            return md5memo[m];
        }

#region BEWARE ALL YE WHOSE EYES GAZE UPON THESE LINES
        static string PrepareToHash(IRosMessage irm)
        {
            MsgTypes m = irm.msgtype;
            string hashme = irm.MessageDefinition.Trim('\n', '\t', '\r', ' ');
            while (hashme.Contains("  "))
                hashme = hashme.Replace("  ", " ");
            while (hashme.Contains("\r\n"))
                hashme = hashme.Replace("\r\n", "\n");
            hashme = hashme.Trim();
            string[] lines = hashme.Split('\n');

            //this shit is bananas.
            Queue<string> haves = new Queue<string>(), havenots = new Queue<string>();
            for (int i = 0; i < lines.Length; i++)
            {
                string l = lines[i];
                if (l.Contains("=")) haves.Enqueue(l); else havenots.Enqueue(l);
            } hashme = "";
            while (haves.Count + havenots.Count > 0) hashme += (haves.Count > 0 ? haves.Dequeue() : havenots.Dequeue()) + (haves.Count + havenots.Count >= 1 ? "\n" : "");
            /*if (irm.IsServiceComponent)
            {
                object o = irm;
                FieldInfo[] infos = SerializationHelper.GetFields(irm.GetType(), ref o, out irm);
                if (infos.Length == 1 && infos[0].FieldType.FullName.Contains("Messages"))
                {

                }
            }*/
            if (irm.IsMetaType || irm.IsServiceComponent)
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
                        Enum.Parse(typeof(MsgTypes), FieldType.FullName.Replace("Messages.", "").Replace(".", "__"));
                    string[] BLADAMN = hashme.Replace(FieldType.Name, Sum(T)).Split('\n');
                    hashme = "";
                    for (int x = 0; x < BLADAMN.Length; x++)
                    {
                        if (BLADAMN[x].Contains(fields[i].Name))
                        {
                            if (BLADAMN[x].Contains("/"))
                            {
                                BLADAMN[x] = BLADAMN[x].Split('/')[1];
                            }

                            if (BLADAMN[x].Contains("[]") && !irm.Fields[fields[i].Name].IsLiteral)
                            {
                                BLADAMN[x] = BLADAMN[x].Replace("[]", "");
                            }
                        }
                        hashme += BLADAMN[x];
                        if (x < BLADAMN.Length - 1)
                            hashme += "\n";
                    }
                }
            }
            return hashme;
        }
#endregion

        public static string Sum(params string[] str)
        {
            return Sum(str.Select(s => Encoding.ASCII.GetBytes(s)).ToArray());
        }
        public static string Sum(params byte[][] data)
        {
            StringBuilder sb = new StringBuilder();
            System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            if (data.Length > 0)
            {
                for (int i = 0; i < data.Length - 1; i++)
                {
                    md5.TransformBlock(data[i], 0, data[i].Length, data[0], 0);
                }
                md5.TransformFinalBlock(data[data.Length - 1], 0, data[data.Length - 1].Length);
            }
            for(int i=0;i<md5.Hash.Length;i++)
            {
                sb.AppendFormat("{0:x2}", md5.Hash[i]);
            }
            return sb.ToString();
        }
    }
}