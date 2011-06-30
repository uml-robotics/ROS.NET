#region USINGZ

using System;
using System.Runtime.InteropServices;

#endregion

namespace XmlRpc_Wrapper
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

        public static byte[] Serialize<T>(T outgoing)
        {
            byte[] buffer = new byte[Marshal.SizeOf(outgoing)];
            GCHandle h = GCHandle.Alloc(buffer, GCHandleType.Pinned);

            // copy the struct into int byte[] mem alloc 
            Marshal.StructureToPtr(outgoing, h.AddrOfPinnedObject(), false);

            h.Free(); //Allow GC to do its job 

            return buffer;
        }
    }
}