#region Using

using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
#endregion

namespace XmlRpc_Wrapper
{
    public abstract class XmlRpcSource : IDisposable
    {
        protected IntPtr __instance;

        public IntPtr instance
        {
            [DebuggerStepThrough]
            get { return __instance; }
            [DebuggerStepThrough]
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

        public virtual void RmRef(ref IntPtr i)
        {
        }

        public virtual void AddRef(IntPtr i)
        {
        }

        public void SegFault()
        {
            if (__instance == IntPtr.Zero)
            {
                throw new Exception("BOOM");
            }
        }

        public int FD
        {
            get { return getfd(instance); }
            set { setfd(instance, value); }
        }

        public bool KeepOpen
        {
            get { return getkeepopen(instance); }
            set { setkeepopen(instance, value); }
        }

        #region IDisposable Members

        public void Dispose()
        {
            Close();
        }

        #endregion

        #region P/Invoke

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcSource_Close", CallingConvention = CallingConvention.Cdecl)]
        private static extern void close(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcSource_GetFd", CallingConvention = CallingConvention.Cdecl)]
        private static extern int getfd(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcSource_SetFd", CallingConvention = CallingConvention.Cdecl)]
        private static extern void setfd(IntPtr target, int fd);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcSource_GetKeepOpen", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool getkeepopen(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcSource_SetKeepOpen", CallingConvention = CallingConvention.Cdecl)]
        private static extern void setkeepopen(IntPtr target, bool keepopen);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcSource_HandleEvent", CallingConvention = CallingConvention.Cdecl)]
        private static extern UInt16 handleevent(IntPtr target, UInt16 eventType);

        #endregion

        internal virtual void Close()
        {
            close(instance);
        }

        internal virtual UInt16 HandleEvent(UInt16 eventType)
        {
            return handleevent(instance, eventType);
        }
    }
}