#region USINGZ

using System;
using System.Diagnostics;
using System.Threading;

#endregion

namespace Ros_CSharp
{
    public class SubscriberCallbacks
    {
        public CallbackQueueInterface Callback;
        public SubscriberStatusCallback connect, disconnect;

        public SubscriberCallbacks() : this(null, null, null)
        {
        }

        public SubscriberCallbacks(SubscriberStatusCallback connectCB, SubscriberStatusCallback disconnectCB,
                                   CallbackQueueInterface Callback)
        {
            connect = connectCB;
            disconnect = disconnectCB;
            this.Callback = Callback;
        }

        internal ulong Get()
        {
            return ROS.getPID();
        }
    }
}