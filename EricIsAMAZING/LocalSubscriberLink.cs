#region Using

using System;
using System.Collections;
using System.Threading;
using Messages;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace Ros_CSharp
{
    public class LocalSubscriberLink : SubscriberLink, IDisposable
    {
        LocalPublisherLink subscriber;
        object drop_mutex = new object();
        bool dropped;

        public LocalSubscriberLink(Publication pub)
        {
            parent = pub;
            topic = parent.Name;
        }

        public void setSubscriber(LocalPublisherLink pub_link)
        {
            subscriber = pub_link;
            connection_id = ConnectionManager.Instance.GetNewConnectionID();
            destination_caller_id = this_node.Name;
        }

        public override void enqueueMessage(IRosMessage msg, bool ser, bool nocopy)
        {
            lock (drop_mutex)
            {
                if (dropped) return;
            }

            if (subscriber != null)
                subscriber.handleMessage(msg, ser, nocopy);
        }


        public new string TransportType
        {
            get { return "INTRAPROCESS"; /*lol... pwned*/ }
        }

        #region IDisposable Members

        public void Dispose()
        {
        }

        #endregion

        public override void drop()
        {
            lock (drop_mutex)
            {
                if (dropped) return;
                dropped = true;
            }
            if (subscriber != null)
            {
                subscriber.drop();
            }

            lock (parent)
                parent.removeSubscriberLink(this);
        }


        public override void getPublishTypes(ref bool ser, ref bool nocopy, ref MsgTypes mt)
        {
            lock (drop_mutex)
            {
                if (dropped)
                {
                    ser = false;
                    nocopy = false;
                    return;
                }
            }
            subscriber.getPublishTypes(ref ser, ref nocopy, ref mt);
        }
    }
}