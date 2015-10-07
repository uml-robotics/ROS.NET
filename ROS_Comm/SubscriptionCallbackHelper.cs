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
// Updated: 10/07/2015

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
        public ParameterAdapter<M> Adapter = new ParameterAdapter<M>();

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

        public M deserialize(SubscriptionCallbackHelperDeserializeParams parms)
        {
            //EDB.WriteLine("SubscriptionCallbackHelper: deserialize(specific)");
            return deserialize<M>(parms);
        }

        public override T deserialize<T>(SubscriptionCallbackHelperDeserializeParams parms)
        {
            //EDB.WriteLine("SubscriptionCallbackHelper: deserialize(generic adapter)");
            T t = base.deserialize<T>(parms);
            return t;
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

        public virtual T deserialize<T>(SubscriptionCallbackHelperDeserializeParams parms) where T : IRosMessage
        {
            //EDB.WriteLine("ISubscriptionCallbackHelper: deserialize");
            IRosMessage msg = ROS.MakeMessage(type);
            assignSubscriptionConnectionHeader(ref msg, parms.connection_header);
            T t = (T) msg;
            t.Deserialize(parms.buffer);
            return t;
            //return SerializationHelper.Deserialize<T>(parms.buffer);
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

    public class SubscriptionCallbackHelperDeserializeParams
    {
        public byte[] buffer;
        public IDictionary connection_header;
        public int length;

        public SubscriptionCallbackHelperDeserializeParams()
        {
            throw new NotImplementedException();
        }
    }

    public class SubscriptionCallbackHelperCallParams<M> : ISubscriptionCallbackHelperCallParams where M : IRosMessage, new()
    {
        public SubscriptionCallbackHelperCallParams(MessageEvent<M> e) : base(e)
        {
        }
    }

    public class ISubscriptionCallbackHelperCallParams
    {
        public IMessageEvent Event;

        public ISubscriptionCallbackHelperCallParams(IMessageEvent e)
        {
            Event = e;
        }
    }

    public class ParameterAdapter<P> where P : IRosMessage, new()
    {
        public P getParameter(MessageEvent<P> Event)
        {
            //EDB.WriteLine("getParameter!");
            return (P) Event.message.Deserialize(Event.message.Serialized);
        }
    }
}