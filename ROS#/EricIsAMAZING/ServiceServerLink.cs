using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace EricIsAMAZING
{
    public class IServiceServerLink
    {
        private string name;
        private bool persistent;
        private string md5sum;
        private string md5sum_2;
        private System.Collections.IDictionary header_values;
        public bool IsValid;
        public Connection connection;
        public IServiceServerLink(string name, bool persistent, string md5sum, string md5sum_2, System.Collections.IDictionary header_values)
        {
            // TODO: Complete member initialization
            this.name = name;
            this.persistent = persistent;
            this.md5sum = md5sum;
            this.md5sum_2 = md5sum_2;
            this.header_values = header_values;
        }
        public IServiceServerLink()
        {
        }

        public static object create(string request, string response)
        {
            Type gen = Type.GetType("ServiceServerLink").MakeGenericType(ROS.GetDataType(request), ROS.GetDataType(response));
            return gen.GetConstructor(null).Invoke(null);
        }
    }

    public class ServiceServerLink<MReq, MRes> : IServiceServerLink
    {
    }

    internal struct CallInfo<MReq, MRes>
    {
        
    }
}
