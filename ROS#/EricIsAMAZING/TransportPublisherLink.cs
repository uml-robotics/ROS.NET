#region USINGZ

using System;
using System.Collections;
using System.Threading;
using Messages;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace EricIsAMAZING
{
    public class TransportPublisherLink : PublisherLink, IDisposable
    {
        public Connection connection;
        public bool dropping;
        private bool needs_retry;
        private DateTime next_retry;
        private TimeSpan retry_period;
        private Timer retry_timer;

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
                ROS.timer_manager.StopTimer(ref retry_timer);
                retry_timer.Dispose();
            }
            connection.drop(Connection.DropReason.Destructing);
        }

        #endregion

        public bool initialize(Connection connection)
        {
            this.connection = connection;
            connection.DroppedEvent += onConnectionDropped;
            if (connection.transport.getRequiresHeader())
            {
                connection.setHeaderReceivedCallback(onHeaderReceived);

                lock (parent)
                {
                    IDictionary header = new Hashtable();
                    header["topic"] = parent.name;
                    header["md5sum"] = parent.md5sum;
                    header["callerid"] = this_node.Name;
                    header["type"] = parent.datatype;
                    header["tcp_nodelay"] = "1";
                    connection.writeHeader(header, onHeaderWritten);
                }
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
                lock (parent)
                    parent.removePublisherLink(this);
        }

        private void onConnectionDropped(Connection conn, Connection.DropReason reason)
        {
            EDB.WriteLine("TransportPublisherLink: onConnectionDropped");
            if (dropping || conn != connection) return;
            lock (parent)
            {
                if (reason == Connection.DropReason.TransportDisconnect)
                {
                    string topic = parent != null ? parent.name : "unknown";
                    needs_retry = true;
                    next_retry = DateTime.Now.Add(retry_period);
                    if (retry_timer == null)
                        retry_period = TimeSpan.FromMilliseconds(100);
                    ROS.timer_manager.StartTimer(ref retry_timer, onRetryTimer,
                                                 (int) Math.Floor(retry_period.TotalMilliseconds), Timeout.Infinite);
                }
                else
                    drop();
            }
        }

        private bool onHeaderReceived(Connection conn, Header header)
        {
            if (conn != connection) return false;
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

        public void handleMessage(IRosMessage m, bool ser, bool nocopy)
        {
            stats.bytes_received += (ulong) m.Serialized.Length;
            stats.messages_received++;
            if (parent != null)
                lock (parent)
                    stats.drops += parent.handleMessage(m, ser, nocopy, connection.header.Values, this);
        }

        private void onHeaderWritten(Connection conn)
        {
            //do nothing
        }

        private void onMessageLength(Connection conn, byte[] buffer, int size, bool success)
        {
            if (retry_timer != null)
                ROS.timer_manager.RemoveTimer(ref retry_timer);
            if (!success)
            {
                if (connection != null)
                    connection.read(4, onMessageLength);
                return;
            }
            if (conn != connection || size != 4) return;
            int len = BitConverter.ToInt32(buffer, 0);
            if (len > 1000000000)
            {
                EDB.WriteLine("TransportPublisherLink: 1 GB message WTF?!");
                drop();
                return;
            }
            connection.read(len, onMessage);
        }

        private void onMessage(Connection conn, byte[] buffer, int size, bool success)
        {
            if (!success && conn == null || conn != connection) return;
            if (success)
            {
                string ty = "Messages." + parent.datatype.Replace("/", ".");
                Type t = TypeHelper.GetType(ty);
                if (t == null)
                    throw new Exception("string fail!");
                IRosMessage msg = new IRosMessage();// ROS.MakeMessage((MsgTypes)Enum.Parse(typeof(MsgTypes), parent.datatype.Replace("/", "__")));
                msg.Serialized = new byte[buffer.Length];
                Array.Copy(buffer, msg.Serialized, buffer.Length);
                handleMessage(msg, true, false);
            }
            if (success || !connection.transport.getRequiresHeader())
                connection.read(4, onMessageLength);
        }

        private void onRetryTimer(object o)
        {
            EDB.WriteLine("TransportPublisherLink: onRetryTimer");
            if (dropping) return;
            if (needs_retry && DateTime.Now.Subtract(next_retry).TotalMilliseconds < 0)
            {
                retry_period =
                    TimeSpan.FromSeconds((retry_period.TotalSeconds > 20) ? 20 : (2*retry_period.TotalSeconds));
                needs_retry = false;
                lock (parent)
                {
                    string topic = parent != null ? parent.name : "unknown";
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
}