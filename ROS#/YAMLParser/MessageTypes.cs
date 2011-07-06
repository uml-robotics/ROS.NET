#region USINGZ

using System;
using System.Collections.Generic;

#endregion

namespace Messages
{
    public static class TypeHelper
    {
        public static Dictionary<TypeEnum, Type> Types = new Dictionary<TypeEnum, Type> {{TypeEnum.Unknown, null}};
    }

    public enum TypeEnum
    {
        Unknown
    }
}