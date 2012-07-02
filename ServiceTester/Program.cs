using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Messages.std_msgs;
using Ros_CSharp;
using Messages;
using Header = Messages.std_msgs.Header;

namespace ServiceTester
{
    class Program
    {
        static NodeHandle nh;
         static Publisher<Messages.std_msgs.String> easy;
         static Subscriber<Messages.std_msgs.String> easyecho;
        static void Main(string[] args)
        {
            ROS.ROS_MASTER_URI = "http://192.13.37.129";
            ROS.ROS_HOSTNAME = "192.13.37.1";
            ROS.ROS_IP = "192.13.37.1";
            ROS.Init(args, "tester");
            nh = new NodeHandle();
           easy = nh.advertise<Messages.std_msgs.String>("/easy", 1000);
            easyecho = nh.subscribe<Messages.std_msgs.String>("/easyecho", 1000, (s) => Console.WriteLine("RECEIVED ECHO: " + s.data));
            int cnt = 0;
            while (true)
            {
                easy.publish(new Messages.std_msgs.String("BLADAZAAAAMMMNNN   " + (cnt++)));
                ROS.spinOnce();
                Thread.Sleep(10);
            }
            easy = null;
            easyecho = null;
            nh.shutdown();
            ROS.shutdown();
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
