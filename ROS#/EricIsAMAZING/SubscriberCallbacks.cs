using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EricIsAMAZING
{
    public class SubscriberCallbacks
    {
        private MulticastDelegate connectCB;
        private MulticastDelegate disconnectCB;
        private CallbackQueue callbackQueue;

        public SubscriberCallbacks(MulticastDelegate connectCB, MulticastDelegate disconnectCB, CallbackQueue callbackQueue)
        {
            // TODO: Complete member initialization
            this.connectCB = connectCB;
            this.disconnectCB = disconnectCB;
            this.callbackQueue = callbackQueue;
        }
    }
}
