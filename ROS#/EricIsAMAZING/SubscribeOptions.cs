#region USINGZ

using Messages;
using m = Messages;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace EricIsAMAZING
{
    public class SubscribeOptions<T> : StuffOptions where T : IRosMessage, new()
    {
        public bool allow_concurrent_callbacks;
        public string datatype;
        public bool has_header;
        public SubscriptionCallbackHelper<T> helper;
        public bool latch;
        public string md5sum;
        public string message_definition;
        public string topic;

        public SubscribeOptions(string topic, int queue_size) : this(topic, queue_size, ROS.GlobalCallbackQueue)
        {
            // TODO: Complete member initialization
            helper = new SubscriptionCallbackHelper<T>(new T().type);
        }

        public SubscribeOptions(string topic, int queue_size, CallbackDelegate<T> cb)
        {
            // TODO: Complete member initialization
            this.topic = topic;
            this.queue_size = queue_size;
            helper = new SubscriptionCallbackHelper<T>(new T().type, cb);
        }

        public SubscribeOptions(string topic, int queue_size, CallbackQueueInterface cb)
        {
            // TODO: Complete member initialization
            this.topic = topic;
            this.queue_size = queue_size;
            helper = new SubscriptionCallbackHelper<T>(cb);
            Callback = cb;
        }
    }

    public class StuffOptions
    {
        public CallbackQueueInterface Callback;
        public int queue_size;
    }

    public delegate void CallbackDelegate<T>(T argument) where T : IRosMessage, new();
}