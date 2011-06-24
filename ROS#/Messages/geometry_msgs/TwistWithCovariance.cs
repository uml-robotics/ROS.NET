#region USINGZ

using System.Runtime.InteropServices;

#endregion

namespace Messages.geometry_msgs
{
    public class TwistWithCovariance
    {
        public const bool HasHeader = false;
        public const bool KnownSize = false;

        public Data data;

        public TwistWithCovariance()
        {
        }

        public TwistWithCovariance(byte[] SERIALIZEDSTUFF)
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
            public Twist.Data twist;
            public double[] covariance;
        }

        #endregion
    }
}