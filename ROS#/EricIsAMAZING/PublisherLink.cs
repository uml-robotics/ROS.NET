using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EricIsAMAZING
{
    public abstract class PublisherLink
    {
        public class Stats
        {
            public UInt64 bytes_received, messages_received, drops;
            public Stats()
            {
            }
        }

        public PublisherLink(Subscription parent, string xmlrpc_uri)
        {
            this.parent = parent;
            XmlRpc_Uri = xmlrpc_uri;
        }
        public PublisherLink()
        {

        }

        public Subscription parent;
        public Stats stats;
        public string XmlRpc_Uri, CallerID, md5sum;
        public uint ConnectionID;
        public bool Latched;
        private Header header;
        public bool setHeader(Header h)
        {
            CallerID = (string)h.Values["callerid"];
            if (!h.Values.Contains("md5sum"))
                return false;
            md5sum = (string)h.Values["md5sum"];
            Latched = false;
            if (!h.Values.Contains("latching"))
                return false;
            if ((string)h.Values["latching"] == "1")
                Latched = true;
            ConnectionID = ConnectionManager.Instance().GetNewConnectionID();
            header = h;
            parent.headerReceived(this, header);
            return true;
        }
        public string TransportType { get { return "TCPROS"; /*lol... pwned*/ } }
        internal virtual void handleMessage(byte[] serializedmessagekinda, bool ser, bool nocopy)
        {
        }

        internal virtual void drop()
        {
        }
    }
}
