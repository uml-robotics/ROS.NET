// File: ServicePublication.cs
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
using System.Collections.Generic;
using Messages;

#endregion

namespace Ros_CSharp
{
    public class ServicePublication<MReq, MRes> : IServicePublication
        where MReq : IRosMessage, new()
        where MRes : IRosMessage, new()
    {
        public ServiceCallbackHelper<MReq, MRes> helper;

        //internal ftw?

        public ServicePublication(string name, string md5Sum, string datatype, string reqDatatype, string resDatatype, ServiceCallbackHelper<MReq, MRes> helper, CallbackQueueInterface callback, object trackedObject)
        {
            if (name == null)
                throw new Exception("NULL NAME?!");
            // TODO: Complete member initialization
            this.name = name;
            md5sum = md5Sum;
            this.datatype = datatype;
            req_datatype = reqDatatype;
            res_datatype = resDatatype;
            this.helper = helper;
            this.callback = callback;
            tracked_object = trackedObject;

            if (trackedObject != null)
                has_tracked_object = true;
        }

        public override void processRequest(ref byte[] buf, int num_bytes, IServiceClientLink link)
        {
            CallbackInterface cb = new ServiceCallback(this, helper, buf, num_bytes, link, has_tracked_object, tracked_object);
            callback.addCallback(cb, ROS.getPID());
        }

        internal override void addServiceClientLink(IServiceClientLink iServiceClientLink)
        {
            lock (client_links_mutex)
                client_links.Add(iServiceClientLink);
        }

        internal override void removeServiceClientLink(IServiceClientLink iServiceClientLink)
        {
            lock (client_links_mutex)
                client_links.Remove(iServiceClientLink);
        }

        public class ServiceCallback : CallbackInterface
        {
            private bool _hasTrackedObject;
            private int _numBytes;
            private object _trackedObject;
            private byte[] buffer;
            private ServicePublication<MReq, MRes> isp;
            private IServiceClientLink link;

            public ServiceCallback(ServiceCallbackHelper<MReq, MRes> _helper, byte[] buf, int num_bytes, IServiceClientLink link, bool has_tracked_object, object tracked_object)
                : this(null, _helper, buf, num_bytes, link, has_tracked_object, tracked_object)
            {
            }

            public ServiceCallback(ServicePublication<MReq, MRes> sp, ServiceCallbackHelper<MReq, MRes> _helper, byte[] buf, int num_bytes, IServiceClientLink link, bool has_tracked_object, object tracked_object)

            {
                isp = sp;
                if (isp != null && _helper != null)
                    isp.helper = _helper;
                buffer = buf;
                _numBytes = num_bytes;
                this.link = link;
                _hasTrackedObject = has_tracked_object;
                _trackedObject = tracked_object;
            }

            internal override CallResult Call()
            {
                if (link.connection.dropped)
                {
                    return CallResult.Invalid;
                }

                ServiceCallbackHelperParams<MReq, MRes> parms = new ServiceCallbackHelperParams<MReq, MRes>
                {
                    request = new MReq(),
                    response = new MRes(),
                    connection_header = link.connection.header.Values
                };
                parms.request.Deserialize(buffer);

                try
                {
                    bool ok = isp.helper.call(parms);
                    link.processResponse(parms.response, ok);
                }
                catch (Exception e)
                {
                    string woops = "Exception thrown while processing service call: " + e;
                    ROS.Error(woops);
                    link.processResponse(woops, false);
                    return CallResult.Invalid;
                }
                return CallResult.Success;
            }
        }
    }

    public class IServicePublication
    {
        internal CallbackQueueInterface callback;
        internal List<IServiceClientLink> client_links = new List<IServiceClientLink>();
        protected object client_links_mutex = new object();
        internal string datatype;
        internal bool has_tracked_object;
        internal bool isDropped;
        internal string md5sum;
        internal string name;
        internal string req_datatype;
        internal string res_datatype;
        internal object tracked_object;

        internal void drop()
        {
            lock (client_links_mutex)
            {
                isDropped = true;
            }
            dropAllConnections();
            callback.removeByID(ROS.getPID());
        }

        private void dropAllConnections()
        {
            List<IServiceClientLink> links;
            lock (client_links_mutex)
            {
                links = new List<IServiceClientLink>(client_links);
                client_links.Clear();
            }

            foreach (IServiceClientLink iscl in links)
            {
                iscl.connection.drop(Connection.DropReason.Destructing);
            }
        }

        internal virtual void addServiceClientLink(IServiceClientLink iServiceClientLink)
        {
            throw new NotImplementedException();
        }

        internal virtual void removeServiceClientLink(IServiceClientLink iServiceClientLink)
        {
            throw new NotImplementedException();
        }

        public virtual void processRequest(ref byte[] buffer, int size, IServiceClientLink iServiceClientLink)
        {
            throw new NotImplementedException();
        }
    }
}