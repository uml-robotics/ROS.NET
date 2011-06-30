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
    public class XmlRpcManager : IDisposable
    {
        public string uri;
        public int port;
        public bool shutting_down;
        public Thread server_thread;
        XmlRpcServer server;
        public bool unbind_requested;
        List<CachedXmlRpcClient> clients = new List<CachedXmlRpcClient>();
        object clients_mutex = new object();
        List<AsyncXmlRpcConnection> added_connections = new List<AsyncXmlRpcConnection>();
        List<AsyncXmlRpcConnection> removed_connections = new List<AsyncXmlRpcConnection>();
        object added_connections_mutex = new object();
        object removed_connections_mutex = new object();
        List<AsyncXmlRpcConnection> connections = new List<AsyncXmlRpcConnection>();
        public class FunctionInfo
        {
            public string name;
            public XMLRPCFunc function;
            public XMLRPCCallWrapper wrapper;
        }
        public void serverThreadFunc()
        {
            while (!shutting_down)
            {
                lock (added_connections_mutex)
                {
                    foreach (AsyncXmlRpcConnection con in added_connections)
                    {
                        con.AddToDispatch(server.Dispatch);
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
                    if (con.Check())
                        removeASyncXMLRPCClient(con);
                }

                lock (removed_connections_mutex)
                {
                    foreach (AsyncXmlRpcConnection con in removed_connections)
                    {
                        con.RemoveFromDispatch(server.Dispatch);
                    }
                    removed_connections.Clear();
                }
            }
        }

        object functions_mutex = new object();
        Dictionary<string, FunctionInfo> functions = new System.Collections.Generic.Dictionary<string, FunctionInfo>();
        public bool validateXmlrpcResponse(string method, XmlRpcValue response, out XmlRpcValue payload)
        {
            payload = null;
            if (response.Type != XmlRpcValue.TypeEnum.TypeArray)
                return validateFailed(method, "didn't return an array");
            if (response.Size != 3)
                return validateFailed(method, "didn't return a 3-element array");
            if (response.Get(0).Type != XmlRpcValue.TypeEnum.TypeInt)
                return validateFailed(method, "didn't return an int as the 1st element");
            int status_code = response.Get<int>(0);
            if (response.Get(1).Type != XmlRpcValue.TypeEnum.TypeString)
                return validateFailed(method, "didn't return a string as the 2nd element");
            string status_string = response.Get<string>(1);
            if (status_code != 1)
                return validateFailed(method, "returned an error ({0}): [{1}]", status_code, status_string);
            payload = new XmlRpcValue(response.Get(2));
            return true;
        }
        private bool validateFailed(string method, string errorfmat, params object[] info)
        {
            Console.WriteLine("XML-RPC Call [{0}] {1}", method, string.Format(errorfmat, info));
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
                            client.client.Close();
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
                functions.Add(function_name, new FunctionInfo { name = function_name, function = cb, wrapper = new XMLRPCCallWrapper(function_name, cb, server) });
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
            return new Action<IntPtr>((p)=>
                {
                    XmlRpcValue v = XmlRpcValue.LookUp(p);
                    if (v == null)
                        v = new XmlRpcValue(p);
                    v.Set(0, new XmlRpcValue(code));
                    v.Set(1, new XmlRpcValue(msg));
                    v.Set(2, new XmlRpcValue(response));
                });
        }

        public Action<IntPtr> responseInt(int code, string msg, int response)
        {
            return new Action<IntPtr>((p) =>
                                          {
                                              XmlRpcValue v = XmlRpcValue.LookUp(p);
                                              if (v == null)
                                                  v = new XmlRpcValue(p);
                                              v.Set(0, new XmlRpcValue(code));
                                              v.Set(1, new XmlRpcValue(msg));
                                              v.Set(2, new XmlRpcValue(response));
                                          });
        }

        public Action<IntPtr> responseBool(int code, string msg, bool response)
        {
            return new Action<IntPtr>((p) =>
                                          {
                                              XmlRpcValue v = XmlRpcValue.LookUp(p);
                                              if (v == null)
                                                  v = new XmlRpcValue(p);
                                              v.Set(0, new XmlRpcValue(code));
                                              v.Set(1, new XmlRpcValue(msg));
                                              v.Set(2, new XmlRpcValue(response));
                                          });
        }

        private static XmlRpcManager _instance;
        public static XmlRpcManager Instance()
        {
            if (_instance == null) _instance = new XmlRpcManager();
            return _instance;
        }

        public XmlRpcManager()
        {
            server = new XmlRpcServer();
            getPid = new XMLRPCFunc((parms, result) => responseInt(1, "", (int)System.Diagnostics.Process.GetCurrentProcess().Id)(result));
        }

        XMLRPCFunc getPid;
        
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

            Console.WriteLine("XmlRpc IN THE HIZI ("+uri+" FOR SHIZI");
            server_thread = new Thread(new ThreadStart(serverThreadFunc));
            server_thread.IsBackground = true;
            server_thread.Start();
        }



        internal void shutdown()
        {
            if (shutting_down)
                return;
            shutting_down = true;
            server_thread.Join();
            server.Close();
            foreach (CachedXmlRpcClient c in clients)
            {
                for (int wait_count = 0; c.in_use && wait_count < 10; wait_count++)
                {
                    Thread.Sleep(10);
                }
                c.client.Close();
            }
            clients.Clear();
            lock (functions_mutex)
            {
                functions.Clear();
            }
            foreach (AsyncXmlRpcConnection ass in connections)
                ass.RemoveFromDispatch(server.Dispatch);
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

        public void Dispose()
        {
            shutdown();
        }
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
