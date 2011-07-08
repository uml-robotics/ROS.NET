#region USINGZ

using System;
using System.Runtime.InteropServices;

#endregion

namespace XmlRpc_Wrapper
{
    public static class WrapperTest
    {
        #region Delegates

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void balls(int val);

        #endregion

        [DllImport("XmlRpcWin32.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int IntegerEcho(int val);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "IntegerEchoFunctionPtr", CallingConvention = CallingConvention.Cdecl)]
        public static extern void IntegerEchoFunctionPtr([MarshalAs(UnmanagedType.FunctionPtr)] balls callback);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "IntegerEchoRepeat", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool IntegerEchoRepeat(int val);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void TellMeHowAwesomeIAm(string s);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "SetStringOutFunc", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetAwesomeFunctionPtr([MarshalAs(UnmanagedType.FunctionPtr)] TellMeHowAwesomeIAm callback);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "StringPassingTest", CallingConvention = CallingConvention.Cdecl)]
        public static extern void StringTest([In] [MarshalAs(UnmanagedType.LPStr)] string str);
    }


    public class XmlRpcClient : XmlRpcSource, IDisposable
    {
        public string HostUri = "";

        public XmlRpcClient(string HostName, int Port, string Uri)
        {
            if (!Create(HostName, Port, Uri))
                Console.WriteLine("Some failure occurred in unmanaged code, and the returned pointer to an XmlRpcClient was null. OWNED!");
        }

        public XmlRpcClient(string HostName, int Port)
            : this(HostName, Port, "/")
        {
        }

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
            if (Create(hn, p, u))
                Console.WriteLine("Successfully Created XmlRpc Client @ http://" + hn + ":" + p + u);
            else
                Console.WriteLine("Some failure occurred in unmanaged code, and the returned pointer to an XmlRpcClient was null. OWNED!");
        }

        #region public get passthroughs

        public bool IsFault
        {
            get { return isfault(instance); }
        }

        public string Host
        {
            get
            {
                string ret = gethost(instance);
                return ret;
            }
        }

        public string Uri
        {
            get { return geturi(instance); }
        }

        public int Port
        {
            get { return getport(instance); }
        }

        public string Request
        {
            get { return getrequest(instance); }
        }

        public string Header
        {
            get { return getheader(instance); }
        }

        public string Response
        {
            get { return getresponse(instance); }
        }

        public int SendAttempts
        {
            get { return getsendattempts(instance); }
        }

        public int BytesWritten
        {
            get { return getbyteswritten(instance); }
        }

        public bool Executing
        {
            get { return getexecuting(instance); }
        }

        public bool EOF
        {
            get { return geteof(instance); }
        }

        public int ContentLength
        {
            get { return getcontentlength(instance); }
        }

        public IntPtr XmlRpcDispatch
        {
            get { return getxmlrpcdispatch(instance); }
        }

        #endregion

        #region public function passthroughs

        public bool Execute(string method, XmlRpcValue parameters, out XmlRpcValue result)
        {
            result = new XmlRpcValue();
            return execute(instance, method, parameters.instance, result.instance);
        }

        public bool ExecuteNonBlock(string method, XmlRpcValue parameters)
        {
            return executenonblock(instance, method, parameters.instance);
        }

        public bool ExecuteCheckDone(XmlRpcValue result)
        {
            return executecheckdone(instance, result.instance);
        }

        public new UInt16 HandleEvent(UInt16 eventType)
        {
            return handleevent(instance, eventType);
        }

        #endregion

        public bool IsNull
        {
            get { return instance == IntPtr.Zero; }
        }

        #region IDisposable Members

        public new void Dispose()
        {
            Console.WriteLine((Close() ? "Successfully disposed Client Instance." : "YOU FUCKING SUCK!"));
        }

        #endregion

        #region P/Invoke

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_Create", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr create
            (
            [In] [MarshalAs(UnmanagedType.LPStr)] string host,
            int port,
            [In] [MarshalAs(UnmanagedType.LPStr)] string uri);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_Close", CallingConvention = CallingConvention.Cdecl)]
        private static extern void close(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_Execute", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool execute
            (IntPtr target,
             [In] [MarshalAs(UnmanagedType.LPStr)] string method,
             IntPtr parameters,
             IntPtr result);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_ExecuteNonBlock", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool executenonblock
            (IntPtr target,
             [In] [MarshalAs(UnmanagedType.LPStr)] string method, IntPtr parameters);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_ExecuteCheckDone", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool executecheckdone(IntPtr target, IntPtr result);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_HandleEvent", CallingConvention = CallingConvention.Cdecl)]
        private static extern UInt16 handleevent(IntPtr target, UInt16 eventType);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_IsFault", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool isfault(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_GetHost", CallingConvention = CallingConvention.Cdecl)]
        private static extern string gethost(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_GetUri", CallingConvention = CallingConvention.Cdecl)]
        private static extern string geturi(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_GetPort", CallingConvention = CallingConvention.Cdecl)]
        private static extern int getport(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_GetRequest", CallingConvention = CallingConvention.Cdecl)]
        private static extern string getrequest(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_GetHeader", CallingConvention = CallingConvention.Cdecl)]
        private static extern string getheader(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_GetResponse", CallingConvention = CallingConvention.Cdecl)]
        private static extern string getresponse(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_GetSendAttempts", CallingConvention = CallingConvention.Cdecl)]
        private static extern int getsendattempts(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_GetBytesWritten", CallingConvention = CallingConvention.Cdecl)]
        private static extern int getbyteswritten(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_GetExecuting", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool getexecuting(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_GetEOF", CallingConvention = CallingConvention.Cdecl)]
        private static extern bool geteof(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_GetContentLength", CallingConvention = CallingConvention.Cdecl)]
        private static extern int getcontentlength(IntPtr Target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_GetXmlRpcDispatch", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr getxmlrpcdispatch(IntPtr target);

        #endregion

        private bool Create(string HostName, int Port, string Uri)
        {
            HostUri = "http://" + HostName + ":" + Port + Uri;
            IntPtr testcreate = create(HostName, Port, Uri);
            if (testcreate != IntPtr.Zero)
            {
                instance = testcreate;
                return true;
            }
            return false;
        }

        public bool Close()
        {
            if (instance != IntPtr.Zero)
            {
                close(instance);
                return true;
            }
            return false;
        }
    }
}