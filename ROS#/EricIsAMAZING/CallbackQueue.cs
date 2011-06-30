using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using XmlRpc_Wrapper;
using m = Messages;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;
using System.Threading;

namespace EricIsAMAZING
{

    public class CallbackQueue : IDisposable
    {
        Semaphore sem = new Semaphore(0, int.MaxValue);
        public TLS tls;
        public void setupTLS()
        {
            if (tls == null)
                tls = new TLS() { calling_in_this_thread = (UInt64)Thread.CurrentThread.ManagedThreadId };
        }
        public int calling;
        bool enabled = false;

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
        public Dictionary<UInt64, IDInfo> id_info = new Dictionary<ulong, IDInfo>();
        public List<ICallbackInfo> callbacks = new List<ICallbackInfo>();
        object mutex = new object();
        object id_info_mutex = new object();
        public void AddCallback(CallbackInterface cb, ulong owner_id)
        {
            ICallbackInfo info = new ICallbackInfo{callback = cb, removal_id = owner_id};

            lock (mutex)
            {
                if (!enabled) return;
                callbacks.Add(info);
            }
            notify_one();
        }

        public void removeByID(UInt64 owner_id)
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

        public void Dispose()
        {
            lock (mutex)
            {
                Disable();
            }
        }
        
        public void Clear()
        {
            lock (mutex)
            {
                callbacks.Clear();
            }
        }

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

        public CallOneResult callOneCB(TLS tls)
        {
            if (tls.calling_in_this_thread == 0xffffffffffffffff)
            {
                tls.current = tls.head;
            }
            if (tls.current == null)
                return CallOneResult.Empty;
            ICallbackInfo info = tls.current.info;
            CallbackInterface cb = info.callback;
            IDInfo id_info = getIDInfo(info.removal_id);
            if (id_info != null)
            {
                lock (id_info.calling_rw_mutex)
                {
                    UInt64 last_calling = tls.calling_in_this_thread;
                    tls.calling_in_this_thread = id_info.id;
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
                tls.spliceout(tls.current.info);
            }
            return CallOneResult.Called;
        }

        public bool IsEnabled { get { return enabled; } }

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
                for(int i=0;i<callbacks.Count;i++)
                {
                    ICallbackInfo info = callbacks[i];
                    if (info.marked_for_removal)
                    {
                        i--;
                        callbacks.RemoveAt(i);
                        continue;
                    }
                    if (info.callback.ready())
                    {
                        cbinfo = info;
                        i--;
                        callbacks.RemoveAt(i);
                        break;
                    }
                }
                if (cbinfo.callback == null) return CallOneResult.TryAgain;
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
                    DateTime prewait = DateTime.Now;
                    sem.WaitOne(timeout);
                    Console.WriteLine("call avail waited for "+DateTime.Now.Subtract(prewait).TotalMilliseconds+" ms");
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
        public CallbackInfoNode head;
        public CallbackInfoNode tail;
        public CallbackInfoNode current;
        public int Count = 0;
        public ICallbackInfo dequeue()
        {
            Count--;
            ICallbackInfo ret = head.info;
            head = head.next;
            if (Count == 0)
            {
                current = null;
                tail = null;
            }
            return ret;
        }
        public void enqueue(ICallbackInfo info)
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
        public ICallbackInfo spliceout(ICallbackInfo info)
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
            walkbehind.next = walk.next;
            walk = null;
            if (Count == 0) current = null;
            return current.info;
        }
        public class CallbackInfoNode
        {
            public ICallbackInfo info;
            public CallbackInfoNode next;
            public CallbackInfoNode(ICallbackInfo i, CallbackInfoNode n)
            {
                info = i;
                next = n;
            }
            public CallbackInfoNode(ICallbackInfo i) : this(i, null)
            {
            }
        }
    }

    public class CallbackInfo<M> : ICallbackInfo where M : m.IRosMessage
    {
        public SubscriptionCallbackHelper<M> helper;
    }

    public abstract class ICallbackInfo
    {
        public UInt64 removal_id;
        public bool marked_for_removal;
        public CallbackInterface callback;
    }

    public class IDInfo
    {
        public UInt64 id;
        public object calling_rw_mutex;
    }

    public enum CallOneResult
    {
        Called,TryAgain,Disabled,Empty
    }
}
