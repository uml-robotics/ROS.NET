// File: iROSImage.cs
// Project: ROS_ImageWPF
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 09/25/2015
// Updated: 10/07/2015

namespace ROS_ImageWPF
{
    public interface iROSImage
    {
        GenericImage getGenericImage();
        void Desubscribe();
        void Resubscribe();
        bool IsSubscribed();
    }
}