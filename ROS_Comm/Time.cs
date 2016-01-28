using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using m = Messages.std_msgs;

namespace Ros_CSharp
{
    internal class SimTime
    {
        private static object _instanceLock = new object();
        private static SimTime _instance = null;

        public delegate void SimTimeDelegate(TimeSpan ts);

        public event SimTimeDelegate SimTimeEvent;

        private Subscriber<Messages.rosgraph_msgs.Clock> simTimeSubscriber;
        private NodeHandle nh;

        private bool checkedSimTime = false;
        private bool simTime = false;

        public bool IsTimeSimulated
        {
            get { return simTime; }
        }

        private void SimTimeCallback(Messages.rosgraph_msgs.Clock time)
        {
            if (!checkedSimTime)
            {
                if (Param.get("/use_sim_time", ref simTime))
                {
                    checkedSimTime = true;
                    if (simTime)
                    {
                        ROS.Warn("Switching to sim time");
                    }
                }
            }
            if (simTime && SimTimeEvent != null)
                SimTimeEvent.DynamicInvoke(TimeSpan.FromMilliseconds(time.clock.data.sec*1000.0 + (time.clock.data.nsec/100000000.0)));

        }

        public static SimTime instance
        {
            get 
            {
                lock (_instanceLock)
                {
                    if (_instance == null)
                    {
                        _instance = new SimTime();
                    }
                    return _instance;
                }
            }
        }

        public SimTime()
        {
            new Thread(() =>
                           {
                               while (!ROS.isStarted() && !ROS.shutting_down)
                               {
                                   Thread.Sleep(100);
                               }
                               NodeHandle nh = new NodeHandle();
                               if (!ROS.shutting_down)
                               {
                                   simTimeSubscriber = nh.subscribe<Messages.rosgraph_msgs.Clock>("/clock", 1, SimTimeCallback);
                               }
                               ROS.waitForShutdown();
                           }).Start();
        }
    }
}
