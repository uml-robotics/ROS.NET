using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EricIsAMAZING
{
    public class AdvertiseServiceOptions<MReq, MRes> : StuffOptions
    {
        public string service;
        public Func<MReq, MRes> srv_func;

        public AdvertiseServiceOptions(string service, Func<MReq, MRes> srv_func)
        {
            // TODO: Complete member initialization
            this.service = service;
            this.srv_func = srv_func;
        }
    }
}
