#region USINGZ

using System;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace EricIsAMAZING
{
    public abstract class Spinner : IDisposable
    {
        #region IDisposable Members

        public virtual void Dispose()
        {
            throw new NotImplementedException();
        }

        #endregion

        public virtual void spin(CallbackQueue queue = null)
        {
        }
    }

    public class SingleThreadSpinner : Spinner
    {
        public override void spin(CallbackQueue callbackInterface = null)
        {
            if (callbackInterface == null)
                callbackInterface = ROS.GlobalCallbackQueue;
            NodeHandle spinnerhandle = new NodeHandle();
            while (spinnerhandle.ok)
            {
                callbackInterface.callAvailable(ROS.WallDuration);
            }
        }

        public override void Dispose()
        {
            throw new NotImplementedException();
        }
    }

    public class MultiThreadSpinner : Spinner
    {
        private int thread_count;

        public MultiThreadSpinner(int tc = 0)
        {
        }

        public override void spin(CallbackQueue callbackInterface = null)
        {
        }

        public override void Dispose()
        {
            throw new NotImplementedException();
        }
    }

    public class AsyncSpinner : IDisposable
    {
        public AsyncSpinner(int tc)
        {
        }

        public AsyncSpinner(int tc, CallbackQueueInterface queue)
        {
        }

        #region IDisposable Members

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        #endregion

        public void Start()
        {
        }

        public void Stop()
        {
        }
    }
}