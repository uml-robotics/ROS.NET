using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XmlRpc_Wrapper;
using m = Messages;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace EricIsAMAZING
{
    public class ServiceManager
    {
        private static ServiceManager _instance;
        public static ServiceManager Instance()
        {
            if (_instance == null) _instance = new ServiceManager();
            return _instance;
        }

        public void Start()
        {
            Console.WriteLine("STARTING SERVICEMANAGER... SERVICE DEEZ NUTS!");
        }

        internal bool advertiseService<MReq, MRes>(AdvertiseServiceOptions<MReq, MRes> ops)
        {
            throw new NotImplementedException();
        }
    }
}
