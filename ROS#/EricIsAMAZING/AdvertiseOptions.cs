using Messages;

namespace EricIsAMAZING
{
    public class AdvertiseOptions<T> where T : struct
    {
        public int queue_size;
        public SubscriberStatusCallback connectCB;
        public string datatype = "";
        public SubscriberStatusCallback disconnectCB;
        public bool has_header;
        public bool latch;
        public string md5sum = "";
        public string message_definition = "";
        public string topic = "";
        public CallbackQueueInterface callback_queue;

        public AdvertiseOptions()
        {
        }

        public AdvertiseOptions(string t, int q_size, string md5, string dt, string message_def, SubscriberStatusCallback connectcallback = null, SubscriberStatusCallback disconnectcallback = null)
        {
            topic = t;
            queue_size = q_size;
            md5sum = md5;
            TypedMessage<T> tt = new TypedMessage<T>();
            if (dt.Length > 0)
                datatype = dt;
            else
            {
                datatype = tt.type.ToString().Replace("__", "/");
            }
            if (message_def.Length == 0)
                message_definition = TypeHelper.MessageDefinitions[tt.type];
            else
                message_definition = message_def;
            has_header = tt.HasHeader;
            connectCB = connectcallback;
            disconnectCB = disconnectcallback;
        }

        public AdvertiseOptions(string t, int q_size, SubscriberStatusCallback connectcallback = null, SubscriberStatusCallback disconnectcallback = null) :
            this(t, q_size, MD5.Sum(t), new TypedMessage<T>().type.ToString().Replace("__","/"), TypeHelper.MessageDefinitions[new TypedMessage<T>().type], connectcallback, disconnectcallback)
        {
        }

        public static AdvertiseOptions<M> Create<M>(string topic, int q_size, SubscriberStatusCallback connectcallback, SubscriberStatusCallback disconnectcallback, CallbackQueue queue) where M : struct
        {
            return new AdvertiseOptions<M>(topic, q_size, connectcallback, disconnectcallback) {callback_queue = queue};
        }
    }
}