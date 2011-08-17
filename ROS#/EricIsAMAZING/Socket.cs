using System;
using System.Collections.Generic;
using System.Linq;
using EricIsAMAZING.CustomSocket;
using System.Net.Sockets;
using Socket = EricIsAMAZING.CustomSocket.Socket;
using System.Text;
using n = System.Net;
using ns = System.Net.Sockets;

namespace EricIsAMAZING.CustomSocket
{
    public class Socket : ns.Socket
    {
        public static Socket Get(uint fd)
        {
            if (_socklist == null || !_socklist.ContainsKey(fd))
                return null;
            return _socklist[fd];
        }
        private static SortedList<uint, Socket> _socklist;
        private uint _fakefd = 0;
        private static uint nextfakefd = 1;
        private static List<uint> _freelist = new List<uint>();
        private bool disposed;
        public bool IsDisposed
        {
            get { return disposed; }
        }
        public uint FD
        {
            get
            {
                if (_fakefd == 0)
                {
                    if (_freelist.Count > 0)
                    {
                        _fakefd = _freelist[0];
                        _freelist.RemoveAt(0);
                    }
                    else
                        _fakefd = (nextfakefd++);
                }
                return _fakefd;
            }
        }

        public Socket(ns.Socket sock) : this(sock.DuplicateAndClose(System.Diagnostics.Process.GetCurrentProcess().Id)) { }

        public Socket(ns.AddressFamily addressFamily, ns.SocketType socketType, ns.ProtocolType protocolType)
            : base(addressFamily, socketType, protocolType)
        {
            if (_socklist == null)
                _socklist = new SortedList<uint, Socket>();
            _socklist.Add(FD, this);
            //EDB.WriteLine("Making socket w/ FD=" + FD);
        }

        public Socket(ns.SocketInformation socketInformation)
            : base(socketInformation)
        {
            if (_socklist == null)
                _socklist = new SortedList<uint, Socket>();
            _socklist.Add(FD, this);
            //EDB.WriteLine("Making socket w/ FD=" + FD);
        }

        ~Socket()
        {
            Dispose(true);
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                EDB.WriteLine("Killing socket w/ FD=" + FD);
                if (Get(FD) != null)
                    _socklist.Remove(FD);
                _freelist.Add(FD);
                disposed = true;
                base.Dispose(disposing);
            }
        }
    }
}
