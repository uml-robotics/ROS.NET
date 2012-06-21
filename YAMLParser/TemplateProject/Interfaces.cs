using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using String = Messages.std_msgs.String;

namespace Messages
{
    public interface IIRosMessage
    {
        void Deserialize(byte[] SERIALIZEDSTUFF);
        byte[] Serialize(bool partofsomethingelse = false);

    }

    public class IRosMessage : IIRosMessage
    {
        public static Dictionary<MsgTypes, Func<MsgTypes, IRosMessage>> constructors = new Dictionary<MsgTypes, Func<MsgTypes, IRosMessage>>();
        private static Dictionary<MsgTypes, Type> _typeregistry = new Dictionary<MsgTypes, Type>();
        public static IRosMessage generate(MsgTypes t)
        {
            if (constructors.ContainsKey(t))
                return constructors[t].Invoke(t);
            Type thistype = typeof(IRosMessage);
            foreach (Type othertype in thistype.Assembly.GetTypes())
            {
                if (thistype == othertype || !othertype.IsSubclassOf(thistype)) continue;
                IRosMessage msg = Activator.CreateInstance(othertype) as IRosMessage;
                if (msg != null)
                {
                    if (msg.msgtype == MsgTypes.Unknown)
                        throw new Exception("OH NOES IRosMessage.generate is borked!");
                    if (!_typeregistry.ContainsKey(msg.msgtype))
                        _typeregistry.Add(msg.msgtype, msg.GetType());
                    if (!constructors.ContainsKey(msg.msgtype))
                        constructors.Add(msg.msgtype, T => Activator.CreateInstance(_typeregistry[T]) as IRosMessage);
                }
            }
            if (constructors.ContainsKey(t))
                return constructors[t].Invoke(t);
            else
                throw new Exception("OH NOES IRosMessage.generate is borked!");
        }

        public bool HasHeader;
        public bool IsMetaType;
        public string MessageDefinition;
        public byte[] Serialized;
        public IDictionary connection_header;
        public MsgTypes msgtype;
        public Dictionary<string, MsgFieldInfo> Fields;

        public IRosMessage()
            : this(MsgTypes.Unknown, "", false, false, null)
        {
        }

        [DebuggerStepThrough]
        public IRosMessage(MsgTypes t, string def, bool hasheader, bool meta, Dictionary<string, MsgFieldInfo> fields)
        {
            msgtype = t;
            MessageDefinition = def;
            HasHeader = hasheader;
            IsMetaType = meta;
            Fields = fields;
        }

        public IRosMessage(byte[] SERIALIZEDSTUFF)
        {
            Deserialize(SERIALIZEDSTUFF);
        }

        public virtual void Deserialize(byte[] SERIALIZEDSTUFF)
        {
            int dontcare = 0;
            SerializationHelper.deserialize(GetType(), SERIALIZEDSTUFF, out dontcare, !IsMetaType && msgtype != MsgTypes.std_msgs__String);
        }

        public virtual byte[] Serialize(bool partofsomethingelse = false)
        {
            throw new NotImplementedException();
        }

        public static IRosMessage DeserializeIt(byte[] SERIALIZEDSTUFF)
        {
            return new IRosMessage(SERIALIZEDSTUFF);
        }
    }
}
