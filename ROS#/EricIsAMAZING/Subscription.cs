#region USINGZ

using System;
using System.Collections;
using System.Collections.Generic;
using Messages;
using XmlRpc_Wrapper;
using m = Messages;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace EricIsAMAZING
{
    public class Subscription
    {
        private List<ICallbackInfo> callbacks = new List<ICallbackInfo>();
        public object callbacks_mutex = new object();
        private XmlRpcClient client;
        public string datatype;
        public string md5sum;
        public object md5sum_mutex = new object();
        public string name;
        public int nonconst_callbacks;
        private List<PendingConnection> pending_connections = new List<PendingConnection>();
        public object pending_connections_mutex = new object();
        private List<PublisherLink> publisher_links = new List<PublisherLink>();
        public object publisher_links_mutex = new object(), shutdown_mutex = new object();
        private bool shutting_down;
        public bool IsDropped { get { return _dropped; } }
        private bool _dropped;

        public Subscription(string n, string md5s, string dt)
        {
            name = n;
            md5sum = md5s;
            datatype = dt;
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
                    v.Set(0, new XmlRpcValue(link.ConnectionID));
                    v.Set(1, new XmlRpcValue(s.bytes_received));
                    v.Set(2, new XmlRpcValue(s.messages_received));
                    v.Set(3, new XmlRpcValue(s.drops));
                    v.Set(4, new XmlRpcValue(0));
                    conn_data.Set(cidx++, v);
                }
            }
            stats.Set(1, conn_data);
            return stats;
        }

        public void getInfo(ref XmlRpcValue info)
        {
            lock (publisher_links_mutex)
            {
                foreach (PublisherLink c in publisher_links)
                {
                    XmlRpcValue curr_info = new XmlRpcValue();
                    curr_info.Set(0, new XmlRpcValue(c.ConnectionID));
                    curr_info.Set(1, c.XmlRpc_Uri);
                    curr_info.Set(2, "i");
                    curr_info.Set(3, c.TransportType);
                    curr_info.Set(4, name);
                    info.Set(info.Size, curr_info);
                }
            }
        }

        public int NumPublishers
        {
            get { lock (publisher_links_mutex) return publisher_links.Count; }
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
            string h1, n1;
            h1 = n1 = "";
            int p1, p2;
            p1 = p2 = 0;
            network.splitURI(ref uri1, ref h1, ref p1);
            network.splitURI(ref uri2, ref n1, ref p2);
            return h1 == n1 && p1 == p2;
        }

        public void removePublisherLink(PublisherLink pub)
        {
            throw new NotImplementedException();
        }

        public void addPublisherLink(PublisherLink pub)
        {
            throw new NotImplementedException();
        }
        public bool PubUpdate(List<string> pubs)
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
                                    found = true; break;
                                }
                            }
                            if (!found) additions.Add(up_i);
                        }
                    }
                }

                foreach (PublisherLink link in subtractions)
                {
                    if (link.XmlRpc_Uri != XmlRpcManager.Instance().uri)
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
                    if (XmlRpcManager.Instance().uri != i)
                        retval &= NegotiateConnection(i);
                    else
                        Console.WriteLine("Skipping myself (" + name + ", " + XmlRpcManager.Instance().uri + ")");
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
            if (!network.splitURI(ref xmlrpc_uri, ref peer_host, ref peer_port))
            {
                Console.WriteLine("Bad xml-rpc URI: [" + xmlrpc_uri + "]");
                return false;
            }

            throw new NotImplementedException();
        }

        public void headerReceived(PublisherLink link, Header header)
        {
            throw new NotImplementedException();
        }

        internal ulong handleMessage(IRosMessage m, bool ser, bool nocopy, IDictionary iDictionary, TransportPublisherLink transportPublisherLink)
        {
            throw new NotImplementedException();
        }

        public void ConnectAsync()
        {
            Console.WriteLine("Began asynchronous xmlrpc connection to [" + client.HostUri + "]");
            new Action
                (() =>
                     {
                         XmlRpcValue result = new XmlRpcValue();
                         while (!client.ExecuteCheckDone(result))
                         {
                             Console.WriteLine("NOT DONE YET!");
                         }
                         Console.WriteLine("HOLY SHIT I GOT SOMETHING BACK!");
                         Console.WriteLine(result);
                     }).BeginInvoke(null, null);
        }

        public void Shutdown()
        {
            if (client != null)
            {
                if (!client.IsNull)
                    client.Close();
                client = null;
            }
        }

        public void Dispose()
        {
            Shutdown();
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
            }
        }

        public void addLocalConnection(Subscription sub)
        {
            throw new Exception("NO LOCAL CONNECTIONS, BUTTHEAD");
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
            public CallbackQueueInterface callback;
            public ISubscriptionCallbackHelper helper;
            public SubscriptionQueue subscription_queue;
        }

        #endregion
    }
}