// File: _Init.cs
// Project: ROS_C-Sharp
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 09/01/2015
// Updated: 10/07/2015

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
#if !TRACE
        [DebuggerStepThrough]
#endif
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

        /// Writes a string or something to System.Console, and fires an optional OtherOutput event for use in the node
        /// <summary>
        ///     Writes a string or something to System.Debug, and fires an optional OtherOutput event for use in the node
        /// </summary>
        /// <param name="o"> A string or something to print </param>
#if !TRACE
        [DebuggerStepThrough]
#endif
        public static void WriteLine(object o)
        {
            _writeline(o);
        }

        /// Writes a formatted something to System.Console, and fires an optional OtherOutput event for use in the node
        /// <summary>
        ///     Writes a formatted something to System.Debug, and fires an optional OtherOutput event for use in the node
        /// </summary>
        /// <param name="format">Format string</param>
        /// <param name="args">Stuff to format</param>
#if !TRACE
        [DebuggerStepThrough]
#endif
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
#if !TRACE
    [DebuggerStepThrough]
#endif
    public static class ROS
    {
        public static TimerManager timer_manager = new TimerManager();

        public static CallbackQueue GlobalCallbackQueue;
        internal static bool initialized, started, atexit_registered, _ok;

        internal static bool _shutting_down, shutdown_requested;
        internal static int init_options;

        /// <summary>
        ///     Means of setting ROS_MASTER_URI programatically before Init is called
        ///     Order of precedence: __master:=... > this variable > User Environment Variable > System Environment Variable
        /// </summary>
        public static string ROS_MASTER_URI;

        /// <summary>
        ///     Means of setting ROS_HOSTNAME directly before Init is called
        ///     Order of precedence: __hostname:=... > this variable > User Environment Variable > System Environment Variable
        /// </summary>
        public static string ROS_HOSTNAME;

        /// <summary>
        ///     Means of setting ROS_IP directly before Init is called
        ///     Order of precedence: __ip:=... > this variable > User Environment Variable > System Environment Variable
        /// </summary>
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

        //last sim time time
        private static TimeSpan lastSimTime;
        //last sim time received time (wall)
        private static TimeSpan lastSimTimeReceived;
        

        public static bool shutting_down
        {
            get { return _shutting_down; }
        }

        private static Dictionary<string, Type> typedict = new Dictionary<string, Type>();

        /// <summary>
        ///     True if ROS is ok, false if not
        /// </summary>
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
            return (UInt64)
                Thread.CurrentThread.ManagedThreadId;
        }

        /// <summary>
        ///     Turns a DateTime into a Time struct
        /// </summary>
        /// <param name="time"> DateTime to convert </param>
        /// <returns> containing secs, nanosecs since 1/1/1970 </returns>
        public static m.Time GetTime(DateTime time)
        {
            return GetTime<m.Time>(time.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)));
        }

        #region time helpers
        internal static long ticksFromData(TimeData data)
        {
            return data.sec * TimeSpan.TicksPerSecond + (uint)Math.Floor(data.nsec / 100.0);
        }

        internal static TimeData ticksToData(long ticks)
        {
            uint seconds = (((uint)Math.Floor(ticks / (1.0 * TimeSpan.TicksPerSecond))));
            uint nanoseconds = ((uint)Math.Floor((double)(ticks - (seconds * TimeSpan.TicksPerSecond))) * 100);
            return new TimeData(seconds, nanoseconds);
        }
        #endregion

        /// <summary>
        ///     Turns a std_msgs.Time into a DateTime
        /// </summary>
        /// <param name="time"> std_msgs.Time to convert </param>
        /// <returns> a DateTime </returns>
        public static DateTime GetTime(m.Time time)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0).Add(new TimeSpan(ticksFromData(time.data)));
        }

        /// <summary>
        ///     Turns a std_msgs.Duration into a TimeSpan
        /// </summary>
        /// <param name="time"> std_msgs.Duration to convert </param>
        /// <returns> a TimeSpan </returns>
        public static TimeSpan GetTime(m.Duration duration)
        {
            return new TimeSpan(ticksFromData(duration.data));
        }

        public static T GetTime<T>(TimeSpan ts) where T : IRosMessage, new()
        {
            T test = Activator.CreateInstance(typeof(T), GetTime(ts)) as T;
            return test;
        }

        /// <summary>
        ///     Turns a TimeSpan into a Time (not a Duration, although it sorta is)
        /// </summary>
        /// <param name="timestamp"> The timespan to convert to seconds/nanoseconds </param>
        /// <returns> a time struct </returns>
        public static TimeData GetTime(TimeSpan timestamp)
        {
            if (lastSimTimeReceived != default(TimeSpan))
            {
                timestamp = timestamp.Subtract(lastSimTimeReceived).Add(lastSimTime);
            }
            return ticksToData(timestamp.Ticks);
        }

        /// <summary>
        ///     Gets the current time as secs/nsecs
        /// </summary>
        /// <returns> </returns>
        public static m.Time GetTime()
        {
            return GetTime(DateTime.Now);
        }

        private static void SimTimeCallback(TimeSpan ts)
        {
            lastSimTime = ts;
            lastSimTimeReceived = DateTime.Now.Subtract(new DateTime());
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
            if (Process.GetCurrentProcess().ProcessName != "devenv")
                throw new Exception("ROS IS FREAKING OUT!");
        }

        /// <summary>
        ///     ROS_INFO(...)
        /// </summary>
        /// <param name="o"> ... </param>
