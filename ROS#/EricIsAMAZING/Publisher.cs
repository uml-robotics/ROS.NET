using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EricIsAMAZING
{
    public class Publisher<M> : IPublisher
    {

        public Publisher()
        {

        }

        public Publisher(string topic, string md5sum, string datatype, NodeHandle nodeHandle, SubscriberCallbacks callbacks)
        {
            // TODO: Complete member initialization
            impl.topic = topic;
            impl.md5sum = md5sum;
            impl.datatype = datatype;
            impl.nodeHandle = nodeHandle;
            impl.callbacks = callbacks;
        }
    }

    public class IPublisher
    {
        public Impl impl;
    }

    public class Impl
    {  
        public string topic;
        public string md5sum;
        public string datatype;
        public NodeHandle nodeHandle;
        public SubscriberCallbacks callbacks;
        public bool unadvertised;
        public double constructed = (int)Math.Floor(DateTime.Now.Subtract(System.Diagnostics.Process.GetCurrentProcess().StartTime).TotalMilliseconds);
        public bool IsValid { get { return !unadvertised; } }
        internal void unadvertise()
        {
            if (!unadvertised)
            {
                unadvertised = true;
                TopicManager.Instance().unadvertise(topic, callbacks);
            }
        }
    }
}
