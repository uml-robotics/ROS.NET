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
        public CallbackQueueInterface Callback;

        public SubscriberCallbacks(SubscriberStatusCallback connectCB, SubscriberStatusCallback disconnectCB, CallbackQueueInterface Callback)
        {
            __uid = _uid++;
            this.connect = connectCB;
            this.disconnect = disconnectCB;
            this.Callback = Callback;
        }
    }
}
