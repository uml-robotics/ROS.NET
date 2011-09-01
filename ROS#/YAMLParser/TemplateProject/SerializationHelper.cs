#region USINGZ

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using String = Messages.std_msgs.String;

#endregion

namespace Messages
{
    public static class SerializationHelper
    {
        [System.Diagnostics.DebuggerStepThrough]
        public static MsgTypes GetMessageType(Type t)
        {
            return GetMessageType(t.FullName);
        }
        [System.Diagnostics.DebuggerStepThrough]
        public static MsgTypes GetMessageType(string s)
        {
            if (s.Contains("TypedMessage`1["))
                s = Type.GetType(s).GetField("data").FieldType.FullName;
            if (!s.Contains("Messages"))
                return MsgTypes.Unknown;
            return (MsgTypes)Enum.Parse(typeof(MsgTypes), s.Replace("Messages.", "").Replace(".", "__"));
        }

        public static TypedMessage<T> Deserialize<T>(byte[] bytes) where T : class, new()
        {
            if (typeof(T).FullName.Contains("TypedMessage"))
                Console.WriteLine("TYPE FAIL!");
            return new TypedMessage<T>((T)deserialize(typeof(T), bytes, IsSizeKnown(TypeHelper.TypeInformation[GetMessageType(typeof(T))].Type, false)));
        }
        [System.Diagnostics.DebuggerStepThrough]
        public static Type GetType(string s)
        {
            MsgTypes mt = GetMessageType(s);
            if (mt == MsgTypes.Unknown)
                return default(Type);
            return TypeHelper.TypeInformation[mt].Type;
        }

