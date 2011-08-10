#region USINGZ

using System;
using System.Diagnostics;
using Messages;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace EricIsAMAZING
{
    public class Publisher<M> : IPublisher where M : class, new()
    {
        public Publisher(string topic, string md5sum, string datatype, NodeHandle nodeHandle,
                         SubscriberCallbacks callbacks)
        {
            // TODO: Complete member initialization
            this.topic = topic;
            this.md5sum = md5sum;
            this.datatype = datatype;
            this.nodeHandle = nodeHandle;
            this.callbacks = callbacks;
        }

        public void publish(M msg)
        {
            TopicManager.Instance.publish(topic, new TypedMessage<M>(msg));
        }
    }

    public class IPublisher
    {
        public SubscriberCallbacks callbacks;

        public double constructed =
            (int) Math.Floor(DateTime.Now.Subtract(Process.GetCurrentProcess().StartTime).TotalMilliseconds);

        public string datatype;
        public string md5sum;
        public NodeHandle nodeHandle;
        public string topic;
        public bool unadvertised;

        public bool IsValid
        {
            get { return !unadvertised; }
        }

        internal void unadvertise()
        {
            if (!unadvertised)
            {
                unadvertised = true;
                TopicManager.Instance.unadvertise(topic, callbacks);
            }
        }
    }
}