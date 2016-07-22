// File: TcpTransport.cs
// Project: ROS_C-Sharp
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 09/01/2015
// Updated: 02/10/2016

#region USINGZ

using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Socket = Ros_CSharp.CustomSocket.Socket;

#endregion

namespace Ros_CSharp
{
    public class TcpTransport
    {
        #region Delegates

        public delegate void AcceptCallback(TcpTransport trans);

        public delegate void DisconnectFunc(TcpTransport trans);

        public delegate void HeaderReceivedFunc(TcpTransport trans, Header header);

        public delegate void ReadFinishedFunc(TcpTransport trans);

        public delegate void WriteFinishedFunc(TcpTransport trans);

        #endregion

        #region Flags enum

        public enum Flags
        {
            SYNCHRONOUS = 1 << 0
        }

        #endregion

        private const int bytesperlong = 4; // 32 / 8
        private const int bitsperbyte = 8;
        public const int POLLERR = 0x008;
        public const int POLLHUP = 0x010;
        public const int POLLNVAL = 0x020;
        public const int POLLIN = 0x001;
        public const int POLLOUT = 0x004;

        public static bool use_keepalive;
        public IPEndPoint LocalEndPoint;
        public string _topic;
        public string cached_remote_host = "";
        public object close_mutex = new object();
        public bool closed;
        public string connected_host;
        public int connected_port;
        public int events;
        public bool expecting_read;
        public bool expecting_write;
        public int flags;
        public bool is_server;
        public bool no_delay;
        public PollSet poll_set;
        public IPEndPoint server_address;
        public int server_port = -1;

        private Socket sock;

        public TcpTransport()
        {
        }


        public TcpTransport(System.Net.Sockets.Socket s, PollSet pollset) : this(s, pollset, 0)
        {
        }

        public TcpTransport(System.Net.Sockets.Socket s, PollSet pollset, int flags) : this(pollset, flags)
        {
            setSocket(new Socket(s));
        }

        public TcpTransport(PollSet pollset)
            : this(pollset, 0)
        {
        }

        public TcpTransport(PollSet pollset, int flags) : this()
        {
            if (pollset != null)
            {
                poll_set = pollset;
                poll_set.DisposingEvent += close;
            }
            else
            {
                EDB.WriteLine("Uh oh! Null pollset in tcptransport ctor");
            }
            this.flags = flags;
        }

        public string ClientURI
        {
#if !TRACE
            [DebuggerStepThrough]
#endif
                get
            {
                if (connected_host == null || connected_port == 0)
                    return "[NOT CONNECTED]";
                return "http://" + connected_host + ":" + connected_port + "/";
            }
        }

        public string Topic
        {
            get { return _topic != null ? _topic : "?!?!?!"; }
        }

        public virtual bool getRequiresHeader()
        {
            return true;
        }

        public event AcceptCallback accept_cb;
        public event DisconnectFunc disconnect_cb;
        public event WriteFinishedFunc write_cb;
        public event ReadFinishedFunc read_cb;

        public bool setNonBlocking()
        {
            if ((flags & (int) Flags.SYNCHRONOUS) == 0)
            {
                try
                {
                    sock.Blocking = false;
                }
                catch (Exception e)
                {
                    EDB.WriteLine(e);
                    close();
                    return false;
                }
            }

            return true;
        }

        public void setNoDelay(bool nd)
        {
            try
            {
                sock.NoDelay = nd;
            }
            catch (Exception e)
            {
                EDB.WriteLine(e);
            }
        }

        public void enableRead()
        {
            if (sock == null)
                return;
            if (!sock.Connected)
                close();
            lock (close_mutex)
            {
                if (closed) return;
            }
            if (!expecting_read && poll_set != null)
            {
                //Console.WriteLine("ENABLE READ:   " + Topic + "(" + sock.FD + ")");
                expecting_read = true;
                poll_set.addEvents(sock, POLLIN);
            }
        }

        public void disableRead()
        {
            if (sock == null)
                return;
            if (!sock.Connected)
                close();
            lock (close_mutex)
            {
                if (closed) return;
            }
            if (expecting_read && poll_set != null)
            {
                //Console.WriteLine("DISABLE READ:  " + Topic + "(" + sock.FD + ")");
                poll_set.delEvents(sock, POLLIN);
                expecting_read = false;
            }
        }

