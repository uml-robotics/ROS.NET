// File: Socket.cs
// Project: ROS_C-Sharp
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 11/06/2013
// Updated: 07/23/2014

#region USINGZ

using System;
using System.Collections.Generic;
using System.Diagnostics;
using n = System.Net;
using ns = System.Net.Sockets;

#endregion

namespace Ros_CSharp.CustomSocket
{
    public class Socket : ns.Socket
    {
        private static SortedList<uint, Socket> _socklist = new SortedList<uint, Socket>();
        private static uint nextfakefd = 1;
        private static List<uint> _freelist = new List<uint>();
        private uint _fakefd;

        private string attemptedConnectionEndpoint;
        private bool disposed;

        public Socket(ns.Socket sock)
            : this(sock.DuplicateAndClose(Process.GetCurrentProcess().Id))
        {
        }

        public Socket(ns.AddressFamily addressFamily, ns.SocketType socketType, ns.ProtocolType protocolType)
            : base(addressFamily, socketType, protocolType)
        {
            lock (_socklist)
            {
                _socklist.Add(FD, this);
            }
            //EDB.WriteLine("Making socket w/ FD=" + FD);
        }

        public Socket(ns.SocketInformation socketInformation)
            : base(socketInformation)
        {
            lock (_socklist)
            {
                _socklist.Add(FD, this);
            }
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
                            _fakefd = _freelist[0];
                            _freelist.RemoveAt(0);
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
            lock (_socklist)
            {
                if (_socklist == null || !_socklist.ContainsKey(fd))
                    return null;
                return _socklist[fd];
            }
        }

        private static void remove(uint fd)
        {
            lock (_socklist)
            {
                if (_socklist == null || !_socklist.ContainsKey(fd))
                    return;
                _socklist.Remove(fd);
            }
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

        [DebuggerStepThrough]
        public override string ToString()
        {
            if (string.IsNullOrEmpty(attemptedConnectionEndpoint))
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
    }
}