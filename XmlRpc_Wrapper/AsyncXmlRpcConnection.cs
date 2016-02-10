// File: AsyncXmlRpcConnection.cs
// Project: XmlRpc_Wrapper
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 11/18/2015
// Updated: 02/10/2016

namespace XmlRpc_Wrapper
{
    public abstract class AsyncXmlRpcConnection
    {
        public abstract void addToDispatch(XmlRpcDispatch disp);

        public abstract void removeFromDispatch(XmlRpcDispatch disp);

        public abstract bool check();
    }
}