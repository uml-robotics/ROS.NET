// File: ServiceServerLink.cs
// Project: ROS_C-Sharp
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 11/06/2013
// Updated: 07/23/2014

#region USINGZ

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Messages;
using String = Messages.std_msgs.String;

#endregion

namespace Ros_CSharp
{
    public class IServiceServerLink
    {
        public bool IsValid;
        public string RequestMd5Sum;
        public MsgTypes RequestType;
        public string ResponseMd5Sum;
        public MsgTypes ResponseType;

        private Queue<CallInfo> call_queue = new Queue<CallInfo>();
        private object call_queue_mutex = new object();
        public Connection connection;
        private CallInfo current_call;
        public bool header_read;
        private IDictionary header_values;
        public bool header_written;
        public string name;
        public bool persistent;

        public IServiceServerLink(string name, bool persistent, string requestMd5Sum, string responseMd5Sum,
            IDictionary header_values)
        {
            // TODO: Complete member initialization
            this.name = name;
            this.persistent = persistent;
            RequestMd5Sum = requestMd5Sum;
            ResponseMd5Sum = responseMd5Sum;
            this.header_values = header_values;
        }

        public void initialize<MSrv>()
            where MSrv : IRosService, new()
        {
            MSrv srv = new MSrv();
            RequestMd5Sum = srv.RequestMessage.MD5Sum;
            ResponseMd5Sum = srv.ResponseMessage.MD5Sum;
            RequestType = srv.RequestMessage.msgtype;
            ResponseType = srv.ResponseMessage.msgtype;
        }

        public void initialize<MReq, MRes>() where MReq : IRosMessage, new() where MRes : IRosMessage, new()
        {
            MReq req = new MReq();
            MRes res = new MRes();
            RequestMd5Sum = req.MD5Sum;
            ResponseMd5Sum = res.MD5Sum;
            RequestType = req.msgtype;
            ResponseType = res.msgtype;
        }

        internal void initialize(Connection connection)
        {
            this.connection = connection;
            connection.DroppedEvent += onConnectionDropped;
            connection.setHeaderReceivedCallback(onHeaderReceived);

            IDictionary dict = new Hashtable();
            dict["service"] = name;
            dict["md5sum"] = IRosService.generate((SrvTypes) Enum.Parse(typeof (SrvTypes), RequestType.ToString().Replace("__Request", "").Replace("__Response", ""))).MD5Sum;
            dict["callerid"] = this_node.Name;
            dict["persistent"] = persistent ? "1" : "0";
            if (header_values != null)
                foreach (object o in header_values.Keys)
                    dict[o] = header_values[o];
            connection.writeHeader(dict, onHeaderWritten);
        }

        private void onConnectionDropped(Connection connection, Connection.DropReason dr)
        {
            if (connection != this.connection) throw new Exception("WRONG CONNECTION ZOMG!");

            ROS.Debug("Service client from [{0}] for [{1}] dropped", connection.RemoteString, name);

            clearCalls();

            ServiceManager.Instance.removeServiceServerLink(this);
        }

        private bool onHeaderReceived(Connection conn, Header header)
        {
            string md5sum;
            if (header.Values.Contains("md5sum"))
                md5sum = (string) header.Values["md5sum"];
            else
            {
                ROS.Error("TCPROS header from service server did not have required element: md5sum");
                return false;
            }
            //TODO check md5sum

            bool empty = false;
            lock (call_queue_mutex)
            {
                empty = call_queue.Count == 0;
                if (empty)
                    header_read = true;
            }

            if (!empty)
            {
                processNextCall();
                header_read = true;
            }

            return true;
        }

        private void callFinished()
        {
            CallInfo saved_call;
            IServiceServerLink self;
            lock (call_queue_mutex)
            {
                lock (current_call.finished_mutex)
                {
                    current_call.finished = true;
                    current_call.notify_all();

                    saved_call = current_call;
                    current_call = null;

                    self = this;
                }
            }
            saved_call = new CallInfo();
            processNextCall();
        }

        private void processNextCall()
        {
            bool empty = false;
            lock (call_queue_mutex)
            {
                if (current_call != null)
                    return;
                if (call_queue.Count > 0)
                {
                    current_call = call_queue.Dequeue();
                }
                else
                    empty = true;
            }
            if (empty)
            {
                if (persistent)
                    connection.drop(Connection.DropReason.Destructing);
            }
            else
            {
                IRosMessage request;
                lock (call_queue_mutex)
                {
                    request = current_call.req;
                }
                request.Serialize();
                connection.write(request.Serialized, (uint) request.Serialized.Length, onRequestWritten);
            }
        }

