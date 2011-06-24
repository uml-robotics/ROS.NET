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
            this.service = service;
            this.nodeHandle = nodeHandle;
        }

        public ServiceServer()
        {
            // TODO: Complete member initialization
        }
    }

    public abstract class IServiceServer
    {
        public string service;
        public NodeHandle nodeHandle;
    }
}
