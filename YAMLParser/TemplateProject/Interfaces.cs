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
                        if (msg.msgtype() == MsgTypes.Unknown)
                            throw new Exception("OH NOES IRosMessage.generate is borked!");
                        if (!_typeregistry.ContainsKey(msg.msgtype()))
                            _typeregistry.Add(msg.msgtype(), msg.GetType());
                        if (!constructors.ContainsKey(msg.msgtype()))
                            constructors.Add(msg.msgtype(), T => Activator.CreateInstance(_typeregistry[T]) as IRosMessage);
                    }
                }
                if (constructors.ContainsKey(t))
                    return constructors[t].Invoke(t);
                else
                    throw new Exception("OH NOES IRosMessage.generate is borked!");
            }
        }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual string MD5Sum() { return ""; }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual bool HasHeader() { return false; }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual bool IsMetaType() { return false; }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual string MessageDefinition() { return ""; }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public byte[] Serialized;
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual MsgTypes msgtype() { return MsgTypes.Unknown; }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual bool IsServiceComponent() { return false; }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]

        public IDictionary connection_header;

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.DebuggerStepThrough]
        public IRosMessage()
        {
        }


        [System.Diagnostics.DebuggerStepThrough]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public IRosMessage(byte[] SERIALIZEDSTUFF)
        {
            Deserialize(SERIALIZEDSTUFF);
        }

        [System.Diagnostics.DebuggerStepThrough]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public IRosMessage(byte[] SERIALIZEDSTUFF, ref int currentIndex)
        {
            Deserialize(SERIALIZEDSTUFF, ref currentIndex);
        }



#if !TRACE
        [DebuggerStepThrough]
#endif
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public void Deserialize(byte[] SERIALIZEDSTUFF)
        {
            int start = 0;
            Deserialize(SERIALIZEDSTUFF, ref start);
        }

#if !TRACE
        [DebuggerStepThrough]
#endif
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual void Deserialize(byte[] SERIALIZEDSTUFF, ref int currentIndex)
        {
            throw new NotImplementedException();
        }

        [System.Diagnostics.DebuggerStepThrough]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public byte[] Serialize()
        {
            return Serialize(false);
        }

        [System.Diagnostics.DebuggerStepThrough]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual byte[] Serialize(bool partofsomethingelse)
        {
            throw new NotImplementedException();
        }

        public virtual void Randomize()
        {
            throw new NotImplementedException();
        }

        public virtual bool Equals(IRosMessage msg)
        {
            throw new NotImplementedException();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as IRosMessage);
        }

        [System.Diagnostics.DebuggerStepThrough]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public delegate IRosMessage RosServiceDelegate(IRosMessage request);
    [System.Diagnostics.DebuggerStepThrough]
    public class IRosService
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual string MD5Sum() { return ""; }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual string ServiceDefinition() { return ""; }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public virtual SrvTypes srvtype() { return SrvTypes.Unknown; }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public MsgTypes msgtype_req
        {
            get { return RequestMessage.msgtype(); }
        }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public MsgTypes msgtype_res
        {
            get { return ResponseMessage.msgtype(); }
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
        {
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
                        if (srv.srvtype() == SrvTypes.Unknown)
                            throw new Exception("OH NOES IRosService.generate is borked!");
                        if (!_typeregistry.ContainsKey(srv.srvtype()))
                            _typeregistry.Add(srv.srvtype(), srv.GetType());
                        if (!constructors.ContainsKey(srv.srvtype()))
                            constructors.Add(srv.srvtype(), T => Activator.CreateInstance(_typeregistry[T]) as IRosService);
                        srv.RequestMessage = IRosMessage.generate((MsgTypes) Enum.Parse(typeof (MsgTypes), srv.srvtype() + "__Request"));
                        srv.ResponseMessage = IRosMessage.generate((MsgTypes) Enum.Parse(typeof (MsgTypes), srv.srvtype() + "__Response"));
                    }
                }

                if (constructors.ContainsKey(t))
                    return constructors[t].Invoke(t);
                else
                    throw new Exception("OH NOES IRosService.generate is borked!");
            }
        }
    }



    public enum ServiceMessageType
    {
        Not,
        Request,
        Response
    }
    [System.Diagnostics.DebuggerStepThrough]
    public struct TimeData
    {
        public uint sec;
        public uint nsec;

        public TimeData(uint s, uint ns)
        {
            sec = s;
            nsec = ns;
        }
    }
}
