using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EricIsAMAZING
{
    public class SubscriberCallbacks
    {
        private static UInt64 _uid;
        public UInt64 Get()
        {
            return __uid;
        }
        private UInt64 __uid;

        public SubscriberStatusCallback connect, disconnect;
        public CallbackQueue callbackQueue;

        public SubscriberCallbacks(SubscriberStatusCallback connectCB, SubscriberStatusCallback disconnectCB, CallbackQueue callbackQueue)
        {
            __uid = _uid++;
            this.connect = connectCB;
            this.disconnect = disconnectCB;
            this.callbackQueue = callbackQueue;
        }
    }
}
