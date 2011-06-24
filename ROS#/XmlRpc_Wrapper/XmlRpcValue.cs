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
        private static Dictionary<IntPtr, XmlRpcValue> ValueRegistry;

        private IntPtr __instance;

        public override string ToString()
        {
            if (instance == IntPtr.Zero)
                return "this XmlRpcValue == (NULL)";
            return "this XmlRpcValue is a " + Type+"\n\tIt enjoys long walks on the beach.\n\tIt is "+(Valid?"Valid":"Invalid")+"\n\tIts size is "+Size;
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

        #endregion

        public XmlRpcValue()
        {
            instance = create();
        }

        public XmlRpcValue(bool value)
        {
            instance = create(value);
        }

        public XmlRpcValue(int value)
        {
            instance = create(value);
        }

        public XmlRpcValue(double value)
        {
            instance = create(value);
        }

        public XmlRpcValue(string value)
        {
            instance = create(value);
        }

        public XmlRpcValue(IntPtr value, int nBytes)
        {
            instance = create(value, nBytes);
        }

        public XmlRpcValue(string xml, int offset)
        {
            instance = create(xml, offset);
        }

        public XmlRpcValue(XmlRpcValue value) : this(value.instance)
        {
        }

        internal XmlRpcValue(IntPtr existingptr)
        {
            instance = existingptr;
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
        private static extern IntPtr create([In] [MarshalAs(UnmanagedType.LPWStr)] string value);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Create6", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr create(IntPtr value, int nBytes);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Create7", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr create([In] [MarshalAs(UnmanagedType.LPWStr)] string xml, int offset);

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
        private static extern bool hasmember(IntPtr target, [In] [MarshalAs(UnmanagedType.LPWStr)] string name);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Set1", CallingConvention = CallingConvention.Cdecl)]
        private static extern void set(IntPtr target, int key, [In] [MarshalAs(UnmanagedType.LPWStr)] string value);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Set2", CallingConvention = CallingConvention.Cdecl)]
        private static extern void set(IntPtr target, [In] [MarshalAs(UnmanagedType.LPWStr)] string key, [In] [MarshalAs(UnmanagedType.LPWStr)] string value);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Set3", CallingConvention = CallingConvention.Cdecl)]
        private static extern void set(IntPtr target, int key, IntPtr value);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Set4", CallingConvention = CallingConvention.Cdecl)]
        private static extern void set(IntPtr target, [In] [MarshalAs(UnmanagedType.LPWStr)] string key, IntPtr value);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Get1", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr get(IntPtr target, int key);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Get2", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr get(IntPtr target, [In] [MarshalAs(UnmanagedType.LPWStr)] string key);

        #endregion

        internal IntPtr instance
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

        public static XmlRpcValue Create(IntPtr existingvalue)
        {
            if (ValueRegistry != null && ValueRegistry.ContainsKey(existingvalue))
                return ValueRegistry[existingvalue];
            Console.WriteLine("Bitch, you're tripping!");
            return new XmlRpcValue();
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

        public XmlRpcValue Get(int key)
        {
            SegFault();
            return new XmlRpcValue(get(instance, key));
        }

        public XmlRpcValue Get(string key)
        {
            SegFault();
            return new XmlRpcValue(get(instance, key));
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
    }
}