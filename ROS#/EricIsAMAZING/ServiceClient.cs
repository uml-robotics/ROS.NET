using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EricIsAMAZING
{
    public class ServiceClient<MReq, MRes> : IServiceClient
    {
        public ServiceClient(string service, bool persistent, IDictionary header_values, string md5sum)
        {
            impl.service = service;
            impl.persistent = persistent;
            impl.header_values = header_values;
            impl.md5sum = md5sum;
            if (persistent)
            {
                impl.server_link = ServiceManager.Instance().createServiceServerLink(impl.service, impl.persistent, impl.md5sum, impl.md5sum, impl.header_values);
            }
        }

    }

    public class IServiceClient
    {
        public Impl impl;
        public class Impl
        {
            public string service;
            public string md5sum;
            public IDictionary header_values;
            public bool persistent;
            public bool is_shutdown;
            public double constructed = (int)Math.Floor(DateTime.Now.Subtract(System.Diagnostics.Process.GetCurrentProcess().StartTime).TotalMilliseconds);
            public IServiceServerLink server_link;
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
                        server_link.getConnection().drop(Connection.Destructing);
                    }
                }
            }
        }
    }
}
