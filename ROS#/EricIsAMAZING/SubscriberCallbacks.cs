#region USINGZ

using System;

#endregion

namespace EricIsAMAZING
{
    public class SubscriberCallbacks
    {
        private static UInt64 _uid;
        public CallbackQueueInterface Callback;
        private UInt64 __uid;
        public SubscriberStatusCallback connect, disconnect;

        public SubscriberCallbacks() : this(null, null, null)
        {
        }

        public SubscriberCallbacks(SubscriberStatusCallback connectCB, SubscriberStatusCallback disconnectCB, CallbackQueueInterface Callback)
        {
            __uid = _uid++;
            connect = connectCB;
            disconnect = disconnectCB;
            this.Callback = Callback;
        }

        public UInt64 Get()
        {
            return __uid;
        }
    }
}