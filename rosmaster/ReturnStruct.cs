using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace rosmaster
{
    public class ReturnStruct
    {

        public int statusCode;
        public String statusMessage;
        public XmlRpc_Wrapper.XmlRpcValue value;

        public ReturnStruct(int _statusCode = 1, String _statusMessage = "", XmlRpc_Wrapper.XmlRpcValue _value = null)
        {
            statusCode = _statusCode;
            statusMessage = _statusMessage;
            value = _value;
        }
    }
}