        public static object deserialize(Type T, byte[] bytes, bool sizeknown = false)
        {
            object thestructure = Activator.CreateInstance(T);
            try
            {
                FieldInfo[] infos = T.GetFields();
                int totallength = sizeknown ? bytes.Length : BitConverter.ToInt32(bytes, 0);
                int currpos = sizeknown ? 0 : 4;
                int currinfo = 0;
                Console.WriteLine("DESERIALIZING:\t" + Encoding.ASCII.GetString(bytes));
                while (currpos < bytes.Length && currinfo < infos.Length)
                {
                    Type type = TypeHelper.TypeInformation[GetMessageType(T)].Fields[infos[currinfo].Name].Type;
                    Type realtype = infos[currinfo].FieldType;
                    MsgTypes msgtype = GetMessageType(type);
                    bool knownpiecelength = IsSizeKnown(type, false) && (!TypeHelper.TypeInformation[GetMessageType(T)].Fields[infos[currinfo].Name].IsArray || TypeHelper.TypeInformation[GetMessageType(T)].Fields[infos[currinfo].Name].Lengths.Count != 0);
                    if (knownpiecelength)
                    {
                        if (realtype.IsArray && msgtype != MsgTypes.std_msgs__String) //must have length defined, or else knownpiecelength would be false... so look it up in the dict!
                        {
                            object val;
                            //if (TypeHelper.TypeInformation[GetMessageType(T)].Fields[infos[currinfo].Name].Lengths.Count != 1)
                            //    throw new Exception("AMG MULTIDIM FAIL!");
                            Type TT = realtype.GetElementType();
                            if (TT.IsArray)
                                throw new Exception("ERIC, YOU NEED TO MAKE DESERIALIZATION RECURSE!!!");
                            Array vals = (infos[currinfo].GetValue(thestructure) as Array);
                            if (vals != null)
                            {
                                bool b;
                                b = IsSizeKnown(TT, false);
                                for (int i = 0; i < vals.Length; i++)
                                {
                                    MsgTypes mt = GetMessageType(TT);
                                    int leng = 0;
                                    if (mt == MsgTypes.Unknown)
                                        leng = Marshal.SizeOf(TT);
                                    if (leng == 0)
                                        throw new Exception("LENGTH ENUMERATION FAIL IN DESERIALIZE!");
                                    IntPtr pIP = Marshal.AllocHGlobal(leng);
                                    Marshal.Copy(bytes, currpos, pIP, leng);
                                    vals.SetValue(Marshal.PtrToStructure(pIP, TT), i);
                                    currpos += leng;
                                    if (currpos >= bytes.Length)
                                        break; //hopefully we're done by now O.o
                                }
                            }
                            infos[currinfo].SetValue(thestructure, vals);
                        }
                        else
                        {
                            int len = Marshal.SizeOf(infos[currinfo].GetValue(thestructure));
                            IntPtr pIP = Marshal.AllocHGlobal(len);
                            Marshal.Copy(bytes, currpos, pIP, len);
                            infos[currinfo].SetValue(thestructure, Marshal.PtrToStructure(pIP, infos[currinfo].FieldType));
                            currpos += len;
                        }
                    }
                    else
                    {
                        Type ft = infos[currinfo].FieldType;
                        if (ft.IsArray)
                        {
                            /*if (ft.GetElementType() == typeof(Messages.std_msgs.String))
                            {
                                Console.WriteLine("ARRAYS OF STRINGS w/ UNKNOWN LENGTH? YOU OUTCHO MIND?!");
                                return thestructure;
                            }
                            else
                            {*/
                            Array val = infos[currinfo].GetValue(thestructure) as Array;
                            int chunklen = BitConverter.ToInt32(bytes, currpos);
                            currpos += 4;
                            Type TT = ft.GetElementType();
                            if (TT == null)
                                throw new Exception("LENGTHLESS ARRAY FAIL -- ELEMENT TYPE IS NULL!");
                            if (TT == typeof(string) || TT.FullName.Contains("Message."))
                                throw new Exception("NOT YET, YOUNG PATAWAN");
                            bool knownlength = IsSizeKnown(TT, true);
                            if (TT.FullName != null && TT.FullName.Contains("Message"))
                            {
                                if (TypeHelper.TypeInformation[GetMessageType(T)].Fields[infos[currinfo].Name].Lengths.Count > 0)
                                {
                                    if (TypeHelper.TypeInformation[GetMessageType(T)].Fields[infos[currinfo].Name].Lengths.Count > 1)
                                        throw new Exception("MULTIDIMS NOT HANDLED YET!");
                                    currpos -= 4;
                                    chunklen = TypeHelper.TypeInformation[GetMessageType(T)].Fields[infos[currinfo].Name].Lengths[0];
                                    Array chunks = Array.CreateInstance(TT, chunklen);
                                    for (int i = 0; i < chunklen; i++)
                                    {
                                        IRosMessage msg = (IRosMessage)Activator.CreateInstance(typeof(TypedMessage<>).MakeGenericType(TypeHelper.TypeInformation[GetMessageType(TT)].Type.GetGenericArguments()));
                                        int len = BitConverter.ToInt32(bytes, currpos);
                                        byte[] chunk = new byte[len + 4];
                                        Array.Copy(bytes, currpos, chunk, 0, len + 4);
                                        msg.Deserialize(chunk);
                                        object data = msg.GetType().GetField("data").GetValue(msg);
                                        chunks.SetValue(data, i);
                                        currpos += len + 4;
                                    }
                                    infos[currinfo].SetValue(thestructure, chunks);
                                }
                                else
                                {
                                    Console.WriteLine("UNKNOWN LENGTH!");
                                }
                            }
                            else
                            {
                                int len = Marshal.SizeOf(TT);
                                val = Array.CreateInstance(TT, chunklen);
                                for (int i = 0; i < chunklen * len; i += len)
                                {
                                    IntPtr pIP = Marshal.AllocHGlobal(len);
                                    Marshal.Copy(bytes, currpos + i, pIP, len);
                                    val.SetValue(Marshal.PtrToStructure(pIP, TT), i / len);
                                }
                                infos[currinfo].SetValue(thestructure, val);
                                currpos += chunklen * len;
                            }
                            // }
                        }
                        else
                        {
                            if (ft.FullName != null && ft.FullName.Contains("Message"))
                            {
                                IRosMessage msg = (IRosMessage)Activator.CreateInstance(typeof(TypedMessage<>).MakeGenericType(TypeHelper.TypeInformation[GetMessageType(infos[currinfo].FieldType)].Type.GetGenericArguments()));
                                Type t = msg.GetType();
                                bool knownsize = IsSizeKnown(t, false) && (!TypeHelper.TypeInformation[msg.type].Fields[infos[currinfo].Name].IsArray || TypeHelper.TypeInformation[msg.type].Fields[infos[currinfo].Name].Lengths.Count != 0);
                                if (!knownsize)
                                {
                                    int len = BitConverter.ToInt32(bytes, currpos);
                                    byte[] smallerpiece = new byte[len + 4];
                                    Array.Copy(bytes, currpos, smallerpiece, 0, len + 4);
                                    //deserialize(t, smallerpiece, knownsize);
                                    msg.Deserialize(smallerpiece);
                                    object data = msg.GetType().GetField("data").GetValue(msg);
                                    infos[currinfo].SetValue(thestructure, data);
                                    currpos += len + 4;

                                }
                                else
                                {
                                    Console.WriteLine("MESSAGE OF KNOWN SIZE... PIZZA CAKE!");
                                }
                            }
                            else
                            {
                                if (infos[currinfo].FieldType == typeof(string))
                                {
                                    int len = BitConverter.ToInt32(bytes, currpos - 4);
                                    byte[] piece = new byte[len];
                                    Array.Copy(bytes, currpos, piece, 0, len);
                                    string str = Encoding.ASCII.GetString(piece);
                                    infos[currinfo].SetValue(thestructure, str);
                                    currpos += len;
                                }
                                else
                                {
                                    Console.WriteLine("ZOMG HANDLE: " + infos[currinfo].FieldType.FullName);

                                    return thestructure;
                                }
                            }

                        }
                    }
                    currinfo++;
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
            }
            return thestructure;
        }

