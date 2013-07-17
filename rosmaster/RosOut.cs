using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            ROS.ROS_MASTER_URI = "http://10.0.2.226:11311";
            ROS.ROS_HOSTNAME = "10.0.2.226";
            ROS.Init(new string[0], "rosout");

            nh = new NodeHandle();
            sub = nh.subscribe<Messages.rosgraph_msgs.Log>("/rosout", 10, rosoutCallback);
            pub = nh.advertise<Messages.rosgraph_msgs.Log>("/rosout_agg", 1000, true);


            new Thread(() =>
            {
                while (!ROS.shutting_down)
                {
                    Thread.Sleep(10);
                }
            }).Start();

        }


        public static void rosoutCallback(Messages.rosgraph_msgs.Log msg)
        {

        }



    }
}
