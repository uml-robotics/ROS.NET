using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Messages;
using XmlRpc_Wrapper;
using m = Messages;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace EricIsAMAZING
{
    public class TopicManager
    {
        private static TopicManager _instance;
        public static TopicManager Instance()
        {
            if (_instance == null) _instance = new TopicManager();
            return _instance;
        }

        private bool shutting_down;
        private object shutting_down_mutex = new object();
        private object subs_mutex = new object();
        private List<Subscription> subscriptions = new List<Subscription>();
        private object advertised_topics_mutex = new object();
        List<Publication> advertised_topics = new List<Publication>();
        List<string> advertised_topics_names = new List<string>();
        private object advertised_topics_names_mutex = new object();
        private PollManager poll_manager;
        private ConnectionManager connection_manager;
        private XmlRpcManager xmlrpc_manager;
        public void Start()
        {
            Console.WriteLine("STARTING TOPICS MANAGER");
            lock (shutting_down_mutex) 
            {
                shutting_down = false;

                poll_manager = PollManager.Instance();
                connection_manager = ConnectionManager.Instance();
                xmlrpc_manager = XmlRpcManager.Instance();

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
                        s.Shutdown();
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

        public bool advertise(AdvertiseOptions ops, SubscriberCallbacks callbacks)
        {
            throw new NotImplementedException();
        }

        public bool subscribe<T>(SubscribeOptions<T> ops)
        {
            throw new NotImplementedException();
        }

        public void unsubscribe(string topic, ISubscriptionCallbackHelper sbch)
        {
            throw new NotImplementedException();
        }

        public int getNumPublishers(string topic)
        {
            throw new NotImplementedException();
        }
        public int getNumSubscribers(string topic)
        {
            throw new NotImplementedException();
        }
        public int getNumSubscriptions()
        {
            throw new NotImplementedException();
        }
        public void publish<M>(string topic, M message) where M : IRosMessage
        {
            publish(topic, message.Serialize(), message);
        }

        public void publish(string topic, byte[] serializedmsg, IRosMessage msg)
        {
            throw new NotImplementedException();
        }

        public void incrementSequence(string topic)
        {
            throw new NotImplementedException();
        }
        public bool isLatched(string topic)
        {
            throw new NotImplementedException();
        }
        public bool md5sumsMatch(string lhs, string rhs)
        {
            return (lhs == "*" || rhs == "*" || lhs == rhs);
        }

        bool addSubCallback<M>(SubscribeOptions<M> ops) where M : m.IRosMessage
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
                throw new Exception("Tried to subscribe to a topic with the same name but different md5sum as a topic that was already subscribed [" + ops.datatype + "/" + ops.md5sum + " vs. " + sub.datatype + "/" + sub.md5sum + "]");

            if (!sub.addCallback<M>(ops.helper, ops.md5sum, ops.callbackQueue))
            {

            }
        }

        bool requestTopic(string topic, XmlRpcValue protos, out XmlRpcValue ret)
        {
            throw new NotImplementedException();
        }

        bool isTopicAdvertised(string topic)
        {
            throw new NotImplementedException();

        }

        bool registerSubscriber(Subscription s, string datatype)
        {
            throw new NotImplementedException();

        }

        bool unregisterSubscriber(string topic)
        {
            throw new NotImplementedException();

        }
        bool unregisterPublisher(string topic)
        {
            throw new NotImplementedException();

        }

        Publication lookupPublicationWithoutLock(string topic)
        {
            throw new NotImplementedException();

        }

        void processPublishQueues()
        {
            lock (advertised_topics_mutex)
            {
                foreach(Publication pub in advertised_topics)
                {
                    pub.processPublishQueue();
                }
            }
        }

        void getBusInfo(ref XmlRpcValue info)
        {
            throw new NotImplementedException();

        }

        void getSubscriptions(ref XmlRpcValue subscriptions)
        {
            throw new NotImplementedException();

        }

        void getPublications(ref XmlRpcValue publications)
        {
            throw new NotImplementedException();

        }

        bool pubUpdate(string topic, List<string> pubs)
        {
            throw new NotImplementedException();

        }

        void pubUpdateCallback(IntPtr parms, IntPtr result)
        {
            XmlRpcValue res = XmlRpcValue.Create(result), parm = XmlRpcValue.Create(parms);
            result = res.instance;
            throw new NotImplementedException();
        }

        void requestTopicCallback(IntPtr parms, IntPtr result)
        {
            XmlRpcValue res = XmlRpcValue.Create(result), parm = XmlRpcValue.Create(parms);
            result = res.instance;
            throw new NotImplementedException();
        }
        void getBusStatusCallback(IntPtr parms, IntPtr result)
        {
            XmlRpcValue res = XmlRpcValue.Create(result), parm = XmlRpcValue.Create(parms);
            result = res.instance;
            throw new NotImplementedException();
        }
        void getBusInfoCallback(IntPtr parms, IntPtr result)
        {
            XmlRpcValue res = XmlRpcValue.Create(result), parm = XmlRpcValue.Create(parms);
            result = res.instance;
            throw new NotImplementedException();
        }
        void getSubscriptionsCallback(IntPtr parms, IntPtr result)
        {
            XmlRpcValue res = XmlRpcValue.Create(result), parm = XmlRpcValue.Create(parms);
            result = res.instance;
            throw new NotImplementedException();
        }
        void getPublicationsCallback(IntPtr parms, IntPtr result)
        {
            XmlRpcValue res = XmlRpcValue.Create(result), parm = XmlRpcValue.Create(parms);
            result = res.instance;
            throw new NotImplementedException();
        }

        public void unadvertise(string topic, SubscriberCallbacks callbacks)
        {
            throw new NotImplementedException();
        }
    }
}
