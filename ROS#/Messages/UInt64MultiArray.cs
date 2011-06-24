#region USINGZ

using System.Runtime.InteropServices;

#endregion

namespace Messages
{
    public class UInt64MultiArray
    {
        public const bool HasHeader = false;
        public const bool KnownSize = false;

        public Data data;

        public UInt64MultiArray()
        {
        }

        public UInt64MultiArray(byte[] SERIALIZEDSTUFF)
        {
            data = SerializationHelper.Deserialize<Data>(SERIALIZEDSTUFF);
        }

        public byte[] Serialize()
        {
            return SerializationHelper.Serialize(data);
        }

        #region Nested type: Data

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Data
        {
            public MultiArrayLayout.Data layout;
            public uint[] data;
        }

        #endregion
    }
}