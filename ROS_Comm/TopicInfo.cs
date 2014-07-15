// File: TopicInfo.cs
// Project: ROS_C-Sharp
// 
// ROS#
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 03/04/2013
// Updated: 15/07/2014

namespace Ros_CSharp
{
    public class TopicInfo
    {
        public string data_type { get; set; }
        public string name { get; set; }

        public TopicInfo(string name, string data_type)
        {
            // TODO: Complete member initialization
            this.name = name;
            this.data_type = data_type;
        }
    }
}
