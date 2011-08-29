#region USINGZ

using System;
using System.Threading;
using Ros_CSharp;
using Messages;
using Messages.custom_msgs;
using Messages.std_msgs;
using XmlRpc_Wrapper;
using Header = Messages.std_msgs.Header;
using String = Messages.std_msgs.String;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace ConsoleApplication1
{
    public class Program
    {
        private const string ROS_MASTER_URI = "http://robot-lab8:11311/";
        //private const string ROS_MASTER_URI = "http://EMVBOX:11311/";
        //private const string ROS_MASTER_URI = "http://localhost:11311/";

        public static WrapperTest.balls BALLS;

        public static WrapperTest.TellMeHowAwesomeIAm tellmehowawesomeiam;


        public static void thisishowawesomeyouare(string s)
        {
            Console.WriteLine(s);
        }

        public static void intCallback(TypedMessage<m.Int32> msg)
        {
            Console.WriteLine(""+msg.data.data);
        }

        public static void stringCallback(TypedMessage<m.String> msg)
        {
            Console.WriteLine("" + msg.data.data);
        }

        public static void intarrayCallback(TypedMessage<Messages.custom_msgs.simpleintarray> msg)
        {
            string s = "";
            for (int i = 0; i < msg.data.knownlengtharray.Length - 1; i++)
                s += "" + msg.data.knownlengtharray[i]+", ";
            s += "" + msg.data.knownlengtharray[msg.data.knownlengtharray.Length - 1];
            Console.WriteLine(s);
        }

        private static void Main(string[] args)
        {
            tellmehowawesomeiam = thisishowawesomeyouare;
            WrapperTest.SetAwesomeFunctionPtr(tellmehowawesomeiam);
            ROS.ROS_MASTER_URI = ROS_MASTER_URI;
            ROS.Init(args, "ROSsharp_Listener");
            NodeHandle nh = new NodeHandle();
            Subscriber<TypedMessage<m.Int32>> intsub = nh.subscribe<m.Int32>("ints", 1000, intCallback);
            Subscriber<TypedMessage<m.String>> strsub = nh.subscribe<m.String>("strings", 1000, stringCallback);
            Subscriber<TypedMessage<simpleintarray>> arraysub = nh.subscribe<simpleintarray>("intarrays", 1000, intarrayCallback);
            ROS.spin();
            while (ROS.ok)
            {
                /*arraytest test = new arraytest { lengthlessintegers = new[] { 2, 3, 4 }, teststring = new String("ZOMGSINGLESTRINGWORX"), teststringarraylengthless = new[] { new String("ZOMG1"), new String("ZOMGZOMG2"), new String("ZOMGZOMGZOMG3") } };
                test.integers[0] = 0;
                test.integers[1] = 1;
                test.teststringarray[0] = new String("string 1");
                test.teststringarray[1] = new String("string 2");
                pub.publish(test);
                ROS.Info("ERIC RULZ! "+count);*/
                /*m.Header ht = new m.Header { seq = count, frame_id = new m.String((""+count)+(""+count)), stamp = new m.Time(count, count++) };
                pub2.publish(ht);
                Time t = new Time {data = new TimeData(1,1)};
                pub3.publish(t);
                pub4.publish(new String("UGH!"));*/
                ROS.spinOnce();
                Thread.Sleep(1000);
            }


            //Publisher<m.TypedMessage<String>> pub = nh.advertise<String>("chatter", 1000);
            //pub.publish(new m.TypedMessage<String>(new String(){ data = "BLAH BLAH BLAH" }));
            /*if (!node.InitSubscriber("/rxconsole_1308702433994048982", "/rosout_agg"))
            {
                Console.WriteLine("FAILED TO REQUEST TOPIC... FUCK STAINS!");
                Console.ReadLine(); 
                node.Shutdown();
                return;
            }
            else
            {
                Console.WriteLine("HOLY FUCKING SHIT THE XMLRPC SHIT FUCKING CONNECTED!");
            }
            node.ConnectAsync();
             */
            Console.ReadLine();
            //node.Shutdown();
        }
    }
}