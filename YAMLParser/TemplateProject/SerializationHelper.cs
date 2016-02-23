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
#if !TRACE
    [System.Diagnostics.DebuggerStepThrough]
#endif
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class SerializationHelper
    {
        static Dictionary<Type, MsgTypes> GetMessageTypeMemo = new Dictionary<Type, MsgTypes>();
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

        internal static void Deserialize<T>(ref T t, byte[] bytes) where T : IRosMessage, new()
        {

            Deserialize<T>(ref t, bytes, null);

        }

        internal static void Deserialize<T>(ref T t, byte[] bytes, Type container) where T : IRosMessage, new()
        {
            int dontcare = 0;
            object o = t;
            _deserialize(ref o, typeof(T), container, bytes, out dontcare, IsSizeKnown(typeof(T), true));
            t = o as T;
        }
        public static void deserialize(object t, Type T, Type container, byte[] bytes, out int amountread)
        {
            deserialize(t, T, container, bytes, out amountread, false);
        }


        public static void deserialize(object t, Type T, Type container, byte[] bytes, out int amountread, bool sizeknown)
        {
            try
            {
                object o = t;
                _deserialize(ref t, T, container, bytes, out amountread, sizeknown);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            amountread = 0;
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

        private static void _deserialize(ref object thestructure, Type T, Type container, byte[] bytes, out int amountread, bool sizeknown)
        {
            int currpos = 0;
            _deserialize(ref thestructure, ref currpos, T, container, bytes, out amountread, sizeknown);
        }
        private static void _deserialize(ref object thestructure, ref int currpos, Type T, Type container, byte[] bytes, out int amountread, bool sizeknown)
        {
            if (bytes == null || bytes.Length - currpos == 0)
            {
                amountread = 0;
                return;
            }
            if (T.FullName.StartsWith("System.") && !T.IsCOMObject && !T.IsArray && T != typeof(string))
            {
                //shortcut for non-string system structs
                if (thestructure == null)
                    thestructure = new object();
                int size = Marshal.SizeOf(T);
                IntPtr mem = IntPtr.Zero;
                if (bytes.Length != 0)
                {
                    mem = Marshal.AllocHGlobal(size);
                    Marshal.Copy(bytes, currpos, mem, size);
                }
                amountread = size;
                currpos += size;
                if (mem == IntPtr.Zero) return;
                thestructure = Marshal.PtrToStructure(mem, T);
                Marshal.FreeHGlobal(mem);
                return;
            }
            IRosMessage MSG;
            int startingpos = currpos;
            MsgTypes MT = MsgTypes.Unknown;
            FieldInfo[] infos = GetFields(T, ref thestructure, out MSG);
            if (MSG != null)
                MT = MSG.msgtype;

            //iterate over all fields of MSG, recursing for a sub-message where necessary
            int currinfo = 0;
            while ((currpos < bytes.Length) && currinfo < infos.Length)
            {
                FieldInfo info = infos[currinfo];
                MsgFieldInfo mfi = MSG.Fields[info.Name];
                Type realtype = info.FieldType;
                Type type = GetType(realtype.FullName);
                bool isMessage = realtype.FullName != null && realtype.FullName.StartsWith("Message");
                MsgTypes msgtype = GetMessageType(type);
                MsgTypes realmsgtype = GetMessageType(realtype);
                bool knownpiecelength = IsSizeKnown(realtype, true);

                if (realtype.IsArray)
                {
#region ARRAY
#region ARRAY THAT CAN BE HANDLED AS A WHOLE
                    if (realtype == typeof (Byte[])) //this is deplorable.
                    {
                        //SHORTCUT FOR BYTE ARRAYS
                        int num = mfi.Length;
                        if (mfi.Length == -1) //if -1, then array length not in definition
                        {
                            num = BitConverter.ToInt32(bytes, currpos);
                            currpos += 4;
                        }
                        byte[] PWNED = new byte[num];
                        Array.Copy(bytes, currpos, PWNED, 0, num);
                        currpos += PWNED.Length;
                        info.SetValue(thestructure, PWNED);
                        PWNED = null;
                    }
                    else if (realtype == typeof (Boolean[]))
                    {
                        //SHORTCUT FOR BOOL ARRAYS
                        int num = mfi.Length;
                        if (mfi.Length == -1) //if -1, then array length not in definition
                        {
                            num = BitConverter.ToInt32(bytes, currpos);
                            currpos += 4;
                        }
                        bool[] PWNED = new bool[num];
                        for (int i = 0; i < num; i++)
                        {
                            PWNED[i] = bytes[i + currpos] > 0;
                        }
                        currpos += PWNED.Length;
                        info.SetValue(thestructure, PWNED);
                        PWNED = null;
                    }
#endregion
                    else
                    {
                        Type ArrayElementType = GetType(realtype.GetElementType().FullName);
                        MsgTypes ArrayElementMsgType = GetMessageType(ArrayElementType);
                        bool ElementSizeKnown = IsSizeKnown(ArrayElementType, true);
                        bool isElementMessage = ArrayElementType.FullName.StartsWith("Messages.");
                        Array vals = (info.GetValue(thestructure) as Array);
#region determine array element size (if determinable)
                        int chunklen = -1;
                        if (vals == null)
                        {
                            if (currpos + 4 <= bytes.Length)
                            {
                                chunklen = BitConverter.ToInt32(bytes, currpos);
                                currpos += 4;
                                vals = Array.CreateInstance(ArrayElementType, chunklen);
                            }
                            else
                            {
                                currpos += 4;
                                vals = Array.CreateInstance(ArrayElementType, 0);
                            }
                        }
                        else
                        {
                            chunklen = vals.Length;
                        }

                        if (chunklen == -1)
                            throw new Exception("Could not determine number of array elements to deserialize");
#endregion
                        if (isElementMessage)
                        {
                            //array of other messages
                            int len;
                            for (int i = 0; i < chunklen; i++)
                            {
                                len = 0;
                                object data = null;
                                _deserialize(ref data, ref currpos, ArrayElementType, T, bytes, out len, ElementSizeKnown);
                                vals.SetValue(data, i);
                            }
                        }
                        else
                        {
                            //Array of literals with known size
                            int len = Marshal.SizeOf(ArrayElementType);
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
                                    o = Marshal.PtrToStructure(pIP, ArrayElementType);
                                vals.SetValue(o, i / len);
                                if (pIP != IntPtr.Zero)
                                    pIP = new IntPtr(pIP.ToInt32() + len);
                            }
                            if (sIP != IntPtr.Zero)
                                Marshal.FreeHGlobal(sIP);
                            currpos += chunklen * len;
                        }
                        info.SetValue(thestructure, vals);
                    }
#endregion
                }
                else
                {
#region NON-ARRAY
                    if (isMessage)
                    {
                        if (msgtype == MsgTypes.std_msgs__Bool)
                        {
                            //special cased because of incompatible .NET byte representation of booleans
                            info.SetValue(thestructure, bytes[currpos] != 0);
                            currpos++;
                        }
                        else if (realmsgtype == MsgTypes.std_msgs__Time || realmsgtype == MsgTypes.std_msgs__Duration || info.FieldType == typeof (TimeData))
                        {
                            //special cased because of custom 8-byte time struct
                            TimeData td;
                            if (currpos + 8 <= bytes.Length)
                            {
                                uint u1 = BitConverter.ToUInt32(bytes, currpos);
                                uint u2 = BitConverter.ToUInt32(bytes, currpos + 4);
                                td = new TimeData(u1, u2);
                            }
                            else
                                throw new Exception("TimeData fail");
                            currpos += 8;
                            if (info.FieldType == typeof (TimeData))
                                info.SetValue(thestructure, td);
                            else if (GetMessageType(realtype) == MsgTypes.std_msgs__Time)
                                info.SetValue(thestructure, (object) new std_msgs.Time(td));
                            else
                                info.SetValue(thestructure, (object) new std_msgs.Duration(td));
                        }
                        else
                        {
                            //all other message types, with extra verification for the length of strings
                            int len = 0;
                            object obj = null;
                            FieldInfo datafield = null;
                            bool isastring = false;
                            if (!knownpiecelength)
                            {
                                datafield = realtype.GetField("data");
                                isastring = (datafield != null && datafield.FieldType == typeof (string));
                            }
                            if (isastring)
                            {   
                                if (currpos + 4 <= bytes.Length)
                                    len = BitConverter.ToInt32(bytes, currpos);
                            }
                            int dontcare = 0;
                            _deserialize(ref obj, ref currpos, realtype, T, bytes, out dontcare, knownpiecelength);
                            if (isastring && bytes.Length != 0 && dontcare != len + 4)
                                throw new Exception("WTF?!");
                            info.SetValue(thestructure, obj);
                        }
                    }
                    else //if (!isMessage)
                    {
                        if (knownpiecelength)
                        {
                            //non-array literal
                            int len = Marshal.SizeOf(info.GetValue(thestructure));
                            IntPtr pIP = Marshal.AllocHGlobal(len);
                            object obj = null;
                            if (currpos + len <= bytes.Length)
                            {
                                Marshal.Copy(bytes, currpos, pIP, len);
                                obj = Marshal.PtrToStructure(pIP, info.FieldType);
                            }
                            info.SetValue(thestructure, obj);
                            Marshal.FreeHGlobal(pIP);
                            currpos += len;
                        }
                        else // must be a length-prefixed string
                        {
                            int len = 0;
                            if (currpos + 4 <= bytes.Length)
                                len = BitConverter.ToInt32(bytes, currpos);
                            currpos += 4;
                            string str = Encoding.ASCII.GetString(bytes, currpos, len);
                            info.SetValue(thestructure, str);
                            currpos += len;
                        }
                    }

                    #endregion
                }
                currinfo++;
            }
            amountread = currpos - startingpos;
            infos = null;
            bytes = null;
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
                for (int i = 0; i < 4; i++)
                    wholeshebang[i] = len[i];
                currpos = 4;
            }
            while (chunks.Count > 0)
            {
                byte[] chunk = chunks.Dequeue();
                for (int i = 0; i < chunk.Length; i++)
                    wholeshebang[currpos + i] = chunk[i];
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
