#region USINGZ

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Messages;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace Ros_CSharp
{
    public class NodeHandle : IDisposable
    {
        public string Namespace = "", UnresolvedNamespace = "";
        private CallbackQueue _callback;
        private bool _ok = true;
        public NodeHandleBackingCollection collection = new NodeHandleBackingCollection();
        public int nh_refcount;
        public object nh_refcount_mutex = new object();
        public bool no_validate;
        public bool node_started_by_nh;
        public IDictionary remappings = new Hashtable(), unresolved_remappings = new Hashtable();

        public NodeHandle(string ns, IDictionary remappings)
        {
            if (ns != "" && ns[0] == '~')
                ns = names.resolve(ns);
            construct(ns, true);
            initRemappings(remappings);
        }

        public NodeHandle(NodeHandle rhs)
        {
            Callback = rhs.Callback;
            remappings = new Hashtable(rhs.remappings);
            unresolved_remappings = new Hashtable(rhs.unresolved_remappings);
            construct(rhs.Namespace, true);
            UnresolvedNamespace = rhs.UnresolvedNamespace;
        }

        public NodeHandle(NodeHandle parent, string ns)
        {
            Namespace = parent.Namespace;
            Callback = parent.Callback;
            remappings = new Hashtable(parent.remappings);
            unresolved_remappings = new Hashtable(parent.unresolved_remappings);
            construct(ns, false);
        }

        public NodeHandle(NodeHandle parent, string ns, IDictionary remappings)
        {
            Namespace = parent.Namespace;
            Callback = parent.Callback;
            this.remappings = new Hashtable(remappings);
            construct(ns, false);
        }

        private static NodeHandle waitplzkthx()
        {
            while (ROS.GlobalNodeHandle == null)
            {
                Console.WriteLine("GLOBALNODEHANDLE IS NULL ZOMG!");
                Thread.Sleep(100);
            }
            return ROS.GlobalNodeHandle;
        }

        public NodeHandle() : this(waitplzkthx())
        {
        }

        public CallbackQueue Callback
        {
            get
            {
                if (_callback == null) return ROS.GlobalCallbackQueue;
                return _callback;
            }
            set { _callback = value; }
        }

        public bool ok
        {
            get { return ROS.ok && _ok; }
            set { _ok = value; }
        }

        #region IDisposable Members

        public void Dispose()
        {
            destruct();
        }

        #endregion

        ~NodeHandle()
        {
            Dispose();
        }

        public void shutdown()
        {
            foreach (ISubscriber sub in collection.subscribers)
                sub.unsubscribe();
            foreach (IPublisher pub in collection.publishers)
                pub.unadvertise();
            foreach (IServiceClient client in collection.serviceclients)
                client.impl.shutdown();
            foreach (IServiceServer srv in collection.serviceservers)
                srv.impl.unadvertise();
        }

        public Publisher<M> advertise<M>(string topic, int q_size, bool l = false) where M : class, new()
        {
            return advertise(new AdvertiseOptions<M>(topic, q_size) {latch = l});
        }

        public Publisher<M> advertise<M>(string topic, int queue_size, SubscriberStatusCallback connectcallback,
                                         SubscriberStatusCallback disconnectcallback, bool l = false)
            where M : class, new()
        {
            return advertise(new AdvertiseOptions<M>(topic, queue_size, connectcallback, disconnectcallback) {latch = l});
        }

        public Publisher<M> advertise<M>(AdvertiseOptions<M> ops) where M : class, new()
        {
            ops.topic = resolveName(ops.topic);
            if (ops.callback_queue == null)
            {
                if (Callback != null)
                    ops.callback_queue = Callback;
                else
                    ops.callback_queue = ROS.GlobalCallbackQueue;
            }
            SubscriberCallbacks callbacks = new SubscriberCallbacks(ops.connectCB, ops.disconnectCB, ops.callback_queue);
            if (TopicManager.Instance.advertise(ops, callbacks))
            {
                Publisher<M> pub = new Publisher<M>(ops.topic, ops.md5sum, ops.datatype, this, callbacks);
                lock (collection.mutex)
                {
                    collection.publishers.Add(pub);
                }
                return pub;
            }
            return null;
        }

        public Subscriber<TypedMessage<M>> subscribe<M>(string topic, int queue_size,
                                                        CallbackDelegate<TypedMessage<M>> cb) where M : class, new()
        {
            return subscribe<M>(topic, queue_size, new Callback<TypedMessage<M>>(cb));
        }

        public Subscriber<TypedMessage<M>> subscribe<M>(string topic, int queue_size, CallbackInterface cb)
            where M : class, new()
        {
            if (_callback == null)
            {
                _callback = ROS.GlobalCallbackQueue;
            }
            SubscribeOptions<TypedMessage<M>> ops = new SubscribeOptions<TypedMessage<M>>(topic, queue_size,
                                                                                          cb.func
                )
                                                        {callback_queue = _callback};
            ops.callback_queue.addCallback(cb);
            return subscribe(ops);
        }

        public Subscriber<TypedMessage<M>> subscribe<M>(SubscribeOptions<TypedMessage<M>> ops) where M : class, new()
        {
            ops.topic = resolveName(ops.topic);
            if (ops.callback_queue == null)
            {
                if (Callback != null)
                    ops.callback_queue = Callback;
                else
                    ops.callback_queue = ROS.GlobalCallbackQueue;
            }
            if (TopicManager.Instance.subscribe(ops))
            {
                Subscriber<TypedMessage<M>> sub = new Subscriber<TypedMessage<M>>(ops.topic, this, ops.helper);
                lock (collection.mutex)
                {
                    collection.subscribers.Add(sub);
                }
                return sub;
            }
            return new Subscriber<TypedMessage<M>>();
        }

        public ServiceServer<T, MReq, MRes> advertiseService<T, MReq, MRes>(string service, Func<MReq, MRes> srv_func)
        {
            return advertiseService<T, MReq, MRes>(new AdvertiseServiceOptions<MReq, MRes>(service, srv_func));
        }

        public ServiceServer<T, MReq, MRes> advertiseService<T, MReq, MRes>(AdvertiseServiceOptions<MReq, MRes> ops)
        {
            ops.service = resolveName(ops.service);
            if (ops.callback_queue == null)
            {
                if (Callback == null)
                    ops.callback_queue = ROS.GlobalCallbackQueue;
                else
                    ops.callback_queue = Callback;
            }
            if (ServiceManager.Instance.advertiseService(ops))
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

        public ServiceClient<MReq, MRes> serviceClient<MReq, MRes>(string service_name, bool persistent = false,
                                                                   IDictionary header_values = null)
        {
            return serviceClient<MReq, MRes>(new ServiceClientOptions(service_name, persistent, header_values));
        }

        public ServiceClient<MReq, MRes> serviceClient<MReq, MRes>(ServiceClientOptions ops)
        {
            ops.service = resolveName(ops.service);
            ServiceClient<MReq, MRes> client = new ServiceClient<MReq, MRes>(ops.service, ops.persistent,
                                                                             ops.header_values, ops.md5sum);
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
            lock (nh_refcount_mutex)
            {
                --nh_refcount;
            }
            if (nh_refcount == 0 && node_started_by_nh)
                ROS.shutdown();
        }

        public void initRemappings(IDictionary rms)
        {
            foreach (object k in rms.Keys)
            {
                string key = (string) k;
                string value = (string) rms[k];
                remappings[resolveName(key, false)] = resolveName(value, false);
                unresolved_remappings[key] = value;
            }
        }

        public string remapName(string name)
        {
            string resolved = resolveName(name, false);
            if (resolved == null)
                resolved = "";
            else if (remappings.Contains(resolved))
                return (string) remappings[resolved];
            return names.remap(resolved);
        }

        public string resolveName(string name, bool remap = true)
        {
            string error = "";
            if (!names.validate(name, ref error))
                names.InvalidName(error);
            return resolveName(name, remap, no_validate);
        }

        public string resolveName(string name, bool remap, bool novalidate)
        {
            //EDB.WriteLine("resolveName(" + name + ")");
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
            return new Timer(tcb, null, 0, (int) Math.Floor(period.TotalMilliseconds));
        }

        #region Nested type: NodeHandleBackingCollection

        public class NodeHandleBackingCollection : IDisposable
        {
            public object mutex = new object();
            public List<IPublisher> publishers = new List<IPublisher>();
            public List<IServiceClient> serviceclients = new List<IServiceClient>();
            public List<IServiceServer> serviceservers = new List<IServiceServer>();
            public List<ISubscriber> subscribers = new List<ISubscriber>();

            #region IDisposable Members

            public void Dispose()
            {
                publishers.Clear();
                serviceservers.Clear();
                subscribers.Clear();
                serviceclients.Clear();
            }

            #endregion
        }

        #endregion
    }
}