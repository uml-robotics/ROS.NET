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
using System.Collections;
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
        private static Dictionary<uint, CustomSocket.Socket> socks = new Dictionary<uint,CustomSocket.Socket>();
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
            if (DisposingEvent != null) DisposingEvent.Invoke();
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
            if (s != null && s.Info != null)
                s.Info.events |= events;
            return true;
        }

        public bool delEvents(Socket s, int events)
        {
            if (s != null && s.Info != null)
                s.Info.events &= ~events;
            return true;
        }

        public void update()
        {
            ArrayList checkWrite = new ArrayList();
            ArrayList checkRead = new ArrayList();
            ArrayList checkError = new ArrayList();
            List<Socket> lsocks = new List<Socket>();
            lock (socks)
                foreach (Socket s in socks.Values)
                {
                    lsocks.Add(s);
                    if ((s.Info.events & Socket.POLLIN) != 0)
                        checkRead.Add(s.realsocket);
                    if ((s.Info.events & Socket.POLLOUT) != 0)
                        checkWrite.Add(s.realsocket);
                    if ((s.Info.events & (Socket.POLLERR | Socket.POLLHUP | Socket.POLLNVAL)) != 0)
                        checkError.Add(s.realsocket);
                }
            if (lsocks.Count == 0 || (checkRead.Count == 0 && checkWrite.Count == 0 && checkError.Count == 0))
                return;
            try
            {
                System.Net.Sockets.Socket.Select(checkRead, checkWrite, checkError, -1);
            }
            catch
            {
                return;
            }
            int nEvents = checkRead.Count + checkWrite.Count + checkError.Count;

            if (nEvents == 0)
                return;

            // Process events
            foreach (var record in lsocks)
            {
                int newmask = 0;
                if (checkRead.Contains(record.realsocket))
                    newmask |= Socket.POLLIN;
                if (checkWrite.Contains(record.realsocket))
                    newmask |= Socket.POLLOUT;
                if (checkError.Contains(record.realsocket))
                    newmask |= Socket.POLLERR;
                record._poll(newmask);
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