#region USINGZ

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

#endregion

namespace ConsoleApplication1
{
    public class Program
    {
        private const string ROS_MASTER_URI = "http://10.0.2.41:11311/";
        //private const string ROS_MASTER_URI = "http://EMVBOX:11311/";
        //private const string ROS_MASTER_URI = "http://localhost:11311/";

        public static WrapperTest.balls BALLS;

        public static WrapperTest.TellMeHowAwesomeIAm tellmehowawesomeiam;


        public static void thisishowawesomeyouare(string s)
        {
            Console.WriteLine(s);
        }

        public static void intCallback(TypedMessage<Int32> msg)
        {
            Console.WriteLine("" + msg.data.data);
        }

        public static void stringCallback(TypedMessage<String> msg)
        {
            Console.WriteLine("" + msg.data.data);
        }

        public static void BREAKSTUFFCallback(TypedMessage<arraytestsquared> msg)
        {
            if (msg.data.first == null)
                Console.WriteLine("FIRST = NULL!");
            else
                arraytestCallback(new TypedMessage<arraytest>(msg.data.first));

            if (msg.data.second == null)
                Console.WriteLine("SECOND = NULL!");
            else
                arraytestCallback(new TypedMessage<arraytest>(msg.data.second));
        }

        public static void arraytestCallback(TypedMessage<arraytest> msg)
        {
            string s = "\n---- CALLBACK ----\nstring:\t\t" + msg.data.teststring.data + "\n";
            s += "int[2]:\t\t";
            for (int i = 0; i < msg.data.integers.Length - 1; i++)
                s += "" + msg.data.integers[i] + ", ";
            s += msg.data.integers[msg.data.integers.Length - 1] + "\nint[]:\t\t";
            for (int i = 0; msg.data.lengthlessintegers != null && i < msg.data.lengthlessintegers.Length - 1; i++)
                s += "" + msg.data.lengthlessintegers[i] + ", ";
            if (msg.data.lengthlessintegers != null)
                s += "" + msg.data.lengthlessintegers[msg.data.lengthlessintegers.Length - 1];
            else
                s += "UNKNOWN LENGTH INT ARRAY = NULL!";
            s += "\nstring[2]:\t";
            for (int i = 0; i < msg.data.teststringarray.Length - 1; i++)
            {
                s += "" + (msg.data.teststringarray[i] == null ? "NULL" : msg.data.teststringarray[i].data) + ", ";
            }
            s += (msg.data.teststringarray[msg.data.teststringarray.Length - 1] == null
                      ? "NULL"
                      : msg.data.teststringarray[msg.data.teststringarray.Length - 1].data) + "\nstring[]:\t";
            for (int i = 0;
                 msg.data.teststringarraylengthless != null && i < msg.data.teststringarraylengthless.Length - 1;
                 i++)
                s += "" +
                     (msg.data.teststringarraylengthless[i] == null
                          ? "NULL"
                          : msg.data.teststringarraylengthless[i].data) + ", ";
            if (msg.data.teststringarraylengthless != null)
                s += "" +
                     (msg.data.teststringarraylengthless[msg.data.teststringarraylengthless.Length - 1] == null
                          ? "NULL"
                          : msg.data.teststringarraylengthless[msg.data.teststringarraylengthless.Length - 1].data);
            else
                s += "List<String> == NULL";
            s += "\n------------------ \n";
            string[] lines = s.Replace("CALLBACK", "ROS# GOES BOTH WAYS ZOMG!!!").Split(new[] {'\n'},
                                                                                          StringSplitOptions.
                                                                                              RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                ROS.Info(lines[i]);
            }
            Console.WriteLine(s);
        }

        private static void Main(string[] args)
        {
            tellmehowawesomeiam = thisishowawesomeyouare;
            WrapperTest.SetAwesomeFunctionPtr(tellmehowawesomeiam);
            ROS.ROS_MASTER_URI = ROS_MASTER_URI;
            ROS.Init(args, "ROSsharp_Listener");
            NodeHandle nh = new NodeHandle();
            Subscriber<TypedMessage<arraytest>> arraysub = nh.subscribe<arraytest>("arraytests", 1000, arraytestCallback);
            Subscriber<TypedMessage<arraytestsquared>> arraysquaredsub = nh.subscribe<arraytestsquared>("hardstuff",
                                                                                                        1000,
                                                                                                        BREAKSTUFFCallback);
            Publisher<arraytestsquared> hardpub = nh.advertise<arraytestsquared>("hardstuff2", 1000, true);
            int count = 0;
            while (ROS.ok)
            {
                arraytest[] pieces = new[] {new arraytest(), new arraytest()};
                pieces[0].teststring = new String("BBQ");
                pieces[1].teststring = new String("QBB");
                for (int i = 0; i < 2; i++)
                {
                    pieces[0].teststringarray[i] = new String("ZOMG " + (count + i));
                    pieces[1].teststringarray[i] = new String("" + (count + 2 - i) + " GMOZ");
                    pieces[0].integers[i] = count + i;
                    pieces[1].integers[i] = count + 2 - i;
                }
                pieces[0].lengthlessintegers = new int[10];
                pieces[1].lengthlessintegers = new int[10];
                pieces[0].teststringarraylengthless = new String[10];
                pieces[1].teststringarraylengthless = new String[10];
                for (int i = 2; i < 12; i++)
                {
                    pieces[0].lengthlessintegers[i - 2] = count + i;
                    pieces[1].lengthlessintegers[i - 2] = count + 12 - i;
                    pieces[0].teststringarraylengthless[i - 2] = new String("ZOMFGBBQ " + (count + i));
                    pieces[1].teststringarraylengthless[i - 2] = new String("" + (count + 12 - i) + " QBBGFMOZ");
                }
                hardpub.publish(new arraytestsquared {first = pieces[0], second = pieces[1]});
                count = (count + 1)%244;
                /*m.Header ht = new m.Header { seq = count, frame_id = new m.String((""+count)+(""+count)), stamp = new m.Time(count, count++) };
                pub2.publish(ht);
                Time t = new Time {data = new TimeData(1,1)};
                pub3.publish(t);
                pub4.publish(new String("UGH!"));*/
                ROS.spinOnce();
                Thread.Sleep(5);
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