        private static T CastAs<T>(object obj) where T : class, new() { return obj as T; }

        public static byte[] Serialize<T>(TypedMessage<T> outgoing, bool partofsomethingelse = false)
            where T : class, new()
        {
            if (outgoing.Serialized != null)
                return outgoing.Serialized;
            outgoing.Serialized = SlapChop(outgoing.data.GetType(), outgoing.data, partofsomethingelse);
            return outgoing.Serialized;
        }

        public static bool IsSizeKnown(Type T, bool recurse)
        {
            if (T.FullName != null && T.FullName.Contains("Messages.TypedMessage`1["))
                return IsSizeKnown(T.GetField("data").FieldType, recurse);
            if (T == typeof(string) || T == typeof(Messages.std_msgs.String) || (T.FullName != null && T.FullName.Contains("Messages.std_msgs.String")) || T.IsArray)
                return false;
            if (!recurse || !T.FullName.Contains("Messages")) return true;
            FieldInfo[] infos = T.GetFields();
            bool b = true;
            foreach (FieldInfo info in infos)
            {
                string fullName = info.FieldType.FullName;
                if (fullName != null)
                {
                    if (fullName.Contains("Messages."))
                        b = TypeHelper.TypeInformation[GetMessageType(info.FieldType)].Fields[info.Name].Type != typeof(string) &&
                            TypeHelper.TypeInformation[GetMessageType(info.FieldType)].Fields[info.Name].Type != typeof(String) &&
                            (!TypeHelper.TypeInformation[GetMessageType(info.FieldType)].Fields[info.Name].IsArray ||
                             TypeHelper.TypeInformation[GetMessageType(info.FieldType)].Fields[info.Name].Lengths.Count !=
                             0);
                    else
                        b = !info.FieldType.IsArray && info.FieldType != typeof(string);
                }
                if (!b)
                    break;
            }
            return b;
        }

