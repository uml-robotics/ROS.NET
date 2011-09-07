#region USINGZ

using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using n = System.Net;
using ns = System.Net.Sockets;

#endregion

namespace Ros_CSharp.CustomSocket
{
    public class Socket : System.Net.Sockets.Socket
    {
        private static SortedList<uint, Socket> _socklist;
        private static uint nextfakefd = 1;
        private static List<uint> _freelist = new List<uint>();
        private uint _fakefd;
        private bool disposed;

        string attemptedConnectionEndpoint = null;

        public new void Connect(IPAddress[] address, int port)
        {
            attemptedConnectionEndpoint = address[0].ToString(); 
            base.Connect(address, port);
        }

        public new void Connect(IPAddress address, int port)
        {
            attemptedConnectionEndpoint = address.ToString();
            base.Connect(address,port);
        }

        public new void Connect(EndPoint ep)
        {
            attemptedConnectionEndpoint = ep.ToString();
            base.Connect(ep);
        }

        public new bool ConnectAsync(SocketAsyncEventArgs e)
        {
            attemptedConnectionEndpoint = e.RemoteEndPoint.ToString();
            return base.ConnectAsync(e);
        }

        public Socket(System.Net.Sockets.Socket sock)
            : this(sock.DuplicateAndClose(Process.GetCurrentProcess().Id))
        {
        }

        public Socket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
            : base(addressFamily, socketType, protocolType)
        {
            if (_socklist == null)
                _socklist = new SortedList<uint, Socket>();
            _socklist.Add(FD, this);
            //EDB.WriteLine("Making socket w/ FD=" + FD);
        }

        public Socket(SocketInformation socketInformation)
            : base(socketInformation)
        {
            if (_socklist == null)
                _socklist = new SortedList<uint, Socket>();
            _socklist.Add(FD, this);
            //EDB.WriteLine("Making socket w/ FD=" + FD);
        }

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

        public static Socket Get(uint fd)
        {
            if (_socklist == null || !_socklist.ContainsKey(fd))
                return null;
            return _socklist[fd];
        }

        ~Socket()
        {
            Dispose(true);
        }

        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                //EDB.WriteLine("Killing socket w/ FD=" + FD+(attemptedConnectionEndpoint==null?"":"\tTO REMOTE HOST\t"+attemptedConnectionEndpoint));
                if (Get(FD) != null)
                {
                    _socklist.Remove(FD);
                }
                _freelist.Add(FD);
                disposed = true;
                base.Dispose(disposing);
            }
        }

        [System.Diagnostics.DebuggerStepThrough]
        public override string ToString()
        {
            if (attemptedConnectionEndpoint == null || attemptedConnectionEndpoint == "")
            {
                if (!Connected)
                    attemptedConnectionEndpoint = "";
                else if (RemoteEndPoint != null)
                {
                    IPEndPoint ipep = RemoteEndPoint as IPEndPoint;
                    if (ipep != null)
                        attemptedConnectionEndpoint = "" + ipep.Address + ":" + ipep.Port;
                }
            }
            return ""+FD+ " -- "+attemptedConnectionEndpoint;
        }
    }
}