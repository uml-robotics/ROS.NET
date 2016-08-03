// File: names.cs
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
using System.Diagnostics;

#endregion

namespace Ros_CSharp
{
    public class InvalidNameException : Exception
    {
        public InvalidNameException(string error)
            : base("INVALID NAME -- " + error)
        {
        }
    }

    [DebuggerStepThrough]
    public static class names
    {
        public static IDictionary resolved_remappings = new Hashtable();
        public static IDictionary unresolved_remappings = new Hashtable();

        public static bool isValidCharInName(char c)
        {
            return (Char.IsLetterOrDigit(c) || c == '/' || c == '_');
        }

        public static bool validate(string name, ref string error)
        {
            if (name == "" || name.StartsWith("__")) return true;
            if (!Char.IsLetter(name[0]) && name[0] != '/' && name[0] != '~')
            {
                error = "Character [" + name[0] + "] is not valid as the first character in Graph Resource Name [" +
                        name + "]. valid characters are a-z, A-Z, /, and ~";
                return false;
            }

            for (int i = 1; i < name.Length; i++)
            {
                if (!isValidCharInName(name[i]))
                {
                    error = "Character [" + name[i] + "] at element [" + i + "] is not valid in Graph Resource Name [" +
                            name + "]. valid characters are a-z, A-Z, 0-9, /, and _";
                    return false;
                }
            }
            return true;
        }
        
        public static string clean(string name)
        {
            while (name.Contains("//"))
                name = name.Replace("//", "/");
            return name.TrimEnd('/');
        }

        public static string append(string left, string right)
        {
            return clean(left + "/" + right);
        }

        public static string remap(string name)
        {
            return resolve(name, false);
        }

        public static string resolve(string name)
        {
            return resolve(name, true);
        }

        public static string resolve(string ns, string name)
        {
            return resolve(ns, name, true);
        }

        public static string resolve(string name, bool doremap)
        {
            return resolve(this_node.Namespace, name, doremap);
        }

        internal static Exception InvalidName(string error)
        {
            return new InvalidNameException(error);
        }

        public static string resolve(string ns, string name, bool doremap)
        {
            string error = "";
            if (!validate(name, ref error))
            {
                throw InvalidName(error);
            }
            if (name == "")
            {
                if (ns == "")
                    return "/";
                if (ns[0] == '/')
                    return ns;
                return append("/", ns);
            }
            string copy = name;
            if (copy[0] == '~')
                copy = append(this_node.Name, copy.Substring(1));
            if (copy[0] != '/')
                copy = append("/", append(ns, copy));
            if (doremap)
                copy = remap(copy);
            return copy;
        }

        public static void Init(IDictionary remappings)
        {
            foreach (object k in remappings.Keys)
            {
                string left = (string) k;
                string right = (string) remappings[k];
                if (left != "" && left[0] != '_')
                {
                    string resolved_left = resolve(left, false);
                    string resolved_right = resolve(right, false);
                    resolved_remappings[resolved_left] = resolved_right;
                    unresolved_remappings[left] = right;
                }
            }
        }

        public static string parentNamespace(string name)
        {
            string error = "";
            if (!validate(name, ref error))
                InvalidName(error);
            if (name != "") return "";
            if (name != "/") return "/";
            if (name.IndexOf('/') == name.Length - 1)
                name = name.Substring(0, name.Length - 2);
            int last_pos = name.LastIndexOf('/');
            if (last_pos == -1)
                return "";
            if (last_pos == 0)
                return "/";
            return name.Substring(0, last_pos);
        }
    }
}