        public static byte[] SlapChop(Type T, object t, bool partofsomethingelse = false)
        {
            FieldInfo[] infos = t.GetType().GetFields();
            Queue<byte[]> chunks = new Queue<byte[]>();
            int totallength = 0;
            foreach (FieldInfo info in infos)
            {
                if (info.Name.Contains("(")) continue;
                if (TypeHelper.TypeInformation[GetMessageType(T)].Fields[info.Name].IsConst) continue;
                if (info.GetValue(t) == null)
                {
                    if (info.FieldType == typeof(string))
                        info.SetValue(t, "");
                    else if (info.FieldType.FullName != null && !info.FieldType.FullName.Contains("Messages."))
                        info.SetValue(t, 0);
                    else
                        info.SetValue(t, Activator.CreateInstance(info.FieldType));
                }
                bool knownpiecelength = TypeHelper.TypeInformation[GetMessageType(T)].Fields[info.Name].Type != typeof(string) &&
                                        (!TypeHelper.TypeInformation[GetMessageType(T)].Fields[info.Name].IsArray ||
                                         TypeHelper.TypeInformation[GetMessageType(T)].Fields[info.Name].Lengths.Count !=
                                         0);
                byte[] thischunk = NeedsMoreChunks(info.FieldType, info.GetValue(t), ref knownpiecelength);
                chunks.Enqueue(thischunk);
                totallength += thischunk.Length;
            }
            byte[] wholeshebang = new byte[totallength];
            int currpos = 0;
            if (!partofsomethingelse)
            {
                wholeshebang = new byte[totallength + 4]; //THE WHOLE SHEBANG
                byte[] len = BitConverter.GetBytes(totallength);
                Array.Copy(len, 0, wholeshebang, 0, 4);
                currpos = 4;
            }
            while (chunks.Count > 0)
            {
                byte[] chunk = chunks.Dequeue();
                Array.Copy(chunk, 0, wholeshebang, currpos, chunk.Length);
                currpos += chunk.Length;
            }
            return wholeshebang;
        }

