#region USINGZ

using System;
using System.Runtime.InteropServices;
using Messages.geometry_msgs;

#endregion

namespace Messages.nav_msgs
{
    public class MapMetaData
    {
        public const bool HasHeader = false;
        public const bool KnownSize = false;

        public Data data;

        public MapMetaData()
        {
        }

        public MapMetaData(byte[] SERIALIZEDSTUFF)
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
            public DateTime map_load_time;
            public double resolution;
            public uint width;
            public uint height;
            public Pose.Data origin;
        }

        #endregion
    }
}