#region USINGZ

using System;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;

#endregion

namespace Ros_CSharp
{
    public class Spinner : IDisposable
    {
        #region IDisposable Members

        public virtual void Dispose()
        {
            throw new NotImplementedException();
        }

        #endregion

        public virtual void spin()            
        {
            spin(null);
        
        }
        public virtual void spin(CallbackQueue queue)
        {
        }
}
    public class SingleThreadSpinner : Spinner
    {
        public override void spin()
        {
            this.spin(null);
        }
        public override void spin(CallbackQueue callbackInterface)
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

        public MultiThreadSpinner(int tc)
        {
            thread_count = tc;
        }
        public MultiThreadSpinner()
            : this(0)
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