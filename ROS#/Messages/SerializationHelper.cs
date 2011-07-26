#region USINGZ

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using YAMLParser;

#endregion

namespace Messages
{
    public static class SerializationHelper
    {
        public static TypedMessage<T> Deserialize<T>(byte[] bytes) where T : class, new()
        {
            return new TypedMessage<T>((T)deserialize(typeof(T), bytes, true));
        }

        public static object deserialize(Type T, byte[] bytes, bool iswhole = false)
        {
            object thestructure = Activator.CreateInstance(T);
            System.Reflection.FieldInfo[] infos = T.GetFields();
            int totallength = BitConverter.ToInt32(bytes, 0);
            int currpos = iswhole ? 4 : 0;
            int currinfo = 0;
            while (currpos < bytes.Length)
            {
                int len = BitConverter.ToInt32(bytes, currpos);
                IntPtr pIP = Marshal.AllocHGlobal(len);
                Marshal.Copy(bytes, currpos, pIP, len);
                if (infos[currinfo].FieldType.ToString().Contains("Messages"))
                {
                    byte[] smallerpiece = new byte[len + 4];
                    Array.Copy(bytes, currpos, smallerpiece, 0, len + 4);
                    infos[currinfo].SetValue(thestructure, deserialize(infos[currinfo].FieldType, smallerpiece));
                }
                else
                    infos[currinfo].SetValue(thestructure, Marshal.PtrToStructure(pIP, infos[currinfo].FieldType));
                currinfo++;
                currpos += len;
            }
            if (iswhole && currpos != totallength + 4)
                throw new Exception("MATH FAIL LOL!");
            return thestructure;
        }


        public static byte[] Serialize<T>(TypedMessage<T> outgoing) where T : class, new()
        {
            if (outgoing.Serialized != null)
                return outgoing.Serialized;
            outgoing.Serialized = SlapChop(outgoing.data.GetType(), outgoing.data);
            return outgoing.Serialized;
        }

        public static byte[] SlapChop(Type T, object t)
        {
            System.Reflection.FieldInfo[] infos = t.GetType().GetFields();
            Queue<byte[]> chunks = new Queue<byte[]>();
            int totallength = 0;
            foreach (System.Reflection.FieldInfo info in infos)
            {
                if (info.Name.Contains("(")) continue;
                byte[] thischunk = NeedsMoreChunks(info.FieldType, info.GetValue(t), (info.GetValue(Activator.CreateInstance(T)) != null));
                chunks.Enqueue(thischunk);
                totallength += thischunk.Length;
            }
#if FALSE
            byte[] wholeshebang = new byte[totallength];
            int currpos = 0;
#else
            byte[] wholeshebang = new byte[totallength + 4]; //THE WHOLE SHEBANG
            byte[] len = BitConverter.GetBytes(totallength);
            Array.Copy(len, 0, wholeshebang, 0, 4);
            int currpos = 4;
#endif
            while (chunks.Count > 0)
            {
                byte[] chunk = chunks.Dequeue();
                Array.Copy(chunk, 0, wholeshebang, currpos, chunk.Length);
                currpos += chunk.Length;
            }
            return wholeshebang;
        }

