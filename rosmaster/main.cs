using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace rosmaster
{
    class main
    {
        private readonly string default_port = "11311";
        private readonly string default_ip = "localhost";

        private static string _port;
        private static string _ip;
        private static string _ROS_MASTER_URI;

        public static void Main(string[] args)
        {

            if (args.Length > 1)
            {
                //throw new Exception("TOO MANY ARGS");
            }
            else if (args.Length == 1)
            {
                _ROS_MASTER_URI = args[0];
            }
            else
            {
                _ROS_MASTER_URI = System.Environment.GetEnvironmentVariable("ROS_MASTER_URI", EnvironmentVariableTarget.Machine);
               
            }
            Console.WriteLine("Connecting to " + _ROS_MASTER_URI);

            Master master = new Master(_ROS_MASTER_URI);
            master.start();


            while (master.ok())
            {
                Thread.Sleep(10);
            }

            master.stop();
        }
    }
}
