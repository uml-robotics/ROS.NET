namespace Messages.geometry_msgs
{
    public class TwistWithCovariance
    {
        public double[] covariance = new double[36];
        public Twist twist;
    }
}