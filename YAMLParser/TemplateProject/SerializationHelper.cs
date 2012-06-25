#region USINGZ

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
        static Dictionary<Type, MsgTypes> GetMessageTypeMemo = new Dictionary<Type, MsgTypes>();
        [DebuggerStepThrough]
        public static MsgTypes GetMessageType(Type t)
        {
            //gross, but it gets the job done            
            if (t == typeof(Single) && !GetMessageTypeMemo.ContainsKey(t))  //ERIC
                GetMessageTypeMemo.Add(typeof(Single), (MsgTypes)Enum.Parse(typeof(MsgTypes), "std_msgs__Float32")); //ERIC
            if (GetMessageTypeMemo.ContainsKey(t))
                return GetMessageTypeMemo[t];
            MsgTypes mt = GetMessageType(t.FullName);
            GetMessageTypeMemo.Add(t, mt);
            return mt;
        }

        [DebuggerStepThrough]
        public static Type GetType(string s)
        {
            MsgTypes mt = GetMessageType(s);
            if (mt == MsgTypes.Unknown)
                return Type.GetType(s, true, true);
            return IRosMessage.generate(mt).GetType();
        }

        static Dictionary<string, MsgTypes> GetMessageTypeMemoString = new Dictionary<string, MsgTypes>();

        [DebuggerStepThrough]
        public static MsgTypes GetMessageType(string s)
        {
            if (GetMessageTypeMemoString.ContainsKey(s))
                return GetMessageTypeMemoString[s];
            if (s.Contains("TimeData"))
                return MsgTypes.std_msgs__Time;
            if (!s.Contains("Messages"))
            {
                if (s.Contains("System."))
                {
                    Array types = Enum.GetValues(typeof(MsgTypes));
                    MsgTypes[] mts = (MsgTypes[])types;
                    string test = s.Split('.')[1];
                    foreach (MsgTypes mt in mts)
                    {
                        if (mt.ToString().ToLower().Contains(test.ToLower()))
                        {
                            return mt;
                        }
                    }
                }
                return MsgTypes.Unknown;
            }
            MsgTypes ms = (MsgTypes)Enum.Parse(typeof(MsgTypes), s.Replace("Messages.", "").Replace(".", "__").Replace("[]", ""));
            GetMessageTypeMemoString.Add(s, ms);
            return ms;
        }

        //[DebuggerStepThrough]
        public static bool IsSizeKnown(Type T, bool recurse)
        {
            if (T == typeof(string) || T == typeof(String) ||
                (T.FullName != null && T.FullName.Contains("Messages.std_msgs.String")) /*|| T.IsArray ERIC*/)
                return false;
            if (T.FullName.Contains("System.") || T.FullName.Contains("Messages.std_msgs.Time") || T.FullName.Contains("Messages.std_msgs.Duration"))
                return true;
            if (!recurse || !T.FullName.Contains("Messages")) return true;
            FieldInfo[] infos = T.GetFields();
            bool b = true;
            foreach (FieldInfo info in infos)
            {
                string fullName = info.FieldType.FullName;
                if (fullName != null)
                {

                    if (fullName.Contains("Messages."))
                    {
                        MsgTypes MT = GetMessageType(info.FieldType);
                        IRosMessage TI = IRosMessage.generate(MT);
                        b &= IsSizeKnown(TI.GetType(), true); //TI.Fields[info.Name].Type != typeof(string) && TI.Fields[info.Name].Type != typeof(String) && (!TI.Fields[info.Name].IsArray || TI.Fields[info.Name].Length != -1);
                    }
                    else
                        b &= !info.FieldType.IsArray && info.FieldType != typeof(string);
                }
                if (!b)
                    break;
            }
            return b;
        }

        internal static T Deserialize<T>(byte[] bytes, Type container = null) where T : IRosMessage, new()
        {
            int dontcare = 0;
            return _deserialize(typeof(T), container, bytes, out dontcare, IsSizeKnown(typeof(T), true)) as T;
        }

        public static object deserialize(Type T, Type container, byte[] bytes, out int amountread, bool sizeknown = false)
        {
            try
            {
                return _deserialize(T, container, bytes, out amountread, sizeknown);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            amountread = 0;
            return null;
        }

        private static Dictionary<MsgTypes, FieldInfo[]> speedyFields = new Dictionary<MsgTypes, FieldInfo[]>();
        private static Dictionary<MsgTypes, List<string>> importantfieldnames = new Dictionary<MsgTypes, List<string>>();
        private static FieldInfo[] GetFields(Type T, ref object instance, out IRosMessage msg)
        {
            if (instance == null)
                instance = Activator.CreateInstance(T);
            IRosMessage MSG = instance as IRosMessage;
            if (MSG == null)
            {
                msg = MSG;
                return instance.GetType().GetFields();
            }
            MsgTypes MT = MSG.msgtype;
            if (MT != MsgTypes.Unknown)
            {
                msg = MSG;
                return msg.GetType().GetFields().Where((fi => MSG.Fields.Keys.Contains(fi.Name))).ToArray();
            }
            throw new Exception("GetFields is weaksauce");
        }

        private static object _deserialize(Type T, Type container, byte[] bytes, out int amountread, bool sizeknown = false)
        {
            bool isoutermost = false;
            int totallength = -1;
            if (container == null)
            {
                totallength = BitConverter.ToInt32(bytes, 0);
                isoutermost = true;
                byte[] allbutlength = new byte[bytes.Length - 4];
                Array.Copy(bytes, 4, allbutlength, 0, allbutlength.Length);
                int read = 0;
                object o = _deserialize(T, T, allbutlength, out read);
                if (o == null || read != totallength)
                {
                    throw new Exception("FAILSERIALIZATION");
                }
                amountread = bytes.Length;
                return o;
            }
            object thestructure = null;
            if (T.FullName.Contains("System.") && !T.IsCOMObject && !T.IsArray && T != typeof(string))
            {
                thestructure = new object();
                int size = Marshal.SizeOf(T);
                IntPtr mem = Marshal.AllocHGlobal(size);
                Marshal.Copy(bytes, 0, mem, size);
                amountread = size;
                return Marshal.PtrToStructure(mem, T);
            }
            IRosMessage MSG;
            int startingpos = 0, currpos = 0;
            MsgTypes MT = MsgTypes.Unknown;
            FieldInfo[] infos = GetFields(T, ref thestructure, out MSG);
            if (MSG != null)
                MT = MSG.msgtype;
            currpos = 0; // sizeknown ? 0 : 4;
            startingpos = currpos;
            int currinfo = 0;
            while (currpos < bytes.Length && currinfo < infos.Length)
            {
                Type type = GetType(infos[currinfo].FieldType.FullName);
                Type realtype = infos[currinfo].FieldType;
                MsgTypes msgtype = GetMessageType(type);

                bool knownpiecelength = IsSizeKnown(realtype, true) && MSG.Fields != null && !MSG.Fields[infos[currinfo].Name].IsArray || MSG.Fields[infos[currinfo].Name].Length != -1;
                if (knownpiecelength)
                {
                    if (infos[currinfo].FieldType.IsArray && msgtype == MsgTypes.std_msgs__Byte)
                    {
                        Array vals = (infos[currinfo].GetValue(thestructure) as Array);
                        if (vals != null)
                        {
                            int num = vals.Length;
                            byte[] PWNED = new byte[num];
                            Array.Copy(bytes, currpos, PWNED, 0, num);
                            currpos += num;
                            infos[currinfo].SetValue(thestructure, PWNED);
                        }
                    }
                    else if (infos[currinfo].FieldType.IsArray && msgtype != MsgTypes.std_msgs__String)
                    //must have length defined, or else knownpiecelength would be false... so look it up in the dict!
                    {
                        Type TT = GetType(realtype.GetElementType().FullName);
                        if (TT.IsArray)
                            throw new Exception("ERIC, YOU NEED TO MAKE DESERIALIZATION RECURSE!!!");
                        Array vals = (infos[currinfo].GetValue(thestructure) as Array);
                        if (vals != null)
                        {
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
                                object o = Marshal.PtrToStructure(pIP, TT);
                                vals.SetValue(o, i);
                                currpos += leng;
                            }
                        }
                        infos[currinfo].SetValue(thestructure, vals);
                    }
                    else
                    {
                        if (type.FullName != null && type.FullName.Contains("Message"))
                        {
                            if (GetMessageType(realtype) == MsgTypes.std_msgs__Time)
                            {
                                uint u1 = BitConverter.ToUInt32(bytes, currpos);
                                uint u2 = BitConverter.ToUInt32(bytes, currpos + 4);
                                TimeData td = new TimeData(u1, u2);
                                infos[currinfo].SetValue(thestructure, (object)new std_msgs.Time(td));
                                currpos += 8;
                            }
                            else
                            {
                                byte[] piece = new byte[bytes.Length - currpos];
                                Array.Copy(bytes, currpos, piece, 0, piece.Length);
                                int len = 0;
                                object obj = _deserialize(realtype, T, piece, out len,
                                                         IsSizeKnown(realtype, true));
                                infos[currinfo].SetValue(thestructure, obj);
                                currpos += len;
                            }
                        }
                        else
                        {
                            int len = Marshal.SizeOf(infos[currinfo].GetValue(thestructure));
                            IntPtr pIP = Marshal.AllocHGlobal(len);
                            Marshal.Copy(bytes, currpos, pIP, len);
                            object obj = Marshal.PtrToStructure(pIP, infos[currinfo].FieldType);
                            infos[currinfo].SetValue(thestructure, obj);
                            currpos += len;
                        }
                    }
                }
                else
                {
                    Type ft = realtype;
                    if (ft.IsArray)
                    {
                        Type TT = GetType(ft.GetElementType().FullName);
                        Array val = infos[currinfo].GetValue(thestructure) as Array;
                        int chunklen;
                        if (val == null)
                        {
                            chunklen = BitConverter.ToInt32(bytes, currpos);
                            currpos += 4;
                            val = Array.CreateInstance(TT, chunklen);
                        }
                        else
                        {
                            chunklen = val.Length;
                        }
                        if (TT == null)
                            throw new Exception("LENGTHLESS ARRAY FAIL -- ELEMENT TYPE IS NULL!");
                        if (TT == typeof(string) || TT.FullName.Contains("Message."))
                            throw new Exception("NOT YET, YOUNG PATAWAN");
                        MsgTypes mt = GetMessageType(TT);
                        if (mt == MsgTypes.std_msgs__Byte)
                        {
                            int num = val.Length;
                            byte[] PWNED = new byte[num];
                            Array.Copy(bytes, currpos, PWNED, 0, num);
                            currpos += num;
                            infos[currinfo].SetValue(thestructure, PWNED);
                        }
                        else if (TT.FullName != null && TT.FullName.Contains("Message"))
                        {
                            for (int i = 0; i < chunklen; i++)
                            {
                                byte[] chunk = new byte[bytes.Length - currpos];
                                Array.Copy(bytes, currpos, chunk, 0, chunk.Length);
                                int len = 0;
                                object data = _deserialize(TT, T, chunk, out len,
                                                          IsSizeKnown(TT, false));
                                val.SetValue(data, i);
                                currpos += len;
                            }
                            infos[currinfo].SetValue(thestructure, val);
                        }
                        else
                        {
                            int len = Marshal.SizeOf(TT);
                            IntPtr pIP = Marshal.AllocHGlobal(len * chunklen);
                            Marshal.Copy(bytes, currpos, pIP, len * chunklen);
                            object o = null;
                            for (int i = 0; i < chunklen * len; i += len)
                            {
                                o = Marshal.PtrToStructure(pIP, TT);
                                val.SetValue(o, i / len);
                                pIP = pIP + len;
                            }
                            infos[currinfo].SetValue(thestructure, val);
                            currpos += chunklen * len;
                        }
                    }
                    else
                    {
                        if (ft.FullName != null && ft.FullName.Contains("Message"))
                        {
                            IRosMessage msg = (IRosMessage)Activator.CreateInstance(ft);
                            Type t = GetType(msg.GetType().FullName);
                            bool knownsize = IsSizeKnown(t, false) && MSG.Fields != null && !MSG.Fields[infos[currinfo].Name].IsArray || MSG.Fields[infos[currinfo].Name].Length != -1;
                            if (!knownsize && t.GetField("data").FieldType == typeof(string))
                            {
                                int len = BitConverter.ToInt32(bytes, currpos);
                                byte[] smallerpiece = new byte[len + 4];
                                Array.Copy(bytes, currpos, smallerpiece, 0, smallerpiece.Length);
                                int dontcare = 0;
                                msg = _deserialize(t, T, smallerpiece, out dontcare, false) as IRosMessage;
                                if (dontcare != len + 4)
                                    throw new Exception("WTF?!");
                                infos[currinfo].SetValue(thestructure, msg);
                                currpos += len + 4;
                            }
                            else // if (!knownsize)
                            {
                                byte[] smallerpiece = new byte[bytes.Length - currpos];
                                Array.Copy(bytes, currpos, smallerpiece, 0, smallerpiece.Length);
                                int len = 0;
                                msg = _deserialize(t, T, smallerpiece, out len, knownsize) as IRosMessage;
                                infos[currinfo].SetValue(thestructure, msg);
                                currpos += len;
                            }
                            /*else
                            {
                                throw new Exception("THIS BROKE SOMEHOW! FIX IT!");
                            }*/
                        }
                        else
                        {
                            if (infos[currinfo].FieldType == typeof(string))
                            {
                                int len = BitConverter.ToInt32(bytes, currpos);
                                byte[] piece = new byte[len];
                                currpos += 4;
                                Array.Copy(bytes, currpos, piece, 0, len);
                                string str = Encoding.ASCII.GetString(piece);
                                infos[currinfo].SetValue(thestructure, str);
                                currpos += len;
                            }
                            else
                            {
                                Console.WriteLine("ZOMG HANDLE: " + infos[currinfo].FieldType.FullName);
                                amountread = currpos - startingpos;
                                return thestructure;
                            }
                        }
                    }
                }
                currinfo++;
            }
            amountread = currpos - startingpos;
            return thestructure;
        }

        internal static byte[] Serialize<T>(T outgoing, bool partofsomethingelse = false)
            where T : IRosMessage, new()
        {
            if (outgoing.Serialized != null)
                return outgoing.Serialized;
            outgoing.Serialized = SlapChop(outgoing.GetType(), outgoing, partofsomethingelse);
            return outgoing.Serialized;
        }

        public static byte[] SlapChop(Type T, object instance, bool partofsomethingelse = false)
        {
            IRosMessage msg;
            FieldInfo[] infos = GetFields(T, ref instance, out msg);
            Queue<byte[]> chunks = new Queue<byte[]>();
            int totallength = 0;
            MsgTypes MT = msg.msgtype;
            foreach (FieldInfo info in infos)
            {
                if (info.Name.Contains("(")) continue;
                if (msg.Fields[info.Name].IsConst) continue;
                if (info.GetValue(instance) == null)
                {
                    if (info.FieldType == typeof(string))
                        info.SetValue(instance, "");
                    else if (info.FieldType.FullName != null && !info.FieldType.FullName.Contains("Messages."))
                        info.SetValue(instance, 0);
                    else
                        info.SetValue(instance, Activator.CreateInstance(info.FieldType));
                }
                bool knownpiecelength = msg.Fields[info.Name].Type !=
                                        typeof(string) &&
                                        (!msg.Fields[info.Name].IsArray ||
                                         msg.Fields[info.Name].Length != -1);
                byte[] thischunk = NeedsMoreChunks(info.FieldType, info.GetValue(instance), ref knownpiecelength);
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
                        msg = val as IRosMessage;
                    else
                        msg = (IRosMessage)Activator.CreateInstance(T);
                    thischunk = msg.Serialize(true);
                }
                else if (T == typeof(byte) || T.HasElementType && T.GetElementType() == typeof(byte))
                {
                    if (T.IsArray)
                    {
                        if (!knownlength)
                        {
                            Array ar = (val as Array);
                            byte[] nolen = new byte[ar.Length];
                            Array.Copy(ar, 0, nolen, 0, ar.Length);
                            thischunk = new byte[nolen.Length + 4];
                            byte[] bylen2 = BitConverter.GetBytes(nolen.Length);
                            Array.Copy(nolen, 0, thischunk, 4, nolen.Length);
                            Array.Copy(bylen2, thischunk, 4);
                        }
                        else
                        {
                            Array ar = (val as Array);
                            thischunk = new byte[ar.Length];
                            Array.Copy(ar, 0, thischunk, 0, ar.Length);
                            knownlength = false;
                        }
                    }
                    else
                    {
                        thischunk = new[] { (byte)val };
                    }
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
                object[] vals = (val as Array).Cast<object>().ToArray();
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

    public class MsgFieldInfo
    {
        public string ConstVal;
        public bool IsArray;
        public bool IsConst;
        public bool IsLiteral;
        public bool IsMetaType;
        public int Length = -1;
        public string Name;
        public Type Type;
        [DebuggerStepThrough]
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
                Length = int.Parse(lengths);
            }
        }

        public static string Generate(SingleType members)
        {
            return string.Format
                ("\"{0}\", new MsgFieldInfo(\"{0}\", {1}, {2}, {3}, \"{4}\", {5}, \"{6}\", {7})",
                 members.Name,
                 members.IsLiteral.ToString().ToLower(),
                 ("typeof(" + members.Type + ")"),
                 members.Const.ToString().ToLower(),
                 members.ConstValue,
                 members.IsArray.ToString().ToLower(),
                 members.length,
                //FIX MEEEEEEEE
                 members.meta.ToString().ToLower());
        }
    }

    public class SrvsFile
    {
        private string GUTS;
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
        public MsgsFile Request, Response;
        public SrvsFile(string filename)
        {
            string[] lines = File.ReadAllLines(filename);
            string[] sp = filename.Replace("ROS_MESSAGES", "").Replace(".srv", "").Split('\\');
            classname = sp[sp.Length - 1];
            Namespace += "." + filename.Replace("ROS_MESSAGES", "").Replace(".srv", "");
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
            def = new List<string>(lines);
            int mid = 0;
            bool found = false;
            List<string> request = new List<string>(), response = new List<string>();
            for (; mid < lines.Length; mid++)
            {
                if (lines[mid].Contains("---"))
                {
                    found = true;
                    continue;
                }
                if (found)
                    response.Add(lines[mid]);
                else
                    request.Add(lines[mid]);
            }
            Request = new MsgsFile(filename, true, request, "\t");
            Response = new MsgsFile(filename, false, response, "\t");
        }

        public void Write(string outdir)
        {
            string[] chunks = Name.Split('.');
            for (int i = 0; i < chunks.Length - 1; i++)
                outdir += "\\" + chunks[i];
            if (!Directory.Exists(outdir))
                Directory.CreateDirectory(outdir);
            string localcn = classname;
            localcn = classname.Replace("Request", "").Replace("Response", "");
            File.WriteAllText(outdir + "\\" + localcn + ".cs", ToString());
        }

        public override string ToString()
        {
            if (fronthalf == null)
            {
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
                            "using Messages.std_msgs;\nusing Messages.roscsharp;\nusing Messages.geometry_msgs;\nusing Messages.nav_msgs;\nusing String=Messages.std_msgs.String;\n\n";
                        fronthalf += "namespace " + Namespace + "\n";
                        continue;
                    }
                    if (!hitvariablehole)
                        fronthalf += lines[i] + "\n";
                    else
                        backhalf += lines[i] + "\n";
                }
            }

            GUTS = fronthalf + "\n\t\tpublic class " + classname + "\n\t\t{" + Request.GetSrvHalf() + Response.GetSrvHalf() + "\t\t}" + "\n" +
                   backhalf;
            return GUTS;
        }
    }

    public class MsgsFile
    {
        private string GUTS;
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
        public ServiceMessageType serviceMessageType = ServiceMessageType.Not;
        public MsgsFile(string filename, bool isrequest, List<string> lines, string extraindent = "")
        {
            serviceMessageType = isrequest ? ServiceMessageType.Request : ServiceMessageType.Response;
            filename = filename.Replace(".srv", ".msg");
            if (!filename.Contains(".msg"))
                throw new Exception("" + filename + " IS NOT A VALID SRV FILE!");
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
            classname += (isrequest ? "Request" : "Response");
            Namespace = Namespace.Trim('.');
            def = new List<string>();
            for (int i = 0; i < lines.Count; i++)
            {
                def.Add(lines[i]);
                if (Name.ToLower() == "string")
                    lines[i].Replace("String", "string");
                SingleType test = KnownStuff.WhatItIs(lines[i], extraindent);
                if (test != null)
                    Stuff.Add(test);
            }
        }
        public MsgsFile(string filename, string extraindent = "")
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
            //lines = lines.Where((st) => (st.Length > 0)).ToList();

            lines.ForEach(new Action<string>((s) =>
            {
                if (s.Contains('#') && s.Split('#')[0].Length != 0)
                    s = s.Split('#')[0];
                if (s.Contains('#'))
                    s = "";
            }));
            lines = lines.Where((st) => (st.Length > 0)).ToList();


            def = new List<string>();
            for (int i = 0; i < lines.Count; i++)
            {
                def.Add(lines[i]);
                if (Name.ToLower() == "string")
                    lines[i].Replace("String", "string");
                SingleType test = KnownStuff.WhatItIs(lines[i], extraindent);
                if (test != null)
                    Stuff.Add(test);
            }
        }

        public string GetSrvHalf()
        {
            string wholename = classname.Replace("Request", ".Request").Replace("Response", ".Response"); ;
            classname = classname.Contains("Request") ? "Request" : "Response";
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
                        "\n\n\t\t\t\t\tpublic String(string s){ data = s; }\n\t\t\t\t\tpublic String(){ data = \"\"; }\n\n";
                }
                else if (classname == "Time")
                {
                    memoizedcontent +=
                        "\n\n\t\t\t\t\tpublic Time(uint s, uint ns) : this(new TimeData{ sec=s, nsec = ns}){}\n\t\t\t\t\tpublic Time(TimeData s){ data = s; }\n\t\t\t\t\tpublic Time() : this(0,0){}\n\n";
                }
                else if (classname == "Duration")
                {
                    memoizedcontent +=
                        "\n\n\t\t\t\t\tpublic Duration(uint s, uint ns) : this(new TimeData{ sec=s, nsec = ns}){}\n\t\t\t\t\tpublic Duration(TimeData s){ data = s; }\n\t\t\t\t\tpublic Duration() : this(0,0){}\n\n";
                }
                while (memoizedcontent.Contains("DataData"))
                    memoizedcontent = memoizedcontent.Replace("DataData", "Data");
            }
            string ns = Namespace.Replace("Messages.", "");
            if (ns == "Messages")
                ns = "";
            GeneratedDictHelper = "";
            foreach (SingleType S in Stuff)
                GeneratedDictHelper += MsgFieldInfo.Generate(S);
            GUTS = fronthalf + "\n\t\t\tpublic class " + classname + "\n\t\t\t{\n" + memoizedcontent + "\t\t\t}" + "\n" +
                   backhalf;
            return GUTS;
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
                        fronthalf +=
                            "using Messages.std_msgs;\nusing Messages.roscsharp;\nusing Messages.geometry_msgs;\nusing Messages.nav_msgs;\nusing String=Messages.std_msgs.String;\n\n";
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
                string ns = Namespace.Replace("Messages.", "");
                if (ns == "Messages")
                    ns = "";
                while (memoizedcontent.Contains("DataData"))
                    memoizedcontent = memoizedcontent.Replace("DataData", "Data");
                //if (GeneratedDictHelper == null)
                //    GeneratedDictHelper = TypeInfo.Generate(classname, ns, HasHeader, meta, def, Stuff);
                GeneratedDictHelper = "\n\t\t\t\t";
                for (int i = 0; i < Stuff.Count; i++)
                    GeneratedDictHelper += ((i > 0) ? "}, \n\t\t\t\t{" : "") + MsgFieldInfo.Generate(Stuff[i]);
                GUTS = (serviceMessageType != ServiceMessageType.Response ? fronthalf : "") + "\n" + memoizedcontent + "\n" +
                       (serviceMessageType != ServiceMessageType.Request ? backhalf : "");
                if (classname.ToLower() == "string")
                {
                    GUTS = GUTS.Replace("$NULLCONSTBODY", "if (data == null)\n\t\t\tdata = \"\";\n");
                    GUTS = GUTS.Replace("$EXTRACONSTRUCTOR", "\n\t\tpublic $WHATAMI(string d) : base($MYMSGTYPE, $MYMESSAGEDEFINITION, $MYHASHEADER, $MYISMETA, new Dictionary<string, MsgFieldInfo>$MYFIELDS)\n\t\t{\n\t\t\tdata = d;\n\t\t}\n");
                }
                else if (classname == "Time" || classname == "Duration")
                {
                    GUTS = GUTS.Replace("$EXTRACONSTRUCTOR", "\n\t\tpublic $WHATAMI(TimeData d) : base($MYMSGTYPE, $MYMESSAGEDEFINITION, $MYHASHEADER, $MYISMETA, new Dictionary<string, MsgFieldInfo>$MYFIELDS)\n\t\t{\n\t\t\tdata = d;\n\t\t}\n");
                }
                GUTS = GUTS.Replace("$WHATAMI", classname);
                GUTS = GUTS.Replace("$MYISMETA", meta.ToString().ToLower());
                GUTS = GUTS.Replace("$MYMSGTYPE", "MsgTypes." + Namespace.Replace("Messages.", "") + "__" + classname);
                GUTS = GUTS.Replace("$MYMESSAGEDEFINITION", "@\"" + def.Aggregate("", (current, d) => current + (d + "\n")).Trim('\n') + "\"");
                GUTS = GUTS.Replace("$MYHASHEADER", HasHeader.ToString().ToLower());
                GUTS = GUTS.Replace("$MYFIELDS", GeneratedDictHelper.Length > 5 ? "{{" + GeneratedDictHelper + "}}" : "()");
                GUTS = GUTS.Replace("$NULLCONSTBODY", "");
                GUTS = GUTS.Replace("$EXTRACONSTRUCTOR", "");
            }
            return GUTS;
        }

        public void Write(string outdir)
        {
            string[] chunks = Name.Split('.');
            for (int i = 0; i < chunks.Length - 1; i++)
                outdir += "\\" + chunks[i];
            if (!Directory.Exists(outdir))
                Directory.CreateDirectory(outdir);
            string localcn = classname;
            if (serviceMessageType != ServiceMessageType.Not)
                localcn = classname.Replace("Request", "").Replace("Response", "");
            if (serviceMessageType == ServiceMessageType.Response)
                File.AppendAllText(outdir + "\\" + localcn + ".cs", ToString());
            else
                File.WriteAllText(outdir + "\\" + localcn + ".cs", ToString());
        }
    }

    public static class KnownStuff
    {
        public static Dictionary<string, string> KnownTypes = new Dictionary<string, string>
                                                                  {
                                                                      {"float64", "double"},
                                                                      {"float32", "Single"},
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

        public static SingleType WhatItIs(string s, string extraindent)
        {
            string[] pieces = s.Split('/');
            if (pieces.Length > 1)
            {
                s = pieces[pieces.Length - 1];
            }
            return WhatItIs(new SingleType(s, extraindent));
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
        private string[] backup;
        public string input;
        public string length = "";
        public bool meta;
        public string output;
        public string rostype = "";
        public string lowestindent = "\t\t";

        public SingleType(string s, string extraindent = "")
        {
            lowestindent += extraindent;
            if (s.Contains('[') && s.Contains(']'))
            {
                string front = "";
                string back = "";
                string[] parts = s.Split('[');
                front = parts[0];
                parts = parts[1].Split(']');
                length = parts[0];
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
                output = lowestindent + "public " + (isconst ? "const " : "") + type + " " + name + othershit + ";";
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
                if (length.Length > 0)
                {
                    IsLiteral = type != "string";
                    output = lowestindent + "public " + type + "[] " + name + " = new " + type + "[" + length + "];";
                }
                else
                    output = lowestindent + "public " + "" + type + "[] " + name + othershit + ";";
                if (othershit.Contains('='))
                {
                    string[] split = othershit.Split('=');
                    othershit = split[0] + " = (" + type + ")" + split[1];
                }
            }
            Type = type;
            if (!KnownStuff.KnownTypes.ContainsKey(rostype))
                meta = true;
            Name = name.Length == 0 ? othershit.Trim() : name;
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
                output = lowestindent + "public " + (isconst ? "const " : "") + type + " " + name + othershit + ";";
                Const = isconst;
                if (othershit.Contains("="))
                {
                    string[] chunks = othershit.Split('=');
                    ConstValue = chunks[chunks.Length - 1].Trim();
                }
            }
            else
            {
                if (length.Length != 0)
                {
                    IsLiteral = type != "string";
                    output = lowestindent + "public " + type + "[" + length + "] " + name + " = new " + type + "[" + length + "];";
                }
                else
                    output = lowestindent + "public " + "" + type + "[] " + name + othershit + ";";
                if (othershit.Contains('='))
                {
                    string[] split = othershit.Split('=');
                    othershit = split[0] + " = (" + type + ")" + split[1];
                }
            }
            Type = type;
            if (!KnownStuff.KnownTypes.ContainsKey(rostype))
                meta = true;
            Name = name.Length == 0 ? othershit.Trim() : name;
        }
    }

    public enum ServiceMessageType
    {
        Not,
        Request,
        Response
    }

    public struct TimeData
    {
        public uint sec;
        public uint nsec;

        public TimeData(uint s, uint ns)
        {
            sec = s;
            nsec = ns;
        }
    }
}