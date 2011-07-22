#region USINGZ

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

#endregion

namespace Messages
{
    public static class SerializationHelper
    {

        public static TypedMessage<T> Deserialize<T>(byte[] bytes) where T : struct
        {
            T thestructure = default(T);
            IntPtr pIP = Marshal.AllocHGlobal(Marshal.SizeOf(thestructure));
            Marshal.Copy(bytes, 0, pIP, Marshal.SizeOf(thestructure));
            thestructure = (T) Marshal.PtrToStructure(pIP, typeof (T));
            Marshal.FreeHGlobal(pIP);
            return new TypedMessage<T>(thestructure);
        }


        public static byte[] Serialize<T>(TypedMessage<T> outgoing) where T : struct
        {
            if (outgoing.Serialized != null)
                return outgoing.Serialized;
            outgoing.Serialized = SlapChop(outgoing.data);
            return outgoing.Serialized;
        }

        public static byte[] SlapChop<T>(T t) where T : struct
        {
            FieldInfo[] infos = t.GetType().GetFields();
            Queue<byte[]> chunks = new Queue<byte[]>();
            int totallength = 0;
            foreach (FieldInfo i in infos)
            {
                if (infos.ToString().Contains("(")) continue;
                byte[] thischunk = null;
                if (i.FieldType.Namespace.Contains("Message"))
                {
                    object val = i.GetValue(t);
                    IRosMessage msg = (IRosMessage)Activator.CreateInstance(typeof(TypedMessage<>).MakeGenericType(), val);
                    thischunk = msg.Serialize();
                }
                else if (i.FieldType == typeof(string))
                {
                    byte[] nolen = Encoding.ASCII.GetBytes((string)i.GetValue(t));
                    thischunk = new byte[nolen.Length+4];
                    byte[] bylen = BitConverter.GetBytes(nolen.Length);
                    Array.Copy(nolen, 0, thischunk, 4, nolen.Length);
                    Array.Copy(bylen, thischunk, 4);
                }
                else
                {
                    byte[] temp = new byte[Marshal.SizeOf(t)];
                    GCHandle h = GCHandle.Alloc(temp, GCHandleType.Pinned);
                    Marshal.StructureToPtr(temp, h.AddrOfPinnedObject(), false);
                    h.Free();
                    thischunk = new byte[temp.Length + 4];
                    byte[] bylen = BitConverter.GetBytes(temp.Length);
                    Array.Copy(bylen, 0, thischunk, 0, 4);
                    Array.Copy(temp, 0, thischunk, 4, temp.Length);
                }
                totallength += thischunk.Length;
                chunks.Enqueue(thischunk);
            }
#if! FALSE
            byte[] wholeshebang = new byte[totallength];
            int currpos = 0;
#else
            byte[] wholeshebang = new byte[totallength+4]; //THE WHOLE SHEBANG
            byte[] len = BitConverter.GetBytes(totallength);
            Array.Copy(len, 0, wholeshebang, 0, 4);
            int currpos = 4;
#endif
            while (chunks.Count > 0)
            {
                byte[] chunk = chunks.Dequeue();
                Array.Copy(chunk, 0, wholeshebang, currpos, chunk.Length);
                currpos += chunk.Length;
            }
            return wholeshebang;
        }
    }

    public class TypedMessage<M> : IRosMessage where M : struct
    {
        public new M data;

        public TypedMessage()
            : base((MsgTypes) Enum.Parse(typeof (MsgTypes), typeof (M).FullName.Replace("Messages.", "").Replace(".", "__")),
                   TypeHelper.MessageDefinitions[(MsgTypes) Enum.Parse(typeof (MsgTypes), typeof (M).FullName.Replace("Messages.", "").Replace(".", "__"))],
                   TypeHelper.IsMetaType[(MsgTypes) Enum.Parse(typeof (MsgTypes), typeof (M).FullName.Replace("Messages.", "").Replace(".", "__"))])
        {
        }

        public TypedMessage(M d)
        {
            data = d;
            base.type = (MsgTypes) Enum.Parse(typeof (MsgTypes), typeof (M).FullName.Replace("Messages.", "").Replace(".", "__"));
            base.MessageDefinition = TypeHelper.MessageDefinitions[(MsgTypes) Enum.Parse(typeof (MsgTypes), typeof (M).FullName.Replace("Messages.", "").Replace(".", "__"))];
            base.IsMeta = TypeHelper.IsMetaType[(MsgTypes) Enum.Parse(typeof (MsgTypes), typeof (M).FullName.Replace("Messages.", "").Replace(".", "__"))];
        }

        public TypedMessage(byte[] SERIALIZEDSTUFF)
        {
            Deserialize(SERIALIZEDSTUFF);
        }

        public override void Deserialize(byte[] SERIALIZEDSTUFF)
        {
            data = SerializationHelper.Deserialize<M>(SERIALIZEDSTUFF).data;
        }

        public override byte[] Serialize()
        {
            return SerializationHelper.Serialize(this);
        }
    }

    public class IRosMessage
    {
        public bool HasHeader;
        public bool IsMeta;
        public bool KnownSize = true;

        public string MessageDefinition;

        public byte[] Serialized;
        public IDictionary connection_header;
        public MsgTypes type;

        public IRosMessage() : this(MsgTypes.Unknown, "", false)
        {
        }

        public IRosMessage(MsgTypes t, string def, bool meta)
        {
            type = t;
            MessageDefinition = def;
            IsMeta = meta;
        }

        public IRosMessage(byte[] SERIALIZEDSTUFF)
        {
            Deserialize(SERIALIZEDSTUFF);
        }

        public virtual void Deserialize(byte[] SERIALIZEDSTUFF)
        {
            throw new NotImplementedException();
        }

        public virtual byte[] Serialize()
        {
            return null;
        }
    }
}