// File: SubscriptionCallbackHelper.cs
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
using Messages;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace Ros_CSharp
{
#if !TRACE
    [DebuggerStepThrough]
#endif
    public class SubscriptionCallbackHelper<M> : ISubscriptionCallbackHelper where M : IRosMessage, new()
    {
        public SubscriptionCallbackHelper(MsgTypes t, CallbackDelegate<M> cb)
        {
            //EDB.WriteLine("SubscriptionCallbackHelper: type and callbackdelegate constructor");
            type = t;
            base.callback(new Callback<M>(cb));
            //if you think about this one too hard, you might die.
        }

        public SubscriptionCallbackHelper(MsgTypes t)
        {
            //EDB.WriteLine("SubscriptionCallbackHelper: type constructor");
            type = t;
        }

        public SubscriptionCallbackHelper(CallbackInterface q)
            : base(q)
        {
            //EDB.WriteLine("SubscriptionCallbackHelper: callbackinterface constructor");
        }

        public override void call(IRosMessage msg)
        {
            //EDB.WriteLine("SubscriptionCallbackHelper: call");
            (callback()).func(msg);
        }
    }

#if !TRACE
    [DebuggerStepThrough]
#endif
    public class ISubscriptionCallbackHelper
    {
        private CallbackInterface _callback;

        public MsgTypes type;

        protected ISubscriptionCallbackHelper()
        {
            // EDB.WriteLine("ISubscriptionCallbackHelper: 0 arg constructor");
        }

        protected ISubscriptionCallbackHelper(CallbackInterface Callback)
        {
            //EDB.WriteLine("ISubscriptionCallbackHelper: 1 arg constructor");
            //throw new NotImplementedException();
            _callback = Callback;
        }

        public virtual CallbackInterface callback()
        {
            return _callback;
        }

        public virtual CallbackInterface callback(CallbackInterface cb)
        {
            _callback = cb;
            return _callback;
        }

        private void assignSubscriptionConnectionHeader(ref IRosMessage msg, IDictionary p)
        {
            // EDB.WriteLine("ISubscriptionCallbackHelper: assignSubscriptionConnectionHeader");
            msg.connection_header = new Hashtable(p);
        }

        public virtual void call(IRosMessage parms)
        {
            // EDB.WriteLine("ISubscriptionCallbackHelper: call");
            throw new NotImplementedException();
        }
    }
}