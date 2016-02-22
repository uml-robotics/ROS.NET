// File: ServiceClientOptions.cs
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

using System.Collections;

#endregion

namespace Ros_CSharp
{
    public class ServiceClientOptions
    {
        public IDictionary header_values;
        public string md5sum;
        public bool persistent;
        public string service;

        public ServiceClientOptions(string service, bool persistent, IDictionary header_values)
            : this(service, persistent, header_values, "")
        {
            //throw new NotImplementedException();
        }

        public ServiceClientOptions(string service, bool persistent, IDictionary header_values, string md5sum)
        {
            // TODO: Complete member initialization
            this.service = service;
            this.persistent = persistent;
            this.header_values = header_values;
            this.md5sum = md5sum;
        }
    }
}