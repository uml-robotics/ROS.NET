#region USINGZ

using System.Runtime.InteropServices;

#endregion

namespace Messages
{
    public class Int32
    {
        public const bool HasHeader = false;
        public const bool KnownSize = true;

        public Data data;

        public Int32()
        {
        }

        public Int32(byte[] SERIALIZEDSTUFF)
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
            public int data;
        }

        #endregion
    }
}