#region USINGZ

using System;
using Messages;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace EricIsAMAZING
{
    public class Callback<T> : CallbackInterface where T : IRosMessage, new()
    {
        public Callback(CallbackDelegate<T> f)
        {
            Event += func;
            /*{
                T t = new T();
                t.Deserialize(ci.Serialized);
                //if (ci == null || ci.Serialized == null) return;
                //byte[] data = new byte[ci.Serialized.Length];
                //Array.Copy(ci.Serialized, data, data.Length);
                //T t = new T();
                //t.Deserialize(data);
                //f(t);
            };*/
            base.Event +=
                (b) =>
                    {
                        if (b.Serialized != null)
                        {
                            T t = new T();
                            //byte[] FUCKNOZZLE = new byte[b.Serialized.Length];
                            //Array.Copy(b.Serialized, FUCKNOZZLE, FUCKNOZZLE.Length);
                            t.Deserialize(b.Serialized);
                            f(t);
                        }
                        else
                            f(b as T);
                    };
            //func = f;
        }

        public event CallbackDelegate<T> Event;

        /*public bool _full;
        public bool allow_concurrent_callbacks;

        public bool callback_mutex;
        public Queue<Item> queue = new Queue<Item>();
        public object queue_mutex = new object();

        public uint queue_size;
        public int size;
        public string topic;

        public Callback(string topic, int queue_size, bool allow_concurrent_callbacks)
        {
            this.topic = topic;
            this.allow_concurrent_callbacks = allow_concurrent_callbacks;
            _full = false;
            size = queue_size;
            this.queue_size = 0;
        }*/

        public void push(ISubscriptionCallbackHelper helper, IMessageDeserializer deserializer, bool nonconst_need_copy,
                         ref bool was_full, DateTime receipt_time = default(DateTime))
        {
            if (Event != null)
            {
                T t = (T) deserializer.deserialize();
                t.connection_header = deserializer.connection_header;
                Event(t);
            }
            /*if (receipt_time == default(DateTime)) receipt_time = DateTime.Now;
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
            ++queue_size;*/
        }

        /*public void clear()
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
        }*/

        public new virtual bool ready()
        {
            return true;
        }

        /*private bool fullNoLock()
        {
            return size > 0 && queue_size >= size;
        }

        public bool full()
        {
            lock (queue_mutex)
            {
                return fullNoLock();
            }
        }*/

        /*public class Item
        {
            public IMessageDeserializer deserializer;
            public ISubscriptionCallbackHelper helper;
            public bool nonconst_need_copy;
            public DateTime receipt_time;
        }*/

        internal override CallResult Call()
        {
            /*if (!allow_concurrent_callbacks)
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
            callback_mutex = false;*/
            return CallResult.Success;
        }
    }

    public class CallbackInterface
    {
        #region Delegates

        public delegate void ICallbackDelegate(IRosMessage msg);

        #endregion

        #region CallResult enum

        public enum CallResult
        {
            Success,
            TryAgain,
            Invalid
        }

        #endregion

        public CallbackInterface()
        {
        }

        public CallbackInterface(ICallbackDelegate f)
        {
            Event += f;
        }

        public void func<T>(T msg) where T : IRosMessage, new()
        {
            if (Event != null)
            {
                Event(msg);
            }
        }

        public event ICallbackDelegate Event;

        internal virtual CallResult Call()
        {
            return CallResult.Invalid;
        }

        internal virtual bool ready()
        {
            return true;
        }
    }
}