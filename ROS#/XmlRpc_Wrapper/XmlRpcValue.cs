#region USINGZ

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

#endregion

namespace XmlRpc_Wrapper
{
    //TODO: OPERATOR GARBAGE?
    public class XmlRpcValue : IDisposable
    {
        private static Dictionary<IntPtr, XmlRpcValue> _instances = new Dictionary<IntPtr, XmlRpcValue>();

        private static Dictionary<IntPtr, XmlRpcValue> ValueRegistry;

        private IntPtr __instance;

        public XmlRpcValue(params object[] initialvalues) : this()
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

        public XmlRpcValue()
        {
            instance = create();
            if (!_instances.ContainsKey(instance))
                _instances.Add(instance, this);
        }

        public XmlRpcValue(bool value)
        {
            instance = create(value);
            if (!_instances.ContainsKey(instance))
                _instances.Add(instance, this);
        }

        public XmlRpcValue(int value)
        {
            instance = create(value);
            if (!_instances.ContainsKey(instance))
                _instances.Add(instance, this);
        }

        public XmlRpcValue(double value)
        {
            instance = create(value);
            if (!_instances.ContainsKey(instance))
                _instances.Add(instance, this);
        }

        public XmlRpcValue(string value)
        {
            instance = create(value);
            if (!_instances.ContainsKey(instance))
                _instances.Add(instance, this);
        }

        public XmlRpcValue(IntPtr value, int nBytes)
        {
            instance = create(value, nBytes);
            if (!_instances.ContainsKey(instance))
                _instances.Add(instance, this);
        }

        public XmlRpcValue(string xml, int offset)
        {
            instance = create(xml, offset);
            if (!_instances.ContainsKey(instance))
                _instances.Add(instance, this);
        }

        public XmlRpcValue(XmlRpcValue value) : this(value.instance)
        {
        }

