#region USINGZ

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using XmlRpc_Wrapper;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace EricIsAMAZING
{
    public class XmlRpcManager : IDisposable
    {
        private static XmlRpcManager _instance;
        private List<AsyncXmlRpcConnection> added_connections = new List<AsyncXmlRpcConnection>();
        private object added_connections_mutex = new object();
        private List<CachedXmlRpcClient> clients = new List<CachedXmlRpcClient>();
        private object clients_mutex = new object();
        private List<AsyncXmlRpcConnection> connections = new List<AsyncXmlRpcConnection>();
        private Dictionary<string, FunctionInfo> functions = new Dictionary<string, FunctionInfo>();
        private object functions_mutex = new object();
        private XMLRPCFunc getPid;
        public int port;
        private List<AsyncXmlRpcConnection> removed_connections = new List<AsyncXmlRpcConnection>();
        private object removed_connections_mutex = new object();
        private XmlRpcServer server;
        public Thread server_thread;
        public bool shutting_down;
        public bool unbind_requested;
        public string uri = "";

        public XmlRpcManager()
        {
            server = new XmlRpcServer();
            getPid = (parms, result) => responseInt(1, "", Process.GetCurrentProcess().Id)(result);
        }

        #region IDisposable Members

        public void Dispose()
        {
            shutdown();
        }

        #endregion

        public void serverThreadFunc()
        {
            while (!shutting_down)
            {
                if (server.Dispatch == null)
                {
                    Console.WriteLine("FUCKING FUCK STAINS!");
                    return;
                }
                lock (added_connections_mutex)
                {
                    foreach (AsyncXmlRpcConnection con in added_connections)
                    {
                        con.addToDispatch(server.Dispatch);
                        connections.Add(con);
                    }
                    added_connections.Clear();
                }

                lock (functions_mutex)
                {
                    server.Work(0.1);
                }

                while (unbind_requested)
                {
                    Thread.Sleep(10);
                }

                if (shutting_down) return;

                foreach (AsyncXmlRpcConnection con in connections)
                {
                    if (con.check())
                        removeASyncXMLRPCClient(con);
                }

                lock (removed_connections_mutex)
                {
                    foreach (AsyncXmlRpcConnection con in removed_connections)
                    {
                        con.removeFromDispatch(server.Dispatch);
                    }
                    removed_connections.Clear();
                }
            }
        }

        public bool validateXmlrpcResponse(string method, XmlRpcValue response, ref XmlRpcValue payload)
        {
            if (response.Type != TypeEnum.TypeArray)
                return validateFailed(method, "didn't return an array -- {0}", response);
            if (response.Size != 3)
                return validateFailed(method, "didn't return a 3-element array -- {0}", response);
            if (response.Get(0).Type != TypeEnum.TypeInt)
                return validateFailed(method, "didn't return an int as the 1st element -- {0}", response);
            int status_code = response.Get<int>(0);
            if (response.Get(1).Type != TypeEnum.TypeString)
                return validateFailed(method, "didn't return a string as the 2nd element -- {0}", response);
            string status_string = response.Get<string>(1);
            if (status_code != 1)
                return validateFailed(method, "returned an error ({0}): [{1}] -- {2}", status_code, status_string, response);
            payload = new XmlRpcValue(response.Get(2));
            return true;
        }

        private bool validateFailed(string method, string errorfmat, params object[] info)
        {
#if DEBUG
            Console.WriteLine("XML-RPC Call [{0}] {1}", method, string.Format(errorfmat, info));
#endif
            return false;
        }

        public XmlRpcClient getXMLRPCClient(string host, int port, string uri)
        {
            XmlRpcClient c = null;
            lock (clients_mutex)
            {
                List<CachedXmlRpcClient> zombies = new List<CachedXmlRpcClient>();
                foreach (CachedXmlRpcClient client in clients)
                {
                    if (!client.in_use)
                    {
                        if (client.client.Host == host && client.client.Port == port && client.client.Uri == uri)
                        {
                            c = client.client;
                            client.in_use = true;
                            client.last_use_time = DateTime.Now;
                            break;
                        }
                        else if (DateTime.Now.Subtract(client.last_use_time).TotalMilliseconds > 30)
                        {
                            client.client.Shutdown();
                            zombies.Add(client);
                        }
                    }
                }
                clients = clients.Except(zombies).ToList();
            }
            if (c == null)
            {
                c = new XmlRpcClient(host, port, uri);
                clients.Add(new CachedXmlRpcClient(c) {in_use = true, last_use_time = DateTime.Now});
            }
            return c;
        }

        public void releaseXMLRPCClient(XmlRpcClient client)
        {
            lock (clients_mutex)
            {
                foreach (CachedXmlRpcClient c in clients)
                {
                    if (client == c.client)
                    {
                        c.in_use = false;
                        break;
                    }
                }
            }
        }

        public void addAsyncConnection(AsyncXmlRpcConnection conn)
        {
            lock (added_connections_mutex)
                added_connections.Add(conn);
        }

        public void removeASyncXMLRPCClient(AsyncXmlRpcConnection conn)
        {
            lock (removed_connections_mutex)
                removed_connections.Add(conn);
        }

        public bool bind(string function_name, XMLRPCFunc cb)
        {
            lock (functions_mutex)
            {
                if (functions.ContainsKey(function_name))
                    return false;
                functions.Add(function_name, new FunctionInfo {name = function_name, function = cb, wrapper = new XMLRPCCallWrapper(function_name, cb, server)});
            }
            return true;
        }

        public void unbind(string function_name)
        {
            unbind_requested = true;
            lock (functions_mutex)
            {
                functions.Remove(function_name);
            }
            unbind_requested = false;
        }


        public Action<IntPtr> responseStr(IntPtr target, int code, string msg, string response)
        {
            return (p) =>
                       {
                           XmlRpcValue v = XmlRpcValue.LookUp(p);
                           if (!v.Initialized)
                               v = new XmlRpcValue(p);
                           v.Set(0, code);
                           v.Set(1, msg);
                           v.Set(2, response);
                       };
        }

        public Action<IntPtr> responseInt(int code, string msg, int response)
        {
            return (p) =>
                       {
                           XmlRpcValue v = XmlRpcValue.LookUp(p);
                           if (!v.Initialized)
                               v = new XmlRpcValue(p);
                           v.Set(0, code);
                           v.Set(1, msg);
                           v.Set(2, response);
                       };
        }

        public Action<IntPtr> responseBool(int code, string msg, bool response)
        {
            return (p) =>
                       {
                           XmlRpcValue v = XmlRpcValue.LookUp(p);
                           if (!v.Initialized)
                               v = new XmlRpcValue(p);
                           v.Set(0, code);
                           v.Set(1, msg);
                           v.Set(2, response);
                       };
        }

        public static XmlRpcManager Instance
        {
            get
            {
                if (_instance == null) _instance = new XmlRpcManager();
                return _instance;
            }
        }

        public void Start()
        {
            shutting_down = false;
            port = 0;
            bind("getPid", getPid);

            bool bound = server.BindAndListen(0);
            if (!bound)
                throw new Exception("RPCServer bind failed");
            port = server.Port;
            if (port == 0)
                throw new Exception("RPCServer's port is invalid");
            uri = "http://" + network.host + ":" + port + "/";

            Console.WriteLine("XmlRpc IN THE HIZI (" + uri + " FOR SHIZI");
            server_thread = new Thread(serverThreadFunc);
            server_thread.IsBackground = true;
            server_thread.Start();
        }


        internal void shutdown()
        {
            if (shutting_down)
                return;
            shutting_down = true;
            server_thread.Join();
            server.Shutdown();
            foreach (CachedXmlRpcClient c in clients)
            {
                for (int wait_count = 0; c.in_use && wait_count < 10; wait_count++)
                {
                    Thread.Sleep(10);
                }
                c.client.Shutdown();
            }
            clients.Clear();
            lock (functions_mutex)
            {
                functions.Clear();
            }
            foreach (AsyncXmlRpcConnection ass in connections)
                ass.removeFromDispatch(server.Dispatch);
            connections.Clear();
            lock (added_connections_mutex)
            {
                added_connections.Clear();
            }
            lock (removed_connections_mutex)
            {
                removed_connections.Clear();
            }
        }

        #region Nested type: FunctionInfo

        public class FunctionInfo
        {
            public XMLRPCFunc function;
            public string name = "";
            public XMLRPCCallWrapper wrapper;
        }

        #endregion
    }

    public class CachedXmlRpcClient
    {
        public XmlRpcClient client;
        public bool in_use;
        public DateTime last_use_time;

        public CachedXmlRpcClient(XmlRpcClient c)
        {
            client = c;
        }
    }
}