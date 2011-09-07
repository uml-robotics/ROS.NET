#region USINGZ

using Messages.geometry_msgs;
using Messages.std_msgs;

#endregion

namespace Messages.nav_msgs
{
    public class GridCells
    {
        public float cell_height;
        public float cell_width;
        public Point[] cells;
        public Header header;
    }
}