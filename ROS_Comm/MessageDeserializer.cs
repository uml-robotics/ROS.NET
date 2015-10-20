// File: MessageDeserializer.cs
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

using System.Collections;
using Messages;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace Ros_CSharp
{
    public class MessageDeserializer<M> : IMessageDeserializer where M : IRosMessage, new()
    {
        public MessageDeserializer(SubscriptionCallbackHelper<M> helper, IRosMessage m, IDictionary connection_header)
            : base(helper, m, connection_header)
        {
        }

        public new M message
        {
            get { return (M) base.message; }
        }

        public new SubscriptionCallbackHelper<M> helper
        {
            get { return ((SubscriptionCallbackHelper<M>) base.helper); }
        }

        public override IRosMessage deserialize()
        {
            if (message.Serialized != null)
            {
                IRosMessage t = new M();
                t = t.Deserialize(message.Serialized);
                t.connection_header = message.connection_header;
                base.message = t;
            }
            return message;
        }
    }

    public class IMessageDeserializer
    {
        public IDictionary connection_header;
        public ISubscriptionCallbackHelper helper;
        public IRosMessage message;

        public IMessageDeserializer(ISubscriptionCallbackHelper helper, IRosMessage m, IDictionary connection_header)
        {
            this.helper = helper;
            message = m;
            this.connection_header = connection_header;
        }

        public virtual IRosMessage deserialize()
        {
            return new IRosMessage {msgtype = MsgTypes.Unknown};
        }
    }
}