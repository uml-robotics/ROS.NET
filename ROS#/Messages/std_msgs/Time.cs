namespace Messages.std_msgs
{
    public class Time
    {
        public ulong data;


        public Time(ulong s)
        {
            data = s;
        }

        public Time()
        {
            data = 0;
        }
    }
}