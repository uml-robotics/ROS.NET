#region USINGZ

using System;
using Messages;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace EricIsAMAZING
{
    public class SubscriberLink
    {
        public uint connection_id;
        public string destination_caller_id = "";
        protected Publication parent;
        public Stats stats;
        public string topic = "";

        public string Md5sum
        {
            get
            {
                lock (parent)
                {
                    return parent.Md5sum;
                }
            }
        }

        public string DataType
        {
            get
            {
                lock (parent)
                {
                    return parent.DataType;
                }
            }
        }

        public string MessageDefinition
        {
            get
            {
                lock (parent)
                {
                    return parent.MessageDefinition;
                }
            }
        }

        public virtual void enqueueMessage(IRosMessage msg, bool ser, bool nocopy)
        {
            throw new NotImplementedException();
        }

        public virtual void drop()
        {
            throw new NotImplementedException();
        }

        public virtual void getPublishTypes(ref bool ser, ref bool nocopy, ref MsgTypes type_info)
        {
            ser = true;
            nocopy = false;
        }

        protected bool verifyDatatype(string datatype)
        {
            if (parent == null)
                return false;
            lock (parent)
            {
                if (datatype != parent.DataType)
                    return false;
                return true;
            }
        }

        #region Nested type: Stats

        public class Stats
        {
            public int bytes_sent, message_data_sent, messages_sent;
        }

        #endregion
    }
}