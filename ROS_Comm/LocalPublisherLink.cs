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
    public class LocalPublisherLink : PublisherLink, IDisposable
    {
        bool dropped;
        object drop_mutex = new object();
        LocalSubscriberLink publisher;

        public LocalPublisherLink(Subscription parent, string xmlrpc_uri) : base(parent,xmlrpc_uri)
        {
        }

        public void setPublisher(LocalSubscriberLink pub_link)
        {
            lock (parent)
            {
                IDictionary header = new Hashtable();
                header["topic"] = parent.name;
                header["md5sum"] = parent.md5sum;
                header["callerid"] = this_node.Name;
                header["type"] = parent.datatype;
                header["tcp_nodelay"] = "1";
                setHeader(new Header { Values = header });
            }
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
            

            if (publisher != null)
            {
                publisher.drop();
            }

            lock (parent)
                parent.removePublisherLink(this);
        }

        public void handleMessage<T>(T m, bool ser, bool nocopy) where T : IRosMessage, new()
        {
            stats.bytes_received += (ulong) m.Serialized.Length;
            stats.messages_received++;
            byte[] tmp = new byte[m.Serialized.Length - 4];
            Array.Copy(m.Serialized, 4, tmp, 0, m.Serialized.Length - 4);
            m.Serialized = tmp;
            if (parent != null)
                lock (parent)
                    stats.drops += parent.handleMessage(m, ser, nocopy, m.connection_header, this);
        }

        public new string TransportType
        {
            get { return "INTRAPROCESS"; /*lol... pwned*/ }
        }

        private void onMessage(Connection conn, ref byte[] buffer, uint size, bool success)
        {
            if (success)
            {
                IRosMessage msg = IRosMessage.generate(parent.msgtype);
                msg.Serialized = new byte[buffer.Length];
                msg.connection_header = getHeader().Values;
                Array.Copy(buffer, msg.Serialized, buffer.Length);
                handleMessage(msg, true, false);
            }
        }

        public void getPublishTypes(ref bool ser, ref bool nocopy, ref MsgTypes mt)
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
            if (parent != null)
                lock (parent)
                {
                    parent.getPublishTypes(ref ser, ref nocopy, ref mt);
                }
            else
            {
                ser = true;
                nocopy = false;
            }
        }
    }
}