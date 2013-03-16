using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using XmlRpcClient = XmlRpc_Wrapper.XmlRpcClient;
namespace rosmaster
{
    class Master
    {
        private String _ROS_MASTER_URI;
        private String _uri;
        private XmlRpcManager master_node;
        private Master_API.ROSMasterHandler handler;

        public Master(String ROS_MASTER_URI)
        {
            _ROS_MASTER_URI = ROS_MASTER_URI;
            //create handler?

//          Start the ROS Master. 

            //handler = new rosmaster.Master_API.ROSMasterHandler();
            //master_node = new XmlRpcManager();//roslib.xmlrpc.XmlRpcNode(self.port, handler) 
            //master_node.start();
    
        }

        public void start()
        {
            //creatre handler??
            Console.WriteLine("Master started.... ");
            
            handler = new Master_API.ROSMasterHandler();
            master_node = new XmlRpcManager();
            master_node.Start(_ROS_MASTER_URI);

            while (master_node.uri == "")
            {
                Thread.Sleep(10);
            }

            Console.WriteLine("Master connected! FUCK YEAH " + master_node.uri);
            _uri = master_node.uri;
        }

        public void stop()
        {
            if (master_node != null)
            {
                master_node.shutdown();
                master_node = null;
            }
        }

        public bool ok()
        {
            if (master_node != null)
                return true;
            else 
                return false;
        }
    }
}