        public static byte[] NeedsMoreChunks(Type T, object val, bool knownlength)
        {
            byte[] thischunk = null;
            if (!T.IsArray)
            {
                if (T.Namespace.Contains("Message"))
                {
                    IRosMessage msg = null;
                    if (val != null)
                        msg = (IRosMessage)Activator.CreateInstance(typeof(TypedMessage<>).MakeGenericType(T), val);
                    else
                        msg = (IRosMessage)Activator.CreateInstance(typeof(TypedMessage<>).MakeGenericType(T));
                    thischunk = msg.Serialize();
                }
                else if (val is string || T == typeof(string))
                {
                    if (val == null)
                        val = "";
                    byte[] nolen = Encoding.ASCII.GetBytes((string)val);
                    thischunk = new byte[nolen.Length + 4];
                    byte[] bylen2 = BitConverter.GetBytes(nolen.Length);
                    Array.Copy(nolen, 0, thischunk, 4, nolen.Length);
                    Array.Copy(bylen2, thischunk, 4);
                }
                else
                {
                    byte[] temp = new byte[Marshal.SizeOf(T)];
                    GCHandle h = GCHandle.Alloc(temp, GCHandleType.Pinned);
                    Marshal.StructureToPtr(val, h.AddrOfPinnedObject(), false);
                    h.Free();
                    thischunk = new byte[temp.Length + (knownlength ? 0 : 4)];
                    if (!knownlength)
                    {
                        byte[] bylen = BitConverter.GetBytes(temp.Length);
                        Array.Copy(bylen, 0, thischunk, 0, 4);
                    }
                    Array.Copy(temp, 0, thischunk, (knownlength ? 0 : 4), temp.Length);

                }
            }
            else
            {
                int arraylength = 0;
                List<object> valslist = new List<object>();
                foreach (object o in (val as Array))
                {
                    valslist.Add(o);
                }
                object[] vals = valslist.ToArray();
                Queue<byte[]> arraychunks = new Queue<byte[]>();
                for (int i = 0; i < vals.Length; i++)
                {
                    Type TT = vals[i].GetType();
#if arraypiecesneedlengthtoo
                    byte[] chunkwithoutlen = NeedsMoreChunks(TT, vals[i]);
                    byte[] chunklen = BitConverter.GetBytes(chunkwithoutlen.Length);
                    byte[] chunk = new byte[chunkwithoutlen.Length + 4];
                    Array.Copy(chunklen, 0, chunk, 0, 4);
                    Array.Copy(chunkwithoutlen, 0, chunk, 4, chunkwithoutlen.Length);
#else
                    byte[] chunk = NeedsMoreChunks(TT, vals[i], true);
#endif
                    arraychunks.Enqueue(chunk);
                    arraylength += chunk.Length;
                }
                thischunk = new byte[knownlength ? arraylength : (arraylength + 4)];
                if (!knownlength)
                {
                    byte[] bylen = BitConverter.GetBytes(vals.Length);
                    Array.Copy(bylen, 0, thischunk, 0, 4);
                }
                int arraypos = knownlength ? 0 : 4;
                while (arraychunks.Count > 0)
                {
                    byte[] chunk = arraychunks.Dequeue();
                    Array.Copy(chunk, 0, thischunk, arraypos, chunk.Length);
                    arraypos += chunk.Length;
                }
            }
            return thischunk;
        }
    }

    public class TypedMessage<M> : IRosMessage where M : class, new()
    {
        public M data = new M();

        public TypedMessage()
            : base((MsgTypes)Enum.Parse(typeof(MsgTypes), typeof(M).FullName.Replace("Messages.", "").Replace(".", "__")),
                   TypeHelper.TypeInformation[(MsgTypes)Enum.Parse(typeof(MsgTypes), typeof(M).FullName.Replace("Messages.", "").Replace(".", "__"))].MessageDefinition,
                   TypeHelper.TypeInformation[(MsgTypes)Enum.Parse(typeof(MsgTypes), typeof(M).FullName.Replace("Messages.", "").Replace(".", "__"))].IsMetaType)
        {
        }

        public TypedMessage(M d)
        {
            data = d;
            base.type = (MsgTypes)Enum.Parse(typeof(MsgTypes), typeof(M).FullName.Replace("Messages.", "").Replace(".", "__"));
            base.MessageDefinition = TypeHelper.TypeInformation[(MsgTypes)Enum.Parse(typeof(MsgTypes), typeof(M).FullName.Replace("Messages.", "").Replace(".", "__"))].MessageDefinition;
            base.IsMeta = TypeHelper.TypeInformation[(MsgTypes)Enum.Parse(typeof(MsgTypes), typeof(M).FullName.Replace("Messages.", "").Replace(".", "__"))].IsMetaType;
        }

        public TypedMessage(byte[] SERIALIZEDSTUFF)
        {
            Deserialize(SERIALIZEDSTUFF);
        }

        public override void Deserialize(byte[] SERIALIZEDSTUFF)
        {
            data = SerializationHelper.Deserialize<M>(SERIALIZEDSTUFF).data;
        }

        public override byte[] Serialize()
        {
            return SerializationHelper.Serialize(this);
        }
    }

