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
#else
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
    public static class SerializationHelper
    {
        static Dictionary<Type, MsgTypes> GetMessageTypeMemo = new Dictionary<Type, MsgTypes>();
        [System.Diagnostics.DebuggerStepThrough]
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
        [System.Diagnostics.DebuggerStepThrough]
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
                    ret = Type.GetType(s, true, true);
                }
                else
                    ret = IRosMessage.generate(mt).GetType();
                return (GetTypeTypeMemo[s] = ret);
            }
        }

        static Dictionary<string, MsgTypes> GetMessageTypeMemoString = new Dictionary<string, MsgTypes>();
        [System.Diagnostics.DebuggerStepThrough]
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

        private static Dictionary<Type, FieldInfo[]> speedyFields = new Dictionary<Type, FieldInfo[]>();
        [System.Diagnostics.DebuggerStepThrough]
        public static FieldInfo[] GetFields(Type T, out IRosMessage msg)
        {
            object dontcare = null;
            return GetFields(T, ref dontcare, out msg);
        }
        [System.Diagnostics.DebuggerStepThrough]
        public static FieldInfo[] GetFields(Type T, ref object instance)
        {
            IRosMessage dontcare = null;
            return GetFields(T, ref instance, out dontcare);
        }
        [System.Diagnostics.DebuggerStepThrough]
        public static FieldInfo[] GetFields(Type T, ref object instance, out IRosMessage msg)
        {
            if (T.IsArray)
                throw new Exception("Called GetFields for an array type. Bad form!");
            if (instance == null)
            {
                if (T.IsArray) //This will never trip
                {
                    T = T.GetElementType();
                    instance = Array.CreateInstance(T, 0);
                }
                else if (T != typeof (string))
                    instance = Activator.CreateInstance(T);
                else
                    instance = (object)"";
            }
            IRosMessage MSG = instance as IRosMessage;
            if (instance != null && MSG == null)
                throw new Exception("Garbage");
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
                    return (speedyFields[T] = MSG.GetType().GetFields().Where((fi => MSG.Fields.Keys.Contains(fi.Name) && !fi.IsStatic)).ToArray());
                }
            }
            throw new Exception("GetFields is weaksauce");
        }
        [System.Diagnostics.DebuggerStepThrough]
        public static string dumphex(byte[] test)
        {
            if (test == null)
                return "dumphex(null)";
            string s = "";
            for (int i = 0; i < test.Length; i++)
                s += (test[i] < 16 ? "0" : "") + test[i].ToString("x") + " ";
            return s;
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
