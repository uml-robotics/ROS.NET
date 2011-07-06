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

        public SubscribeOptions(string topic, int queue_size)
        {
            // TODO: Complete member initialization
            this.topic = topic;
            this.queue_size = queue_size;
            helper = new SubscriptionCallbackHelper<T>(new T());
        }

        public SubscribeOptions(string topic, int queue_size, CallbackQueue cb)
        {
            // TODO: Complete member initialization
            this.topic = topic;
            this.queue_size = queue_size;
            Callback = cb;
            helper = new SubscriptionCallbackHelper<T>(cb);
        }
    }

    public class StuffOptions
    {
        public CallbackQueueInterface Callback;
        public int queue_size;
    }
}