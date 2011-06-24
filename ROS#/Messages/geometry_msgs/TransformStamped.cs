#region USINGZ

using System.Runtime.InteropServices;

#endregion

namespace Messages.geometry_msgs
{
    public class TransformStamped
    {
        public const bool HasHeader = true;
        public const bool KnownSize = false;

        public Data data;

        public TransformStamped()
        {
        }

        public TransformStamped(byte[] SERIALIZEDSTUFF)
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
            public Transform.Data transform;
        }

        #endregion
    }
}