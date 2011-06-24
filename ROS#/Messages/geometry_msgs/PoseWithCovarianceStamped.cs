#region USINGZ

using System.Runtime.InteropServices;

#endregion

namespace Messages.geometry_msgs
{
    public class PoseWithCovarianceStamped
    {
        public const bool HasHeader = true;
        public const bool KnownSize = false;

        public Data data;

        public PoseWithCovarianceStamped()
        {
        }

        public PoseWithCovarianceStamped(byte[] SERIALIZEDSTUFF)
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
            public PoseWithCovariance.Data pose;
        }

        #endregion
    }
}