        public void enableWrite()
        {
            if (sock == null)
                return;
            if (!sock.Connected) close();
            lock (close_mutex)
            {
                if (closed) return;
            }
            if (!expecting_write && poll_set != null)
            {
                //Console.WriteLine("ENABLE WRITE:  " + Topic + "(" + sock.FD + ")");
                expecting_write = true;
                poll_set.addEvents(sock, POLLOUT);
            }
        }

        public void disableWrite()
        {
            if (sock == null)
                return;
            if (!sock.Connected) close();
            lock (close_mutex)
            {
                if (closed) return;
            }
            if (expecting_write && poll_set != null)
            {
                //Console.WriteLine("DISABLE WRITE: " + Topic + "(" + sock.FD + ")");
                poll_set.delEvents(sock, POLLOUT);
                expecting_write = false;
            }
        }

        public bool connect(string host, int port)
        {
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            connected_host = host;
            connected_port = port;
            if (!setNonBlocking())
                throw new Exception("Failed to make socket nonblocking");
            setNoDelay(true);
            IPAddress IPA = null;

            if (!IPAddress.TryParse(host, out IPA))
            {
                foreach (IPAddress ipa in Dns.GetHostAddresses(host).Where(ipa => !ipa.ToString().Contains(":")))
                {
                    IPA = ipa;
                    break;
                }
                if (IPA == null)
                {
                    close();
                    EDB.WriteLine("Couldn't resolve host name [{0}]", host);
                    return false;
                }
            }

            if (IPA == null)
                return false;

            IPEndPoint ipep = new IPEndPoint(IPA, port);
            LocalEndPoint = ipep;
            DateTime connectionAttempted = DateTime.Now;
            IAsyncResult asyncres;
            lock (this)
                asyncres = sock.BeginConnect(ipep, iar =>
                {
                    lock(this)
                        if (sock != null)
                            try
                            {
                                sock.EndConnect(iar);
                            }
                            catch (Exception e)
                            {
                                EDB.WriteLine(e);
                            }
                }, null);
            bool completed = false;
            while (ROS.ok && !ROS.shutting_down)
            {
#pragma warning disable 665
                if ((completed = asyncres.AsyncWaitHandle.WaitOne(10,false)))
#pragma warning restore 665
                    break;
                if (DateTime.Now.Subtract(connectionAttempted).TotalSeconds >= 3)
                {
                    EDB.WriteLine("TRYING TO CONNECT FOR " + DateTime.Now.Subtract(connectionAttempted).TotalSeconds + "s\t: " + this);
                    if (!asyncres.AsyncWaitHandle.WaitOne(100,true))
                    {
                        sock.Close();
                        sock = null;
                    }
                }
            }
            if (!completed || sock == null || !sock.Connected)
                return false;
            return ROS.ok && initializeSocket();
        }

        public bool listen(int port, int backlog, AcceptCallback accept_cb)
        {
            is_server = true;
            this.accept_cb = accept_cb;

            sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            setNonBlocking();
            sock.Bind(new IPEndPoint(IPAddress.Any, port));
            server_port = ((IPEndPoint) sock.LocalEndPoint).Port;
            sock.Listen(backlog);
            if (!initializeSocket())
                return false;
            if ((flags & (int)Flags.SYNCHRONOUS) == 0)
                enableRead();
            return true;
        }

