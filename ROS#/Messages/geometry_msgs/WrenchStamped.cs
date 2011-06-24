#region USINGZ

using System.Runtime.InteropServices;

#endregion

namespace Messages.geometry_msgs
{
    public class WrenchStamped
    {
        public const bool HasHeader = true;
        public const bool KnownSize = false;

        public Data data;

        public WrenchStamped()
        {
        }

        public WrenchStamped(byte[] SERIALIZEDSTUFF)
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
            public Wrench.Data wrench;
        }

        #endregion
    }
}