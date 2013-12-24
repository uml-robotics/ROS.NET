#region Using

using System;
using System.Collections.Generic;

#endregion

namespace FauxMessages
{
    public enum MsgTypes
    {
        Unknown,
        std_msgs__String,
        std_msgs__Header,
        std_msgs__Time,
        std_msgs__Duration,
        std_msgs__Byte,
        std_msgs__UInt8,
        std_msgs__Int8,
        std_msgs__Char
    }

    public enum SrvTypes
    {
        Unknown
    }
}