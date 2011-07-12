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
            this.topic = topic;
            nodehandle = new NodeHandle(nodeHandle);
            helper = cb;
        }

        public Subscriber(Subscriber<M> s)
        {
            topic = s.topic;
            nodehandle = new NodeHandle(s.nodehandle);
            helper = s.helper;
        }

        public Subscriber()
        {
            throw new Exception("EMPTY CONSTRUCTOR CALLED...");
        }

        public int NumPublishers
        {
            get
            {
                if (IsValid)
                    return TopicManager.Instance.getNumPublishers(topic);
                return 0;
            }
        }

        public override void shutdown()
        {
            unsubscribe();
        }
    }

    public class ISubscriber
    {
        public double constructed = DateTime.Now.Subtract(Process.GetCurrentProcess().StartTime).Ticks;
        public ISubscriptionCallbackHelper helper;
        public NodeHandle nodehandle;
        public string topic = "";
        public bool unsubscribed;

        public bool IsValid
        {
            get { return !unsubscribed; }
        }

        public virtual void unsubscribe()
        {
            if (!unsubscribed)
            {
                unsubscribed = true;
                    TopicManager.Instance.unsubscribe(topic, helper);
            }
        }

        public virtual void shutdown()
        {
            throw new NotImplementedException();
        }
    }
}