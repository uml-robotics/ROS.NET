using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Messages;
using XmlRpc_Wrapper;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

namespace EricIsAMAZING
{
    public class RosOutAppender
    {
        public bool shutting_down;
        public Thread publish_thread;
        public object queue_mutex = new object();
        public Queue<IRosMessage> log_queue = new Queue<IRosMessage>();

        public RosOutAppender()
        {
            publish_thread = new Thread(logThread);
            publish_thread.IsBackground = true;
            publish_thread.Start();
            AdvertiseOptions<Messages.rosgraph_msgs.Log> ops = new AdvertiseOptions<Messages.rosgraph_msgs.Log>(names.resolve("/rosout"), 0);
            ops.latch = true;
            SubscriberCallbacks cbs = new SubscriberCallbacks();
            TopicManager.Instance.advertise<Messages.rosgraph_msgs.Log>(ops, cbs);
        }

        public void shutdown()
        {
            lock (queue_mutex)
            {
                shutting_down = true;
                publish_thread.Join();
            }
        }

        public void Append(string m)
        {
            Messages.rosgraph_msgs.Log l = new Messages.rosgraph_msgs.Log();
            l.msg = m;
            l.level = 8;
            l.name = this_node.Name;
            l.file = "*.cs";
            l.function = "SOMECSFUNCTION";
            l.line = 666;
            l.topics = this_node.AdvertisedTopics().ToArray();
            TypedMessage<Messages.rosgraph_msgs.Log> MSG = new TypedMessage<Messages.rosgraph_msgs.Log>(l);
            lock(queue_mutex)
                log_queue.Enqueue(MSG);
        }

        public void logThread()
        {
            while (!shutting_down)
            {
                Queue<IRosMessage> localqueue = null;
                lock (queue_mutex)
                {
                    if (shutting_down) return;
                    localqueue = new Queue<IRosMessage>(log_queue);
                    if (shutting_down) return;
                    log_queue.Clear();
                    if (shutting_down) return;
                }
                if (shutting_down) return;
                while (localqueue.Count > 0)
                {
                    if (shutting_down) return;
                    IRosMessage msg = localqueue.Dequeue();
                    if (shutting_down) return;
                    TopicManager.Instance.publish(names.resolve("/rosout"), msg);
                    if (shutting_down) return;
                }
            }
        }
    }
}
