namespace Messages.std_msgs
{
    internal class Time
    {
        public TimeData data;


        public Time(uint s, uint ns) : this(new TimeData { sec = s, nsec = ns }) { }
        public Time(TimeData s) { data = s; }
        public Time() : this(0, 0) { }
    }
}