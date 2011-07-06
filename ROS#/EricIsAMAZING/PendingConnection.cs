#region USINGZ

using XmlRpc_Wrapper;
using m = Messages;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace EricIsAMAZING
{
    public class PendingConnection : AsyncXmlRpcConnection
    {
        public string RemoteUri;
        public XmlRpcClient client;
        public Subscription parent;

        public PendingConnection(XmlRpcClient client, Subscription s, string uri)
        {
            this.client = client;
            parent = s;
            RemoteUri = uri;
        }

        public virtual void addToDispatch(XmlRpcDispatch disp)
        {
            disp.AddSource(client, (int) (XmlRpcDispatch.EventType.WritableEvent | XmlRpcDispatch.EventType.Exception));
        }

        public virtual void removeFromDispatch(XmlRpcDispatch disp)
        {
            disp.RemoveSource(client);
        }

        public virtual bool check()
        {
            if (parent == null) return true;
            XmlRpcValue result = new XmlRpcValue();
            if (client.ExecuteCheckDone(result))
            {
                parent.pendingConnectionDone(this, result);
                return true;
            }
            return false;
        }
    }
}