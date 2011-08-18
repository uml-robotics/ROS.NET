#region USINGZ

using System;
using System.Diagnostics;
using System.Threading;

#endregion

namespace EricIsAMAZING
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

        public UInt64 Get()
        {
            return (UInt64) Process.GetCurrentProcess().Threads[Thread.CurrentThread.ManagedThreadId].Id;
        }
    }
}