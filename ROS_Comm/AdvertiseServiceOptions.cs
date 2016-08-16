// File: AdvertiseServiceOptions.cs
// Project: ROS_C-Sharp
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 04/28/2015
// Updated: 02/10/2016

#region USINGZ

using System;
using Messages;

#endregion

namespace Ros_CSharp
{
    public class AdvertiseServiceOptions<MReq, MRes> where MReq : IRosMessage, new() where MRes : IRosMessage, new()
    {
        public CallbackQueueInterface callback_queue;
        public string datatype;
        public ServiceCallbackHelper<MReq, MRes> helper;
        public string md5sum;
        public int queue_size;
        public string req_datatype;
        public string res_datatype;
        public string service = "";
        public ServiceFunction<MReq, MRes> srv_func;
        public SrvTypes srvtype;
        public object tracked_object;

        public AdvertiseServiceOptions(string service, ServiceFunction<MReq, MRes> srv_func)
        {
            // TODO: Complete member initialization
            init(service, srv_func);
        }

        public void init(string service, ServiceFunction<MReq, MRes> callback)
        {
            this.service = service;
            srv_func = callback;
            helper = new ServiceCallbackHelper<MReq, MRes>(callback);
            req_datatype = new MReq().msgtype().ToString().Replace("__", "/").Replace("/Request", "__Request");
            res_datatype = new MRes().msgtype().ToString().Replace("__", "/").Replace("/Response", "__Response");
            srvtype = (SrvTypes) Enum.Parse(typeof (SrvTypes), req_datatype.Replace("__Request", "").Replace("/", "__"));
            datatype = srvtype.ToString().Replace("__", "/");
            md5sum = IRosService.generate(srvtype).MD5Sum();
        }
    }
}