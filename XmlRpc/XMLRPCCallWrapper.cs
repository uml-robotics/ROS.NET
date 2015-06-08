// File: XMLRPCCallWrapper.cs
// Project: XmlRpc_Wrapper
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 11/06/2013
// Updated: 07/23/2014

#region USINGZ

//#define REFDEBUGWrapper
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

#endregion

namespace XmlRpc
{
    public class XmlRpcServerMethod //: IDisposable
    {

        private XMLRPCFunc _FUNC;

        public string name;
        public XmlRpcServer server;

        [DebuggerStepThrough]
		public XmlRpcServerMethod(string function_name, XMLRPCFunc func, XmlRpcServer server)
        {
            name = function_name;
            this.server = server;
            //__instance = create(function_name, server.instance);
            //AddRef(__instance);
            //SegFault();
            FUNC = func;
        }


        public XMLRPCFunc FUNC
        {
            [DebuggerStepThrough] get { return _FUNC; }
            [DebuggerStepThrough] set { SetFunc((_FUNC = value)); }
        }

        #region IDisposable Members

        #endregion

        public void SetFunc(XMLRPCFunc func)
        {
			_FUNC = func;
            //SegFault();
            //setfunc(instance, func);
        }

        public void Execute(XmlRpcValue parms, XmlRpcValue reseseses)
        {
            //SegFault();
            //reseseses = new XmlRpcValue();
            //execute(parms, reseseses);
			_FUNC(parms, reseseses);
        }

		public virtual string Help()
		{
			return "no help";
		}

        #region P/Invoke

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcServerMethod_Create",
            CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr create([In] [Out] [MarshalAs(UnmanagedType.LPStr)] string name, IntPtr server);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcServerMethod_SetFunc",
            CallingConvention = CallingConvention.Cdecl)]
        private static extern void setfunc(IntPtr target, [MarshalAs(UnmanagedType.FunctionPtr)] XMLRPCFunc cb);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcServerMethod_Execute",
            CallingConvention = CallingConvention.Cdecl)]
        private static extern void execute(IntPtr target, [In] [Out] IntPtr parms, [In] [Out] IntPtr res);

        #endregion
    }
    //[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void XMLRPCFunc(XmlRpcValue parms, XmlRpcValue reseseses);
}