#region Using

//#define REFDEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

#endregion

namespace XmlRpc_Wrapper
{
    [DebuggerStepThrough]
    public class XmlRpcClient : XmlRpcSource, IDisposable
    {
        #region Reference Tracking + unmanaged pointer management
                
        public void Dispose()
        {
            Shutdown();
        }

        ~XmlRpcClient()
        {
            Shutdown();
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
                }
                Thread.Sleep(500);
            }
        }
#endif

        [DebuggerStepThrough]
        public static XmlRpcDispatch LookUp(IntPtr ptr)
        {
            if (ptr != IntPtr.Zero)
            {
                AddRef(ptr);
                return new XmlRpcDispatch(ptr);
            }
            return null;
        }


        [DebuggerStepThrough]
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

        [DebuggerStepThrough]
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
                        Console.WriteLine("KILLING " + ptr + " BECAUSE IT'S NOT VERY NICE!");
#endif
                        _refs.Remove(ptr);
                        close(ptr);
                        ptr = IntPtr.Zero;
                    }
                    return;
                }
            }
        }
        
        public void Shutdown()
        {
            if (Shutdown(__instance)) Dispose();
        }

        public static bool Shutdown(IntPtr ptr)
        {
            if (ptr != IntPtr.Zero)
            {
                RmRef(ref ptr);
                return (ptr == IntPtr.Zero);
            }
            return true;
        }

        #endregion

        public string HostUri = "";

        [DebuggerStepThrough]
        public XmlRpcClient(string HostName, int Port, string Uri)
        {
            instance = create(HostName, Port, Uri);
        }

        [DebuggerStepThrough]
        public XmlRpcClient(string HostName, int Port)
            : this(HostName, Port, "/")
        {
        }

        [DebuggerStepThrough]
        public XmlRpcClient(string WHOLESHEBANG)
        {
            if (!WHOLESHEBANG.Contains("://")) throw new Exception("INVALID ARGUMENT DIE IN A FIRE!");
            WHOLESHEBANG = WHOLESHEBANG.Remove(0, WHOLESHEBANG.IndexOf("://") + 3);
            WHOLESHEBANG.Trim('/');
            string[] chunks = WHOLESHEBANG.Split(':');
            string hn = chunks[0];
            string[] chunks2 = chunks[1].Split('/');
            int p = int.Parse(chunks2[0]);
            string u = "/";
            if (chunks2.Length > 1 && chunks2[1].Length != 0)
                u = chunks2[1];
            instance = create(hn, p, u);
        }

        #region public get passthroughs

        public bool IsConnected
        {
            [DebuggerStepThrough]
            get { return isconnected(instance) != 0; }
        }

        public string Host
        {
            [DebuggerStepThrough]
            get { return Marshal.PtrToStringAnsi(gethost(instance)); }
        }

        public string Uri
        {
            [DebuggerStepThrough]
            get { return Marshal.PtrToStringAnsi(geturi(instance)); }            
        }

        public int Port
        {
            [DebuggerStepThrough]
            get { return getport(instance); }
        }

        public string Request
        {
            [DebuggerStepThrough]
            get { return Marshal.PtrToStringAnsi(getrequest(instance)); }
        }

        public string Header
        {
            [DebuggerStepThrough]
            get { return Marshal.PtrToStringAnsi(getheader(instance)); }
        }

        public string Response
        {
            [DebuggerStepThrough]
            get { return Marshal.PtrToStringAnsi(getresponse(instance)); }
        }

        public int SendAttempts
        {
            [DebuggerStepThrough]
            get { return getsendattempts(instance); }
        }

        public int BytesWritten
        {
            [DebuggerStepThrough]
            get { return getbyteswritten(instance); }
        }

        public bool Executing
        {
            [DebuggerStepThrough]
            get { return getexecuting(instance) != 0; }
        }

        public bool EOF
        {
            [DebuggerStepThrough]
            get { return geteof(instance) != 0; }
        }

        public int ContentLength
        {
            [DebuggerStepThrough]
            get { return getcontentlength(instance); }
        }

        public IntPtr XmlRpcDispatch
        {
            [DebuggerStepThrough]
            get { return getxmlrpcdispatch(instance); }
        }

        #endregion

        #region public function passthroughs
        public bool CheckIdentity(string host, int port, string uri)
        {
            return checkident(instance, host, port, uri) != 0;
        }

        public bool Execute(string method, XmlRpcValue parameters, XmlRpcValue result)
        {
            bool r = execute(instance, method, parameters.instance, result.instance) != 0;
            return r;
        }

        public bool ExecuteNonBlock(string method, XmlRpcValue parameters)
        {
            return executenonblock(instance, method, parameters.instance) != 0;
        }

        public bool ExecuteCheckDone(XmlRpcValue result)
        {
            return executecheckdone(instance, result.instance) != 0;
        }

        public UInt16 HandleEvent(UInt16 eventType)
        {
            return handleevent(instance, eventType);
        }

        #endregion

        #region P/Invoke

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_Create", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr create
            (
            [In] [Out] [MarshalAs(UnmanagedType.LPStr)] string host,
            int port,
            [In] [Out] [MarshalAs(UnmanagedType.LPStr)] string uri);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_Close", CallingConvention = CallingConvention.Cdecl)]
        private static extern void close(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_Execute", CallingConvention = CallingConvention.Cdecl)]
        private static extern byte execute
            (IntPtr target,
             [In] [Out] [MarshalAs(UnmanagedType.LPStr)] string method,
             IntPtr parameters,
             IntPtr result);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_ExecuteNonBlock",
            CallingConvention = CallingConvention.Cdecl)]
        private static extern byte executenonblock
            (IntPtr target,
             [In] [Out] [MarshalAs(UnmanagedType.LPStr)] string method, IntPtr parameters);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_ExecuteCheckDone",
            CallingConvention = CallingConvention.Cdecl)]
        private static extern byte executecheckdone([In] [Out] IntPtr target, [In] [Out] IntPtr result);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_HandleEvent",
            CallingConvention = CallingConvention.Cdecl)]
        private static extern UInt16 handleevent(IntPtr target, UInt16 eventType);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_IsFault", CallingConvention = CallingConvention.Cdecl)]
        private static extern byte isconnected(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_GetHost", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr gethost(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_GetUri", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr geturi(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_GetPort", CallingConvention = CallingConvention.Cdecl)]
        private static extern int getport(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_GetRequest", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr getrequest(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_GetHeader", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr getheader(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_GetResponse", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr getresponse(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_GetSendAttempts",
            CallingConvention = CallingConvention.Cdecl)]
        private static extern int getsendattempts(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_GetBytesWritten",
            CallingConvention = CallingConvention.Cdecl)]
        private static extern int getbyteswritten(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_GetExecuting",
            CallingConvention = CallingConvention.Cdecl)]
        private static extern byte getexecuting(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_GetEOF", CallingConvention = CallingConvention.Cdecl)]
        private static extern byte geteof(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_CheckIdent", CallingConvention = CallingConvention.Cdecl)]
        private static extern byte checkident(IntPtr target, [In] [Out] [MarshalAs(UnmanagedType.LPStr)] string host, int port, [In] [Out] [MarshalAs(UnmanagedType.LPStr)] string uri);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_GetContentLength",
            CallingConvention = CallingConvention.Cdecl)]
        private static extern int getcontentlength(IntPtr Target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_GetXmlRpcDispatch",
            CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr getxmlrpcdispatch(IntPtr target);

        #endregion
    }
}