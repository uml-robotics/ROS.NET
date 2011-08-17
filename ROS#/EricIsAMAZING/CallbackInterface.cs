#region USINGZ

using System;
using Messages;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace EricIsAMAZING
{
    public class Callback<T> : CallbackInterface where T : IRosMessage, new()
    {
        public new CallbackDelegate<T> func;
        public T message;

        public Callback(T m)
        {
            message = m;
        }

        public Callback(CallbackDelegate<T> f) : base((ci) => f((T) ci))
        {
            func = f;
        }

        internal override CallResult Call()
        {
            return Call(message);
        }

        internal CallResult Call(IRosMessage m)
        {
            func(m as T);
            return CallResult.Success;
        }
    }

    public class CallbackInterface
    {
        #region CallResult enum

        public enum CallResult
        {
            Success,
            TryAgain,
            Invalid
        }

        #endregion

        public CallbackDelegate<IRosMessage> func;

        public CallbackInterface()
        {
        }

        public CallbackInterface(CallbackDelegate<IRosMessage> f)
        {
            func = f;
        }

        internal virtual CallResult Call()
        {
            return CallResult.Invalid;
        }

        internal virtual bool ready()
        {
            return true;
        }
    }
}