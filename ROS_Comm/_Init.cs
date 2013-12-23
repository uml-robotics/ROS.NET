// File: _Init.cs
// Project: ROS_C-Sharp
// 
// ROS#
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 03/04/2013
// Updated: 07/26/2013

#region USINGZ

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using Messages;
using XmlRpc_Wrapper;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace Ros_CSharp
{
    /// <summary>
    ///     Helper class for display and/or other output of debugging/peripheral information
    /// </summary>
    public static class EDB
    {
        #region Delegates

        /// <summary>
        ///     This delegate and associated event can be used for logging of all output from EDB to something controlled by the
        ///     program using ROS_Sharp, such as file, or other
        /// </summary>
        public delegate void otheroutput(object o);

        #endregion

        public static event otheroutput OtherOutput;

        //does the actual writing
        private static void _writeline(object o)
        {
            if (OtherOutput != null)
                OtherOutput(o);
#if DEBUG
            Debug.WriteLine(o);
#else
            Console.WriteLine(o);
#endif
        }

#if DEBUG
#else
    /// Writes a string or something to System.Console, and fires an optional OtherOutput event for use in the node
#endif

        /// <summary>
        ///     Writes a string or something to System.Debug, and fires an optional OtherOutput event for use in the node
        /// </summary>
        /// <param name="o"> A string or something to print </param>
        [DebuggerStepThrough]
        public static void WriteLine(object o)
        {
            _writeline(o);
        }

#if DEBUG
#else
    /// Writes a formatted something to System.Console, and fires an optional OtherOutput event for use in the node
#endif

        /// <summary>
        ///     Writes a formatted something to System.Debug, and fires an optional OtherOutput event for use in the node
        /// </summary>
        /// <param name="format">Format string</param>
        /// <param name="args">Stuff to format</param>
        [DebuggerStepThrough]
        public static void WriteLine(string format, params object[] args)
        {
            if (args != null && args.Length > 0)
                _writeline(string.Format(format, args));
            else
                _writeline(format);
        }
    }

    /// <summary>
    ///     Everything happens here.
    /// </summary>
    public static class ROS
    {
        public static TimerManager timer_manager = new TimerManager();

        public static CallbackQueue GlobalCallbackQueue;
        internal static bool initialized, started, atexit_registered, _ok;

        internal static bool shutting_down, shutdown_requested;
        internal static int init_options;
        public static string ROS_MASTER_URI;
        public static string ROS_HOSTNAME;
        public static string ROS_IP;
        private static object start_mutex = new object();

        /// <summary>
        ///     general global sleep time in miliseconds
        /// </summary>
        public static int WallDuration = 20;

        internal static RosOutAppender rosoutappender;
        public static NodeHandle GlobalNodeHandle;
        private static object shutting_down_mutex = new object();
        private static bool dictinit;

        private static Dictionary<string, Type> typedict = new Dictionary<string, Type>();

        public static bool ok
        {
            get { return _ok; }
        }

        /// <summary>
        ///     Gets the current thread's TID, emulating the behavior ROS has in a more interprocess situation on xnix
        /// </summary>
        /// <returns> </returns>
        public static UInt64 getPID()
        {
            return (UInt64) Thread.CurrentThread.ManagedThreadId;
        }

        /// <summary>
        ///     Turns a DateTime into a Time struct
        /// </summary>
        /// <param name="time"> DateTime to convert </param>
        /// <returns> containing secs, nanosecs since 1/1/1970 </returns>
        public static m.Time GetTime(DateTime time)
        {
            return GetTime(time.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)));
        }

        /// <summary>
        ///     Turns a TimeSpan into a Time (not a Duration, although it sorta is)
        /// </summary>
        /// <param name="timestamp"> The timespan to convert to seconds/nanoseconds </param>
        /// <returns> a time struct </returns>
        public static m.Time GetTime(TimeSpan timestamp)
        {
            uint seconds = (((uint) Math.Floor(timestamp.TotalSeconds) & 0xFFFFFFFF));
            uint nanoseconds = ((uint) Math.Floor(((timestamp.TotalSeconds - seconds)*1000000000)));
            m.Time stamp = new m.Time(new TimeData(seconds, nanoseconds));
            return stamp;
        }

        /// <summary>
        ///     Gets the current time as secs/nsecs
        /// </summary>
        /// <returns> </returns>
        public static m.Time GetTime()
        {
            return GetTime(DateTime.Now);
        }

        /// <summary>
        ///     This is self-explanatory
        /// </summary>
        /// <param name="type"> The type of message to make </param>
        /// <returns> A message of that type </returns>
        internal static IRosMessage MakeMessage(MsgTypes type)
        {
            return IRosMessage.generate(type);
        }

        /// <summary>
        ///     If this happens, then the fact that there's a static function called FREAKOUT exists is the least of your problems.
        /// </summary>
        public static void FREAKOUT()
        {
            throw new Exception("ROS IS FREAKING OUT!");
        }

        /// <summary>
        ///     ROS_INFO(...)
        /// </summary>
        /// <param name="o"> ... </param>
        [DebuggerStepThrough]
        public static void Info(object o)
        {
            if (initialized && rosoutappender != null)
                rosoutappender.Append((string) o, RosOutAppender.ROSOUT_LEVEL.INFO);
        }

        /// <summary>
        ///     ROS_INFO(...) (formatted)
        /// </summary>
        /// <param name="format"> format string </param>
        /// <param name="args"> ... </param>
        [DebuggerStepThrough]
        public static void Info(string format, params object[] args)
        {
            string s = string.Format(format, args);
            Console.WriteLine("[Info] " + s);
            Info((object) s);
        }

        /// <summary>
        ///     ROS_DEBUG(...)
        /// </summary>
        /// <param name="o"> ... </param>
        [DebuggerStepThrough]
        public static void Debug(object o)
        {
            if (initialized && rosoutappender != null)
                rosoutappender.Append((string) o, RosOutAppender.ROSOUT_LEVEL.DEBUG);
        }

        /// <summary>
        ///     ROS_DEBUG(...) (formatted)
        /// </summary>
        /// <param name="format"> format string </param>
        /// <param name="args"> ... </param>
        [DebuggerStepThrough]
        public static void Debug(string format, params object[] args)
        {
            Debug((object) string.Format(format, args));
        }

        /// <summary>
        ///     ROS_ERROR(...)
        /// </summary>
        /// <param name="o"> ... </param>
        [DebuggerStepThrough]
        public static void Error(object o)
        {
            if (initialized && rosoutappender != null)
                rosoutappender.Append((string) o, RosOutAppender.ROSOUT_LEVEL.ERROR);
        }

        /// <summary>
        ///     ROS_INFO(...) (formatted)
        /// </summary>
        /// <param name="format"> format string </param>
        /// <param name="args"> ... </param>
        [DebuggerStepThrough]
        public static void Error(string format, params object[] args)
        {
            string s = string.Format(format, args);
            Console.WriteLine("[Error] " + s);
            Error((object) string.Format(format, args));
        }

        /// <summary>
        ///     Initializes ROS so nodehandles and nodes can exist
        /// </summary>
        /// <param name="args"> argv - parsed for remapping args (AND PARAMS??) </param>
        /// <param name="name"> the node's name </param>
        //TODO make sure params are parsed
        public static void Init(string[] args, string name)
        {
            Init(args, name, 0);
        }

        /// <summary>
        ///     Initializes ROS so nodehandles and nodes can exist
        /// </summary>
        /// <param name="args"> argv - parsed for remapping args (AND PARAMS??) </param>
        /// <param name="name"> the node's name </param>
        /// <param name="options"> options? </param>
        public static void Init(string[] args, string name, int options)
        {
            // ROS_MASTER_URI/ROS_HOSTNAME definition precedence:
            // 1. explicitely set by program
            // 2. passed in as remap argument
            // 3. environment variable

            IDictionary remapping = new Hashtable();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Contains(":="))
                {
                    string[] chunks = args[i].Split(':');
                    chunks[1] = chunks[1].TrimStart('=').Trim();
                    chunks[0] = chunks[0].Trim();
                    remapping.Add(chunks[0], chunks[1]);
                    switch (chunks[0])
                    {
                            //if already defined, then it was defined by the program, so leave it
                        case "__master":
                            if (string.IsNullOrEmpty(ROS_MASTER_URI)) ROS_MASTER_URI = chunks[1].Trim();
                            break;
                        case "__hostname":
                            if (string.IsNullOrEmpty(ROS_HOSTNAME)) ROS_HOSTNAME = chunks[1].Trim();
                            break;
                    }
                }
            }

            //If ROS.ROS_MASTER_URI was not explicitely set by the program calling Init, and was not passed in as a remapping argument, then try to find it in ENV.
            if (string.IsNullOrEmpty(ROS_MASTER_URI))
            {
                IDictionary _vars;

                //check user env first, then machine if user doesn't have uri defined.
                if ((_vars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User)).Contains("ROS_MASTER_URI")
                    || (_vars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine)).Contains("ROS_MASTER_URI"))
                    ROS_MASTER_URI = (string) _vars["ROS_MASTER_URI"];
            }

            //If ROS.ROS_HOSTNAME was not explicitely set by the program calling Init, check the environment.
            if (string.IsNullOrEmpty(ROS_HOSTNAME))
            {
                IDictionary _vars;

                //check user env first, then machine if user doesn't have uri defined.
                if ((_vars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User)).Contains("ROS_HOSTNAME")
                    || (_vars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine)).Contains("ROS_HOSTNAME"))
                    ROS_HOSTNAME = (string)_vars["ROS_HOSTNAME"];
            }

            //if defined NOW, then add to remapping, or replace remapping (in the case it was explicitly set by program AND was passed as remapping arg)
            if (!string.IsNullOrEmpty(ROS_MASTER_URI))
            {
                if (remapping.Contains("__master"))
                    remapping["__master"] = ROS_MASTER_URI;
                else
                    remapping.Add("__master", ROS_MASTER_URI);
            }
            else
                //this is fatal
                throw new Exception("Unknown ROS_MASTER_URI\n" + @"ROS_MASTER_URI needs to be defined for your program to function.
Either:
    set an environment variable called ROS_MASTER_URI,
    pass a __master remapping argument to your program, 
    or set the URI explicitely in your program before calling Init.");

            if (!string.IsNullOrEmpty(ROS_HOSTNAME))
            {
                if (remapping.Contains("__hostname"))
                    remapping["__hostname"] = ROS_HOSTNAME;
                else
                    remapping.Add("__hostname", ROS_HOSTNAME);
            }

            if (!string.IsNullOrEmpty(ROS_IP))
            {
                if (remapping.Contains("__ip"))
                    remapping["__ip"] = ROS_IP;
                else
                    remapping.Add("__ip", ROS_IP);
            }

            Init(remapping, name, options);
        }

        /// <summary>
        ///     Initializes ROS so nodehandles and nodes can exist
        /// </summary>
        /// <param name="remapping_args"> Dictionary of remapping args </param>
        /// <param name="name"> node name </param>
        internal static void Init(IDictionary remapping_args, string name)
        {
            Init(remapping_args, name, 0);
        }

        /// <summary>
        ///     Initializes ROS so nodehandles and nodes can exist
        /// </summary>
        /// <param name="remapping_args"> dictionary of remapping args </param>
        /// <param name="name"> node name </param>
        /// <param name="options"> options? </param>
        internal static void Init(IDictionary remapping_args, string name, int options)
        {
            // if we haven't sunk our fangs into the processes jugular so we can tell
            //    when it has stopped kicking, do so now
            if (!atexit_registered)
            {
                atexit_registered = true;
                Process.GetCurrentProcess().EnableRaisingEvents = true;
                Process.GetCurrentProcess().Exited += (o, args) => _shutdown();
            }

            // this needs to exist for connections and stuff to happen
            if (GlobalCallbackQueue == null)
            {
                GlobalCallbackQueue = new CallbackQueue();
            }

            // kick the tires and light the fires
            if (!initialized)
            {
                init_options = options;
                _ok = true;
                network.init(remapping_args);
                master.init(remapping_args);
                this_node.Init(name, remapping_args, options);
                Param.init(remapping_args);
                initialized = true;
                GlobalNodeHandle = new NodeHandle(this_node.Namespace, remapping_args);
            }
        }

        /// <summary>
        ///     shutdowns are async with the call to shutdown. This delays shutting down ROS feels like it.
        /// </summary>
        internal static void checkForShutdown()
        {
            lock (shutting_down_mutex)
            {
                if (!shutdown_requested || shutting_down)
                    return;
            }
            _shutdown();
            shutdown_requested = false;
        }

        /// <summary>
        ///     This is called when rosnode kill is invoked, or something
        /// </summary>
        /// <param name="p"> pointer to unmanaged XmlRpcValue containing params </param>
        /// <param name="r"> pointer to unmanaged XmlRpcValue that will contain return value </param>
        private static void shutdownCallback(IntPtr p, IntPtr r)
        {
            XmlRpcValue parms = XmlRpcValue.LookUp(p);
            int num_params = 0;
            if (parms.Type == TypeEnum.TypeArray)
                num_params = parms.Size;
            if (num_params > 1)
            {
                string reason = parms[1].Get<string>();
                EDB.WriteLine("Shutdown request received.");
                EDB.WriteLine("Reason given for shutdown: [" + reason + "]");
                shutdown();
            }
            XmlRpcManager.Instance.responseInt(1, "", 0)(r);
        }

        /// <summary>
        ///     Hang the current thread until ROS shuts down
        /// </summary>
        public static void waitForShutdown()
        {
            while (_ok)
            {
                Thread.Sleep(WallDuration);
            }
        }

        /// <summary>
        ///     Finishes intialization This is called by the first NodeHandle when it initializes
        /// </summary>
        internal static void start()
        {
            lock (start_mutex)
            {
                if (started) return;
                PollManager.Instance.addPollThreadListener(checkForShutdown);
                XmlRpcManager.Instance.bind("shutdown", shutdownCallback);
                //initInternalTimerManager();
                TopicManager.Instance.Start();
                try
                {
                    ServiceManager.Instance.Start();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                ConnectionManager.Instance.Start();
                PollManager.Instance.Start();
                XmlRpcManager.Instance.Start();

                rosoutappender = new RosOutAppender();

                //Time.Init();
                GlobalCallbackQueue.Enable();

                shutdown_requested = false;
                shutting_down = false;
                started = true;
                _ok = true;
            }
        }

        /// <summary>
        ///     self explanatory
        /// </summary>
        /// <returns> guess </returns>
        public static bool isStarted()
        {
            return started;
        }

        /// <summary>
        ///     Tells ROS that it should shutdown the next time it feels like doing so.
        /// </summary>
        public static void shutdown()
        {
            shutdown_requested = true;
        }

        /// <summary>
        ///     Kills all the things. Called by checkForShutdown
        /// </summary>
        private static void _shutdown()
        {
            lock (shutting_down_mutex)
            {
                if (shutting_down)
                    return;
                shutting_down = true;
                _ok = false;

                EDB.WriteLine("ROS is shutting down.");

                GlobalCallbackQueue.Disable();
                GlobalCallbackQueue.Clear();

                if (started)
                {
                    TopicManager.Instance.shutdown();
                    ServiceManager.Instance.shutdown();
                    PollManager.Instance.shutdown();
                    XmlRpcManager.Instance.shutdown();
                    rosoutappender.shutdown();
                }

                started = false;
                _ok = false;
            }
        }

        /// <summary>
        ///     Turns a string into a type, with magic, introspection, and a dictionary
        /// </summary>
        /// <param name="name"> the name of the type to return </param>
        /// <returns> the type named by name </returns>
        internal static Type GetDataType(string name)
        {
            if (!dictinit)
            {
                dictinit = true;
                foreach (
                    Assembly a in AppDomain.CurrentDomain.GetAssemblies().Union(new[] {Assembly.GetExecutingAssembly()})
                    )
                {
                    foreach (Type t in a.GetTypes())
                    {
                        if (!typedict.ContainsKey(t.ToString()))
                        {
                            typedict.Add(t.ToString(), t);
                        }
                    }
                }
            }
            return typedict[name];
        }
    }

    /// <summary>
    ///     This is probably useless
    /// </summary>
    public enum InitOption
    {
        NosigintHandler = 1 << 0,
        AnonymousName = 1 << 1,
        NoRousout = 1 << 2
    }
}