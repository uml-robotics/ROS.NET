#region USINGZ

using System.Runtime.InteropServices;
using Messages.geometry_msgs;

#endregion

namespace Messages.nav_msgs
{
    public class Odometry
    {
        public const bool HasHeader = true;
        public const bool KnownSize = false;

        public Data data;

        public Odometry()
        {
        }

        public Odometry(byte[] SERIALIZEDSTUFF)
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
            public string child_frame_id;
            public PoseWithCovariance.Data pose;
            public TwistWithCovariance.Data twist;
        }

        #endregion
    }
}