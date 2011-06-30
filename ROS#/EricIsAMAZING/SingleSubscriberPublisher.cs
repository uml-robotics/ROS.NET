using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XmlRpc_Wrapper;
using m = Messages;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;
using System.Threading;

namespace EricIsAMAZING
{
    public class SingleSubscriberPublisher
    {
        public string topic { get { return link.topic; } }
        public string subscriber_name { get { return link.destination_caller_id; }} 
        public SubscriberLink link;
        public SingleSubscriberPublisher(SubscriberLink link)
        {
            this.link = link;
        }

        public void publish<M>(M message) where M : m.IRosMessage
        {
            link.enqueueMessage(message, true, true);
        }
    }
}
