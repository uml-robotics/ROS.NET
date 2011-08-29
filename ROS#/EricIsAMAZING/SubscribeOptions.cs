#region USINGZ

using System;
using Messages;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace Ros_CSharp
{
    public class SubscribeOptions<T> where T : IRosMessage, new()
    {
        public bool allow_concurrent_callbacks;
        public CallbackQueueInterface callback_queue;
        public string datatype = "";
        public bool has_header;
        public SubscriptionCallbackHelper<T> helper;
        public bool latch;
        public string md5sum = "";
        public string message_definition = "";
        public int queue_size;
        public string topic = "";

        public SubscribeOptions() : this("", 1)
        {
            allow_concurrent_callbacks = false;
        }

        public SubscribeOptions(string topic, int queue_size, CallbackDelegate<T> CALL = null)
        {
            // TODO: Complete member initialization
            this.topic = topic;
            this.queue_size = queue_size;
            if (CALL != null)
                helper = new SubscriptionCallbackHelper<T>(new T().type, CALL);
            else
                helper = new SubscriptionCallbackHelper<T>(new T().type);


            Type msgtype = typeof (T).GetGenericArguments()[0];
            string[] chunks = msgtype.FullName.Split('.');
            datatype = chunks[1] + "/" + chunks[2];
            md5sum = MD5.Sum(new T().type);
        }
    }

    public delegate void CallbackDelegate<T>(T argument) where T : IRosMessage, new();
}