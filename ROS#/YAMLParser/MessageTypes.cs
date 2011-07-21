#region USINGZ

using System;
using System.Collections.Generic;

#endregion

namespace Messages
{
    public static class TypeHelper
    {
        public static Dictionary<MsgTypes, Type> Types = new Dictionary<MsgTypes, Type> {{MsgTypes.Unknown, null}};
        public static Dictionary<MsgTypes, string> MessageDefinitions = new Dictionary<MsgTypes, string> {{MsgTypes.Unknown, "IDFK"}};
        public static Dictionary<MsgTypes, bool> IsMetaType = new Dictionary<MsgTypes, bool>();
    }

    public enum MsgTypes
    {
        Unknown
    }
}