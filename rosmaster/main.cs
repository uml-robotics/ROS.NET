using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Ros_CSharp;

namespace rosmaster
{
    class main
    {
        public static void Main(string[] args)
        {
            //use ROS remapping spec for setting uri by command line
            for (int i = 0; i < args.Length; i++)
                if (args[i].Contains(":=")) {
                    string[] chunks = args[i].Split(new[]{":="},StringSplitOptions.RemoveEmptyEntries);
                    if (chunks.Length == 2)
                        switch (chunks[0]) {
                            case "__master": ROS.ROS_MASTER_URI = chunks[1].Trim(); break;
                            case "__hostname": ROS.ROS_HOSTNAME = chunks[1].Trim(); break;
                        }
                }

            //if wasn't passed in with __master:=_______, then check environment variable
            if (string.IsNullOrEmpty(ROS.ROS_MASTER_URI))
            {
                IDictionary _vars;

                //check user env first, then machine if user doesn't have uri defined.
                if ((_vars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User)).Contains("ROS_MASTER_URI")
                    || (_vars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine)).Contains("ROS_MASTER_URI"))
                    ROS.ROS_MASTER_URI = (string)_vars["ROS_MASTER_URI"];
                else
                    //apparently it's not defined, so take a shot in the dark.
                    ROS.ROS_MASTER_URI = "http://localhost:11311";
            }

            if (string.IsNullOrEmpty(ROS.ROS_HOSTNAME))
            {
                IDictionary _vars;

                //check user env first, then machine if user doesn't have uri defined.
                if ((_vars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User)).Contains("ROS_HOSTNAME")
                    || (_vars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine)).Contains("ROS_HOSTNAME"))
                    ROS.ROS_HOSTNAME = (string)_vars["ROS_HOSTNAME"];
                else
                    //apparently it's not defined, so take a shot in the dark.
                    ROS.ROS_HOSTNAME = "localhost";
            }

            Console.WriteLine("RosMaster initializing...");
            Console.WriteLine("ROS_MASTER_URI = "+ROS.ROS_MASTER_URI);
            Console.WriteLine("ROS_HOSTNAME = " + ROS.ROS_HOSTNAME);

            Master master = new Master();
            master.start();

            while (master.ok())
            {
                Thread.Sleep(10);
            }

            master.stop();
        }
    }
}
