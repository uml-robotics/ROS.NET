// File: CallbackInterface.cs
// Project: ROS_C-Sharp
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 11/06/2013
// Updated: 07/23/2014

#region USINGZ

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Messages;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace Ros_CSharp
{
    [DebuggerStepThrough]
    public class Callback<T> : CallbackInterface where T : IRosMessage, new()
    {
        public Callback(CallbackDelegate<T> f, string topic, uint queue_size, bool allow_concurrent_callbacks) : this(f)
        {
            this.topic = topic;
            this.allow_concurrent_callbacks = allow_concurrent_callbacks;
            _full = false;
            size = queue_size;
            this.queue_size = 0;
        }

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
                b =>
                {
                    if (b.Serialized != null)
                    {
                        IRosMessage t = new T();
                        t = t.Deserialize(b.Serialized);
                        t.connection_header = b.connection_header;
                        f(t as T);
                    }
                    else
                        f(b as T);
                };
            //func = f;
        }

#pragma warning disable 67
        public new event CallbackDelegate<T> Event;
#pragma warning restore 67

        public bool _full;
        public bool allow_concurrent_callbacks;

        public bool callback_mutex;
        public volatile Queue<Item> queue = new Queue<Item>();
        public object queue_mutex = new object();

        public uint queue_size;
        public uint size;
        public string topic;

        public void push(SubscriptionCallbackHelper<T> helper, MessageDeserializer<T> deserializer, bool nonconst_need_copy, ref bool was_full)
        {
            push(helper, deserializer, nonconst_need_copy, ref was_full, new TimeData());
        }


        public void push(SubscriptionCallbackHelper<T> helper, MessageDeserializer<T> deserializer, bool nonconst_need_copy,
            ref bool was_full, TimeData receipt_time)
        {
            pushitgood(helper, deserializer, nonconst_need_copy, ref was_full, receipt_time);
        }

        public override void pushitgood(ISubscriptionCallbackHelper helper, IMessageDeserializer deserializer, bool nonconst_need_copy, ref bool was_full, TimeData receipt_time)
        {
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
        }

        public override void clear()
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

        public class Item
        {
            public IMessageDeserializer deserializer;
            public ISubscriptionCallbackHelper helper;
            public bool nonconst_need_copy;
            public TimeData receipt_time;
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
            {
                callback_mutex = false;
                return CallResult.Invalid;
            }
            i.deserializer.deserialize();
            callback_mutex = false;
            return CallResult.Success;
        }
    }

    [DebuggerStepThrough]
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

        public virtual void pushitgood(ISubscriptionCallbackHelper helper, IMessageDeserializer deserializer, bool nonconst_need_copy, ref bool was_full, TimeData receipt_time)
        {
            throw new NotImplementedException();
        }


        public virtual void clear()
        {
            throw new NotImplementedException();
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