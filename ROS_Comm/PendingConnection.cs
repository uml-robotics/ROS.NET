// File: PendingConnection.cs
// Project: ROS_C-Sharp
// 
// ROS#
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 03/04/2013
// Updated: 07/26/2013

#region Using

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
        public XmlRpcClient client;
        public Subscription parent;
        private XmlRpcValue chk;
        private int _failures = 0;

        public int failures
        {
            get { return _failures; }
            set { _failures = value; }
        }

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
            chk.Dispose();
            client.Dispose();
            client = null;
        }

        #endregion

        public override void addToDispatch(XmlRpcDispatch disp)
        {
            if (disp == null)
                return;
            if (check())
                return;
            client.SegFault();
            disp.AddSource(client, (int) (XmlRpcDispatch.EventType.WritableEvent | XmlRpcDispatch.EventType.Exception));
        }

        public override void removeFromDispatch(XmlRpcDispatch disp)
        {
            client.SegFault();
            disp.RemoveSource(client);
        }

        public override bool check()
        {
            client.SegFault();
            if (parent == null)
                return false;
            if (client.ExecuteCheckDone(chk))
            {
                parent.pendingConnectionDone(this, chk.instance);
                return true;
            }
            return false;
        }
    }
}