    public class IRosMessage
    {
        public bool HasHeader;
        public bool IsMeta;
        public bool KnownSize = true;

        public string MessageDefinition;

        public byte[] Serialized;
        public IDictionary connection_header;
        public MsgTypes type;

        public IRosMessage()
            : this(MsgTypes.Unknown, "", false)
        {
        }

        public IRosMessage(MsgTypes t, string def, bool meta)
        {
            type = t;
            MessageDefinition = def;
            IsMeta = meta;
        }

        public IRosMessage(byte[] SERIALIZEDSTUFF)
        {
            Deserialize(SERIALIZEDSTUFF);
        }

        public virtual void Deserialize(byte[] SERIALIZEDSTUFF)
        {
            throw new NotImplementedException();
        }

        public virtual byte[] Serialize()
        {
            return null;
        }
    }

    public class TypeInfo
    {
        public MsgTypes Type = MsgTypes.Unknown;
        public string MessageDefinition = "";
        public bool IsMetaType;
        public Dictionary<string, MsgFieldInfo> Fields;
        public TypeInfo(MsgTypes t, bool meta, string def, Dictionary<string, MsgFieldInfo> fields)
        {
            Type = t;
            MessageDefinition = def;
            IsMetaType = meta;
            Fields = fields;
        }
        public static string Generate(string name, string ns, bool meta, List<string> defs, List<SingleType> types)
        {
            string def = "";
            foreach (string d in defs) def += d+"\n";
            def = def.Trim('\n');
            string ret = string.Format("MsgTypes.{0}{1}, new TypeInfo(MsgTypes.{0}{1}, {2}, \n@\"{3}\",\n\t\t\t\t new Dictionary<string, MsgFieldInfo>{{\n", (ns.Length > 0 ? (ns + "__") : ""), name, meta.ToString().ToLower(), def);
            for (int i = 0; i < types.Count; i++)
            {
                ret += "\t\t\t\t\t{\"" + types[i].Name + "\", " + MsgFieldInfo.Generate(types[i]) + "}";
                if (i < types.Count - 1)
                    ret += ",\n";
            }
            return ret + "\n\t\t\t})";
        }
    }

    public class MsgFieldInfo
    {
        public string Name;
        public Type Type;
        public bool IsArray;
        public bool IsLiteral;
        public bool IsMetaType;
        public bool IsConst;
        public string ConstVal;
        public List<int> Lengths = new List<int>();
        public MsgFieldInfo(string name, bool isliteral, Type type, bool isconst, string constval, bool isarray, string lengths, bool meta)
        {
            Name = name;
            IsArray = isarray;
            Type = type;
            IsLiteral = isliteral;
            IsMetaType = meta;
            IsConst = isconst;
            ConstVal = constval;
            if (lengths == null) return;
            if (lengths.Length > 0)
            {
                if (!lengths.Contains(","))
                    Lengths.Add(int.Parse(lengths));
                else
                {
                    string[] chunks = lengths.Split(',');
                    foreach (string s in chunks)
                    {
                        string trimmed = s.Trim();
                        if (trimmed.Length > 0) Lengths.Add(int.Parse(trimmed));
                    }
                }
            }
        }
        public static string Generate(SingleType members)
        {
            return string.Format("new MsgFieldInfo(\"{0}\", {1}, {2}, {3}, \"{4}\", {5}, \"{6}\", {7})",
                members.Name,
                members.IsLiteral.ToString().ToLower(),
                (members.IsLiteral ? ("typeof(" + members.Type + ")") : ("typeof(TypedMessage<" + members.Type + ">)")),
                members.Const.ToString().ToLower(),
                members.ConstValue,
                members.IsArray.ToString().ToLower(),
                members.lengths,
                //FIX MEEEEEEEE
                members.meta.ToString().ToLower());
        }
    }

    public class MsgsFile
    {
        public string GeneratedDictHelper;
        List<string> def = new List<string>();
        private bool HasHeader;
        public string Name;
        public string Namespace = "Messages";
        public List<SingleType> Stuff = new List<SingleType>();
        public string backhalf;
        public string classname;
        public string fronthalf;
        private string memoizedcontent;
        private bool meta;
        public string dimensions = "";

