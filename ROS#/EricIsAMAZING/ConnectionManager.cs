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

        public int TCPPort
        {
            get { return tcpserver_transport.server_port; }
        }
        
        private uint connection_id_counter;
        private object connection_id_counter_mutex = new object();
        private List<Connection> connections = new List<Connection>();
        private object connections_mutex = new object();
        private List<Connection> dropped_connections = new List<Connection>();
        private object dropped_connections_mutex = new object();
        public PollManager poll_manager;
        private PollManager.Poll_Signal signal;
        public TcpTransport tcpserver_transport;

        public void poll_conn()
        {

        }

        public static ConnectionManager Instance
        {
            get
            {
                if (_instance == null) _instance = new ConnectionManager();
                return _instance;
            }
        }

        public uint GetNewConnectionID()
        {
            lock (connection_id_counter_mutex)
            {
                return connection_id_counter++;
            }
        }

        public void addConnection(Connection connection)
        {
            lock (connections_mutex)
            {
                connections.Add(connection);
                connection.DroppedEvent += onConnectionDropped;
            }
        }

        public void Clear(Connection.DropReason reason)
        {
            List<Connection> local_connections = null;
            lock (connections_mutex)
            {
                local_connections = new List<Connection>(connections);
                connections.Clear();
            }
            foreach (Connection c in local_connections)
            {
                c.drop(reason);
            }

            lock (dropped_connections_mutex)
                dropped_connections.Clear();
        }

        private void onConnectionDropped(Connection conn, Connection.DropReason r)
        {
            lock (dropped_connections_mutex)
                dropped_connections.Add(conn);
        }

        private void removeDroppedConnections()
        {
            List<Connection> local_dropped = null;
            lock (dropped_connections_mutex)
            {
                local_dropped = new List<Connection>(dropped_connections);
                dropped_connections.Clear();
            }
            lock (connections_mutex)
            {
                foreach (Connection c in local_dropped)
                {
                    connections.Remove(c);
                }
            }
        }

        public void shutdown()
        {
            if (tcpserver_transport != null)
            {
                tcpserver_transport.close();
                tcpserver_transport = null;
            }

            poll_manager.removePollThreadListener(poll_conn);

            Clear(Connection.DropReason.Destructing);
        }

        public void tcpRosAcceptConnection(TcpTransport transport)
        {
            string client_uri = transport.ClientURI;
            Connection conn = new Connection();
            addConnection(conn);
            conn.initialize(transport, true, onConnectionHeaderReceived);
        }

        public bool onConnectionHeaderReceived(Connection conn, Header header)
        {
            bool ret = false;
            string val = "";
            if (header.Values.Contains("topic"))
            {
                val = (string)header.Values["topic"];
                TransportSubscriberLink sub_link = new TransportSubscriberLink();
                sub_link.initialize(conn);
                ret = sub_link.handleHeader(header);
            }
            else if (header.Values.Contains("service"))
            {
                throw new Exception("IMPLEMENT SERVICECLIENT LINKS!");
            }
            else
            {
                Console.WriteLine("got a connection for a type other than topic or service from [" + conn.RemoteString + "]. Fail.");
                return false;
            }
            return ret;
        }


        public void Start()
        {
            Console.WriteLine("... and connection manager started too.");
            poll_manager = PollManager.Instance;
            poll_manager.addPollThreadListener(removeDroppedConnections);

            tcpserver_transport = new TcpTransport(poll_manager.poll_set);

            if (!tcpserver_transport.listen(network.tcpros_server_port, 100, tcpRosAcceptConnection))
                throw new Exception("FUCK");
        }
    }
}