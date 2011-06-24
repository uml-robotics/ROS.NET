#region USINGZ

using System.Runtime.InteropServices;

#endregion

namespace Messages.geometry_msgs
{
    public class PointStamped
    {
        public const bool HasHeader = true;
        public const bool KnownSize = false;

        public Data data;

        public PointStamped()
        {
        }

        public PointStamped(byte[] SERIALIZEDSTUFF)
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
            public Header.Data header;
            public Point.Data point;
        }

        #endregion
    }
}