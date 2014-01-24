// File: CallbackQueue.cs
// Project: ROS_C-Sharp
// 
// ROS#
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 03/04/2013
// Updated: 07/26/2013

#region USINGZ

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Messages;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace Ros_CSharp
{
    public class CallbackQueue : CallbackQueueInterface, IDisposable
    {
        public List<ICallbackInfo> callbacks = new List<ICallbackInfo>();
        public int calling;
        private int Count;
        private Thread cbthread;
        private bool enabled;
        public Dictionary<UInt64, IDInfo> id_info = new Dictionary<UInt64, IDInfo>();
        private object id_info_mutex = new object();
        private object mutex = new object();
        private Semaphore sem = new Semaphore(0, int.MaxValue);
        public TLS tls;

        public bool IsEmpty
        {
            get
            {
                return Count == 0;
            }
        }

        public bool IsEnabled
        {
            get { return enabled; }
        }

        #region IDisposable Members

        public void Dispose()
        {
            lock (mutex)
            {
                Disable();
            }
        }

        #endregion

        public void setupTLS()
        {
            if (tls == null)
                tls = new TLS
                {
                    calling_in_this_thread = ROS.getPID()
                };
        }

        internal void notify_all()
        {
            sem.Release();
        }

        internal void notify_one()
        {
            sem.Release(1);
        }

        public IDInfo getIDInfo(UInt64 id)
        {
            lock (id_info_mutex)
            {
                if (id_info.ContainsKey(id))
                    return id_info[id];
            }
            return null;
        }

        public override void addCallback(CallbackInterface cb, UInt64 owner_id)
        {
            ICallbackInfo info = new ICallbackInfo {Callback = cb, removal_id = owner_id};

            lock (mutex)
            {
                if (!enabled)
                    return;
                callbacks.Add(info);
                Count++;
            }
            lock (id_info_mutex)
            {
                if (!id_info.ContainsKey(owner_id))
                {
                    id_info.Add(owner_id, new IDInfo {calling_rw_mutex = new object(), id = owner_id});
                }
            }
            notify_one();
        }

        public override void removeByID(UInt64 owner_id)
        {
            setupTLS();
            IDInfo idinfo;
            lock (id_info_mutex)
            {
                if (!id_info.ContainsKey(owner_id)) return;
                idinfo = id_info[owner_id];
            }
            if (idinfo.id == tls.calling_in_this_thread)
                lock (idinfo.calling_rw_mutex)
                {
                    removeemall(owner_id);
                }
            else
            {
                Console.WriteLine("removeByID w/ WRONG THREAD ID");
                removeemall(owner_id);
            }
        }

        private void removeemall(ulong owner_id)
        {
            lock (mutex)
            {
                callbacks.RemoveAll((ici) => ici.removal_id == owner_id);
                Count = callbacks.Count;
            }
        }

        private void threadFunc()
        {
            while (ROS.ok && !ROS.shutting_down && enabled)
            {
                callAvailable();
                Thread.Sleep(ROS.WallDuration);
            }
            Console.WriteLine("CallbackQueue thread broke out!");
        }

        public void Enable()
        {
            lock (mutex)
            {
                enabled = true;
                notify_all();
            }
            if (cbthread == null)
            {
                cbthread = new Thread(threadFunc);
                cbthread.Start();
            }
        }

        public void Disable()
        {
            lock (mutex)
            {
                enabled = false;
                notify_all();
            }
            if (cbthread != null)
            {
                cbthread.Join();
                cbthread = null;
            }
        }

        public void Clear()
        {
            lock (mutex)
            {
                callbacks.Clear();
                Count = 0;
            }
        }

        public CallOneResult callOneCB(TLS tls)
        {
            ICallbackInfo info = tls.head;
            if (info == null)
                return CallOneResult.Empty;
            IDInfo idinfo = null;
            idinfo = getIDInfo(info.removal_id);
            if (idinfo != null)
            {
                CallbackInterface cb = info.Callback;
                lock (idinfo.calling_rw_mutex)
                {
                    CallbackInterface.CallResult result = CallbackInterface.CallResult.Invalid;
                    tls.spliceout(info);
                    if (!info.marked_for_removal)
                    {
                        result = cb.Call();
                    }
                    if (result == CallbackInterface.CallResult.TryAgain && !info.marked_for_removal)
                    {
                        lock (mutex)
                        {
                            callbacks.Add(info);
                            Count++;
                        }
                        return CallOneResult.TryAgain;
                    }
                }
                return CallOneResult.Called;
            }
            ICallbackInfo cbi = tls.spliceout(info);
            if (cbi != null)
                cbi.Callback.Call();
            return CallOneResult.Called;
        }

        public CallOneResult callOne()
        {
            return callOne(ROS.WallDuration);
        }

        public CallOneResult callOne(int timeout)
        {
            setupTLS();
            ICallbackInfo cbinfo = null;
            lock (mutex)
            {
                if (!enabled) return CallOneResult.Disabled;
                if (Count == 0 && timeout != 0)
                {
                    sem.WaitOne(timeout, false);
                }
                if (Count == 0) return CallOneResult.Empty;
                if (!enabled) return CallOneResult.Disabled;
                for (int i = 0; i < callbacks.Count; i++)
                {
                    ICallbackInfo info = callbacks[i];
                    if (info.marked_for_removal)
                    {
                        callbacks.RemoveAt(--i);
                        Count--;
                        continue;
                    }
                    if (info.Callback.ready())
                    {
                        cbinfo = info;
                        callbacks.RemoveAt(--i);
                        Count--;
                        break;
                    }
                }
                sem.Release();
                if (cbinfo != null && cbinfo.Callback == null) return CallOneResult.TryAgain;
                calling++;
            }
            tls.enqueue(cbinfo);
            CallOneResult res = callOneCB(tls);
            if (res != CallOneResult.Empty)
            {
                lock (mutex)
                {
                    --calling;
                }
            }
            return res;
        }

        public void callAvailable()
        {
            callAvailable(ROS.WallDuration);
        }

        public void callAvailable(int timeout)
        {
            setupTLS();
            int called = 0;
            lock (mutex)
            {
                if (!enabled) return;
            }
            if (Count == 0 && timeout != 0)
            {
                sem.WaitOne(timeout);
            }
            lock (mutex)
            {
                if (Count == 0 || !enabled)
                    return;
                callbacks.ForEach(cbi => tls.enqueue(cbi));
                callbacks.Clear();
                Count = 0;
                calling += tls.Count;
            }

            while (tls.Count > 0 && ROS.ok)
            {
                if (callOneCB(tls) != CallOneResult.Empty)
                    ++called;
                //Console.WriteLine(tls.calling_in_this_thread + " = " + tls.Count+" -- "+called);
            }

            lock (mutex)
            {
                calling -= called;
            }
        }
    }

    public class TLS
    {
        private volatile Queue<CallbackQueueInterface.ICallbackInfo> _queue = new Queue<CallbackQueueInterface.ICallbackInfo>();
        private UInt64 _count = 0;
        public UInt64 calling_in_this_thread = 0xffffffffffffffff;
#if SAFE
        private object mut = new object();
#endif

        public int Count
        {
            get { return (int)_count; }
        }

        public CallbackQueueInterface.ICallbackInfo head
        {
            get { if (Count == 0) return null; 
#if SAFE
                lock (mut) 
#endif
                    return _queue.Peek(); }
        }

        public CallbackQueueInterface.ICallbackInfo tail
        {
            get { if (Count == 0) return null; 
#if SAFE
                lock(mut) 
#endif
                    return _queue.Last(); }
        }

        public CallbackQueueInterface.ICallbackInfo dequeue()
        {
            CallbackQueueInterface.ICallbackInfo icb;
#if SAFE
            lock (mut)
#endif
            {
                icb = _queue.Dequeue();
                _count--;
            }
            return icb;
        }

        public void enqueue(CallbackQueueInterface.ICallbackInfo info)
        {
            if (info.Callback == null)
                return;
#if SAFE
            lock (mut)
#endif
            {
                if (!_queue.Contains(info))
                {
                    _queue.Enqueue(info);
                    _count++;
                }
            }
        }

        public CallbackQueueInterface.ICallbackInfo spliceout(CallbackQueueInterface.ICallbackInfo info)
        {
            CallbackQueueInterface.ICallbackInfo icb;
            int stop;
#if SAFE
            lock (mut)
#endif
            {
                if (!_queue.Contains(info))
                    return null;
                stop = Count;
                _queue = new Queue<CallbackQueueInterface.ICallbackInfo>(_queue.Except(new[]{info}));
                _count--;
                return info;
            }
        }
    }

    /*
    public class TLS
    {
        public UInt64 calling_in_this_thread = 0xffffffffffffffff;
        public CallbackInfoNode current;
        public CallbackInfoNode head;
        public CallbackInfoNode tail;
        public int Count { get; set; }

        public CallbackQueueInterface.ICallbackInfo dequeue()
        {
            Count--;
            CallbackQueueInterface.ICallbackInfo ret = head.info;
            head = head.next;
            if (Count == 0)
            {
                current = null;
                tail = null;
            }
            return ret;
        }

        public void enqueue(CallbackQueueInterface.ICallbackInfo info)
        {
            Count++;
            if (head == null)
            {
                head = new CallbackInfoNode(info);
                tail = head;
            }
            else if (tail == null)
            {
                tail = head;
            }
            else
            {
                tail.next = new CallbackInfoNode(info);
                tail = tail.next;
            }
            if (current == null) current = head;
        }

        public CallbackQueueInterface.ICallbackInfo spliceout(CallbackQueueInterface.ICallbackInfo info)
        {
            CallbackInfoNode walk = head, walkbehind = null;
            while ((walkbehind = walk) != null && (walk = walk.next) != null)
            {
                if (walk.info == info)
                    break;
            }
            if (walk == tail && walk.info != info)
                return null;
            Count--;
            if (walk == tail)
                tail = walkbehind;
            if (walk != null && walkbehind != null)
                walkbehind.next = walk.next;
            else
                walkbehind = null;
            walk = null;
            if (Count == 0)
            {
                current = null;
                return null;
            }
            if (current != null)
                return current.info;
            return null;
        }

        #region Nested type: CallbackInfoNode

        public class CallbackInfoNode
        {
            public CallbackQueueInterface.ICallbackInfo info;
            public CallbackInfoNode next;

            public CallbackInfoNode(CallbackQueueInterface.ICallbackInfo i, CallbackInfoNode n)
            {
                info = i;
                next = n;
            }

            public CallbackInfoNode(CallbackQueueInterface.ICallbackInfo i)
                : this(i, null)
            {
            }
        }

        #endregion
    }
     */

    public class CallbackQueueInterface
    {
        public virtual void addCallback(CallbackInterface callback)
        {
            addCallback(callback, ROS.getPID());
        }

        public virtual void addCallback(CallbackInterface callback, UInt64 owner_id)
        {
            throw new NotImplementedException();
        }

        public virtual void removeByID(UInt64 owner_id)
        {
            throw new NotImplementedException();
        }

        #region Nested type: CallbackInfo

        public class CallbackInfo<M> : ICallbackInfo where M : IRosMessage, new()
        {
            public SubscriptionCallbackHelper<M> helper;
        }

        #endregion

        #region Nested type: ICallbackInfo

        public class ICallbackInfo
        {
            public CallbackInterface Callback;
            public bool marked_for_removal;
            public UInt64 removal_id;
        }

        #endregion
    }

    public class IDInfo
    {
        public object calling_rw_mutex;
        public UInt64 id;
    }

    public enum CallOneResult
    {
        Called,
        TryAgain,
        Disabled,
        Empty
    }
}