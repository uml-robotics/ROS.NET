// File: CallbackQueue.cs
// Project: ROS_C-Sharp
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 09/01/2015
// Updated: 02/10/2016

#region USINGZ

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace Ros_CSharp
{
#if !TRACE
    [DebuggerStepThrough]
#endif
    public class CallbackQueue : CallbackQueueInterface, IDisposable
    {
        private int Count;
        public List<ICallbackInfo> callbacks = new List<ICallbackInfo>();
        public int calling;
        private Thread cbthread;
        private bool enabled;
        public Dictionary<UInt64, IDInfo> id_info = new Dictionary<UInt64, IDInfo>();
        private object id_info_mutex = new object();
        private AutoResetEvent sem = new AutoResetEvent(false);
        private object mutex = new object();
        public TLS tls;

        public bool IsEmpty
        {
            get { return Count == 0; }
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
            sem.Set();
        }

        internal void notify_one()
        {
            sem.Set();
        }

        public IDInfo getIDInfo(UInt64 id)
        {
            lock (id_info_mutex)
            {
                IDInfo value;
                if (id_info.TryGetValue(id, out value))
                    return value;
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
                removeemall(owner_id);
            else
            {
#if DEBUG
                EDB.WriteLine("removeByID w/ WRONG THREAD ID");
#endif
                removeemall(owner_id);
            }
        }

        private void removeemall(ulong owner_id)
        {
            lock (mutex)
            {
                callbacks.RemoveAll(ici => ici.removal_id == owner_id);
                Count = callbacks.Count;
            }
        }

        private void threadFunc()
        {
            TimeSpan wallDuration = new TimeSpan(0, 0, 0, 0, ROS.WallDuration);
            while (ROS.ok)
            {
                DateTime begin = DateTime.Now;
                if (!callAvailable(ROS.WallDuration))
                    break;
                DateTime end = DateTime.Now;
                if (wallDuration.Subtract(end.Subtract(begin)).Ticks > 0)
                    Thread.Sleep(wallDuration.Subtract(end.Subtract(begin)));
            }
#if DEBUG
            EDB.WriteLine("CallbackQueue thread broke out!");
#endif
        }

        public void Enable()
        {
            lock (mutex)
            {
                enabled = true;
            }
            notify_all();
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
            }
            notify_all();
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

        public bool callAvailable()
        {
            return callAvailable(ROS.WallDuration);
        }

        public bool callAvailable(int timeout)
        {
            setupTLS();
            int called = 0;
            lock (mutex)
            {
                if (!enabled) return false;
            }
            if (Count == 0 && timeout != 0)
            {
                if (!sem.WaitOne(timeout))
                    return true;
            }
            lock (mutex)
            {
                if (Count == 0)
                    return true;
                if (!enabled)
                    return false;
                callbacks.ForEach(cbi => tls.enqueue(cbi));
                callbacks.Clear();
                Count = 0;
                calling += tls.Count;
            }

            while (tls.Count > 0 && ROS.ok)
            {
                if (callOneCB(tls) != CallOneResult.Empty)
                    ++called;
            }
            lock (mutex)
            {
                calling -= called;
            }
            sem.Set();
            return true;
        }
    }

    public class TLS
    {
        private volatile List<CallbackQueueInterface.ICallbackInfo> _queue = new List<CallbackQueueInterface.ICallbackInfo>();
        public UInt64 calling_in_this_thread = 0xffffffffffffffff;

        public int Count
        {
            get { 
                lock(_queue)
                    return _queue.Count; }
        }

        public CallbackQueueInterface.ICallbackInfo head
        {
            get
            {
                lock (_queue)
                {
                    if (_queue.Count == 0) return null;
                    return _queue[0];
                }
            }
        }

        public CallbackQueueInterface.ICallbackInfo tail
        {
            get
            {
                lock (_queue)
                {
                    if (_queue.Count == 0) return null;
                    return _queue[_queue.Count - 1];
                }
            }
        }

        public CallbackQueueInterface.ICallbackInfo dequeue()
        {
            CallbackQueueInterface.ICallbackInfo tmp;
            lock (_queue)
            {
                if (_queue.Count == 0) return null;
                tmp = _queue[0];
                _queue.RemoveAt(0);
            }
            return tmp;
        }

        public void enqueue(CallbackQueueInterface.ICallbackInfo info)
        {
            if (info.Callback == null)
                return;
            lock(_queue)
                _queue.Add(info);
        }

        public CallbackQueueInterface.ICallbackInfo spliceout(CallbackQueueInterface.ICallbackInfo info)
        {
            lock(_queue)
            {
                if (!_queue.Contains(info))
                    return null;
                _queue.RemoveAt(_queue.IndexOf(info));
                return info;
            }
        }
    }

#if !TRACE
    [DebuggerStepThrough]
#endif
    public class CallbackQueueInterface
    {
        public virtual void addCallback(CallbackInterface callback)
        {
            addCallback(callback, callback.Get());
        }

        public virtual void addCallback(CallbackInterface callback, UInt64 owner_id)
        {
            throw new NotImplementedException();
        }

        public virtual void removeByID(UInt64 owner_id)
        {
            throw new NotImplementedException();
        }

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