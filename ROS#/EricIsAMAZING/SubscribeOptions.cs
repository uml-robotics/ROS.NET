using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EricIsAMAZING
{
    class SubscribeOptions<T>
    {
        private string topic;
        private int queue_size;
        private Action<T> cb;

        public SubscribeOptions(string topic, int queue_size)
        {
            // TODO: Complete member initialization
            this.topic = topic;
            this.queue_size = queue_size;
        }

        public SubscribeOptions(string topic, int queue_size, Action<T> cb)
        {
            // TODO: Complete member initialization
            this.topic = topic;
            this.queue_size = queue_size;
            this.cb = cb;
        }
    }
}
