using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using XmlRpc_Wrapper;
using m = Messages;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;
using System.Threading;

namespace EricIsAMAZING
{
    public class NodeHandle
    {
        List<IPublisher> publishers = new List<IPublisher>();
        List<IServiceProvider> serviceproviders = new List<IServiceProvider>();
        List<ISubscriber> subscribers = new List<ISubscriber>();
        List<IServiceClient> serviceclients = new List<IServiceClient>();
        public CallbackQueue callbackQueue
        {
            get
            {
                if (_callbackQueue == null) return ROS.GlobalCalbackQueue;
                return _callbackQueue;
            }
            set { _callbackQueue = value; }
        }
        public static bool ok { get { return ROS.ok && _ok; } }
        private static bool _ok;
        private CallbackQueue _callbackQueue;
        public string Namespace, UnresolvedNamespace;
        public string resolveName(string name, bool remap = true)
        {
            throw new NotImplementedException();
        }

        Publisher<M> advertise<M>(string topic, int q_size, bool l = false)
        {
            return advertise<M>(new AdvertiseOptions(topic, q_size) { latch = l });
        }

        Publisher<M> advertise<M>(string topic, int queue_size, MulticastDelegate connectcallback, MulticastDelegate disconnectcallback, bool l = false)
        {
            return advertise<M>(new AdvertiseOptions(topic, queue_size, connectcallback, disconnectcallback) { latch = l });
        }

        Publisher<M> advertise<M>(AdvertiseOptions ops)
        {
 	        throw new NotImplementedException();
        }
        Subscriber<T,M> subscribe<T,M>(string topic, int queue_size, Action<T> cb)
        {
            return subscribe<T,M>(new SubscribeOptions<T>(topic, queue_size, cb));
        }

        Subscriber<T,M> subscribe<T,M>(SubscribeOptions<T> subscribeOptions)
        {
 	        throw new NotImplementedException();
        }

        public NodeHandle(string ns, IDictionary remappings)
        {
        }

        public NodeHandle(NodeHandle rhs)
        {
        }

        public NodeHandle(NodeHandle parent, string ns)
        {
        }

        public NodeHandle(NodeHandle parent, string ns, IDictionary remappings)
        {
        }

        public ServiceServer<T,MReq,MRes> advertiseService<T,MReq,MRes>(string service, Func<MReq,MRes> srv_func)
        {
            return advertiseService<T,MReq,MRes>(new AdvertiseServiceOptions<MReq, MRes>(service, srv_func));
        }

        public ServiceServer<T,MReq,MRes> advertiseService<T,MReq,MRes>(AdvertiseServiceOptions<MReq,MRes> ops)
        {
            throw new NotImplementedException();
        }

        public ServiceClient<MReq, MRes> serviceClient<MReq,MRes>(string service_name, bool persistent = false, IDictionary header_values = null)
        {
            return serviceClient<MReq, MRes>(new ServiceClientOptions(service_name, persistent, header_values));
        }

        public ServiceClient<MReq, MRes> serviceClient<MReq,MRes>(ServiceClientOptions ops)
        {
            throw new NotImplementedException();
        }
    }
}
