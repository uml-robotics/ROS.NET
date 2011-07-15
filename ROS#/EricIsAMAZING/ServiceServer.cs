#region USINGZ

using System;
using System.Diagnostics;

#endregion

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
            throw new NotImplementedException();
        }
    }

    public class IServiceServer
    {
        public Impl impl;

        #region Nested type: Impl

        public class Impl
        {
            public double constructed = (int) Math.Floor(DateTime.Now.Subtract(Process.GetCurrentProcess().StartTime).TotalMilliseconds);
            public NodeHandle nodeHandle;
            public string service;
            public bool unadvertised;

            public bool IsValid
            {
                get { return !unadvertised; }
            }

            internal void unadvertise()
            {
                if (!unadvertised)
                {
                    unadvertised = true;
                    ServiceManager.Instance.unadvertiseService(service);
                }
            }
        }

        #endregion
    }
}