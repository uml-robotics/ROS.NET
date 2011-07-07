#region USINGZ

using m = Messages;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace EricIsAMAZING
{
    public class Callback<T> : CallbackInterface where T : m.IRosMessage, new()
    {
        public CallbackDelegate<T> func = null;

        public Callback()
        {
        }

        public Callback(CallbackDelegate<T> f)
        {
            func = f;
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