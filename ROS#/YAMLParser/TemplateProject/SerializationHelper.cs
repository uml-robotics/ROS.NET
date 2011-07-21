#region USINGZ

using System;
using System.Collections;
using System.Runtime.InteropServices;

#endregion

namespace Messages
{
    public static class SerializationHelper
    {
        public static T Deserialize<T>(byte[] bytes)
        {
            T thestructure = default(T);
            IntPtr pIP = Marshal.AllocHGlobal(Marshal.SizeOf(thestructure));
            Marshal.Copy(bytes, 0, pIP, Marshal.SizeOf(thestructure));
            thestructure = (T) Marshal.PtrToStructure(pIP, typeof (T));
            Marshal.FreeHGlobal(pIP);
            /*StructTranslator thisone = new StructTranslator();
            T thestructure = default(T);
            if (thisone.Read<T>(bytes, 0, ref thestructure))
                Console.WriteLine("YAY!");*/
            return thestructure;
        }


        public static byte[] Serialize<T>(TypedMessage<T> outgoing) where T : struct
        {
            if (outgoing.Serialized != null)
                return outgoing.Serialized;
            outgoing.Serialized = new byte[Marshal.SizeOf(outgoing.data)];
            GCHandle h = GCHandle.Alloc(outgoing.Serialized, GCHandleType.Pinned);

            // copy the struct into int byte[] mem alloc 
            Marshal.StructureToPtr(outgoing.data, h.AddrOfPinnedObject(), false);

            h.Free(); //Allow GC to do its job 

            return outgoing.Serialized;
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
            data = SerializationHelper.Deserialize<M>(SERIALIZEDSTUFF);
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

        #region Nested type: data

        public struct data
        {
        }

        #endregion
    }
}