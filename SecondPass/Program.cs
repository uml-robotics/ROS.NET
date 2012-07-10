using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Messages;
using Ros_CSharp;

namespace SecondPass
{
    class Program
    {
        static void Main(string[] args)
        {
            foreach (MsgTypes mt in Enum.GetValues(typeof(MsgTypes)))
            {
                if (mt == MsgTypes.Unknown) continue;
                Console.WriteLine("********** "+mt+" ************");
                Console.WriteLine(IRosMessage.generate(mt).MessageDefinition);
                Console.WriteLine("md5: "+IRosMessage.generate(mt).MD5Sum);
                Console.WriteLine();
            }
        }
    }
}
