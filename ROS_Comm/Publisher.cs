// File: Publisher.cs
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
using System.Diagnostics;
using Messages;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace Ros_CSharp
{
    public class Publisher<M> : IPublisher where M : IRosMessage, new()
    {
        private Publication p;

        /// <summary>
        ///     Creates a ros publisher
        /// </summary>
        /// <param name="topic">Topic name to publish to</param>
        /// <param name="md5sum">md5sum for topic and type</param>
        /// <param name="datatype">Datatype to publish</param>
        /// <param name="nodeHandle">nodehandle</param>
        /// <param name="callbacks">Any callbacks to attach</param>
        public Publisher(string topic, string md5sum, string datatype, NodeHandle nodeHandle,
            SubscriberCallbacks callbacks)
        {
            // TODO: Complete member initialization
            this.topic = topic;
            this.md5sum = md5sum;
            this.datatype = datatype;
            this.nodeHandle = nodeHandle;
            this.callbacks = callbacks;
        }

        public void publish(M msg)
        {
            if (p == null)
                p = TopicManager.Instance.lookupPublication(topic);
            if (p != null)
            {
                msg.Serialized = null;
                TopicManager.Instance.publish(p, msg);
            }
        }
    }

    public class IPublisher
    {
        public SubscriberCallbacks callbacks;

        public double constructed =
            (int) Math.Floor(DateTime.Now.Subtract(Process.GetCurrentProcess().StartTime).TotalMilliseconds);

        public string datatype;
        public string md5sum;
        public NodeHandle nodeHandle;
        public string topic;
        public bool unadvertised;

        public bool IsValid
        {
            get { return !unadvertised; }
        }

        internal void unadvertise()
        {
            if (!unadvertised)
            {
                unadvertised = true;
                TopicManager.Instance.unadvertise(topic, callbacks);
            }
        }

        public void shutdown()
        {
            unadvertise();
        }
    }
}