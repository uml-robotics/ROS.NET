using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Messages.std_msgs;
using Ros_CSharp;

namespace TransformTestConsoleWrapper
{
    class Program
    {

        static void Main(string[] args)
        {
            new TransformTestLib.TransformTest().TestMethod1();
            Console.ReadLine();
        }
    }
}
