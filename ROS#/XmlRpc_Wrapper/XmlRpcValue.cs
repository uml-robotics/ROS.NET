#region USINGZ
//#define REFDEBUG
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

#endregion

namespace XmlRpc_Wrapper
{
    //TODO: OPERATOR GARBAGE?
    public class XmlRpcValue : IDisposable
    {


        #region Reference Tracking + unmanaged pointer management
        
        private IntPtr __instance;

        public void Dispose()
        {
            Clear();
        }

        ~XmlRpcValue()
        {
            Clear();
        }

        public bool Initialized
        {
            get
            {
                return __instance != IntPtr.Zero;
            }
        }

        private static Dictionary<IntPtr, int> _refs = new Dictionary<IntPtr, int>();
        private static object reflock = new object();
#if REFDEBUG
        private static Thread refdumper;
        private static void dumprefs()
        {
            while (true)
            {
                Dictionary<IntPtr, int> dainbrammage = null;
                lock (reflock)
                {
                    dainbrammage = new Dictionary<IntPtr, int>(_refs);
                }
                Console.WriteLine("REF DUMP");
                foreach (KeyValuePair<IntPtr, int> reff in dainbrammage)
                {
                    Console.WriteLine("\t" + reff.Key + " = " + reff.Value);
                    Console.WriteLine("\t" + new XmlRpcValue(reff.Key));
                }
                Thread.Sleep(500);
            }
        }
#endif

        public static XmlRpcValue LookUp(IntPtr ptr)
        {
            if (ptr != IntPtr.Zero)
            {
                AddRef(ptr);
                return new XmlRpcValue(ptr);
            }
            return null;
        }


        private static void AddRef(IntPtr ptr)
        {
#if REFDEBUG
            if (refdumper == null)
            {
                refdumper = new Thread(dumprefs);
                refdumper.IsBackground = true;
                refdumper.Start();
            }
#endif
            lock (reflock)
            {
                if (!_refs.ContainsKey(ptr))
                {
#if REFDEBUG
                    Console.WriteLine("Adding a new reference to: " + ptr + " (" + 0 + "==> " + 1 + ")");
#endif
                    _refs.Add(ptr, 1);
                }
                else
                {
#if REFDEBUG
                    Console.WriteLine("Adding a new reference to: " + ptr + " (" + _refs[ptr] + "==> " + (_refs[ptr] + 1) + ")");
#endif
                    _refs[ptr]++;
                }
            }
        }

        private static void RmRef(ref IntPtr ptr)
        {
            lock (reflock)
            {
                if (_refs.ContainsKey(ptr))
                {
#if REFDEBUG
                    Console.WriteLine("Removing a reference to: " + ptr + " (" + _refs[ptr] + "==> " + (_refs[ptr] - 1) + ")");
#endif
                    _refs[ptr]--;
                    if (_refs[ptr] <= 0)
                    {
#if REFDEBUG
                        Console.WriteLine("KILLING " + ptr + " BECAUSE IT'S A BITCH!");
#endif
                        _refs.Remove(ptr);
                        Clear(ptr);
                        ptr = IntPtr.Zero;
                    }
                    return;
                }
            }
        }

        public IntPtr instance
        {
            get
            {
                if (__instance == IntPtr.Zero)
                {
                    Console.WriteLine("UH OH MAKING A NEW INSTANCE IN instance.get!");
                    __instance = create();
                    AddRef(__instance);
                }
                return __instance;
            }
            set
            {
                if (value != IntPtr.Zero)
                {
                    if (__instance != IntPtr.Zero)
                        RmRef(ref __instance);
                    AddRef(value);
                    __instance = value;
                }
            }
        }

        #endregion

        public XmlRpcValue()
        {
            __instance = create();
            AddRef(__instance);
        }

        public XmlRpcValue(params object[] initialvalues)
            : this()
        {
            for (int i = 0; i < initialvalues.Length; i++)
            {
                int ires = 0;
                double dres = 0;
                bool bres = false;
                if (initialvalues[i] == null)
                {
                    Set(i, "");
                    continue;
                }
                if (int.TryParse(initialvalues[i].ToString(), out ires))
                {
                    Set(i, ires);
                    continue;
                }
                if (double.TryParse(initialvalues[i].ToString(), out dres))
                {
                    Set(i, dres);
                    continue;
                }
                if (bool.TryParse(initialvalues[i].ToString(), out bres))
                {
                    Set(i, bres);
                    continue;
                }
                Set(i, initialvalues[i].ToString());
            }
        }

