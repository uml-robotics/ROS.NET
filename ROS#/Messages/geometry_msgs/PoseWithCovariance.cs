namespace Messages.geometry_msgs
{
    public class PoseWithCovariance
    {
        public double[] covariance = new double[36];
        public Pose pose;
    }
}