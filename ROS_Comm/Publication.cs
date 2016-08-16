// File: Publication.cs
// Project: ROS_C-Sharp
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 09/01/2015
// Updated: 02/10/2016

#region USINGZ

using System;
using System.Collections.Generic;
using System.Linq;
using Messages;
using XmlRpc_Wrapper;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace Ros_CSharp
{
    public class Publication : IDisposable
    {
        public string DataType = "";
        public bool Dropped;
        public bool HasHeader;
        public bool Latch;
        public int MaxQueue;
        public string Md5sum = "", MessageDefinition = "";
        public string Name = "";
        public uint _seq;

        public List<SubscriberCallbacks> callbacks = new List<SubscriberCallbacks>();
        public object callbacks_mutex = new object();
        public Header connection_header;
        internal MessageAndSerializerFunc last_message;
        internal Queue<MessageAndSerializerFunc> publish_queue = new Queue<MessageAndSerializerFunc>();
        public object publish_queue_mutex = new object();
        public object seq_mutex = new object();
        public List<SubscriberLink> subscriber_links = new List<SubscriberLink>();
        public object subscriber_links_mutex = new object();

        public Publication(string name, string datatype, string md5sum, string message_definition, int max_queue,
            bool latch, bool has_header)
        {
            Name = name;
            DataType = datatype;
            Md5sum = md5sum;
            MessageDefinition = message_definition;
            MaxQueue = max_queue;
            Latch = latch;
            HasHeader = has_header;
        }

        public int NumCallbacks
        {
            get { lock (callbacks_mutex) return callbacks.Count; }
        }

        public bool HasSubscribers
        {
            get { lock (subscriber_links_mutex) return subscriber_links.Count > 0; }
        }

        public int NumSubscribers
        {
            get { lock (subscriber_links_mutex) return subscriber_links.Count; }
        }

        #region IDisposable Members

        public void Dispose()
        {
            drop();
        }

        #endregion

        public XmlRpcValue GetStats()
        {
            XmlRpcValue stats = new XmlRpcValue();
            stats.Set(0, Name);
            XmlRpcValue conn_data = new XmlRpcValue();
            conn_data.SetArray(0);
            lock (subscriber_links_mutex)
            {
                int cidx = 0;
                foreach (SubscriberLink sub_link in subscriber_links)
                {
                    SubscriberLink.Stats s = sub_link.stats;
                    XmlRpcValue inside = new XmlRpcValue();
                    inside.Set(0, sub_link.connection_id);
                    inside.Set(1, s.bytes_sent);
                    inside.Set(2, s.message_data_sent);
                    inside.Set(3, s.messages_sent);
                    inside.Set(4, 0);
                    conn_data.Set(cidx++, inside);
                }
            }
            stats.Set(1, conn_data);
            return stats;
        }

        public void drop()
        {
            lock (publish_queue_mutex)
            {
                lock (subscriber_links_mutex)
                {
                    if (Dropped)
                        return;
                    Dropped = true;
                }
            }
            dropAllConnections();
        }

        public void addSubscriberLink(SubscriberLink link)
        {
            lock (subscriber_links_mutex)
            {
                if (Dropped) return;
                subscriber_links.Add(link);
                PollManager.Instance.addPollThreadListener(processPublishQueue);
            }

            if (Latch && last_message != null)
            {
                link.enqueueMessage(last_message);
            }

            peerConnect(link);
        }

        public void removeSubscriberLink(SubscriberLink link)
        {
            SubscriberLink lnk = null;
            lock (subscriber_links_mutex)
            {
                if (Dropped)
                    return;
                if (subscriber_links.Contains(link))
                {
                    lnk = link;
                    subscriber_links.Remove(lnk);
                    if (subscriber_links.Count == 0)
                        PollManager.Instance.removePollThreadListener(processPublishQueue);
                }
            }
            if (lnk != null)
                peerDisconnect(lnk);
        }

        internal void publish(MessageAndSerializerFunc msg)
        {
            lock (publish_queue_mutex)
            {
                publish_queue.Enqueue(msg);
            }
        }

        public bool validateHeader(Header header, ref string error_message)
        {
            string md5sum = "", topic = "", client_callerid = "";
            if (!header.Values.Contains("md5sum") || !header.Values.Contains("topic") ||
                !header.Values.Contains("callerid"))
            {
                const string msg = "Header from subscriber did not have the required elements: md5sum, topic, callerid";
                EDB.WriteLine(msg);
                error_message = msg;
                return false;
            }
            md5sum = (string) header.Values["md5sum"];
            topic = (string) header.Values["topic"];
            client_callerid = (string) header.Values["callerid"];
            if (Dropped)
            {
                string msg = "received a tcpros connection for a nonexistent topic [" + topic + "] from [" +
                             client_callerid + "].";
                EDB.WriteLine(msg);
                error_message = msg;
                return false;
            }

            if (Md5sum != md5sum && (md5sum != "*") && Md5sum != "*")
            {
                string datatype = header.Values.Contains("type") ? (string) header.Values["type"] : "unknown";
                string msg = "Client [" + client_callerid + "] wants topic [" + topic + "] to hava datatype/md5sum [" +
                             datatype + "/" + md5sum + "], but our version has [" + DataType + "/" + Md5sum +
                             "]. Dropping connection";
                EDB.WriteLine(msg);
                error_message = msg;
                return false;
            }
            return true;
        }

        public void getInfo(XmlRpcValue info)
        {
            lock (subscriber_links_mutex)
            {
                foreach (SubscriberLink c in subscriber_links)
                {
                    XmlRpcValue curr_info = new XmlRpcValue();
                    curr_info.Set(0, (int) c.connection_id);
                    curr_info.Set(1, c.destination_caller_id);
                    curr_info.Set(2, "o");
                    curr_info.Set(3, "TCPROS");
                    curr_info.Set(4, Name);
                    info.Set(info.Size, curr_info);
                }
            }
        }

        public void addCallbacks(SubscriberCallbacks callbacks)
        {
            lock (callbacks_mutex)
            {
                this.callbacks.Add(callbacks);
                if (callbacks.connect != null && callbacks.Callback != null)
                {
                    lock (subscriber_links_mutex)
                    {
                        foreach (SubscriberLink i in subscriber_links)
                        {
                            CallbackInterface cb = new PeerConnDisconnCallback(callbacks.connect, i);

                            callbacks.Callback.addCallback(cb, callbacks.Get());
                        }
                    }
                }
            }
        }

        public void removeCallbacks(SubscriberCallbacks callbacks)
        {
            lock (callbacks_mutex)
            {
                callbacks.Callback.removeByID(callbacks.Get());
                if (this.callbacks.Contains(callbacks))
                    this.callbacks.Remove(callbacks);
            }
        }

        public string dumphex(byte[] test)
        {
            return test.Aggregate("", (current, t) => current + ((t < 16 ? "0" : "") + t.ToString("x") + " "));
        }

        internal bool EnqueueMessage(MessageAndSerializerFunc holder)
        {
            lock (subscriber_links_mutex)
            {
                if (Dropped) return false;
            }

            uint seq = incrementSequence();

            if (HasHeader)
            {
                object h = holder.msg.GetType().GetField("header").GetValue(holder.msg);
                m.Header header;
                if (h == null)
                    header = new m.Header();
                else
                    header = (m.Header) h;
                header.seq = seq;
                if (header.stamp == null)
                {
                    header.stamp = ROS.GetTime();
                }
                if (header.frame_id == null)
                {
                    header.frame_id = "";
                }
                holder.msg.GetType().GetField("header").SetValue(holder.msg, header);
            }
            holder.msg.connection_header = connection_header.Values;

            lock (subscriber_links_mutex)
                foreach (SubscriberLink sub_link in subscriber_links)
                {
                    sub_link.enqueueMessage(holder);
                }

            if (Latch)
            {
                last_message = new MessageAndSerializerFunc(holder.msg, holder.serfunc, false, true);
            }
            return true;
        }

        public void dropAllConnections()
        {
            List<SubscriberLink> local_publishers = null;
            lock (subscriber_links_mutex)
            {
                local_publishers = new List<SubscriberLink>(subscriber_links);
                subscriber_links.Clear();
            }
            foreach (SubscriberLink link in local_publishers)
            {
                link.drop();
            }
            local_publishers.Clear();
        }

        public void peerConnect(SubscriberLink sub_link)
        {
            //EDB.WriteLine("PEER CONNECT: ["+sub_link.topic+"]");
            foreach (SubscriberCallbacks cbs in callbacks)
            {
                if (cbs.connect != null && cbs.Callback != null)
                {
                    CallbackInterface cb = new PeerConnDisconnCallback(cbs.connect, sub_link);
                    cbs.Callback.addCallback(cb, cbs.Get());
                }
            }
        }

        public void peerDisconnect(SubscriberLink sub_link)
        {
            //EDB.WriteLine("PEER DISCONNECT: [" + sub_link.topic + "]");
            foreach (SubscriberCallbacks cbs in callbacks)
            {
                if (cbs.disconnect != null && cbs.Callback != null)
                {
                    CallbackInterface cb = new PeerConnDisconnCallback(cbs.disconnect, sub_link);
                    cbs.Callback.addCallback(cb, cbs.Get());
                }
            }
        }

        public uint incrementSequence()
        {
            lock (seq_mutex)
            {
                uint old_seq = _seq;
                ++_seq;
                return old_seq;
            }
        }

        public void processPublishQueue()
        {
            lock (publish_queue_mutex)
            {
                if (Dropped) return;
                while (publish_queue.Count > 0)
                {
                    EnqueueMessage(publish_queue.Dequeue());
                }
            }
        }

        internal void getPublishTypes(ref bool serialize, ref bool nocopy, MsgTypes typeEnum)
        {
            lock (subscriber_links_mutex)
            {
                foreach (SubscriberLink sub in subscriber_links)
                {
                    bool s = false, n = false;
                    sub.getPublishTypes(ref s, ref n, typeEnum);
                    serialize = serialize || s;
                    nocopy = nocopy || n;
                    if (serialize && nocopy)
                        break;
                }
            }
        }
    }

    public class PeerConnDisconnCallback : CallbackInterface
    {
        public SubscriberStatusCallback callback;
        public SubscriberLink sub_link;

        public PeerConnDisconnCallback(SubscriberStatusCallback callback, SubscriberLink sub_link)
        {
            this.callback = callback;
            this.sub_link = sub_link;
        }

        public virtual CallResult call()
        {
            ROS.Debug("CALLED PEERCONNDISCONNCALLBACK");
            SingleSubscriberPublisher pub = new SingleSubscriberPublisher(sub_link);
            callback(pub);
            return CallResult.Success;
        }
    }

    public delegate void SubscriberStatusCallback(SingleSubscriberPublisher pub);
}