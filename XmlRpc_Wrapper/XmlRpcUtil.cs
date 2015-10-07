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
        private static printint _PRINTINT;
        private static printstr _PRINTSTR;

        private static void thisishowawesomeyouare(string s)
        {
            Debug.WriteLine("XMLRPC NATIVE OUT: " + s);
        }

        public static void ShowOutputFromXmlRpcPInvoke(printstr handler = null)
        {
            if (handler == null)
                handler = thisishowawesomeyouare;
            if (handler != _PRINTSTR)
            {
                _PRINTSTR = thisishowawesomeyouare;
                SetAwesomeFunctionPtr(_PRINTSTR);
            }
        }

        #region bad voodoo

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void printint(int val);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void printstr(string s);

        [DllImport("XmlRpcWin32.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int IntegerEcho(int val);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "IntegerEchoFunctionPtr", CallingConvention = CallingConvention.Cdecl
            )]
        private static extern void IntegerEchoFunctionPtr([MarshalAs(UnmanagedType.FunctionPtr)] printint callback);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "IntegerEchoRepeat", CallingConvention = CallingConvention.Cdecl)]
        private static extern byte IntegerEchoRepeat(int val);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "SetStringOutFunc", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SetAwesomeFunctionPtr(
            [MarshalAs(UnmanagedType.FunctionPtr)] printstr callback);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "StringPassingTest", CallingConvention = CallingConvention.Cdecl)]
        private static extern void StringTest([In] [Out] [MarshalAs(UnmanagedType.LPStr)] string str);


        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcGiblets_Free", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Free(IntPtr val);

        #endregion
    }
}