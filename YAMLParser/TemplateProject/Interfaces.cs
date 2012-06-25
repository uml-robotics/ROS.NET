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
    public class IRosMessage
    {
        internal static Dictionary<MsgTypes, Func<MsgTypes, IRosMessage>> constructors = new Dictionary<MsgTypes, Func<MsgTypes, IRosMessage>>();
        private static Dictionary<MsgTypes, Type> _typeregistry = new Dictionary<MsgTypes, Type>();
        [DebuggerStepThrough]
        internal static IRosMessage generate(MsgTypes t)
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

        internal bool HasHeader;
        internal bool IsMetaType;
        internal string MessageDefinition;
        internal byte[] Serialized;
        internal IDictionary connection_header;
        internal MsgTypes msgtype;
        internal Dictionary<string, MsgFieldInfo> Fields;

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
            int dontcare = 0;
            SerializationHelper.deserialize(GetType(), null, SERIALIZEDSTUFF, out dontcare, !IsMetaType && msgtype != MsgTypes.std_msgs__String);
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
