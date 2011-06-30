using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EricIsAMAZING
{
    public class SubscriptionQueue : CallbackInterface
    {
        public struct Item
        {
            public ISubscriptionCallbackHelper helper;
            public bool nonconst_need_copy;
            public DateTime receipt_time;
        }

        public Queue<Item> queue = new Queue<Item>();

        public SubscriptionQueue(string topic, int queue_size, bool allow_concurrent_callbacks)
        {
            this.topic = topic;
            this.allow_concurrent_callbacks = allow_concurrent_callbacks;
            full = false;
            size = queue_size;
            this.queue_size = 0;
        }

        public void push(ISubscriptionCallbackHelper helper, bool nonconst_need_copy,  ref bool was_full, DateTime receipt_time = default(DateTime))
        {
            if (receipt_time == default(DateTime)) receipt_time = DateTime.Now;
            lock (queue_mutex)
            {
                if (was_full)
                    was_full = false;
                if (fullNoLock())
                {
                    queue.Dequeue();
                    --queue_size;

                    full = true;
                    if (was_full)
                        was_full = true;
                }
                else
                    full = false;
            }

            Item i = new Item { helper = helper, nonconst_need_copy = nonconst_need_copy, receipt_time = receipt_time };
            queue.Enqueue(i);
            ++queue_size;
        }

        public void clear()
        {
            lock (callback_mutex)
            {
                lock (queue_mutex)
                {
                    queue.Clear();
                    queue_size = 0;
                }
            }
        }

        public virtual CallbackInterface.CallResult call()
        {

        }

        public new virtual bool ready()
        {

        }
        
        private bool fullNoLock()
        {

        }

        public string topic;
        public int size;
        public bool full;

        public object queue_mutex = new object();

        public uint queue_size;
        public bool allow_concurrent_callbacks;

        public object callback_mutex = new object();
    }
}
