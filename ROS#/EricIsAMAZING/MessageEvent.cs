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
    public class MessageEvent<M> : IMessageEvent where M : m.IRosMessage, new()
    {
        public delegate M SpecCreateFunction();
        public SpecCreateFunction speccreate;
        public static CreateFunction convert(SpecCreateFunction spec)
        {
            return () => (m.IRosMessage)spec.Invoke();
        }
        public static SpecCreateFunction convert(CreateFunction spec)
        {
            return () => (M)spec.Invoke();
        }
        public MessageEvent() : base()
        {
        }

        public MessageEvent(MessageEvent<M> rhs)
            : base(rhs)
        {
        }

        public MessageEvent(MessageEvent<M> rhs, bool needcopy) : base(rhs,needcopy)
        {
        }

        public MessageEvent(MessageEvent<M> rhs, SpecCreateFunction c)
            : base(rhs, convert(c))
        {
            speccreate = c;
        }

        public MessageEvent(MessageEvent<M> rhs, CreateFunction c)
            : base(rhs, c)
        {
            speccreate = convert(c);
        }
        public MessageEvent(M msg) : base(msg)
        {
        }

        public MessageEvent(M msg, DateTime rec)
            : base(msg, rec)
        {
        }

        public MessageEvent(M msg, string head, DateTime rec)
            : base(msg, head, rec)
        {
        }

        public MessageEvent(M msg, string head, DateTime rec, bool needcopy, SpecCreateFunction c) : base(msg, head, rec, needcopy, convert(c))
        {
            speccreate = c;
        }
        public MessageEvent(M msg, string head, DateTime rec, bool needcopy, CreateFunction c)
            : base(msg, head, rec, needcopy, c)
        {
            speccreate = convert(c);
        }
        public override void init(m.IRosMessage msg, string connhead, DateTime rec, bool needcopy, CreateFunction c)
        {
            base.init(msg, connhead, rec, needcopy, c);
            speccreate = convert(c);
        }
        public new M getMessage()
        {
            return (M)base.getMessage();
        }

    }

    public class IMessageEvent
    {
        public m.IRosMessage getMessage()
        {
            if (nonconst_need_copy)
                return new m.IRosMessage(message.Serialize());
            return message;
        }

        public static CreateFunction DefaultCreator = () => new m.IRosMessage();
        public m.IRosMessage message;
        public string connection_header;
        public DateTime receipt_time;
        public bool nonconst_need_copy;
        public CreateFunction create;
        public IMessageEvent()
        {
            nonconst_need_copy = false;
        }
        public IMessageEvent(IMessageEvent rhs)
        {
            message = rhs.message;
            connection_header = rhs.connection_header;
            receipt_time = rhs.receipt_time;
            nonconst_need_copy = rhs.nonconst_need_copy;
            create = rhs.create;
        }
        public IMessageEvent(IMessageEvent rhs, bool needcopy)
            : this(rhs)
        {
            nonconst_need_copy = needcopy;
        }
        public IMessageEvent(IMessageEvent rhs, CreateFunction c)
            : this(rhs.message, rhs.connection_header, rhs.receipt_time, rhs.nonconst_need_copy, c)
        {
        }
        public IMessageEvent(m.IRosMessage msg)
            : this(msg, msg.connection_header, DateTime.Now, true, DefaultCreator)
        {
        }
        public IMessageEvent(m.IRosMessage msg, DateTime rec)
            : this(msg, msg.connection_header, rec, true, DefaultCreator)
        {
        }
        public IMessageEvent(m.IRosMessage msg, string head, DateTime rec)
            : this(msg, head, rec, true, DefaultCreator)
        {
        }
        public IMessageEvent(m.IRosMessage msg, string conhead, DateTime rectime, bool needcopy, CreateFunction c)
        {
            init(msg, conhead, rectime, needcopy, c);
        }

        public virtual void init(m.IRosMessage msg, string connhead, DateTime rec, bool needcopy, CreateFunction c)
        {
            message = msg;
            connection_header = connhead;
            receipt_time = rec;
            nonconst_need_copy = needcopy;
            create = c;
        }
    }

    public delegate m.IRosMessage CreateFunction();
}
