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

#endregion

namespace videoView
{
    public class Program
    {
        private const string ROS_MASTER_URI = "http://robot-brain-1:11311/";
        //private const string ROS_MASTER_URI = "http://EMVBOX:11311/";
        //private const string ROS_MASTER_URI = "http://localhost:11311/";

        public static WrapperTest.balls BALLS;

        public static WrapperTest.TellMeHowAwesomeIAm tellmehowawesomeiam;


        public static void thisishowawesomeyouare(string s)
        {
            Console.WriteLine(s);
        }

        public static void videoCallback( TypedMessage<sm.Image> image)
        {
            Console.WriteLine("I GOT SOME VIDEO YO!");
        }

        private static void Main(string[] args)
        {
            tellmehowawesomeiam = thisishowawesomeyouare;
            WrapperTest.SetAwesomeFunctionPtr(tellmehowawesomeiam);
            ROS.ROS_MASTER_URI = ROS_MASTER_URI;
            ROS.Init(args, "ROSsharp_Listener");
            NodeHandle node = new NodeHandle();

            //Subscriber<> SubscribeOptions = node.subscribe<>();

            //Publisher<String> pub = node.advertise<String>("myTopic", 1000, true);

           // Messages.geometry_msgs.Twist t = new gm.Twist { angular = new gm.Vector3 { x = 0, y = 0, z = 0 }, linear = new gm.Vector3 { x = 0, y = 0, z = 0 } };
            //Publisher<gm.Twist> pub = node.advertise<gm.Twist>("rosaria/cmd_vel", 1000, true);


            Subscriber<TypedMessage<m.Header>> subby = node.subscribe<m.Header>("headercrap", 1000, (h) => {
                Console.WriteLine(h.data.seq + "\t\t" + h.data.stamp.data.sec + "." + h.data.stamp.data.nsec + "\t\t" + h.data.frame_id.data);
            });

            //Subscriber<TypedMessage<sm.Image>> subby = node.subscribe<sm.Image>("/camera/rgb/image_color", 1000, videoCallback);

            while (ROS.ok)
            {
                Console.WriteLine("just keep swimming");
                //Subscriber<TypedMessage<arraytest>> arraysub = node.subscribe<arraytest>("arraytests", 1000, arraytestCallback);

                //pub.publish(new m.String("Hello, World!") /*{ data = "Hello, World!" }*/ );
                // Console.WriteLine(new m.String { data = "Hello, World!" }.data);
                 Thread.Sleep(500);
            }            

            Console.ReadLine();
        }
    }
}