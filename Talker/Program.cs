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
using am = Messages.arm_status_msgs;
using System.Text;

#endregion

namespace videoView
{
    public class Program
    {
        private static void Main(string[] args)
        {
            ROS.ROS_MASTER_URI = "http://10.0.3.88:11311";
            ROS.ROS_HOSTNAME = "10.0.3.141";
            ROS.Init(args, "Talker");
            NodeHandle node = new NodeHandle();
            Publisher<m.String> Talker = node.advertise<m.String>("/Chatter", 1);
            int count = 0;
            while (ROS.ok)
            {
                ROS.Info("Publishing a chatter message:    \"Blah blah blah " + count + "\"");
                String pow = new String("Blah blah blah "+(count++));
                
                Talker.publish(pow);
                ROS.spinOnce(node);
                Thread.Sleep(100);
            }
        }
    }
}

