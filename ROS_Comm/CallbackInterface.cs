// File: CallbackInterface.cs
// Project: ROS_C-Sharp
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 04/28/2015
// Updated: 02/10/2016

#region USINGZ

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Messages;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace Ros_CSharp
{
#if !TRACE
    [DebuggerStepThrough]
#endif
    public class Callback<T> : CallbackInterface where T : IRosMessage, new()
    {
        public Callback(CallbackDelegate<T> f, string topic, uint queue_size, bool allow_concurrent_callbacks) : this(f)
        {
            this.topic = topic;
            this.allow_concurrent_callbacks = allow_concurrent_callbacks;
            _full = false;
            size = queue_size;
        }

        public Callback(CallbackDelegate<T> f)
        {
            Event += func;
            base.Event += b => f(b as T);
        }

#pragma warning disable 67
        public new event CallbackDelegate<T> Event;
#pragma warning restore 67

        public bool _full;
        public bool allow_concurrent_callbacks;

        public volatile ConcurrentQueue<Item> queue = new ConcurrentQueue<Item>();
        private volatile bool callback_state;

        public uint size;
        public string topic;

        public override void pushitgood(ISubscriptionCallbackHelper helper, IRosMessage message, bool nonconst_need_copy, ref bool was_full, TimeData receipt_time)
        {
            if (was_full)
                was_full = false;
            if (fullNoLock())
            {
                Item toss;
                queue.TryDequeue(out toss);

                _full = true;
                was_full = true;
            }
            else
                _full = false;

            Item i = new Item
            {
                helper = helper,
                message = message,
                nonconst_need_copy = nonconst_need_copy,
                receipt_time = receipt_time
            };
            queue.Enqueue(i);
        }

        public override void clear()
        {
            queue = new ConcurrentQueue<Item>();
        }

        public new virtual bool ready()
        {
            return true;
        }

        private bool fullNoLock()
        {
            return size > 0 && queue.Count >= size;
        }

        public bool full()
        {
            return fullNoLock();
        }

        public class Item
        {
            public ISubscriptionCallbackHelper helper;
            public IRosMessage message;
            public bool nonconst_need_copy;
            public TimeData receipt_time;
        }

        internal override CallResult Call()
        {
            if (!allow_concurrent_callbacks)
            {
                if (callback_state)
                    return CallResult.TryAgain;
                callback_state = true;
            }
            Item i = null;
            if (queue.Count == 0)
                return CallResult.Invalid;
            if (!queue.TryDequeue(out i) || i == null)
            {
                callback_state = false;
                return CallResult.Invalid;
            }
            i.helper.call(i.message);
            callback_state = false;
            return CallResult.Success;
        }
    }

#if !TRACE
    [DebuggerStepThrough]
#endif
    public class CallbackInterface
    {
        private static object uidlock = new object();
        private static UInt64 nextuid;
        private UInt64 uid;

        public CallbackInterface()
        {
            lock (uidlock)
            {
                uid = nextuid;
                nextuid++;
            }
        }

        public UInt64 Get()
        {
            return uid;
        }

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

        public CallbackInterface(ICallbackDelegate f) : this()
        {
            Event += f;
        }

        public void func<T>(T msg) where T : IRosMessage, new()
        {
            if (Event != null)
            {
                Event(msg);
            }
            else
            {
                Console.WriteLine("EVENT IS NULL");
            }
        }

        public virtual void pushitgood(ISubscriptionCallbackHelper helper, IRosMessage msg, bool nonconst_need_copy, ref bool was_full, TimeData receipt_time)
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