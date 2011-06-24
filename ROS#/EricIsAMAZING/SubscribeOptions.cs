using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EricIsAMAZING
{
    public class SubscribeOptions<T> : StuffOptions
    {
        public bool latch = false, has_header;
        public string topic, md5sum, datatype, message_definition;

        public SubscribeOptions(string topic, int queue_size)
        {
            // TODO: Complete member initialization
            this.topic = topic;
            this.queue_size = queue_size;
        }

        public SubscribeOptions(string topic, int queue_size, CallbackQueue cb)
        {
            // TODO: Complete member initialization
            this.topic = topic;
            this.queue_size = queue_size;
            this.callbackQueue = cb;
        }
    }

    public class StuffOptions
    {
        public CallbackQueue callbackQueue;
        public int queue_size;
    }
}
