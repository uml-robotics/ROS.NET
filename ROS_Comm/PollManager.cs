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
// Updated: 10/07/2015

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
        private List<Poll_Signal> signals = new List<Poll_Signal>();
        public event Poll_Signal poll_signal;

        public bool shutting_down;
        public object signal_mutex = new object();
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
                    throw new Exception("DOUBLE LISTENER ADDITION");
                }
                Console.WriteLine("Adding pollthreadlistener " + poll.Method);
                poll_signal += poll;
            }
            signal();
        }

        private void signal()
        {
            poll_signal.DynamicInvoke();
        }

        public void removePollThreadListener(Poll_Signal poll)
        {
            lock (signal_mutex)
            {
                if (signals.Contains(poll))
                {
                    throw new Exception("DOUBLE LISTENER REMOVAL");
                }
                Console.WriteLine("Removing pollthreadlistener " + poll.Method);
                poll_signal -= poll;
            }
            signal();
        }

        private void threadFunc()
        {
            while (!shutting_down)
            {
                signal();

                if (shutting_down) return;

                poll_set.update(10);
            }
        }


        public void Start()
        {
            shutting_down = false;
            thread = new Thread(threadFunc);
            thread.Start();
        }

        public void shutdown()
        {
            shutting_down = true;
            poll_set = null;
            thread.Join();
            signals.Clear();
            poll_signal = null;
        }
    }
}