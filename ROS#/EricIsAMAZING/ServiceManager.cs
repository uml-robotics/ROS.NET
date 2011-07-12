#region USINGZ

using System;
using System.Collections;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace EricIsAMAZING
{
    public class ServiceManager
    {
        private static ServiceManager _instance;

        public static ServiceManager Instance
        {
            get
            {
                if (_instance == null) _instance = new ServiceManager();
                return _instance;
            }
        }

        public void Start()
        {
            Console.WriteLine("STARTING SERVICEMANAGER... SERVICE DEEZ NUTS!");
        }

        internal bool advertiseService<MReq, MRes>(AdvertiseServiceOptions<MReq, MRes> ops)
        {
            throw new NotImplementedException();
        }

        internal void unadvertiseService(string service)
        {
            throw new NotImplementedException();
        }

        internal IServiceServerLink createServiceServerLink(string name, bool persistent, string md5sum, string md5sum_2, IDictionary header_values)
        {
            return new IServiceServerLink(name, persistent, md5sum, md5sum_2, header_values);
        }

        internal void shutdown()
        {
            throw new NotImplementedException();
        }
    }
}