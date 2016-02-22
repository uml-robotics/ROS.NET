// File: SubscriberCallbacks.cs
// Project: ROS_C-Sharp
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 04/28/2015
// Updated: 02/10/2016

#region Using

#endregion

namespace Ros_CSharp
{
    public class SubscriberCallbacks
    {
        public CallbackQueueInterface Callback;
        public SubscriberStatusCallback connect, disconnect;

        public SubscriberCallbacks() : this(null, null, null)
        {
        }

        public SubscriberCallbacks(SubscriberStatusCallback connectCB, SubscriberStatusCallback disconnectCB,
            CallbackQueueInterface Callback)
        {
            connect = connectCB;
            disconnect = disconnectCB;
            this.Callback = Callback;
        }

        internal ulong Get()
        {
            return ROS.getPID();
        }
    }
}