#region USINGZ

using Messages.std_msgs;

#endregion

namespace Messages.geometry_msgs
{
    public class PolygonStamped
    {
        public Header header;
        public Polygon polygon;
    }
}