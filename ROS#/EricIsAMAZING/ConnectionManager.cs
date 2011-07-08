#region USINGZ

using System;
using System.Collections.Generic;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace EricIsAMAZING
{
    public class ConnectionManager
    {
        private static ConnectionManager _instance;
        public uint TCPPort;
        public uint UDPPort;
        private uint connection_id_counter;
        private object connection_id_counter_mutex = new object();
        private List<Connection> connections = new List<Connection>();
        private object connections_mutex = new object();
        private List<Connection> dropped_connections = new List<Connection>();
        private object dropped_connections_mutex = new object();
        public PollManager poll_manager;
        private PollManager.Poll_Signal signal;
        public TcpTransport tcpserver_transport;

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

        private void onConnectionDropped(Connection conn)
        {
        }

        private void removeDroppedConnections()
        {
        }

        public void shutdown()
        {
        }

        public void tcpRosConnection(TcpTransport accepted)
        {
        }

        public void Start()
        {
            Console.WriteLine("... and connection manager started too.");
            poll_manager = PollManager.Instance();
            poll_manager.addPollThreadListener(removeDroppedConnections);

            tcpserver_transport = new TcpTransport(poll_manager.poll_set);

            if (!tcpserver_transport.listen(network.tcpros_server_port, 100, tcpRosConnection))
                throw new Exception("FUCK");
        }
    }
}