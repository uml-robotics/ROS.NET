using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Messages.geometry_msgs;
using Messages.std_msgs;
using Ros_CSharp;
using Messages;
using Header = Messages.std_msgs.Header;

namespace ServiceTester
{
    class Program
    {
        static NodeHandle nh,nh2;
        static Publisher<Messages.tf.tfMessage> etoj;
        static Subscriber<Messages.tf.tfMessage> jtoe;
        static void Main(string[] args)
        {
            ROS.ROS_MASTER_URI = "http://10.0.2.42:11311";
            ROS.ROS_HOSTNAME = "10.0.2.177";
            ROS.ROS_IP = "10.0.2.177";
            ROS.Init(args, "eric_is_the_shuiznuit");
            nh = new NodeHandle();
            nh2 = new NodeHandle();
            jtoe = nh2.subscribe<Messages.tf.tfMessage>("/jtoe", 1000, ReachAround);
            etoj = nh.advertise<Messages.tf.tfMessage>("/etoj", 1000);
            new Thread(() =>
                {
                    Random r = new Random();
                    while (ROS.ok)
                    {
                        etoj.publish(new Messages.tf.tfMessage
                        {
                            transforms = new TransformStamped[]{
                            new TransformStamped { child_frame_id = new Messages.std_msgs.String("/eric_is_the_greatest_"+r.Next()), 
                                                   header = new Header { 
                                                       seq = 0, 
                                                       stamp = ROS.GetTime(), 
                                                       frame_id = new Messages.std_msgs.String("/emlt/wtfbbq") },
                                                   transform = new Transform { 
                                                       rotation = new Quaternion { 
                                                           w = r.NextDouble(),
                                                           x = r.NextDouble(),
                                                           y = r.NextDouble(),
                                                           z = r.NextDouble() },
                                                       translation = new Vector3 {
                                                           x = r.NextDouble(),
                                                           y = r.NextDouble(),
                                                           z = r.NextDouble() }
                                                   }
                                                }
                            }
                        });
                        Thread.Sleep(1000);
                    }
                }).Start();
            ROS.spin();
        }

        static Messages.tf.tfMessage outbound;
        static object mutex = new object();
        public static void ReachAround(Messages.tf.tfMessage msg)
        {
            lock (mutex)
            {
                outbound = new Messages.tf.tfMessage { transforms = new TransformStamped[msg.transforms.Length] };
                for (int i = 0; i < msg.transforms.Length; i++)
                {
                    tfmsgdump(msg);
                    HeaderDump(msg.transforms[i].header);
                    outbound.transforms[i] = msg.transforms[i];
                    if (msg.transforms[i].header.seq >= 5)
                    {
                        outbound = null;
                        break;
                    }
                    outbound.Serialized = null;
                    outbound.transforms[i].header.seq = msg.transforms[i].header.seq + 1;
                    outbound.transforms[i].child_frame_id = msg.transforms[i].child_frame_id;
                    outbound.transforms[i].Serialized = null;
                    outbound.transforms[i].header.Serialized = null;
                    outbound.transforms[i].transform.Serialized = null;
                }

                if (outbound != null)
                {
                    etoj.publish(outbound);
                }
            }
        }

        public static void tfmsgdump(Messages.tf.tfMessage m)
        {
            Console.WriteLine("tfMessage");
            for (int i = 0; i < m.transforms.Length; i++)
            {
                Console.Write("" + i + HeaderDump(m.transforms[i].header) + "\n" + TransformDump(m.transforms[i].transform)+"\n");
            }
            Console.WriteLine();
        }
        public static string TransformDump(Messages.geometry_msgs.Transform t)
        {
            return "\n\tTransform t:\n\t\ttranslation" + v3dump(t.translation) + "\n\t\trotation" + quatdump(t.rotation);
        }
        public static string v3dump(Messages.geometry_msgs.Vector3 v)
        {
            return "("+v.x+", "+v.y+", "+v.z+")";
        }
        public static string quatdump(Messages.geometry_msgs.Quaternion v)
        {
            return "(" + v.w+", "+ v.x + ", " + v.y + ", " + v.z + ")";
        }
        public static string HeaderDump(Messages.std_msgs.Header h)
        {
            return "\n\tframe_id: " + h.frame_id.data + "\n\tstamp: " + h.stamp.data.sec + "." + h.stamp.data.nsec + "\n\tseq: " + h.seq;
        }


