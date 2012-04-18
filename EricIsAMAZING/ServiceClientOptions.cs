#region USINGZ

using System;
using System.Collections;

#endregion

namespace Ros_CSharp
{
    public class ServiceClientOptions
    {
        public IDictionary header_values;
        public string md5sum;
        public bool persistent;
        public string service;

        public ServiceClientOptions(string service, bool persistent, IDictionary header_values)
            : this(service, persistent, header_values, "")
        {
            //throw new NotImplementedException();
        }

        public ServiceClientOptions(string service, bool persistent, IDictionary header_values, string md5sum)
        {
            // TODO: Complete member initialization
            this.service = service;
            this.persistent = persistent;
            this.header_values = header_values;
            this.md5sum = md5sum;
        }
    }
}