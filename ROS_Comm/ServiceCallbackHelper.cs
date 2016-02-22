// File: ServiceCallbackHelper.cs
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
using Messages;

#endregion

namespace Ros_CSharp
{
    public delegate bool ServiceFunction<in MReq, MRes>(MReq req, ref MRes res)
        where MReq : IRosMessage, new()
        where MRes : IRosMessage, new();

    public class ServiceCallbackHelperParams<MReq, MRes> : IServiceCallbackHelperParams
    {
        public new MReq request;
        public new MRes response;
    }

    public class IServiceCallbackHelperParams
    {
        public IDictionary connection_header;
        public IRosMessage request, response;
    }

    public class ServiceCallbackHelper<MReq, MRes> : IServiceCallbackHelper
        where MReq : IRosMessage, new()
        where MRes : IRosMessage, new()
    {
        protected new ServiceFunction<MReq, MRes> _callback;

        public ServiceCallbackHelper(ServiceFunction<MReq, MRes> srv_func)
        {
            // TODO: Complete member initialization
            _callback = srv_func;
        }

        internal bool call(ServiceCallbackHelperParams<MReq, MRes> parms)
        {
            return _callback.Invoke(parms.request, ref parms.response);
        }
    }

    public class IServiceCallbackHelper
    {
        protected ServiceFunction<IRosMessage, IRosMessage> _callback;

        public MsgTypes type;

        protected IServiceCallbackHelper()
        {
            // EDB.WriteLine("ISubscriptionCallbackHelper: 0 arg constructor");
        }

        protected IServiceCallbackHelper(ServiceFunction<IRosMessage, IRosMessage> Callback)
        {
            //EDB.WriteLine("ISubscriptionCallbackHelper: 1 arg constructor");
            //throw new NotImplementedException();
            _callback = Callback;
        }

        public virtual ServiceFunction<IRosMessage, IRosMessage> callback()
        {
            return _callback;
        }

        public virtual ServiceFunction<IRosMessage, IRosMessage> callback(ServiceFunction<IRosMessage, IRosMessage> cb)
        {
            _callback = cb;
            return _callback;
        }

        public virtual MReq deserialize<MReq, MRes>(ServiceCallbackHelperParams<MReq, MRes> parms) where MReq : IRosMessage where MRes : IRosMessage
        {
            //EDB.WriteLine("ISubscriptionCallbackHelper: deserialize");
            IRosMessage msg = ROS.MakeMessage(type);
            assignSubscriptionConnectionHeader(ref msg, parms.connection_header);
            MReq t = (MReq) msg;
            t.Deserialize(parms.response.Serialized);
            return t;
            //return SerializationHelper.Deserialize<T>(parms.buffer);
        }

        private void assignSubscriptionConnectionHeader(ref IRosMessage msg, IDictionary p)
        {
            // EDB.WriteLine("ISubscriptionCallbackHelper: assignSubscriptionConnectionHeader");
            msg.connection_header = new Hashtable(p);
        }
    }
}