#region USINGZ

using Messages.std_msgs;

#endregion

namespace Messages.geometry_msgs
{
    public class PoseArray
    {
        public Header header;
        public Pose[] poses;
    }
}