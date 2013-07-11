#region Using

using System;
using System.Diagnostics;

#endregion

namespace Ros_CSharp
{
    public class ServiceServer
    {
        public ServiceServer(string service, NodeHandle nodeHandle)
        {
            service = service;
            nodeHandle = nodeHandle;
        }

        public void shutdown()
        {
            unadvertise();
        }

        public string getService()
        {
                return service;
        }
        internal double constructed =
            (int) Math.Floor(DateTime.Now.Subtract(Process.GetCurrentProcess().StartTime).TotalMilliseconds);

        internal NodeHandle nodeHandle;
        internal string service="";
        internal bool unadvertised;

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
}