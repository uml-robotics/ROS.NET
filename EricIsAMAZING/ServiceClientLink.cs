#region Using

using System;
using System.Collections;
using Messages;

#endregion

namespace Ros_CSharp
{
    public class IServiceClientLink
    {
        public Connection connection;
        public IServicePublication parent;
        public bool persistent;
        

        public IServiceClientLink()
        {
        }

        public static object create(string request, string response)
        {
            Type gen = Type.GetType("ServiceServerLink").MakeGenericType(ROS.GetDataType(request),
                                                                         ROS.GetDataType(response));
            return gen.GetConstructor(null).Invoke(null);
        }

        public bool initialize(Connection conn)
        {
            connection = conn;
            connection.DroppedEvent += onConnectionDropped;
            return true;
        }

        public bool handleHeader(Header header)
        {
            string md5sum, service, client_callerid;
            if (!header.Values.Contains("md5sum") || !header.Values.Contains("service") || !header.Values.Contains("callerid"))
            {
                string bbq = "Bogus tcpros header. did not have required elements: md5sum, service, callerid";
                ROS.Error(bbq);
                connection.sendHeaderError(ref bbq);
                return false;
            }
            md5sum = (string)header.Values["md5sum"];
            service = (string)header.Values["service"];
            client_callerid = (string)header.Values["client_callerid"];

            if (header.Values.Contains("persistent") && (header.Values["persistent"] == "1" || header.Values["persistent"] == "true"))
                persistent = true;

            ROS.Debug("Service client [{0}] wants service [{1}] with md5sum [{2}]", client_callerid, service, md5sum);
            IServicePublication isp = ServiceManager.Instance.lookupServicePublication(service);
            if (isp == null)
            {
                string bbq = string.Format("received a tcpros connection for a nonexistent service [{0}]", service);
                ROS.Error(bbq);
                connection.sendHeaderError(ref bbq);
                return false;
            }

            if (isp.md5sum != md5sum && md5sum != "*" && isp.md5sum != "*")
            {
                string bbq = "client wants service " + service + " to have md5sum " + md5sum + " but it has " + isp.md5sum + ". Dropping connection";
                ROS.Error(bbq);
                connection.sendHeaderError(ref bbq);
                return false;
            }

            if (isp.isDropped)
            {
                string bbq = "received a tcpros connection for a nonexistent service [" + service + "]";
                ROS.Error(bbq);
                connection.sendHeaderError(ref bbq);
                return false;
            }

            parent = isp;
            IDictionary m = new Hashtable();
            m["request_type"] = isp.req_datatype;
            m["response_type"] = isp.res_datatype;
            m["type"] = isp.datatype;
            m["md5sum"] = isp.md5sum;
            m["callerid"] = this_node.Name;

            connection.writeHeader(m, onHeaderWritten);

            isp.addServiceClientLink(this);
            return true;
        }

        public virtual void processResponse(string error, bool success)
        {
            Messages.std_msgs.String msg = new Messages.std_msgs.String(error);
            msg.Serialize();
            byte[] buf = new byte[msg.Serialized.Length + 1];
            buf[0] = (byte)(success ? 0x01 : 0x00);
            msg.Serialized.CopyTo(buf, 1);
            connection.write(buf, (uint)buf.Length, onResponseWritten, true);
        }
        public virtual void processResponse(IRosMessage msg, bool success)
        {
            msg.Serialize();
            byte[] buf = new byte[msg.Serialized.Length + 1];
            buf[0] = (byte)(success ? 0x01 : 0x00);
            msg.Serialized.CopyTo(buf, 1);
            connection.write(buf, (uint)buf.Length, onResponseWritten, true);
        }

        public virtual void drop()
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
                parent.removeServiceClientLink(this);
            }
        }


        public virtual void onRequestLength(Connection conn, ref byte[] buffer, uint size, bool success)
        {
            if (!success) return;

            if (conn != connection || size != 4)
                throw new Exception("Invalid request length read");

            uint len = BitConverter.ToUInt32(buffer,0);
            if (len > 10000000000)
            {
                ROS.Error("A message over a gigabyte was predicted... stop... being... bad.");
                connection.drop(Connection.DropReason.Destructing);
                return;
            }
            connection.read(len, onRequest);
        }

        public virtual void onRequest(Connection conn, ref byte[] buffer, uint size, bool success)
        {
            if (!success) return;
            if (conn != connection)
                throw new Exception("WRONG CONNECTION!");

            if (parent != null)
                lock (parent)
                {
                    parent.processRequest(ref buffer, size, this);
                }
        }

        public virtual void onHeaderWritten(Connection conn)
        {
            connection.read(4, onRequestLength);
        }

        public virtual void onResponseWritten(Connection conn)
        {
            if (conn != connection)
                throw new Exception("WRONG CONNECTION!");

            if (persistent)
                connection.read(4, onRequestLength);
            else
                connection.drop(Connection.DropReason.Destructing);
        }
    }

    /*public class ServiceClientLink<MReq, MRes> : IServiceClientLink
        where MReq : IRosMessage, new()
        where MRes : IRosMessage, new()
    {
        public new ServicePublication<MReq, MRes> parent;

        public override void processResponse(IRosMessage msg)
        {
            connection.write(msg.Serialized, (uint)msg.Serialized.Length, onResponseWritten);
        }
        public override void drop()
        {
            throw new NotImplementedException();
        }


        internal bool call<MReq, MRes>(MReq request, ref MRes response)
        {
            throw new NotImplementedException();
            return true;
        }
    }*/
}