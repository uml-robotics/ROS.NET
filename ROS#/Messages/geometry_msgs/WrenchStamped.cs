#region USINGZ

using Messages.std_msgs;

#endregion

namespace Messages.geometry_msgs
{
    public class WrenchStamped
    {
        public Header header;
        public Wrench wrench;
    }
}