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
// Updated: 10/07/2015

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
    public class PollSet : IDisposable
    {
        #region Delegates

        public delegate void SocketUpdateFunc(int stufftodo);

        #endregion

        private Socket[] localpipeevents = new Socket[2];
        public AutoResetEvent signal_mutex = new AutoResetEvent(true);

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
            addEvents(localpipeevents[0].FD, Socket.POLLIN);
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            signal_mutex.WaitOne();
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
                byte[] b = {0};
                if (localpipeevents[1] == null)
                    return;
                if (localpipeevents[1].Poll(1, SelectMode.SelectWrite))
                {
                    localpipeevents[1].Send(b);
                }
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

        public void onLocalPipeEvents(int stuff)
        {
            if ((stuff & Socket.POLLIN) != 0)
            {
                byte[] b = new byte[1];
                while (localpipeevents[0].Poll(1, SelectMode.SelectRead) && localpipeevents[0].Receive(b) > 0)
                {
                }
            }
        }

#if !TRACE
        [DebuggerStepThrough]
#endif
        public override string ToString()
        {
            string s = "";
            s = Socket.AllOfThem.Values.Aggregate(s, (current, si) => current + ("" + si.FD + ", "));
            s = s.Remove(s.Length - 3, 2);
            return s;
        }
    }

    public class SocketInfo
    {
        public int revents;
        public int events;
        public PollSet.SocketUpdateFunc func;
        public uint sock;
        public TcpTransport transport;
    }
}