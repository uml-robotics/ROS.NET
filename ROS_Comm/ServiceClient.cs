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
            linkmaker = () => ServiceManager.Instance.createServiceServerLink<MReq, MRes>(service, persistent, md5sum, md5sum, header_values);
            if (persistent)
            {
                server_link = linkmaker();
            }
        }

        public bool call(MReq request, ref MRes response)
        {
            string md5 = request.MD5Sum();
            return call(request, ref response, md5);
        }

        public bool call(MReq request, ref MRes response, string service_md5sum)
        {
            if (!precall(service_md5sum) || server_link == null)
            {
                shutdown();
                return false;
            }
            var serviceServerLink = server_link as ServiceServerLink<MReq, MRes>;
            return postcall(serviceServerLink != null && serviceServerLink.call(request, ref response));
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
            linkmaker = () => ServiceManager.Instance.createServiceServerLink<MSrv>(service, persistent, md5sum, md5sum, header_values);
            if (persistent)
            {
                server_link = linkmaker();
            }
        }

        public bool call(MSrv srv)
        {
            string md5 = srv.RequestMessage.MD5Sum();
            return call(srv, md5);
        }

        public bool call(MSrv srv, string service_md5sum)
        {
            if (!precall(service_md5sum) || server_link == null)
            {
                shutdown();
                return false;
            }
            var serviceServerLink = server_link as ServiceServerLink<MSrv>;
            return postcall(serviceServerLink != null && serviceServerLink.call(srv));
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
        protected delegate IServiceServerLink ServerLinkMakerDelegate();
        protected ServerLinkMakerDelegate linkmaker;

        protected IServiceClient()
        {
        }

        public bool IsValid
        {
            get { return !persistent || (!is_shutdown && server_link != null && server_link.IsValid); }
        }

        protected bool precall(string service_md5sum)
        {
            if (service_md5sum != md5sum)
            {
                EDB.WriteLine("Call to service [{0} with md5sum [{1} does not match md5sum when the handle was created([{2}])", service, service_md5sum, md5sum);
                return false;
            }
            if (server_link != null && server_link.connection.dropped)
            {
                if (persistent)
                    EDB.WriteLine("WARNING: persistent service client's server link has been dropped. trying to reconnect to proceed with this call");
                server_link = null;
            }
            if (is_shutdown && persistent)
                EDB.WriteLine("WARNING: persistent service client is self-resurrecting");
            is_shutdown = false;
            if (persistent && server_link == null || !persistent)
            {
                server_link = linkmaker();
            }
            return true;
        }

        protected bool postcall(bool retval)
        {
            while (ROS._shutting_down && ROS.ok)
            {
                Thread.Sleep(new TimeSpan(0, 0, 0, 0, ROS.WallDuration));
            }
            return retval;
        }

        public void shutdown()
        {
            if (!is_shutdown)
            {
                is_shutdown = true;
                if (!persistent && server_link != null)
                {
                    ServiceManager.Instance.removeServiceServerLink(server_link);
                    server_link = null;
                }
            }
        }
    }
}