        private bool setKeepAlive(Socket sock, ulong time, ulong interval)
        {
            try
            {
                // resulting structure
                byte[] SIO_KEEPALIVE_VALS = new byte[3*bytesperlong];

                // array to hold input values
                ulong[] input = new ulong[3];

                // put input arguments in input array
                if (time == 0 || interval == 0) // enable disable keep-alive
                    input[0] = (0UL); // off
                else
                    input[0] = (1UL); // on

                input[1] = (time); // time millis
                input[2] = (interval); // interval millis

                // pack input into byte struct
                for (int i = 0; i < input.Length; i++)
                {
                    SIO_KEEPALIVE_VALS[i*bytesperlong + 3] =
                        (byte) (input[i] >> ((bytesperlong - 1)*bitsperbyte) & 0xff);
                    SIO_KEEPALIVE_VALS[i*bytesperlong + 2] =
                        (byte) (input[i] >> ((bytesperlong - 2)*bitsperbyte) & 0xff);
                    SIO_KEEPALIVE_VALS[i*bytesperlong + 1] =
                        (byte) (input[i] >> ((bytesperlong - 3)*bitsperbyte) & 0xff);
                    SIO_KEEPALIVE_VALS[i*bytesperlong + 0] =
                        (byte) (input[i] >> ((bytesperlong - 4)*bitsperbyte) & 0xff);
                }
                // create bytestruct for result (bytes pending on server socket)
                byte[] result = BitConverter.GetBytes(0);
                // write SIO_VALS to Socket IOControl
                sock.IOControl(IOControlCode.KeepAliveValues, SIO_KEEPALIVE_VALS, result);

                ByteDump(result);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        public void parseHeader(Header header)
        {
            if (_topic == null)
            {
                if (header.Values.Contains("topic"))
                    _topic = header.Values["topic"].ToString();
            }
            string nodelay = "";
            if (header.Values.Contains("tcp_nodelay"))
                nodelay = (string) header.Values["tcp_nodelay"];
            if (nodelay == "1")
            {
                setNoDelay(true);
            }
        }

        private bool setKeepAlive(Socket sock, ulong time, ulong interval, ulong count)
        {
            try
            {
                // resulting structure
                byte[] SIO_KEEPALIVE_VALS = new byte[3*bytesperlong];

                // array to hold input values
                ulong[] input = new ulong[4];

                // put input arguments in input array
                if (time == 0 || interval == 0) // enable disable keep-alive
                    input[0] = (0UL); // off
                else
                    input[0] = (1UL); // on

                input[1] = (time); // time millis
                input[2] = (interval); // interval millis
                input[3] = count;
                // pack input into byte struct
                for (int i = 0; i < input.Length; i++)
                {
                    SIO_KEEPALIVE_VALS[i*bytesperlong + 3] =
                        (byte) (input[i] >> ((bytesperlong - 1)*bitsperbyte) & 0xff);
                    SIO_KEEPALIVE_VALS[i*bytesperlong + 2] =
                        (byte) (input[i] >> ((bytesperlong - 2)*bitsperbyte) & 0xff);
                    SIO_KEEPALIVE_VALS[i*bytesperlong + 1] =
                        (byte) (input[i] >> ((bytesperlong - 3)*bitsperbyte) & 0xff);
                    SIO_KEEPALIVE_VALS[i*bytesperlong + 0] =
                        (byte) (input[i] >> ((bytesperlong - 4)*bitsperbyte) & 0xff);
                }
                // create bytestruct for result (bytes pending on server socket)
                byte[] result = BitConverter.GetBytes(0);
                // write SIO_VALS to Socket IOControl
                sock.IOControl(IOControlCode.KeepAliveValues, SIO_KEEPALIVE_VALS, result);

                ByteDump(result);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }


        public static string ByteDumpCondensed(byte[] b)
        {
            return b.Aggregate("", (current, t) => current + ("" + t.ToString("x") + ""));
        }

        public static string ByteDump(byte[] b)
        {
            string s = "";
            string bs = "";
            for (int i = 0; i < b.Length; i++)
            {
                bs = b[i].ToString("x");
                if (b[i] < 16) bs = "" + 0 + bs;
                s += "" + bs + " ";
                if (i%4 == 0) s += "     ";
                if (i%16 == 0 && i != b.Length - 1) s += "\n";
            }
            return s;
        }

        public void setKeepAlive(bool use, int idle, int interval, int count)
        {
            if (use)
            {
                if (!setKeepAlive(sock, (ulong) idle, (ulong) interval, (ulong) count) && 
                    !setKeepAlive(sock, (ulong) idle, (ulong) interval))

                    try
                    {
                        sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, use);
                    }
                    catch (Exception e)
                    {
                        EDB.WriteLine(e);
                        return;
                    }
            }
        }

        public int read(byte[] buffer, int pos, int length)
        {
            ScopedTimer.Ping();
            lock (close_mutex)
            {
                if (closed)
                    return -1;
            }
            int num_bytes = 0;
            SocketError err;
            num_bytes = sock.realsocket.Receive(buffer, pos, length, SocketFlags.None, out err);
            if (num_bytes < 0)
            {
                if (err == SocketError.TryAgain || err == SocketError.WouldBlock)
                    num_bytes = 0;
                else if (err != SocketError.InProgress && err != SocketError.IsConnected && err != SocketError.Success)
                {
                    close();
                    num_bytes = -1;
                }
            }
            return num_bytes;
        }

        public int write(byte[] buffer, int pos, int size)
        {
            ScopedTimer.Ping();
            lock (close_mutex)
            {
                if (closed)
                    return -1;
            }
            SocketError err;
            //EDB.WriteLine(ByteDumpCondensed(buffer));
            int num_bytes = sock.Send(buffer, pos, size, SocketFlags.None, out err);
            if (num_bytes <= 0)
            {
                if (err == SocketError.TryAgain || err == SocketError.WouldBlock)
                    num_bytes = 0;
                else if (err != SocketError.InProgress && err != SocketError.IsConnected && err != SocketError.Success)
                {
                    close();
                    return -1;
                }
                else
                    return 0;
            }
            return num_bytes;
        }

        private bool initializeSocket()
        {
            if (!setNonBlocking())
                return false;
            setNoDelay(true);
            setKeepAlive(use_keepalive, 60, 10, 9);

            if (string.IsNullOrEmpty(cached_remote_host))
            {
                if (is_server)
                    cached_remote_host = "TCPServer Socket";
                else
                    cached_remote_host = ClientURI + " on socket " + sock;
            }
            //Console.WriteLine("cached_remote_host = "+cached_remote_host);

            if (poll_set != null)
            {
                poll_set.addSocket(sock, socketUpdate, this);
            }
            if (!is_server && !sock.Connected)
            {
                close();
                return false;
            }
            return true;
        }

        private bool setSocket(Socket s)
        {
            sock = s;
            return initializeSocket();
        }

        public TcpTransport accept()
        {
            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            if (sock == null || !sock.AcceptAsync(args))
                return null;
            if (args.AcceptSocket == null)
            {
                EDB.WriteLine("NOTHING TO ACCEPT SO RETURNING NULL!");
                return null;
            }
            Socket acc = new Socket(args.AcceptSocket);
            TcpTransport transport = new TcpTransport(poll_set, flags);
            if (!transport.setSocket(acc))
            {
                throw new Exception("FAILED TO ADD SOCKET TO TRANSPORT ZOMG!");
            }
            return transport;
        }


#if !TRACE
        [DebuggerStepThrough]
#endif
        public override string ToString()
        {
            return "TCPROS connection to [" + sock + "]";
        }

        private void socketUpdate(int events)
        {
            ScopedTimer.Ping();
            lock (close_mutex)
            {
                if (closed) return;
            }

            if (is_server)
            {
                TcpTransport transport = accept();
                if (transport != null)
                {
                    if (accept_cb == null) throw new Exception("NULL ACCEPT_CB FTL!");
                    accept_cb(transport);
                }
            }
            else
            {
                if ((events & POLLIN) != 0 && expecting_read) //POLL IN FLAG
                {
                    if (read_cb != null)
                    {
                        read_cb(this);
                    }
                }

                if ((events & POLLOUT) != 0 && expecting_write)
                {
                    if (write_cb != null)
                        write_cb(this);
                }

                if ((events & POLLERR) != 0 || (events & POLLHUP) != 0 || (events & POLLNVAL) != 0)
                {
                    int error = 0;
                    try
                    {
                        error = (int) sock.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Error);
                    }
                    catch (Exception e)
                    {
                        EDB.WriteLine("Failed to get sock options! (error: " + error + ")" + e);
                    }
                    if (error != 0)
                        EDB.WriteLine("SOCKET ERROR = " + error);
                    close();
                }
            }
        }

        public void close()
        {
            DisconnectFunc disconnect_cb = null;
            lock (close_mutex)
            {
                if (!closed)
                {
                    closed = true;
                    if (poll_set != null)
                        poll_set.delSocket(sock);
                    if (sock.Connected)
                        sock.Shutdown(SocketShutdown.Both);
                    sock.Close();
                    sock = null;
                    disconnect_cb = this.disconnect_cb;
                    this.disconnect_cb = null;
                    read_cb = null;
                    write_cb = null;
                    accept_cb = null;
                }
            }
            if (disconnect_cb != null)
            {
                disconnect_cb(this);
            }
        }
    }
}
