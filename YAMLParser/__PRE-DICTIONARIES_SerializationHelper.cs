#region USINGZ

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

#endregion

namespace Messages
{
    public static class SerializationHelper
    {
        public static TypedMessage<T> Deserialize<T>(byte[] bytes) where T : class, new()
        {
            return new TypedMessage<T>((T)deserialize(typeof(T), bytes, true));
        }

        public static object deserialize(Type T, byte[] bytes, bool iswhole = false)
        {
            object thestructure = Activator.CreateInstance(T);
            FieldInfo[] infos = T.GetFields();
            int totallength = BitConverter.ToInt32(bytes, 0);
            int currpos = iswhole ? 4 : 0;
            int currinfo = 0;
            while (currpos < bytes.Length)
            {
                int len = BitConverter.ToInt32(bytes, currpos);
                IntPtr pIP = Marshal.AllocHGlobal(len);
                Marshal.Copy(bytes, currpos, pIP, len);
                if (infos[currinfo].FieldType.ToString().Contains("Messages"))
                {
                    byte[] smallerpiece = new byte[len + 4];
                    Array.Copy(bytes, currpos, smallerpiece, 0, len + 4);
                    infos[currinfo].SetValue(thestructure, deserialize(infos[currinfo].FieldType, smallerpiece));
                }
                else
                    infos[currinfo].SetValue(thestructure, Marshal.PtrToStructure(pIP, infos[currinfo].FieldType));
                currinfo++;
                currpos += len;
            }
            if (iswhole && currpos != totallength + 4)
                throw new Exception("MATH FAIL LOL!");
            return thestructure;
        }


        public static byte[] Serialize<T>(TypedMessage<T> outgoing) where T : class, new()
        {
            if (outgoing.Serialized != null)
                return outgoing.Serialized;
            outgoing.Serialized = SlapChop(outgoing.data.GetType(), outgoing.data);
            return outgoing.Serialized;
        }

        public static byte[] SlapChop(Type T, object t)
        {
            FieldInfo[] infos = t.GetType().GetFields();
            Queue<byte[]> chunks = new Queue<byte[]>();
            int totallength = 0;
            foreach (FieldInfo info in infos)
            {
                if (info.Name.Contains("(")) continue;
                byte[] thischunk = NeedsMoreChunks(info.FieldType, info.GetValue(t), info, t, (info.GetValue(Activator.CreateInstance(T)) != null));
                chunks.Enqueue(thischunk);
                totallength += thischunk.Length;
            }
#if FALSE
            byte[] wholeshebang = new byte[totallength];
            int currpos = 0;
#else
            byte[] wholeshebang = new byte[totallength + 4]; //THE WHOLE SHEBANG
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

        public static byte[] NeedsMoreChunks(Type T, object val, FieldInfo fi, object topmost, bool knownlength)
        {
            byte[] thischunk = null;
            if (!T.IsArray)
            {
                if (T.Namespace.Contains("Message"))
                {
                    IRosMessage msg = null;
                    if (val != null)
                        msg = (IRosMessage)Activator.CreateInstance(typeof(TypedMessage<>).MakeGenericType(T), val);
                    else
                        msg = (IRosMessage)Activator.CreateInstance(typeof(TypedMessage<>).MakeGenericType(T));
                    thischunk = msg.Serialize();
                }
                else if (val is string || T == typeof(string))
                {
                    if (val == null)
                        val = "";
                    byte[] nolen = Encoding.ASCII.GetBytes((string)val);
                    thischunk = new byte[nolen.Length + 4];
                    byte[] bylen2 = BitConverter.GetBytes(nolen.Length);
                    Array.Copy(nolen, 0, thischunk, 4, nolen.Length);
                    Array.Copy(bylen2, thischunk, 4);
                }
                else
                {
                    byte[] temp = new byte[Marshal.SizeOf(T)];
                    GCHandle h = GCHandle.Alloc(temp, GCHandleType.Pinned);
                    Marshal.StructureToPtr(val, h.AddrOfPinnedObject(), false);
                    h.Free();
                    thischunk = new byte[temp.Length + (knownlength?0:4)];
                    if (!knownlength)
                    {
                        byte[] bylen = BitConverter.GetBytes(temp.Length);
                        Array.Copy(bylen, 0, thischunk, 0, 4);
                    }
                    Array.Copy(temp, 0, thischunk, (knownlength?0:4), temp.Length);

                }
            }
            else
            {
                int arraylength = 0;
                List<object> valslist = new List<object>();
                foreach (object o in (val as Array))
                {
                    valslist.Add(o);
                }
                object[] vals = valslist.ToArray();
                Queue<byte[]> arraychunks = new Queue<byte[]>();
                for (int i = 0; i < vals.Length; i++)
                {
                    Type TT = vals[i].GetType();
#if arraypiecesneedlengthtoo
                    bool pieceknownlength = ;
                    byte[] chunkwithoutlen = NeedsMoreChunks(TT, vals[i], fi, topmost, pieceknownlength);
                    byte[] chunklen = BitConverter.GetBytes(chunkwithoutlen.Length);
                    byte[] chunk = new byte[chunkwithoutlen.Length + (pieceknownlength ? 0 : 4)];
                    if (!pieceknownlength)
                        Array.Copy(chunklen, 0, chunk, 0, 4);
                    Array.Copy(chunkwithoutlen, 0, chunk, (pieceknownlength ? 0 : 4), chunkwithoutlen.Length);
#else
                    byte[] chunk = NeedsMoreChunks(TT, vals[i], fi, topmost, true);
#endif
                    arraychunks.Enqueue(chunk);
                    arraylength += chunk.Length;
                }
                thischunk = new byte[knownlength?arraylength:(arraylength + 4)];
                if (!knownlength)
                {
                    byte[] bylen = BitConverter.GetBytes(vals.Length);
                    Array.Copy(bylen, 0, thischunk, 0, 4);
                }
                int arraypos = knownlength?0:4;
                while (arraychunks.Count > 0)
                {
                    byte[] chunk = arraychunks.Dequeue();
                    Array.Copy(chunk, 0, thischunk, arraypos, chunk.Length);
                    arraypos += chunk.Length;
                }
            }
            return thischunk;
        }
    }

    public class TypedMessage<M> : IRosMessage where M : class, new()
    {
        public M data = new M();

        public TypedMessage()
            : base((MsgTypes)Enum.Parse(typeof(MsgTypes), typeof(M).FullName.Replace("Messages.", "").Replace(".", "__")),
                   TypeHelper.MessageDefinitions[(MsgTypes)Enum.Parse(typeof(MsgTypes), typeof(M).FullName.Replace("Messages.", "").Replace(".", "__"))],
                   TypeHelper.IsMetaType[(MsgTypes)Enum.Parse(typeof(MsgTypes), typeof(M).FullName.Replace("Messages.", "").Replace(".", "__"))])
        {
        }

        public TypedMessage(M d)
        {
            data = d;
            base.type = (MsgTypes)Enum.Parse(typeof(MsgTypes), typeof(M).FullName.Replace("Messages.", "").Replace(".", "__"));
            base.MessageDefinition = TypeHelper.MessageDefinitions[(MsgTypes)Enum.Parse(typeof(MsgTypes), typeof(M).FullName.Replace("Messages.", "").Replace(".", "__"))];
            base.IsMeta = TypeHelper.IsMetaType[(MsgTypes)Enum.Parse(typeof(MsgTypes), typeof(M).FullName.Replace("Messages.", "").Replace(".", "__"))];
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

        public IRosMessage()
            : this(MsgTypes.Unknown, "", false)
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