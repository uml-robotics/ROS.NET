using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using XmlRpc_Wrapper;
using m = Messages;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;
using System.Threading;

namespace EricIsAMAZING
{
    public class Subscription
    {
        public class CallbackInfo<M> : ICallbackInfo where M : m.IRosMessage, new()
        {
            public new SubscriptionCallbackHelper<M> helper;
        }

        public class ICallbackInfo
        {
            public ISubscriptionCallbackHelper helper;
            public CallbackQueueInterface callback;
            public SubscriptionQueue subscription_queue;
        }

        public object md5sum_mutex = new object(), callbacks_mutex = new object();
        public string name, md5sum, datatype;
        List<ICallbackInfo> callbacks = new List<ICallbackInfo>();
        List<PendingConnection> pending_connections = new List<PendingConnection>();
        List<PublisherLink> publisher_links = new List<PublisherLink>();
        public object publisher_links_mutex = new object();
        public object pending_connections_mutex = new object();
        private bool dropped,shutting_down;
        public int nonconst_callbacks;
        public Subscription(string n, string md5s, string dt)
        {
            name = n;
            md5sum = md5s;
            datatype = dt;
        }
        public void removePublisherLink(PublisherLink pub)
        {
            throw new NotImplementedException();
        }
        public void addPublisherLink(PublisherLink pub)
        {
            throw new NotImplementedException();
        }
        public void drop()
        {
            throw new NotImplementedException();
        }
        public bool PubUpdate(List<string> pubs)
        {
            throw new NotImplementedException();
        }
        public bool NegotiateConnection(string xmlrpc_uri)
        {
            XmlRpcValue tcpros_array = new XmlRpcValue(), protos_array = new XmlRpcValue(), Params = new XmlRpcValue();
            tcpros_array.Set(0, "TCPROS");
            protos_array.Set(protos++, tcpros_array);
            Params.Set(0, this_node.Name);
            Params.Set(1, name);
            Params.Set(2, protos_array);
            throw new NotImplementedException();
        }
        public void headerReceived(PublisherLink link, Header header)
        {
            throw new NotImplementedException();
        }

        internal ulong handleMessage(m.IRosMessage m, bool ser, bool nocopy, System.Collections.IDictionary iDictionary, TransportPublisherLink transportPublisherLink)
        {
            throw new NotImplementedException();
        }

        public bool IsDropped
        {
            get { return dropped; }
            set { dropped = value; }
        }

        private int protos;
        private XmlRpcClient client = null;
        public void ConnectAsync()
        {
            Console.WriteLine("Began asynchronous xmlrpc connection to [" + client.HostUri + "]");
            new Action(() =>
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

        internal bool addCallback<M>(SubscriptionCallbackHelper<M> helper, string md5sum, CallbackQueueInterface queue, int queue_size, bool allow_concurrent_callbacks) where M : m.IRosMessage, new()
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
    }
}
