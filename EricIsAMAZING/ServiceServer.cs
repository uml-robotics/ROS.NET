#region Using

using System;
using System.Diagnostics;

#endregion

namespace Ros_CSharp
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

        public void shutdown()
        {
            if (impl != null)
                impl.unadvertise();
        }

        public string getService()
        {
            if (impl != null && impl.IsValid)
            {
                return impl.service;
            }
            return "";
        }
    }

    public class IServiceServer
    {
        public Impl impl = new Impl();

        #region Nested type: Impl

        public class Impl
        {
            public double constructed =
                (int) Math.Floor(DateTime.Now.Subtract(Process.GetCurrentProcess().StartTime).TotalMilliseconds);

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