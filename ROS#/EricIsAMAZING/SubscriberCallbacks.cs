#region USINGZ

using System;

#endregion

namespace EricIsAMAZING
{
    public class SubscriberCallbacks
    {
        public CallbackQueueInterface Callback;

        private static UInt64 _uid;
        private UInt64 __uid;
        public SubscriberStatusCallback connect, disconnect;

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