        public MsgsFile(string filename)
        {
            if (!filename.Contains(".msg"))
                throw new Exception("" + filename + " IS NOT A VALID MSG FILE!");
            string[] sp = filename.Replace("ROS_MESSAGES", "").Replace(".msg", "").Split('\\');
            classname = sp[sp.Length - 1];
            Namespace += "." + filename.Replace("ROS_MESSAGES", "").Replace(".msg", "");
            Namespace = Namespace.Replace("\\", ".").Replace("..", ".");
            string[] sp2 = Namespace.Split('.');
            Namespace = "";
            for (int i = 0; i < sp2.Length - 2; i++)
                Namespace += sp2[i] + ".";
            Namespace += sp2[sp2.Length - 2];
            //THIS IS BAD!
            classname = classname.Replace("/", ".");
            Name = Namespace.Replace("Messages", "").TrimStart('.') + "." + classname;
            Name = Name.TrimStart('.');
            classname = Name.Split('.').Length > 1 ? Name.Split('.')[1] : Name;
            Namespace = Namespace.Trim('.');
            List<string> lines = new List<string>(File.ReadAllLines(filename));
            lines = lines.Where((st) => (!st.Contains('#') || st.Split('#')[0].Length != 0)).ToList();
            for (int i = 0; i < lines.Count; i++)
                lines[i] = lines[i].Split('#')[0].Trim();
            lines = lines.Where((st) => (st.Length > 0)).ToList();
            def = new List<string>();
            for (int i = 0; i < lines.Count; i++)
            {
                def.Add(lines[i]);
                SingleType test = KnownStuff.WhatItIs(lines[i]);
                if (test != null)
                    Stuff.Add(test);
            }
        }
        public override string ToString()
        {
            bool wasnull = false;
            if (fronthalf == null)
            {
                wasnull = true;
                fronthalf = "";
                backhalf = "";
                string[] lines = File.ReadAllLines("TemplateProject\\PlaceHolder._cs");
                bool hitvariablehole = false;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains("$$DOLLADOLLABILLS"))
                    {
                        hitvariablehole = true;
                        continue;
                    }
                    if (lines[i].Contains("namespace"))
                    {
                        fronthalf += "using Messages.std_msgs;\nusing Messages.geometry_msgs;\nusing Messages.nav_msgs;\n\n";
                        fronthalf += "namespace " + Namespace + "\n";
                        continue;
                    }
                    if (!hitvariablehole)
                        fronthalf += lines[i] + "\n";
                    else
                        backhalf += lines[i] + "\n";
                }
            }

