using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
namespace EricIsAMAZING
{
    public class PollSet
    {
        public Dictionary<Socket, SocketInfo> socket_info = new Dictionary<Socket, SocketInfo>();
        public object socket_info_mutex = new object(), just_deleted_mutex = new object();
        public List<Socket> just_deleted = new List<Socket>();
        public bool sockets_changed;
        public List<PollFD> ufds = new List<PollFD>();
        public delegate void SocketUpdateFunc(int stufftodo);

        public bool addSocket(Socket s, SocketUpdateFunc update_func)
        {
            return addSocket(s, update_func, new TcpTransport());
        }

        public bool addSocket(Socket s, SocketUpdateFunc update_func, TcpTransport trans)
        {
            SocketInfo info = new SocketInfo() { sock = s, func = update_func, transport = trans };
            lock (socket_info_mutex)
            {
                if (socket_info.ContainsKey(info.sock))
                    return false;
                socket_info.Add(info.sock, info);
                sockets_changed = true;
            }
            return true;
        }

        public bool delSocket(Socket s)
        {
            lock (socket_info_mutex)
            {
                if (!socket_info.ContainsKey(s))
                    return false;
                socket_info.Remove(s);
                lock (just_deleted_mutex)
                {
                    just_deleted.Add(s);
                }
                sockets_changed = true;
            }
            return true;
        }

        public bool addEvents(Socket s, int events)
        {
            lock (socket_info_mutex)
            {
                if (!socket_info.ContainsKey(s))
                    return false;
                socket_info[s].events |= events;
            }
            return true;
        }

        public bool delEvents(Socket sock, int events)
        {
            lock (socket_info_mutex)
            {
                if (!socket_info.ContainsKey(sock))
                    return false;
                socket_info[sock].events &= ~events;
            }
            return true;
        }

        public void update(int poll_timeout)
        {
            createNativePollSet();

            int udfscount = ufds.Count;
            if (udfscount == 0) return;
            for (int i = 0; i < udfscount; i++)
            {
                if (ufds[i].revents == 0)
                    continue;

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

                int revents = ufds[i].events;

                if (func != null && ((events & revents)!=0 || (revents & 0x008)!=0 || (revents & 0x010)!=0 || (revents & 0x020)!=0))
                {
                    bool skip = false;
                    if ((revents & (0x008 | 0x010 | 0x020)) != 0)
                    {
                        lock (just_deleted_mutex)
                        {
                            if (just_deleted.Contains(ufds[i].sock))
                                skip = true;
                        }
                    }

                    if (!skip)
                    {
                        func.Invoke(revents & (events | 0x008 | 0x010 | 0x020));
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
                ufds.Clear();
                foreach (SocketInfo info in socket_info.Values)
                {
                    ufds.Add(new PollFD() {events = info.events, sock = info.sock, revents = 0});
                }
            }
        }
    }

    public class SocketInfo
    {
        public TcpTransport transport;
        public PollSet.SocketUpdateFunc func;
        public Socket sock;
        public int events;
    }

    public class PollFD
    {
        public Socket sock;
        public int events;
        public int revents;
    }
}
