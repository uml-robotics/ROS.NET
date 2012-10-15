using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Messages;
using Ros_CSharp;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            ROS.start();

            TestMethod1();
            /*
            List<IRosMessage> msgs = new List<IRosMessage>();
            Array arr = Enum.GetValues(typeof(MsgTypes));
            MsgTypes[] all = arr as MsgTypes[];
            foreach (MsgTypes s in all)
            {
                if (s == MsgTypes.Unknown) continue;
                msgs.Add(IRosMessage.generate(s));
            }

            foreach (IRosMessage m in msgs)
            {
                byte[] res = m.Serialize();
                IRosMessage deserialized = IRosMessage.generate(m.msgtype);
                deserialized = deserialized.Deserialize(res);
            }*/
            Console.ReadLine();
        }
        static void TestMethod1()
        {   
            Publisher<IRosMessage> GenericPublisher;
            string TopicName;
            NodeHandle node = new NodeHandle();
            List<IRosMessage> msgs = new List<IRosMessage>();
            

            Array arr = Enum.GetValues(typeof(MsgTypes));
            MsgTypes[] all = arr as MsgTypes[];
            foreach (MsgTypes s in all)
            {
                if (s == MsgTypes.Unknown) continue;
                msgs.Add(IRosMessage.generate(s));
            }

            bool pass;
            foreach (IRosMessage m in msgs)
            {
                TopicName = m.GetType().FullName + "Pub";
                GenericPublisher = new Publisher<IRosMessage>(TopicName, m.MD5Sum, m.GetType().FullName, node, new SubscriberCallbacks());
                GenericPublisher.publish(m);

                byte[] res = m.Serialize();
                IRosMessage deserialized = IRosMessage.generate(m.msgtype);
                deserialized = deserialized.Deserialize(res);
                deserialized.Serialized = null;
                byte[] dres = deserialized.Serialize();
                pass = TestEqual(res, dres);
                if (!pass)
                {
                    Console.Error.WriteLine("\nTestEqual Failed: " + m.GetType().ToString() + " != " + deserialized.GetType().ToString());
                    Console.WriteLine(SerializationHelper.dumphex(res));
                    Console.WriteLine(SerializationHelper.dumphex(dres));
                }
                else
                    Console.Error.WriteLine("\nTestEqual Succeded: " + m.GetType().ToString() + " == " + deserialized.GetType().ToString());
            }
        }
        static bool TestEqual(byte[] original, byte[] copy)
        {
            if (original.Length != copy.Length)
            {
                Console.Error.WriteLine("\nSize Mismatch:");
                return false;
            }
            for (int i = 0; i < original.Length; i++)
            {
                if (original[i] != copy[i])
                {
                    return false;
                }
            }
            return true;
        }
    }
}
