#region USINGZ

using System;
using Messages;
using m = Messages;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace EricIsAMAZING
{
    public class MessageEvent<M> : IMessageEvent where M : IRosMessage, new()
    {
        #region Delegates

        public delegate M SpecCreateFunction();

        #endregion

        public SpecCreateFunction speccreate;

        public MessageEvent()
        {
        }

        public MessageEvent(MessageEvent<M> rhs)
            : base(rhs)
        {
        }

        public MessageEvent(MessageEvent<M> rhs, bool needcopy) : base(rhs, needcopy)
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

        public static CreateFunction convert(SpecCreateFunction spec)
        {
            return () => (IRosMessage) spec.Invoke();
        }

        public static SpecCreateFunction convert(CreateFunction spec)
        {
            return () => (M) spec.Invoke();
        }

        public override void init(IRosMessage msg, string connhead, DateTime rec, bool needcopy, CreateFunction c)
        {
            base.init(msg, connhead, rec, needcopy, c);
            speccreate = convert(c);
        }

        public new M getMessage()
        {
            return (M) base.getMessage();
        }
    }

    public class IMessageEvent
    {
        public static CreateFunction DefaultCreator = () => new IRosMessage();
        public string connection_header;
        public CreateFunction create;
        public IRosMessage message;
        public bool nonconst_need_copy;
        public DateTime receipt_time;

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

        public IMessageEvent(IRosMessage msg)
            : this(msg, msg.connection_header, DateTime.Now, true, DefaultCreator)
        {
        }

        public IMessageEvent(IRosMessage msg, DateTime rec)
            : this(msg, msg.connection_header, rec, true, DefaultCreator)
        {
        }

        public IMessageEvent(IRosMessage msg, string head, DateTime rec)
            : this(msg, head, rec, true, DefaultCreator)
        {
        }

        public IMessageEvent(IRosMessage msg, string conhead, DateTime rectime, bool needcopy, CreateFunction c)
        {
            init(msg, conhead, rectime, needcopy, c);
        }

        public IRosMessage getMessage()
        {
            if (nonconst_need_copy)
                return new IRosMessage(message.Serialize());
            return message;
        }

        public virtual void init(IRosMessage msg, string connhead, DateTime rec, bool needcopy, CreateFunction c)
        {
            message = msg;
            connection_header = connhead;
            receipt_time = rec;
            nonconst_need_copy = needcopy;
            create = c;
        }
    }

    public delegate IRosMessage CreateFunction();
}