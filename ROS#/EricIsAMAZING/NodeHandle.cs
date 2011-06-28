using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using XmlRpc_Wrapper;
using m = Messages;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;
using System.Threading;

namespace EricIsAMAZING
{
    public class NodeHandle
    {
        public void shutdown()
        {
            foreach (ISubscriber sub in collection.subscribers)
                sub.impl.unsubscribe();
            foreach (IPublisher pub in collection.publishers)
                pub.impl.unadvertise();
            foreach (IServiceClient client in collection.serviceclients)
                client.impl.shutdown();
            foreach (IServiceServer srv in collection.serviceservers)
                srv.impl.unadvertise();
        }

        public NodeHandle(string ns, IDictionary remappings)
        {
            if (ns != "" && ns[0] == '~')
                ns = names.resolve(ns);
            construct(ns, true);
            initRemappings(remappings);
        }

        public NodeHandle(NodeHandle rhs)
        {
            callbackQueue = rhs.callbackQueue;
            remappings = rhs.remappings;
            unresolved_remappings = rhs.unresolved_remappings;
            construct(rhs.Namespace, true);
            UnresolvedNamespace = rhs.UnresolvedNamespace;
        }

        public NodeHandle(NodeHandle parent, string ns)
        {
            Namespace = parent.Namespace;
            callbackQueue = parent.callbackQueue;
            remappings = parent.remappings;
            unresolved_remappings = parent.unresolved_remappings;
            construct(ns, false);
        }

        public NodeHandle(NodeHandle parent, string ns, IDictionary remappings)
        {
            Namespace = parent.Namespace;
            callbackQueue = parent.callbackQueue;
            this.remappings = parent.remappings;
            unresolved_remappings = parent.unresolved_remappings;

        }

        public NodeHandle()
        {
            
        }
        public class NodeHandleBackingCollection : IDisposable
        {
            public List<IPublisher> publishers = new List<IPublisher>();
            public List<IServiceServer> serviceservers = new List<IServiceServer>();
            public List<ISubscriber> subscribers = new List<ISubscriber>();
            public List<IServiceClient> serviceclients = new List<IServiceClient>();
            public object mutex = new object();
            public void Dispose()
            {
                publishers.Clear();
                serviceservers.Clear();
                subscribers.Clear();
                serviceclients.Clear();
            }
        }
        public NodeHandleBackingCollection collection;
        public CallbackQueue callbackQueue
        {
            get
            {
                if (_callbackQueue == null) return ROS.GlobalCallbackQueue;
                return _callbackQueue;
            }
            set { _callbackQueue = value; }
        }
        public bool ok { get { return ROS.ok && _ok; } set { _ok = value; }
        }
        public int nh_refcount = 0;
        public object nh_refcount_mutex = new object();
        private bool _ok;
        public bool node_started_by_nh = false;
        private CallbackQueue _callbackQueue;
        public string Namespace, UnresolvedNamespace;
        public bool no_validate = false;
        public IDictionary remappings = new Hashtable(),unresolved_remappings = new Hashtable();

        Publisher<M> advertise<M>(string topic, int q_size, bool l = false)
        {
            return advertise<M>(new AdvertiseOptions(topic, q_size) { latch = l });
        }

        Publisher<M> advertise<M>(string topic, int queue_size, MulticastDelegate connectcallback, MulticastDelegate disconnectcallback, bool l = false)
        {
            return advertise<M>(new AdvertiseOptions(topic, queue_size, connectcallback, disconnectcallback) { latch = l });
        }

        Publisher<M> advertise<M>(AdvertiseOptions ops)
        {
            ops.topic = resolveName(ops.topic);
            if (ops.callbackQueue == null)
            {
                if (callbackQueue != null)
                    ops.callbackQueue = callbackQueue;
                else
                    ops.callbackQueue = ROS.GlobalCallbackQueue;
            }
            SubscriberCallbacks callbacks = new SubscriberCallbacks(ops.connectCB, ops.disconnectCB, ops.callbackQueue);
            if (TopicManager.Instance().advertise(ops, callbacks))
            {
                Publisher<M> pub = new Publisher<M>(ops.topic, ops.md5sum, ops.datatype, this, callbacks);
                lock (collection.mutex)
                {
                    collection.publishers.Add(pub);
                }
                return pub;
            }
            return new Publisher<M>();
        }
        Subscriber<M> subscribe<M>(string topic, int queue_size, CallbackQueue cb)
        {
            return subscribe<M>(new SubscribeOptions<M>(topic, queue_size, cb));
        }

