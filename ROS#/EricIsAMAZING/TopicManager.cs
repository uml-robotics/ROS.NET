using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public List<string> getAdvertisedTopics()
        {
            throw new NotImplementedException();
        }
        public List<string> getSubscribedTopics()
        {
            throw new NotImplementedException();
        }

        public void Start()
        {
            Console.WriteLine("STARTING MANAGER");
        }

        internal bool advertise(AdvertiseOptions ops, SubscriberCallbacks callbacks)
        {
            throw new NotImplementedException();
        }

        internal bool subscribe<T>(SubscribeOptions<T> ops)
        {
            throw new NotImplementedException();
        }
    }
}
