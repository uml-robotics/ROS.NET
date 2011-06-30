using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace XmlRpc_Wrapper
{
    public class XmlRpcDispatch : IDisposable
    {
        private static Dictionary<IntPtr, XmlRpcDispatch> _instances = new Dictionary<IntPtr, XmlRpcDispatch>();
        public static XmlRpcDispatch LookUp(IntPtr ptr)
        {
            if (!_instances.ContainsKey(ptr)) return null;
            return _instances[ptr];
        }
        internal IntPtr instance;

        public XmlRpcDispatch()
        {
            Create();
        }

        public void Create()
        {
            instance = create();
            if (instance != IntPtr.Zero && !_instances.ContainsKey(instance))
                _instances.Add(instance, this);
            else
                throw new Exception("Dispatch creation failed... either got null pointer returned, or identical pointer already in instances dictionary.");
        }

        public void Close()
        {
            close(instance);
            if (_instances.ContainsKey(instance))
                _instances.Remove(instance);
        }

        public void Dispose()
        {
            Close();
        }

        public void AddSource(XmlRpcSource source, int eventMask)
        {
            addsource(instance, source.instance, (uint)eventMask);
        }

        public void RemoveSource(XmlRpcSource source)
        {
            removesource(instance, source.instance);
        }

        public void SetSourceEvents(XmlRpcSource source, int eventMask)
        {
            setsourceevents(instance, source.instance, (uint)eventMask);
        }

        public void Work(double msTime)
        {
            work(instance, msTime);
        }

        public void Exit()
        {
            exit(instance);
        }

        public void Clear()
        {
            clear(instance);
        }

        public enum EventType
        {
            ReadableEvent=1,
            WritableEvent=2,
            Exception=4
        }

        #region P/Invoke
        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcDispatch_Create", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr create();

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcDispatch_Close", CallingConvention = CallingConvention.Cdecl)]
        private static extern void close(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcDispatch_AddSource", CallingConvention = CallingConvention.Cdecl)]
        private static extern void addsource(IntPtr target, IntPtr source, uint eventMask);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcDispatch_RemoveSource", CallingConvention = CallingConvention.Cdecl)]
        private static extern void removesource(IntPtr target, IntPtr source);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcDispatch_SetSourceEvents", CallingConvention = CallingConvention.Cdecl)]
        private static extern void setsourceevents(IntPtr target, IntPtr source, uint eventMask);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcDispatch_Work", CallingConvention = CallingConvention.Cdecl)]
        private static extern void work(IntPtr target, double msTime);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcDispatch_Exit", CallingConvention = CallingConvention.Cdecl)]
        private static extern void exit(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcDispatch_Clear", CallingConvention = CallingConvention.Cdecl)]
        private static extern void clear(IntPtr target);
        #endregion
    }
}
