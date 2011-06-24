using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EricIsAMAZING
{
    public class ServiceClientOptions
    {
        public string service;
        public bool persistent;
        public System.Collections.IDictionary header_values;
        public string md5sum;

        public ServiceClientOptions(string service, bool persistent, System.Collections.IDictionary header_values) : this(service, persistent, header_values, "")
        {
        }

        public ServiceClientOptions(string service, bool persistent, System.Collections.IDictionary header_values, string md5sum)
        {
            // TODO: Complete member initialization
            this.service = service;
            this.persistent = persistent;
            this.header_values = header_values;
            this.md5sum = md5sum;
        }
    }
}
