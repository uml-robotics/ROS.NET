// File: MessageTypes.cs
// Project: YAMLParser
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 04/28/2015
// Updated: 10/07/2015

namespace FauxMessages
{
    public enum MsgTypes
    {
        Unknown,
        std_msgs__String,
        std_msgs__Header,
        std_msgs__Time,
        std_msgs__Duration,
        std_msgs__Byte,
        std_msgs__UInt8,
        std_msgs__Int8,
        std_msgs__Char
    }

    public enum SrvTypes
    {
        Unknown
    }
}