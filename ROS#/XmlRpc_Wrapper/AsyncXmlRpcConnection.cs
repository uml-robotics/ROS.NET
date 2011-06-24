using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XmlRpc_Wrapper
{
    public abstract class AsyncXmlRpcConnection
    {
        public virtual void AddToDispatch(XmlRpcDispatch disp)
        {
        }
        public virtual void RemoveFromDispatch(XmlRpcDispatch disp)
        {
        }
        public virtual bool Check()
        {
            return false;
        }
    }
}
