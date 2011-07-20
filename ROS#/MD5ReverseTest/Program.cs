using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EricIsAMAZING;

namespace MD5ReverseTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string datatype = "std_msgs/String";
            string topic = "chatter";
            /*Console.WriteLine("MD5SUM OF " + datatype + " = " + new MD5(datatype).ToString());
            Console.WriteLine("MD5SUM OF " + topic + " = " + new MD5("/" + topic).ToString());
            Console.WriteLine("MD5SUM OF /" + topic + " = " + new MD5(topic).ToString());
            Console.WriteLine("MD5SUM OF /ERICRULZ = " + new MD5("/ERICRULZ").ToString());
            Console.WriteLine("MD5SUM OF ERICRULZ = " + new MD5("ERICRULZ").ToString());*/
            //Console.WriteLine(MD5.Reverse("128ae49ebb65f1a2ec9baf65647e23c"));
            Console.WriteLine(MD5.Reverse(MD5.Sum("ABCDE")));
            Console.ReadLine();
        }
    }
}
