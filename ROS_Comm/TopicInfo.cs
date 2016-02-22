// File: TopicInfo.cs
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

namespace Ros_CSharp
{
    public class TopicInfo
    {
        public TopicInfo(string name, string data_type)
        {
            // TODO: Complete member initialization
            this.name = name;
            this.data_type = data_type;
        }

        public string data_type { get; set; }
        public string name { get; set; }
    }
}