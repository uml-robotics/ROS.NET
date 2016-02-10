// File: network.cs
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

using System;
using System.Collections;

#endregion

namespace Ros_CSharp
{
    public static class network
    {
        public static string host;
        public static int tcpros_server_port;

        public static bool splitURI(string uri, ref string host, ref int port)
        {
            if (uri == null)
                throw new Exception("NULL STUFF FAIL!");
            if (uri.Substring(0, 7) == "http://")
                host = uri.Substring(7);
            else if (uri.Substring(0, 9) == "rosrpc://")
                host = uri.Substring(9);
            string[] split = host.Split(':');
            if (split.Length < 2) return false;
            string port_str = split[1];
            port_str = port_str.Trim('/');
            port = int.Parse(port_str);
            host = split[0];
            return true;
        }

        public static bool isPrivateIp(string ip)
        {
            bool b = (String.CompareOrdinal("192.168", ip) >= 7) || (String.CompareOrdinal("10.", ip) > 3) ||
                     (String.CompareOrdinal("169.253", ip) > 7);
            return b;
        }

        public static string determineHost()
        {
            return Environment.MachineName;
        }

        public static void init(IDictionary remappings)
        {
            if (remappings.Contains("__hostname"))
                host = (string) remappings["__hostname"];
            else
            {
                if (remappings.Contains("__ip"))
                    host = (string) remappings["__ip"];
            }

            if (remappings.Contains("__tcpros_server_port"))
            {
                tcpros_server_port = int.Parse((string) remappings["__tcpros_server_port"]);
            }

            if (string.IsNullOrEmpty(host))
                host = determineHost();
        }
    }
}