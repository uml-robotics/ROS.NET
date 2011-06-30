using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace XmlRpc_Wrapper
{
    public class XmlRpcServer : XmlRpcSource, IDisposable
    {
        public IntPtr instance;
        private static Dictionary<IntPtr, XmlRpcServer> _instances = new Dictionary<IntPtr, XmlRpcServer>();
        public static XmlRpcServer LookUp(IntPtr ptr)
        {
            if (!_instances.ContainsKey(ptr)) return null;
            return _instances[ptr];
        }

        public void AddMethod(XMLRPCCallWrapper method)
        {
            SegFault();
            addmethod(instance, method.instance);
        }

        public void RemoveMethod(XMLRPCCallWrapper method)
        {
            SegFault();
            removemethod(instance, method.instance);
        }

        public void RemoveMethod(string name)
        {
            SegFault();
            removemethodbyname(instance, name);
        }

        public void Work(double msTime)
        {
            SegFault();
            work(instance, msTime);
        }

        public void Exit()
        {
            SegFault();
            exit(instance);
        }

        public void Shutdown()
        {
            SegFault();
            shutdown(instance);
        }

        public int Port
        {
            get { SegFault(); return getport(instance); }
        }

        public XmlRpcDispatch Dispatch
        {
            get 
            {
                SegFault();
                IntPtr ret = getdispatch(instance);
                if (ret == IntPtr.Zero)
                    return null;
                return XmlRpcDispatch.LookUp(ret);
            }
        }

        public XMLRPCCallWrapper FindMethod(string name)
        {
            SegFault();
            IntPtr ret = findmethod(instance, name);
            if (ret == IntPtr.Zero) return null;
            return XMLRPCCallWrapper.LookUp(ret);
        }

        public bool BindAndListen(int port, int backlog = 5)
        {
            SegFault();
            return bindandlisten(instance, port, backlog);
        }

        public XmlRpcServer()
        {
            instance = create();
            SegFault();
            if (_instances.ContainsKey(instance))
                throw new Exception("Instance already in dictionary.... FAIL!");
            else
                _instances.Add(instance, this);
        }


        public void SegFault()
        {
            if (instance == IntPtr.Zero)
                throw new Exception("This isn't really a segfault, but your pointer is invalid, so it would have been!");
        }

        public new void Close()
        {
            base.Close();
            if (_instances.ContainsKey(instance))
                _instances.Remove(instance);
        }

        public new void Dispose()
        {
            Close();
        }

        #region P/Invoke
        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcServer_Create", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr create();

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcServer_AddMethod", CallingConvention = CallingConvention.Cdecl)]
        private static extern void addmethod(IntPtr target, IntPtr method);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcServer_RemoveMethod", CallingConvention = CallingConvention.Cdecl)]
        private static extern void removemethod(IntPtr target, IntPtr method);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcServer_RemoveMethodByName", CallingConvention = CallingConvention.Cdecl)]
        private static extern void removemethodbyname(IntPtr target, [In] [MarshalAs(UnmanagedType.LPWStr)] string method);
        
        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcServer_FindMethod", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr findmethod(IntPtr target, [In] [MarshalAs(UnmanagedType.LPWStr)] string name);
        
        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcServer_BindAndListen", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool bindandlisten(IntPtr target, int port, int backlog);
        
        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcServer_Work", CallingConvention = CallingConvention.Cdecl)]
        private static extern void work(IntPtr target, double msTime);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcServer_Exit", CallingConvention = CallingConvention.Cdecl)]
        private static extern void exit(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcServer_Shutdown", CallingConvention = CallingConvention.Cdecl)]
        private static extern void shutdown(IntPtr target);
        
        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcServer_GetPort", CallingConvention = CallingConvention.Cdecl)]
        private static extern int getport(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcServer_GetDispatch", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr getdispatch(IntPtr target);
        #endregion
    }
}
