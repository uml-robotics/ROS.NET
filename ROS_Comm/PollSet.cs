// File: PollSet.cs
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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Socket = Ros_CSharp.CustomSocket.Socket;

#endregion

namespace Ros_CSharp
{
    public class PollSet : Poll_Signal
    {
        private static Dictionary<uint, CustomSocket.Socket> socks = new Dictionary<uint, CustomSocket.Socket>();
        #region Delegates

        public delegate void SocketUpdateFunc(int stufftodo);

        #endregion

        public PollSet() : base(null)
        {
            base.Op = update;
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public new void Dispose()
        {
            base.Dispose();
            DisposingEvent?.Invoke();
        }

        public delegate void DisposingDelegate();

        public event DisposingDelegate DisposingEvent;
        
        public bool addSocket(Socket s, SocketUpdateFunc update_func)
        {
            return addSocket(s, update_func, null);
        }

        public bool addSocket(Socket s, SocketUpdateFunc update_func, TcpTransport trans)
        {
            s.Info = new SocketInfo { sock = s.FD, func = update_func, transport = trans };
            lock (socks)
                socks.Add(s.FD, s);
            return true;
        }

        public bool delSocket(Socket s)
        {
            lock (socks)
                socks.Remove(s.FD);
            s.Dispose();
            return true;
        }

        public bool addEvents(Socket s, int events)
        {
            s.Info.events |= events;
            s.signal();
            return true;
        }

        public bool delEvents(Socket s, int events)
        {
            s.Info.events &= ~events;
            s.signal();
            return true;
        }

        public void update()
        {
            lock (socks)
                foreach (Socket s in socks.Values)
                {
                    s.signal();
                }
        }
    }

    public class SocketInfo
    {
        public int events;
        public PollSet.SocketUpdateFunc func;
        public int revents;
        public uint sock;
        public TcpTransport transport;
    }
}