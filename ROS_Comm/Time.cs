// File: Time.cs
// Project: ROS_C-Sharp
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 10/14/2015
// Updated: 02/10/2016

#region USINGZ

using System;
using System.Threading;
using Messages.rosgraph_msgs;
using m = Messages.std_msgs;

#endregion

namespace Ros_CSharp
{
    public class SimTime
    {
        public delegate void SimTimeDelegate(TimeSpan ts);

        private static object _instanceLock = new object();
        private static SimTime _instance;

        private bool checkedSimTime;
        private NodeHandle nh;
        private bool simTime;
        private Subscriber<Clock> simTimeSubscriber;

        public SimTime()
        {
            new Thread(() =>
                           {
                               while (!ROS.isStarted() && !ROS.shutting_down)
                               {
                                   Thread.Sleep(100);
                               }
                               nh = new NodeHandle();
                               if (!ROS.shutting_down)
                               {
                                   simTimeSubscriber = nh.subscribe<Clock>("/clock", 1, SimTimeCallback);
                               }
                           }).Start();
        }

        public bool IsTimeSimulated
        {
            get { return simTime; }
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

        public event SimTimeDelegate SimTimeEvent;

        private void SimTimeCallback(Clock time)
        {
            if (!checkedSimTime)
            {
                if (Param.get("/use_sim_time", ref simTime))
                {
                    checkedSimTime = true;
                }
            }
            if (simTime && SimTimeEvent != null)
                SimTimeEvent.Invoke(TimeSpan.FromMilliseconds(time.clock.data.sec*1000.0 + (time.clock.data.nsec/100000000.0)));
        }
    }
}