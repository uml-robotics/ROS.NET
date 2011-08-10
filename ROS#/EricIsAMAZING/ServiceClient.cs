#region USINGZ

using System;
using System.Collections;
using System.Diagnostics;

#endregion

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
                impl.server_link = ServiceManager.Instance.createServiceServerLink(impl.service, impl.persistent,
                                                                                   impl.md5sum, impl.md5sum,
                                                                                   impl.header_values);
            }
            throw new NotImplementedException();
        }
    }

    public class IServiceClient
    {
        public Impl impl;

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
}