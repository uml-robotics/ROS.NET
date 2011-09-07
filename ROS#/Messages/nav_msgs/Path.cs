#region USINGZ

using Messages.geometry_msgs;
using Messages.std_msgs;

#endregion

namespace Messages.nav_msgs
{
    public class Path
    {
        public Header header;
        public PoseStamped[] poses;
    }
}