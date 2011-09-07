#region USINGZ

using Messages.geometry_msgs;
using Messages.std_msgs;

#endregion

namespace Messages.nav_msgs
{
    public class Odometry
    {
        public String child_frame_id;
        public Header header;
        public PoseWithCovariance pose;
        public TwistWithCovariance twist;
    }
}