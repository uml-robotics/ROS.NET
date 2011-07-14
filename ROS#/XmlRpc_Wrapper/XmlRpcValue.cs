#region USINGZ
//#define REFDEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

#endregion

namespace XmlRpc_Wrapper
{
    //TODO: OPERATOR GARBAGE?
    public class XmlRpcValue : IDisposable
    {
        #region Reference Tracking + unmanaged pointer management

        public XmlRpcValue this[int key] 
        {
            get { return Get(key); }
            set { Set(key, value); }
        }

        public XmlRpcValue this[string key]
        {
            get { return Get(key); }
            set { Set(key, value); }
        }

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
                        clear(ptr);
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
        private static extern IntPtr create(IntPtr rhs);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Clear", CallingConvention = CallingConvention.Cdecl)]
        private static extern void clear(IntPtr Target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Valid", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool valid(IntPtr target);
        
        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_SetType", CallingConvention = CallingConvention.Cdecl)]
        private static extern int settype(IntPtr target, int type);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Type", CallingConvention = CallingConvention.Cdecl)]
        private static extern int gettype(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Size", CallingConvention = CallingConvention.Cdecl)]
        private static extern int getsize(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_SetSize", CallingConvention = CallingConvention.Cdecl)]
        private static extern void setsize(IntPtr target, int size);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_HasMember", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool hasmember(IntPtr target, [In] [Out] [MarshalAs(UnmanagedType.LPStr)] string name);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Set1", CallingConvention = CallingConvention.Cdecl)]
        private static extern void set(IntPtr target, [In] [Out] [MarshalAs(UnmanagedType.LPStr)] string value);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Set3", CallingConvention = CallingConvention.Cdecl)]
        private static extern void set(IntPtr target, IntPtr value);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Set5", CallingConvention = CallingConvention.Cdecl)]
        private static extern void set(IntPtr target, int value);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Set7", CallingConvention = CallingConvention.Cdecl)]
        private static extern void set(IntPtr target, bool value);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Set9", CallingConvention = CallingConvention.Cdecl)]
        private static extern void set(IntPtr target, double value);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Get1", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr get(IntPtr target, int key);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Get2", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr get(IntPtr target, [In] [Out] [MarshalAs(UnmanagedType.LPStr)] string key);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_GetInt0", CallingConvention = CallingConvention.Cdecl)]
        private static extern int getint(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_GetString0", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr getstring(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_GetBool0", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool getbool(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_GetDouble0", CallingConvention = CallingConvention.Cdecl)]
        private static extern double getdouble(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Dump", CallingConvention = CallingConvention.Cdecl)]
        private static extern void dump(IntPtr target);
        #endregion

        public TypeEnum Type
        {
            [DebuggerStepThrough]
            get
            {
                if (!Initialized)
                    return TypeEnum.TypeInvalid;
                int balls = gettype(instance);
                if (balls < 0 || balls >= ValueTypeHelper._typearray.Length)
                {
                    throw new Exception("YOU THOUGHT THIS WAS GOING TO BE WATER BUT IT WASN'T... ROCK AND ROOLLLLL!!!!!!");
                    //return TypeEnum.TypeInvalid;
                }
                return ValueTypeHelper._typearray[balls];
            }
            [DebuggerStepThrough]
            set
            {
                SegFault();
                settype(instance, (int)value);
            }
        }

        public bool Valid
        {
            [DebuggerStepThrough]
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
            [DebuggerStepThrough]
            get
            {
                SegFault();
                if (!Valid || Type == TypeEnum.TypeInvalid || Type == TypeEnum.TypeIDFK)
                {
                    //Clear();
                    return 0;
                }
                if (Type != TypeEnum.TypeString && Type != TypeEnum.TypeStruct && Type != TypeEnum.TypeArray)
                    return 0;
                return getsize(instance);
            }
            [DebuggerStepThrough]
            set
            {
                SegFault();
                setsize(instance, value);
            }
        }

        [DebuggerStepThrough]
        public void Set<T>(T t)
        {
            if ("" is T)
            {
                set(instance, (string)(object)t);
            }
            else if ((int)0 is T)
            {
                set(instance, (int)(object)t);
            }
            else if (this is T)
            {
                set(instance, ((XmlRpcValue)(object)t).instance);
            }
            else if (true is T)
            {
                set(instance, (bool)(object)t);
            }
            else if (0d is T)
            {
                set(instance, (double)(object)t);
            }
        }

        [DebuggerStepThrough]
        public void Set<T>(int key, T t)
        {
            Get(key).Set(t);
        }

        [DebuggerStepThrough]
        public void Set<T>(string key, T t)
        {
            Get(key).Set(t);
        }

        [DebuggerStepThrough]
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

        [DebuggerStepThrough]
        public T Get<T>(int key)
        {
            return Get(key).Get<T>();
        }

        [DebuggerStepThrough]
        public T Get<T>(string key)
        {
            return Get(key).Get<T>();
        }

        [DebuggerStepThrough]
        public XmlRpcValue Get(int key)
        {
            IntPtr nested = get(instance, key);
            return LookUp(nested);
        }

        [DebuggerStepThrough]
        public XmlRpcValue Get(string key)
        {
            IntPtr nested = get(instance, key);
            return LookUp(nested);
        }

        [DebuggerStepThrough]
        public int GetInt()
        {
            SegFault();
            return getint(__instance);
        }

        [DebuggerStepThrough]
        public string GetString()
        {
            SegFault();
            return Marshal.PtrToStringAnsi(getstring(__instance));
        }

        [DebuggerStepThrough]
        public bool GetBool()
        {
            SegFault();
            return getbool(__instance);
        }

        [DebuggerStepThrough]
        public double GetDouble()
        {
            SegFault();
            return getdouble(__instance);
        }

        [DebuggerStepThrough]
        public override string ToString()
        {
            if (__instance == IntPtr.Zero)
                return "this XmlRpcValue == (NULL)";
            string s = "XmlRpcValue ( " + Type.ToString() + " ) -- size = " + Size;
            return s;
        }

        [DebuggerStepThrough]
        public static XmlRpcValue Create(ref IntPtr existingvalue)
        {
            if (existingvalue == IntPtr.Zero)
            {
                Console.WriteLine("Well, that pointer was invalid, so here's a real one.");
                XmlRpcValue PSYCHE = new XmlRpcValue();
                existingvalue = PSYCHE.__instance;
                return PSYCHE;
            }
            return new XmlRpcValue(existingvalue);
        }

        [DebuggerStepThrough]
        public bool HasMember(string name)
        {
            SegFault();
            return hasmember(instance, name);
        }

        [DebuggerStepThrough]
        public void Dump()
        {
            SegFault();
            dump(__instance);
        }

        [DebuggerStepThrough]
        public void Clear()
        {
            if (Clear(__instance)) Dispose();
        }

        [DebuggerStepThrough]
        public static bool Clear(IntPtr ptr)
        {
            if (ptr != IntPtr.Zero)
            {
                RmRef(ref ptr);
                return (ptr == IntPtr.Zero);
            }
            return true;
        }

        [DebuggerStepThrough]
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
    }

    [DebuggerStepThrough]
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