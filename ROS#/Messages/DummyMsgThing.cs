#region USINGZ

using System.Runtime.InteropServices;
using Messages.geometry_msgs;

#endregion

namespace Messages
{
    public class DummyMsgThing
    {
        public const bool HasHeader = false;
        public const bool KnownSize = false;

        public Data data;

        public DummyMsgThing()
        {
        }

        public DummyMsgThing(byte[] SERIALIZEDSTUFF)
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
            public Twist.Data leftnipple;
            public Twist.Data rightnipple;
        }

        #endregion
    }
}