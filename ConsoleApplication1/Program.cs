using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Messages;
using Ros_CSharp;
using System.Threading;

namespace ConsoleApplication1
{
    class Program
    {
        static int TopicCounter = 0;
        static void Main(string[] args)
        {
            ROS.ROS_HOSTNAME = "10.0.2.47";
            ROS.ROS_MASTER_URI = "http://10.0.2.42:11311/";
            ROS.Init(new string[0], "TestPubs");

            
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
        }
        static void TestMethod1()
        {
            NodeHandle node = new NodeHandle();
            List<IRosMessage> msgs = new List<IRosMessage>();
            List<Publisher<IRosMessage>> pubs = new List<Publisher<IRosMessage>>();
            List<Subscriber<IRosMessage>> subs = new List<Subscriber<IRosMessage>>();
            

            new Thread(() =>
            {
                ROS.spin();
            }).Start();
            new Thread(() =>
                {
                    while (ROS.ok)
                    {
                        ROS.spinOnce(node);
                        Thread.Sleep(1);
                    }
                }).Start();

            Array arr = Enum.GetValues(typeof(MsgTypes));
            MsgTypes[] all = arr as MsgTypes[];
            foreach (MsgTypes s in all)
            {
                if (s == MsgTypes.Unknown || Enum.GetNames(typeof(SrvTypes)).Count((S) => s.ToString().Contains(S)) >= 1) continue;
                IRosMessage m = IRosMessage.generate(s);
                msgs.Add(m);
                AdvertiseOptions<IRosMessage> Pubops = new AdvertiseOptions<IRosMessage>(m.msgtype.ToString().Replace("__", "/").Split('/').Last(), 1, m.MD5Sum, m.msgtype.ToString().Replace("__", "/"), m.MessageDefinition);                                
                
                
               // subs.Add(
               //     ((ISubscriber)typeof(NodeHandle).GetMethod("subscribe", new Type[]{typeof(string), typeof(int), typeof(Callback<>).MakeGenericType(ROS.MakeMessage(s).GetType())}).MakeGenericMethod(
               //         ROS.MakeMessage(s).GetType()).Invoke(node, new object[]{
               ///*topic*/        m.msgtype.ToString().Replace("__", "/").Split('/').Last(),
               ///*queue size*/   1, 
               ///*callback-ish*/ Activator.CreateInstance(typeof(Callback<>).MakeGenericType(ROS.MakeMessage(s).GetType()), (object)(new Action<IRosMessage>((M)=>SubCall(M))))})));
               // //ISubscriber sub = (ISubscriber)Activator.CreateInstance(typeof(Subscriber<>).MakeGenericType(ROS.MakeMessage(s).GetType()),  });
               // //CallbackInterface cb = 

                
                //var SubOps = Activator.CreateInstance(typeof(SubscribeOptions<>).MakeGenericType(m.GetType()));
                pubs.Add(node.advertise<IRosMessage>(Pubops));
                //subs.Add(node.subscribe(SubOps));
            }
           

            Thread.Sleep(1000);
            bool pass;
            for (int i = 0; i < msgs.Count; i++)
            {
                IRosMessage m = msgs[i];
                Publisher<IRosMessage> pub = pubs[i];
                string[] names = m.GetType().FullName.Split('.');

                pub.publish(m);
                TopicCounter++;

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
            Thread.Sleep(1000);
            while (TopicCounter > 0) 
            {
                if(TopicCounter==89)
                    pubs[0].publish(msgs[0]);
                Thread.Sleep(100);
            }
            Thread.Sleep(10000);
            ROS.shutdown();
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
        static void SubCall(object o)
        {
            TopicCounter--;
        }
    
    }
}
