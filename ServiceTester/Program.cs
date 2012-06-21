using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServiceTester
{
    class Program
    {
        static void Main(string[] args)
        {
            SerDeser test = new SerDeser();
            test.Easy();
            test.Hard();
        }
    }

    public class SerDeser
    {
        public void Easy()
        {
            string original = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            Messages.std_msgs.String outbound = new Messages.std_msgs.String(original);
            byte[] serd = outbound.Serialize();
            Messages.std_msgs.String inbound = Messages.std_msgs.String.DeserializeIt(serd);
            if (original == inbound.data)
                Console.WriteLine("YAY!");
            else
                Console.WriteLine("GAY!");
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
                    seq = (uint)new Random().Next()
                },
                width = 10,
                height = 10,
                encoding = new Messages.std_msgs.String("BGR8")
            };
            byte[] serd = original.Serialize();
            Messages.sensor_msgs.Image inbound = Messages.sensor_msgs.Image.DeserializeIt(serd);
            if (original == inbound)
                Console.WriteLine("YAY!");
            else
                Console.WriteLine("GAY! CHECK IF VALUES ARE THE SAME!");
        }
    }
}
