#region USINGZ

using System;
using System.Collections;
using System.Collections.Generic;
using Messages;
using XmlRpc_Wrapper;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace EricIsAMAZING
{
    public class Subscription
    {
        private bool _dropped;
        public Dictionary<MsgTypes, IMessageDeserializer> cached_deserializers = new Dictionary<MsgTypes, IMessageDeserializer>();
        private List<ICallbackInfo> callbacks = new List<ICallbackInfo>();
        public object callbacks_mutex = new object();
        public string datatype = "";
        public Dictionary<PublisherLink, LatchInfo> latched_messages = new Dictionary<PublisherLink, LatchInfo>();
        public string md5sum = "";
        public object md5sum_mutex = new object();
        public string name = "";
        public int nonconst_callbacks;
        public List<PendingConnection> pending_connections = new List<PendingConnection>();
        public object pending_connections_mutex = new object();
        public List<PublisherLink> publisher_links = new List<PublisherLink>();
        public object publisher_links_mutex = new object(), shutdown_mutex = new object();
        private bool shutting_down;

        public Subscription(string n, string md5s, string dt)
        {
            name = n;
            md5sum = md5s;
            datatype = dt;
        }

        public bool IsDropped
        {
            get { return _dropped; }
        }

        public int NumPublishers
        {
            get { lock (publisher_links_mutex) return publisher_links.Count; }
        }

        public int NumCallbacks
        {
            get { lock (callbacks_mutex) return callbacks.Count; }
        }

        public void shutdown()
        {
            lock (shutdown_mutex)
            {
                shutting_down = true;
            }
            drop();
        }

        public XmlRpcValue getStats()
        {
            XmlRpcValue stats = new XmlRpcValue();
            stats.Set(0, name);
            XmlRpcValue conn_data = new XmlRpcValue();
            conn_data.Size = 0;
            lock (publisher_links_mutex)
            {
                int cidx = 0;
                foreach (PublisherLink link in publisher_links)
                {
                    XmlRpcValue v = new XmlRpcValue();
                    PublisherLink.Stats s = link.stats;
                    v.Set(0, link.ConnectionID);
                    v.Set(1, s.bytes_received);
                    v.Set(2, s.messages_received);
                    v.Set(3, s.drops);
                    v.Set(4, 0);
                    conn_data.Set(cidx++, v);
                }
            }
            stats.Set(1, conn_data);
            return stats;
        }

        public void getInfo(XmlRpcValue info)
        {
            lock (publisher_links_mutex)
            {
                //Console.WriteLine("SUB: getInfo with " + publisher_links.Count + " publinks in list");
                foreach (PublisherLink c in publisher_links)
                {
                    //Console.WriteLine("PUB: adding a curr_info to info!");
                    XmlRpcValue curr_info = new XmlRpcValue();
                    curr_info.Set(0, (int) c.ConnectionID);
                    curr_info.Set(1, c.XmlRpc_Uri);
                    curr_info.Set(2, "i");
                    curr_info.Set(3, c.TransportType);
                    curr_info.Set(4, name);
                    //Console.Write("PUB curr_info DUMP:\n\t");
                    //curr_info.Dump();
                    info.Set(info.Size, curr_info);
                }
                //Console.WriteLine("SUB: outgoing info is of type: " + info.Type + " and has size: " + info.Size);
            }
        }

        public void drop()
        {
            if (!_dropped)
            {
                _dropped = true;
                dropAllConnections();
            }
        }

        public void dropAllConnections()
        {
            List<PublisherLink> localsubscribers = null;
            lock (publisher_links_mutex)
            {
                localsubscribers = new List<PublisherLink>(publisher_links);
                publisher_links.Clear();
            }
            foreach (PublisherLink it in localsubscribers)
            {
                //hot it's like
                it.drop();
                //drop it like it's hot, backwards.
            }
        }

        public bool urisEqual(string uri1, string uri2)
        {
            if (uri1 == null || uri2 == null)
                throw new Exception("ZOMG IT'S NULL IN URISEQUAL!");
            string h1, n1;
            h1 = n1 = "";
            int p1, p2;
            p1 = p2 = 0;
            network.splitURI(uri1, ref h1, ref p1);
            network.splitURI(uri2, ref n1, ref p2);
            return h1 == n1 && p1 == p2;
        }

        public void removePublisherLink(PublisherLink pub)
        {
            lock (publisher_links_mutex)
            {
                if (publisher_links.Contains(pub))
                {
                    publisher_links.Remove(pub);
                }
                if (pub.Latched)
                    latched_messages.Remove(pub);
            }
        }

        public void addPublisherLink(PublisherLink pub)
        {
            publisher_links.Add(pub);
        }

        public bool pubUpdate(List<string> pubs)
        {
            lock (shutdown_mutex)
            {
                if (shutting_down || _dropped)
                    return false;
            }
            bool retval = true;
#if DEBUG
            string ss = "";
            foreach (string s in pubs)
            {
                ss += s + ", ";
            }
            ss += " already have these connections: ";
            foreach (PublisherLink spc in publisher_links)
                ss += spc.XmlRpc_Uri;
            Console.WriteLine("Publisher update for [" + name + "]: " + ss);
#endif
            string tt = "Publisher URIS passed to publisher update = ";
            foreach (string s in pubs)
                tt += "\n\t" + s;
            Console.WriteLine(tt);
            List<string> additions = new List<string>();
            List<PublisherLink> subtractions = new List<PublisherLink>(), to_add = new List<PublisherLink>();
            lock (publisher_links_mutex)
            {
                foreach (PublisherLink spc in publisher_links)
                {
                    bool found = false;
                    foreach (string up_i in pubs)
                    {
                        if (urisEqual(spc.XmlRpc_Uri, up_i))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found) subtractions.Add(spc);
                }
                foreach (string up_i in pubs)
                {
                    bool found = false;
                    foreach (PublisherLink spc in publisher_links)
                    {
                        if (urisEqual(up_i, spc.XmlRpc_Uri))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        lock (pending_connections_mutex)
                        {
                            foreach (PendingConnection pc in pending_connections)
                            {
                                if (urisEqual(up_i, pc.RemoteUri))
                                {
                                    found = true;
                                    break;
                                }
                            }
                            if (!found) additions.Add(up_i);
                        }
                    }
                }

                foreach (PublisherLink link in subtractions)
                {
                    if (link.XmlRpc_Uri != XmlRpcManager.Instance.uri)
                    {
#if DEBUG
                        Console.WriteLine("Disconnecting from publisher [" + link.CallerID + "] of topic [" + name + "] at [" + link.XmlRpc_Uri + "]");
                        link.drop();
#endif
                    }
                    else
                    {
#if DEBUG
                        Console.WriteLine("NOT DISCONNECTING FROM MYSELF FOR TOPIC " + name);
#endif
                    }
                }

                foreach (string i in additions)
                {
                    if (XmlRpcManager.Instance.uri != i)
                        retval &= NegotiateConnection(i);
                    else
                        Console.WriteLine("Skipping myself (" + name + ", " + XmlRpcManager.Instance.uri + ")");
                }
            }
            return retval;
        }

        public bool NegotiateConnection(string xmlrpc_uri)
        {
            int protos = 0;
            XmlRpcValue tcpros_array = new XmlRpcValue(), protos_array = new XmlRpcValue(), Params = new XmlRpcValue();
            tcpros_array.Set(0, "TCPROS");
            protos_array.Set(protos++, tcpros_array);
            Params.Set(0, this_node.Name);
            Params.Set(1, name);
            Params.Set(2, protos_array);
            string peer_host = "";
            int peer_port = 0;
            if (!network.splitURI(xmlrpc_uri, ref peer_host, ref peer_port))
            {
                Console.WriteLine("Bad xml-rpc URI: [" + xmlrpc_uri + "]");
                return false;
            }
            XmlRpcClient c = new XmlRpcClient(peer_host, peer_port);
            if (!c.ExecuteNonBlock("requestTopic", Params))
            {
                Console.WriteLine("Failed to contact publisher [" + peer_host + ":" + peer_port + "] for topic [" + name + "]");
                c.Dispose();
                return false;
            }
#if DEBUG
            Console.WriteLine("Began asynchronous xmlrpc connection to [" + peer_host + ":" + peer_port + "]");
#endif
            PendingConnection conn = new PendingConnection(c, this, xmlrpc_uri);
            lock (pending_connections_mutex)
            {
                pending_connections.Add(conn);
            }
            XmlRpcManager.Instance.addAsyncConnection(conn);
            return true;
        }

        public void pendingConnectionDone(PendingConnection conn, IntPtr res)
        {
            XmlRpcValue result = XmlRpcValue.LookUp(res);
            lock (shutdown_mutex)
            {
                if (shutting_down || _dropped)
                    return;
            }
            lock (pending_connections_mutex)
            {
                pending_connections.Remove(conn);
            }
            string peer_host = conn.client.Host;
            int peer_port = conn.client.Port;
            string xmlrpc_uri = "http://" + peer_host + ":" + peer_port + "/";
            Console.WriteLine("PENDING CONNECTION DONE W/ " + xmlrpc_uri);
            XmlRpcValue proto = new XmlRpcValue();
            if (!XmlRpcManager.Instance.validateXmlrpcResponse("requestTopic", result, ref proto))
            {
                Console.WriteLine("Failed to contact publisher [" + xmlrpc_uri + "] for topic [" + name + "]");
                return;
            }
            if (proto.Size == 0)
            {
#if DEBUG
                Console.WriteLine("Coudsn't agree on any common protocols with [" + xmlrpc_uri + "] for topic [" + name + "]");
#endif
                return;
            }
            if (proto.Type != TypeEnum.TypeArray)
            {
                Console.WriteLine("Available protocol info returned from " + xmlrpc_uri + " is not a list.");
                return;
            }
            string proto_name = proto[0].Get<string>();
            if (proto_name == "UDPROS")
            {
                Console.WriteLine("OWNED! Only tcpros is supported right now.");
                return;
            }
            else if (proto_name == "TCPROS")
            {
                if (proto.Size != 3 || proto[1].Type != TypeEnum.TypeString || proto[2].Type != TypeEnum.TypeInt)
                {
                    Console.WriteLine("publisher implements TCPROS... BADLY! parameters aren't string,int");
                    return;
                }
                string pub_host = proto[1].Get<string>();
                int pub_port = proto[2].Get<int>();
#if DEBUG
                Console.WriteLine("Connecting via tcpros to topic [" + name + "] at host [" + pub_host + ":" + pub_port + "]");
#endif

                TcpTransport transport = new TcpTransport(PollManager.Instance.poll_set);
                if (transport.connect(pub_host, pub_port))
                {
                    Connection connection = new Connection();
                    TransportPublisherLink pub_link = new TransportPublisherLink(this, xmlrpc_uri);

                    connection.initialize(transport, false, (c, h) => true);
                    pub_link.initialize(connection);

                    ConnectionManager.Instance.addConnection(connection);

                    lock (publisher_links_mutex)
                    {
                        addPublisherLink(pub_link);
                    }


#if DEBUG
                    Console.WriteLine("Connected to publisher of topic [" + name + "] at  [" + pub_host + ":" + pub_port + "]");
#endif
                }
                else
                {
                    Console.WriteLine("Failed to connect to publisher of topic [" + name + "] at  [" + pub_host + ":" + pub_port + "]");
                }
            }
            else
            {
                Console.WriteLine("Your xmlrpc server be talking jibber jabber, foo");
                return;
            }
        }

        public void headerReceived(PublisherLink link, Header header)
        {
            lock (md5sum_mutex)
            {
                if (md5sum == "*")
                    md5sum = link.md5sum;
            }
        }

        internal ulong handleMessage(IRosMessage msg, bool ser, bool nocopy, IDictionary connection_header, TransportPublisherLink link)
        {
            lock (callbacks_mutex)
            {
                ulong drops = 0;
                cached_deserializers.Clear();
                DateTime receipt_time = DateTime.Now;
                foreach (ICallbackInfo info in callbacks)
                {
                    MsgTypes ti = info.helper.type;
                    if (nocopy && ti != MsgTypes.Unknown || ser && (msg.type == MsgTypes.Unknown || ti != msg.type))
                    {
                        IMessageDeserializer deserializer = null;
                        if (cached_deserializers.ContainsKey(ti))
                            deserializer = cached_deserializers[ti];
                        else
                        {
                            deserializer = MakeDeserializer(ti);
                            cached_deserializers.Add(ti, deserializer);
                        }
                        bool was_full = false;
                        bool nonconst_need_copy = false;
                        if (callbacks.Count > 1)
                            nonconst_need_copy = true;
                        info.subscription_queue.push(info.helper, deserializer, nonconst_need_copy, ref was_full, receipt_time);
                        if (was_full)
                            ++drops;
                        else
                            info.callback.addCallback(info.subscription_queue, info.Get());
                    }
                }

                if (link.Latched)
                {
                    LatchInfo li = new LatchInfo {message = msg, link = link, connection_header = connection_header, receipt_time = receipt_time};
                    if (latched_messages.ContainsKey(link))
                        latched_messages[link] = li;
                    else
                        latched_messages.Add(link, li);
                }

                cached_deserializers.Clear();
                return drops;
            }
        }

        public IMessageDeserializer MakeDeserializer(MsgTypes type)
        {
            if (type == MsgTypes.Unknown) return null;
            return ROS.MakeDeserializer(ROS.MakeMessage<IRosMessage>(type));
            //return (IMessageDeserializer)Activator.CreateInstance(typeof(MessageDeserializer<>).MakeGenericType(TypeHelper.Types[type].GetGenericArguments()));
        }

        public void Dispose()
        {
            shutdown();
        }

        internal bool addCallback<M>(SubscriptionCallbackHelper<M> helper, string md5sum, CallbackQueueInterface queue, int queue_size, bool allow_concurrent_callbacks) where M : IRosMessage, new()
        {
            lock (md5sum_mutex)
            {
                if (this.md5sum == "*" && md5sum != "*")
                    this.md5sum = md5sum;
            }

            if (md5sum != "*" && md5sum != this.md5sum)
                return false;
            lock (callbacks_mutex)
            {
                CallbackInfo<M> info = new CallbackInfo<M>();
                info.helper = helper;
                info.callback = queue;
                info.subscription_queue = new SubscriptionQueue(name, queue_size, allow_concurrent_callbacks);
                if (!helper.isConst())
                {
                    ++nonconst_callbacks;
                }

                callbacks.Add(info);

                if (latched_messages.Count > 0)
                {
                    lock (publisher_links_mutex)
                    {
                        foreach (PublisherLink link in publisher_links)
                        {
                            if (link.Latched)
                            {
                                if (latched_messages.ContainsKey(link))
                                {
                                    LatchInfo latch_info = latched_messages[link];
                                    IMessageDeserializer des = new IMessageDeserializer(helper, latch_info.message, latch_info.connection_header);
                                    bool was_full = false;
                                    info.subscription_queue.push(info.helper, des, true, ref was_full, latch_info.receipt_time);
                                    if (!was_full)
                                        info.callback.addCallback(info.subscription_queue, info.Get());
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }

        public void removeCallback(ISubscriptionCallbackHelper helper)
        {
            lock (callbacks_mutex)
            {
                foreach (ICallbackInfo info in callbacks)
                {
                    if (info.helper == helper)
                    {
                        info.subscription_queue.clear();
                        info.callback.removeByID(info.Get());
                        callbacks.Remove(info);
                        if (!helper.isConst())
                            --nonconst_callbacks;
                        break;
                    }
                }
            }
        }

        public void addLocalConnection(Publication pub)
        {
            throw new Exception("NO LOCAL CONNECTIONS, BUTTHEAD");
        }

        public void getPublishTypes(ref bool ser, ref bool nocopy, ref MsgTypes ti)
        {
            lock (callbacks_mutex)
            {
                foreach (ICallbackInfo info in callbacks)
                {
                    if (info.helper.type == ti)
                        nocopy = true;
                    else
                        ser = true;
                    if (nocopy && ser)
                        return;
                }
            }
        }

        #region Nested type: CallbackInfo

        public class CallbackInfo<M> : ICallbackInfo where M : IRosMessage, new()
        {
            public new SubscriptionCallbackHelper<M> helper;
        }

        #endregion

        #region Nested type: ICallbackInfo

        public class ICallbackInfo
        {
            private static UInt64 _uid;
            private UInt64 __uid;
            public CallbackQueueInterface callback;
            public ISubscriptionCallbackHelper helper;
            public SubscriptionQueue subscription_queue;

            public ICallbackInfo()
            {
                __uid = _uid++;
            }

            public UInt64 Get()
            {
                return __uid;
            }
        }

        #endregion

        #region Nested type: LatchInfo

        public class LatchInfo
        {
            public IDictionary connection_header;
            public PublisherLink link;
            public IRosMessage message;
            public DateTime receipt_time;
        }

        #endregion
    }
}