        Subscriber<M> subscribe<M>(SubscribeOptions<M> ops)
        {
            ops.topic = resolveName(ops.topic);
            if (ops.callbackQueue == null)
            {
                if (callbackQueue != null)
                    ops.callbackQueue = callbackQueue;
                else
                    ops.callbackQueue = ROS.GlobalCallbackQueue;
            }
            if (TopicManager.Instance().subscribe<M>(ops))
            {
                Subscriber<M> sub = new Subscriber<M>(ops.topic, this, new SubscriptionCallbackHelper(ops.callbackQueue));
                lock (collection.mutex)
                {
                    collection.subscribers.Add(sub);
                }
                return sub;
            }
            return new Subscriber<M>();
        }

        public ServiceServer<T,MReq,MRes> advertiseService<T,MReq,MRes>(string service, Func<MReq,MRes> srv_func)
        {
            return advertiseService<T,MReq,MRes>(new AdvertiseServiceOptions<MReq, MRes>(service, srv_func));
        }

        public ServiceServer<T,MReq,MRes> advertiseService<T,MReq,MRes>(AdvertiseServiceOptions<MReq,MRes> ops)
        {
            ops.service = resolveName(ops.service);
            if (ops.callbackQueue == null)
            {
                if (callbackQueue == null)
                    ops.callbackQueue = ROS.GlobalCallbackQueue;
                else
                    ops.callbackQueue = callbackQueue;
            }
            if (ServiceManager.Instance().advertiseService<MReq,MRes>(ops))
            {
                ServiceServer<T, MReq, MRes> srv = new ServiceServer<T, MReq, MRes>(ops.service, this);
                lock (collection.mutex)
                {
                    collection.serviceservers.Add(srv);
                }
                return srv;
            }
            return new ServiceServer<T, MReq, MRes>();
        }

        public ServiceClient<MReq, MRes> serviceClient<MReq,MRes>(string service_name, bool persistent = false, IDictionary header_values = null)
        {
            return serviceClient<MReq, MRes>(new ServiceClientOptions(service_name, persistent, header_values));
        }

        public ServiceClient<MReq, MRes> serviceClient<MReq,MRes>(ServiceClientOptions ops)
        {
            ops.service = resolveName(ops.service);
            ServiceClient<MReq, MRes> client = new ServiceClient<MReq, MRes>(ops.service, ops.persistent, ops.header_values, ops.md5sum);
            if (client != null)
            {
                lock (collection.mutex)
                {
                    collection.serviceclients.Add(client);
                }
            }
            return client;
        }

        public void construct(string ns, bool validate_name)
        {
            if (!ROS.initialized)
                ROS.FREAKTHEFUCKOUT();
            collection = new NodeHandleBackingCollection();
            UnresolvedNamespace = ns;
            if (validate_name)
                Namespace = resolveName(ns);
            else
                Namespace = resolveName(ns, true, true);

            ok = true;
            lock (nh_refcount_mutex)
            {
                if (nh_refcount == 0 && !ROS.isStarted())
                {
                    node_started_by_nh = true;
                    ROS.start();
                }
                ++nh_refcount;
            }
        }

        public void destruct()
        {
            collection.Dispose();
            collection = null;
            lock(nh_refcount_mutex)
            {
                --nh_refcount;
            }
            if (nh_refcount == 0 && node_started_by_nh)
                ROS.shutdown();
        }

        public void initRemappings(IDictionary rms)
        {
            foreach(object k in rms.Keys)
            {
                string key = (string)k;
                string value = (string)rms[k];
                remappings[resolveName(key, false)] = resolveName(value, false);
                unresolved_remappings[key] = value;
            }
        }

        public string remapName(string name)
        {
            string resolved = resolveName(name, false);
            if (remappings.Contains(resolved))
                return (string)remappings[resolved];
            return names.remap(resolved);
        }

        public string resolveName(string name, bool remap=true)
        {
            string error="";
            if (!names.validate(name, ref error))
                names.InvalidName(error);
            return resolveName(name, remap, no_validate);
        }

        public string resolveName(string name, bool remap, bool novalidate)
        {
            if (name == "") return Namespace;
            string final = name;
            if (final[0] == '~')
                names.InvalidName("THERE'S A ~ IN THAT!");
            else if (final[0] != '/' && Namespace != "")
            {
                final = names.append(Namespace, final);
            }
            final = names.clean(final);
            if (remap)
            {
                final = remapName(final);
            }
            return names.resolve(final, false);
        }

        public Timer createTimer(TimeSpan period, TimerCallback tcb, bool oneshot)
        {
            return new Timer(tcb, null, 0, (int)Math.Floor(period.TotalMilliseconds));
        }
    }
}
