// File: Subscriber.cs
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

#endregion

namespace Ros_CSharp
{
    public class Subscriber<M> : ISubscriber
    {
        /// <summary>
        ///     Creates a ROS Subscriber
        /// </summary>
        /// <param name="topic">Topic name to subscribe to</param>
        /// <param name="nodeHandle">nodehandle</param>
        /// <param name="cb">callback function to be fired when message is received</param>
        public Subscriber(string topic, NodeHandle nodeHandle, ISubscriptionCallbackHelper cb) : base(topic)
        {
            // TODO: Complete member initialization
            this.topic = topic;
            nodehandle = new NodeHandle(nodeHandle);
            helper = cb;
        }

        /// <summary>
        ///     Deep Copy of a subscriber
        /// </summary>
        /// <param name="s">Subscriber to copy</param>
        public Subscriber(Subscriber<M> s) : base(s.topic)
        {
            topic = s.topic;
            nodehandle = new NodeHandle(s.nodehandle);
            helper = s.helper;
        }

        /// <summary>
        ///     Creates a ROS subscriber
        /// </summary>
        public Subscriber() : base(null)
        {
        }

        /// <summary>
        ///     Returns the number of publishers on the subscribers topic
        /// </summary>
        public int NumPublishers
        {
            get
            {
                if (IsValid)
                    return subscription.NumPublishers;
                return 0;
            }
        }

        /// <summary>
        ///     Shutdown a subscriber gracefully.
        /// </summary>
        public override void shutdown()
        {
            unsubscribe();
        }
    }

    public class ISubscriber
    {
        protected ISubscriber(string topic)
        {
            if (topic !=null)
            {
                this.topic = topic;
                subscription = TopicManager.Instance.getSubscription(topic);
            }
        }

        public double constructed = DateTime.Now.Subtract(Process.GetCurrentProcess().StartTime).Ticks;
        public ISubscriptionCallbackHelper helper;
        public NodeHandle nodehandle;
        protected Subscription subscription;
        public string topic = "";
        public bool unsubscribed;

        public bool IsValid
        {
            get { return !unsubscribed; }
        }

        public virtual void unsubscribe()
        {
            if (!unsubscribed)
            {
                unsubscribed = true;
                TopicManager.Instance.unsubscribe(topic, helper);
            }
        }

        public virtual void shutdown()
        {
            throw new NotImplementedException();
        }
    }
}