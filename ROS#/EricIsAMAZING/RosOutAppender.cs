using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Messages;
using XmlRpc_Wrapper;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

namespace EricIsAMAZING
{
    public class RosOutAppender
    {
        public RosOutAppender()
        {
            AdvertiseOptions<Messages.rosgraph_msgs.Log> ops = new AdvertiseOptions<Messages.rosgraph_msgs.Log>(names.resolve("/rosout"), 0);
            ops.latch = true;
            SubscriberCallbacks cbs = new SubscriberCallbacks();
            if (TopicManager.Instance.advertise(ops, cbs))
                Console.WriteLine("YAY!");
            else
                Console.WriteLine("BOO!");
        }

        public void Write(string m)
        {
            Messages.rosgraph_msgs.Log l = new Messages.rosgraph_msgs.Log();
            l.msg = m;
            l.level = 2;
            l.name = this_node.Name;
            l.file = "*.cs";
            l.function = "SOMECSFUNCTION";
            l.line = 666;
            l.topics = this_node.AdvertisedTopics().ToArray();
            TypedMessage<Messages.rosgraph_msgs.Log> MSG = new TypedMessage<Messages.rosgraph_msgs.Log>(l);
            TopicManager.Instance.publish<TypedMessage<Messages.rosgraph_msgs.Log>>(names.resolve("/rosout"), MSG);
        }
    }
}
