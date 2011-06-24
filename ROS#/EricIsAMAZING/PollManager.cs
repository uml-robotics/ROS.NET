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
    public class PollManager
    {
        private static PollManager _instance;
        public static PollManager Instance()
        {
            if (_instance == null) _instance = new PollManager();
            return _instance;
        }
        public void Start()
        {
            Console.WriteLine("POLEMANAGER STARTED! YOUR MOM MANAGES POLE!");
        }
    }
}
