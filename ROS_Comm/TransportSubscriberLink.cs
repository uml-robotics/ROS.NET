// File: TransportSubscriberLink.cs
// Project: ROS_C-Sharp
// 
// ROS#
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 03/04/2013
// Updated: 07/26/2013

#region Using

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Messages;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace Ros_CSharp
{
    public class TransportSubscriberLink : SubscriberLink, IDisposable
    {
        public Connection connection;
        private bool header_written;
        private Queue<IRosMessage> outbox = new Queue<IRosMessage>();
        private object outbox_mutex = new object();
        private new Publication parent;
        private bool queue_full;
        private bool writing_message;

        public TransportSubscriberLink()
        {
            writing_message = false;
            header_written = false;
            queue_full = false;
        }

        #region IDisposable Members

        public void Dispose()
        {
            drop();
        }

        #endregion

        public bool initialize(Connection connection)
        {
            if (parent != null)
                EDB.WriteLine("Init transport subscriber link: " + parent.Name);
            this.connection = connection;
            connection.DroppedEvent += onConnectionDropped;
            return true;
        }

        public bool handleHeader(Header header)
        {
            if (!header.Values.Contains("topic"))
            {
                string msg = "Header from subscriber did not have the required element: topic";
                EDB.WriteLine(msg);
                connection.sendHeaderError(ref msg);
                return false;
            }
            string name = (string) header.Values["topic"];
            string client_callerid = (string) header.Values["callerid"];
            Publication pt = TopicManager.Instance.lookupPublication(name);
            if (pt == null)
            {
                string msg = "received a connection for a nonexistent topic [" + name + "] from [" +
                             connection.transport + "] [" + client_callerid + "]";
                EDB.WriteLine(msg);
                connection.sendHeaderError(ref msg);
                return false;
            }
            string error_message = "";
            if (!pt.validateHeader(header, ref error_message))
            {
                connection.sendHeaderError(ref error_message);
                EDB.WriteLine(error_message);
                return false;
            }
            destination_caller_id = client_callerid;
            connection_id = ConnectionManager.Instance.GetNewConnectionID();
            name = pt.Name;
            parent = pt;

            IDictionary m = new Hashtable();
            m["type"] = pt.DataType;
            m["md5sum"] = pt.Md5sum;
            m["message_definition"] = pt.MessageDefinition;
            m["callerid"] = this_node.Name;
            m["latching"] = pt.Latch;
            connection.writeHeader(m, onHeaderWritten);
            pt.addSubscriberLink(this);
            EDB.WriteLine("Finalize transport subscriber link for " + name);
            return true;
        }

        public override void enqueueMessage(IRosMessage msg, bool ser, bool nocopy)
        {
            lock (outbox_mutex)
            {
                int max_queue = 0;
                if (parent != null)
                    lock (parent)
                    {
                        max_queue = parent.MaxQueue;
                    }
                if (max_queue > 0 && outbox.Count >= max_queue)
                {
                    if (!queue_full)
                    {
                        outbox.Dequeue();
                        queue_full = true;
                    }
                }
                else
                    queue_full = false;
                outbox.Enqueue(msg);
            }

            startMessageWrite(false);
        }

        public override void drop()
        {
            if (connection.sendingHeaderError)
                connection.DroppedEvent -= onConnectionDropped;
            else
                connection.drop(Connection.DropReason.Destructing);
        }

        private void onConnectionDropped(Connection conn, Connection.DropReason reason)
        {
            if (conn != connection || parent == null) return;
            lock (parent)
            {
                parent.removeSubscriberLink(this);
            }
        }

        private void onHeaderWritten(Connection conn)
        {
            header_written = true;
            startMessageWrite(true);
        }

        private void onMessageWritten(Connection conn)
        {
            writing_message = false;
            startMessageWrite(true);
        }

        private void startMessageWrite(bool immediate_write)
        {
            IRosMessage m = null;
            lock (outbox_mutex)
            {
                if (writing_message || !header_written)
                    return;
                if (outbox.Count > 0)
                {
                    writing_message = true;
                    m = outbox.Dequeue();
                }
            }
            if (m != null)
            {
                byte[] M = m.Serialize();
                stats.messages_sent++;
                //EDB.WriteLine("Message backlog = " + (triedtosend - stats.messages_sent));
                stats.bytes_sent += M.Length;
                stats.message_data_sent += M.Length;
                connection.write(M, (uint) M.Length, onMessageWritten, immediate_write);
            }
        }

        public string dumphex(byte[] test)
        {
            return test.Aggregate("", (current, t) => current + ((t < 16 ? "0" : "") + t.ToString("x") + " "));
        }
    }
}