        public static string dumphex(byte[] test)
        {
            if (test == null)
                return "dumphex(null)";
            string s = "";
            for (int i = 0; i < test.Length; i++)
                s += (test[i] < 16 ? "0" : "") + test[i].ToString("x") + " ";
            return s;
        }
    }

    public class SerDeser
    {
        public void Easy()
        {
            string original = "/thisisatestofthebanananana/";
            Messages.std_msgs.String outbound = new Messages.std_msgs.String(original);
            byte[] serd = outbound.Serialize();
            Messages.std_msgs.String inbound = Messages.std_msgs.String.DeserializeIt(serd);
            if (inbound == null || inbound.data == null) { Console.WriteLine("NULL"); return; }
            if (original == inbound.data)
                Console.WriteLine("YAY!");
        }
        public void Medium()
        {
            Header header = new Messages.std_msgs.Header
                {
                    frame_id = new Messages.std_msgs.String("/thisisatestofthebanananana/"),
                    seq = (uint)new Random().Next(),
                    stamp = new Messages.std_msgs.Time(new Messages.TimeData(1337, 5318008))
                };
            byte[] serd = header.Serialize();
            Header inbound = Header.DeserializeIt(serd);
            if (inbound == null) { Console.WriteLine("NULL"); return; }
            Console.WriteLine(HeaderDump(header));
            Console.WriteLine(HeaderDump(inbound));
            if (header == inbound)
                Console.WriteLine("YAY");
            else
                Console.WriteLine("GAY");
        }
        public void Hard()
        {
            byte[] imgdata = new byte[300];
            new Random().NextBytes(imgdata);
            Messages.sensor_msgs.Image original = new Messages.sensor_msgs.Image
            {
                data = imgdata,
                header = new Messages.std_msgs.Header
                {
                    frame_id = new Messages.std_msgs.String("/thisisatest/"),
                    seq = (uint)new Random().Next(),
                    stamp = new Messages.std_msgs.Time(new Messages.TimeData(6,9))
                },
                width = 10,
                height = 10,
                encoding = new Messages.std_msgs.String("BGR8")
            };
            byte[] serd = original.Serialize();
            Messages.sensor_msgs.Image inbound = Messages.sensor_msgs.Image.DeserializeIt(serd);
            if (inbound == null || inbound.data == null) { Console.WriteLine("NULL"); return; }
            ImgDump(original);
            ImgDump(inbound);
            if (original == inbound)
                Console.WriteLine("YAY!");
            else
                Console.WriteLine("GAY! CHECK IF VALUES ARE THE SAME!");
        }
        public void ImgDump(Messages.sensor_msgs.Image img)
        {
            StringBuilder sb = new StringBuilder("Image:");
            sb.AppendLine("data: #");
            foreach (byte b in img.data)
                sb.Append("" + b + " ");
            sb.AppendLine("header: " + HeaderDump(img.header));
            sb.AppendLine("width: " + img.width);
            sb.AppendLine("height: " + img.height);
            sb.AppendLine("encoding: " + img.encoding.data);
            Console.WriteLine(sb);
        }
        public string HeaderDump(Messages.std_msgs.Header h)
        {
            return "\n\tframe_id: " + h.frame_id.data + "\n\tstamp: " + h.stamp.data.sec + "." + h.stamp.data.nsec + "\n\tseq: " + h.seq;
        }
    }
}
