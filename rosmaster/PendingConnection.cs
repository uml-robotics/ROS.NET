#region Using

using System;
using XmlRpc_Wrapper;


#endregion

namespace rosmaster
{
    public class PendingConnection : AsyncXmlRpcConnection, IDisposable
    {
        public bool NEVERAGAIN;
        public string RemoteUri;
        public XmlRpcClient client;

        //public XmlRpcValue stickaroundyouwench = null;
        public PendingConnection(XmlRpcClient client, string uri)
        {
            this.client = client;
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
            disp.AddSource(client, (XmlRpcDispatch.EventType.WritableEvent | XmlRpcDispatch.EventType.Exception));
        }

        public override void removeFromDispatch(XmlRpcDispatch disp)
        {
            disp.RemoveSource(client);
        }

        public override bool check()
        {
            XmlRpcValue chk = new XmlRpcValue();

            bool res = client.IsConnected;
            if (res == false)
                Console.WriteLine("DEAD MASTER DETECTED!");
            else
            {
                res &= client.ExecuteCheckDone(chk);
            }
            if (client.ExecuteCheckDone(chk))
            {
                return true;
            }
            return false;
        }
    }
}