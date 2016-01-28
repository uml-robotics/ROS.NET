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
// Updated: 10/07/2015

#region USINGZ

using System;
using System.Collections.Concurrent;
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
        public enum ROSOUT_LEVEL
        {
            DEBUG = 1,
            INFO = 2,
            WARN = 4,
            ERROR = 8,
            FATAL = 16
        }

        public ConcurrentQueue<IRosMessage> log_queue = new ConcurrentQueue<IRosMessage>();
        public Thread publish_thread;
        public bool shutting_down;

        private Log logmsg = new Log() {msg = new m.String(), name = new m.String(this_node.Name), file = new m.String(), function = new m.String(), topics=new m.String[0]};

        public RosOutAppender()
        {
            publish_thread = new Thread(logThread) {IsBackground = true};
            publish_thread.Start();
            AdvertiseOptions<Log> ops = new AdvertiseOptions<Log>(names.resolve("/rosout"), 0) {latch = true};
            SubscriberCallbacks cbs = new SubscriberCallbacks();
            TopicManager.Instance.advertise(ops, cbs);
        }

        public void shutdown()
        {
            shutting_down = true;
            publish_thread.Join();
        }

        public void Append(string m, int level = 1)
        {
            Append(m, ROSOUT_LEVEL.INFO, level + 1);
        }

        public void Append(string m, ROSOUT_LEVEL lvl, int level = 1)
        {
            StackFrame sf = new StackTrace(new StackFrame(level, true)).GetFrame(0);
            logmsg.msg.data = m;
            logmsg.level = ((byte) ((int) lvl));
            logmsg.file.data = sf.GetFileName();
            logmsg.function.data = sf.GetMethod().Name;
            logmsg.line = (uint) sf.GetFileLineNumber();
            IEnumerable<string> advert = this_node.AdvertisedTopics();
            if (advert.Count() != logmsg.topics.Length)
            {
                logmsg.topics = new m.String[advert.Count()];
                int i = 0;
                advert.ToList().ForEach((ad) => logmsg.topics[i++] = new m.String(ad));
            }
            log_queue.Enqueue(logmsg);
        }

        public void logThread()
        {
            string n = names.resolve("/rosout");
            IRosMessage msg = null;
            while (!shutting_down)
            {
                while (!shutting_down && log_queue.TryDequeue(out msg))
                {
                    TopicManager.Instance.publish(n, msg);
                }
                if (shutting_down) return;
                Thread.Sleep(100);
            }
        }
    }
}