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
    public class Subscription
    {
        string name, md5sum, datatype;
        List<CallbackInfo> callbacks = new List<CallbackInfo>();
        List<PendingConnection> pendingconnections = new List<PendingConnection>();
        private bool dropped,shutting_down;
        public Subscription(string n, string md5s, string dt)
        {
            name = n;
            md5sum = md5s;
            datatype = dt;
        }
        public void drop()
        {
            throw new NotImplementedException();
        }
        public bool PubUpdatte(List<string> pubs)
        {
            throw new NotImplementedException();
        }
        public bool NegotiateConnection(string xmlrpc_uri)
        {
            client = new XmlRpcClient(xmlrpc_uri);
            tcpros_array.Set(0, "TCPROS");
            protos_array.Set(protos++, tcpros_array);
            Params.Set(0, name);
            Params.Set(1, this_node.Name);
            Params.Set(2, protos_array);
            return !client.IsNull && client.ExecuteNonBlock("requestTopic", Params);
        }
        public void headerReceived(PublisherLink link, Header header)
        {
            throw new NotImplementedException();
        }

        public bool IsDropped
        {
            get { return dropped; }
            set { dropped = value; }
        }

        private NetworkStream streamer;
        private List<PollHelper> pollset = new List<PollHelper>();
        private int protos;
        private XmlRpcClient client = null;
        private XmlRpcValue tcpros_array = new XmlRpcValue(), protos_array = new XmlRpcValue(), Params = new XmlRpcValue();
        public void ConnectAsync()
        {
            Console.WriteLine("Began asynchronous xmlrpc connection to [" + client.HostUri + "]");
            new Action(() =>
            {
                XmlRpcValue result = new XmlRpcValue();
                while (!client.ExecuteCheckDone(result))
                {
                    Console.WriteLine("NOT DONE YET!");
                }
                Console.WriteLine("HOLY SHIT I GOT SOMETHING BACK!");
                Console.WriteLine(result);
            }).BeginInvoke(null, null);
        }

        public void Shutdown()
        {
            if (client != null)
            {
                if (!client.IsNull)
                    client.Close();
                client = null;
            }
        }

        #region socket stuff and such
        private void WriteThatShit(byte[] towrite)
        {
            if (streamer != null)
            {
                streamer.Write(towrite, 0, towrite.Length);
            }
            string analbead = Encoding.ASCII.GetString(towrite);
            Console.WriteLine(analbead);
            Console.WriteLine();
            streamer.Flush();
        }

        private void AcceptRape(Socket tcp)
        {
            tcp.BeginAccept(EndAcceptRape, tcp);
        }

        private void EndAcceptRape(IAsyncResult iar)
        {
            Socket sock = (Socket)iar.AsyncState;
            pollset.Add(new PollHelper(sock.EndAccept(iar)));
        }

        private void udprape(IAsyncResult iar)
        {
            IPEndPoint from = null;
            UdpClient udp = (UdpClient)iar;
            udp.EndReceive(iar, ref from);
            Console.WriteLine("GOT SOME UDP SHIT FROM " + from + " WHAT THE FUCK?");
            udp.BeginReceive(udprape, udp);
        }
        #endregion

        #region Nested type: PollHelper

        public class PollHelper
        {
            private static int ID;
            public byte[] buffer;
            public int id;
            public EndPoint ipep;
            public Socket sock;

            public PollHelper(Socket s)
            {
                Console.WriteLine("MAKING NEW FUCKING SOCKET!");
                Console.WriteLine("Socket[" + (ID) + "] = \t " + s.LocalEndPoint + "==> " + s.RemoteEndPoint);
                sock = s;
                id = ID++;
                Poll();
            }

            public void Poll()
            {
                sock.BeginReceiveFrom
                    ((buffer = new byte[8192]), 0, 8192, SocketFlags.None, ref ipep,
                     EndReceive, null);
            }

            public void EndReceive(IAsyncResult iar)
            {
                int cnt = sock.EndReceiveFrom(iar, ref ipep);
                Console.WriteLine
                    ("Socket[" + id + "] received " + cnt + " bytes...\n" +
                     Encoding.ASCII.GetString(buffer));
                Poll();
            }
        }

        #endregion

        public void Dispose()
        {
            Shutdown();
        }
    }
}
