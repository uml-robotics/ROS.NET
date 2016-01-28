// File: Socket.cs
// Project: ROS_C-Sharp
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 09/01/2015
// Updated: 10/07/2015

#region USINGZ

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using n = System.Net;
using ns = System.Net.Sockets;

#endregion

namespace Ros_CSharp.CustomSocket
{
#if !TRACE
    [DebuggerStepThrough]
#endif
    public class Socket : ns.Socket
    {
        private static ConcurrentDictionary<uint, Socket> _socklist = new ConcurrentDictionary<uint, Socket>();
        private static uint nextfakefd = 1;
        private static ConcurrentBag<uint> _freelist = new ConcurrentBag<uint>();
        private uint _fakefd;

        private string attemptedConnectionEndpoint;
        private bool disposed;

        public static ConcurrentDictionary<uint, Socket> AllOfThem
        {
            get { return _socklist; }
        }

        public Socket(ns.Socket sock)
            : this(sock.DuplicateAndClose(Process.GetCurrentProcess().Id))
        {
        }

        public Socket(ns.AddressFamily addressFamily, ns.SocketType socketType, ns.ProtocolType protocolType)
            : base(addressFamily, socketType, protocolType)
        {
            _poller = new Action<int>(_poll);
            _socklist.TryAdd(FD, this);
            //EDB.WriteLine("Making socket w/ FD=" + FD);
        }

        public Socket(ns.SocketInformation socketInformation)
            : base(socketInformation)
        {
            _poller = new Action<int>(_poll);
            _socklist.TryAdd(FD, this);
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
                    lock (_freelist)
                    {
                        if (_freelist.Count > 0)
                        {
                            _freelist.TryTake(out _fakefd);
                        }
                        else
                            _fakefd = (nextfakefd++);
                    }
                }
                return _fakefd;
            }
        }

        public new void BeginConnect(n.EndPoint endpoint, AsyncCallback ac, object st)
        {
            n.IPEndPoint ipep = endpoint as n.IPEndPoint;
            if (endpoint == null)
                throw new Exception("Sorry, guy... but this isn't in the scope of this class's purpose.");
            attemptedConnectionEndpoint = ipep.Address.ToString();
            base.BeginConnect(endpoint, ac, st);
        }

        public new void BeginConnect(n.IPAddress address, int port, AsyncCallback ac, object st)
        {
            attemptedConnectionEndpoint = address.ToString();
            base.BeginConnect(address, port, ac, st);
        }

        public new void BeginConnect(n.IPAddress[] addresses, int port, AsyncCallback ac, object st)
        {
            attemptedConnectionEndpoint = addresses[0].ToString();
            base.BeginConnect(addresses, port, ac, st);
        }

        public new void BeginConnect(string host, int port, AsyncCallback ac, object st)
        {
            attemptedConnectionEndpoint = host;
            base.BeginConnect(host, port, ac, st);
        }

        public new void Connect(n.IPAddress[] address, int port)
        {
            attemptedConnectionEndpoint = address[0].ToString();
            base.Connect(address, port);
        }

        public new void Connect(n.IPAddress address, int port)
        {
            attemptedConnectionEndpoint = address.ToString();
            base.Connect(address, port);
        }

        public new void Connect(n.EndPoint ep)
        {
            attemptedConnectionEndpoint = ep.ToString();
            base.Connect(ep);
        }

        public new bool ConnectAsync(ns.SocketAsyncEventArgs e)
        {
            attemptedConnectionEndpoint = e.RemoteEndPoint.ToString();
            return base.ConnectAsync(e);
        }

        public static Socket Get(uint fd)
        {
            if (_socklist == null || !_socklist.ContainsKey(fd))
                return null;
            return _socklist[fd];
        }

        private static void remove(uint fd)
        {
            if (_socklist == null || !_socklist.ContainsKey(fd))
                return;
            Socket s = null;
            _socklist.TryRemove(fd, out s);
            s.Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            lock (this)
            {
                if (!disposed)
                {
                    EDB.WriteLine("Killing socket w/ FD=" + _fakefd + (attemptedConnectionEndpoint == null ? "" : "\tTO REMOTE HOST\t" + attemptedConnectionEndpoint));
                    disposed = true;
                    remove(_fakefd);
                    _freelist.Add(_fakefd);
                    _fakefd = 0;
                    base.Dispose(disposing);
                }
            }
        }

        public bool SafePoll(int timeout, ns.SelectMode sm)
        {
            lock (this)
            {
                bool res = false;
                try
                {
                    res = !disposed && Poll(timeout, sm);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    res = !disposed && sm == ns.SelectMode.SelectError;
                }
                return res;
            }
        }

#if !TRACE
        [DebuggerStepThrough]
#endif
        public override string ToString()
        {
            if (String.IsNullOrEmpty(attemptedConnectionEndpoint))
            {
                if (!Connected)
                    attemptedConnectionEndpoint = "";
                else if (RemoteEndPoint != null)
                {
                    n.IPEndPoint ipep = RemoteEndPoint as n.IPEndPoint;
                    if (ipep != null)
                        attemptedConnectionEndpoint = "" + ipep.Address + ":" + ipep.Port;
                }
            }
            return "" + _fakefd + " -- " + attemptedConnectionEndpoint;
        }


        public const int POLLERR = 0x008;
        public const int POLLHUP = 0x010;
        public const int POLLNVAL = 0x020;
        public const int POLLIN = 0x001;
        public const int POLLOUT = 0x004;

        private void _poll(int poll_timeout)
        {
            if (!disposed && !Connected)
            {
                Info.revents |= POLLHUP;
            }
            else if (disposed || SafePoll(poll_timeout, ns.SelectMode.SelectError))
            {
                Info.revents |= POLLERR;
            }
            else
            {
                if (!disposed && SafePoll(poll_timeout, ns.SelectMode.SelectWrite))
                {
                    Info.revents |= POLLOUT;
                }
                if (!disposed && SafePoll(poll_timeout, ns.SelectMode.SelectRead))
                {
                    Info.revents |= POLLIN;
                }
            }
            if (Info.revents == 0)
            {
                return;
            }

            PollSet.SocketUpdateFunc func = Info.func;
            int events = Info.events;
            int revents = Info.revents;

            if (Info.func != null &&
                ((Info.events & Info.revents) != 0 || (Info.revents & POLLERR) != 0 || (Info.revents & POLLHUP) != 0 ||
                (Info.revents & POLLNVAL) != 0))
            {
                bool skip = false;
                if ((Info.revents & (POLLERR | POLLHUP | POLLNVAL)) != 0)
                {
                    if (IsDisposed)
                    skip = true;
                }

                if (!skip)
                {
                    func(Info.revents & (Info.events | POLLERR | POLLHUP | POLLNVAL));
                }
            }

            Info.revents = 0;
            Info.poll_mutex.Set();
        }

        private Action<int> _poller;

        public void Poll(int poll_timeout)
        {
            if (_poller != null && Info.poll_mutex.WaitOne(0))
            {
                _poller.BeginInvoke(poll_timeout, (iar) =>
                                                      {
                                                          _poller.EndInvoke(iar);
                                                          Info.poll_mutex.Set();
                                                      }, null);
            }
        }

        public SocketInfo Info = null;
    }
}