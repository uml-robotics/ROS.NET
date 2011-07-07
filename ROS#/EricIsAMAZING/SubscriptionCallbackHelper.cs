#region USINGZ

using Messages;
using m = Messages;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace EricIsAMAZING
{
    public class SubscriptionCallbackHelper<M> : ISubscriptionCallbackHelper where M : IRosMessage, new()
    {
        public ParameterAdapter<M> Adapter = new ParameterAdapter<M>();

        public SubscriptionCallbackHelper(m.TypeEnum t, CallbackDelegate<M> cb)
        {
            type = t;
            Callback.addCallback(new Callback<M>(cb));
        }

        public SubscriptionCallbackHelper(TypeEnum t)
        {
            type = t;
        }

        public SubscriptionCallbackHelper(CallbackQueueInterface q)
            : base(q)
        {
        }
    }

    public class ISubscriptionCallbackHelper
    {
        public CallbackQueueInterface Callback;

        public TypeEnum type;

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

        public virtual TypeEnum getTypeInfo()
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
        public string connection_header;
        public int length;
    }

    public class SubscriptionCallbackHelperCallParams
    {
        public IMessageEvent Event;
    }

    public class ParameterAdapter<P> : IParameterAdapter where P : IRosMessage
    {
    }

    public abstract class IParameterAdapter
    {
    }

    public class MessageStuff<T>
    {
    }
}