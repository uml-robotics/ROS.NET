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
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Socket = Ros_CSharp.CustomSocket.Socket;

#endregion

namespace Ros_CSharp
{
    public class PollSet : IDisposable
    {
        #region Delegates

        public delegate void SocketUpdateFunc(int stufftodo);

        #endregion

        public AutoResetEvent signal_mutex = new AutoResetEvent(true);

        public PollSet()
        {
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            signal_mutex.WaitOne();
            if (DisposingEvent != null)
                DisposingEvent();
            signal_mutex.Set();
        }

        public delegate void DisposingDelegate();

        public event DisposingDelegate DisposingEvent;

        public void signal()
        {
            if (signal_mutex.WaitOne(0))
            {
                signal_mutex.Set();
            }
        }

        public bool addSocket(Socket s, SocketUpdateFunc update_func)
        {
            return addSocket(s, update_func, null);
        }

        public bool addSocket(Socket s, SocketUpdateFunc update_func, TcpTransport trans)
        {
            s.Info = new SocketInfo {sock = s.FD, func = update_func, transport = trans};
            signal();
            return true;
        }

        public bool delSocket(Socket s)
        {
            s.Dispose();
            signal();
            return true;
        }

        public bool addEvents(uint s, int events)
        {
            Socket.Get(s).Info.events |= events;
            signal();
            return true;
        }

        public bool delEvents(uint sock, int events)
        {
            Socket.Get(sock).Info.events &= ~events;
            signal();
            return true;
        }

        public void update(int poll_timeout)
        {
            Socket.Poll(poll_timeout);
        }

#if !TRACE
        [DebuggerStepThrough]
#endif
        public override string ToString()
        {
            string s = "";
            s = Socket.FDs;
            s = s.Remove(s.Length - 3, 2);
            return s;
        }
    }

    public class SocketInfo
    {
        public int events;
        public PollSet.SocketUpdateFunc func;
        internal AutoResetEvent poll_mutex = new AutoResetEvent(true);
        public int revents;
        public uint sock;
        public TcpTransport transport;
    }
}