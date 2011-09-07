#region USINGZ

using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Socket = Ros_CSharp.CustomSocket.Socket;

#endregion

namespace Ros_CSharp
{
    public class PollSet
    {
        #region Delegates

        public delegate void SocketUpdateFunc(int stufftodo);

        #endregion

        public const int POLLERR = 0x008;
        public const int POLLHUP = 0x010;
        public const int POLLNVAL = 0x020;
        public const int POLLIN = 0x001;
        public const int POLLOUT = 0x004;

        public List<Socket> just_deleted = new List<Socket>();
        public object just_deleted_mutex = new object();
        private Socket[] localpipeevents = new Socket[2];
        public bool signal_locked;
        public object signal_mutex = new object();

        public Dictionary<uint, SocketInfo> socket_info = new Dictionary<uint, SocketInfo>();
        public object socket_info_mutex = new object();
        public bool sockets_changed;
        public List<PollFD> ufds = new List<PollFD>();

        public PollSet()
        {
            localpipeevents[0] = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            localpipeevents[0].Bind(new IPEndPoint(IPAddress.Loopback, 0));
            localpipeevents[1] = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            localpipeevents[1].Connect(localpipeevents[0].LocalEndPoint);
            localpipeevents[0].Connect(localpipeevents[1].LocalEndPoint);
            localpipeevents[0].Blocking = false;
            localpipeevents[1].Blocking = false;
            addSocket(localpipeevents[0], onLocalPipeEvents);
            addEvents(localpipeevents[0].FD, POLLIN);
        }

        ~PollSet()
        {
            if (localpipeevents[0] != null)
            {
                localpipeevents[0].Close();
                localpipeevents[0] = null;
            }
            if (localpipeevents[1] != null)
            {
                localpipeevents[1].Close();
                localpipeevents[1] = null;
            }
        }

        public void signal()
        {
            if (!signal_locked)
                lock (signal_mutex)
                {
                    signal_locked = true;
                    byte[] b = new byte[] {0};
                    if (localpipeevents[1].Poll(1, SelectMode.SelectWrite))
                    {
                        localpipeevents[1].Send(b);
                    }
                    signal_locked = false;
                }
        }

        public bool addSocket(Socket s, SocketUpdateFunc update_func)
        {
            return addSocket(s, update_func, null);
        }

        public bool addSocket(Socket s, SocketUpdateFunc update_func, TcpTransport trans)
        {
            SocketInfo info = new SocketInfo {sock = s.FD, func = update_func, transport = trans};
            lock (socket_info_mutex)
            {
                if (socket_info.ContainsKey(info.sock))
                    return false;
                socket_info.Add(info.sock, info);
                sockets_changed = true;
            }
            signal();
            return true;
        }

        public bool delSocket(Socket s)
        {
            lock (socket_info_mutex)
            {
                uint fd = s.FD;
                if (!socket_info.ContainsKey(fd))
                    return false;
                socket_info.Remove(fd);
                lock (just_deleted_mutex)
                {
                    just_deleted.Add(s);
                }
                sockets_changed = true;
            }
            signal();
            return true;
        }

        public bool addEvents(uint s, int events)
        {
            lock (socket_info_mutex)
            {
                if (!socket_info.ContainsKey(s))
                    return false;
                socket_info[s].events |= events;
            }
            signal();
            return true;
        }

        public bool delEvents(uint sock, int events)
        {
            lock (socket_info_mutex)
            {
                if (!socket_info.ContainsKey(sock))
                    return false;
                socket_info[sock].events &= ~events;
            }
            signal();
            return true;
        }

        public void update(int poll_timeout)
        {
            createNativePollSet();
            int udfscount = ufds.Count;
            int ret = 0;
            for (int i = 0; i < ufds.Count; i++)
            {
                Socket sock = Socket.Get(ufds[i].sock);
                if (!sock.Connected)
                {
                    ufds[i].revents |= POLLHUP;
                }
                else if (sock.Poll(poll_timeout, SelectMode.SelectError))
                {
                    ufds[i].revents |= POLLERR;
                }
                else
                {
                    if (sock.Poll(poll_timeout, SelectMode.SelectWrite))
                    {
                        ufds[i].revents |= POLLOUT;
                    }
                    if (sock.Poll(poll_timeout, SelectMode.SelectRead))
                    {
                        ufds[i].revents |= POLLIN;
                    }
                }
            }
            if (udfscount == 0)
                return;
            for (int i = 0; i < udfscount; i++)
            {
                if (ufds[i].revents == 0)
                {
                    EDB.WriteLine("NOTHING TO DO FOR SOCKET " + Socket.Get(ufds[i].sock).FD);
                    continue;
                }

                SocketUpdateFunc func = null;
                TcpTransport trans = null;
                int events = 0;
                lock (socket_info_mutex)
                {
                    if (!socket_info.ContainsKey(ufds[i].sock)) continue;
                    SocketInfo info = socket_info[ufds[i].sock];
                    func = info.func;
                    trans = info.transport;
                    events = info.events;
                }

                int revents = ufds[i].revents;

                if (func != null &&
                    ((events & revents) != 0 || (revents & POLLERR) != 0 || (revents & POLLHUP) != 0 ||
                     (revents & POLLNVAL) != 0))
                {
                    bool skip = false;
                    if ((revents & (POLLERR | POLLHUP | POLLNVAL)) != 0)
                    {
                        lock (just_deleted_mutex)
                        {
                            if (just_deleted.Contains(Socket.Get(ufds[i].sock)))
                                skip = true;
                        }
                    }

                    if (!skip)
                    {
                        func(revents & (events | POLLERR | POLLHUP | POLLNVAL));
                    }
                }

                ufds[i].revents = 0;
            }

            lock (just_deleted_mutex)
            {
                just_deleted.Clear();
            }
        }

        public void createNativePollSet()
        {
            lock (socket_info_mutex)
            {
                if (!sockets_changed)
                    return;
                foreach (SocketInfo info in socket_info.Values)
                {
                    if (!ufds.Exists((p) => p.sock == info.sock))
                        ufds.Add(new PollFD {events = info.events, sock = info.sock, revents = 0});
                }
                List<PollFD> gtfo = new List<PollFD>();
                foreach (PollFD fd in ufds)
                {
                    if (!socket_info.ContainsKey(fd.sock))
                        gtfo.Add(fd);
                }
                foreach (PollFD fd in gtfo)
                    ufds.Remove(fd);
            }
        }

        public void onLocalPipeEvents(int stuff)
        {
            if ((stuff & POLLIN) != 0)
            {
                byte[] b = new byte[1];
                while (localpipeevents[0].Poll(1, SelectMode.SelectRead) && localpipeevents[0].Receive(b) > 0)
                {
                }
            }
        }

        [DebuggerStepThrough]
        public override string ToString()
        {
            string s = "";
            lock (socket_info_mutex)
                foreach (SocketInfo si in socket_info.Values)
                    s += "" + si.sock + ", ";
            s = s.Remove(s.Length - 3, 2);
            return s;
        }
    }

    public class SocketInfo
    {
        public int events;
        public PollSet.SocketUpdateFunc func;
        public uint sock;
        public TcpTransport transport;
    }

    public class PollFD
    {
        public int events;
        public int revents;
        public uint sock;
    }
}