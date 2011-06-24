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
            this.service = service;
            this.persistent = persistent;
            this.header_values = header_values;
            this.md5sum = md5sum;
        }
    }

    public abstract class IServiceClient
    {
        public string service;
        public string md5sum;
        public IDictionary header_values;
        public bool persistent;
    }
}
