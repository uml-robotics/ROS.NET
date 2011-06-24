#region USINGZ

using System.Runtime.InteropServices;

#endregion

namespace Messages.geometry_msgs
{
    public class Point
    {
        public const bool HasHeader = false;
        public const bool KnownSize = true;

        public Data data;

        public Point()
        {
        }

        public Point(byte[] SERIALIZEDSTUFF)
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
            public double x;
            public double y;
            public double z;
        }

        #endregion
    }
}