// File: XmlRpcUtil.cs
// Project: XmlRpc_Wrapper
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 04/28/2015
// Updated: 10/07/2015

#region USINGZ

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

#endregion

namespace XmlRpc_Wrapper
{
    public static class XmlRpcUtil
    {
        private static printstr _PRINTSTR;

        public enum XMLRPC_LOG_LEVEL
        {
            NOTHING=0,
            LOW=1,
            MEDIUM=2,
            HIGH=3,
            ULTRA=4,
            EVERYTHING=5
        }

        private static void thisishowawesomeyouare(string s)
        {
            Console.WriteLine("XmlRpc_Wrapper:: " + s);
        }

        public static void ShowOutputFromXmlRpcPInvoke(XMLRPC_LOG_LEVEL verb, printstr handler = null)
        {
            if (handler == null)
                handler = thisishowawesomeyouare;
            if (handler != _PRINTSTR)
            {
                _PRINTSTR = thisishowawesomeyouare;
                SetAwesomeFunctionPtr(_PRINTSTR);
            }
            SetLogLevel((int)verb);
        }

        #region bad voodoo

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void printstr(string s);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "SetStringOutFunc", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SetAwesomeFunctionPtr(
            [MarshalAs(UnmanagedType.FunctionPtr)] printstr callback);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "SetLogLevel", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SetLogLevel(int verb);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcGiblets_Free", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Free(IntPtr val);

        #endregion
    }
}