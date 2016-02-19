#region Using

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
    [System.Diagnostics.DebuggerStepThrough]
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class SerializationHelper
    {
        private static bool WHAT_IS_HAPPENING;
        public static void ShowDeserializationSteps()
        {
            WHAT_IS_HAPPENING = true;
        }

        static Dictionary<Type, MsgTypes> GetMessageTypeMemo = new Dictionary<Type, MsgTypes>();
#if !TRACE
        [DebuggerStepThrough]
#endif
        public static MsgTypes GetMessageType(Type t)
        {
            MsgTypes mt;
            //gross, but it gets the job done   
            if (t == typeof(Boolean) && !GetMessageTypeMemo.ContainsKey(t))  //ERIC
                lock (GetMessageTypeMemo)
                {
                    if (!GetMessageTypeMemo.ContainsKey(t))
                        GetMessageTypeMemo.Add(t, (MsgTypes)Enum.Parse(typeof(MsgTypes), "std_msgs__Bool")); //ERIC
                }
            if (GetMessageTypeMemo.ContainsKey(t))
                return GetMessageTypeMemo[t];
            lock (GetMessageTypeMemo)
            {
                if (GetMessageTypeMemo.ContainsKey(t))
                    return GetMessageTypeMemo[t];
                mt = GetMessageType(t.FullName);
                GetMessageTypeMemo.Add(t, mt);
                return mt;
            }
        }

        static Dictionary<string, Type> GetTypeTypeMemo = new Dictionary<string, Type>();
#if !TRACE
        [DebuggerStepThrough]
#endif
        public static Type GetType(string s)
        {
            lock (GetTypeTypeMemo)
            {
                if (GetTypeTypeMemo.ContainsKey(s))
                    return GetTypeTypeMemo[s];
                Type ret;
                MsgTypes mt = GetMessageType(s);
                if (mt == MsgTypes.Unknown)
                {
                    System.Diagnostics.Debug.WriteLine("GetType is falling back to using Type.GetType! YUCK");
                    ret = Type.GetType(s, true, true);
                }
                else
                    ret = IRosMessage.generate(mt).GetType();
                return (GetTypeTypeMemo[s] = ret);
            }
        }

        static Dictionary<string, MsgTypes> GetMessageTypeMemoString = new Dictionary<string, MsgTypes>();

#if !TRACE
        [DebuggerStepThrough]
#endif
        public static MsgTypes GetMessageType(string s)
        {
            //Console.WriteLine("LOOKING FOR: " + s + "'s type");
            lock (GetMessageTypeMemoString)
            {
                if (GetMessageTypeMemoString.ContainsKey(s))
                    return GetMessageTypeMemoString[s];
                if (s.Contains("TimeData"))
                {
                    GetMessageTypeMemoString.Add(s, MsgTypes.std_msgs__Time);
                    return MsgTypes.std_msgs__Time;
                }
                if (!s.Contains("Messages"))
                {
                    if (s.Contains("System."))
                    {
                        Array types = Enum.GetValues(typeof(MsgTypes));
                        string test = s.Replace("[]", "").Split('.')[1];
                        string testmsgtype;
                        MsgTypes m = MsgTypes.Unknown;
                        for (int i = 0; i < types.Length; i++)
                        {
                            testmsgtype = types.GetValue(i).ToString();
                            if (!testmsgtype.Contains("std_msgs")) continue;
                            string[] pieces = testmsgtype.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
                            testmsgtype = pieces[pieces.Length - 1];
                            if (testmsgtype.ToLower().Equals(test.ToLower()))
                            {
                                m = (MsgTypes)types.GetValue(i);
                                break;
                            }
                        }
                        GetMessageTypeMemoString.Add(s, m);
                        return m;
                    }
                    return MsgTypes.Unknown;
                }
                MsgTypes ms = (MsgTypes)Enum.Parse(typeof(MsgTypes), s.Replace("Messages.", "").Replace(".", "__").Replace("[]", ""));
                GetMessageTypeMemoString.Add(s, ms);
                return ms;
            }
        }

        private static Dictionary<Type, bool> SizeKnowledge = new Dictionary<Type, bool>();

#if !TRACE
        [DebuggerStepThrough]
#endif
        public static bool IsSizeKnown(Type T, bool recurse)
        {
            lock (SizeKnowledge)
            {
                bool b = false;
                if (SizeKnowledge.TryGetValue(T, out b))
                    return b;
                return IsSizeKnownNoLock(T, recurse);
            }
        }

#if !TRACE
        [DebuggerStepThrough]
#endif
        private static bool IsSizeKnownNoLock(Type T, bool recurse)
        {
            if (T == typeof(string) || T == typeof(String) ||
                (T.FullName != null && T.FullName.Contains("Messages.std_msgs.String")) /*|| T.IsArray ERIC*/)
                return (SizeKnowledge[T] = false);
            if (T.FullName.Contains("System.") || T.FullName.Contains("Messages.std_msgs.Time") || T.FullName.Contains("Messages.std_msgs.Duration"))
                return (SizeKnowledge[T] = true);
            if (!recurse || !T.FullName.Contains("Messages")) return (SizeKnowledge[T] = true);
            bool b = true;
            FieldInfo[] infos = T.GetFields();
            foreach (FieldInfo info in infos)
            {
                string fullName = info.FieldType.FullName;
                if (fullName != null)
                {

                    if (fullName.Contains("Messages."))
                    {
                        MsgTypes MT = GetMessageType(info.FieldType);
                        IRosMessage TI = IRosMessage.generate(MT);
                        b &= IsSizeKnownNoLock(TI.GetType(), true); //TI.Fields[info.Name].Type != typeof(string) && TI.Fields[info.Name].Type != typeof(String) && (!TI.Fields[info.Name].IsArray || TI.Fields[info.Name].Length != -1);
                    }
                    else
                        b &= !info.FieldType.IsArray && info.FieldType != typeof(string);
                }
                if (!b)
                    break;
            }
            return (SizeKnowledge[T] = b);
        }

        internal static T Deserialize<T>(byte[] bytes) where T : IRosMessage, new()
        {

            return Deserialize<T>(bytes, null);

        }

        internal static T Deserialize<T>(byte[] bytes, Type container) where T : IRosMessage, new()
        {
            int dontcare = 0;
            return _deserialize(typeof(T), container, bytes, out dontcare, IsSizeKnown(typeof(T), true)) as T;
        }
        public static object deserialize(Type T, Type container, byte[] bytes, out int amountread)
        {
            return deserialize(T, container, bytes, out amountread, false);
        }


        public static object deserialize(Type T, Type container, byte[] bytes, out int amountread, bool sizeknown)
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

        private static Dictionary<Type, FieldInfo[]> speedyFields = new Dictionary<Type, FieldInfo[]>();
        public static FieldInfo[] GetFields(Type T, ref object instance, out IRosMessage msg)
        {
            if (instance == null)
            {
                if (T.IsArray)
                    instance = Array.CreateInstance(T.GetElementType(), 0);
                else
                    instance = Activator.CreateInstance(T);
            }

            IRosMessage MSG = instance as IRosMessage;
            msg = MSG;
            lock (speedyFields)
            {
                if (speedyFields.ContainsKey(T))
                    return speedyFields[T];
                if (MSG == null || MSG.msgtype == MsgTypes.Unknown)
                {
                    return (speedyFields[T] = instance.GetType().GetFields());
                }
                else if (MSG != null)
                {
                    return (speedyFields[T] = msg.GetType().GetFields().Where((fi => MSG.Fields.Keys.Contains(fi.Name) && !fi.IsStatic)).ToArray());
                }
            }
            throw new Exception("GetFields is weaksauce");
        }
        public static string dumphex(byte[] test)
        {
            if (test == null)
                return "dumphex(null)";
            string s = "";
            for (int i = 0; i < test.Length; i++)
                s += (test[i] < 16 ? "0" : "") + test[i].ToString("x") + " ";
            return s;
        }

        private static object _deserialize(Type T, Type container, byte[] bytes, out int amountread, bool sizeknown)
        {
            if (bytes == null && !WHAT_IS_HAPPENING || bytes.Length == 0 && !WHAT_IS_HAPPENING)
            {
                //                Console.WriteLine("Deserializing empty array?");
                amountread = 0;
                return null;
            }
            object thestructure = null;
            if (T.FullName.Contains("System.") && !T.IsCOMObject && !T.IsArray && T != typeof(string))
            {
                thestructure = new object();
                int size = Marshal.SizeOf(T);
                IntPtr mem = IntPtr.Zero;
                if (bytes.Length != 0)
                {
                    mem = Marshal.AllocHGlobal(size);
                    Marshal.Copy(bytes, 0, mem, size);
                }
                amountread = size;
                if (WHAT_IS_HAPPENING)
                {
                    /*
Console.WriteLine("//deserialize: " + T.FullName);
                    /*Console.WriteLine(string.Format(@"    $A = new {0}();    
    IntPtr $B = Marshal.AllocHGlobal({1})
    Marshal.Copy(bytes, 0, $B, {1});
    $A = Marshal.PtrToStructure($B, typeof({0}));    
", T.FullName, Marshal.SizeOf(T)));*/
                }
                if (mem == IntPtr.Zero) return null;
                object obj = Marshal.PtrToStructure(mem, T);
                Marshal.FreeHGlobal(mem);
                return obj;
            }
            IRosMessage MSG;
            int startingpos = 0, currpos = 0;
            /*if (container==null)
               currpos = 4;*/
            MsgTypes MT = MsgTypes.Unknown;
            FieldInfo[] infos = GetFields(T, ref thestructure, out MSG);
            if (MSG != null)
                MT = MSG.msgtype;
            startingpos = currpos;

            int currinfo = 0;
            while ((currpos < bytes.Length || WHAT_IS_HAPPENING) && currinfo < infos.Length)
            {
                // Console.WriteLine(infos[currinfo].Name + "(" + currpos + "/" + bytes.Length + ")");
                Type type = GetType(infos[currinfo].FieldType.FullName);
                //Console.WriteLine("GetType returned: " + type.FullName);
                Type realtype = infos[currinfo].FieldType;

                MsgTypes msgtype = GetMessageType(type);
                bool knownpiecelength = IsSizeKnown(realtype, true) && MSG.Fields != null && !MSG.Fields[infos[currinfo].Name].IsArray || MSG.Fields[infos[currinfo].Name].Length != -1;

                if (realtype == typeof(Byte[])) //this is deplorable.
                {
                    //SHORTCUT
                    byte[] PWNED;
                    MsgFieldInfo mfi = MSG.Fields[infos[currinfo].Name];
                    int num = mfi.Length;
                    if (mfi.Length == -1) //if -1, then array length not in definition
                    {
                        num = BitConverter.ToInt32(bytes, currpos);
                        currpos += 4;
                    }
                    PWNED = new byte[num];
                    Array.Copy(bytes, currpos, PWNED, 0, num);
                    currpos += PWNED.Length;
                    infos[currinfo].SetValue(thestructure, PWNED);
                    PWNED = null;
                }
                else if (realtype == typeof(Boolean[]))
                {
                    bool[] PWNED;
                    MsgFieldInfo mfi = MSG.Fields[infos[currinfo].Name];
                    int num = mfi.Length;
                    if (mfi.Length == -1) //if -1, then array length not in definition
                    {
                        num = BitConverter.ToInt32(bytes, currpos);
                        currpos += 4;
                    }
                    PWNED = new bool[num];
                    for (int i = 0; i < num; i++)
                    {
                        PWNED[i] = bytes[i + currpos] > 0;
                    }
                    currpos += PWNED.Length;
                    infos[currinfo].SetValue(thestructure, PWNED);
                    PWNED = null;
                }
                else if (knownpiecelength)
                {
                    if (infos[currinfo].FieldType.IsArray && msgtype != MsgTypes.std_msgs__String)
                    //must have length defined, or else knownpiecelength would be false... so look it up in the dict!
                    {
                        Type TT = GetType(infos[currinfo].FieldType.GetElementType().FullName);
                        if (TT.IsArray)
                            throw new Exception("ERIC, YOU NEED TO MAKE DESERIALIZATION RECURSE!!!");
                        Array vals = (infos[currinfo].GetValue(thestructure) as Array);
                        if (vals != null)
                        {
                            for (int i = 0; i < vals.Length; i++)
                            {
                                MsgTypes mt = GetMessageType(TT);
                                int leng = 0;
                                Type et = realtype.GetElementType();
                                if (mt == MsgTypes.Unknown)
                                    leng = Marshal.SizeOf(et);
                                if (leng == 0)
                                    leng = Marshal.SizeOf(vals.GetValue(i));
                                if (leng == 0)
                                    throw new Exception("LENGTH ENUMERATION FAIL IN DESERIALIZE!");
                                if (leng + currpos <= bytes.Length)
                                {
                                    IntPtr pIP = Marshal.AllocHGlobal(leng);
                                    Marshal.Copy(bytes, currpos, pIP, leng);
                                    object o = Marshal.PtrToStructure(pIP, TT);
                                    vals.SetValue(o, i);
                                    Marshal.FreeHGlobal(pIP);
                                }
                                else
                                    vals.SetValue(null, i);
                                currpos += leng;
                            }
                        }
                        infos[currinfo].SetValue(thestructure, vals);
                    }
                    else
                    {
                        if (type.FullName != null && type.FullName.Contains("Message"))
                        {
                            if (GetMessageType(realtype) == MsgTypes.std_msgs__Time || GetMessageType(realtype) == MsgTypes.std_msgs__Duration || infos[currinfo].FieldType == typeof(TimeData))
                            {
                                TimeData td;
                                if (currpos + 8 <= bytes.Length)
                                {
                                    uint u1 = BitConverter.ToUInt32(bytes, currpos);
                                    uint u2 = BitConverter.ToUInt32(bytes, currpos + 4);
                                    td = new TimeData(u1, u2);
                                }
                                else
                                    td = new TimeData(0, 0);
                                currpos += 8;
                                if (infos[currinfo].FieldType == typeof(TimeData))
                                    infos[currinfo].SetValue(thestructure, td);
                                else if (GetMessageType(realtype) == MsgTypes.std_msgs__Time)
                                    infos[currinfo].SetValue(thestructure, (object)new std_msgs.Time(td));
                                else
                                    infos[currinfo].SetValue(thestructure, (object)new std_msgs.Duration(td));
                            }
                            else
                            {
                                byte[] piece = new byte[bytes.Length != 0 ? bytes.Length - currpos : 0];
                                if (bytes.Length != 0)
                                    Array.Copy(bytes, currpos, piece, 0, piece.Length);
                                int len = 0;
                                object obj = _deserialize(realtype, T, piece, out len,
                                                            IsSizeKnown(realtype, true));
                                //if ((int)(infos[currinfo].Attributes & FieldAttributes.InitOnly) != 0)
                                infos[currinfo].SetValue(thestructure, obj);
                                currpos += len;
                            }
                        }
                        else if (msgtype == MsgTypes.std_msgs__Bool)
                        {
                            infos[currinfo].SetValue(thestructure, bytes[currpos] != 0);
                            currpos++;
                        }
                        else
                        {
                            int len = Marshal.SizeOf(infos[currinfo].GetValue(thestructure));
                            IntPtr pIP = Marshal.AllocHGlobal(len);
                            object obj = null;
                            if (currpos + len <= bytes.Length)
                            {
                                Marshal.Copy(bytes, currpos, pIP, len);
                                obj = Marshal.PtrToStructure(pIP, infos[currinfo].FieldType);
                            }
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
                        Type TT = ft.GetElementType();
                        Array val = infos[currinfo].GetValue(thestructure) as Array;
                        int chunklen = 0;
                        if (val == null)
                        {
                            if (currpos + 4 <= bytes.Length)
                            {
                                chunklen = BitConverter.ToInt32(bytes, currpos);
                                currpos += 4;
                                val = Array.CreateInstance(TT, chunklen);
                            }
                            else
                            {
                                currpos += 4;
                                val = Array.CreateInstance(TT, 0);
                            }
                        }
                        else
                        {
                            chunklen = val.Length;
                        }
                        if (TT == null)
                            throw new Exception("LENGTHLESS ARRAY FAIL -- ELEMENT TYPE IS NULL!");
                        if (TT.FullName.Contains("Message."))
                            throw new Exception("NOT YET, YOUNG PATAWAN");
                        MsgTypes mt = GetMessageType(TT);
                        if (mt == MsgTypes.std_msgs__Byte || mt == MsgTypes.std_msgs__UInt8 || mt == MsgTypes.std_msgs__Int8)
                        {
                            int num = val.Length;
                            byte[] PWNED = new byte[num];
                            if (currpos + num <= bytes.Length)
                            {
                                Array.Copy(bytes, currpos, PWNED, 0, num);
                            }
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
                        else if (TT == typeof(string))
                        {
                            for (int i = 0; i < chunklen; i++)
                            {
                                int len = 0;
                                if (currpos + 4 <= bytes.Length)
                                    len = BitConverter.ToInt32(bytes, currpos);
                                byte[] piece = new byte[len];
                                currpos += 4;
                                if (currpos + len <= bytes.Length)
                                    Array.Copy(bytes, currpos, piece, 0, len);
                                string str = Encoding.ASCII.GetString(piece);
                                val.SetValue(str, i);
                                currpos += len;
                            }
                            infos[currinfo].SetValue(thestructure, val);
                        }
                        else
                        {
                            int len = Marshal.SizeOf(TT);
                            IntPtr pIP = IntPtr.Zero, sIP = IntPtr.Zero;
                            if (currpos + len * chunklen <= bytes.Length)
                            {
                                sIP = pIP = Marshal.AllocHGlobal(len * chunklen);
                                Marshal.Copy(bytes, currpos, pIP, len * chunklen);
                            }
                            object o = null;
                            for (int i = 0; i < chunklen * len; i += len)
                            {
                                if (pIP != IntPtr.Zero)
                                    o = Marshal.PtrToStructure(pIP, TT);
                                val.SetValue(o, i / len);
                                if (pIP != IntPtr.Zero)
                                    pIP = new IntPtr(pIP.ToInt32() + len);
                            }
                            infos[currinfo].SetValue(thestructure, val);
                            if (sIP != IntPtr.Zero)
                                Marshal.FreeHGlobal(sIP);
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
                            FieldInfo datafield = t.GetField("data");
                            if (!knownsize && (datafield != null && datafield.FieldType == typeof(string)))
                            {
                                int len = -4;
                                if (currpos + 4 <= bytes.Length)
                                    len = BitConverter.ToInt32(bytes, currpos);
                                byte[] smallerpiece = new byte[len + 4];
                                if (currpos + 4 <= bytes.Length)
                                    Array.Copy(bytes, currpos, smallerpiece, 0, smallerpiece.Length);
                                int dontcare = 0;
                                msg = _deserialize(t, T, smallerpiece, out dontcare, false) as IRosMessage;
                                if (bytes.Length != 0 && dontcare != len + 4)
                                    throw new Exception("WTF?!");
                                infos[currinfo].SetValue(thestructure, msg);
                                currpos += len + 4;
                            }
                            else // if (!knownsize)
                            {
                                byte[] smallerpiece = new byte[bytes.Length != 0 ? bytes.Length - currpos : 0];
                                if (bytes.Length != 0)
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
                                int len = 0;
                                if (currpos + 4 <= bytes.Length)
                                    len = BitConverter.ToInt32(bytes, currpos);
                                byte[] piece = new byte[len];
                                currpos += 4;
                                if (currpos + len <= bytes.Length)
                                    Array.Copy(bytes, currpos, piece, 0, len);
                                string str = Encoding.ASCII.GetString(piece);
                                infos[currinfo].SetValue(thestructure, str);
                                currpos += len;
                            }
                            else
                            {
                                //Console.WriteLine("ZOMG HANDLE: " + infos[currinfo].FieldType.FullName);
                                amountread = currpos - startingpos;
                                return thestructure;
                            }
                        }
                    }
                }
                currinfo++;
            }
            amountread = currpos - startingpos;
            infos = null;
            bytes = null;
            return thestructure;
        }


        internal static byte[] Serialize<T>(T outgoing)
            where T : IRosMessage, new()
        {
            return Serialize(outgoing, false);
        }

        internal static byte[] Serialize<T>(T outgoing, bool partofsomethingelse)
            where T : IRosMessage, new()
        {
            outgoing.Serialized = SlapChop(outgoing.GetType(), outgoing, partofsomethingelse);
            return outgoing.Serialized;
        }

        public static byte[] SlapChop(Type T, object instance)
        {
            return SlapChop(T, instance, false);
        }

        public static byte[] SlapChop(Type T, object instance, bool partofsomethingelse)
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
                    else if (info.FieldType.IsArray)
                        info.SetValue(instance, Array.CreateInstance(info.FieldType.GetElementType(), 0));
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
                else if (val is bool || T == typeof(bool))
                {
                    thischunk = new byte[1];
                    thischunk[0] = (byte)((bool)val ? 1 : 0);
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
                Array valArray = val as Array;
                List<byte> thechunk = new List<byte>();
                for (int i = 0; i < valArray.GetLength(0); i++)
                {
                    if (valArray.GetValue(i) == null)
                        valArray.SetValue(Activator.CreateInstance(T.GetElementType()), i);
                    Type TT = valArray.GetValue(i).GetType();
                    MsgTypes mt = GetMessageType(TT);
                    bool piecelengthknown = mt != MsgTypes.std_msgs__String;
                    thechunk.AddRange(NeedsMoreChunks(TT, valArray.GetValue(i), ref piecelengthknown));
                }
                if (!knownlength)
                {
                    thechunk.InsertRange(0, BitConverter.GetBytes(valArray.GetLength(0)));
                }
                return thechunk.ToArray();
            }
            return thischunk;
        }
    }
    [System.Diagnostics.DebuggerStepThrough]
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
        public MsgTypes message_type;

#if !TRACE
        [DebuggerStepThrough]
#endif
        public MsgFieldInfo(string name, bool isliteral, Type type, bool isconst, string constval, bool isarray,
            string lengths, bool meta, MsgTypes mt)
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
            message_type = mt;
        }
    }



    public enum ServiceMessageType
    {
        Not,
        Request,
        Response
    }
    [System.Diagnostics.DebuggerStepThrough]
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
