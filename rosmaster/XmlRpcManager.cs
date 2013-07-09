#region Using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using XmlRpc_Wrapper;
using System.Runtime.InteropServices;

#endregion

namespace rosmaster
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

        Master_API.ROSMasterHandler handler;
        public XmlRpcManager(Master_API.ROSMasterHandler _handler = null)
        {
            handler = _handler;
            server = new XmlRpcServer();
            getPid = (parms, result) => responseInt(1, "", Process.GetCurrentProcess().Id)(result);
        }

        public static XmlRpcManager Instance
        {
            [DebuggerStepThrough]
            get
            {
                if (_instance == null) _instance = new XmlRpcManager();
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
                    return;
                }
                lock (added_connections_mutex)
                {
                    foreach (AsyncXmlRpcConnection con in added_connections)
                    {
                        Console.WriteLine("Completed ASYNC XmlRpc connection to: " + (con as PendingConnection).RemoteUri);
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
                        connections.Remove(con);
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
            if (response[0].Type != TypeEnum.TypeInt)
                return validateFailed(method, "didn't return an int as the 1st element -- {0}", response);
            int status_code = response[0].Get<int>();
            if (response[1].Type != TypeEnum.TypeString)
                return validateFailed(method, "didn't return a string as the 2nd element -- {0}", response);
            string status_string = response[1].Get<string>();
            if (status_code != 1)
                return validateFailed(method, "returned an error ({0}): [{1}] -- {2}", status_code, status_string,
                                      response);
            payload = response[2];
            return true;
        }

        private bool validateFailed(string method, string errorfmat, params object[] info)
        {
#if DEBUG
            //EDB.WriteLine("XML-RPC Call [{0}] {1}", method, string.Format(errorfmat, info));
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
                        else if (DateTime.Now.Subtract(client.last_use_time).TotalSeconds > 30 ||
                                 !client.client.IsConnected)
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
                functions.Add(function_name,
                              new FunctionInfo
                                  {
                                      name = function_name,
                                      function = cb,
                                      wrapper = new XMLRPCCallWrapper(function_name, cb, server)
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


        public Action<IntPtr> responseStr(IntPtr target, int code, string msg, string response)
        {
            return (p) =>
                       {
                           XmlRpcValue v = XmlRpcValue.LookUp(p);
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
                           v.Set(0, code);
                           v.Set(1, msg);
                           v.Set(2, response);
                       };
        }

        public void Start(String hostname)
        {
            int what = int.Parse( hostname.Split(':')[2].TrimEnd('/'));
            shutting_down = false;
            port = 0;

            bind("getPublications", getPublications);
            bind("getSubscriptions", getSubscriptions);

            bind("publisherUpdate", pubUpdate);

            bind("requestTopic", requestTopic);
            bind("getSystemState", getSystemState);


            bind("getPublishedTopics",getPublishedTopics);

            bind("registerPublisher", registerPublisher);
            bind("unregisterPublisher", unregisterPublisher);

            bind("registerSubscriber",registerSubscriber);
            bind("unregisterSubscriber",unregisterSubscriber);

            bind("hasParam", hasParam);
            bind("setParam", setParam);
            bind("getParam", getParam);
            bind("deleteParam", deleteParam);
            bind("paramUpdate", paramUpdate);
            bind("subscribeParam", subscribeParam);
            bind("getParamNames", getParamNames);

            bind("getPid", getPid);
            bind("getBusStats", getBusStatus);
            bind("getBusInfo", getBusInfo);

            bind("Time", getTime);

            bind("Duration", getTime);

            bind("get_rostime", getTime);

            bind("get_time", getTime);
            bind("lookupNode", lookupNode);

            bind("getTopicTypes", getTopicTypes);
            
            
            //SERVICE??

            bool bound = server.BindAndListen(what); //use any port available
            if (!bound)
                throw new Exception("RPCServer bind failed");
            port = server.Port;
            if (port == 0)
                throw new Exception("RPCServer's port is invalid");
            uri = hostname;

            Console.WriteLine("XmlRpc IN THE HIZI (" + uri + ") FOR SHIZI");
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
            {
                ass.removeFromDispatch(server.Dispatch);
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
            public XMLRPCCallWrapper wrapper;
        }

        #endregion


        public void getTopicTypes([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        {
            XmlRpcValue res = XmlRpcValue.Create(ref result), parm = XmlRpcValue.Create(ref parms);

            String topic = parm[0].GetString();

            String caller_id = parm[1].GetString();
            Dictionary<String, String> types = handler.getTopicTypes(topic);

            XmlRpcValue value = new XmlRpcValue();
            int index = 0;
            foreach (KeyValuePair<String, String> pair in types)
            {
                XmlRpcValue payload = new XmlRpcValue();
                payload.Set(0, pair.Key);
                payload.Set(1, pair.Value);
                value.Set(index, payload);
            }

            res.Set(0, 1);
            res.Set(1, "getTopicTypes");
            res.Set(2, value);
        }


        public void lookupNode([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        {
            XmlRpcValue res = XmlRpcValue.Create(ref result), parm = XmlRpcValue.Create(ref parms);

            String topic = parm[0].GetString();
            String caller_id = parm[1].GetString();
            String api = handler.lookupNode(caller_id, topic);

           // if(api == "")
         //       res.Set(0, 0);
           // else
            res.Set(0, 1);
            res.Set(1, "lookupNode");
            res.Set(2, api);
        }
        

        /// <summary>
        /// Returns list of all publications
        /// </summary>
        /// <param name="parms"></param>
        /// <param name="result"></param>
        public void getPublications([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        {
            XmlRpcValue res = XmlRpcValue.Create(ref result);
            res.Set(0, 1); //length
            res.Set(1, "publications"); //response too
            XmlRpcValue response = new XmlRpcValue(); //guts, new value here

            //response.Size = 0;
            List<List<String>> current = handler.getPublishedTopics("","");
            
            for (int i = 0; i < current.Count; i += 2)
            {
                XmlRpcValue pub = new XmlRpcValue();
                pub.Set(0, current[0]);
                current.RemoveAt(0);
                pub.Set(1, current[0]);
                current.RemoveAt(0);
                response.Set(i, pub);
            }
            res.Set(2, response);
        }

        public void getTime([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        {
            throw new Exception("NOT IMPLEMENTED YET!");
        }
        /// <summary>
        /// Get a list of all subscriptions
        /// </summary>
        /// <param name="parms"></param>
        /// <param name="result"></param>
        public void getSubscriptions([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        {
            throw new Exception("NOT IMPLEMENTED YET!");
        }


        /// <summary>
        /// Notify subscribers of an update??
        /// </summary>
        /// <param name="parms"></param>
        /// <param name="result"></param>
        public void pubUpdate([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        {
            throw new Exception("NOT IMPLEMENTED YET!");
            //mlRpcValue parm = XmlRpcValue.Create(ref parms);
            //List<string> pubs = new List<string>();
            //for (int idx = 0; idx < parm[2].Size; idx++)
            //    pubs.Add(parm[2][idx].Get<string>());
            //if (pubUpdate(parm[1].Get<string>(), pubs))
            //    XmlRpcManager.Instance.responseInt(1, "", 0)(result);
            //else
            //{
            //    EDB.WriteLine("Unknown Error");
            //    XmlRpcManager.Instance.responseInt(0, "Unknown Error or something", 0)(result);
            //}



            //EDB.WriteLine("TopicManager is updating publishers for " + topic);
            //Subscription sub = null;
            //lock (subs_mutex)
            //{
            //    if (shutting_down) return false;
            //    foreach (Subscription s in subscriptions)
            //    {
            //        if (s.name != topic || s.IsDropped)
            //            continue;
            //        sub = s;
            //        break;
            //    }
            //}
            //if (sub != null)
            //    return sub.pubUpdate(pubs);
            //else
            //    EDB.WriteLine("got a request for updating publishers of topic " + topic +
            //                  ", but I don't have any subscribers to that topic.");
            //return false;
        }


        /// <summary>
        /// No clue.
        /// </summary>
        /// <param name="parms"></param>
        /// <param name="result"></param>
        public void requestTopic([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        {
            throw new Exception("NOT IMPLEMENTED YET!");
            XmlRpcValue res = XmlRpcValue.Create(ref result), parm = XmlRpcValue.Create(ref parms);
        }

        /// <summary>
        /// Returns list of all, publishers, subscribers, and services
        /// </summary>
        /// <param name="parms"></param>
        /// <param name="result"></param>
        public void getSystemState([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        {
            XmlRpcValue res = XmlRpcValue.Create(ref result), parm = XmlRpcValue.Create(ref parms);
            res.Set(0, 1);
            res.Set(1, "getSystemState");
            List<List<List<String>>> systemstatelist = handler.getSystemState("");//parm.GetString()

            XmlRpcValue listoftypes = new XmlRpcValue();

            XmlRpcValue listofvalues = new XmlRpcValue();

            int index = 0;
            
            foreach (List<List<String>> types in systemstatelist) //publisher, subscriber, services
            {
                int bullshitindex = 0;
                XmlRpcValue typelist;
                XmlRpcValue bullshit = new XmlRpcValue();
                if (types.Count > 0)
                {
                    foreach (List<String> l in types)
                    {
                        int typeindex = 0;
                        typelist = new XmlRpcValue();
                        //XmlRpcValue value = new XmlRpcValue();
                        typelist.Set(typeindex++, l[0]);
                        XmlRpcValue payload = new XmlRpcValue();
                        for (int i = 1; i < l.Count; i++)
                        {
                            payload.Set(i - 1, l[i]);
                        }

                        typelist.Set(typeindex++, payload);
                        //typelist.Set(typeindex++, value);
                        bullshit.Set(bullshitindex++, typelist);
                    }
                }
                else
                {
                    typelist = new XmlRpcValue();
                    bullshit.Set(bullshitindex++, typelist);
                }


                listoftypes.Set(index++,bullshit);
            }

            res.Set(2,listoftypes);
        }

        /// <summary>
        /// Get a list of all published topics
        /// </summary>
        /// <param name="parms"></param>
        /// <param name="result"></param>
        public void getPublishedTopics([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        {
            XmlRpcValue res = XmlRpcValue.Create(ref result), parm = XmlRpcValue.Create(ref parms);
            List<List<String>> publishedtopics = handler.getPublishedTopics("","");
            res.Set(0, 1);
            res.Set(1, "current system state");

            XmlRpcValue listofvalues = new XmlRpcValue();
            int index = 0;
            foreach (List<String> l in publishedtopics)
            {
                XmlRpcValue value = new XmlRpcValue();
                value.Set(0, l[0]); //Topic Name
                value.Set(1, l[1]); // Topic type
                listofvalues.Set(index, value);
                index++;
            }
            res.Set(2, listofvalues);
        }

        /// <summary>
        /// Register a new publisher to a topic
        /// </summary>
        /// <param name="parms"></param>
        /// <param name="result"></param>
        public void registerPublisher([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        {
            XmlRpcValue res = XmlRpcValue.Create(ref result), parm = XmlRpcValue.Create(ref parms);

            String caller_id = parm[0].GetString();
            String topic = parm[1].GetString();
            String type = parm[2].GetString();
            String caller_api = parm[3].GetString(); //hostname

            Console.WriteLine("PUBLISHING: " + caller_id + " : " + caller_api);

            handler.registerPublisher(caller_id, topic, type, caller_api);
            res.Set(0,1);
            res.Set(1,"GOOD JOB!");
            res.Set(2, new XmlRpcValue(""));


        }

        /// <summary>
        /// Unregister an existing publisher
        /// </summary>
        /// <param name="parms"></param>
        /// <param name="result"></param>
        public void unregisterPublisher([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        {
            XmlRpcValue res = XmlRpcValue.Create(ref result), parm = XmlRpcValue.Create(ref parms);

            String caller_id = parm[0].GetString();
            String topic = parm[1].GetString();
            String caller_api = parm[2].GetString();
            Console.WriteLine("UNPUBLISHING: " + caller_id + " : " + caller_api);

            int ret = handler.unregisterPublisher(caller_id, topic, caller_api);
            res.Set(0, ret);
            res.Set(1, "unregistered " + caller_id+ "as provder of "+ topic);
        }

        /// <summary>
        /// Register a new subscriber
        /// </summary>
        /// <param name="parms"></param>
        /// <param name="result"></param>
        public void registerSubscriber([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        {
            XmlRpcValue res = XmlRpcValue.Create(ref result), parm = XmlRpcValue.Create(ref parms);

            String caller_id = parm[0].GetString();
            String topic = parm[1].GetString();
            String type = parm[2].GetString();
            String caller_api = parm[3].GetString(); //hostname

            handler.registerSubscriber(caller_id, topic, type, caller_api);
            res.Set(0, 1);
            res.Set(1, "GOOD JOB!");
            res.Set(2, new XmlRpcValue(""));

            //string uri = XmlRpcManager.Instance.uri;

            //XmlRpcValue args = new XmlRpcValue(this_node.Name, s.name, datatype, uri);
            //XmlRpcValue result = new XmlRpcValue();
            //XmlRpcValue payload = new XmlRpcValue();
            //if (!master.execute("registerSubscriber", args, ref result, ref payload, true))
            //    return false;
            //List<string> pub_uris = new List<string>();
            //for (int i = 0; i < payload.Size; i++)
            //{
            //    XmlRpcValue asshole = payload[i];
            //    string pubed = asshole.Get<string>();
            //    if (pubed != uri && !pub_uris.Contains(pubed))
            //    {
            //        pub_uris.Add(pubed);
            //    }
            //}
            //bool self_subscribed = false;
            //Publication pub = null;
            //string sub_md5sum = s.md5sum;
            //lock (advertised_topics_mutex)
            //{
            //    foreach (Publication p in advertised_topics)
            //    {
            //        pub = p;
            //        string pub_md5sum = pub.Md5sum;
            //        if (pub.Name == s.name && md5sumsMatch(pub_md5sum, sub_md5sum) && !pub.Dropped)
            //        {
            //            self_subscribed = true;
            //            break;
            //        }
            //    }
            //}

            //s.pubUpdate(pub_uris);
            //if (self_subscribed)
            //    s.addLocalConnection(pub);
            //return true;
        }

        /// <summary>
        /// Unregister an existing subscriber
        /// </summary>
        /// <param name="parms"></param>
        /// <param name="result"></param>
        public void unregisterSubscriber([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        {
            XmlRpcValue res = XmlRpcValue.Create(ref result), parm = XmlRpcValue.Create(ref parms);

            String caller_id = parm[0].GetString();
            String topic = parm[1].GetString();
            String caller_api = parm[2].GetString();

            Console.WriteLine("UNSUBSCRIBING: " + caller_id + " : " + caller_api);

            int ret = handler.unregisterSubscriber(caller_id, topic, caller_api);
            res.Set(0, ret);
            res.Set(1, "unregistered " + caller_id + "as provder of " + topic);

            //throw new Exception("NOT IMPLEMENTED YET!");
            //XmlRpcValue args = new XmlRpcValue(this_node.Name, topic, XmlRpcManager.Instance.uri),
            //            result = new XmlRpcValue(),
            //            payload = new XmlRpcValue();
            //master.execute("unregisterSubscriber", args, ref result, ref payload, false);
            //return true;
        }

        



        /// <summary>
        /// Check whether a parameter exists
        /// </summary>
        /// <param name="parms"></param>
        /// <param name="result"></param>
        public void hasParam([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        {
            XmlRpcValue res = XmlRpcValue.Create(ref result), parm = XmlRpcValue.Create(ref parms);
            res.Set(0, 1);
            res.Set(1, "hasParam");

            String caller_id = parm[0].GetString();
            String topic = parm[1].GetString();

            res.Set(2, handler.hasParam(caller_id, topic));
        }

        /// <summary>
        /// Set a new parameter
        /// </summary>
        /// <param name="parms"></param>
        /// <param name="result"></param>
        public void setParam([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        {
            XmlRpcValue res = XmlRpcValue.Create(ref result), parm = XmlRpcValue.Create(ref parms);
            res.Set(0, 1);
            res.Set(1,"setParam");

            String caller_api = parm[0].GetString();
            String topic = parm[1].GetString();
            XmlRpcValue value = parm[2];
            handler.setParam(caller_api, topic,value);
            res.Set(2, "parameter " + topic + " set");
        }



        /// <summary>
        /// Retrieve a value for an existing parameter, if it exists
        /// </summary>
        /// <param name="parms"></param>
        /// <param name="result"></param>
        public void getParam([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        {
            XmlRpcValue res = XmlRpcValue.Create(ref result), parm = XmlRpcValue.Create(ref parms);
            res.Set(0, 1);
            res.Set(1, "getParam");

            String caller_id = parm[0].GetString();
            String topic = parm[1].GetString();

            // value = new XmlRpcValue();
             XmlRpcValue value = handler.getParam(caller_id, topic);
            //value
             // String vi = v.getString();
             if (value == null)
             {
                 res.Set(0, 0);
                 res.Set(1, "Parameter "+ topic+" is not set");
                 value = new XmlRpcValue("");
             }
            res.Set(2,value);
            
            //XmlRpcValue parm2 = new XmlRpcValue(), result2 = new XmlRpcValue();
            //parm2.Set(0, this_node.Name);
            //parm2.Set(1, mapped_key);

            //bool ret = master.execute("getParam", parm2, ref result2, ref v, false);
            
        }

        /// <summary>
        /// Delete a parameter, if it exists
        /// </summary>
        /// <param name="parms"></param>
        /// <param name="result"></param>
        public void deleteParam([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        {
            throw new Exception("NOT IMPLEMENTED YET!");
            //XmlRpcValue parm = new XmlRpcValue(), result = new XmlRpcValue(), payload = new XmlRpcValue();
            //parm.Set(0, this_node.Name);
            //parm.Set(1, mapped_key);
            //if (!master.execute("deleteParam", parm, ref result, ref payload, false))
            //    return false;
            //return true;
        }

        public void getParamNames([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        {
            XmlRpcValue res = XmlRpcValue.Create(ref result), parm = XmlRpcValue.Create(ref parms);
            res.Set(0, 1);
            res.Set(1, "getParamNames");

            String caller_id = parm[0].GetString();
            List<String> list = handler.getParamNames(caller_id);

            XmlRpcValue response = new XmlRpcValue();
            int index = 0;
            foreach (String s in list)
            {
                response.Set(index++, s);
            }

            res.Set(2, response);


            //throw new Exception("NOT IMPLEMENTED YET!");
            //XmlRpcValue parm = new XmlRpcValue(), result = new XmlRpcValue(), payload = new XmlRpcValue();
            //parm.Set(0, this_node.Name);
            //parm.Set(1, mapped_key);
            //if (!master.execute("deleteParam", parm, ref result, ref payload, false))
            //    return false;
            //return true;
        }



        /// <summary>
        /// Notify of new parameter updates
        /// </summary>
        /// <param name="parm"></param>
        /// <param name="result"></param>
        public static void paramUpdate(IntPtr parm, IntPtr result)
        {
            XmlRpcValue val = XmlRpcValue.LookUp(parm);
            val.Set(0, 1);
            val.Set(1, "");
            val.Set(2, 0);
            //update(XmlRpcValue.LookUp(parm)[1].Get<string>(), XmlRpcValue.LookUp(parm)[2]);
        }

        /// <summary>
        /// Subscribe to a param value
        /// </summary>
        /// <param name="parms"></param>
        /// <param name="result"></param>
        public void subscribeParam([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        {
            throw new Exception("NOT IMPLEMENTED YET!");
        }
        

        /// <summary>
        /// Get BUS status??? WUT
        /// </summary>
        /// <param name="parms"></param>
        /// <param name="result"></param>
        public void getBusStatus([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        {
            throw new Exception("NOT IMPLEMENTED YET!");
        }

        /// <summary>
        /// Get BUS info??? WUT
        /// </summary>
        /// <param name="parms"></param>
        /// <param name="result"></param>
        public void getBusInfo([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        {
            throw new Exception("NOT IMPLEMENTED YET!");
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