// File: XmlRpcManager.cs
// Project: ROS_C-Sharp
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 04/28/2015
// Updated: 02/10/2016

#region USINGZ

#if DEBUG
//#define XMLRPC_DEBUG
#endif
//using XmlRpc_Wrapper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using XmlRpc_Wrapper;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace Ros_CSharp
{
    [DebuggerStepThrough]
    public class XmlRpcManager : IDisposable
    {
        private static object singleton_mutex = new object();
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
            XmlRpcUtil.SetLogLevel(
#if !DEBUG
                    XmlRpcUtil.XMLRPC_LOG_LEVEL.ERROR
#else
#if TRACE
                    XmlRpcUtil.XMLRPC_LOG_LEVEL.INFO
#else
                    XmlRpcUtil.XMLRPC_LOG_LEVEL.WARNING
#endif
#endif
);

            server = new XmlRpcServer();
            getPid = (parms, result) => responseInt(1, "", Process.GetCurrentProcess().Id)(result);
        }

        public static XmlRpcManager Instance
        {
#if !TRACE
            [DebuggerStepThrough]
#endif
            get
            {
                if (_instance == null)
                {
                    lock (singleton_mutex)
                    {
                        if (_instance == null)
                            _instance = new XmlRpcManager();
                    }
                }
                return _instance;
            }
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
                    throw new Exception("XmlRpcManager isn't initialized yet!");
                    return;
                }
                lock (added_connections_mutex)
                {
                    foreach (AsyncXmlRpcConnection con in added_connections)
                    {
                        //EDB.WriteLine("Completed ASYNC XmlRpc connection to: " + ((con as PendingConnection) != null ? ((PendingConnection) con).RemoteUri : "SOMEWHERE OVER THE RAINBOW"));
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
                    Thread.Sleep(ROS.WallDuration);
                }

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
                        connections.Remove(con);
                    }
                    removed_connections.Clear();
                }
            }
        }

        public bool validateXmlrpcResponse(string method, XmlRpcValue response, XmlRpcValue payload)
        {
            if (response.Type != XmlRpcValue.ValueType.TypeArray)
                return validateFailed(method, "didn't return an array -- {0}", response);
            if (response.Size != 3)
                return validateFailed(method, "didn't return a 3-element array -- {0}", response);
            if (response[0].Type != XmlRpcValue.ValueType.TypeInt)
                return validateFailed(method, "didn't return an int as the 1st element -- {0}", response);
            int status_code = response[0].Get<int>();
            if (response[1].Type != XmlRpcValue.ValueType.TypeString)
                return validateFailed(method, "didn't return a string as the 2nd element -- {0}", response);
            string status_string = response[1].Get<string>();
            if (status_code != 1)
                return validateFailed(method, "returned an error ({0}): [{1}] -- {2}", status_code, status_string,
                    response);
            switch (response[2].Type)
            {
                case XmlRpcValue.ValueType.TypeArray:
                    {
                        payload.SetArray(0);
                        for (int i = 0; i < response[2].Length; i++)
                        {
                            payload.Set(i, response[2][i]);
                        }
                    }
                    break;
                case XmlRpcValue.ValueType.TypeInt:
                    payload.asInt = response[2].asInt;
                    break;
                case XmlRpcValue.ValueType.TypeDouble:
                    payload.asDouble = response[2].asDouble;
                    break;
                case XmlRpcValue.ValueType.TypeString:
                    payload.asString = response[2].asString;
                    break;
                case XmlRpcValue.ValueType.TypeBoolean:
                    payload.asBool = response[2].asBool;
                    break;
                case XmlRpcValue.ValueType.TypeInvalid:
                    break;
                default:
                    throw new Exception("Unhandled valid xmlrpc payload type: " + response[2].Type);
            }
            return true;
        }

        private bool validateFailed(string method, string errorfmat, params object[] info)
        {
#if XMLRPC_DEBUG
            EDB.WriteLine("XML-RPC Call [{0}] {1} failed validation", method, string.Format(errorfmat, info));
#endif
            return false;
        }

        public CachedXmlRpcClient getXMLRPCClient(string host, int port, string uri)
        {
            CachedXmlRpcClient c = null;
            lock (clients_mutex)
            {
                List<CachedXmlRpcClient> zombies = new List<CachedXmlRpcClient>();
                foreach (CachedXmlRpcClient client in clients)
                {
                    if (!client.in_use)
                    {
                        if (DateTime.Now.Subtract(client.last_use_time).TotalSeconds > 30 || client.dead)
                        {
                            zombies.Add(client);
                        }
                        else if (client.CheckIdentity(host, port, uri))
                        {
                            c = client;
                            break;
                        }
                    }
                }
                foreach (CachedXmlRpcClient C in zombies)
                {
                    clients.Remove(C);
                    C.Dispose();
                }
                if (c == null)
                {
                    c = new CachedXmlRpcClient(host, port, uri);
                    clients.Add(c);
                }
            }
            c.AddRef();
            return c;
        }

        public void releaseXMLRPCClient(CachedXmlRpcClient client)
        {
            client.DelRef();
            client.Dispose();
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
                functions.Add(function_name,
                    new FunctionInfo
                    {
                        name = function_name,
                        function = cb,
                        wrapper = new XmlRpcServerMethod(function_name, cb, server)
                    });
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


        public Action<XmlRpcValue> responseStr(IntPtr target, int code, string msg, string response)
        {
            return (XmlRpcValue v) =>
            {
                v.Set(0, code);
                v.Set(1, msg);
                v.Set(2, response);
            };
        }

        public Action<XmlRpcValue> responseInt(int code, string msg, int response)
        {
            return (XmlRpcValue v) =>
            {
                v.Set(0, code);
                v.Set(1, msg);
                v.Set(2, response);
            };
        }

        public Action<XmlRpcValue> responseBool(int code, string msg, bool response)
        {
            return (XmlRpcValue v) =>
            {
                v.Set(0, code);
                v.Set(1, msg);
                v.Set(2, response);
            };
        }

        /// <summary>
        ///     <para> This function starts the XmlRpcServer used to handle inbound calls on this node </para>
        ///     <para>
        ///         The optional argument is used to force ROS to try to bind to a specific port.
        ///         Doing so should only be done when acting as the RosMaster.
        ///     </para>
        ///     <para> </para>
        ///     <para>
        ///         Jordan, this function used to have the following:
        ///         <list type="bullet">
        ///             <item>
        ///                 <description>A string argument named "host"</description>
        ///             </item>
        ///             <item>
        ///                 <description>that defaulted to "0"</description>
        ///             </item>
        ///             <item>
        ///                 <description>and it was used to determine the port.</description>
        ///             </item>
        ///         </list>
        ///     </para>
        ///     <para>
        ///         ... so now I'm all up in your codes. :-P
        ///     </para>
        ///     <para>
        ///         -Eric
        ///     </para>
        /// </summary>
        /// <param name="p">The specific port number to bind to, if any</param>
        public void Start(int p = 0)
        {
            shutting_down = false;

            bind("getPid", getPid);

            if (p != 0)
            {
                //if port isn't 0, then we better be the master, 
                //      so let's grab this bull by the horns
                uri = ROS.ROS_MASTER_URI;
                port = p;
            }
            //if port is 0, then we need to get our hostname from ROS' network init,
            //   and we don't know our port until we're bound and listening

            bool bound = server.BindAndListen(port);
            if (!bound)
                throw new Exception("RPCServer bind failed");

            if (p == 0)
            {
                //if we weren't called with a port #, then we have to figure out what
                //    our port number is now that we're bound
                port = server.Port;
                if (port == 0)
                    throw new Exception("RPCServer's port is invalid");
                uri = "http://" + network.host + ":" + port + "/";
            }

            //EDB.WriteLine("XmlRpc Server IN THE HIZI (" + uri + ") FOR SHIZI");
            server_thread = new Thread(serverThreadFunc) { IsBackground = true };
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
                while (c.in_use)
                    Thread.Sleep(ROS.WallDuration);
                c.Dispose();
            }
            clients.Clear();
            lock (functions_mutex)
            {
                functions.Clear();
            }
            if (server != null)
            {
                foreach (AsyncXmlRpcConnection ass in connections)
                {
                    ass.removeFromDispatch(server.Dispatch);
                }
            }
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
            public XmlRpcServerMethod wrapper;
        }

        #endregion
    }

    [DebuggerStepThrough]
    public class CachedXmlRpcClient : IDisposable
    {
        private XmlRpcClient client;

        public bool in_use
        {
            get { lock (busyMutex) return refs != 0; }
        }

        public DateTime last_use_time;

        private object busyMutex = new object();
        private object client_lock = new object();
        private volatile int refs;

        internal bool dead
        {
            get { lock(client_lock) return client == null; }
        }

        public CachedXmlRpcClient(string host, int port, string uri)
            : this(new XmlRpcClient(host, port, uri))
        {
        }

        private CachedXmlRpcClient(XmlRpcClient c)
        {
            lock (client_lock)
            {
                client = c;
                client.Disposed += () =>
                                       {
                                           client = null;
                                       };
            }
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            lock (busyMutex)
            	if (refs != 0)
                	EDB.WriteLine("warning: XmlRpcClient disposed with "+refs+" refs held");
            lock (client_lock)
            {
                if (client != null)
                {
                    client.Dispose();
                    client = null;
                }
            }
        }

        internal void AddRef()
        {
            lock (busyMutex)
            {
                refs++;
            }
            last_use_time = DateTime.Now;
        }

        internal void DelRef()
        {
            lock (busyMutex)
            {
                refs--;
            }
        }

        public bool CheckIdentity(string host, int port, string uri)
        {
            lock (client_lock)
            {
                return client != null && port == client.Port && (host == null || client.Host != null && string.Equals(host, client.Host)) && (uri == null || client.Uri != null && string.Equals(uri, client.Uri));
            }
        }

        #region XmlRpcClient passthrough functions and properties

        public bool Execute(string method, XmlRpcValue parameters, XmlRpcValue result)
        {
            lock (client_lock)
                return client.Execute(method, parameters, result);
        }

        public bool ExecuteNonBlock(string method, XmlRpcValue parameters)
        {
            lock (client_lock)
                return client.ExecuteNonBlock(method, parameters);
        }

        public bool ExecuteCheckDone(XmlRpcValue result)
        {
            lock (client_lock)
                return client.ExecuteCheckDone(result);
        }

        public bool IsConnected
        {
            get
            {
                lock (client_lock) return client != null && client.IsConnected;
            }
        }

        #endregion
    }
}