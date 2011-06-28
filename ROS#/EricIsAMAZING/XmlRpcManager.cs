using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using XmlRpc_Wrapper;
using m=Messages;
using gm=Messages.geometry_msgs;
using nm=Messages.nav_msgs;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
namespace EricIsAMAZING
{
    public class XmlRpcManager
    {
        public string uri;
        public int port;
        public bool shutting_down;
        public Thread serverThreadFunc;
        XmlRpcServer server;
        List<CachedXmlRpcClient> clients = new List<CachedXmlRpcClient>();
        object clients_mutex = new object();
        List<ASyncXMLRPCConnection> added_connections;
        object added_connections_mutex = new object();
        List<ASyncXMLRPCConnection> removed_connections;
        object removed_connections_mutex = new object();
        public class FunctionInfo
        {
            public string name;
            public XMLRPCFunc function;
            public XMLRPCCallWrapper wrapper;
        }
        object functions_mutex = new object();
        Dictionary<string, FunctionInfo> functions = new System.Collections.Generic.Dictionary<string, FunctionInfo>();
        public bool validateXmlrpcResponse(string method, XmlRpcValue response, XmlRpcValue payload)
        {
            throw new NotImplementedException();

        }

        public XmlRpcClient getXMLRPCClient(string host, int port, string uri)
        {

            throw new NotImplementedException();
        }

        public void releaseXMLRPCClient(XmlRpcClient client)
        {

        }

        public void addAsyncConnection(ASyncXMLRPCConnection conn)
        {

        }

        public void removeASyncXMLRPCClient(ASyncXMLRPCConnection conn)
        {

        }

        public bool bind(string function_name, XMLRPCFunc cb)
        {
            throw new NotImplementedException();
        }

        public void unbind(string function_name)
        {

        }



        public XmlRpcValue responseStr(int code, string msg, string response)
        {
            XmlRpcValue v = new XmlRpcValue();
            v.Set(0, new XmlRpcValue(code));
            v.Set(1, msg);
            v.Set(2, response);
            return v;
        }

        public XmlRpcValue responseInt(int code, string msg, int response)
        {
            XmlRpcValue v = new XmlRpcValue();
            v.Set(0, new XmlRpcValue(code));
            v.Set(1, msg);
            v.Set(2, new XmlRpcValue(response));
            return v;
        }

        public XmlRpcValue responseBool(int code, string msg, bool response)
        {
            XmlRpcValue v = new XmlRpcValue();
            v.Set(0, new XmlRpcValue(code));
            v.Set(1, msg);
            v.Set(2, new XmlRpcValue(response));
            return v;
        }

        private static XmlRpcManager _instance;
        public static XmlRpcManager Instance()
        {
            if (_instance == null) _instance = new XmlRpcManager();
            return _instance;
        }

        public void Start()
        {
            Console.WriteLine("XmlRpc IN THE HIZI FOR SHIZI");
        }

        internal void shutdown()
        {
            throw new NotImplementedException();
        }
    }

    public abstract class ASyncXMLRPCConnection
    {
        public virtual void addToDispatch(ref XmlRpcDispatch disp)
        {

        }

        public virtual void removeFromDispatch(ref XmlRpcDispatch disp)
        {

        }

        public virtual bool check()
        {
            throw new NotImplementedException();
        }
    }

    public class CachedXmlRpcClient
    {
        public XmlRpcClient client;
        bool in_use;
        DateTime last_use_time;
        public CachedXmlRpcClient(XmlRpcClient c)
        {
            client = c;
        }
    }

    public class XMLRPCCallWrapper
    {
        internal IntPtr instance;

        public XMLRPCCallWrapper(string function_name, XMLRPCFunc cb, XmlRpcServer s)
        {
            instance = create(function_name, s.instance);
            name = function_name;
            func = cb;
        }

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XMLRPCCallWrapper_Create", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr create([In] [MarshalAs(UnmanagedType.LPWStr)] string function_name, IntPtr server);

        private string name;
        private XMLRPCFunc func;

        public void execute(XmlRpcValue Params, XmlRpcValue result)
        {


            IntPtr XMLRPCFuncCB = Marshal.GetFunctionPointerForDelegate(func);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            CallDelegateDirectly(XMLRPCFuncCB);
        }

        [DllImport(@"MarshalLib.dll", EntryPoint = "CallDelegate")]
        public static extern void CallDelegateDirectly(IntPtr XMLRPCFuncPtr);
    }

    public delegate void XMLRPCFunc(XmlRpcValue Params, XmlRpcValue result);
}
