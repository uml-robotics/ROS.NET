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
        public static void FREAKTHEFUCKOUT()
        {
            throw new Exception("ROS IS FREAKING THE FUCK OUT!");
        }

        public static CallbackQueue GlobalCalbackQueue;
        public static bool initialized, started, atexit_registered, ok, shutting_down,shutdown_requested;
        public static int int_options;
        public static string ROS_MASTER_URI;
        public static object start_mutex = new object();
        /// <summary>
        /// general global sleep time in miliseconds
        /// </summary>
        public static int WallDuration = 100;

        public static void Init(string[] args, string name, int options = 0)
        {
        }

        public static void Init(IDictionary remapping_args, string name, int options = 0)
        {
        }
        public static void spinOnce()
        {
            GlobalCalbackQueue.callAvailable(WallDuration);
        }

        public static void spin()
        {
            spin(new SingleThreadSpinner());
        }

        public static void spin(Spinner spinner)
        {
            spinner.spin();
        }

        public static void waitForShutdown()
        {
        }

        public static void requestShutdown()
        {
        }

        public static Thread internal_queue_thread;

        public static void start()
        {
            lock (start_mutex)
            {
                if (started) return;
                shutdown_requested = false;
                shutting_down = false;
                started = true;
                ok = true;
                //PollManager.Instance().addPollThreadListener(checkForShutdown);
                //XmlRpcManager.Instance().bind("shutdown", shutdownCallback);
                //initInternalTimerManager();
                TopicManager.Instance().Start();
                ServiceManager.Instance().Start();
                ConnectionManager.Instance().Start();
                PollManager.Instance().Start();
                XmlRpcManager.Instance().Start();
                //Time.Init();
                //internal_queue_manager = new Thread(new ThreadStart(internalCallbackQueueThreadFunc));
                //internal_queue_thread.Start();

            }
        }

        public static bool isStarted()
        {
            return false;
        }

        public static void shutdown()
        {
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

