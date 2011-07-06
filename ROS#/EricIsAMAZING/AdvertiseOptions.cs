namespace EricIsAMAZING
{
    public class AdvertiseOptions : StuffOptions
    {
        public SubscriberStatusCallback connectCB;
        public string datatype;
        public SubscriberStatusCallback disconnectCB;
        public bool has_header;
        public bool latch;
        public string md5sum;
        public string message_definition;
        public string topic;

        public AdvertiseOptions()
        {
        }

        public AdvertiseOptions(string t, int q_size, string md5, string dt, string message_def, SubscriberStatusCallback connectcallback = null, SubscriberStatusCallback disconnectcallback = null)
        {
            topic = t;
            queue_size = q_size;
            md5sum = md5;
            datatype = dt;
            message_definition = message_def;
            connectCB = connectcallback;
            disconnectCB = disconnectcallback;
        }

        public AdvertiseOptions(string t, int q_size, SubscriberStatusCallback connectcallback = null, SubscriberStatusCallback disconnectcallback = null) :
            this(t, q_size, "", "", "", connectcallback, disconnectcallback)
        {
        }

        public static AdvertiseOptions Create(string topic, int q_size, SubscriberStatusCallback connectcallback, SubscriberStatusCallback disconnectcallback, CallbackQueue queue)
        {
            return new AdvertiseOptions(topic, q_size, connectcallback, disconnectcallback) {Callback = queue};
        }
    }
}