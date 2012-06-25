using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Messages.std_msgs;

namespace ServiceTester
{
    class Program
    {
        static void Main(string[] args)
        {
            SerDeser test = new SerDeser();
            test.Easy();
            test.Medium();
            test.Hard();
            Console.ReadLine();
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
            if (original == inbound.data)
                Console.WriteLine("YAY!");
            else
                Console.WriteLine("GAY!");
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
