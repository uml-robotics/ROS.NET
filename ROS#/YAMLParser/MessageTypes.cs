#region USINGZ

using System;
using System.Collections.Generic;

#endregion

namespace Messages
{
    public static class TypeHelper
    {
        public static Dictionary<MsgTypes, Type> Types = new Dictionary<MsgTypes, Type> { { MsgTypes.Unknown, null } };
    }

    public enum MsgTypes
    {
        Unknown
    }
}