#if !TRACE
        [DebuggerStepThrough]
#endif
        public static void Info(object o, int level = 1)
        {
            Console.WriteLine("[INFO] " + o);
            if (initialized && rosoutappender == null)
                rosoutappender = new RosOutAppender();
            if (initialized)
                rosoutappender.Append(o.ToString(), RosOutAppender.ROSOUT_LEVEL.INFO, level + 1);
        }

        /// <summary>
        ///     ROS_INFO(...) (formatted)
        /// </summary>
        /// <param name="format"> format string </param>
        /// <param name="args"> ... </param>
#if !TRACE
        [DebuggerStepThrough]
#endif
        public static void Info(string format, params object[] args)
        {
            Info((object) string.Format(format, args), 2);
        }

        /// <summary>
        ///     ROS_DEBUG(...)
        /// </summary>
        /// <param name="o"> ... </param>
#if !TRACE
        [DebuggerStepThrough]
#endif
        public static void Debug(object o, int level = 1)
        {
#if DEBUG
            Console.WriteLine("[DEBUG] " + o);
#endif
            if (initialized && rosoutappender == null)
                rosoutappender = new RosOutAppender();
            if (initialized)
                rosoutappender.Append(o.ToString(), RosOutAppender.ROSOUT_LEVEL.DEBUG, level + 1);
        }

        /// <summary>
        ///     ROS_DEBUG(...) (formatted)
        /// </summary>
        /// <param name="format"> format string </param>
        /// <param name="args"> ... </param>
#if !TRACE
        [DebuggerStepThrough]
#endif
        public static void Debug(string format, params object[] args)
        {
            Debug((object) string.Format(format, args), 2);
        }

        /// <summary>
        ///     ROS_ERROR(...)
        /// </summary>
        /// <param name="o"> ... </param>
#if !TRACE
        [DebuggerStepThrough]
#endif
        public static void Error(object o, int level = 1)
        {
            Console.WriteLine("[Error] " + o);
            if (initialized && rosoutappender == null)
                rosoutappender = new RosOutAppender();
            if (initialized)
                rosoutappender.Append(o.ToString(), RosOutAppender.ROSOUT_LEVEL.ERROR, level + 1);
        }

        /// <summary>
        ///     ROS_INFO(...) (formatted)
        /// </summary>
        /// <param name="format"> format string </param>
        /// <param name="args"> ... </param>
#if !TRACE
        [DebuggerStepThrough]
#endif
        public static void Error(string format, params object[] args)
        {
            Error((object) string.Format(format, args), 2);
        }

        /// <summary>
        ///     ROS_WARN(...)
        /// </summary>
        /// <param name="o"> ... </param>
#if !TRACE
        [DebuggerStepThrough]
#endif
        public static void Warn(object o, int level = 1)
        {
            Console.WriteLine("[Warn] " + o);
            if (initialized && rosoutappender == null)
                rosoutappender = new RosOutAppender();
            if (initialized)
                rosoutappender.Append((string) o, RosOutAppender.ROSOUT_LEVEL.WARN, level + 1);
        }

        /// <summary>
        ///     ROS_WARN(...) (formatted)
        /// </summary>
        /// <param name="format"> format string </param>
        /// <param name="args"> ... </param>
#if !TRACE
        [DebuggerStepThrough]
#endif
        public static void Warn(string format, params object[] args)
        {
            Warn((object) string.Format(format, args), 2);
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
            if (Process.GetCurrentProcess().ProcessName == "devenv")
                return;
            // ROS_MASTER_URI/ROS_HOSTNAME definition precedence:
            // 1. explicitely set by program
            // 2. passed in as remap argument
            // 3. environment variable

            IDictionary remapping;
            if (RemappingHelper.GetRemappings(ref args, out remapping))
                Init(remapping, name, options);
            else
                throw new Exception("Init failed");
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
                Process.GetCurrentProcess().Exited += (o, args) =>
                                                          {
                                                              _shutdown();
                                                              waitForShutdown();
                                                          };
                Console.CancelKeyPress += (o, args) =>
                                              {
                                                  _shutdown();
                                                  waitForShutdown();
                                                  args.Cancel = true;
                                              };
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
                SimTime.instance.SimTimeEvent += SimTimeCallback;
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
                if (!shutdown_requested || _shutting_down)
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

                //Time.Init();
                GlobalCallbackQueue.Enable();

                shutdown_requested = false;
                _shutting_down = false;
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
                if (_shutting_down)
                    return;
                _shutting_down = true;

                EDB.WriteLine("ROS is shutting down.");
            }

            if (started)
            {
                GlobalNodeHandle.shutdown();
                TopicManager.Instance.shutdown();
                ServiceManager.Instance.shutdown();
                PollManager.Instance.shutdown();
                XmlRpcManager.Instance.shutdown();
                if (rosoutappender != null)
                    rosoutappender.shutdown();
            }
            started = false;
            _ok = false;

            GlobalCallbackQueue.Disable();
            GlobalCallbackQueue.Clear();
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