        public XmlRpcValue(IntPtr existingptr)
        {
            instance = existingptr;
            if (!_instances.ContainsKey(instance))
                _instances.Add(instance, this);
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

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Close", CallingConvention = CallingConvention.Cdecl)]
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
        [return: MarshalAs(UnmanagedType.LPStr)]
        private static extern string getstring(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_GetString1", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        private static extern string getstring(IntPtr target, int key);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_GetString2", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        private static extern string getstring(IntPtr target, [In] [MarshalAs(UnmanagedType.LPStr)] string key);

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

        public IntPtr instance
        {
            get { return __instance; }
            set
            {
                __instance = value;
                if (ValueRegistry == null)
                    ValueRegistry = new Dictionary<IntPtr, XmlRpcValue>();
                if (!ValueRegistry.ContainsKey(__instance))
                    ValueRegistry.Add(__instance, this);
            }
        }

        public TypeEnum Type
        {
            get
            {
                SegFault();
                return _typearray[gettype(instance)];
            }
        }

        public bool Valid
        {
            get
            {
                SegFault();
                return valid(instance);
            }
        }

        public int Size
        {
            get
            {
                SegFault();
                if (!Valid)
                    return 0;
                return getsize(instance);
            }
            set
            {
                SegFault();
                setsize(instance, value);
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (instance == IntPtr.Zero) return;
            clear(instance);
            instance = IntPtr.Zero;
        }

        #endregion

        public static XmlRpcValue LookUp(IntPtr ptr)
        {
            if (_instances.ContainsKey(ptr))
                return _instances[ptr];
            if (ptr != IntPtr.Zero)
                return new XmlRpcValue(ptr);
            return null;
        }

        public new Type GetType()
        {
            return GetType(Type);
        }

        public object GetRobust()
        {
            int iret = 0;
            string sret = "";
            double dret = 0;
            bool bret = false;
            iret = Get<int>();
            if (iret != default(int))
                return iret;
            sret = Get<string>();
            if (sret != default(string))
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
            if (sret != default(string))
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
            if (sret != default(string))
                return sret;
            dret = Get<double>(key);
            if (dret != default(double))
                return dret;
            bret = Get<bool>(key);
            if (bret != default(bool))
                return bret;
            return null;
        }

        public T Get<T>()
        {
            T ret = (T)Get(default(T));
            return ret;
        }

        public T Get<T>(int key)
        {
            T ret = (T)Get(default(T), key);
            return ret;
        }

        public T Get<T>(string key)
        {
            T ret = (T)Get(default(T), key);
            return ret;
        }

        public object Get<T>(T t)
        {
            if (!Valid)
            {
                Console.WriteLine("Trying to get something with an invalid size... BAD JUJU!\n\t"+this);
            }
            else if (t is string)
            {
                return GetString();
            }
            else if (t is int)
            {
                return GetInt();
            }
            else if (t is XmlRpcValue)
            {
                return this;
            }
            else if (t is bool)
            {
                return GetBool();
            }
            else if (t is double)
            {
                return GetDouble();
            }
            return default(T);
        }

        public object Get<T>(T t, int key)
        {
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

        public int GetInt()
        {
            SegFault();
            return getint(instance);
        }

        public int GetInt(int key)
        {
            SegFault();
            return getint(instance, key);
        }

        public int GetInt(string key)
        {
            SegFault();
            return getint(instance, key);
        }

        public string GetString()
        {
            SegFault();
            return getstring(instance);
        }

        public string GetString(int key)
        {
            SegFault();
            return getstring(instance, key);
        }

        public string GetString(string key)
        {
            SegFault();
            return getstring(instance, key);
        }

        public bool GetBool()
        {
            return getbool(instance);
        }

        public bool GetBool(int key)
        {
            SegFault();
            return getbool(instance, key);
        }

        public bool GetBool(string key)
        {
            SegFault();
            return getbool(instance, key);
        }

        public double GetDouble()
        {
            return getdouble(instance);
        }

        public double GetDouble(int key)
        {
            SegFault();
            return getdouble(instance, key);
        }

        public double GetDouble(string key)
        {
            SegFault();
            return getdouble(instance, key);
        }

        public XmlRpcValue Get(int key)
        {
            SegFault();
            return Create(get(instance, key));
        }

        public XmlRpcValue Get(string key)
        {
            SegFault();
            return Create(get(instance, key));
        }

        public override string ToString()
        {
            if (instance == IntPtr.Zero)
                return "this XmlRpcValue == (NULL)";
            return Dump();
        }

        public static XmlRpcValue Create(IntPtr existingvalue)
        {
            if (ValueRegistry != null && ValueRegistry.ContainsKey(existingvalue))
                return ValueRegistry[existingvalue];
            return new XmlRpcValue();
        }
        public string Dump()
        {
            SegFault();
            if (Valid)
            {
                try
                {
                    dump(instance);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            return "WHOAH DAMN";
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

        public void Clear()
        {
            if (instance != IntPtr.Zero)
            {
                clear(instance);
                instance = IntPtr.Zero;
            }
        }

        public void SegFault()
        {
            if (instance == IntPtr.Zero)
            {
                throw new Exception("IF YOU DEREFERENCE A NULL POINTER AGAIN I'LL PUNCH YOU IN THE ASS!");
            }
        }

        #region myjunk

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

        private TypeEnum[] _typearray = new[]
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

        public Type GetType(TypeEnum t)
        {
            switch (t)
            {
                case TypeEnum.TypeBoolean:
                    return typeof (bool);
                case TypeEnum.TypeInt:
                    return typeof (int);
                case TypeEnum.TypeDouble:
                    return typeof (double);
                case TypeEnum.TypeString:
                    return typeof (string);
                case TypeEnum.TypeDateTime:
                    return typeof (DateTime);
                case TypeEnum.TypeBase64:
                    return typeof (UInt64);
                case TypeEnum.TypeArray:
                    return typeof (object[]);
                case TypeEnum.TypeStruct:
                    throw new Exception("STRUCT IN XMLRPCVALUE ZOMFG WTF");
            }
            return default(Type);
        }

        #endregion
    }
}