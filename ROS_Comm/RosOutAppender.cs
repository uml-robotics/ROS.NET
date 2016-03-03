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

        public Queue<IRosMessage> log_queue = new Queue<IRosMessage>();
        private Log logmsg = new Log {msg = "", name = this_node.Name, file = "", function = "", topics = new string[0]};
        public Thread publish_thread;
        public bool shutting_down;

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
            logmsg.msg = m;
            logmsg.level = ((byte) ((int) lvl));
            logmsg.file = sf.GetFileName();
            logmsg.function = sf.GetMethod().Name;
            logmsg.line = (uint) sf.GetFileLineNumber();
            IEnumerable<string> advert = this_node.AdvertisedTopics();
            if (advert.Count() != logmsg.topics.Length)
            {
                logmsg.topics = advert.ToArray();
            }
            log_queue.Enqueue(logmsg);
        }

        public void logThread()
        {
            string n = names.resolve("/rosout");
            IRosMessage msg = null;

            Publication p = TopicManager.Instance.lookupPublication(n);
            while (!shutting_down)
            {
                if (p == null) p = TopicManager.Instance.lookupPublication(n);
                while (!shutting_down && log_queue.Count > 0 && (msg = log_queue.Dequeue())!=null)
                {
                    TopicManager.Instance.publish(p, msg);
                }
                if (shutting_down) return;
                Thread.Sleep(100);
            }
        }
    }
}