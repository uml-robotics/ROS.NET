#region USINGZ

using System;
using System.Collections.Generic;

#endregion

namespace Messages
{
    public static class TypeHelper
    {
        public static Dictionary<MsgTypes, TypeInfo> TypeInformation = new Dictionary<MsgTypes, TypeInfo>
                                                                           {{MsgTypes.Unknown, null}};

        public static Type GetType(string name)
        {
            return Type.GetType(name, true, true);
        }

        //public static Dictionary<MsgTypes, Type> Types = new Dictionary<MsgTypes, Type> {{MsgTypes.Unknown, null}};
        //public static Dictionary<MsgTypes, string> MessageDefinitions = new Dictionary<MsgTypes, string> {{MsgTypes.Unknown, "IDFK"}};
        //public static Dictionary<MsgTypes, bool> IsMetaType = new Dictionary<MsgTypes, bool>();
        //public static Dictionary<MsgTypes, Dimensions> MessageDimensions = new Dictionary<MsgTypes, Dimensions>();
    }

    public enum MsgTypes
    {
        Unknown,
        std_msgs__String,
        std_msgs__Header,
        std_msgs__Time
    }
}