#region USINGZ

using Messages.std_msgs;

#endregion

namespace Messages.geometry_msgs
{
    public class TwistWithCovarianceStamped
    {
        public Header header;
        public TwistWithCovariance twist;
    }
}