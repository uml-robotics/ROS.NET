// File: AdvertiseOptions.cs
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
using System.Collections.Generic;
using Messages;

#endregion

namespace Ros_CSharp
{
    public class AdvertiseOptions<T> where T : IRosMessage, new()
    {
        public CallbackQueueInterface callback_queue;
        public SubscriberStatusCallback connectCB;
        public string datatype = "";
        public SubscriberStatusCallback disconnectCB;
        public bool has_header;
        public bool latch;
        public string md5sum = "";
        public string message_definition = "";
        public int queue_size;
        public string topic = "";

        public AdvertiseOptions()
        {
        }

        public AdvertiseOptions(string t, int q_size, string md5, string dt, string message_def,
            SubscriberStatusCallback connectcallback)
            : this(t, q_size, md5, dt, message_def, connectcallback, null)
        {
        }


        public AdvertiseOptions(string t, int q_size, string md5, string dt, string message_def)
            : this(t, q_size, md5, dt, message_def, null, null)
        {
        }

        public AdvertiseOptions(string t, int q_size, string md5, string dt, string message_def,
            SubscriberStatusCallback connectcallback,
            SubscriberStatusCallback disconnectcallback)
        {
            topic = t;
            queue_size = q_size;
            md5sum = md5;
            T tt = new T();
            if (dt.Length > 0)
                datatype = dt;
            else
            {
                datatype = tt.msgtype().ToString().Replace("__", "/");
            }
            if (message_def.Length == 0)
                message_definition = tt.MessageDefinition();
            else
                message_definition = message_def;
            has_header = tt.HasHeader();
            connectCB = connectcallback;
            disconnectCB = disconnectcallback;
        }

        public AdvertiseOptions(string t, int q_size)
            : this(t, q_size, null, null)
        {
        }

        public AdvertiseOptions(string t, int q_size, SubscriberStatusCallback connectcallback,
            SubscriberStatusCallback disconnectcallback) :
                this(
                t, q_size, new T().MD5Sum(),
                new T().msgtype().ToString().Replace("__", "/"),
                new T().MessageDefinition(),
                connectcallback, disconnectcallback)
        {
        }

        public static AdvertiseOptions<M> Create<M>(string topic, int q_size, SubscriberStatusCallback connectcallback,
            SubscriberStatusCallback disconnectcallback, CallbackQueue queue)
            where M : IRosMessage, new()
        {
            return new AdvertiseOptions<M>(topic, q_size, connectcallback, disconnectcallback) {callback_queue = queue};
        }
    }
}