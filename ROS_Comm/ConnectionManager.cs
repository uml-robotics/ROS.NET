// File: ConnectionManager.cs
// Project: ROS_C-Sharp
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 04/28/2015
// Updated: 02/10/2016

#define TCPSERVER

#region USINGZ

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace Ros_CSharp
{
    public class ConnectionManager
    {
        private static ConnectionManager _instance;
        private static object singleton_mutex = new object();
        private uint connection_id_counter;
        private object connection_id_counter_mutex = new object();
        private List<Connection> connections = new List<Connection>();
        private object connections_mutex = new object();
        private List<Connection> dropped_connections = new List<Connection>();
        private object dropped_connections_mutex = new object();
        public PollManager.Poll_Signal poll_conn;
        public PollManager poll_manager;
#if TCPSERVER
        public TcpListener tcpserver_transport;
#else
        public TcpTransport tcpserver_transport;
#endif


        public int TCPPort
        {
            get
            {
#if TCPSERVER
                if (tcpserver_transport == null || tcpserver_transport.LocalEndpoint == null)
                    return -1;
                return ((IPEndPoint) tcpserver_transport.LocalEndpoint).Port;
#else
                if (tcpserver_transport == null || tcpserver_transport.LocalEndPoint == null)
                    return -1;
                return ((IPEndPoint)tcpserver_transport.LocalEndPoint).Port;
#endif
            }
        }

        public static ConnectionManager Instance
        {
#if !TRACE
            [DebuggerStepThrough]
#endif
                get
            {
                if (_instance == null)
                {
                    lock (singleton_mutex)
                    {
                        if (_instance == null)
                            _instance = new ConnectionManager();
                    }
                }
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
            removeDroppedConnections();
            List<Connection> local_connections = null;
            lock (connections_mutex)
            {
                local_connections = new List<Connection>(connections);
                connections.Clear();
            }
            foreach (Connection c in local_connections)
            {
                if (!c.dropped)
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
                    Console.WriteLine("Removing dropped connection: " + c.CallerID);
                    connections.Remove(c);
                }
            }
        }

        public void shutdown()
        {
            acceptor.Stop();
            if (tcpserver_transport != null)
            {
#if TCPSERVER
                tcpserver_transport.Stop();
                tcpserver_transport = null;
#else
                tcpserver_transport.close();
                tcpserver_transport = null;
#endif
            }
            poll_manager.removePollThreadListener(poll_conn);

            Clear(Connection.DropReason.Destructing);
        }

        public void tcpRosAcceptConnection(TcpTransport transport)
        {
            Connection conn = new Connection();
            addConnection(conn);
            conn.initialize(transport, true, onConnectionHeaderReceived);
        }

        public bool onConnectionHeaderReceived(Connection conn, Header header)
        {
            bool ret = false;
            if (header.Values.Contains("topic"))
            {
                TransportSubscriberLink sub_link = new TransportSubscriberLink();
                ret = sub_link.initialize(conn);
                ret &= sub_link.handleHeader(header);
            }
            else if (header.Values.Contains("service"))
            {
                IServiceClientLink iscl = new IServiceClientLink();
                ret = iscl.initialize(conn);
                ret &= iscl.handleHeader(header);
            }
            else
            {
                EDB.WriteLine("got a connection for a type other than topic or service from [" + conn.RemoteString +
                              "]. Fail.");
                return false;
            }
            //EDB.WriteLine("CONNECTED [" + val + "]. WIN.");
            return ret;
        }

#if TCPSERVER
        public void CheckAndAccept(object nothing)
        {
            while (tcpserver_transport != null && tcpserver_transport.Pending())
            {
                tcpRosAcceptConnection(new TcpTransport(
                    tcpserver_transport.
#if TCPSERVER
                        AcceptSocket()
#else
                    accept()
#endif
                    , poll_manager.poll_set));
            }
        }

        private WrappedTimer acceptor;
#endif

        public void Start()
        {
            poll_manager = PollManager.Instance;
            poll_conn = removeDroppedConnections;
            poll_manager.addPollThreadListener(poll_conn);

#if TCPSERVER
            tcpserver_transport = new TcpListener(IPAddress.Any, network.tcpros_server_port);
            tcpserver_transport.Start(10);
            acceptor = ROS.timer_manager.StartTimer(CheckAndAccept, 100, 100);
#else
            tcpserver_transport = new TcpTransport(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp), poll_manager.poll_set);
            new Thread(() =>
                {
                    while (true)
                    {
                        bool diedinfire;
                        lock (fierydeathmutex)
                            diedinfire = _diaf;
                        if (diedinfire) break;
                        TcpTransport newguy = tcpserver_transport.accept();
                        if (newguy != null)
                            tcpRosAcceptConnection(newguy);
                        else
                            Thread.Sleep(90);
                        Thread.Sleep(10);
                    }
                }).Start();
#endif
        }
    }
}