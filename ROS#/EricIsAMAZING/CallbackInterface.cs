using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EricIsAMAZING
{
    public abstract class CallbackInterface
    {
        public enum CallResult
        {
            Success, TryAgain, Invalid
        }

        internal virtual CallResult Call()
        {
            return CallResult.Success;
        }

        internal virtual bool Read()
        {
            return true;
        }
    }
}
