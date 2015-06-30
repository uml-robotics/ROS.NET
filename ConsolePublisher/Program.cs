using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ros_CSharp;
//using XmlRpc_Wrapper;
using Messages;
using System.Threading;

namespace ConsolePublisher
{
	class Program
	{
		static Publisher<Messages.std_msgs.String> pub;
		static NodeHandle nh;

		static void Main(string[] args)
		{
			ROS.ROS_MASTER_URI = "http://notemind02:11311";
			try
			{
				ROS.Init(new string[0], "net_talker");
				nh = new NodeHandle();
				pub = nh.advertise<Messages.std_msgs.String>("chatter", 1, false);
				
				int i = 0;
				Messages.std_msgs.String msg;
				while (ROS.ok)
				{
					msg = new Messages.std_msgs.String("foo " + (i++));
					pub.publish(msg);
					Thread.Sleep(100);
				}

				ROS.shutdown();
				ROS.waitForShutdown();
			}
			catch (Exception e)
			{
				System.Diagnostics.Debug.Write(e);
			}
		}
	}
}
