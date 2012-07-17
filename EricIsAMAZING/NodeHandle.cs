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

         public Publisher<M> advertise<M>(string topic, int q_size) where M : IRosMessage, new()
        {
            return advertise<M>(topic, q_size, false);
        }

        public Publisher<M> advertise<M>(string topic, int q_size, bool l) where M : IRosMessage, new()
        {
            return advertise(new AdvertiseOptions<M>(topic, q_size) {latch = l});
        }

        public Publisher<M> advertise<M>(string topic, int queue_size, SubscriberStatusCallback connectcallback,
                                         SubscriberStatusCallback disconnectcallback, bool l)
            where M : IRosMessage, new()
        {
            return advertise(new AdvertiseOptions<M>(topic, queue_size, connectcallback, disconnectcallback) {latch = l});
        }

        public Publisher<M> advertise<M>(AdvertiseOptions<M> ops) where M : IRosMessage, new()
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

        public Subscriber<M> subscribe<M>(string topic, int queue_size,CallbackDelegate<M> cb) where M : IRosMessage, new()
        {
            return subscribe<M>(topic, queue_size, new Callback<M>(cb), new M().MD5Sum);
        }

        public Subscriber<M> subscribe<M>(string topic, int queue_size,
                                                        CallbackDelegate<M> cb, string thisisveryverybad ) where M : IRosMessage, new()
        {
            return subscribe<M>(topic, queue_size, new Callback<M>(cb), thisisveryverybad);

        }
         
         public Subscriber<M> subscribe<M>(string topic, int queue_size, CallbackInterface cb)
            where M : IRosMessage, new()
        {
              return subscribe<M>( topic, queue_size, cb, null);
       }


        public Subscriber<M> subscribe<M>(string topic, int queue_size, CallbackInterface cb, string thisisveryverybad)
            where M : IRosMessage, new()
        {
            if (_callback == null)
            {
                _callback = ROS.GlobalCallbackQueue;
            }
            SubscribeOptions<M> ops = new SubscribeOptions<M>(topic, queue_size,
                                                                                          cb.func, thisisveryverybad
                )
                                                        {callback_queue = _callback};
            ops.callback_queue.addCallback(cb);
            return subscribe(ops);
        }

        public Subscriber<M> subscribe<M>(SubscribeOptions<M> ops) where M : IRosMessage, new()
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
                Subscriber<M> sub = new Subscriber<M>(ops.topic, this, ops.helper);
                lock (collection.mutex)
                {
                    collection.subscribers.Add(sub);
                }
                return sub;
            }
            return new Subscriber<M>();
        }

        public ServiceServer<T, MReq, MRes> advertiseService<T, MReq, MRes>(string service, ServiceFunction<MReq, MRes> srv_func)
            where MReq : IRosMessage, new()
            where MRes : IRosMessage, new()
        {
            return advertiseService<T, MReq, MRes>(new AdvertiseServiceOptions<MReq, MRes>(service, srv_func));
        }

        public ServiceServer<T, MReq, MRes> advertiseService<T, MReq, MRes>(AdvertiseServiceOptions<MReq, MRes> ops)
            where MReq : IRosMessage, new()
            where MRes : IRosMessage, new()
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

        public ServiceClient<MReq, MRes> serviceClient<MReq, MRes>(string service_name)
            where MReq : IRosMessage, new()
            where MRes : IRosMessage, new()
        {
            return serviceClient<MReq, MRes>(new ServiceClientOptions(service_name, false, null));
        }

        public ServiceClient<MReq, MRes> serviceClient<MReq, MRes>(string service_name,bool persistent)
            where MReq : IRosMessage, new()
            where MRes : IRosMessage, new()
        {
            return serviceClient<MReq, MRes>(new ServiceClientOptions(service_name, persistent, null));
        }
        public ServiceClient<MReq, MRes> serviceClient<MReq, MRes>(string service_name, bool persistent,
                                                                   IDictionary header_values)
            where MReq : IRosMessage, new()
            where MRes : IRosMessage, new()
        {
            return serviceClient<MReq, MRes>(new ServiceClientOptions(service_name, persistent, header_values));
        }

        public ServiceClient<MReq, MRes> serviceClient<MReq, MRes>(ServiceClientOptions ops)
            where MReq : IRosMessage, new()
            where MRes : IRosMessage, new()
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
            
            foreach (object k in remappings.Keys)
            {
                string left = (string)k;
                string right = (string)remappings[k];
                if (left != "" && left[0] != '_')
                {
                    string resolved_left = resolveName(left, false);
                    string resolved_right = resolveName(right, false);
                    remappings[resolved_left] = resolved_right;
                    unresolved_remappings[left] = right;
                }
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
        public string resolveName(string name)
        {
           return resolveName(name, true);
        }


        public string resolveName(string name, bool remap )
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