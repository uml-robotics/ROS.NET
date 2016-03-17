// File: XMLRPCCallWrapper.cs
// Project: XmlRpc_Wrapper
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 03/16/2016
// Updated: 03/17/2016

#region USINGZ

//#define REFDEBUGWrapper


#endregion

using System.Diagnostics;

namespace XmlRpc_Wrapper
{
#if !TRACE
    [DebuggerStepThrough]
#endif

    public class XmlRpcServerMethod //: IDisposable
    {
        private XMLRPCFunc _FUNC;

        public string name;
        public XmlRpcServer server;

        public XmlRpcServerMethod(string function_name, XMLRPCFunc func, XmlRpcServer server)
        {
            name = function_name;
            this.server = server;
            //SegFault();
            FUNC = func;
            if (server != null)
                server.AddMethod(this);
        }


        public XMLRPCFunc FUNC
        {
            get { return _FUNC; }
            set { SetFunc((_FUNC = value)); }
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