#region USINGZ

using Messages.std_msgs;

#endregion

namespace Messages.nav_msgs
{
    public class OccupancyGrid
    {
        public sbyte[] data;
        public Header header;
        public MapMetaData info;
    }
}