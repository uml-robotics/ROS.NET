// File: RosOutAppender.cs
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Messages;
using Messages.rosgraph_msgs;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace Ros_CSharp
{
    public class RosOutAppender
    {
        private static object singleton_init_mutex = new object();
        private static RosOutAppender _instance;

        public static RosOutAppender Instance
        {
            get
            {
                if (_instance == null)
                    lock (singleton_init_mutex)
                    {
                        if (_instance == null)
                            _instance = new RosOutAppender();
                    }
                return _instance;
            }
        }

        internal enum
            ROSOUT_LEVEL
        {
            DEBUG = 1,
            INFO = 2,
            WARN = 4,
            ERROR = 8,
            FATAL = 16
        }

        private Queue<Log> log_queue = new Queue<Log>();
        private Thread publish_thread;
        private bool shutting_down;
        private Publisher<Log> publisher;

        public RosOutAppender()
        {
            publish_thread = new Thread(logThread) { IsBackground = true };
        }

        public bool started
        {
            get { return publish_thread != null && (publish_thread.ThreadState == System.Threading.ThreadState.Running || publish_thread.ThreadState == System.Threading.ThreadState.Background); }
        }

        public void start()
        {
            if (!shutting_down && !started)
            {
                if (publisher == null)
                    publisher = ROS.GlobalNodeHandle.advertise<Log>("/rosout", 0);
                publish_thread.Start();
            }
        }

        public void shutdown()
        {
            shutting_down = true;
            publish_thread.Join();
            if (publisher != null)
            {
                publisher.shutdown();
                publisher = null;
            }
        }

        internal void Append(string m, ROSOUT_LEVEL lvl)
        {
            Append(m, lvl, 4);
        }

        private void Append(string m, ROSOUT_LEVEL lvl, int level)
        {
            StackFrame sf = new StackTrace(new StackFrame(level, true)).GetFrame(0);
            Log logmsg = new Log
            {
                msg = m, name = this_node.Name, file = sf.GetFileName(), function = sf.GetMethod().Name, line = (uint) sf.GetFileLineNumber(), level = ((byte) ((int) lvl)),
                header = new m.Header() { stamp = ROS.GetTime() }
            };
            TopicManager.Instance.getAdvertisedTopics(out logmsg.topics);
            lock (log_queue)
                log_queue.Enqueue(logmsg);
        }

        private void logThread()
        {
            Queue<Log> localqueue;
            while (!shutting_down)
            {
                lock (log_queue)
                {
                    localqueue = new Queue<Log>(log_queue);
                    log_queue.Clear();
                }
                while (!shutting_down && localqueue.Count > 0)
                {
                    publisher.publish(localqueue.Dequeue());
                }
                if (shutting_down) return;
                Thread.Sleep(100);
            }
        }
    }
}