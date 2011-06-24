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
            this.topic = topic;
            this.md5sum = md5sum;
            this.datatype = datatype;
            this.nodeHandle = nodeHandle;
            this.callbacks = callbacks;
        }
    }

    public abstract class IPublisher
    {  
        public string topic;
        public string md5sum;
        public string datatype;
        public NodeHandle nodeHandle;
        public SubscriberCallbacks callbacks;
    }
}
