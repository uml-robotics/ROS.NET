using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XmlRpc_Wrapper;
using m = Messages;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

namespace EricIsAMAZING
{
    public abstract class SubscriberLink
    {
        public class Stats
        {
            public int bytes_sent, message_data_sent, messages_sent;
        }

        public SubscriberLink()
        {

        }

        public string topic, destination_caller_id;
        public uint connection_id;
        public Stats stats;

        public string Md5sum { get { lock(parent)
        {
            return parent.Md5sum;
        }
        } }
        public string DataType { get
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

        public virtual void enqueueMessage(m.IRosMessage msg, bool ser, bool nocopy)
        {
            
        }

        public virtual void drop()
        {

        }

        public virtual void getPublishTypes(ref bool ser, ref bool nocopy, ref XmlRpcValue.TypeEnum type_info)
        {
            ser = true;
            nocopy = false;
        }

        protected bool verifyDatatype(string datatype)
        {
            if (parent == null)
                return false;
            lock(parent)
            {
                if (datatype != parent.DataType)
                    return false;
                return true;
            }
        }

        protected Publication parent;
    }
}
