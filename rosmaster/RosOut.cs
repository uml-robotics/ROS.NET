using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Messages.rosgraph_msgs;
using Ros_CSharp;
using XmlRpc_Wrapper;
using Messages;
using System.Threading;

namespace rosmaster
{
    /// <summary>
    /// Subscribed Topics:
    /// /rosout
    /// 
    /// Published Topics:
    /// /rosout_agg
    /// </summary>
    static class RosOut
    {

        static Publisher<Messages.rosgraph_msgs.Log> pub;
        static Subscriber<Messages.rosgraph_msgs.Log> sub;
        static NodeHandle nh;

        public static void start()
        {
            ROS.Init(new string[0], "rosout");

            nh = new NodeHandle();
            sub = nh.subscribe<Messages.rosgraph_msgs.Log>("/rosout", 10, rosoutCallback);
            pub = nh.advertise<Messages.rosgraph_msgs.Log>("/rosout_agg", 1000, true);

            new Thread(() =>
            {
                while (!ROS.ok)
                {
                    Thread.Sleep(10);
                }
            }).Start();

        }


        public static void rosoutCallback(Messages.rosgraph_msgs.Log msg)
        {
            string pfx = "[?]";
            switch (msg.level)
            {
                case Log.DEBUG:
                    pfx = "[DEBUG]";
                    break;
                case Log.ERROR:
                    pfx = "[ERROR]";
                    break;
                case Log.FATAL:
                    pfx = "[FATAL]";
                    break;
                case Log.INFO:
                    pfx = "[INFO]";
                    break;
                case Log.WARN:
                    pfx = "[WARN]";
                    break;
            }
            TimeData td = ROS.GetTime().data;
            Console.WriteLine("["+td.sec+"."+td.nsec+"]: "+pfx+": "+msg.msg+" ("+msg.file+" ("+msg.function+" @"+msg.line+"))");
            pub.publish(msg);
        }
    }
}
