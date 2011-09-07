namespace Messages.std_msgs
{
    public class Duration
    {
        public TimeData data;


        public Duration(uint s, uint ns) : this(new TimeData {sec = s, nsec = ns})
        {
        }

        public Duration(TimeData s)
        {
            data = s;
        }

        public Duration() : this(0, 0)
        {
        }
    }
}