using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XmlRpc_Wrapper;
using m = Messages;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace EricIsAMAZING
{
    public class ConnectionManager
    {
        List<Connection> connections = new List<Connection>();
        List<Connection> dropped_connections = new List<Connection>();
        uint connection_id_counter;
        object connections_mutex = new object(), dropped_connections_mutex = new object(), connection_id_counter_mutex = new object();
        PollManager.Poll_Signal signal;
        public PollManager poll_manager;
        private static ConnectionManager _instance;
        public static ConnectionManager Instance()
        {
            if (_instance == null) _instance = new ConnectionManager();
            return _instance;
        }
        public uint GetNewConnectionID()
        {
            throw new NotImplementedException("IMPLEMENT THIS ID SHIT FOOL");
        }
        public void addConnection(Connection connection)
        {

        }
        public void Clear(Connection.DropReason reason)
        {

        }

        public uint TCPPort;
        public uint UDPPort;

        public TcpTransport tcpserver_transport;

        private void onConnectionDropped(Connection conn)
        {

        }

        private void removeDroppedConnections()
        {

        }

        public void shutdown()
        {

        }

        public void Start()
        {
            Console.WriteLine("... and connection manager started too.");
            poll_manager = PollManager.Instance();
            poll_manager.addPollThreadListener(removeDroppedConnections);

            tcpserver_transport = new TcpTransport(poll_manager.poll_set);

            if (!tcpserver_transport.listen(
        }
    }
}
