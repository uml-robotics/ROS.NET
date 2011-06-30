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
        public object md5sum_mutex = new object(), callbacks_mutex = new object();
        public string name, md5sum, datatype;
        List<ICallbackInfo> callbacks = new List<ICallbackInfo>();
        List<PendingConnection> pendingconnections = new List<PendingConnection>();
        private bool dropped,shutting_down;
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
        public bool PubUpdatte(List<string> pubs)
        {
            throw new NotImplementedException();
        }
        public bool NegotiateConnection(string xmlrpc_uri)
        {
            client = new XmlRpcClient(xmlrpc_uri);
            tcpros_array.Set(0, "TCPROS");
            protos_array.Set(protos++, tcpros_array);
            Params.Set(0, name);
            Params.Set(1, this_node.Name);
            Params.Set(2, protos_array);
            return !client.IsNull && client.ExecuteNonBlock("requestTopic", Params);
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
        private XmlRpcValue tcpros_array = new XmlRpcValue(), protos_array = new XmlRpcValue(), Params = new XmlRpcValue();
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

        internal bool addCallback<M>(SubscriptionCallbackHelper<M> helper, string md5sum, CallbackInterface queue) where M : m.IRosMessage
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
    }
}
