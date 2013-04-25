#region USINGZ

using System;
using XmlRpc_Wrapper;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace Ros_CSharp
{
    public class PendingConnection : AsyncXmlRpcConnection, IDisposable
    {
        public bool NEVERAGAIN;
        public string RemoteUri;
        public XmlRpcClient client;
        public Subscription parent;
        //public XmlRpcValue stickaroundyouwench = null;
        public PendingConnection(XmlRpcClient client, Subscription s, string uri)
        {
            this.client = client;
            parent = s;
            RemoteUri = uri;
        }

        #region IDisposable Members

        public void Dispose()
        {
            client.Dispose();
            client = null;
        }

        #endregion

        public override void addToDispatch(XmlRpcDispatch disp)
        {
            if (disp == null)
                return;
            if (!check())
                return;
            client.SegFault();
            disp.AddSource(client, (int) (XmlRpcDispatch.EventType.WritableEvent | XmlRpcDispatch.EventType.Exception));
        }

        public override void removeFromDispatch(XmlRpcDispatch disp)
        {
            client.SegFault();
            disp.RemoveSource(client);
        }

        public override bool check()
        {
            client.SegFault();
            XmlRpcValue chk = new XmlRpcValue();
            if (parent == null)
                return false;
            bool res = client.IsConnected;
            if (res == false)
                EDB.WriteLine("DEAD MASTER DETECTED!");
            else
            {
                res &= client.ExecuteCheckDone(chk);
                if (res)
                    parent.pendingConnectionDone(this, chk.instance);
            }
            if (client.ExecuteCheckDone(chk))
            {
                parent.pendingConnectionDone(this, chk.instance);
                return true;
            }
            return false;
        }
    }
}