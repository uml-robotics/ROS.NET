using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using XmlRpc_Wrapper;
using m = Messages;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;
using System.Threading;

namespace EricIsAMAZING
{
    public class PendingConnection : AsyncXmlRpcConnection
    {
        public XmlRpcClient xmlrpcclient;
        public UdpClient udpclient;
        public Subscription subscription;
        public string RemoteUri;

        public PendingConnection(XmlRpcClient client, UdpClient udp, Subscription s, string uri)
        {
            xmlrpcclient = client;
            udpclient = udp;
            subscription = s;
            RemoteUri = uri;
        }

        public virtual void addToDispatch(XmlRpcDispatch disp)
        {
            disp.AddSource(xmlrpcclient, (int)(XmlRpcDispatch.EventType.WritableEvent | XmlRpcDispatch.EventType.Exception));
        }

        public virtual void removeFromDispatch(XmlRpcDispatch disp)
        {
            disp.RemoveSource(xmlrpcclient);
        }

        public virtual bool check()
        {
            return true;
        }
    }
}
