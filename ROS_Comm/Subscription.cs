// File: Subscription.cs
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Messages;
using XmlRpc_Wrapper;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace Ros_CSharp
{
    public class Subscription
    {
        private bool _dropped;

        private List<ICallbackInfo> callbacks = new List<ICallbackInfo>();
        public object callbacks_mutex = new object();
        public string datatype = "";
        public Dictionary<PublisherLink, LatchInfo> latched_messages = new Dictionary<PublisherLink, LatchInfo>();
        public string md5sum = "";
        public object md5sum_mutex = new object();
        public MsgTypes msgtype;
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
            msgtype = (MsgTypes) Enum.Parse(typeof (MsgTypes), dt.Replace("/", "__"));
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
            conn_data.SetArray(0);
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
                //EDB.WriteLine("SUB: getInfo with " + publisher_links.Count + " publinks in list");
                foreach (PublisherLink c in publisher_links)
                {
                    //EDB.WriteLine("PUB: adding a curr_info to info!");
                    XmlRpcValue curr_info = new XmlRpcValue();
                    curr_info.Set(0, (int) c.ConnectionID);
                    curr_info.Set(1, c.XmlRpc_Uri);
                    curr_info.Set(2, "i");
                    curr_info.Set(3, c.TransportType);
                    curr_info.Set(4, name);
                    //EDB.Write("PUB curr_info DUMP:\n\t");
                    //curr_info.Dump();
                    info.Set(info.Size, curr_info);
                }
                //EDB.WriteLine("SUB: outgoing info is of type: " + info.Type + " and has size: " + info.Size);
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
            string n1;
            string h1 = n1 = "";
            int p2;
            int p1 = p2 = 0;
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

        public bool pubUpdate(IEnumerable<string> pubs)
        {
            lock (shutdown_mutex)
            {
                if (shutting_down || _dropped)
                    return false;
            }
            bool retval = true;
#if DEBUG
#if DUMP
            EDB.WriteLine("Publisher update for [" + name + "]: " + publisher_links.Aggregate(pubs.Aggregate("", (current, s) => current + (s + ", "))+" already have these connections: ", (current, spc) => current + spc.XmlRpc_Uri));
#else
            EDB.WriteLine("Publisher update for [" + name + "]");
#endif
#endif
            List<string> additions = new List<string>();
            List<PublisherLink> subtractions = new List<PublisherLink>();
            lock (publisher_links_mutex)
            {
                subtractions.AddRange(from spc in publisher_links let found = pubs.Any(up_i => urisEqual(spc.XmlRpc_Uri, up_i)) where !found select spc);
                foreach (string up_i in pubs)
                {
                    bool found = publisher_links.Any(spc => urisEqual(up_i, spc.XmlRpc_Uri));
                    if (found) continue;
                    lock (pending_connections_mutex)
                    {
                        if (pending_connections.Any(pc => urisEqual(up_i, pc.RemoteUri)))
                        {
                            found = true;
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
                    EDB.WriteLine("Disconnecting from publisher [" + link.CallerID + "] of topic [" + name +
                                  "] at [" + link.XmlRpc_Uri + "]");
#endif
                    link.drop();
                }
                else
                {
                    EDB.WriteLine("NOT DISCONNECTING FROM MYSELF FOR TOPIC " + name);
                }
            }

            foreach (string i in additions)
            {
                if (XmlRpcManager.Instance.uri != i)
                {
                    retval &= NegotiateConnection(i);
                    //EDB.WriteLine("NEGOTIATINGING");
                }
                else
                    EDB.WriteLine("Skipping myself (" + name + ", " + XmlRpcManager.Instance.uri + ")");
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
                EDB.WriteLine("Bad xml-rpc URI: [" + xmlrpc_uri + "]");
                return false;
            }
            XmlRpcClient c = new XmlRpcClient(peer_host, peer_port);
            if (!c.IsConnected || !c.ExecuteNonBlock("requestTopic", Params))
            {
                EDB.WriteLine("Failed to contact publisher [" + peer_host + ":" + peer_port + "] for topic [" + name +
                              "]");
                c.Dispose();
                return false;
            }
#if DEBUG
            EDB.WriteLine("Began asynchronous xmlrpc connection to http://" + peer_host + ":" + peer_port + "/ for topic [" + name +
                          "]");
#endif
            PendingConnection conn = new PendingConnection(c, this, xmlrpc_uri, Params);
            lock (pending_connections_mutex)
            {
                pending_connections.Add(conn);
            }
            XmlRpcManager.Instance.addAsyncConnection(conn);
            return true;
        }

        public void pendingConnectionDone(PendingConnection conn, XmlRpcValue result)
        {
            //XmlRpcValue result = XmlRpcValue.LookUp(res);
            lock (shutdown_mutex)
            {
                if (shutting_down || _dropped)
                    return;
            }
            XmlRpcValue proto = new XmlRpcValue();
            if (!XmlRpcManager.Instance.validateXmlrpcResponse("requestTopic", result, proto))
            {
                conn.failures++;
                EDB.WriteLine("Negotiating for " + conn.parent.name + " has failed " + conn.failures + " times");
                return;
            }
            lock (pending_connections_mutex)
            {
                pending_connections.Remove(conn);
            }
            string peer_host = conn.client.Host;
            int peer_port = conn.client.Port;
            string xmlrpc_uri = "http://" + peer_host + ":" + peer_port + "/";
            if (proto.Size == 0)
            {
#if DEBUG
                EDB.WriteLine("Couldn't agree on any common protocols with [" + xmlrpc_uri + "] for topic [" + name +
                              "]");
#endif
                return;
            }
            if (proto.Type != XmlRpcValue.ValueType.TypeArray)
            {
                EDB.WriteLine("Available protocol info returned from " + xmlrpc_uri + " is not a list.");
                return;
            }
            string proto_name = proto[0].Get<string>();
            if (proto_name == "UDPROS")
            {
                EDB.WriteLine("OWNED! Only tcpros is supported right now.");
            }
            else if (proto_name == "TCPROS")
            {
                if (proto.Size != 3 || proto[1].Type != XmlRpcValue.ValueType.TypeString || proto[2].Type != XmlRpcValue.ValueType.TypeInt)
                {
                    EDB.WriteLine("publisher implements TCPROS... BADLY! parameters aren't string,int");
                    return;
                }
                string pub_host = proto[1].Get<string>();
                int pub_port = proto[2].Get<int>();
#if DEBUG
                EDB.WriteLine("Connecting via tcpros to topic [" + name + "] at host [" + pub_host + ":" + pub_port +
                              "]");
#endif

                TcpTransport transport = new TcpTransport(PollManager.Instance.poll_set) {_topic = name};
                if (transport.connect(pub_host, pub_port))
                {
                    Connection connection = new Connection();
                    TransportPublisherLink pub_link = new TransportPublisherLink(this, xmlrpc_uri);

                    connection.initialize(transport, false, null);
                    pub_link.initialize(connection);

                    ConnectionManager.Instance.addConnection(connection);

                    lock (publisher_links_mutex)
                    {
                        addPublisherLink(pub_link);
                    }


#if DEBUG
                    EDB.WriteLine("Connected to publisher of topic [" + name + "] at  [" + pub_host + ":" + pub_port +
                                  "]");
#endif
                }
                else
                {
                    EDB.WriteLine("Failed to connect to publisher of topic [" + name + "] at  [" + pub_host + ":" +
                                  pub_port + "]");
                }
            }
            else
            {
                EDB.WriteLine("Your xmlrpc server be talking jibber jabber, foo");
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

        internal ulong handleMessage(IRosMessage msg, bool ser, bool nocopy, IDictionary connection_header,
            PublisherLink link)
        {
            IRosMessage t = null;
            ulong drops = 0;
            TimeData receipt_time = ROS.GetTime().data;
            if (msg.Serialized != null) //will be null if self-subscribed
                msg.Deserialize(msg.Serialized);
            lock (callbacks_mutex)
            {
                foreach (ICallbackInfo info in callbacks)
                {
                    MsgTypes ti = info.helper.type;
                    if (nocopy || ser)
                    {
                        t = msg;
                        t.connection_header = msg.connection_header;
                        t.Serialized = null;
                        bool was_full = false;
                        bool nonconst_need_copy = callbacks.Count > 1;
                        info.subscription_queue.pushitgood(info.helper, t, nonconst_need_copy, ref was_full, receipt_time);
                        if (was_full)
                            ++drops;
                        else
                            info.callback.addCallback(info.subscription_queue, info.Get());
                    }
                }
            }

            if (t != null && link.Latched)
            {
                LatchInfo li = new LatchInfo
                {
                    message = t,
                    link = link,
                    connection_header = connection_header,
                    receipt_time = receipt_time
                };
                if (latched_messages.ContainsKey(link))
                    latched_messages[link] = li;
                else
                    latched_messages.Add(link, li);
            }

            return drops;
        }

        public void Dispose()
        {
            shutdown();
        }

        internal bool addCallback<M>(SubscriptionCallbackHelper<M> helper, string md5sum, CallbackQueueInterface queue,
            uint queue_size, bool allow_concurrent_callbacks, string topiclol) where M : IRosMessage, new()
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
                CallbackInfo<M> info = new CallbackInfo<M> {helper = helper, callback = queue, subscription_queue = new Callback<M>(helper.Callback.func, topiclol, queue_size, allow_concurrent_callbacks)};
                //if (!helper.isConst())
                //{
                ++nonconst_callbacks;
                //}

                callbacks.Add(info);

                if (latched_messages.Count > 0)
                {
                    MsgTypes ti = info.helper.type;
                    lock (publisher_links_mutex)
                    {
                        foreach (PublisherLink link in publisher_links)
                        {
                            if (link.Latched)
                            {
                                if (latched_messages.ContainsKey(link))
                                {
                                    LatchInfo latch_info = latched_messages[link];
                                    bool was_full = false;
                                    bool nonconst_need_copy = callbacks.Count > 1;
                                    info.subscription_queue.pushitgood(info.helper, latched_messages[link].message, nonconst_need_copy, ref was_full, ROS.GetTime().data);
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
                        //if (!helper.isConst())
                        --nonconst_callbacks;
                        break;
                    }
                }
            }
        }

        public void addLocalConnection(Publication pub)
        {
            lock (publisher_links_mutex)
            {
                if (_dropped) return;

                EDB.WriteLine("Creating intraprocess link for topic [{0}]", name);

                LocalPublisherLink pub_link = new LocalPublisherLink(this, XmlRpcManager.Instance.uri);
                LocalSubscriberLink sub_link = new LocalSubscriberLink(pub);
                pub_link.setPublisher(sub_link);
                sub_link.setSubscriber(pub_link);

                addPublisherLink(pub_link);
                pub.addSubscriberLink(sub_link);
            }
        }

        public void getPublishTypes(ref bool ser, ref bool nocopy, MsgTypes ti)
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

#if !TRACE
        [DebuggerStepThrough]
#endif
        public class CallbackInfo<M> : ICallbackInfo where M : IRosMessage, new()
        {
            public CallbackInfo()
            {
                helper = new SubscriptionCallbackHelper<M>(new M().msgtype());
            }
        }

        #endregion

        #region Nested type: ICallbackInfo

#if !TRACE
        [DebuggerStepThrough]
#endif
        public class ICallbackInfo
        {
            public CallbackQueueInterface callback;
            public ISubscriptionCallbackHelper helper;
            public CallbackInterface subscription_queue;

            public UInt64 Get()
            {
                return subscription_queue.Get();
            }
        }

        #endregion

        #region Nested type: LatchInfo

#if !TRACE
        [DebuggerStepThrough]
#endif
        public class LatchInfo
        {
            public IDictionary connection_header;
            public PublisherLink link;
            public IRosMessage message;
            public TimeData receipt_time;
        }

        #endregion
    }
}