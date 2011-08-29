namespace Ros_CSharp
{
    public class TopicInfo
    {
        public string data_type;
        public string name;

        public TopicInfo(string name, string data_type)
        {
            // TODO: Complete member initialization
            this.name = name;
            this.data_type = data_type;
        }
    }
}