        private void clearCalls()
        {
            CallInfo local_current;
            lock (call_queue_mutex)
                local_current = current_call;
            if (local_current != null)
                cancelCall(local_current);
            lock (call_queue_mutex)
            {
                while (call_queue.Count > 0)
                    cancelCall(call_queue.Dequeue());
            }
        }

        private void cancelCall(CallInfo info)
        {
            CallInfo local = info;
            lock (local.finished_mutex)
            {
                local.finished = true;
                local.notify_all();
            }
            //yield
        }

        private void onHeaderWritten(Connection conn)
        {
            header_written = true;
        }

        private void onRequestWritten(Connection conn)
        {
            connection.read(5, onResponseOkAndLength);
        }

        private void onResponseOkAndLength(Connection conn, ref byte[] buf, uint size, bool success)
        {
            if (conn != connection || size != 5)
                throw new Exception("response or length NOT OK!");
            if (!success) return;
            byte ok = buf[0];
            uint len = BitConverter.ToUInt32(buf, 1);
            if (len > 1000000000)
            {
                ROS.Error("GIGABYTE IS TOO BIIIIG");
                connection.drop(Connection.DropReason.Destructing);
                return;
            }
            lock (call_queue_mutex)
            {
                if (ok != 0)
                    current_call.success = true;
                else
                    current_call.success = false;
            }
            if (len > 0)
                connection.read(len, onResponse);
            else
            {
                byte[] f = new byte[0];
                onResponse(conn, ref f, 0, true);
            }
        }

        private void onResponse(Connection conn, ref byte[] buf, uint size, bool success)
        {
            if (conn != connection) throw new Exception("WRONG CONNECTION");

            if (!success) return;
            lock (call_queue_mutex)
            {
                if (current_call.success)
                {
                    current_call.resp.Serialized = buf;
                }
                else
                    current_call.exception = new String(buf).data;
            }

            callFinished();
        }

        public bool call(IRosService srv)
        {
            return call(srv.RequestMessage, ref srv.ResponseMessage);
        }

        public bool call(IRosMessage req, ref IRosMessage resp)
        {
            CallInfo info = new CallInfo {req = req, resp = resp, success = false, finished = false, call_finished = false, caller_thread_id = ROS.getPID()};

            bool immediate = false;
            lock (call_queue_mutex)
            {
                if (connection.dropped)
                {
                    info.call_finished = true;
                    return false;
                }
                if (call_queue.Count == 0 && header_written && header_read)
                    immediate = true;
                call_queue.Enqueue(info);
            }

            if (immediate)
                processNextCall();

            while (!info.finished)
            {
                info.finished_condition.WaitOne();
            }

            lock (info.finished_mutex)
            {
                info.call_finished = true;
            }

            resp = resp.Deserialize(resp.Serialized);

            if (info.exception.Length > 0)
            {
                ROS.Error("Service call failes: service [{0}] responded with an error: {1}", name, info.exception);
            }
            return info.success;
        }
    }

    public class ServiceServerLink<MReq, MRes> : IServiceServerLink
        where MReq : IRosMessage, new()
        where MRes : IRosMessage, new()
    {
        public ServiceServerLink(string name, bool persistent, string requestMd5Sum, string responseMd5Sum,
            IDictionary header_values)
            : base(name, persistent, requestMd5Sum, responseMd5Sum, header_values)
        {
            initialize<MReq, MRes>();
        }

        public bool call(MReq req, ref MRes resp)
        {
            IRosMessage iresp = resp;
            bool r = call(req, ref iresp);
            if (iresp != null)
                resp = (MRes)resp.Deserialize(iresp.Serialized);
            else
                resp = null; //std_servs.Empty, I hope?
            return r;
        }
    }

    public class ServiceServerLink<MSrv> : IServiceServerLink
        where MSrv : IRosService, new()
    {
        public ServiceServerLink(string name, bool persistent, string requestMd5Sum, string responseMd5Sum,
            IDictionary header_values)
            : base(name, persistent, requestMd5Sum, responseMd5Sum, header_values)
        {
            initialize<MSrv>();
        }

        public bool call(MSrv srv)
        {
            IRosMessage iresp = srv.ResponseMessage;
            bool r = call(srv.RequestMessage, ref iresp);
            if (iresp != null)
                srv.ResponseMessage = srv.ResponseMessage.Deserialize(iresp.Serialized);
            else
                srv.ResponseMessage = null; //std_servs.Empty, I hope?
            return r;
        }
    }

    internal class CallInfo
    {
        internal bool call_finished;
        internal UInt64 caller_thread_id;
        internal string exception = "";
        internal bool finished;
        internal Semaphore finished_condition = new Semaphore(0, int.MaxValue);
        internal object finished_mutex = new object();
        internal IRosMessage req, resp;
        internal bool success;

        internal void notify_all()
        {
            finished_condition.Release();
        }
    }
}