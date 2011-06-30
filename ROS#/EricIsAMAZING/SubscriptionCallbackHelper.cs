using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EricIsAMAZING
{
    public class SubscriptionCallbackHelper<M> : ISubscriptionCallbackHelper where M : Messages.IRosMessage
    {
        public ParameterAdapter<M> Adapter = new ParameterAdapter<M>();
        
        public SubscriptionCallbackHelper()
        {
            
        }

        public SubscriptionCallbackHelper(CallbackQueue q)
            : base(q)
        {
        }
    }

    public abstract class ISubscriptionCallbackHelper
    {
        private CallbackQueue callbackQueue;

        public ISubscriptionCallbackHelper()
        {
        }

        public ISubscriptionCallbackHelper(CallbackQueue callbackQueue)
        {
            this.callbackQueue = callbackQueue;
        }
    }

    public class ParameterAdapter<P> : IParameterAdapter where P : Messages.IRosMessage
    {
        
    }

    public abstract class IParameterAdapter
    {

    }

    public class MessageStuff<T>
    {

    }
}
