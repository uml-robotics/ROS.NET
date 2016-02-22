// File: RemappingHelper.cs
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
using System.Collections.Generic;
using System.Linq;

#endregion

namespace Ros_CSharp
{
    public static class RemappingHelper
    {
        public static bool GetRemappings(ref string[] args, out IDictionary remapping)
        {
            remapping = new Hashtable();
            List<string> toremove = new List<string>();
            if (args != null)
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].Contains(":="))
                    {
                        string[] chunks = args[i].Split(new[] {':'}, 2); // Handles master URIs with semi-columns such as http://IP
                        chunks[1] = chunks[1].TrimStart('=').Trim();
                        chunks[0] = chunks[0].Trim();
                        remapping.Add(chunks[0], chunks[1]);
                        switch (chunks[0])
                        {
                                //if already defined, then it was defined by the program, so leave it
                            case "__master":
                                if (string.IsNullOrEmpty(ROS.ROS_MASTER_URI)) ROS.ROS_MASTER_URI = chunks[1].Trim();
                                break;
                            case "__hostname":
                                if (string.IsNullOrEmpty(ROS.ROS_HOSTNAME)) ROS.ROS_HOSTNAME = chunks[1].Trim();
                                break;
                        }
                        toremove.Add(args[i]);
                    }
                    args = args.Except(toremove).ToArray();
                }

            //If ROS.ROS_MASTER_URI was not explicitely set by the program calling Init, and was not passed in as a remapping argument, then try to find it in ENV.
            if (string.IsNullOrEmpty(ROS.ROS_MASTER_URI))
            {
                IDictionary _vars;

                //check user env first, then machine if user doesn't have uri defined.
                if ((_vars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User)).Contains("ROS_MASTER_URI")
                    || (_vars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine)).Contains("ROS_MASTER_URI"))
                    ROS.ROS_MASTER_URI = (string) _vars["ROS_MASTER_URI"];
            }

            //If ROS.ROS_HOSTNAME was not explicitely set by the program calling Init, check the environment.
            if (string.IsNullOrEmpty(ROS.ROS_HOSTNAME))
            {
                IDictionary _vars;

                //check user env first, then machine if user doesn't have uri defined.
                if ((_vars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User)).Contains("ROS_HOSTNAME")
                    || (_vars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine)).Contains("ROS_HOSTNAME"))
                    ROS.ROS_HOSTNAME = (string) _vars["ROS_HOSTNAME"];
            }

            //if defined NOW, then add to remapping, or replace remapping (in the case it was explicitly set by program AND was passed as remapping arg)
            if (!string.IsNullOrEmpty(ROS.ROS_MASTER_URI))
            {
                if (remapping.Contains("__master"))
                    remapping["__master"] = ROS.ROS_MASTER_URI;
                else
                    remapping.Add("__master", ROS.ROS_MASTER_URI);
            }
            else
                //this is fatal
                throw new Exception("Unknown ROS_MASTER_URI\n" + @"ROS_MASTER_URI needs to be defined for your program to function.
Either:
    set an environment variable called ROS_MASTER_URI,
    pass a __master remapping argument to your program, 
    or set the URI explicitely in your program before calling Init.");

            if (!string.IsNullOrEmpty(ROS.ROS_HOSTNAME))
            {
                if (remapping.Contains("__hostname"))
                    remapping["__hostname"] = ROS.ROS_HOSTNAME;
                else
                    remapping.Add("__hostname", ROS.ROS_HOSTNAME);
            }

            if (!string.IsNullOrEmpty(ROS.ROS_IP))
            {
                if (remapping.Contains("__ip"))
                    remapping["__ip"] = ROS.ROS_IP;
                else
                    remapping.Add("__ip", ROS.ROS_IP);
            }
            return true;
        }
    }
}