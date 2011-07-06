using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;

namespace XmlRpc_Wrapper
{
    public class XMLRPCCallWrapper : IDisposable
    {
        private static Dictionary<IntPtr, XMLRPCCallWrapper> _instances = new Dictionary<IntPtr, XMLRPCCallWrapper>();
        public static XMLRPCCallWrapper LookUp(IntPtr ptr)
        {
            if (!_instances.ContainsKey(ptr)) return null;
            return _instances[ptr];
        }
        public string name;
        public XmlRpcServer server;
        private XMLRPCFunc _FUNC;
        public XMLRPCFunc FUNC
        {
            get { return _FUNC; }
            set { SetFunc((_FUNC = value)); }
        }
        public IntPtr instance;

        public XMLRPCCallWrapper(string function_name, XMLRPCFunc func, XmlRpcServer server)
        {
            name = function_name;
            this.server = server;
            instance = create(function_name, server.instance);
            SegFault();
            if (!_instances.ContainsKey(instance))
                _instances.Add(instance, this);
            else
                throw new Exception("DUPLICATE ADDRESS ZOMG!");
            FUNC = func;
        }

        public void SetFunc(XMLRPCFunc func)
        {
            SegFault();
            setfunc(instance, func);
        }

        public void Execute(XmlRpcValue parms, out XmlRpcValue reseseses)
        {
            SegFault();
            reseseses = new XmlRpcValue();
            execute(instance, parms.instance, reseseses.instance);
        }

        public void SegFault()
        {
            if (instance == IntPtr.Zero)
                throw new Exception("This isn't really a segfault, but your pointer is invalid, so it would have been!");
        }

        #region P/Invoke
        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcServerMethod_Create", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr create([In] [MarshalAs(UnmanagedType.LPWStr)] string name, IntPtr server);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcServerMethod_SetFunc", CallingConvention = CallingConvention.Cdecl)]
        private static extern void setfunc(IntPtr target, [MarshalAs(UnmanagedType.FunctionPtr)] XMLRPCFunc cb);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcServerMethod_Execute", CallingConvention = CallingConvention.Cdecl)]
        private static extern void execute(IntPtr target, IntPtr parms, IntPtr res);
        #endregion

        public void Dispose()
        {
            if (_instances.ContainsKey(instance))
                _instances.Remove(instance);
            FUNC = null;
        }
    }

        
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void XMLRPCFunc([In][Out] IntPtr addrofparams, [In][Out] IntPtr addrofresult);
}
