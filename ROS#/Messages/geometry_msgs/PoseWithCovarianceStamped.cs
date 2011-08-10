#region USINGZ

using Messages.std_msgs;

#endregion

namespace Messages.geometry_msgs
{
    public class PoseWithCovarianceStamped
    {
        public Header header;
        public PoseWithCovariance pose;
    }
}