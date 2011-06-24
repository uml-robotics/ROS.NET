using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using XmlRpc_Wrapper;
using m = Messages;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;
using System.Threading;

namespace EricIsAMAZING
{
    public static class ROS
    {

        public static CallbackQueue GlobalCalbackQueue;
        public static bool IsInitialized, IsShuttingDown,atexit_registered,ok,init_options, shutting_down;
        public static int int_options;
        public static string ROS_MASTER_URI;
        /*public ROSNode(string RosMasterUri)
        {
            ROS_MASTER_URI = RosMasterUri;
            TopicManager.Instance().Start();
            ServiceManager.Instance().Start();
            ConnectionManager.Instance().Start();
            PollManager.Instance().Start();
            XmlRpcManager.Instance().Start();
        }*/

        public static void Init(string[] args, string name, int options = 0)
        {
        }

        public static void Init(IDictionary remapping_args, string name, int options = 0)
        {
        }
        public static void spinOnce()
        {
        }

        public static void spin()
        {
        }

        public static void spin(Spinner spinner)
        {

        }

        public static void waitForShutdown()
        {
        }

        public static void OK()
        {
        }
        public static void requestShutdown()
        {
        }

        public static void start()
        {
        }

        public static bool isStarted()
        {
            return false;
        }


        public static void removeROSArgs(string[] args, out string[] argsout)
        {
            argsout = null;
        }
    }

    public enum InitOption
    {
        NosigintHandler = 1 << 0,
        AnonymousName = 1 << 1,
        NoRousout = 1 << 2
    }
}

