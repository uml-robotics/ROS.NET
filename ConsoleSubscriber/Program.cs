using Ros_CSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleSubscriber
{
	class Program
	{
		static Subscriber<Messages.std_msgs.String> sub;

		static Subscriber<Messages.std_msgs.Time> subTime;
        static NodeHandle nh;

		public static void subCallbackTime(Messages.std_msgs.Time msg)
		{
			Debug.WriteLine(String.Format("Got message: {0}:{1}", msg.data.sec, msg.data.nsec));
		}
        public static void subCallback(Messages.std_msgs.String msg)
        {
			Debug.WriteLine(String.Format("Got message: {0}", msg.data));
			/*
            Dispatcher.Invoke(new Action(() =>
            {
                l.Content = "Receieved:\n" + msg.data;
            }), new TimeSpan(0,0,1));*/
        }
		static void Main(string[] args)
		{
			ROS.ROS_MASTER_URI = "http://notemind02:11311";
            ROS.Init(new string[0], "wpf_listener");
            nh = new NodeHandle();

			//sub = nh.subscribe<Messages.std_msgs.String>("/chatter", 10, Program.subCallback);

			subTime = nh.subscribe<Messages.std_msgs.Time>("/heartbeat", 10, Program.subCallbackTime);

			Debug.WriteLine("Initialization is complete");
		}
	}
}