        public static byte[] NeedsMoreChunks(Type T, object val, ref bool knownlength)
        {
            byte[] thischunk = null;
            if (!T.IsArray)
            {
                if (T != typeof(TimeData) && T.Namespace.Contains("Message"))
                {
                    IRosMessage msg = null;
                    if (val != null)
                        msg = (IRosMessage)Activator.CreateInstance(typeof(TypedMessage<>).MakeGenericType(T), val);
                    else
                        msg = (IRosMessage)Activator.CreateInstance(typeof(TypedMessage<>).MakeGenericType(T));
                    thischunk = msg.Serialize(true);
                }
                else if (val is string || T == typeof(string))
                {
                    if (!knownlength)
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
                        thischunk = Encoding.ASCII.GetBytes((string)val);
                        knownlength = false;
                    }
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
                    MsgTypes mt = GetMessageType(TT);
                    bool piecelengthknown = mt != MsgTypes.std_msgs__String;
                    byte[] chunk = NeedsMoreChunks(TT, vals[i], ref piecelengthknown);
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
            : base(
                (MsgTypes)
                Enum.Parse(typeof(MsgTypes), typeof(M).FullName.Replace("Messages.", "").Replace(".", "__")),
                TypeHelper.TypeInformation[
                    (MsgTypes)
                    Enum.Parse(typeof(MsgTypes), typeof(M).FullName.Replace("Messages.", "").Replace(".", "__"))].
                    MessageDefinition,
                TypeHelper.TypeInformation[
                    (MsgTypes)
                    Enum.Parse(typeof(MsgTypes), typeof(M).FullName.Replace("Messages.", "").Replace(".", "__"))].
                    HasHeader,
                TypeHelper.TypeInformation[
                    (MsgTypes)
                    Enum.Parse(typeof(MsgTypes), typeof(M).FullName.Replace("Messages.", "").Replace(".", "__"))].
                    IsMetaType)
        {
        }

        public TypedMessage(M d)
        {
            data = d;
            base.type =
                (MsgTypes)
                Enum.Parse(typeof(MsgTypes), typeof(M).FullName.Replace("Messages.", "").Replace(".", "__"));
            base.MessageDefinition =
                TypeHelper.TypeInformation[
                    (MsgTypes)
                    Enum.Parse(typeof(MsgTypes), typeof(M).FullName.Replace("Messages.", "").Replace(".", "__"))].
                    MessageDefinition;
            base.HasHeader =
                TypeHelper.TypeInformation[
                    (MsgTypes)
                    Enum.Parse(typeof(MsgTypes), typeof(M).FullName.Replace("Messages.", "").Replace(".", "__"))].
                    HasHeader;
            base.IsMeta =
                TypeHelper.TypeInformation[
                    (MsgTypes)
                    Enum.Parse(typeof(MsgTypes), typeof(M).FullName.Replace("Messages.", "").Replace(".", "__"))].
                    IsMetaType;
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
            return Serialize(false);
        }

        public override byte[] Serialize(bool partofsomethingelse = false)
        {
            return SerializationHelper.Serialize(this, partofsomethingelse);
        }
    }

    public class IRosMessage
    {
        public bool HasHeader;
        public bool IsMeta;

        public string MessageDefinition;

        public byte[] Serialized;
        public IDictionary connection_header;
        public MsgTypes type;

        public IRosMessage()
            : this(MsgTypes.Unknown, "", false, false)
        {
        }

        public IRosMessage(MsgTypes t, string def, bool hasheader, bool meta)
        {
            type = t;
            MessageDefinition = def;
            HasHeader = hasheader;
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
            return Serialize(false);
        }

        public virtual byte[] Serialize(bool partofsomethingelse = false)
        {
            return null;
        }
    }

    public class TypeInfo
    {
        public Dictionary<string, MsgFieldInfo> Fields;
        public bool HasHeader;
        public bool IsMetaType;
        public string MessageDefinition = "";
        public Type Type;

        public TypeInfo(Type t, bool hasheader, bool meta, string def, Dictionary<string, MsgFieldInfo> fields)
        {
            Type = t;
            HasHeader = hasheader;
            MessageDefinition = def;
            IsMetaType = meta;
            Fields = fields;
        }

        public static string Generate(string name, string ns, bool HasHeader, bool meta, List<string> defs,
                                      List<SingleType> types)
        {
            string def = "";
            foreach (string d in defs) def += d + "\n";
            def = def.Trim('\n');
            string ret = string.Format
                ("MsgTypes.{0}{1}, new TypeInfo({2}, {3}, {4},\n@\"{5}\",\n\t\t\t\t new Dictionary<string, MsgFieldInfo>{{\n",
                 (ns.Length > 0 ? (ns + "__") : ""), name,
                 "typeof(TypedMessage<" + (ns.Length > 0 ? (ns + ".") : "") + name + ">)",
                 HasHeader.ToString().ToLower(), meta.ToString().ToLower(), def);
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
        public string ConstVal;
        public bool IsArray;
        public bool IsConst;
        public bool IsLiteral;
        public bool IsMetaType;
        public List<int> Lengths = new List<int>();
        public string Name;
        public Type Type;

        public MsgFieldInfo(string name, bool isliteral, Type type, bool isconst, string constval, bool isarray,
                            string lengths, bool meta)
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
            return string.Format
                ("new MsgFieldInfo(\"{0}\", {1}, {2}, {3}, \"{4}\", {5}, \"{6}\", {7})",
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
        private bool HasHeader;
        public string Name;
        public string Namespace = "Messages";
        public List<SingleType> Stuff = new List<SingleType>();
        public string backhalf;
        public string classname;
        private List<string> def = new List<string>();
        public string dimensions = "";
        public string fronthalf;
        private string memoizedcontent;
        private bool meta;

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
                if (Name.ToLower() == "string")
                    lines[i].Replace("String", "string");
                SingleType test = KnownStuff.WhatItIs(lines[i]);
                if (test != null)
                    Stuff.Add(test);
            }
        }

        string GUTS = null;
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
                        fronthalf +=
                            "using Messages.std_msgs;\nusing Messages.geometry_msgs;\nusing Messages.nav_msgs;\nusing String=Messages.std_msgs.String;\n\n";
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
                    if (thisthing.Type == "Header")
                    {
                        HasHeader = true;
                    }
                    else if (classname == "String")
                    {
                        thisthing.input = thisthing.input.Replace("String", "string");
                        thisthing.Type = thisthing.Type.Replace("String", "string");
                        thisthing.output = thisthing.output.Replace("String", "string");
                    }
                    else if (classname == "Time")
                    {
                        thisthing.input = thisthing.input.Replace("Time", "TimeData");
                        thisthing.Type = thisthing.Type.Replace("Time", "TimeData");
                        thisthing.output = thisthing.output.Replace("Time", "TimeData");
                    }
                    else if (classname == "Duration")
                    {
                        thisthing.input = thisthing.input.Replace("Duration", "TimeData");
                        thisthing.Type = thisthing.Type.Replace("Duration", "TimeData");
                        thisthing.output = thisthing.output.Replace("Duration", "TimeData");
                    }
                    meta |= thisthing.meta;
                    memoizedcontent += "\t" + thisthing.output + "\n";
                }
                if (classname.ToLower() == "string")
                {
                    memoizedcontent +=
                        "\n\n\t\t\tpublic String(string s){ data = s; }\n\t\t\tpublic String(){ data = \"\"; }\n\n";
                }
                else if (classname == "Time")
                {
                    memoizedcontent +=
                        "\n\n\t\t\tpublic Time(uint s, uint ns) : this(new TimeData{ sec=s, nsec = ns}){}\n\t\t\tpublic Time(TimeData s){ data = s; }\n\t\t\tpublic Time() : this(0,0){}\n\n";
                }
                else if (classname == "Duration")
                {
                    memoizedcontent +=
                        "\n\n\t\t\tpublic Duration(uint s, uint ns) : this(new TimeData{ sec=s, nsec = ns}){}\n\t\t\tpublic Duration(TimeData s){ data = s; }\n\t\t\tpublic Duration() : this(0,0){}\n\n";
                }
                string ns = Namespace.Replace("Messages.", "");
                if (ns == "Messages")
                    ns = "";
                while (memoizedcontent.Contains("DataData"))
                    memoizedcontent = memoizedcontent.Replace("DataData", "Data");
                GeneratedDictHelper = TypeInfo.Generate(classname, ns, HasHeader, meta, def, Stuff);
            }
            if (wasnull)
            {
            }
            GUTS = fronthalf + "\n\t\tpublic class " + classname + "\n\t\t{\n" + memoizedcontent + "\t\t}" + "\n" +
                         backhalf;
            return GUTS;
        }


        public void Write(string outdir)
        {
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
                                                                      {"float32", "float"},
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
                                                                      {"char", "char"},
                                                                      {"time", "Time"},
                                                                      {"string", "String"},
                                                                      {"duration", "Duration"}
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
        public bool Const;
        public string ConstValue = "";
        public bool IsArray;
        public bool IsLiteral;
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

        string[] backup;

        public SingleType Finalize(string[] s, bool isliteral)
        {
            backup = new string[s.Length];
            Array.Copy(s, backup, s.Length);
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
                    ConstValue = chunks[chunks.Length - 1].Trim();
                    ;
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

        public void refinalize(string REALTYPE)
        {
            bool isconst = false;
            string type = REALTYPE;
            string name = backup[1];
            string othershit = "";
            if (name.Contains('='))
            {
                string[] parts = name.Split('=');
                isconst = true;
                name = parts[0];
                othershit = " = " + parts[1];
            }
            for (int i = 2; i < backup.Length; i++)
                othershit += " " + backup[i];
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
                    ConstValue = chunks[chunks.Length - 1].Trim();
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
        }
    }

    public struct TimeData
    {
        public uint sec;
        public uint nsec;
        public TimeData(uint s, uint ns) { sec = s; nsec = ns; }
    }
}