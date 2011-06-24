#region USINGZ

using System.Runtime.InteropServices;

#endregion

namespace Messages
{
    public class Int16MultiArray
    {
        public const bool HasHeader = false;
        public const bool KnownSize = false;

        public Data data;

        public Int16MultiArray()
        {
        }

        public Int16MultiArray(byte[] SERIALIZEDSTUFF)
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
            public int[] data;
        }

        #endregion
    }
}