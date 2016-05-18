// File: this_node.cs
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
using System.Diagnostics;

#endregion

namespace Ros_CSharp
{
    public static class this_node
    {
        public static string Name = "empty";
        public static string Namespace = "";

        public static void Init(string n, IDictionary remappings)
        {
            Init(n, remappings, 0);
        }

        public static void Init(string n, IDictionary remappings, int options)
        {
            Name = n;
            bool disable_anon = false;
            if (remappings.Contains("__name"))
            {
                Name = (string) remappings["__name"];
                disable_anon = true;
            }
            if (remappings.Contains("__ns"))
            {
                Namespace = (string) remappings["__ns"];
            }
            if (Namespace == "") Namespace = "/";

            long walltime = DateTime.Now.Subtract(Process.GetCurrentProcess().StartTime).Ticks;
            names.Init(remappings);
            if (Name.Contains("/"))
                throw new Exception("NAMES CANT HAVE SLASHES, WENCH!");
            if (Name.Contains("~"))
                throw new Exception("NAMES CANT HAVE SQUIGGLES, WENCH!");
            try
            {
                Name = names.resolve(Namespace, Name);
            }
            catch (Exception e)
            {
                EDB.WriteLine(e);
            }
            if ((options & (int) InitOption.AnonymousName) == (int) InitOption.AnonymousName && !disable_anon)
            {
                int lbefore = Name.Length;
                Name += "_" + walltime;
                if (Name.Length - lbefore > 201)
                    Name = Name.Remove(lbefore + 201);
            }
        }
    }
}