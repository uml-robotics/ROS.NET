#region USINGZ

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
            IRosMessage irm = new MReq();
            IRosMessage irs = new MRes();
            md5sum = MD5.Sum(MD5.Sum(irm.msgtype) + MD5.Sum(irs.msgtype));
        }
    }
}