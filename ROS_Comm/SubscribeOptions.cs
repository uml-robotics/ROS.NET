// File: SubscribeOptions.cs
// Project: ROS_C-Sharp
// 
// ROS#
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 03/04/2013
// Updated: 07/26/2013

#region Using

using System;
using Messages;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace Ros_CSharp
{
    public class SubscribeOptions<T> where T : IRosMessage, new()
    {
        public bool allow_concurrent_callbacks = true;
        public CallbackQueueInterface callback_queue;
        public string datatype = "";
        public bool has_header;
        public SubscriptionCallbackHelper<T> helper;
        public bool latch;
        public string md5sum = "";
        public string message_definition = "";
        public uint queue_size;
        public string topic = "";

        public SubscribeOptions() : this("", 1, null)
        {
            //allow_concurrent_callbacks = false;
            //allow_concurrent_callbacks = true;
        }

        public SubscribeOptions(string topic, uint queue_size)
            : this(topic, queue_size, null, null)
        {
        }

        public SubscribeOptions(string topic, uint queue_size, CallbackDelegate<T> CALL)
            : this(topic, queue_size, CALL, null)
        {
        }


        public SubscribeOptions(string topic, uint queue_size, CallbackDelegate<T> CALL, string thisisveryverybad)
        {
            // TODO: Complete member initialization
            this.topic = topic;
            this.queue_size = queue_size;
            if (CALL != null)
                helper = new SubscriptionCallbackHelper<T>(new T().msgtype, CALL);
            else
                helper = new SubscriptionCallbackHelper<T>(new T().msgtype);


            Type msgtype = new T().GetType();
            string[] chunks = msgtype.FullName.Split('.');
            datatype = chunks[chunks.Length - 2] + "/" + chunks[chunks.Length - 1];
            md5sum = thisisveryverybad ?? new T().MD5Sum;
        }

        public SubscribeOptions(string topic, uint queue_size, CallbackDelegate<T> CALL, string thisisveryverybad, Type JPAddedType)
        {
            // TODO: Complete member initialization
            this.topic = topic;
            this.queue_size = queue_size;
            if (CALL != null)
                helper = new SubscriptionCallbackHelper<T>(new T().msgtype, CALL);
            else
                helper = new SubscriptionCallbackHelper<T>(new T().msgtype);


            Type msgtype = JPAddedType;
            string[] chunks = msgtype.FullName.Split('.');
            datatype = chunks[chunks.Length - 2] + "/" + chunks[chunks.Length - 1];
            md5sum = thisisveryverybad ?? new T().MD5Sum;
        }
    }


    public delegate void CallbackDelegate<in T>(T argument) where T : IRosMessage, new();
}