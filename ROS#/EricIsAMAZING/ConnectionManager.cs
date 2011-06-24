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
    public class ConnectionManager
    {
        private static ConnectionManager _instance;
        public static ConnectionManager Instance()
        {
            if (_instance == null) _instance = new ConnectionManager();
            return _instance;
        }
        public int GetNewConnectionID()
        {
            throw new NotImplementedException("IMPLEMENT THIS ID SHIT FOOL");
        }

        public void Start()
        {
            Console.WriteLine("... and connection manager started too.");
        }
    }
}
