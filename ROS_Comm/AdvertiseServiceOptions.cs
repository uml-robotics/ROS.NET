#region Using

using System;
using Messages;

#endregion

namespace Ros_CSharp
{
    public class AdvertiseServiceOptions<MReq, MRes> where MReq : Messages.IRosMessage, new() where MRes : Messages.IRosMessage, new()
    {
        public CallbackQueueInterface callback_queue;
        public int queue_size;
        public string service = "";
        public ServiceFunction<MReq, MRes> srv_func;
        public string md5sum;
        public string datatype;
        public string req_datatype;
        public string res_datatype;
        public SrvTypes srvtype;
        public ServiceCallbackHelper<MReq,MRes> helper;
        public object tracked_object;
        public AdvertiseServiceOptions(string service, ServiceFunction<MReq, MRes> srv_func) 
        {
            // TODO: Complete member initialization
            init(service, srv_func);
        }
        public void init(string service, ServiceFunction<MReq, MRes> callback)
        {
            this.service = service;
            this.srv_func = callback;
            helper = new ServiceCallbackHelper<MReq, MRes>(callback);
            this.req_datatype = new MReq().msgtype.ToString().Replace("__", "/").Replace("/Request","__Request");
            this.res_datatype = new MRes().msgtype.ToString().Replace("__", "/").Replace("/Response", "__Response");
            srvtype = (SrvTypes)Enum.Parse(typeof(SrvTypes),this.req_datatype.Replace("__Request", "").Replace("/","__"));
            this.datatype = srvtype.ToString().Replace("__","/");
            md5sum = IRosService.generate(this.srvtype).MD5Sum;
        }
    }
}