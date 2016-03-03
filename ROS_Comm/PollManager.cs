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

#endregion

namespace Ros_CSharp
{
    public class PollManager
    {
        #region Delegates

        public delegate void Poll_Signal();

        #endregion

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

        public void addPollThreadListener(Poll_Signal poll)
        {
            lock (signal_mutex)
            {
                if (signals.Contains(poll))
                {
                    return;
                }
                signals.Add(poll);
                Console.WriteLine("Adding pollthreadlistener " + poll.Method);
            }
            signal();
        }

        private void signal()
        {
            lock (this)
                if (shutting_down)
                    return;
            lock (signal_mutex)
            {
                foreach (Poll_Signal signal in signals)
                {
                    signal.BeginInvoke(signalComplete, signal);
                }
            }
        }

        private void signalComplete(IAsyncResult iar)
        {
            Poll_Signal ps = (Poll_Signal) iar.AsyncState;
            ps.EndInvoke(iar);
        }

        public void removePollThreadListener(Poll_Signal poll)
        {
            lock (signal_mutex)
            {
                if (!signals.Contains(poll))
                {
                    throw new Exception("DOUBLE LISTENER REMOVAL");
                }
                signals.Remove(poll);
                Console.WriteLine("Removing pollthreadlistener " + poll.Method);
            }
            signal();
        }

        private void threadFunc()
        {
            while (true)
            {
                if (shutting_down)
                    break;
                signal();
                if (shutting_down)
                    break;
                poll_set.update(10);
            }
            Console.WriteLine("PollManager thread IS FREE");
        }


        public void Start()
        {
            lock(this)
                if (thread == null)
                {
                    shutting_down = false;
                    thread = new Thread(threadFunc);
                    thread.Start();
                }
        }

        public void shutdown()
        {
            lock (this)
                if (thread != null && !shutting_down)
                {
                    shutting_down = true;
                    lock (signal_mutex)
                    {
                        signals.ForEach(s =>
                                            {
                                                Console.WriteLine("PollManager cleanup: removing " + s.Method);
                                                removePollThreadListener(s);
                                            });
                        signals.Clear();
                    }
                    if (!thread.Join(2000))
                    {
                        try
                        {
                            thread.Abort();
                        }
                        catch
                        {
                        }
                        Console.WriteLine("PollManager had 2 seconds to drink the coolaid, and didn't. Trying the \"funnel method\".");
                    }
                    thread = null;
                    poll_set.Dispose();
                    poll_set = null;
                }
        }
    }
}