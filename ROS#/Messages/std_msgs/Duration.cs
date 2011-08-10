namespace Messages.std_msgs
{
    public class Duration
    {
        public ulong data;


        public Duration(ulong s)
        {
            data = s;
        }

        public Duration()
        {
            data = 0;
        }
    }
}