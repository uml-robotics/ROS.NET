#region USINGZ

using System.Runtime.InteropServices;

#endregion

namespace Messages
{
    public class Float64
    {
        public const bool HasHeader = false;
        public const bool KnownSize = true;

        public Data data;

        public Float64()
        {
        }

        public Float64(byte[] SERIALIZEDSTUFF)
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
            public double data;
        }

        #endregion
    }
}