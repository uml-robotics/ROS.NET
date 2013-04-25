#region USINGZ

using System;
using System.Collections;
using Messages;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace Ros_CSharp
{
    public class MessageEvent<M> : IMessageEvent where M : IRosMessage, new()
    {
        #region Delegates

        public delegate T SpecCreateFunction<T>();

        #endregion

        public SpecCreateFunction<M> speccreate;

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

        public MessageEvent(MessageEvent<M> rhs, SpecCreateFunction<M> c)
            : base(rhs, ()=>c())
        {
            speccreate = c;
        }

        public MessageEvent(MessageEvent<M> rhs, CreateFunction c)
            : base(rhs, c)
        {
            speccreate = ()=>(M)c();
        }

        public MessageEvent(M msg) : base(msg)
        {
        }

        public MessageEvent(M msg, TimeData rec)
            : base(msg, rec)
        {
        }

        public MessageEvent(M msg, IDictionary head, TimeData rec)
            : base(msg, head, rec)
        {
        }

        public MessageEvent(M msg, IDictionary head, TimeData rec, bool needcopy, SpecCreateFunction<M> c)
            : base(msg, head, rec, needcopy, ()=>(IRosMessage)c())
        {
            speccreate = c;
        }

        public MessageEvent(IRosMessage iRosMessage, IDictionary iDictionary, TimeData timeData, bool p, SpecCreateFunction<M> s) : this((M)iRosMessage, iDictionary, timeData, p, s)
        {
        }

        public void init(M msg, IDictionary connhead, TimeData rec, bool needcopy, SpecCreateFunction<M> c)
        {
            speccreate = c;
            _init(msg, connhead, rec, needcopy, () => (IRosMessage)c());
        }

        public override IRosMessage getMessage()
        {
            return (M) base.getMessage();
        }
    }

    public class IMessageEvent
    {
        public static CreateFunction DefaultCreator = () => new IRosMessage();
        public IDictionary connection_header;
        public CreateFunction create;
        public IRosMessage message;
        public bool nonconst_need_copy;
        public TimeData receipt_time;

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
            : this(msg, msg.connection_header, ROS.GetTime().data, true, DefaultCreator)
        {
        }

        public IMessageEvent(IRosMessage msg, TimeData rec)
            : this(msg, msg.connection_header, rec, true, DefaultCreator)
        {
        }

        public IMessageEvent(IRosMessage msg, IDictionary head, TimeData rec)
            : this(msg, head, rec, true, DefaultCreator)
        {
        }

        public IMessageEvent(IRosMessage msg, IDictionary conhead, TimeData rectime, bool needcopy, CreateFunction c)
        {
            _init(msg, conhead, rectime, needcopy, c);
        }

        public virtual IRosMessage getMessage()
        {
            if (nonconst_need_copy)
                return new IRosMessage(message.Serialize());
            return message;
        }

        public virtual void _init(IRosMessage msg, IDictionary connhead, TimeData rec, bool needcopy, CreateFunction c)
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