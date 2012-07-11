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
            SerializationHelper.ShowDeserializationSteps();
            foreach (MsgTypes mt in Enum.GetValues(typeof(MsgTypes)))
            {
                if (mt == MsgTypes.Unknown) continue;
                byte[] PWNED = new byte[0];
                IRosMessage.generate(mt).Deserialize(PWNED);
            }
        }
    }
}
