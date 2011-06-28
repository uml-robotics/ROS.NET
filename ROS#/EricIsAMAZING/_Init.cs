using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
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
        public static CallbackQueue GlobalCallbackQueue;
        public static bool initialized, started, atexit_registered, ok, shutting_down,shutdown_requested;
        public static int init_options;
        public static string ROS_MASTER_URI;
        public static object start_mutex = new object();
        /// <summary>
        /// general global sleep time in miliseconds
        /// </summary>
        public static int WallDuration = 100;

        public static void Init(string[] args, string name, int options = 0)
        {
            IDictionary dick = new Hashtable();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Contains(":="))
                {
                    string[] chunks = args[i].Split(':');
                    chunks[1].TrimStart('=');
                    chunks[0].Trim();
                    chunks[1].Trim();
                    dick.Add(chunks[0], chunks[1]);
                }
            }
            Init(dick, name, options);
        }

        public static void Init(IDictionary remapping_args, string name, int options = 0)
        {
            if (!atexit_registered)
            {
                atexit_registered = true;
                System.Diagnostics.Process.GetCurrentProcess().EnableRaisingEvents = true;
                System.Diagnostics.Process.GetCurrentProcess().Exited += (o, args) => shutdown();
            }

            if (GlobalCallbackQueue == null)
            {
                GlobalCallbackQueue = new CallbackQueue();
            }

            if (!initialized)
            {
                init_options = options;
                ok = true;
                network.init(remapping_args);
                master.init(remapping_args);
                this_node.Init(name, remapping_args, options);
                Param.init(remapping_args);
                initialized = true;
            }
        }
        public static void spinOnce()
        {
            GlobalCallbackQueue.callAvailable(WallDuration);
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
                internal_queue_thread = new Thread(new ThreadStart(internalCallbackQueueThreadFunc));
                internal_queue_thread.IsBackground = true;
                internal_queue_thread.Start();
                GlobalCallbackQueue.Enable();
            }
        }

        public static void internalCallbackQueueThreadFunc()
        {

        }

        public static bool isStarted()
        {
            return false;
        }


        public static object shutting_down_mutex = new object();
        public static void shutdown()
        {
            lock (shutting_down_mutex)
            {
                if (shutting_down)
                    return;
                else
                    shutting_down = true;

                Console.WriteLine("We're going down down....");

                GlobalCallbackQueue.Disable();
                GlobalCallbackQueue.Clear();
                internal_queue_thread.Join();

                if (started)
                {
                    TopicManager.Instance().shutdown();
                    ServiceManager.Instance().shutdown();
                    PollManager.Instance().shutdown();
                    XmlRpcManager.Instance().shutdown();
                }

                started = false;
                ok = false;
            }
        }

        public static void removeROSArgs(string[] args, out string[] argsout)
        {
            List<string> argssss = new List<string>();
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (!arg.Contains(":="))
                    argssss.Add(arg);
            }
            argsout = argssss.ToArray();
        }

        public static Type GetDataType(string name)
        {
            if (!dictinit)
            {
                dictinit = true;
                foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies().Union(new Assembly[] { Assembly.GetExecutingAssembly() }))
                {
                    foreach (Type t in a.GetTypes())
                    {
                        if (!typedict.ContainsKey(t.ToString()))
                        {
                            typedict.Add(t.ToString(), t);
                            Console.WriteLine(t.ToString());
                        }
                    }
                }
            }
            return typedict[name];
        }

        private static bool dictinit = false;
        private static Dictionary<string, Type> typedict = new Dictionary<string, Type>();
    }

    public enum InitOption
    {
        NosigintHandler = 1 << 0,
        AnonymousName = 1 << 1,
        NoRousout = 1 << 2
    }
}

