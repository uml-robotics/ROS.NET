using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XmlRpc_Wrapper;
using m = Messages;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

namespace EricIsAMAZING
{
    public class SubscriptionCallbackHelper<M> : ISubscriptionCallbackHelper where M : m.IRosMessage, new()
    {
        public ParameterAdapter<M> Adapter = new ParameterAdapter<M>();
        
        public SubscriptionCallbackHelper(M msg)
        {
            type = msg.type;
        }

        public SubscriptionCallbackHelper(CallbackQueueInterface q)
            : base(q)
        {
        }
    }

    public class ISubscriptionCallbackHelper
    {
        public m.TypeEnum type = m.TypeEnum.Unknown;

        private CallbackQueueInterface Callback;

        protected ISubscriptionCallbackHelper()
        {
        }

        protected ISubscriptionCallbackHelper(CallbackQueueInterface Callback)
        {
            this.Callback = Callback;
        }

        public virtual byte[] deserialize(SubscriptionCallbackHelperDeserializeParams parms)
        {
            return null;
        }

        public virtual void call(SubscriptionCallbackHelperCallParams parms)
        {
        }

        public virtual m.TypeEnum getTypeInfo()
        {
            return type;
        }

        public virtual bool isConst()
        {
            return true;
        }
    }

    public class SubscriptionCallbackHelperDeserializeParams
    {
        public byte[] buffer;
        public int length;
        public string connection_header;
    }

    public class SubscriptionCallbackHelperCallParams
    {
        public IMessageEvent Event;
    }

    public class ParameterAdapter<P> : IParameterAdapter where P : m.IRosMessage
    {
        
    }

    public abstract class IParameterAdapter
    {

    }

    public class MessageStuff<T>
    {

    }
}
