#region USINGZ

using Messages.std_msgs;

#endregion

namespace Messages.geometry_msgs
{
    public class TransformStamped
    {
        public String child_frame_id;
        public Header header;
        public Transform transform;
    }
}