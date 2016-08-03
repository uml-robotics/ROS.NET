// File: PollManager.cs
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
using System.Threading;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;
using System.Reflection;
using System.Linq;

#endregion

namespace Ros_CSharp
{
    public class Poll_Signal : IDisposable
    {
        public MethodInfo Method;
        public object Target;
        public delegate void PollSignalFunc();
        private Thread thread;
        private Action _op;
        private AutoResetEvent _go = new AutoResetEvent(false);
        private bool disposed = false;

        /// <summary>
        /// Sets this Poll_Signal's periodic operation, AND makes it be auto-polled by PollManager.
        /// </summary>
        public Action Op
        {
            get
            {
                return _op;
            }

            set
            {
                ManualOp = value;
                if (value != null)
                {
                    SignalEvent += signal;
                }
                else
                {
                    SignalEvent -= signal;
                }
            }
        }

        /// <summary>
        /// Sets this Poll_Signal's operation, without making it be auto-polled by PollManager
        /// </summary>
        public Action ManualOp
        {
            get
            {
                return _op;
            }
            set
            {
                try
                {
                    SignalEvent -= signal;
                }
                catch { }
                Method = value.Method;
                Target = value.Target;
                _op = value;
            }
        }

        internal static event PollSignalFunc SignalEvent;

        public Poll_Signal(Action psf)
        {
            if (psf != null)
            {
                Op = psf;
            }
            thread = new Thread(threadFunc) { IsBackground = true };
            thread.Start();
        }

        internal void signal()
        {
            _go.Set();
        }

        private void threadFunc()
        {
            while(ROS.ok && !disposed)
            {
                _go.WaitOne();
                if (ROS.ok && !disposed)
                    Op();
            }
            thread = null;
        }
        
        internal static void Signal()
        {
            if (SignalEvent != null) SignalEvent.Invoke();
        }

        public void Dispose()
        {
            SignalEvent -= signal;
            disposed = true;
            do
            {
                signal();
            } while (thread != null && !thread.Join(1));
        }
    }

    public class PollManager
    {
        private static PollManager _instance;
        private static object singleton_mutex = new object();
        public PollSet poll_set;
        public bool shutting_down;
        public object signal_mutex = new object();
        private List<Poll_Signal> signals = new List<Poll_Signal>();
        public TcpTransport tcpserver_transport;
        private Thread thread;

        public PollManager()
        {
            poll_set = new PollSet();
        }

        public static PollManager Instance
        {
#if !TRACE
            [DebuggerStepThrough]
#endif
                get
            {
                if (_instance == null)
                    lock (singleton_mutex)
                        if (_instance == null)
                            _instance = new PollManager();
                return _instance;
            }
        }

        public void addPollThreadListener(Action poll)
        {
#if DEBUG
            EDB.WriteLine("Adding pollthreadlistener " + poll.Target+":"+poll.Method);
#endif
            lock (signal_mutex)
            {
                signals.Add(new Poll_Signal(poll));
            }
            signal();
        }

        private void signal()
        {
            Poll_Signal.Signal();
        }

        public void removePollThreadListener(Action poll)
        {
            lock (signal_mutex)
            {
                signals.RemoveAll((s) => s.Op == poll);
            }
            signal();
        }

        private void threadFunc()
        {
            while (!shutting_down)
            {
                signal();
                Thread.Sleep(ROS.WallDuration);
                if (shutting_down) return;
            }
#if DEBUG
            EDB.WriteLine("PollManager thread IS FREE");
#endif
        }


        public void Start()
        {
            if (thread == null)
            {
                shutting_down = false;
                thread = new Thread(threadFunc);
                thread.Start();
            }
        }

        public void shutdown()
        {
            if (thread != null && !shutting_down)
            {
                shutting_down = true;
                poll_set.Dispose();
                poll_set = null;
                signals.Clear();
                if (!thread.Join(2000))
                {
                    EDB.WriteLine("PollManager had 2 seconds to drink the coolaid, and didn't. Trying the \"funnel method\".");
                    try
                    {
                        thread.Abort();
                    }
                    catch (Exception ex)
                    {
                    }
                }
                thread = null;
            }
        }
    }
}