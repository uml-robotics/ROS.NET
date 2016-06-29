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
// Updated: 02/10/2016

#region USINGZ

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using n = System.Net;
using ns = System.Net.Sockets;

#endregion

namespace Ros_CSharp.CustomSocket
{
#if !TRACE
    [DebuggerStepThrough]
#endif
    public class Socket : IDisposable
    {
        internal ns.Socket realsocket { get; private set; }
        private static List<uint> _freelist = new List<uint>();
        private uint _fakefd;
        private volatile static uint nextfakefd;

        private string attemptedConnectionEndpoint;
        private bool disposed = true;
        
        public Socket(ns.Socket sock)
        {
            realsocket = sock;
            disposed = false;
        }

        public Socket(ns.AddressFamily addressFamily, ns.SocketType socketType, ns.ProtocolType protocolType)
            : this(new ns.Socket(addressFamily, socketType, protocolType))
        {
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
                if (!disposed && _fakefd == 0)
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

        public IAsyncResult BeginConnect(n.EndPoint endpoint, AsyncCallback ac, object st)
        {
            n.IPEndPoint ipep = endpoint as n.IPEndPoint;
            if (endpoint == null)
                throw new Exception("Sorry, guy... but this isn't in the scope of this class's purpose.");
            attemptedConnectionEndpoint = ipep.Address.ToString();
            return realsocket.BeginConnect(endpoint, ac, st);
        }

        public IAsyncResult BeginConnect(n.IPAddress address, int port, AsyncCallback ac, object st)
        {
            attemptedConnectionEndpoint = address.ToString();
            return realsocket.BeginConnect(address, port, ac, st);
        }

        public IAsyncResult BeginConnect(n.IPAddress[] addresses, int port, AsyncCallback ac, object st)
        {
            attemptedConnectionEndpoint = addresses[0].ToString();
            return realsocket.BeginConnect(addresses, port, ac, st);
        }

        public IAsyncResult BeginConnect(string host, int port, AsyncCallback ac, object st)
        {
            attemptedConnectionEndpoint = host;
            return realsocket.BeginConnect(host, port, ac, st);
        }

        public void Connect(n.IPAddress[] address, int port)
        {
            attemptedConnectionEndpoint = address[0].ToString();
            realsocket.Connect(address, port);
        }

        public void Connect(n.IPAddress address, int port)
        {
            attemptedConnectionEndpoint = address.ToString();
            realsocket.Connect(address, port);
        }

        public void Connect(n.EndPoint ep)
        {
            attemptedConnectionEndpoint = ep.ToString();
            realsocket.Connect(ep);
        }

        public bool ConnectAsync(ns.SocketAsyncEventArgs e)
        {
            attemptedConnectionEndpoint = e.RemoteEndPoint.ToString();
            return realsocket.ConnectAsync(e);
        }

        public bool AcceptAsync(ns.SocketAsyncEventArgs a)
        {
            return realsocket.AcceptAsync(a);
        }

        public void Bind(n.EndPoint ep)
        {
            realsocket.Bind(ep);
        }

        public bool Blocking
        {
            get { return realsocket.Blocking; }
            set { realsocket.Blocking = value; }
        }

        public void Close()
        {
            if (realsocket != null) realsocket.Close();
        }

        public void Close(int timeout)
        {
            if (realsocket != null) realsocket.Close(timeout);
        }

        public bool Connected
        {
            get { return realsocket != null && realsocket.Connected; }
        }

        public void EndConnect(IAsyncResult iar)
        {
            realsocket.EndConnect(iar);
        }

        public object GetSocketOption(ns.SocketOptionLevel lvl, ns.SocketOptionName n)
        {
            return realsocket.GetSocketOption(lvl, n);
        }

        public void GetSocketOption(ns.SocketOptionLevel lvl, ns.SocketOptionName n, byte[] optionvalue)
        {
            realsocket.GetSocketOption(lvl, n, optionvalue);
        }

        public byte[] GetSocketOption(ns.SocketOptionLevel lvl, ns.SocketOptionName n, int optionlength)
        {
            return realsocket.GetSocketOption(lvl, n, optionlength);
        }

        public int IOControl(int code, byte[] inval, byte[] outval)
        {
            return realsocket.IOControl(code, inval, outval);
        }

        public int IOControl(ns.IOControlCode code, byte[] inval, byte[] outval)
        {
            return realsocket.IOControl(code, inval, outval);
        }

        public n.EndPoint LocalEndPoint
        {
            get { return realsocket.LocalEndPoint; }
        }

        public void Listen(int backlog)
        {
            realsocket.Listen(backlog);
        }

        public bool NoDelay
        {
            get { return realsocket.NoDelay; }
            set { realsocket.NoDelay = value; }
        }

        public int Receive(byte[] arr, int offset, int size, ns.SocketFlags f)
        {
            return realsocket.Receive(arr,offset,size,f);
        }

        public int Receive(byte[] arr, int offset, int size, ns.SocketFlags f, out ns.SocketError er)
        {
            return realsocket.Receive(arr, offset, size, f, out er);
        }

        public int Send(byte[] arr, int offset, int size, ns.SocketFlags f, out ns.SocketError er)
        {
            return realsocket.Send(arr, offset, size, f, out er);
        }

        public void SetSocketOption(ns.SocketOptionLevel lvl, ns.SocketOptionName n, bool optionvalue)
        {
            realsocket.SetSocketOption(lvl, n, optionvalue);
        }

        public void SetSocketOption(ns.SocketOptionLevel lvl, ns.SocketOptionName n, byte[] optionvalue)
        {
            realsocket.SetSocketOption(lvl, n, optionvalue);
        }

        public void SetSocketOption(ns.SocketOptionLevel lvl, ns.SocketOptionName n, int optionvalue)
        {
            realsocket.SetSocketOption(lvl, n, optionvalue);
        }

        public void SetSocketOption(ns.SocketOptionLevel lvl, ns.SocketOptionName n, object optionvalue)
        {
            realsocket.SetSocketOption(lvl, n, optionvalue);
        }

        public void Shutdown(ns.SocketShutdown sd)
        {
            realsocket.Shutdown(sd);
        }

        public bool SafePoll(int timeout, ns.SelectMode sm)
        {
            bool res = false;
            try
            {
                if (!disposed)
                    res = realsocket.Poll(timeout, sm);
            }
            catch (ns.SocketException e)
            {
                EDB.WriteLine(e);
                res = !disposed && sm == ns.SelectMode.SelectError;
            }
            return res;
        }

#if !TRACE
        [DebuggerStepThrough]
#endif
        public override string ToString()
        {
            if (String.IsNullOrEmpty(attemptedConnectionEndpoint))
            {
                if (!realsocket.Connected)
                    attemptedConnectionEndpoint = "";
                else if (realsocket.RemoteEndPoint != null)
                {
                    n.IPEndPoint ipep = realsocket.RemoteEndPoint as n.IPEndPoint;
                    if (ipep != null)
                        attemptedConnectionEndpoint = "" + ipep.Address + ":" + ipep.Port;
                }
            }
            return "" + _fakefd + " -- " + attemptedConnectionEndpoint + (Info != null ? " for " + Info.transport._topic : "");
        }


        public const int POLLERR = 0x008;
        public const int POLLHUP = 0x010;
        public const int POLLNVAL = 0x020;
        public const int POLLIN = 0x001;
        public const int POLLOUT = 0x004;
        private int poll_timeout = 10;

        internal void _poll(int POLLFLAGS)
        {
            if (realsocket == null || !realsocket.Connected || disposed)
            {
                Info.revents |= POLLHUP;
            }
            else
            {
                Info.revents |= POLLFLAGS;
            }
            if (Info.revents == 0)
            {
                return;
            }
            if (Info.func != null &&
                ((Info.events & Info.revents) != 0 || (Info.revents & POLLERR) != 0 || (Info.revents & POLLHUP) != 0 ||
                    (Info.revents & POLLNVAL) != 0))
            {
                bool skip = false;
                if ((Info.revents & (POLLERR | POLLHUP | POLLNVAL)) != 0)
                {
                    if (realsocket == null || disposed || !realsocket.Connected)
                        skip = true;
                }

                if (!skip)
                {
                    //Info.func.BeginInvoke(Info.revents & (Info.events | POLLERR | POLLHUP | POLLNVAL), Info.func.EndInvoke, null);
                    Info.func(Info.revents & (Info.events | POLLERR | POLLHUP | POLLNVAL));
                }
            }
            Info.revents = 0;
        }


        internal void _poll()
        {
            int revents = 0;
            if (!realsocket.Connected || disposed)
            {
                revents |= POLLHUP;
            }
            else
            {
                if (SafePoll(poll_timeout, ns.SelectMode.SelectError))
                    revents |= POLLERR;
                if (SafePoll(poll_timeout, ns.SelectMode.SelectWrite))
                    revents |= POLLOUT;
                if (SafePoll(poll_timeout, ns.SelectMode.SelectRead))
                    revents |= POLLIN;
            }
            _poll(revents);
        }

        public SocketInfo Info = null;

        public void Dispose()
        {
            lock (this)
            {
                if (!disposed && _fakefd != 0)
                {
#if DEBUG
                    EDB.WriteLine("Killing socket w/ FD=" + _fakefd + (attemptedConnectionEndpoint == null ? "" : "\tTO REMOTE HOST\t" + attemptedConnectionEndpoint));
#endif
                    disposed = true;
                    _freelist.Add(_fakefd);
                    _fakefd = 0;
                    realsocket.Close();
                    realsocket = null;
                }
            }
        }
    }
}