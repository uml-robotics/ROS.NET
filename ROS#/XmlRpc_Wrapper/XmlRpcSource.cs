using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace XmlRpc_Wrapper
{
    public abstract class XmlRpcSource : IDisposable
    {
        internal IntPtr instance;

        internal virtual void Close()
        {
            close(instance);
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

        internal virtual UInt16 HandleEvent(UInt16 eventType)
        {
            return handleevent(instance, eventType);
        }

        public void Dispose()
        {
            Close();
        }

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
    }
}
