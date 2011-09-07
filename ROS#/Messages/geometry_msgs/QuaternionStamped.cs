#region USINGZ

using Messages.std_msgs;

#endregion

namespace Messages.geometry_msgs
{
    public class QuaternionStamped
    {
        public Header header;
        public Quaternion quaternion;
    }
}