#region USINGZ

using Messages.geometry_msgs;
using Messages.std_msgs;

#endregion

namespace Messages.nav_msgs
{
    public class MapMetaData
    {
        public uint height;
        public Time map_load_time;
        public Pose origin;
        public float resolution;
        public uint width;
    }
}