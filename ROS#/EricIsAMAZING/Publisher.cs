#region USINGZ

using System;
using System.Diagnostics;
using XmlRpc_Wrapper;
using m = Messages.std_messages;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace EricIsAMAZING
{
    public class Publisher<M> : IPublisher where M : m.IRosMessage
    {

        public Publisher(string topic, string md5sum, string datatype, NodeHandle nodeHandle, SubscriberCallbacks callbacks)
        {
            // TODO: Complete member initialization
            impl.topic = topic;
            impl.md5sum = md5sum;
            impl.datatype = datatype;
            impl.nodeHandle = nodeHandle;
            impl.callbacks = callbacks;
        }

        public void publish(M msg)
        {
            if (impl == null)
                return;
            if (!impl.IsValid)
                return;
            TopicManager.Instance().publish(impl.topic, msg);
        }
    }

    public class IPublisher
    {
        public Impl impl;
    }

    public class Impl
    {
        public SubscriberCallbacks callbacks;
        public double constructed = (int) Math.Floor(DateTime.Now.Subtract(Process.GetCurrentProcess().StartTime).TotalMilliseconds);
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
                TopicManager.Instance().unadvertise(topic, callbacks);
            }
        }
    }
}