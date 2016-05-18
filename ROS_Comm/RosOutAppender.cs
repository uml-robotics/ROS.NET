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
    internal class RosOutAppender
    {
        internal enum ROSOUT_LEVEL
        {
            DEBUG = 1,
            INFO = 2,
            WARN = 4,
            ERROR = 8,
            FATAL = 16
        }

        private Queue<IRosMessage> log_queue = new Queue<IRosMessage>();
        private Thread publish_thread;
        private bool shutting_down;

        internal RosOutAppender()
        {
            publish_thread = new Thread(logThread) {IsBackground = true};
            publish_thread.Start();
        }

        internal void shutdown()
        {
            shutting_down = true;
            publish_thread.Join();
        }

        internal void Append(string m, ROSOUT_LEVEL lvl)
        {
            Append(m, lvl, 4);
        }

        private void Append(string m, ROSOUT_LEVEL lvl, int level)
        {
            StackFrame sf = new StackTrace(new StackFrame(level, true)).GetFrame(0);
            IEnumerable<string> advert;
            TopicManager.Instance.getAdvertisedTopics(out advert);
            Log logmsg = new Log { msg = m, name = this_node.Name, file = sf.GetFileName(), function = sf.GetMethod().Name, line=(uint) sf.GetFileLineNumber(), level=((byte) ((int) lvl)),topics = (advert as string[] ?? advert.ToArray()).ToArray() };
            lock(log_queue)
                log_queue.Enqueue(logmsg);
        }

        private void logThread()
        {
            while ((!ROS.initialized || !ROS.isStarted() || !ROS.ok) && !shutting_down && !ROS.shutting_down)
            {
                Thread.Sleep(100);
            }
            if (shutting_down || ROS.shutting_down)
                return;
            AdvertiseOptions<Log> ops = new AdvertiseOptions<Log>(names.resolve("/rosout"), 0) { latch = true };
            SubscriberCallbacks cbs = new SubscriberCallbacks();
            TopicManager.Instance.advertise(ops, cbs);
            Queue<IRosMessage> localqueue;
            while (!shutting_down)
            {
                Publication p = TopicManager.Instance.lookupPublication(names.resolve("/rosout"));
                lock (log_queue)
                {
                    localqueue = new Queue<IRosMessage>(log_queue);
                    log_queue.Clear();
                }
                while (!shutting_down && localqueue.Count > 0)
                {
                    IRosMessage msg = localqueue.Dequeue();
                    TopicManager.Instance.publish(p, msg);
                }
                if (shutting_down) return;
                Thread.Sleep(100);
            }
        }
    }
}