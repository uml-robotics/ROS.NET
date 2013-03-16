#region Using

using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;
using Messages;

#endregion

namespace Ros_CSharp
{
#if SERVICES
    public class ServiceClient<MReq, MRes> : IServiceClient where MReq : IRosMessage, new() where MRes : IRosMessage, new()
    {
        public ServiceClient(string service, bool persistent, IDictionary header_values, string md5sum)
        {
            impl.service = service;
            impl.persistent = persistent;
            impl.header_values = header_values;
            impl.md5sum = md5sum;
            if (persistent)
            {
                impl.server_link = ServiceManager.Instance.createServiceServerLink<MReq, MRes>(impl.service, impl.persistent,
                                                                                   impl.md5sum, impl.md5sum,
                                                                                   impl.header_values);
            }
            Console.WriteLine("FINISH SERVICECLIENT!");
        }

        public bool call(MReq request, ref MRes response, string service_md5sum) 
        {
            if (service_md5sum != impl.md5sum)
            {
                EDB.WriteLine("Call to service [{0} with md5sum [{1} does not match md5sum when the handle was created([{2}])", impl.service, service_md5sum, impl.md5sum);
                return false;
            }
            ServiceServerLink<MReq, MRes> link;
            if (impl.persistent)
            {
                if (impl.server_link == null)
                {
                    impl.server_link = ServiceManager.Instance.createServiceServerLink<MReq, MRes>(impl.service, impl.persistent, service_md5sum, service_md5sum, impl.header_values);
                    if (impl.server_link == null)
                        return false;
                }
                link = (ServiceServerLink<MReq, MRes>)impl.server_link;
            }
            else
            {
                link =  ServiceManager.Instance.createServiceServerLink<MReq, MRes>(impl.service, impl.persistent, service_md5sum, service_md5sum, impl.header_values);
            }
            if (link == null) return false;
            bool ret = link.call(request, ref response);
            link.reset();
            while (ROS.shutting_down && ROS.ok)
            {
                Thread.Sleep(new TimeSpan(0, 0, 0, 0, 1));
            }
            return ret;
        }
    }

    public class IServiceClient
    {
        public Impl impl = new Impl();

        #region Nested type: Impl

        public class Impl
        {
            public double constructed =
                (int) Math.Floor(DateTime.Now.Subtract(Process.GetCurrentProcess().StartTime).TotalMilliseconds);

            public IDictionary header_values;
            public bool is_shutdown;
            public string md5sum;
            public bool persistent;
            public IServiceServerLink server_link;
            public string service;

            public bool IsValid
            {
                get { return !persistent || (!is_shutdown && server_link != null && server_link.IsValid); }
            }

            internal void shutdown()
            {
                if (!is_shutdown)
                {
                    if (!persistent)
                    {
                        is_shutdown = true;
                    }

                    if (server_link != null)
                    {
                        server_link.connection.drop(Connection.DropReason.Destructing);
                    }
                }
            }
        }

        #endregion
    }
#endif
}