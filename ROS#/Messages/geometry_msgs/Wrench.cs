#region USINGZ

using System.Runtime.InteropServices;

#endregion

namespace Messages.geometry_msgs
{
    public class Wrench
    {
        public const bool HasHeader = false;
        public const bool KnownSize = false;

        public Data data;

        public Wrench()
        {
        }

        public Wrench(byte[] SERIALIZEDSTUFF)
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
            public Vector3.Data force;
            public Vector3.Data torque;
        }

        #endregion
    }
}