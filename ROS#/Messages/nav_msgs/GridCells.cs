#region USINGZ

using Messages.geometry_msgs;
using Messages.std_msgs;

#endregion

namespace Messages.nav_msgs
{
    public class GridCells
    {
        public double cell_height;
        public double cell_width;
        public Point[] cells;
        public Header header;
    }
}