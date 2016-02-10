// File: XmlRpcSource.cs
// Project: XmlRpc_Wrapper
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 11/18/2015
// Updated: 02/10/2016

#region USINGZ

using System;
using System.Diagnostics;
using System.Net.Sockets;

#endregion

namespace XmlRpc_Wrapper
{
    [DebuggerStepThrough]
    public abstract class XmlRpcSource : IDisposable
    {
        private bool _deleteOnClose;

        // In the client, keep connections open if you intend to make multiple calls.
        private bool _keepOpen;

        public bool KeepOpen
        {
            get { return _keepOpen; }
            set { _keepOpen = value; }
        }

        public virtual Socket getSocket()
        {
            return null;
        }

        public virtual void Close()
        {
            throw new NotImplementedException();
        }

        public virtual XmlRpcDispatch.EventType HandleEvent(XmlRpcDispatch.EventType eventType)
        {
            throw new NotImplementedException();
        }

        //! Return whether the file descriptor should be kept open if it is no longer monitored.
        public bool getKeepOpen()
        {
            return _keepOpen;
        }

        //! Specify whether the file descriptor should be kept open if it is no longer monitored.
        public void setKeepOpen(bool b = true)
        {
            _keepOpen = b;
        }

        #region IDisposable Members

        public void Dispose()
        {
            Close();
        }

        #endregion

        // In the server, a new source (XmlRpcServerConnection) is created
        // for each connected client. When each connection is closed, the
        // corresponding source object is deleted.
    }
}