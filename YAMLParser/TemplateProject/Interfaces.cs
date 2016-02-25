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
using uint8 = System.Byte;

namespace Messages
{
    public class IRosMessage
    {
        internal static Dictionary<MsgTypes, Func<MsgTypes, IRosMessage>> constructors = new Dictionary<MsgTypes, Func<MsgTypes, IRosMessage>>();
        private static Dictionary<MsgTypes, Type> _typeregistry = new Dictionary<MsgTypes, Type>();

#if !TRACE
    [DebuggerStepThrough]
#endif
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static IRosMessage generate(MsgTypes t)
        {
            lock (constructors)
            {
                if (constructors.ContainsKey(t))
                    return constructors[t].Invoke(t);
                Type thistype = typeof (IRosMessage);
                foreach (Type othertype in thistype.Assembly.GetTypes())
                {
                    if (thistype == othertype || !othertype.IsSubclassOf(thistype))
                    {
                        continue;
                    }
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
        }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public string MD5Sum;
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool HasHeader;
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool IsMetaType;
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public string MessageDefinition;
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public byte[] Serialized;
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public IDictionary connection_header;
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public MsgTypes msgtype;
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool IsServiceComponent;
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public Dictionary<string, MsgFieldInfo> Fields;

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.DebuggerStepThrough]
        public IRosMessage()
            : this(MsgTypes.Unknown, "", false, false, null)
        {
        }

#if !TRACE
    [DebuggerStepThrough]
#endif
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public IRosMessage(MsgTypes t, string def, bool hasheader, bool meta, Dictionary<string, MsgFieldInfo> fields)
        :this(t,def,hasheader,meta,fields,"")
        {}

#if !TRACE
    [DebuggerStepThrough]
#endif
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public IRosMessage(MsgTypes t, string def, bool hasheader, bool meta, Dictionary<string, MsgFieldInfo> fields, string ms5) : this(t,def,hasheader,meta,fields,ms5,false)
        {
        }

#if !TRACE
    [DebuggerStepThrough]
#endif
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public IRosMessage(MsgTypes t, string def, bool hasheader, bool meta, Dictionary<string, MsgFieldInfo> fields, string ms5, bool isservicemessage)
        {
            msgtype = t;
            MessageDefinition = def;
            HasHeader = hasheader;
            IsMetaType = meta;
            Fields = fields;
            MD5Sum = ms5;
            IsServiceComponent = isservicemessage;
        }
        [System.Diagnostics.DebuggerStepThrough]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public IRosMessage(byte[] SERIALIZEDSTUFF)
        {
            int dontcare = 0;
            SerializationHelper.deserialize(this, GetType(), null, SERIALIZEDSTUFF, out dontcare, !IsMetaType && msgtype != MsgTypes.std_msgs__String);
        }

#if !TRACE
    [DebuggerStepThrough]
#endif
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual void Deserialize(byte[] SERIALIZEDSTUFF)
        {
            throw new NotImplementedException();
        }
        [System.Diagnostics.DebuggerStepThrough]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual byte[] Serialize()
        {
            throw new NotImplementedException();
        }
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public delegate IRosMessage RosServiceDelegate(IRosMessage request);
    [System.Diagnostics.DebuggerStepThrough]
    public class IRosService
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public string MD5Sum;
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public string ServiceDefinition;
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public SrvTypes srvtype = SrvTypes.Unknown;
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public MsgTypes msgtype_req
        {
            get { return RequestMessage.msgtype; }
        }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public MsgTypes msgtype_res
        {
            get { return ResponseMessage.msgtype; }
        }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public IRosMessage RequestMessage, ResponseMessage;
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        protected IRosMessage GeneralInvoke(RosServiceDelegate invocation, IRosMessage m)
        {
            return invocation.Invoke(m);
        }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public IRosService()
            : this(SrvTypes.Unknown, "", "")
        {
        }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public IRosService(SrvTypes t, string def, string md5)
        {
            srvtype = t;
            ServiceDefinition = def;
            MD5Sum = md5;
        }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        protected void InitSubtypes(IRosMessage request, IRosMessage response)
        {
            RequestMessage = request;
            ResponseMessage = response;
        }

        internal static Dictionary<SrvTypes, Func<SrvTypes, IRosService>> constructors = new Dictionary<SrvTypes, Func<SrvTypes, IRosService>>();
        private static Dictionary<SrvTypes, Type> _typeregistry = new Dictionary<SrvTypes, Type>();

#if !TRACE
    [DebuggerStepThrough]
#endif
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static IRosService generate(SrvTypes t)
        {
            lock (constructors)
            {
                if (constructors.ContainsKey(t))
                    return constructors[t].Invoke(t);
                Type thistype = typeof (IRosService);
                foreach (Type othertype in thistype.Assembly.GetTypes())
                {
                    if (thistype == othertype || !othertype.IsSubclassOf(thistype))
                    {
                        continue;
                    }
                    IRosService srv = Activator.CreateInstance(othertype) as IRosService;
                    if (srv != null)
                    {
                        if (srv.srvtype == SrvTypes.Unknown)
                            throw new Exception("OH NOES IRosService.generate is borked!");
                        if (!_typeregistry.ContainsKey(srv.srvtype))
                            _typeregistry.Add(srv.srvtype, srv.GetType());
                        if (!constructors.ContainsKey(srv.srvtype))
                            constructors.Add(srv.srvtype, T => Activator.CreateInstance(_typeregistry[T]) as IRosService);
                        srv.RequestMessage = IRosMessage.generate((MsgTypes) Enum.Parse(typeof (MsgTypes), srv.srvtype + "__Request"));
                        srv.ResponseMessage = IRosMessage.generate((MsgTypes) Enum.Parse(typeof (MsgTypes), srv.srvtype + "__Response"));
                    }
                }

                if (constructors.ContainsKey(t))
                    return constructors[t].Invoke(t);
                else
                    throw new Exception("OH NOES IRosService.generate is borked!");
            }
        }
    }
}
