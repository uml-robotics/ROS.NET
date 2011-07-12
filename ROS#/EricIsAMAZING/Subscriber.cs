#region USINGZ

using System;
using System.Diagnostics;

#endregion

namespace EricIsAMAZING
{
    public class Subscriber<M> : ISubscriber
    {
        public Subscriber(string topic, NodeHandle nodeHandle, ISubscriptionCallbackHelper cb)
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

        public int NumPublishers
        {
            get
            {
                if (impl != null && impl.IsValid)
                {
                    return TopicManager.Instance.getNumPublishers(impl.topic);
                }
                return 0;
            }
        }

        public void shutdown()
        {
            if (impl != null)
                impl.unsubscribe();
        }
    }

    public abstract class ISubscriber
    {
        public Impl impl = new Impl();


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

        internal void unsubscribe()
        {
            impl.unsubscribe();
        }

        #region Nested type: Impl

        public class Impl
        {
            public double constructed = DateTime.Now.Subtract(Process.GetCurrentProcess().StartTime).Ticks;
            public ISubscriptionCallbackHelper helper;
            public NodeHandle nodehandle;
            public string topic;
            public bool unsubscribed;

            public bool IsValid
            {
                get { return !unsubscribed; }
            }

            public void unsubscribe()
            {
                if (!unsubscribed)
                {
                    unsubscribed = true;
                    TopicManager.Instance.unsubscribe(topic, helper);
                }
            }
        }

        #endregion
    }
}