        public XmlRpcValue(bool value)
        {
            __instance = create(value);
            AddRef(__instance);
        }

        public XmlRpcValue(int value)
        {
            __instance = create(value);
            AddRef(__instance);
        }

        public XmlRpcValue(double value)
        {
            __instance = create(value);

            AddRef(__instance);
        }

        public XmlRpcValue(string value)
        {
            __instance = create(value);
            AddRef(__instance);
        }

        /*public XmlRpcValue(IntPtr value, int nBytes)
        {
            __instance = create(value, nBytes);
            if (!_instances.ContainsKey(instance))
                _instances.Add(instance, this);
        }

        public XmlRpcValue(string xml, int offset)
        {
            __instance = create(xml, offset);
            if (!_instances.ContainsKey(instance))
                _instances.Add(instance, this);
        }*/

        public XmlRpcValue(XmlRpcValue value)
            : this(value.instance)
        {
        }

        public XmlRpcValue(IntPtr existingptr)
        {
            if (existingptr == IntPtr.Zero)
                throw new Exception("SUCK IS CONTAGEOUS!");
            __instance = existingptr;
            AddRef(existingptr);
        }

        public static bool operator ==(XmlRpcValue left, XmlRpcValue right)
        {
            return left != null && right != null && (left.__instance == right.__instance);
        }
        public static bool operator !=(XmlRpcValue left, XmlRpcValue right)
        {
            return left == null || right == null || (left.__instance != right.__instance);
        }
        public override bool Equals(object obj)
        {
            XmlRpcValue comp = obj as XmlRpcValue;
            if (comp == null)
                return false;
            return ((__instance == comp.__instance) && (__instance != IntPtr.Zero)) || (this != comp);
        }
        public override int GetHashCode()
        {
            if (__instance != IntPtr.Zero)
                return __instance.ToInt32();
            return base.GetHashCode();
        }

