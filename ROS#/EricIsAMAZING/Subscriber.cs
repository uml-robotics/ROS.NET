using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EricIsAMAZING
{
    public class Subscriber<M> : ISubscriber
    {
        public Subscriber(string topic, NodeHandle nodeHandle, CallbackQueue cb)
        {
            // TODO: Complete member initialization
            this.topic = topic;
            this.nodeHandle = nodeHandle;
            callbacks = cb;
        }

       

        public Subscriber()
        {
        }
    }

    public abstract class ISubscriber
    {
        public string topic;
        public string md5sum;
        public string datatype;
        public NodeHandle nodeHandle;
        public CallbackQueue callbacks;
    }
}
