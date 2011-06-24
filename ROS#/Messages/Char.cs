#region USINGZ

using System.Runtime.InteropServices;

#endregion

namespace Messages
{
    public class Char
    {
        public const bool HasHeader = false;
        public const bool KnownSize = true;

        public Data data;

        public Char()
        {
        }

        public Char(byte[] SERIALIZEDSTUFF)
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
            public char data;
        }

        #endregion
    }
}