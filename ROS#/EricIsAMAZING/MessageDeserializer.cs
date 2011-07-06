using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XmlRpc_Wrapper;
using m = Messages;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;
using System.Threading;

namespace EricIsAMAZING
{
    public class MessageDeserializer<M> : IMessageDeserializer where M : m.IRosMessage, new()
    {
        public new M message
        {
            get { return (M)base.message; }
        }

        public SubscriptionCallbackHelper<M> helper
        {
            get { return (SubscriptionCallbackHelper<M>)base.helper; }
        }

        public MessageDeserializer(SubscriptionCallbackHelper<M> helper, M m, string connection_header) : base(helper, m, connection_header)
        {

        }
    }

    public class IMessageDeserializer
    {
        public string connection_header;
        public m.IRosMessage message;
        public ISubscriptionCallbackHelper helper;
        public IMessageDeserializer(ISubscriptionCallbackHelper helper, m.IRosMessage m, string connection_header)
        {
            this.helper = helper;
            message = m;
            this.connection_header = connection_header;
        }
    }
}
