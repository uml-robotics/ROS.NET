#region USINGZ

using System;
using System.Runtime.InteropServices;

#endregion

namespace Messages
{
    public class Header
    {
        public const bool HasHeader = false;
        public const bool KnownSize = true;

        public Data data;

        public Header()
        {
        }

        public Header(byte[] SERIALIZEDSTUFF)
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
            public uint seq;
            public DateTime stamp;
            public string frame_id;
        }

        #endregion
    }
}