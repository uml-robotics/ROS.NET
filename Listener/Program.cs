#region Imports

using System;
using System.IO;
using System.Threading;
using Messages;
using Messages.custom_msgs;
using Ros_CSharp;
using XmlRpc_Wrapper;
using Int32 = Messages.std_msgs.Int32;
using String = Messages.std_msgs.String;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;
using sm = Messages.sensor_msgs;
using System.Text;

#endregion

namespace Listener
{
    public class Program
    {
        private static void chatterCallback(m.String s)
        {
            ROS.Info("RECEIVED: " + s.data);
        }
        private static void Main(string[] args)
        {
            ROS.Init(args, "Listener");
            NodeHandle node = new NodeHandle();
            Subscriber<m.String> Subscriber = node.subscribe<m.String>("/chatter", 1, chatterCallback);
            ROS.waitForShutdown();
        }
    }
}