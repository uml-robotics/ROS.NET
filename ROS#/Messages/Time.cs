#region USINGZ

using System;
using System.Runtime.InteropServices;

#endregion

namespace Messages
{
    public class Time
    {
        public const bool HasHeader = false;
        public const bool KnownSize = true;

        public Data data;

        public Time()
        {
        }

        public Time(byte[] SERIALIZEDSTUFF)
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
            public DateTime data;
        }

        #endregion
    }
}