using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ros_CSharp;

namespace RosParamClient
{
    class Program
    {
        private enum op
        {
            set,
            get,
            has,
            del,
            list
        }

        private int _result = -1;

        public int result()
        {
            return _result;
        }

        private Program(string[] args)
        {
            op OP = op.list;
            if (args.Length == 0 || !Enum.TryParse<op>(args[0], true, out OP))
            {
                ShowUsage(0);
                return;
            }
            if (args.Length == 1 && OP != op.list)
            {
                ShowUsage(1);
                return;
            }
            switch (OP)
            {
                case op.del:
                    if (!Param.del(args[1])) 
                            Console.WriteLine("Failed to delete "+args[1]);
                    break;
                case op.get:
                    if (Param.has(args[1]))
                        Console.WriteLine("Parameter "+args[1]+" is not defined");
                    else
                    {
                        Console.WriteLine(Param.getParam(args[1]));
                        _result = 0;
                    }
                    break;
                case op.list:
                    foreach (string s in Param.list())
                        Console.WriteLine(s);
                    break;
                case op.set:
                    if (!Param.del(args[1]))
                        Console.WriteLine("Failed to delete "+args[1]);

                    break;
            }
        }

        private void ShowUsage(int p)
        {
            switch (p)
            {
                case 0:
                    Console.WriteLine("Valid operations:");
                    foreach (op o in (op[])Enum.GetValues(typeof(op)))
                        Console.WriteLine("\t"+o.ToString());
                    break;
                case 1:
                    Console.WriteLine("You must specify a param name for this rosparam operation.");
                    break;
            }
        }

        static void Main(string[] args)
        {   
            ROS.ROS_MASTER_URI = "http://192.168.121.129:11311";
            ROS.ROS_HOSTNAME = "192.168.121.1";
            IDictionary remappings;
            RemappingHelper.GetRemappings(ref args, out remappings);
            ROS.Init(args, "");
            new Program(args).result();
        }
    }
}
