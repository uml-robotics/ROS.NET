#region USINGZ

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Messages;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace EricIsAMAZING
{
    public class CallbackQueue : CallbackQueueInterface, IDisposable
    {
        public List<ICallbackInfo> callbacks = new List<ICallbackInfo>();
        public int calling;
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
                lock (mutex)
                {
                    return callbacks.Count == 0;
                }
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
                tls = new TLS {calling_in_this_thread = (UInt64) Process.GetCurrentProcess().Threads[Thread.CurrentThread.ManagedThreadId].Id};
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
                if (!enabled) return;
                callbacks.Add(info);
            }
            lock (id_info_mutex)
            {
                if (!id_info.ContainsKey(owner_id))
                {
                    id_info.Add(owner_id, new IDInfo { calling_rw_mutex = new object(), id = owner_id });
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
                removeemall(owner_id);
        }

        private void removeemall(ulong owner_id)
        {
            lock (mutex)
            {
                for (int i = 0; i < callbacks.Count; i++)
                {
                    ICallbackInfo info = callbacks[i];
                    if (info.removal_id == owner_id)
                    {
                        i--;
                        callbacks.RemoveAt(i);
                    }
                }
            }
        }

        public void Enable()
        {
            lock (mutex)
            {
                enabled = true;
                notify_all();
            }
        }

        public void Disable()
        {
            lock (mutex)
            {
                enabled = false;
                notify_all();
            }
        }

        public void Clear()
        {
            lock (mutex)
            {
                callbacks.Clear();
            }
        }

        public CallOneResult callOneCB(TLS tls)
        {
            if (tls.calling_in_this_thread == 0xffffffffffffffff)
            {
                tls.current = tls.head;
            }
            if (tls.current == null)
                return CallOneResult.Empty;
            ICallbackInfo info = tls.current.info;
            CallbackInterface cb = info.Callback;
            IDInfo idinfo = getIDInfo(info.removal_id);
            if (idinfo != null)
            {
                lock (idinfo.calling_rw_mutex)
                {
                    UInt64 last_calling = tls.calling_in_this_thread;
                    tls.calling_in_this_thread = idinfo.id;
                    CallbackInterface.CallResult result = CallbackInterface.CallResult.Invalid;
                    tls.spliceout(info);
                    if (!info.marked_for_removal)
                        result = cb.Call();
                    tls.calling_in_this_thread = last_calling;
                    if (result == CallbackInterface.CallResult.TryAgain && !info.marked_for_removal)
                    {
                        lock (mutex)
                        {
                            callbacks.Add(info);
                        }
                        return CallOneResult.TryAgain;
                    }
                }
                return CallOneResult.Called;
            }
            else
            {
                ICallbackInfo cbi = tls.spliceout(tls.current.info);
                if (cbi != null)
                    cbi.Callback.Call();
            }
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
                if (callbacks.Count == 0 && timeout != 0)
                {
                    sem.WaitOne(timeout);
                }
                if (callbacks.Count == 0) return CallOneResult.Empty;
                if (!enabled) return CallOneResult.Disabled;
                for (int i = 0; i < callbacks.Count; i++)
                {
                    ICallbackInfo info = callbacks[i];
                    if (info.marked_for_removal)
                    {
                        i--;
                        callbacks.RemoveAt(i);
                        continue;
                    }
                    if (info.Callback.ready())
                    {
                        cbinfo = info;
                        i--;
                        callbacks.RemoveAt(i);
                        break;
                    }
                }
                if (cbinfo.Callback == null) return CallOneResult.TryAgain;
                calling++;
            }
            bool wasempty = tls.Count == 0;
            tls.enqueue(cbinfo);
            if (wasempty)
                tls.current = tls.head;
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
                if (callbacks.Count == 0 && timeout != 0)
                {
                    //DateTime prewait = DateTime.Now;
                    sem.WaitOne(timeout);
                    //Console.WriteLine("call avail waited for " + DateTime.Now.Subtract(prewait).TotalMilliseconds + " ms");
                }
                if (callbacks.Count == 0 || !enabled) return;
                bool wasempty = tls.Count == 0;
                callbacks.ForEach((cbi) => tls.enqueue(cbi));
                if (wasempty)
                    tls.current = tls.head;
                callbacks.Clear();
                calling += tls.Count;
            }

            while (tls.Count > 0)
            {
                if (callOneCB(tls) != CallOneResult.Empty)
                    ++called;
            }

            lock (mutex)
            {
                calling -= called;
            }
        }
    }

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
            else
            {
                tail.next = new CallbackInfoNode(info);
                tail = tail.next;
            }
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
            return current.info;
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

    public class CallbackQueueInterface
    {
        public virtual void addCallback(CallbackInterface callback, UInt64 owner_id = 0)
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