using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EricIsAMAZING
{
    public class ServiceClientOptions
    {
        private string service_name;
        private bool persistent;
        private System.Collections.IDictionary header_values;

        public ServiceClientOptions(string service_name, bool persistent, System.Collections.IDictionary header_values)
        {
            // TODO: Complete member initialization
            this.service_name = service_name;
            this.persistent = persistent;
            this.header_values = header_values;
        }
    }
}
