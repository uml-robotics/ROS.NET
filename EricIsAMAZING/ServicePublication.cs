using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Messages;

namespace Ros_CSharp
{
    public class ServicePublication<MReq, MRes> : IServicePublication 
        where MReq : Messages.IRosMessage, new()
        where MRes : Messages.IRosMessage, new()
    {
        public new ServiceCallbackHelper<MReq, MRes> helper;

        //internal ftw?
        public class ServiceCallback : CallbackInterface
        {
            ServicePublication<MReq, MRes> isp;
            byte[] buffer;
            uint num_bytes;
            bool has_tracked_object;
            object tracked_object;
            IServiceClientLink link;

            public ServiceCallback(ServiceCallbackHelper<MReq, MRes> _helper, byte[] buf, uint num_bytes, IServiceClientLink link, bool has_tracked_object, object tracked_object)
                : this(null,_helper, buf, num_bytes, link, has_tracked_object, tracked_object)
            {
            }

            public ServiceCallback(ServicePublication<MReq, MRes> sp, ServiceCallbackHelper<MReq, MRes> _helper, byte[] buf, uint num_bytes, IServiceClientLink link, bool has_tracked_object, object tracked_object)
                
            {
                isp = sp;
                if (isp != null && _helper != null)
                    isp.helper = _helper;
                buffer = buf;
                this.num_bytes = num_bytes;
                this.link = link;
                this.has_tracked_object = has_tracked_object;
                this.tracked_object = tracked_object;
            }

            internal override CallResult Call()
            {
                if (link.connection.dropped)
                {
                    return CallResult.Invalid;
                }

                ServiceCallbackHelperParams<MReq, MRes> parms = new ServiceCallbackHelperParams<MReq, MRes> {
                    request = new MReq().Deserialize(buffer) as MReq, 
                    response = new MRes(),
                    connection_header = link.connection.header.Values
                };

                try
                {
                    bool ok = isp.helper.call(parms);
                    if (ok)
                    {
                        link.processResponse(parms.response, true);
                    }
                    else
                    {
                        IRosMessage res = new MRes();
                        link.processResponse(res, false);
                    }
                }
                catch (Exception e)
                {
                    string woops = "Exception thrown while processing service call: " + e;
                    ROS.Error(woops);
                    link.processResponse(woops, false);
                    return CallResult.Invalid;
                }
                return CallResult.Success;
            }
        }

        public ServicePublication(string name, string md5Sum, string datatype, string reqDatatype, string resDatatype, ServiceCallbackHelper<MReq, MRes> helper, CallbackQueueInterface callback, object trackedObject)
        {
            if (name == null)
                throw new Exception("NULL NAME?!");
            // TODO: Complete member initialization
            this.name = name;
            this.md5sum = md5Sum;
            this.datatype = datatype;
            this.req_datatype = reqDatatype;
            this.res_datatype = resDatatype;
            this.helper = helper;
            this.callback = callback;
            this.tracked_object = trackedObject;

            if (trackedObject != null)
                has_tracked_object = true;

        }

        public override void processRequest(ref byte[] buf, uint num_bytes, IServiceClientLink link)
        {
            CallbackInterface cb = new ServiceCallback(this, helper, buf, num_bytes, link, has_tracked_object, tracked_object);
            callback.addCallback(cb, ROS.getPID());
        }

        internal override void addServiceClientLink(IServiceClientLink iServiceClientLink)
        {
            lock (client_links_mutex)
                client_links.Add(iServiceClientLink);
        }
        internal override void removeServiceClientLink(IServiceClientLink iServiceClientLink)
        {
            lock (client_links_mutex)
                client_links.Remove(iServiceClientLink);
        }
    }

    public class IServicePublication
    {
        internal bool has_tracked_object;
        internal string name;
        internal bool isDropped;
        internal string md5sum;
        internal string datatype;
        internal string req_datatype;
        internal string res_datatype;
        internal IServiceCallbackHelper helper;
        protected object client_links_mutex = new object();
        internal CallbackQueueInterface callback;
        internal List<IServiceClientLink> client_links = new List<IServiceClientLink>();
        internal object tracked_object;
        internal virtual void drop()
        {
            lock (client_links_mutex)
            {
                isDropped = true;
            }
            dropAllConnections();
            callback.removeByID(ROS.getPID());
        }
        private void dropAllConnections()
        {
            List<IServiceClientLink> links;
            lock (client_links_mutex)
            {
                links = new List<IServiceClientLink>(client_links);
                client_links.Clear();
            }

            foreach (IServiceClientLink iscl in links)
            {
                iscl.connection.drop(Connection.DropReason.Destructing);
            }
        }

        internal virtual void addServiceClientLink(IServiceClientLink iServiceClientLink)
        {
            throw new NotImplementedException();
        }

        internal virtual void removeServiceClientLink(IServiceClientLink iServiceClientLink)
        {
            throw new NotImplementedException();
        }

        public virtual void processRequest(ref byte[] buffer, uint size, IServiceClientLink iServiceClientLink)
        {
            throw new NotImplementedException();
        }
    }
}
