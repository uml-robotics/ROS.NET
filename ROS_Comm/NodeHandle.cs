// File: NodeHandle.cs
// Project: ROS_C-Sharp
// 
// ROS#
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 03/04/2013
// Updated: 07/26/2013

#region USINGZ

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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

        /// <summary>
        ///     Creates a new node
        /// </summary>
        /// <param name="ns">Namespace of node</param>
        /// <param name="remappings">any remappings</param>
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

        /// <summary>
        ///     Creates a new child node
        /// </summary>
        /// <param name="parent">Parent node to attach</param>
        /// <param name="ns">Namespace of new node</param>
        public NodeHandle(NodeHandle parent, string ns)
        {
            Console.WriteLine("NEW NODEHANDLE!");
            Namespace = parent.Namespace;
            Callback = parent.Callback;
            remappings = new Hashtable(parent.remappings);
            unresolved_remappings = new Hashtable(parent.unresolved_remappings);
            construct(ns, false);
        }

        /// <summary>
        ///     Creates a new child node with remappings
        /// </summary>
        /// <param name="parent">Parent node to attach</param>
        /// <param name="ns">Namespace of new node</param>
        /// <param name="remappings">Remappings</param>
        public NodeHandle(NodeHandle parent, string ns, IDictionary remappings)
        {
            Console.WriteLine("NEW NODEHANDLE!");
            Namespace = parent.Namespace;
            Callback = parent.Callback;
            this.remappings = new Hashtable(remappings);
            construct(ns, false);
        }

        /// <summary>
        ///     Creates a new node
        /// </summary>
        public NodeHandle() : this(this_node.Namespace, null)
        {
        }

        /// <summary>
        ///     Current callbacks in callback queue
        /// </summary>
        public CallbackQueue Callback
        {
            get
            {
                if (_callback == null)
                {
                    _callback = new CallbackQueue();
                    _callback.Enable();
                }

                return _callback;
            }
            set { _callback = value; }
        }

        /// <summary>
        ///     Management boolean, if ros is still running
        /// </summary>
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

        /// <summary>
        ///     Unregister every subscriber and publisher in this node
        /// </summary>
        public void shutdown()
        {
            lock (collection.mutex)
            {
                foreach (ISubscriber sub in collection.subscribers)
                    sub.unsubscribe();
                foreach (IPublisher pub in collection.publishers)
                    pub.unadvertise();

                foreach (IServiceClient client in collection.serviceclients)
                    client.shutdown();
                foreach (ServiceServer srv in collection.serviceservers)
                    srv.shutdown();
            }
        }

        /// <summary>
        ///     Creates a publisher
        /// </summary>
        /// <typeparam name="M">Type of topic</typeparam>
        /// <param name="topic">Name of topic</param>
        /// <param name="q_size">How many messages to qeueue if asynchrinous</param>
        /// <returns>A publisher with the specified topic type, name and options</returns>
        public Publisher<M> advertise<M>(string topic, int q_size) where M : IRosMessage, new()
        {
            return advertise<M>(topic, q_size, false);
        }

        /// <summary>
        ///     Creates a publisher, specify latching
        /// </summary>
        /// <typeparam name="M">Type of topic</typeparam>
        /// <param name="topic">Name of topic</param>
        /// <param name="q_size">How many messages to enqueue if asynchrinous</param>
        /// <param name="l">Boolean determines whether the given publisher will latch or not</param>
        /// <returns>A publisher with the specified topic type, name and options</returns>
        public Publisher<M> advertise<M>(string topic, int q_size, bool l) where M : IRosMessage, new()
        {
            return advertise(new AdvertiseOptions<M>(topic, q_size) {latch = l});
        }

        /// <summary>
        ///     Creates a publisher with connect and disconnect callbacks
        /// </summary>
        /// <typeparam name="M">Type of topic</typeparam>
        /// <param name="topic">Name of topic</param>
        /// <param name="queue_size">How many messages to enqueue if asynchrinous</param>
        /// <param name="connectcallback">Callback to fire when this node connects</param>
        /// <param name="disconnectcallback">Callback to fire when this node disconnects</param>
        /// <returns>A publisher with the specified topic type, name and options</returns>
        public Publisher<M> advertise<M>(string topic, int queue_size, SubscriberStatusCallback connectcallback,
            SubscriberStatusCallback disconnectcallback)
            where M : IRosMessage, new()
        {
            return advertise<M>(topic, queue_size, connectcallback, disconnectcallback, false);
        }

        /// <summary>
        ///     Creates a publisher with connect and disconnect callbacks, specify latching.
        /// </summary>
        /// <typeparam name="M">Type of topic</typeparam>
        /// <param name="topic">Name of topic</param>
        /// <param name="queue_size">How many messages to enqueue if asynchrinous</param>
        /// <param name="connectcallback">Callback to fire when this node connects</param>
        /// <param name="disconnectcallback">Callback to fire when this node disconnects</param>
        /// <param name="l">Boolean determines whether the given publisher will latch or not</param>
        /// <returns>A publisher with the specified topic type, name and options</returns>
        public Publisher<M> advertise<M>(string topic, int queue_size, SubscriberStatusCallback connectcallback,
            SubscriberStatusCallback disconnectcallback, bool l)
            where M : IRosMessage, new()
        {
            return advertise(new AdvertiseOptions<M>(topic, queue_size, connectcallback, disconnectcallback) {latch = l});
        }

        /// <summary>
        ///     Creates a publisher with the given advertise options
        /// </summary>
        /// <typeparam name="M">Type of topic</typeparam>
        /// <param name="ops">Advertise options</param>
        /// <returns>A publisher with the specified options</returns>
        public Publisher<M> advertise<M>(AdvertiseOptions<M> ops) where M : IRosMessage, new()
        {
            ops.topic = resolveName(ops.topic);
            if (ops.callback_queue == null)
            {
                ops.callback_queue = Callback ?? ROS.GlobalCallbackQueue;
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

        /// <summary>
        ///     Creates a subscriber with the given topic name.
        /// </summary>
        /// <typeparam name="M">Type of the subscriber message</typeparam>
        /// <param name="topic">Topic name</param>
        /// <param name="queue_size">How many messages to qeueue</param>
        /// <param name="cb">Callback to fire when a message is receieved</param>
        /// <returns>A subscriber</returns>
        public Subscriber<M> subscribe<M>(string topic, uint queue_size, CallbackDelegate<M> cb) where M : IRosMessage, new()
        {
            return subscribe<M>(topic, queue_size, new Callback<M>(cb), new M().MD5Sum);
        }

        /// <summary>
        ///     Creates a subscriber with the given topic name.
        /// </summary>
        /// <typeparam name="M">Topic type</typeparam>
        /// <param name="topic">Topic name</param>
        /// <param name="queue_size">How many messages to qeueue</param>
        /// <param name="cb">Function to fire when a message is recieved , delegate</param>
        /// <param name="thisisveryverybad">internal use</param>
        /// <returns></returns>
        public Subscriber<M> subscribe<M>(string topic, uint queue_size,
            CallbackDelegate<M> cb, string thisisveryverybad) where M : IRosMessage, new()
        {
            return subscribe<M>(topic, queue_size, new Callback<M>(cb), thisisveryverybad);
        }

        /// <summary>
        ///     Creates a subscriber
        /// </summary>
        /// <typeparam name="M">Topic type</typeparam>
        /// <param name="topic">Topic name</param>
        /// <param name="queue_size">How many messages to qeueue</param>
        /// <param name="cb">Function to fire when a message is recieved</param>
        /// <returns>A subscriber</returns>
        public Subscriber<M> subscribe<M>(string topic, uint queue_size, CallbackInterface cb)
            where M : IRosMessage, new()
        {
            return subscribe<M>(topic, queue_size, cb, null);
        }

        /// <summary>
        ///     Creates a subscriber
        /// </summary>
        /// <typeparam name="M">Topic type</typeparam>
        /// <param name="topic">Topic name</param>
        /// <param name="queue_size">How many messages to qeueue</param>
        /// <param name="cb">Function to fire when a message is recieved</param>
        /// <param name="thisisveryverybad">internal use</param>
        /// <returns>A subscriber</returns>
        public Subscriber<M> subscribe<M>(string topic, uint queue_size, CallbackInterface cb, string thisisveryverybad)
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

        /// <summary>
        ///     Creates a subscriber with given subscriber options
        /// </summary>
        /// <typeparam name="M">Topic type</typeparam>
        /// <param name="ops">Subscriber options</param>
        /// <returns>A subscriber</returns>
        public Subscriber<M> subscribe<M>(SubscribeOptions<M> ops) where M : IRosMessage, new()
        {
            ops.topic = resolveName(ops.topic);
            if (ops.callback_queue == null)
            {
                ops.callback_queue = Callback ?? ROS.GlobalCallbackQueue;
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


        public ServiceServer advertiseService<MReq, MRes>(string service, ServiceFunction<MReq, MRes> srv_func)
            where MReq : IRosMessage, new()
            where MRes : IRosMessage, new()
        {
            return advertiseService(new AdvertiseServiceOptions<MReq, MRes>(service, srv_func));
        }

        public ServiceServer advertiseService<MReq, MRes>(AdvertiseServiceOptions<MReq, MRes> ops)
            where MReq : IRosMessage, new()
            where MRes : IRosMessage, new()
        {
            ops.service = resolveName(ops.service);
            if (ops.callback_queue == null)
            {
                ops.callback_queue = Callback ?? ROS.GlobalCallbackQueue;
            }
            if (ServiceManager.Instance.advertiseService(ops))
            {
                ServiceServer srv = new ServiceServer(ops.service, this);
                lock (collection.mutex)
                {
                    collection.serviceservers.Add(srv);
                }
                return srv;
            }
            throw new Exception("Something ain't right with this advertisement.");
        }

        public ServiceClient<MReq, MRes> serviceClient<MReq, MRes>(string service_name)
            where MReq : IRosMessage, new()
            where MRes : IRosMessage, new()
        {
            return serviceClient<MReq, MRes>(new ServiceClientOptions(service_name, false, null));
        }

        public ServiceClient<MReq, MRes> serviceClient<MReq, MRes>(string service_name, bool persistent)
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
            ops.md5sum = new MReq().MD5Sum;
            return new ServiceClient<MReq, MRes>(ops.service, ops.persistent, ops.header_values, ops.md5sum);
        }

        public void construct(string ns, bool validate_name)
        {
            if (!ROS.initialized)
                ROS.FREAKOUT();
            collection = new NodeHandleBackingCollection();
            UnresolvedNamespace = ns;
            Namespace = validate_name ? resolveName(ns) : resolveName(ns, true, true);

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
            _callback = null;
            if (nh_refcount == 0 && node_started_by_nh)
                ROS.shutdown();
        }

        [DebuggerStepThrough]
        public void initRemappings(IDictionary rms)
        {
            foreach (object k in remappings.Keys)
            {
                string left = (string) k;
                string right = (string) remappings[k];
                if (left != "" && left[0] != '_')
                {
                    string resolved_left = resolveName(left, false);
                    string resolved_right = resolveName(right, false);
                    remappings[resolved_left] = resolved_right;
                    unresolved_remappings[left] = right;
                }
            }
        }

        [DebuggerStepThrough]
        public string remapName(string name)
        {
            string resolved = resolveName(name, false);
            if (resolved == null)
                resolved = "";
            else if (remappings.Contains(resolved))
                return (string) remappings[resolved];
            return names.remap(resolved);
        }

        [DebuggerStepThrough]
        public string resolveName(string name)
        {
            return resolveName(name, true);
        }

        [DebuggerStepThrough]
        public string resolveName(string name, bool remap)
        {
            string error = "";
            if (!names.validate(name, ref error))
                names.InvalidName(error);
            return resolveName(name, remap, no_validate);
        }

        [DebuggerStepThrough]
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
            public List<ServiceServer> serviceservers = new List<ServiceServer>();
            public List<ISubscriber> subscribers = new List<ISubscriber>();

            #region IDisposable Members

            public void Dispose()
            {
                publishers.Clear();
                subscribers.Clear();

                serviceservers.Clear();
                serviceclients.Clear();
            }

            #endregion
        }

        #endregion
    }
}