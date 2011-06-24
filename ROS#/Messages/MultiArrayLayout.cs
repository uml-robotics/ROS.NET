#region USINGZ

using System.Runtime.InteropServices;

#endregion

namespace Messages
{
    public class MultiArrayLayout
    {
        public const bool HasHeader = false;
        public const bool KnownSize = false;

        public Data data;

        public MultiArrayLayout()
        {
        }

        public MultiArrayLayout(byte[] SERIALIZEDSTUFF)
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
            public MultiArrayDimension.Data[] dim;
            public uint data_offset;
        }

        #endregion
    }
}