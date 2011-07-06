#region USINGZ

using Messages;
using m = Messages;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace EricIsAMAZING
{
    public class SingleSubscriberPublisher
    {
        public SubscriberLink link;

        public SingleSubscriberPublisher(SubscriberLink link)
        {
            this.link = link;
        }

        public string topic
        {
            get { return link.topic; }
        }

        public string subscriber_name
        {
            get { return link.destination_caller_id; }
        }

        public void publish<M>(M message) where M : IRosMessage, new()
        {
            link.enqueueMessage(message, true, true);
        }
    }
}