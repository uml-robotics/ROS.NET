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

namespace XmlRpc_Wrapper
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
            //SegFault();
            FUNC = func;
			if(server != null)
				server.AddMethod(this);
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
        }

        public void Execute(XmlRpcValue parms, XmlRpcValue reseseses)
        {
			_FUNC(parms, reseseses);
        }

		public virtual string Help()
		{
			return "no help";
		}
    }
    //[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	public delegate void XMLRPCFunc(XmlRpcValue parms, XmlRpcValue reseseses);
}