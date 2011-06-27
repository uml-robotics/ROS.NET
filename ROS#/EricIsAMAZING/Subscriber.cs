using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EricIsAMAZING
{
    public class Subscriber<M> : ISubscriber
    {
        public Subscriber(string topic, NodeHandle nodeHandle, SubscriptionCallbackHelper cb)
        {
            // TODO: Complete member initialization
            impl.topic = topic;
            impl.nodehandle = new NodeHandle(nodeHandle);
            impl.helper = cb;
        }

        public Subscriber(Subscriber<M> s)
        {
            impl = s.impl;
        }

        public Subscriber()
        {
        }

        public void shutdown()
        {
            if (impl != null)
                impl.unsubscribe();
        }

        public int NumPublishers
        {
            get
            {
                if (impl != null && impl.IsValid)
                {
                    return TopicManager.Instance().getNumPublishers(impl.topic);
                }
                return 0;
            }
        }
    }

    public abstract class ISubscriber
    {
        public Impl impl;


        public string Topic
        {
            get
            {
                if (impl == null) return "";
                return impl.topic;
            }
        }

        public static bool operator ==(ISubscriber lhs, ISubscriber rhs)
        {
            return lhs.impl == rhs.impl;
        }
        public static bool operator !=(ISubscriber lhs, ISubscriber rhs)
        {
            return lhs.impl != rhs.impl;
        }
        public class Impl
        {
            public bool unsubscribed;
            public double constructed = DateTime.Now.Subtract(System.Diagnostics.Process.GetCurrentProcess().StartTime).Ticks;
            public string topic;
            public SubscriptionCallbackHelper helper;
            public NodeHandle nodehandle;
            public bool IsValid { get { return !unsubscribed; } }
            public void unsubscribe()
            {
                if (!unsubscribed)
                {
                    unsubscribed = true;
                    TopicManager.Instance().unsubscribe(topic, helper);
                }
            }
        }

        internal void unsubscribe()
        {
            impl.unsubscribe();
        }
    }
}
