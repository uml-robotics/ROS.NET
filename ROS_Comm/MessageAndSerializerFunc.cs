using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Messages;

namespace Ros_CSharp
{
    internal class MessageAndSerializerFunc
    {
        internal bool serialize;
        internal bool nocopy;
        internal TopicManager.SerializeFunc serfunc;
        internal IRosMessage msg;

        internal MessageAndSerializerFunc(IRosMessage msg, TopicManager.SerializeFunc serfunc, bool ser, bool nc)
        {
            this.msg = msg;
            this.serfunc = serfunc;
            serialize = ser;
            nocopy = nc;
        }
    }
}