            if (memoizedcontent == null)
            {
                memoizedcontent = "";
                for (int i = 0; i < Stuff.Count; i++)
                {
                    SingleType thisthing = Stuff[i];
                    if (thisthing.Type == "Header") HasHeader = true;
                    meta |= thisthing.meta;
                    memoizedcontent += "\t" + thisthing.output + "\n";
                }
                string ns = Namespace.Replace("Messages.", "");
                if (ns == "Messages")
                    ns = "";
                GeneratedDictHelper = TypeInfo.Generate(classname, ns, meta, def, Stuff);
            }
            if (wasnull)
            {
            }
            string ret = fronthalf +"\n\t\tpublic class " +classname +"\n\t\t{\n" +memoizedcontent + "\t\t}" +"\n" + backhalf;
            return ret;
        }

        public void Write()
        {
            string outdir = Program.outputdir;
            string[] chunks = Name.Split('.');
            for (int i = 0; i < chunks.Length - 1; i++)
                outdir += "\\" + chunks[i];
            if (!Directory.Exists(outdir))
                Directory.CreateDirectory(outdir);
            File.WriteAllText(outdir + "\\" + classname + ".cs", ToString());
        }
    }

    public static class KnownStuff
    {
        public static Dictionary<string, string> KnownTypes = new Dictionary<string, string>
                                                                  {
                                                                      {"float64", "double"},
                                                                      {"float32", "double"},
                                                                      {"uint64", "ulong"},
                                                                      {"uint32", "uint"},
                                                                      {"uint16", "ushort"},
                                                                      {"uint8", "byte"},
                                                                      {"int64", "long"},
                                                                      {"int32", "int"},
                                                                      {"int16", "short"},
                                                                      {"int8", "sbyte"},
                                                                      {"byte", "byte"},
                                                                      {"bool", "bool"},
                                                                      {"string", "string"},
                                                                      {"time", "ulong"},
                                                                      {"duration", "ulong"},
                                                                      {"char", "char"}
                                                                  };

        public static SingleType WhatItIs(string s)
        {
            string[] pieces = s.Split('/');
            if (pieces.Length > 1)
            {
                s = pieces[pieces.Length - 1];
            }
            return WhatItIs(new SingleType(s));
        }

        public static SingleType WhatItIs(SingleType t)
        {
            foreach (KeyValuePair<string, string> test in KnownTypes)
            {
                if (t.Test(test))
                {
                    t.rostype = t.Type;
                    return t.Finalize(test);
                }
            }
            return t.Finalize(t.input.Split(' '), false);
        }
    }

    public class SingleType
    {
        public bool IsArray;
        public bool IsLiteral;
        public bool Const;
        public string ConstValue = "";
        public string Name;
        public string Type;
        public string input;
        public string lengths = "";
        public bool meta;
        public string output;
        public string rostype = "";
        public SingleType(string s)
        {
            if (s.Contains('[') && s.Contains(']'))
            {
                string front = "";
                string back = "";
                string[] parts = s.Split('[');
                front = parts[0];
                parts = parts[1].Split(']');
                lengths = parts[0];
                back = parts[1];
                IsArray = true;
                s = front + back;
            }
            input = s;
        }

        public bool Test(KeyValuePair<string, string> candidate)
        {
            return (input.Split(' ')[0].ToLower().Equals(candidate.Key));
        }

        public SingleType Finalize(KeyValuePair<string, string> csharptype)
        {
            string[] PARTS = input.Split(' ');
            rostype = PARTS[0];
            if (!KnownStuff.KnownTypes.ContainsKey(rostype))
                meta = true;
            PARTS[0] = csharptype.Value;
            return Finalize(PARTS, true);
        }

        public SingleType Finalize(string[] s, bool isliteral)
        {
            bool isconst = false;
            IsLiteral = isliteral;
            string type = s[0];
            string name = s[1];
            string othershit = "";
            if (name.Contains('='))
            {
                string[] parts = name.Split('=');
                isconst = true;
                name = parts[0];
                othershit = " = " + parts[1];
            }
            for (int i = 2; i < s.Length; i++)
                othershit += " " + s[i];
            if (othershit.Contains('=')) isconst = true;
            if (!IsArray)
            {
                if (othershit.Contains('=') && type == "string")
                {
                    othershit = othershit.Replace("\\", "\\\\");
                    othershit = othershit.Replace("\"", "\\\"");
                    string[] split = othershit.Split('=');
                    othershit = split[0] + " = \"" + split[1] + "\"";
                }
                if (othershit.Contains('=') && type == "bool")
                {
                    othershit = othershit.Replace("0", "false").Replace("1", "true");
                }
                if (othershit.Contains('=') && type == "byte")
                {
                    othershit = othershit.Replace("-1", "255");
                }
                output = "\t\tpublic " + (isconst ? "const " : "") + type + " " + name + othershit + ";";
                Const = isconst;
                if (othershit.Contains("="))
                {
                    string[] chunks = othershit.Split('=');
                    ConstValue = chunks[chunks.Length - 1].Trim(); ;
                }
            }
            else
            {
                if (lengths.Length > 0)
                {
                    IsLiteral = type != "string";
                    string commas = "";
                    for (int i = 0; i < lengths.Count((c) => c == ','); i++) commas += ",";
                    output = "\t\tpublic " + type + "[" + commas + "] " + name + " = new " + type + "[" + lengths + "];";
                }
                else
                    output = "\t\tpublic " + "" + type + "[] " + name + othershit + ";";
                if (othershit.Contains('='))
                {
                    string[] split = othershit.Split('=');
                    othershit = split[0] + " = (" + type + ")" + split[1];
                }
            }
            Type = type;
            if (!KnownStuff.KnownTypes.ContainsKey(rostype))
                meta = true;
            if (name.Length == 0)
                Name = othershit.Trim();
            else
                Name = name;
            return this;
        }
    }
}