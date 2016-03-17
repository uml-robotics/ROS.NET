// File: AsyncXmlRpcConnection.cs
// Project: XmlRpc_Wrapper
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 03/16/2016
// Updated: 03/17/2016

namespace XmlRpc_Wrapper
{
    public abstract class AsyncXmlRpcConnection
    {
        public abstract void addToDispatch(XmlRpcDispatch disp);

        public abstract void removeFromDispatch(XmlRpcDispatch disp);

        public abstract bool check();
    }
}