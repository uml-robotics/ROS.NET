#region USINGZ

using System;
using System.Runtime.InteropServices;

#endregion

namespace Messages
{
    public static class SerializationHelper
    {
        public static T Deserialize<T>(byte[] bytes) where T : IRosMessage
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

        public static byte[] Serialize<T>(T outgoing) where T : IRosMessage
        {
            if (outgoing.Serialized != null)
                return outgoing.Serialized;
            outgoing.Serialized = new byte[Marshal.SizeOf(outgoing)];
            GCHandle h = GCHandle.Alloc(outgoing.Serialized, GCHandleType.Pinned);

            // copy the struct into int byte[] mem alloc 
            Marshal.StructureToPtr(outgoing, h.AddrOfPinnedObject(), false);

            h.Free(); //Allow GC to do its job 

            return outgoing.Serialized;
        }
    }

    public abstract class IRosMessage
    {
        public bool HasHeader = false;
        public bool KnownSize = true;

        public byte[] Serialized;

        public IRosMessage()
        {
        }

        public IRosMessage(byte[] SERIALIZEDSTUFF)
        {
            Deserialize(SERIALIZEDSTUFF);
        }

        public virtual void Deserialize(byte[] SERIALIZEDSTUFF)
        {
        }

        public virtual byte[] Serialize()
        {
            return null;
        }
    }

    /*public class StructTranslator
    {
        public bool Read<T>(byte[] buffer, int index, ref T retval)
        {
            if (index == buffer.Length) return false;
            int size = Marshal.SizeOf(typeof(T));
            if (index + size > buffer.Length) throw new IndexOutOfRangeException();
            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                IntPtr addr = (IntPtr)((long)handle.AddrOfPinnedObject() + index);
                retval = (T)Marshal.PtrToStructure(addr, typeof(T));
            }
            finally
            {
                handle.Free();
            }
            return true;
        }

        public bool Read<T>(Stream stream, ref T retval)
        {
            int size = Marshal.SizeOf(typeof(T));
            if (buffer == null || size > buffer.Length) buffer = new byte[size];
            int len = stream.Read(buffer, 0, size);
            if (len == 0) return false;
            if (len != size) throw new EndOfStreamException();
            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                retval = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            }
            finally
            {
                handle.Free();
            }
            return true;
        }

        private byte[] buffer;
    }*/
}