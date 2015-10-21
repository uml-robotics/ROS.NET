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
                    if (!Param.del(names.resolve(args[1]))) 
                            Console.WriteLine("Failed to delete "+args[1]);
                    break;
                case op.get:
                {
                    string s = null;
                    Param.get(args[1], ref s);
                    if (s != null)
                        Console.WriteLine(s);
                }
                    break;
                case op.list:
                {
                    foreach (string s in Param.list())
                        Console.WriteLine(s);
                }
                    break;
                case op.set:
                    Param.set(args[1], args[2]);
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
            IDictionary remappings;
            RemappingHelper.GetRemappings(ref args, out remappings);
            network.init(remappings);
            master.init(remappings);
            this_node.Init("", remappings, (int) (InitOption.AnonymousName | InitOption.NoRousout));
            Param.init(remappings);
            //ROS.Init(args, "");
            new Program(args).result();
        }
    }
}
