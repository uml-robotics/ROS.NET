#region USINGZ

using System.Runtime.InteropServices;

#endregion

namespace Messages.nav_msgs
{
    public class OccupancyGrid
    {
        public const bool HasHeader = true;
        public const bool KnownSize = false;

        public Data data;

        public OccupancyGrid()
        {
        }

        public OccupancyGrid(byte[] SERIALIZEDSTUFF)
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
            public MapMetaData.Data info;
            public byte[] data;
        }

        #endregion
    }
}