#region USINGZ

using m = Messages.std_msgs;

#endregion

namespace Ros_CSharp
{
    /*public class SubscriptionQueue : CallbackInterface
    {
        public bool _full;
        public bool allow_concurrent_callbacks;

        public bool callback_mutex;
        public Queue<Item> queue = new Queue<Item>();
        public object queue_mutex = new object();

        public uint queue_size;
        public int size;
        public string topic;

        public SubscriptionQueue(string topic, int queue_size, bool allow_concurrent_callbacks)
        {
            this.topic = topic;
            this.allow_concurrent_callbacks = allow_concurrent_callbacks;
            _full = false;
            size = queue_size;
            this.queue_size = 0;
        }

        public void push(ISubscriptionCallbackHelper helper, IMessageDeserializer deserializer, bool nonconst_need_copy,
                         ref bool was_full, DateTime receipt_time = default(DateTime))
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

                    _full = true;
                    if (was_full)
                        was_full = true;
                }
                else
                    _full = false;
            }

            Item i = new Item
                         {
                             helper = helper,
                             deserializer = deserializer,
                             nonconst_need_copy = nonconst_need_copy,
                             receipt_time = receipt_time
                         };
            queue.Enqueue(i);
            ++queue_size;
        }

        public void clear()
        {
            while (callback_mutex)
            {
            }
            callback_mutex = true;
            lock (queue_mutex)
            {
                queue.Clear();
                queue_size = 0;
            }
            callback_mutex = false;
        }

        internal override CallResult Call()
        {
            if (!allow_concurrent_callbacks)
            {
                if (callback_mutex)
                    return CallResult.TryAgain;
                callback_mutex = true;
            }
            Item i = null;
            lock (queue_mutex)
            {
                if (queue.Count == 0)
                    return CallResult.Invalid;
                i = queue.Dequeue();
                --queue_size;
            }
            if (i == null)
                return CallResult.Invalid;
            SubscriptionCallbackHelperCallParams parms = new SubscriptionCallbackHelperCallParams();
            parms.Event = new IMessageEvent(i.deserializer.deserialize(), i.deserializer.connection_header, i.receipt_time,
                                            i.nonconst_need_copy, IMessageEvent.DefaultCreator);
            i.helper.call(parms);
            callback_mutex = false;
            return CallResult.Success;
        }

        public new virtual bool ready()
        {
            return true;
        }

        private bool fullNoLock()
        {
            return size > 0 && queue_size >= size;
        }

        public bool full()
        {
            lock (queue_mutex)
            {
                return fullNoLock();
            }
        }

        #region Nested type: Item

        public class Item
        {
            public IMessageDeserializer deserializer;
            public ISubscriptionCallbackHelper helper;
            public bool nonconst_need_copy;
            public DateTime receipt_time;
        }

        #endregion
    }*/
}