#region USINGZ

using System.Collections;

#endregion

namespace EricIsAMAZING
{
    public class ServiceClientOptions
    {
        public IDictionary header_values;
        public string md5sum;
        public bool persistent;
        public string service;

        public ServiceClientOptions(string service, bool persistent, IDictionary header_values) : this(service, persistent, header_values, "")
        {
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