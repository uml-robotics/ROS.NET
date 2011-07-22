#region USINGZ

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

        public Callback()
        {
        }

        public Callback(CallbackDelegate<T> f) : base((ci)=>f((T)ci))
        {
            func = f;
        }
    }

    public class CallbackInterface
    {
        public CallbackDelegate<IRosMessage> func;

        public CallbackInterface()
        {
        }

        public CallbackInterface(CallbackDelegate<IRosMessage> f)
        {
            func = f;
        }

        #region CallResult enum

        public enum CallResult
        {
            Success,
            TryAgain,
            Invalid
        }

        #endregion

        internal virtual CallResult Call()
        {
            return CallResult.Success;
        }

        internal virtual bool ready()
        {
            return true;
        }
    }
}