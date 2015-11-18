// File: XmlRpcSource.cs
// Project: XmlRpc_Wrapper
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
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;

#endregion

namespace XmlRpc
{
    [DebuggerStepThrough]
    public abstract class XmlRpcSource : IDisposable
    {
        public bool KeepOpen
        {
            get { return _keepOpen; }
            set { _keepOpen = value; }
        }

		public virtual Socket getSocket()
		{
			return null;
		}

        #region IDisposable Members

        public void Dispose()
        {
            Close();
        }

        #endregion
		
        public virtual void Close()
        {
			throw new NotImplementedException();
        }

		public virtual XmlRpcDispatch.EventType HandleEvent(XmlRpcDispatch.EventType eventType)
        {
			throw new NotImplementedException();
        }

		//! Return whether the file descriptor should be kept open if it is no longer monitored.
		public bool getKeepOpen() { return _keepOpen; }
		//! Specify whether the file descriptor should be kept open if it is no longer monitored.
		public void setKeepOpen(bool b=true) { _keepOpen = b; }

		// In the server, a new source (XmlRpcServerConnection) is created
		// for each connected client. When each connection is closed, the
		// corresponding source object is deleted.
		bool _deleteOnClose;

		// In the client, keep connections open if you intend to make multiple calls.
		bool _keepOpen;
    }
}