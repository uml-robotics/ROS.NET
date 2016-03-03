// File: PollSet.cs
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
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Socket = Ros_CSharp.CustomSocket.Socket;

#endregion

namespace Ros_CSharp
{
    public class PollSet : IDisposable
    {
        #region Delegates

        public delegate void SocketUpdateFunc(int stufftodo);

        #endregion

        public AutoResetEvent signal_mutex = new AutoResetEvent(true);

        public PollSet()
        {
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            signal_mutex.WaitOne();
            if (DisposingEvent != null)
                DisposingEvent();
            signal_mutex.Set();
        }

        public delegate void DisposingDelegate();

        public event DisposingDelegate DisposingEvent;

        public void signal()
        {
            if (signal_mutex.WaitOne())
            {
                Socket.Poll(ROS.WallDuration);
                signal_mutex.Set();
            }
        }

        public bool addSocket(Socket s, SocketUpdateFunc update_func)
        {
            return addSocket(s, update_func, null);
        }

        public bool addSocket(Socket s, SocketUpdateFunc update_func, TcpTransport trans)
        {
            s.Info = new SocketInfo {func = update_func, transport = trans};
            signal();
            return true;
        }

        public bool delSocket(Socket s)
        {
            s.Dispose();
            signal();
            return true;
        }

        public bool addEvents(uint s, int events)
        {
            Socket.Get(s).Info.events |= events;
            signal();
            return true;
        }

        public bool delEvents(uint sock, int events)
        {
            Socket.Get(sock).Info.events &= ~events;
            signal();
            return true;
        }

        public void update(int poll_timeout)
        {
            DateTime begin = DateTime.Now;
            if (signal_mutex.WaitOne(0))
            {
                Socket.Poll(poll_timeout);
                signal_mutex.Set();
            }
            DateTime end = DateTime.Now;
            double difference = 1.0 * poll_timeout - (end.Subtract(begin).TotalMilliseconds);
            if (difference > 0)
                Thread.Sleep((int)difference);
        }

#if !TRACE
        [DebuggerStepThrough]
#endif
        public override string ToString()
        {
            string s = "";
            s = Socket.FDs;
            s = s.Remove(s.Length - 3, 2);
            return s;
        }
    }

    public class SocketInfo
    {
        public class SocketUpdateQueueItem
        {
            public PollSet.SocketUpdateFunc func;
            public int revents;

            public SocketUpdateQueueItem(PollSet.SocketUpdateFunc f, int r)
            {
                func = f;
                revents = r;
            }
        }

        public Queue<SocketUpdateQueueItem> workqueue = new Queue<SocketUpdateQueueItem>();
        public int events;
        public PollSet.SocketUpdateFunc func;
        internal AutoResetEvent poll_mutex = new AutoResetEvent(true);
        internal AutoResetEvent queue_mutex = new AutoResetEvent(false);
        public int revents;
        public TcpTransport transport;
        private bool disposing = false;
        private Thread workThread = null;


        public void Enqueue(PollSet.SocketUpdateFunc f, int r)
        {
            lock (this)
            {
                if (disposing) 
                    return;
                if (workThread == null)
                {
                    workThread = new Thread(WorkFunc);
                    workThread.Start();
                }
            }
            lock (workqueue)
            {
                workqueue.Enqueue(new SocketUpdateQueueItem(f, r));
            }
            queue_mutex.Set();
        }

        public void WorkFunc()
        {
            Queue<SocketUpdateQueueItem> localqueue = new Queue<SocketUpdateQueueItem>();
            SocketUpdateQueueItem next = null;
            while (true)
            {
                lock (this)
                {
                    if (disposing)
                        break;
                }
                if (queue_mutex.WaitOne(100))
                {
                    lock (workqueue)
                    {
                        while (workqueue.Count > 0 && !disposing)
                            localqueue.Enqueue(workqueue.Dequeue());
                    }
                    while (localqueue.Count > 0 && !disposing)
                    {
                        next = localqueue.Dequeue();
                        next.func(next.revents);
                    }
                }
            }
        }

        public void Shutdown()
        {
            lock (this)
            {
                if (!disposing)
                    disposing = true;
                else
                    return;
                if (workThread != null)
                {
                    if (!workThread.Join(2000))
                    {
                        try
                        {
                            workThread.Abort();
                        }
                        catch
                        {
                        }
                    }
                    workThread = null;
                }
            }
        }
    }
}