        #region P/Invoke

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Create1", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr create();

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Create2", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr create(bool value);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Create3", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr create(int value);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Create4", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr create(double value);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Create5", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr create([In] [MarshalAs(UnmanagedType.LPStr)] string value);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Create6", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr create(IntPtr value, int nBytes);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Create7", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr create([In] [MarshalAs(UnmanagedType.LPStr)] string xml, int offset);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Create8", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr create(IntPtr rhs);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Clear", CallingConvention = CallingConvention.Cdecl)]
        private static extern void clear(IntPtr Target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Valid", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool valid(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Type", CallingConvention = CallingConvention.Cdecl)]
        private static extern int gettype(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Size", CallingConvention = CallingConvention.Cdecl)]
        private static extern int getsize(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_SetSize", CallingConvention = CallingConvention.Cdecl)]
        private static extern void setsize(IntPtr target, int size);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_HasMember", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool hasmember(IntPtr target, [In] [MarshalAs(UnmanagedType.LPStr)] string name);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Set1", CallingConvention = CallingConvention.Cdecl)]
        private static extern void set(IntPtr target, int key, [In] [MarshalAs(UnmanagedType.LPStr)] string value);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Set2", CallingConvention = CallingConvention.Cdecl)]
        private static extern void set(IntPtr target, [In] [MarshalAs(UnmanagedType.LPStr)] string key, [In] [MarshalAs(UnmanagedType.LPStr)] string value);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Set3", CallingConvention = CallingConvention.Cdecl)]
        private static extern void set(IntPtr target, int key, IntPtr value);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Set4", CallingConvention = CallingConvention.Cdecl)]
        private static extern void set(IntPtr target, [In] [MarshalAs(UnmanagedType.LPStr)] string key, IntPtr value);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Set5", CallingConvention = CallingConvention.Cdecl)]
        private static extern void set(IntPtr target, int key, int value);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Set6", CallingConvention = CallingConvention.Cdecl)]
        private static extern void set(IntPtr target, [In] [MarshalAs(UnmanagedType.LPStr)] string key, int value);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Set7", CallingConvention = CallingConvention.Cdecl)]
        private static extern void set(IntPtr target, int key, bool value);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Set8", CallingConvention = CallingConvention.Cdecl)]
        private static extern void set(IntPtr target, [In] [MarshalAs(UnmanagedType.LPStr)] string key, bool value);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Set9", CallingConvention = CallingConvention.Cdecl)]
        private static extern void set(IntPtr target, int key, double value);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Set10", CallingConvention = CallingConvention.Cdecl)]
        private static extern void set(IntPtr target, [In] [MarshalAs(UnmanagedType.LPStr)] string key, double value);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Get1", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr get(IntPtr target, int key);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Get2", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr get(IntPtr target, [In] [MarshalAs(UnmanagedType.LPStr)] string key);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_GetInt0", CallingConvention = CallingConvention.Cdecl)]
        private static extern int getint(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_GetInt1", CallingConvention = CallingConvention.Cdecl)]
        private static extern int getint(IntPtr target, int key);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_GetInt2", CallingConvention = CallingConvention.Cdecl)]
        private static extern int getint(IntPtr target, [In] [MarshalAs(UnmanagedType.LPStr)] string key);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_GetString0", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr getstring(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_GetString1", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr getstring(IntPtr target, int key);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_GetString2", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr getstring(IntPtr target, [In] [MarshalAs(UnmanagedType.LPStr)] string key);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_GetBool0", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool getbool(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_GetBool1", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool getbool(IntPtr target, int key);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_GetBool2", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool getbool(IntPtr target, [In] [MarshalAs(UnmanagedType.LPStr)] string key);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_GetDouble0", CallingConvention = CallingConvention.Cdecl)]
        private static extern double getdouble(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_GetDouble1", CallingConvention = CallingConvention.Cdecl)]
        private static extern double getdouble(IntPtr target, int key);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_GetDouble2", CallingConvention = CallingConvention.Cdecl)]
        private static extern double getdouble(IntPtr target, [In] [MarshalAs(UnmanagedType.LPStr)] string key);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Dump", CallingConvention = CallingConvention.Cdecl)]
        private static extern void dump(IntPtr target);
        #endregion

        public TypeEnum Type
        {
            get
            {
                if (!Initialized)
                    return TypeEnum.TypeInvalid;
                int balls = gettype(instance);
                if (balls < 0 || balls >= ValueTypeHelper._typearray.Length)
                    return TypeEnum.TypeIDFK;
                return ValueTypeHelper._typearray[balls];
            }
        }

        public bool Valid
        {
            get
            {
                SegFault();
                if (!Initialized)
                    return false;
                return valid(__instance);
            }
        }

        public int Size
        {
            get
            {
                SegFault();
                if (!Valid || Type == TypeEnum.TypeInvalid || Type == TypeEnum.TypeIDFK)
                    return 0;
                if (Type != TypeEnum.TypeString && Type != TypeEnum.TypeStruct && Type != TypeEnum.TypeArray)
                    return 0;
                return getsize(instance);
            }
            set
            {
                SegFault();
                setsize(instance, value);
            }
        }

        /*public new Type GetType()
        {
            return GetType(Type);
        }*/

        /*public object GetRobust()
        {
            int iret = 0;
            string sret = "";
            double dret = 0;
            bool bret = false;
            iret = Get<int>();
            if (iret != default(int))
                return iret;
            sret = Get<string>();
            if (sret != "")
                return sret;
            dret = Get<double>();
            if (dret != default(double))
                return dret;
            bret = Get<bool>();
            if (bret != default(bool))
                return bret;
            return null;
        }

        public object GetRobust(int key)
        {
            int iret = 0;
            string sret = "";
            double dret = 0;
            bool bret = false;
            iret = Get<int>(key);
            if (iret != default(int))
                return iret;
            sret = Get<string>(key);
            if (sret != "")
                return sret;
            dret = Get<double>(key);
            if (dret != default(double))
                return dret;
            bret = Get<bool>(key);
            if (bret != default(bool))
                return bret;
            return null;
        }

        public object GetRobust(string key)
        {
            int iret = 0;
            string sret = "";
            double dret = 0;
            bool bret = false;
            iret = Get<int>(key);
            if (iret != default(int))
                return iret;
            sret = Get<string>(key);
            if (sret != "")
                return sret;
            dret = Get<double>(key);
            if (dret != default(double))
                return dret;
            bret = Get<bool>(key);
            if (bret != default(bool))
                return bret;
            return null;
        }*/

        /*public T GetClass<T>() where T : class, new()
        {
            T ret = (T)GetClass(new T());
            return T;
        }

        public object GetClass<T>(T t)
        {

        }*/

        public T Get<T>()// where T : struct
        {
            if (!Valid)
            {
                Console.WriteLine("Trying to get something with an invalid size... BAD JUJU!\n\t" + this);
            }
            else if ("" is T)
            {
                return (T)(object)GetString();
            }
            else if ((int)0 is T)
            {
                return (T)(object)GetInt();
            }
            else if (this is T)
            {
                return (T)(object)this;
            }
            else if (true is T)
            {
                return (T)(object)GetBool();
            }
            else if (0d is T)
            {
                return (T)(object)GetDouble();
            }
            Console.WriteLine("I DUNNO WHAT THAT IS!");
            return default(T);
        }

        public T Get<T>(int key)
        {
            if ("" is T)
                return Get(key).Get<T>();
            T ret = Get(key).Get<T>();
            if (ret == null)
                throw new Exception("Null return!");
            return ret;
        }

        public T Get<T>(string key)
        {
            T ret = Get(key).Get<T>();
            if (ret == null)
                throw new Exception("Null return!");
            return ret;
        }

        /*public object Get<T>(int key)
        {
            return Get(key).Get<T>();
            if (!Valid)
            {
                Console.WriteLine("Trying to get something with an invalid size... BAD JUJU!\n\t" + this);
            }
            else if (t is string)
            {
                return GetString(key);
            }
            else if (t is int)
            {
                return GetInt(key);
            }
            else if (t is XmlRpcValue)
            {
                return Get(key);
            }
            else if (t is bool)
            {
                return GetBool(key);
            }
            else if (t is double)
            {
                return GetDouble(key);
            }
            return default(T);
        }

        public object Get<T>(T t, string key)
        {
            return Get(key).Get<T>();
            if (!Valid)
            {
                Console.WriteLine("Trying to get something with an invalid size... BAD JUJU!\n\t" + this);
            }
            else if (t is string)
            {
                return GetString(key);
            }
            else if (t is int)
            {
                return GetInt(key);
            }
            else if (t is XmlRpcValue)
            {
                return Get(key);
            }
            else if (t is bool)
            {
                return GetBool(key);
            }
            else if (t is double)
            {
                return GetDouble(key);
            }
            return default(T);
        }*/

        public int GetInt()
        {
            SegFault();
            return getint(__instance);
        }

        /*public int GetInt(int key)
        {
            SegFault();
            return getint(instance, key);
        }

        public int GetInt(string key)
        {
            SegFault();
            return getint(instance, key);
        }*/

        public string GetString()
        {
            SegFault();
            return Marshal.PtrToStringAnsi(getstring(__instance));
        }

        /*public string GetString(int key)
        {
            SegFault();
            string st =  getstring(instance, key);
            if (st == null)
                throw new Exception("Value returned null string!");
            return st;
        }

        public string GetString(string key)
        {
            SegFault();
            string st = getstring(instance, key);
            if (st == null)
                throw new Exception("Value returned null string!");
            return st;
        }*/

        public bool GetBool()
        {
            SegFault();
            return getbool(__instance);
        }

        public bool GetBool(int key)
        {
            SegFault();
            return getbool(__instance, key);
        }

        public bool GetBool(string key)
        {
            SegFault();
            return getbool(__instance, key);
        }

        public double GetDouble()
        {
            SegFault();
            return getdouble(__instance);
        }

        public double GetDouble(int key)
        {
            SegFault();
            return getdouble(__instance, key);
        }

        public double GetDouble(string key)
        {
            SegFault();
            return getdouble(__instance, key);
        }

        public XmlRpcValue Get(int key)
        {
            SegFault();
            IntPtr othervalue = get(instance, key);
            return LookUp(othervalue);
        }

        public XmlRpcValue Get(string key)
        {
            SegFault();
            IntPtr othervalue = get(instance, key);
            return LookUp(othervalue);
        }

        public override string ToString()
        {
            if (__instance == IntPtr.Zero)
                return "this XmlRpcValue == (NULL)";
            string s = "XmlRpcValue ( " + Type.ToString() + " ) -- size = " + Size;
            return s;
        }

        public static XmlRpcValue Create(ref IntPtr existingvalue)
        {
            if (existingvalue == IntPtr.Zero)
            {
                XmlRpcValue PSYCHE = new XmlRpcValue();
                existingvalue = PSYCHE.__instance;
                return PSYCHE;
            }
            return new XmlRpcValue(existingvalue);
        }

        public void Set(int key, int value)
        {
            SegFault();
            set(instance, key, value);
        }

        public void Set(string key, int value)
        {
            SegFault();
            set(instance, key, value);
        }

        public void Set(int key, bool value)
        {
            SegFault();
            set(instance, key, value);
        }

        public void Set(string key, bool value)
        {
            SegFault();
            set(instance, key, value);
        }

        public void Set(int key, double value)
        {
            SegFault();
            set(instance, key, value);
        }

        public void Set(string key, double value)
        {
            SegFault();
            set(instance, key, value);
        }

        public void Set(int key, string value)
        {
            SegFault();
            set(instance, key, value);
        }

        public void Set(string key, string value)
        {
            SegFault();
            set(instance, key, value);
        }

        public void Set(int key, XmlRpcValue value)
        {
            SegFault();
            set(instance, key, value.instance);
        }

        public void Set(string key, XmlRpcValue value)
        {
            SegFault();
            set(instance, key, value.instance);
        }

        public bool HasMember(string name)
        {
            SegFault();
            return hasmember(instance, name);
        }

        public void Dump()
        {
            SegFault();
            dump(__instance);
        }

        public void Clear()
        {
            if (Clear(__instance)) Dispose();
        }

        public static bool Clear(IntPtr ptr)
        {
            if (ptr != IntPtr.Zero)
            {
                RmRef(ref ptr);
                return (ptr == IntPtr.Zero);
            }
            return true;
        }

        public void SegFault()
        {
            if (__instance == IntPtr.Zero)
            {
                if (!Initialized)
                {
                    Console.WriteLine("SAVING YOUR ASS!");
                    __instance = create();
                    AddRef(__instance);
                }
                else
                    Console.WriteLine("IF YOU DEREFERENCE A NULL POINTER AGAIN I'LL PUNCH YOU IN THE ASS!");
            }
        }

        #region myjunk
        /*
        public Type GetType(TypeEnum t)
        {
            switch (t)
            {
                case TypeEnum.TypeBoolean:
                    return typeof(bool);
                case TypeEnum.TypeInt:
                    return typeof(int);
                case TypeEnum.TypeDouble:
                    return typeof(double);
                case TypeEnum.TypeString:
                    return typeof(string);
                case TypeEnum.TypeDateTime:
                    return typeof(DateTime);
                case TypeEnum.TypeBase64:
                    return typeof(UInt64);
                case TypeEnum.TypeArray:
                    return typeof(object[]);
                case TypeEnum.TypeStruct:
                    throw new Exception("STRUCT IN XMLRPCVALUE ZOMFG WTF");
            }
            return default(Type);
        }*/

        #endregion
    }

    public static class ValueTypeHelper
    {
        public static TypeEnum[] _typearray = new[]
                                            {
                                                TypeEnum.TypeInvalid,
                                                TypeEnum.TypeBoolean,
                                                TypeEnum.TypeInt,
                                                TypeEnum.TypeDouble,
                                                TypeEnum.TypeString,
                                                TypeEnum.TypeDateTime,
                                                TypeEnum.TypeBase64,
                                                TypeEnum.TypeArray,
                                                TypeEnum.TypeStruct,
                                                TypeEnum.TypeIDFK
                                            };

    }

    public enum TypeEnum
    {
        TypeInvalid,
        TypeBoolean,
        TypeInt,
        TypeDouble,
        TypeString,
        TypeDateTime,
        TypeBase64,
        TypeArray,
        TypeStruct,
        TypeIDFK
    }
}