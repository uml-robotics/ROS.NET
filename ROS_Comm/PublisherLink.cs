// File: PublisherLink.cs
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

#endregion

namespace Ros_CSharp
{
    public class PublisherLink
    {
        public string CallerID = "";
        public uint ConnectionID;
        public bool Latched;
        public string XmlRpc_Uri = "";
        private Header header;
        public string md5sum = "";
        public Subscription parent;
        public Stats stats = new Stats();

        public PublisherLink(Subscription parent, string xmlrpc_uri)
        {
            this.parent = parent;
            XmlRpc_Uri = xmlrpc_uri;
        }

        public string TransportType
        {
            get { return "TCPROS"; /*lol... pwned*/ }
        }

        public Header getHeader()
        {
            return header;
        }

        public bool setHeader(Header h)
        {
            CallerID = (string) h.Values["callerid"];
            if (!h.Values.Contains("md5sum"))
                return false;
            md5sum = (string) h.Values["md5sum"];
            Latched = false;
            if (!h.Values.Contains("latching"))
                return false;
            if ((string) h.Values["latching"] == "1")
                Latched = true;
            ConnectionID = ConnectionManager.Instance.GetNewConnectionID();
            header = h;
            parent.headerReceived(this, header);
            return true;
        }

        internal virtual void handleMessage(byte[] serializedmessagekinda, bool ser, bool nocopy)
        {
            throw new NotImplementedException();
        }

        public virtual void drop()
        {
            throw new NotImplementedException();
        }

        #region Nested type: Stats

        public class Stats
        {
            public UInt64 bytes_received;
            public UInt64 drops;
            public UInt64 messages_received;
        }

        #endregion
    }
}