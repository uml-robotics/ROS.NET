// File: ServiceClient.cs
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
using System.Collections;
using System.Diagnostics;
using System.Threading;
using Messages;

#endregion

namespace Ros_CSharp
{
    public class ServiceClient<MReq, MRes> : IServiceClient where MReq : IRosMessage, new() where MRes : IRosMessage, new()
    {
        internal ServiceClient(string service, bool persistent, IDictionary header_values, string md5sum)
        {
            this.service = service;
            this.persistent = persistent;
            this.header_values = header_values;
            this.md5sum = md5sum;
            if (persistent)
            {
                server_link = ServiceManager.Instance.createServiceServerLink<MReq, MRes>(service, persistent, md5sum, md5sum, header_values);
            }
        }

        public bool call(MReq request, ref MRes response)
        {
            string md5 = request.MD5Sum;
            return call(request, ref response, md5);
        }

        public bool call(MReq request, ref MRes response, string service_md5sum)
        {
            if (service_md5sum != md5sum)
            {
                EDB.WriteLine("Call to service [{0} with md5sum [{1} does not match md5sum when the handle was created([{2}])", service, service_md5sum, md5sum);
                return false;
            }
            if (persistent)
            {
                if (server_link == null)
                {
                    server_link = ServiceManager.Instance.createServiceServerLink<MReq, MRes>(service, persistent, service_md5sum, service_md5sum, header_values);
                    if (server_link == null)
                    {
                        return false;
                    }
                }
            }
            else
            {
                server_link = ServiceManager.Instance.createServiceServerLink<MReq, MRes>(service, persistent, service_md5sum, service_md5sum, header_values);
            }
            if (server_link == null)
            {
                shutdown();
                return false;
            }
            var serviceServerLink = server_link as ServiceServerLink<MReq, MRes>;
            if (serviceServerLink == null)
            {
                return false;
            }
            bool ret = serviceServerLink.call(request, ref response);
            while (ROS._shutting_down && ROS.ok)
            {
                Thread.Sleep(new TimeSpan(0, 0, 0, 0, 1));
            }
            if (!persistent)
            {
                serviceServerLink = null;
                server_link.connection.drop(Connection.DropReason.Destructing);
            }
            return ret;
        }
    }

    public class ServiceClient<MSrv> : IServiceClient
        where MSrv : IRosService, new()
    {
        internal ServiceClient(string service, bool persistent, IDictionary header_values, string md5sum)
        {
            this.service = service;
            this.persistent = persistent;
            this.header_values = header_values;
            this.md5sum = md5sum;
            if (persistent)
            {
                server_link = ServiceManager.Instance.createServiceServerLink<MSrv>(service, persistent, md5sum, md5sum, header_values);
            }
        }

        public bool call(MSrv srv)
        {
            string md5 = srv.RequestMessage.MD5Sum;
            return call(srv, md5);
        }

        public bool call(MSrv srv, string service_md5sum)
        {
            if (service_md5sum != md5sum)
            {
                EDB.WriteLine("Call to service [{0} with md5sum [{1} does not match md5sum when the handle was created([{2}])", service, service_md5sum, md5sum);
                return false;
            }
            if (server_link != null && server_link.connection.dropped)
            {
                server_link = null;
            }
            if (persistent)
            {
                if (server_link == null)
                {
                    server_link = ServiceManager.Instance.createServiceServerLink<MSrv>(service, persistent, service_md5sum, service_md5sum, header_values);
                    if (server_link == null)
                    {
                        return false;
                    }
                }
            }
            else
            {
                server_link = ServiceManager.Instance.createServiceServerLink<MSrv>(service, persistent, service_md5sum, service_md5sum, header_values);
            }
            if (server_link == null)
            {
                shutdown();
                return false;
            }
            var serviceServerLink = server_link as ServiceServerLink<MSrv>;
            bool ret = serviceServerLink != null && serviceServerLink.call(srv.RequestMessage, ref srv.ResponseMessage);
            while (ROS._shutting_down && ROS.ok)
            {
                Thread.Sleep(new TimeSpan(0, 0, 0, 0, 1));
            }
            if (!persistent)
            {
                serviceServerLink = null;
                server_link.connection.drop(Connection.DropReason.Destructing);
            }
            return ret;
        }
    }

    public class IServiceClient
    {
        internal double constructed =
            (int) Math.Floor(DateTime.Now.Subtract(Process.GetCurrentProcess().StartTime).TotalMilliseconds);

        internal IDictionary header_values;
        internal bool is_shutdown;
        internal string md5sum;
        internal bool persistent;
        internal IServiceServerLink server_link;
        internal string service;

        protected IServiceClient()
        {
        }

        public bool IsValid
        {
            get { return !persistent || (!is_shutdown && server_link != null && server_link.IsValid); }
        }

        public void shutdown()
        {
            if (!is_shutdown)
            {
                if (!persistent)
                {
                    is_shutdown = true;
                }

                if (server_link != null)
                {
                    ServiceManager.Instance.removeServiceServerLink(server_link);
                    server_link = null;
                }
            }
        }
    }
}