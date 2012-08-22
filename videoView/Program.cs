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

namespace videoView
{
    public class Program
    {
        public static WrapperTest.balls BALLS;
        public static WrapperTest.TellMeHowAwesomeIAm tellmehowawesomeiam;


        public static void thisishowawesomeyouare(string s)
        {
            Console.WriteLine(s);
        }

        private static void Main(string[] args)
        {
            //tellmehowawesomeiam = thisishowawesomeyouare;
            //WrapperTest.SetAwesomeFunctionPtr(tellmehowawesomeiam);
            ROS.ROS_MASTER_URI = "http://10.0.2.179:11311";
            ROS.ROS_HOSTNAME = "10.0.2.47";
            ROS.Init(args, "add_two_ints_client_csharp");
            NodeHandle node = new NodeHandle();
            Publisher<Messages.std_msgs.String> fuckYouNoob;                                    
            fuckYouNoob = node.advertise<Messages.std_msgs.String>("/suckit", 10);
            while (true)
            {
                Messages.std_msgs.String pow = new String("RIGHT IN THE KISSER");
                fuckYouNoob.publish(pow);
                ROS.spinOnce();
                Thread.Sleep(100);
            }
                
            //ServiceClient<TypedMessage<AddTwoInts.Request>, TypedMessage<AddTwoInts.Response>> testclient = node.serviceClient<TypedMessage<AddTwoInts.Request>, TypedMessage<AddTwoInts.Response>>("/add_two_ints");
            //TypedMessage<AddTwoInts.Response> resp = new TypedMessage<AddTwoInts.Response>();
            //if (testclient.call(new TypedMessage<AddTwoInts.Request>(new AddTwoInts.Request { a = 1, b = 2 }), ref resp, ""))
            //    Console.WriteLine(resp.data.sum);
            //else
            //    Console.WriteLine("figured it wouldn't work on the first go... ballsass");
            //Console.WriteLine("Going down");
            //Console.ReadLine();
        }
    }
}