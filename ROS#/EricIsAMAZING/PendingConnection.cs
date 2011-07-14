#region USINGZ

using XmlRpc_Wrapper;
using m = Messages.std_msgs;
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
        public XmlRpcValue stickaroundyouwench = null;
        public PendingConnection(XmlRpcClient client, Subscription s, string uri)
        {
            this.client = client;
            parent = s;
            RemoteUri = uri;
        }

        public override void addToDispatch(XmlRpcDispatch disp)
        {
            if (disp == null)
                return;
            disp.AddSource(client, (int) (XmlRpcDispatch.EventType.WritableEvent | XmlRpcDispatch.EventType.Exception));
        }

        public override void removeFromDispatch(XmlRpcDispatch disp)
        {
            disp.RemoveSource(client);
        }

        public override bool check()
        {
            if (parent == null) 
                return false;
            if (stickaroundyouwench == null)
                stickaroundyouwench = new XmlRpcValue();
            if (client.ExecuteCheckDone(stickaroundyouwench))
            {
                parent.pendingConnectionDone(this, stickaroundyouwench.instance);
                return true;
            }
            return false;
        }
    }
}