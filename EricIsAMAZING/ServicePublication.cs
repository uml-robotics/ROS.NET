using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ros_CSharp
{
    public class ServicePublication<MReq, MRes> : IServicePublication
    {
        internal ServiceCallbackHelper<MReq, MRes> Helper;
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
            this.Helper = helper;
            base.helper = (IServiceCallbackHelper)helper;
            this.callback = callback;
            this.tracked_object = trackedObject;
        }
        
    }

    public class IServicePublication
    {
        internal string name;
        internal bool isDropped;
        internal string md5sum;
        internal string datatype;
        internal string req_datatype;
        internal string res_datatype;
        internal IServiceCallbackHelper helper;
        internal CallbackQueueInterface callback;
        internal object tracked_object;
        internal void drop()
        {
            throw new NotImplementedException();
        }
    }
}
