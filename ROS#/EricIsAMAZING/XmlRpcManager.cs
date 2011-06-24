using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using XmlRpc_Wrapper;
using m=Messages;
using gm=Messages.geometry_msgs;
using nm=Messages.nav_msgs;
using System.Threading;

namespace EricIsAMAZING
{
    public class XmlRpcManager
    {
        private static XmlRpcManager _instance;
        public static XmlRpcManager Instance()
        {
            if (_instance == null) _instance = new XmlRpcManager();
            return _instance;
        }

        public void Start()
        {
            Console.WriteLine("XmlRpc IN THE HIZI FOR SHIZI");
        }
    }
}
