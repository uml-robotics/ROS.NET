// File: TransportPublisherLink.cs
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

#region USINGZ

using System;
using System.Collections;
using Messages;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace Ros_CSharp
{
    public class TransportPublisherLink : PublisherLink, IDisposable
    {
        public Connection connection;
        public bool dropping;
        private bool needs_retry;
        private DateTime next_retry;
        private TimeSpan retry_period;
        private WrappedTimer retry_timer;

        public TransportPublisherLink(Subscription parent, string xmlrpc_uri) : base(parent, xmlrpc_uri)
        {
            needs_retry = false;
            dropping = false;
        }

        #region IDisposable Members

        public void Dispose()
        {
            dropping = true;
            if (retry_timer != null)
            {
                ROS.timer_manager.RemoveTimer(ref retry_timer);
            }
            connection.drop(Connection.DropReason.Destructing);
        }

        #endregion

        public bool initialize(Connection connection)
        {
#if DEBUG
            EDB.WriteLine("Init transport publisher link: " + parent.name);
#endif
            this.connection = connection;
            connection.DroppedEvent += onConnectionDropped;
            if (connection.transport.getRequiresHeader())
            {
                connection.setHeaderReceivedCallback(onHeaderReceived);
                
                IDictionary header = new Hashtable();
                header["topic"] = parent.name;
                header["md5sum"] = parent.md5sum;
                header["callerid"] = this_node.Name;
                header["type"] = parent.datatype;
                header["tcp_nodelay"] = "1";
                connection.writeHeader(header, onHeaderWritten);
            }
            else
            {
                connection.read(4, onMessageLength);
            }
            return true;
        }

        public override void drop()
        {
            dropping = true;
            connection.drop(Connection.DropReason.Destructing);
            if (parent != null)
                parent.removePublisherLink(this);
            else
            {
                EDB.WriteLine("TransportPublisherLink met an untimely demise.");
            }
        }

        private void onConnectionDropped(Connection conn, Connection.DropReason reason)
        {
#if DEBUG
            EDB.WriteLine("TransportPublisherLink: onConnectionDropped -- " + reason);
#endif
            if (dropping || conn != connection)
                return;
            if (reason == Connection.DropReason.TransportDisconnect)
            {
                needs_retry = true;
                next_retry = DateTime.Now.Add(retry_period);
                if (retry_timer == null)
                {
                    retry_timer = ROS.timer_manager.StartTimer(onRetryTimer, 100);
                }
                else
                {
                    retry_timer.Restart();
                }
            }
            else
            {
                if (reason == Connection.DropReason.HeaderError)
                {
                    EDB.WriteLine("SOMETHING BE WRONG WITH THE HEADER FOR: " +
                                    (parent != null ? parent.name : "unknown"));
                }
                drop();
            }
        }

        private bool onHeaderReceived(Connection conn, Header header)
        {
            ScopedTimer.Ping();
            if (conn != connection)
                return false;
            if (!setHeader(header))
            {
                drop();
                return false;
            }
            if (retry_timer != null)
                ROS.timer_manager.RemoveTimer(ref retry_timer);
            connection.read(4, onMessageLength);
            return true;
        }

        public void handleMessage<T>(T m, bool ser, bool nocopy) where T : IRosMessage, new()
        {
            stats.bytes_received += (ulong) m.Serialized.Length;
            stats.messages_received++;
            m.connection_header = getHeader().Values;
            if (parent != null)
                stats.drops += parent.handleMessage(m, ser, nocopy, connection.header.Values, this);
            else
                Console.WriteLine("merrr?");
        }

        private bool onHeaderWritten(Connection conn)
        {
            return true;
        }

        private bool onMessageLength(Connection conn, byte[] buffer, int size, bool success)
        {
            ScopedTimer.Ping();
            if (retry_timer != null)
                ROS.timer_manager.RemoveTimer(ref retry_timer);
            if (!success)
            {
                if (connection != null)
                    connection.read(4, onMessageLength);
                return true;
            }
            if (conn != connection || size != 4)
                return false;
            int len = BitConverter.ToInt32(buffer, 0);
            if (len > 1000000000)
            {
                EDB.WriteLine("TransportPublisherLink: 1 GB message WTF?!");
                drop();
                return false;
            }
            connection.read(len, onMessage);
            return true;
        }

        private bool onMessage(Connection conn, byte[] buffer, int size, bool success)
        {
            ScopedTimer.Ping();
            if (!success || conn == null || conn != connection) return false;
            if (success)
            {
                IRosMessage msg = IRosMessage.generate(parent.msgtype);
                msg.Serialized = buffer;
                msg.connection_header = getHeader().Values;
                handleMessage(msg, true, false);
            }
            if (success || !connection.transport.getRequiresHeader())
                connection.read(4, onMessageLength);
            return true;
        }

        private void onRetryTimer(object o)
        {
#if DEBUG
            EDB.WriteLine("TransportPublisherLink: onRetryTimer");
#endif
            if (dropping) return;
            if (needs_retry && DateTime.Now.Subtract(next_retry).TotalMilliseconds < 0)
            {
                retry_period =
                    TimeSpan.FromSeconds((retry_period.TotalSeconds > 20) ? 20 : (2*retry_period.TotalSeconds));
                needs_retry = false;
                TcpTransport old_transport = connection.transport;
                string host = old_transport.connected_host;
                int port = old_transport.connected_port;

                TcpTransport transport = new TcpTransport();
                if (transport.connect(host, port))
                {
                    Connection conn = new Connection();
                    conn.initialize(transport, false, null);
                    initialize(conn);
                    ConnectionManager.Instance.addConnection(conn);
                }
            }
        }
    }
}