using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EricIsAMAZING
{
    public class ServiceServer<T, MReq, MRes> : IServiceServer
    {
        public ServiceServer(string service, NodeHandle nodeHandle)
        {
            impl.service = service;
            impl.nodeHandle = nodeHandle;
        }

        public ServiceServer()
        {
            // TODO: Complete member initialization
        }
    }
    public class IServiceServer
    {
        public Impl impl;

        public class Impl
        {
            public string service;
            public NodeHandle nodeHandle;
            public double constructed = (int)Math.Floor(DateTime.Now.Subtract(System.Diagnostics.Process.GetCurrentProcess().StartTime).TotalMilliseconds);
            public bool unadvertised;
            public bool IsValid { get { return !unadvertised; } }
                internal void unadvertise()
            {
                if (!unadvertised)
                {
                    unadvertised = true;
                    ServiceManager.Instance().unadvertiseService(service);
                }
            }
        }
    }
}
