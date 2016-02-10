// File: PendingConnection.cs
// Project: ROS_C-Sharp
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 04/28/2015
// Updated: 02/10/2016

#region USINGZ

using System;
using XmlRpc_Wrapper;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace Ros_CSharp
{
    public class PendingConnection : AsyncXmlRpcConnection, IDisposable
    {
        public string RemoteUri;
        private int _failures;
        private XmlRpcValue chk;
        public XmlRpcClient client;
        public Subscription parent;

        //public XmlRpcValue stickaroundyouwench = null;
        public PendingConnection(XmlRpcClient client, Subscription s, string uri, XmlRpcValue chk)
        {
            this.client = client;
            this.chk = chk;
            parent = s;
            RemoteUri = uri;
        }

        #region IDisposable Members

        public void Dispose()
        {
            chk = null; //.Dispose();
            client.Dispose();
            client = null;
        }

        #endregion

        public int failures
        {
            get { return _failures; }
            set { _failures = value; }
        }

        public override void addToDispatch(XmlRpcDispatch disp)
        {
            if (disp == null)
                return;
            if (check())
                return;
            disp.AddSource(client, (XmlRpcDispatch.EventType.WritableEvent | XmlRpcDispatch.EventType.Exception));
        }

        public override void removeFromDispatch(XmlRpcDispatch disp)
        {
            disp.RemoveSource(client);
        }

        public override bool check()
        {
            if (parent == null)
                return false;
            if (client.ExecuteCheckDone(chk))
            {
                parent.pendingConnectionDone(this, chk);
                return true;
            }
            return false;
        }
    }
}