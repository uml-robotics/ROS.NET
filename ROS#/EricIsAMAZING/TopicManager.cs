#region USINGZ

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Messages;
using XmlRpc_Wrapper;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace EricIsAMAZING
{
    public class TopicManager
    {
        #region Delegates

        public delegate byte[] SerializeFunc();

        #endregion

        private static TopicManager _instance;

        private List<Publication> advertised_topics = new List<Publication>();
        private object advertised_topics_mutex = new object();
        private List<string> advertised_topics_names = new List<string>();
        private object advertised_topics_names_mutex = new object();
        private ConnectionManager connection_manager = ConnectionManager.Instance;
        private PollManager poll_manager = PollManager.Instance;
        private bool shutting_down;
        private object shutting_down_mutex = new object();
        private object subs_mutex = new object();
        private List<Subscription> subscriptions = new List<Subscription>();
        private XmlRpcManager xmlrpc_manager = XmlRpcManager.Instance;

        public static TopicManager Instance
        {
            get
            {
                if (_instance == null) _instance = new TopicManager();
                return _instance;
            }
        }

        public void Start()
        {
            //Console.WriteLine("STARTING TOPICS MANAGER");
            lock (shutting_down_mutex)
            {
                shutting_down = false;

                poll_manager = PollManager.Instance;
                connection_manager = ConnectionManager.Instance;
                xmlrpc_manager = XmlRpcManager.Instance;

                xmlrpc_manager.bind("publisherUpdate", pubUpdateCallback);
                xmlrpc_manager.bind("requestTopic", requestTopicCallback);
                xmlrpc_manager.bind("getBusStats", getBusStatusCallback);
                xmlrpc_manager.bind("getBusInfo", getBusInfoCallback);
                xmlrpc_manager.bind("getSubscriptions", getSubscriptionsCallback);
                xmlrpc_manager.bind("getPublications", getPublicationsCallback);

                poll_manager.addPollThreadListener(processPublishQueues);
            }
        }

        public void shutdown()
        {
            lock (shutting_down_mutex)
            {
                if (shutting_down) return;
                lock (advertised_topics_mutex)
                {
                    lock (subs_mutex)
                    {
                        shutting_down = true;
                    }
                }
                xmlrpc_manager.unbind("publisherUpdate");
                xmlrpc_manager.unbind("requestTopic");
                xmlrpc_manager.unbind("getBusStats");
                xmlrpc_manager.unbind("getBusInfo");
                xmlrpc_manager.unbind("getSubscriptions");
                xmlrpc_manager.unbind("getPublications");

                Console.WriteLine("shutting down topics...");

                Console.WriteLine("\tshutting down publishers");
                lock (advertised_topics_mutex)
                {
                    foreach (Publication p in advertised_topics)
                    {
                        if (!p.Dropped)
                            unregisterPublisher(p.Name);
                        p.drop();
                    }
                    advertised_topics.Clear();
                }
                Console.WriteLine("\tshutting down subscribers");
                lock (subs_mutex)
                {
                    foreach (Subscription s in subscriptions)
                    {
                        unregisterSubscriber(s.name);
                        s.shutdown();
                    }
                    subscriptions.Clear();
                }
            }
        }

        public void getAdvertisedTopics(ref List<string> topics)
        {
            lock (advertised_topics_names_mutex)
            {
                topics = new List<string>(advertised_topics_names);
            }
        }

        public void getSubscribedTopics(ref List<string> topics)
        {
            lock (subs_mutex)
            {
                topics.Clear();
                topics.AddRange(subscriptions.Select(s => s.name));
            }
        }

        public Publication lookupPublication(string topic)
        {
            lock (advertised_topics_mutex)
            {
                return lookupPublicationWithoutLock(topic);
            }
        }

        public bool advertise<T>(AdvertiseOptions<T> ops, SubscriberCallbacks callbacks) where T : struct
        {
            if (ops.datatype == "*")
                throw new Exception("Advertising with * as the datatype is not allowed.  Topic [" + ops.topic + "]");
            if (ops.md5sum == "*")
                throw new Exception("Advertising with * as the md5sum is not allowed.  Topic [" + ops.topic + "]");
            if (ops.md5sum == "")
                throw new Exception("Advertising on topic [" + ops.topic + "] with an empty md5sum");
            if (ops.datatype == "")
                throw new Exception("Advertising on topic [" + ops.topic + "] with an empty datatype");
            if (ops.message_definition == "")
                Console.WriteLine
                    ("Danger, Will Robinson... Advertising on topic [" + ops.topic + "] with an empty message definition. Some tools (that don't exist in this implementation) may not work correctly");
            Publication pub = null;
            lock (advertised_topics_mutex)
            {
                if (shutting_down)
                    return false;
                pub = lookupPublicationWithoutLock(ops.topic);
                if (pub != null)
                {
                    if (pub.Md5sum != ops.md5sum)
                    {
                        Console.WriteLine
                            ("Tried to advertise on topic [{0}] with md5sum [{1}] and datatype [{2}], but the topic is already advertised as md5sum [{3}] and datatype [{4}]", ops.topic, ops.md5sum,
                             ops.datatype, pub.Md5sum, pub.DataType);
                        return false;
                    }
                }
                else
                    pub = new Publication(ops.topic, ops.datatype, ops.md5sum, ops.message_definition, ops.queue_size, ops.latch, ops.has_header);
                pub.addCallbacks(callbacks);
                advertised_topics.Add(pub);
            }

            lock (advertised_topics_names_mutex)
            {
                advertised_topics_names.Add(ops.topic);
            }

            bool found = false;
            Subscription sub = null;
            lock (subs_mutex)
            {
                foreach (Subscription s in subscriptions)
                {
                    if (s.name == ops.topic && md5sumsMatch(s.md5sum, ops.md5sum) && !s.IsDropped)
                    {
                        found = true;
                        sub = s;
                        break;
                    }
                }
            }

            if (found)
                sub.addLocalConnection(pub);

            XmlRpcValue args = new XmlRpcValue(this_node.Name, ops.topic, ops.datatype, xmlrpc_manager.uri), result = new XmlRpcValue(), payload = new XmlRpcValue();
            master.execute("registerPublisher", args, ref result, ref payload, true);
            return true;
        }

        public bool subscribe<T>(SubscribeOptions<TypedMessage<T>> ops) where T : struct
        {
            lock (subs_mutex)
            {
                if (addSubCallback(ops))
                    return true;
                if (shutting_down)
                    return false;
                if (ops.md5sum == "")
                    throw subscribeFail(ops, "with an empty md5sum");
                if (ops.datatype == "")
                    throw subscribeFail(ops, "with an empty datatype");
                if (ops.helper == null)
                    throw subscribeFail(ops, "without a callback");
                string md5sum = ops.md5sum;
                string datatype = ops.datatype;
                Subscription s = new Subscription(ops.topic, md5sum, datatype);
                s.addCallback(ops.helper, ops.md5sum, ops.Callback, ops.queue_size, ops.allow_concurrent_callbacks);
                if (!registerSubscriber(s, ops.datatype))
                {
                    Console.WriteLine("Couldn't register subscriber on topic [{0}]", ops.topic);
                    s.shutdown();
                    return false;
                }
                subscriptions.Add(s);
                return true;
            }
        }

        public Exception subscribeFail<T>(SubscribeOptions<TypedMessage<T>> ops, string reason) where T : struct
        {
            return new Exception("Subscribing to topic [" + ops.topic + "] " + reason);
        }

        public bool unsubscribe(string topic, ISubscriptionCallbackHelper sbch)
        {
            Subscription sub = null;
            lock (subs_mutex)
            {
                if (shutting_down) return false;
                foreach (Subscription s in subscriptions)
                {
                    if (s.name == topic)
                    {
                        sub = s;
                        break;
                    }
                }
            }
            if (sub == null) return false;
            sub.removeCallback(sbch);
            if (sub.NumCallbacks == 0)
            {
                lock (subs_mutex)
                {
                    subscriptions.Remove(sub);

                    if (!unregisterSubscriber(topic))
                        Console.WriteLine("Couldn't unregister subscriber for topic [" + topic + "]");
                }

                sub.shutdown();
                return true;
            }
            return true;
        }

        public int getNumPublishers(string topic)
        {
            lock (subs_mutex)
            {
                if (shutting_down) return 0;

                foreach (Subscription t in subscriptions)
                {
                    if (!t.IsDropped && t.name == topic)
                        return t.NumPublishers;
                }
            }
            return 0;
        }

        public int getNumSubscribers(string topic)
        {
            lock (advertised_topics_mutex)
            {
                if (shutting_down) return 0;
                Publication p = lookupPublicationWithoutLock(topic);
                if (p != null)
                    return p.NumSubscribers;
                return 0;
            }
        }

        public int getNumSubscriptions()
        {
            lock (subs_mutex)
            {
                return subscriptions.Count;
            }
        }

        public void publish<M>(string topic, M message) where M : IRosMessage
        {
            publish(topic, message.Serialize, message);
        }

        public void publish(string topic, SerializeFunc serfunc, IRosMessage msg)
        {
            if (msg == null) return;
            lock (advertised_topics_mutex)
            {
                if (shutting_down) return;

                Publication p = lookupPublicationWithoutLock(topic);
                if (p == null) return;
                if (p.HasSubscribers || p.Latch)
                {
                    bool nocopy = false;
                    bool serialize = false;
                    if (msg != null && msg.type != MsgTypes.Unknown)
                    {
                        Console.WriteLine("This line is sketchy... TopicManager.cs:254-ish... publish(string, byte, msg)");
                        p.getPublishTypes(ref serialize, ref nocopy, ref msg.type);
                    }
                    else
                        serialize = true;
                    if (!nocopy)
                    {
                        //Console.WriteLine("This line is also sketchy... TopicManager.cs:262-ish... publish(string, byte, msg)");
                        msg.type = MsgTypes.Unknown;
                    }
                    if (serialize)
                    {
                        msg.Serialized = serfunc();
                    }

                    p.publish(msg);

                    //if (serialize)
                    //    Console.WriteLine("Signal your mom's pollset!");
                }
                else
                    p.incrementSequence();
            }
        }

        public void incrementSequence(string topic)
        {
            Publication pub = lookupPublication(topic);
            if (pub != null)
                pub.incrementSequence();
        }

        public bool isLatched(string topic)
        {
            Publication pub = lookupPublication(topic);
            if (pub != null) return pub.Latch;
            return false;
        }

        public bool md5sumsMatch(string lhs, string rhs)
        {
            return (lhs == "*" || rhs == "*" || lhs == rhs);
        }

        public bool addSubCallback<M>(SubscribeOptions<M> ops) where M : IRosMessage, new()
        {
            bool found = false;
            bool found_topic = false;
            Subscription sub = null;
            if (shutting_down) return false;
            foreach (Subscription s in subscriptions)
            {
                sub = s;
                if (!sub.IsDropped && sub.name == ops.topic)
                {
                    found_topic = true;
                    if (md5sumsMatch(ops.md5sum, sub.md5sum))
                        found = true;
                    break;
                }
            }
            if (found_topic && !found)
                throw new Exception
                    ("Tried to subscribe to a topic with the same name but different md5sum as a topic that was already subscribed [" + ops.datatype + "/" + ops.md5sum + " vs. " + sub.datatype + "/" +
                     sub.md5sum + "]");
            else if (found)
                if (!sub.addCallback(ops.helper, ops.md5sum, ops.Callback, ops.queue_size, ops.allow_concurrent_callbacks))
                    return false;
            return found;
        }

        public bool requestTopic(string topic, XmlRpcValue protos, ref XmlRpcValue ret)
        {
            protos.Dump();
            for (int proto_idx = 0; proto_idx < protos.Size; proto_idx++)
            {
                XmlRpcValue proto = protos[proto_idx];
                if (proto.Type != TypeEnum.TypeArray)
                {
                    Console.WriteLine("requestTopic protocol list was not a list of lists");
                    return false;
                }
                if (proto[0].Type != TypeEnum.TypeString)
                {
                    Console.WriteLine("requestTopic received a protocol list in which a sublist did not start with a string");
                    return false;
                }

                string proto_name = proto[0].Get<string>();

                if (proto_name == "TCPROS")
                {
                    XmlRpcValue tcp_ros_params = new XmlRpcValue("TCPROS", network.host, connection_manager.TCPPort);
                    ret.Set(0, 1);
                    ret.Set(1, "");
                    ret.Set(2, tcp_ros_params);
                    return true;
                }
                else if (proto_name == "UDPROS")
                {
                    Console.WriteLine("IGNORING UDP GIZNARBAGE");
                }
                else
                    Console.WriteLine("an unsupported protocol was offered: [{0}]", proto_name);
            }
            Console.WriteLine("The caller to requestTopic has NO IDEA WHAT'S GOING ON!");
            return false;
        }

        public bool isTopicAdvertised(string topic)
        {
            return advertised_topics.Count((o) => o.Name == topic) > 0;
        }

        public bool registerSubscriber(Subscription s, string datatype)
        {
            string fuckinguriyo = xmlrpc_manager.uri;

            XmlRpcValue args = new XmlRpcValue(this_node.Name, s.name, datatype, fuckinguriyo);
            XmlRpcValue result = new XmlRpcValue();
            XmlRpcValue payload = new XmlRpcValue();
            if (!master.execute("registerSubscriber", args, ref result, ref payload, true))
                return false;
            List<string> pub_uris = new List<string>();
            for (int i = 0; i < payload.Size; i++)
            {
                XmlRpcValue asshole = payload[i];
                string pubed = asshole.Get<string>();
                if (pubed != fuckinguriyo && !pub_uris.Contains(pubed))
                {
                    pub_uris.Add(pubed);
                }
            }
            bool self_subscribed = false;
            Publication pub = null;
            string sub_md5sum = s.md5sum;
            lock (advertised_topics_mutex)
            {
                foreach (Publication p in advertised_topics)
                {
                    pub = p;
                    string pub_md5sum = pub.Md5sum;
                    if (pub.Name == s.name && md5sumsMatch(pub_md5sum, sub_md5sum) && !pub.Dropped)
                    {
                        self_subscribed = true;
                        break;
                    }
                }
            }

            s.pubUpdate(pub_uris);
            if (self_subscribed)
                s.addLocalConnection(pub);
            return true;
        }

        public bool unregisterSubscriber(string topic)
        {
            XmlRpcValue args = new XmlRpcValue(this_node.Name, topic, xmlrpc_manager.uri), result = new XmlRpcValue(), payload = new XmlRpcValue();
            master.execute("unregisterSubscriber", args, ref result, ref payload, false);
            return true;
        }

        public bool unregisterPublisher(string topic)
        {
            XmlRpcValue args = new XmlRpcValue(this_node.Name, topic, xmlrpc_manager.uri), result = new XmlRpcValue(), payload = new XmlRpcValue();
            master.execute("unregisterPublisher", args, ref result, ref payload, false);
            return true;
        }

        public Publication lookupPublicationWithoutLock(string topic)
        {
            Publication t = null;
            foreach (Publication p in advertised_topics)
            {
                if (p.Name == topic && !p.Dropped)
                {
                    t = p;
                    break;
                }
            }
            return t;
        }

        public void processPublishQueues()
        {
            lock (advertised_topics_mutex)
            {
                foreach (Publication pub in advertised_topics)
                {
                    pub.processPublishQueue();
                }
            }
        }

        public void getBusStats(ref XmlRpcValue stats)
        {
            XmlRpcValue publish_stats = new XmlRpcValue(), subscribe_stats = new XmlRpcValue(), service_stats = new XmlRpcValue();
            publish_stats.Size = 0;
            subscribe_stats.Size = 0;
            service_stats.Size = 0;
            int pidx = 0;
            lock (advertised_topics_mutex)
            {
                foreach (Publication t in advertised_topics)
                {
                    publish_stats.Set(pidx++, t.GetStats());
                }
            }
            int sidx = 0;
            lock (subs_mutex)
            {
                foreach (Subscription t in subscriptions)
                {
                    subscribe_stats.Set(sidx++, t.getStats());
                }
            }
            stats.Set(0, publish_stats);
            stats.Set(1, subscribe_stats);
            stats.Set(2, service_stats);
        }

        public void getBusInfo(IntPtr i)
        {
            XmlRpcValue info = XmlRpcValue.LookUp(i);
            //Console.WriteLine("was " + info.Type);
            //info.Type = TypeEnum.TypeArray;
            //Console.WriteLine("now is " + info.Type);
            info.Size = 0;
            lock (advertised_topics_mutex)
            {
                foreach (Publication t in advertised_topics)
                {
                    //Console.WriteLine("ADDING PUB: " + t.Name + " to BusInfo");
                    t.getInfo(info);
                }
            }
            lock (subs_mutex)
            {
                foreach (Subscription t in subscriptions)
                {
                    //Console.WriteLine("ADDING SUB: " + t.name + " w/ "+t.pending_connections.Count+" pending connections to BusInfo");
                    t.getInfo(info);
                }
            }
        }

        public void getSubscriptions(ref XmlRpcValue subs)
        {
            subs.Size = 0;
            lock (subs_mutex)
            {
                int sidx = 0;
                foreach (Subscription t in subscriptions)
                {
                    subs.Set(sidx++, new XmlRpcValue(t.name, t.datatype));
                }
            }
        }

        public void getPublications(ref XmlRpcValue pubs)
        {
            pubs.Size = 0;
            lock (advertised_topics_mutex)
            {
                int sidx = 0;
                foreach (Publication t in advertised_topics)
                {
                    XmlRpcValue pub = new XmlRpcValue();
                    pub.Set(0, t.Name);
                    pub.Set(1, t.DataType);
                    pubs.Set(sidx++, pub);
                }
            }
        }

        public bool pubUpdate(string topic, List<string> pubs)
        {
            Console.WriteLine("TopicManager is updating publishers for " + topic);
            Subscription sub = null;
            lock (subs_mutex)
            {
                if (shutting_down) return false;
                foreach (Subscription s in subscriptions)
                {
                    if (s.name != topic || s.IsDropped)
                        continue;
                    sub = s;
                    break;
                }
            }
            if (sub != null)
                return sub.pubUpdate(pubs);
            else
                Console.WriteLine("got a request for updating publishers of topic " + topic + ", but I don't have any subscribers to that topic.");
            return false;
        }

        public void pubUpdateCallback([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        {
            XmlRpcValue res = XmlRpcValue.Create(ref result), parm = XmlRpcValue.Create(ref parms);
            result = res.instance;
            List<string> pubs = new List<string>();
            for (int idx = 0; idx < parm[2].Size; idx++)
                pubs.Add(parm[2][idx].Get<string>());
            if (pubUpdate(parm[1].Get<string>(), pubs))
                XmlRpcManager.Instance.responseInt(1, "", 0)(result);
            else
            {
                Console.WriteLine("Unknown Error or some shit");
                XmlRpcManager.Instance.responseInt(0, "Unknown Error or some shit", 0)(result);
            }
        }

        public void requestTopicCallback([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        {
            Console.WriteLine("REQUEST TOPIC CALLBACK!");
            XmlRpcValue res = XmlRpcValue.Create(ref result), parm = XmlRpcValue.Create(ref parms);
            result = res.instance;
            if (!requestTopic(parm[1].Get<string>(), parm[2], ref res))
            {
                string last_error = "Unknown error or some shit";

                XmlRpcManager.Instance.responseInt(0, last_error, 0)(result);
            }
        }

        public void getBusStatusCallback([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        {
            XmlRpcValue res = XmlRpcValue.Create(ref result), parm = XmlRpcValue.Create(ref parms);
            result = res.instance;
            res.Set(0, 1);
            res.Set(1, "");
            XmlRpcValue response = new XmlRpcValue();
            getBusStats(ref response);
            res.Set(2, response);
        }

        public void getBusInfoCallback([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        {
            XmlRpcValue res = XmlRpcValue.Create(ref result);
            res.Set(0, 1);
            res.Set(1, "");
            XmlRpcValue response = new XmlRpcValue();
            IntPtr resp = response.instance;
            getBusInfo(resp);
            res.Set(2, response);
        }

        public void getSubscriptionsCallback([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        {
            XmlRpcValue res = XmlRpcValue.Create(ref result);
            res.Set(0, 1);
            res.Set(1, "subscriptions");
            XmlRpcValue response = new XmlRpcValue();
            getSubscriptions(ref response);
            res.Set(2, response);
        }

        public void getPublicationsCallback([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        {
            XmlRpcValue res = XmlRpcValue.Create(ref result), parm = XmlRpcValue.Create(ref parms);
            result = res.instance;
            res.Set(0, 1);
            res.Set(1, "publications");
            XmlRpcValue response = new XmlRpcValue();
            getPublications(ref response);
            res.Set(2, response);
        }

        public bool unadvertise(string topic, SubscriberCallbacks callbacks)
        {
            Publication pub = null;
            lock (advertised_topics_mutex)
            {
                if (shutting_down) return false;
                foreach (Publication p in advertised_topics)
                {
                    if (p.Name == topic && !p.Dropped)
                    {
                        pub = p;
                        break;
                    }
                }
            }
            if (pub == null)
                return false;
            pub.removeCallbacks(callbacks);
            lock (advertised_topics_mutex)
            {
                if (pub.NumCallbacks == 0)
                {
                    unregisterPublisher(pub.Name);
                    pub.drop();
                    advertised_topics.Remove(pub);
                    lock (advertised_topics_names_mutex)
                        advertised_topics_names.Remove(pub.Name);
                }
            }
            return true;
        }
    }
}