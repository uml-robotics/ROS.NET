#region USINGZ

using System;
using System.Net;
using System.Net.Sockets;

#endregion

namespace EricIsAMAZING
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

        public static bool use_keepalive;
        public AcceptCallback accept_cb;
        public string cached_remote_host;
        public object close_mutex = new object();
        public bool closed;
        public string connected_host;
        public int connected_port;
        public DisconnectFunc disconnect_cb;
        public int events;
        public bool expecting_read;
        public bool expecting_write;
        public int flags;
        public bool is_server;
        public bool no_delay;
        public PollSet poll_set;
        public ReadFinishedFunc read_cb;
        public IPEndPoint server_address;
        public int server_port = -1;
        private Socket sock;
        public WriteFinishedFunc write_cb;

        public TcpTransport()
        {
        }

        public TcpTransport(PollSet pollset, int flags = 0)
        {
            poll_set = pollset;
            this.flags = flags;
            Console.WriteLine("Making a fucking socket, MOTHERFUCKER!");
        }

        public string ClientURI
        {
            get { return sock.RemoteEndPoint.ToString(); }
        }

        public virtual bool getRequiresHeader()
        {
            return true;
        }

        public event DisconnectFunc DisconnectCallback;
        public event WriteFinishedFunc WriteCallback;
        public event ReadFinishedFunc ReadCallback;

        public bool setNonBlocking()
        {
            if ((flags & (int)Flags.SYNCHRONOUS) == 0)
            {
                try
                {
                    sock.Blocking = false;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    close();
                    return false;
                }
            }

            setNoDelay(true);
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
                Console.WriteLine(e);
            }
        }

        public void enableRead()
        {
            lock (close_mutex)
            {
                if (closed) return;
            }
            if (!expecting_read)
            {
                poll_set.addEvents(sock, 0x001);
                expecting_read = true;
            }
        }

        public void disableRead()
        {
            lock (close_mutex)
            {
                if (closed) return;
            }
            if (expecting_read)
            {
                poll_set.delEvents(sock, 0x001);
                expecting_read = false;
            }
        }

        public void enableWrite()
        {
            lock (close_mutex)
            {
                if (closed) return;
            }
            if (!expecting_write)
            {
                poll_set.addEvents(sock, 0x004);
                expecting_write = true;
            }
        }

        public void disableWrite()
        {
            lock (close_mutex)
            {
                if (closed) return;
            }
            if (expecting_write)
            {
                poll_set.delEvents(sock, 0x004);
                expecting_write = false;
            }
        }

        public bool connect(string host, int port)
        {
            sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            connected_host = host;
            connected_port = port;

            setNonBlocking();

            IPAddress IPA = null;

            if (!IPAddress.TryParse(host, out IPA))
            {
                foreach (IPAddress ipa in Dns.GetHostAddresses(host))
                    if (ipa.ToString().Contains(":"))
                        continue;
                    else
                    {
                        IPA = ipa;
                        break;
                    }
                if (IPA == null)
                {
                    close();
                    Console.WriteLine("Couldn't resolve host name [{0}]", host);
                    return false;
                }
            }

            if (IPA == null)
                return false;

            IPEndPoint ipep = new IPEndPoint(IPA, port);

            bool done = false;
            bool result = false;
            sock.BeginConnect(ipep, (iar) =>
            {
                sock.EndConnect(iar);
                result = initializeSocket();
                done = true;
            }, null);

            while (!done)
            {
            }

            return result;
        }

        public bool listen(int port, int backlog, AcceptCallback accept_cb)
        {
            is_server = true;
            this.accept_cb = accept_cb;

            sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            sock.Bind(new IPEndPoint(IPAddress.Any, port));
            server_port = (sock.LocalEndPoint as IPEndPoint).Port;
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
                byte[] SIO_KEEPALIVE_VALS = new byte[3 * bytesperlong];

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
                    SIO_KEEPALIVE_VALS[i * bytesperlong + 3] = (byte)(input[i] >> ((bytesperlong - 1) * bitsperbyte) & 0xff);
                    SIO_KEEPALIVE_VALS[i * bytesperlong + 2] = (byte)(input[i] >> ((bytesperlong - 2) * bitsperbyte) & 0xff);
                    SIO_KEEPALIVE_VALS[i * bytesperlong + 1] = (byte)(input[i] >> ((bytesperlong - 3) * bitsperbyte) & 0xff);
                    SIO_KEEPALIVE_VALS[i * bytesperlong + 0] = (byte)(input[i] >> ((bytesperlong - 4) * bitsperbyte) & 0xff);
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
            string nodelay = "";
            if (header.Values.Contains("tcp_nodelay"))
                nodelay = (string)header.Values["tcp_nodelay"];
            if (nodelay == "1")
            {
                Console.WriteLine("SETTING NODELAY ON THIS SHIZNIT!");
                setNoDelay(true);
            }
        }

        private bool setKeepAlive(Socket sock, ulong time, ulong interval, ulong count)
        {
            try
            {
                // resulting structure
                byte[] SIO_KEEPALIVE_VALS = new byte[3 * bytesperlong];

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
                    SIO_KEEPALIVE_VALS[i * bytesperlong + 3] = (byte)(input[i] >> ((bytesperlong - 1) * bitsperbyte) & 0xff);
                    SIO_KEEPALIVE_VALS[i * bytesperlong + 2] = (byte)(input[i] >> ((bytesperlong - 2) * bitsperbyte) & 0xff);
                    SIO_KEEPALIVE_VALS[i * bytesperlong + 1] = (byte)(input[i] >> ((bytesperlong - 3) * bitsperbyte) & 0xff);
                    SIO_KEEPALIVE_VALS[i * bytesperlong + 0] = (byte)(input[i] >> ((bytesperlong - 4) * bitsperbyte) & 0xff);
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

        public static void ByteDump(byte[] b)
        {
            string s = "";
            for (int i = 0; i < b.Length; i++)
            {
                s += "" + b[i].ToString("x") + " ";
                if (i % 4 == 0) s += "     ";
                if (i % 16 == 0 && i != b.Length - 1) s += "\n";
            }
            Console.WriteLine(s);
        }

        public void setKeepAlive(bool use, int idle, int interval, int count)
        {
            if (use)
            {
                try
                {
                    sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 1);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return;
                }

                if (!setKeepAlive(sock, (ulong)idle, (ulong)interval, (ulong)count) && !setKeepAlive(sock, (ulong)idle, (ulong)interval))
                    Console.WriteLine("FAIL!");
            }
            else
                try
                {
                    sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, 0);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return;
                }
        }

        public int read(ref byte[] buffer, int pos, int length)
        {
            lock (close_mutex)
            {
                if (closed)
                    return -1;
            }
            try
            {
                int num_bytes = sock.Receive(buffer, pos, length, SocketFlags.None);
                return num_bytes;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return -1;
        }

        public int write(byte[] buffer, int pos, int size)
        {
            lock (close_mutex)
            {
                if (closed)
                    return -1;
            }

            int num_bytes = sock.Send(buffer, pos, size, SocketFlags.None);
            return num_bytes;
        }

        private bool initializeSocket()
        {
            if (!setNonBlocking())
                return false;

            setKeepAlive(use_keepalive, 60, 10, 9);

            if (cached_remote_host == "")
            {
                if (is_server)
                    cached_remote_host = "TCPServer Socket";
                else
                    cached_remote_host = ClientURI + " on socket " + sock.RemoteEndPoint;
            }

            setNoDelay(true);

            if (poll_set != null)
            {
                poll_set.addSocket(sock, socketUpdate, this);
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
            Socket acc = sock.Accept();
            TcpTransport transport = new TcpTransport(poll_set, flags);
            if (!transport.setSocket(acc))
            {
                throw new Exception("FAILED TO ADD SOCKET TO TRANSPORT ZOMG!");
            }
            return transport;
        }

        public override string ToString()
        {
            return "TCPROS connection to [" + cached_remote_host + "]";
        }

        private void socketUpdate(int events)
        {
            lock (close_mutex)
            {
                if (closed) return;
            }
            if ((events & 0x001) != 0 && expecting_read) //POLL IN FLAG
            {
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
                    if (read_cb != null)
                    {
                        read_cb(this);
                    }
                }
            }
        }

        public void close()
        {
            DisconnectFunc disconnect_cb = null;
            if (!closed)
            {
                lock (close_mutex)
                {
                    if (!closed)
                    {
                        closed = true;
                        if (poll_set != null)
                            poll_set.delSocket(sock);
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
            }
            if (disconnect_cb != null)
            {
                disconnect_cb(this);
            }

            if (closed) return;
            if ((events & 0x008) != 0 || (events & 0x010) != 0 || (events & 0x020) != 0)
            {
                close();
            }
        }
    }
}