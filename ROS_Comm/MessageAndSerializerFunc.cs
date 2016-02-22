// File: MessageAndSerializerFunc.cs
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

#region USINGZ

using Messages;

#endregion

namespace Ros_CSharp
{
    internal class MessageAndSerializerFunc
    {
        internal IRosMessage msg;
        internal bool nocopy;
        internal TopicManager.SerializeFunc serfunc;
        internal bool serialize;

        internal MessageAndSerializerFunc(IRosMessage msg, TopicManager.SerializeFunc serfunc, bool ser, bool nc)
        {
            this.msg = msg;
            this.serfunc = serfunc;
            serialize = ser;
            nocopy = nc;
        }
    }
}