using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using XmlRpc_Wrapper;
using m = Messages;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;
using System.Threading;

namespace EricIsAMAZING
{

    public class Spinner : IDisposable
    {
        public virtual void spin(CallbackQueue queue = null)
        {

        }
        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

    public class SingleThreadSpinner : Spinner
    {
        public virtual void spin(CallbackQueue queue = null)
        {
            if (queue == null)
                queue = ROS.GlobalCalbackQueue;
            throw new NotImplementedException();
            NodeHandle nh = new NodeHandle();
            while (nh.ok)
            {
                queue.callAvailable(ROS.WallDuration);
            }
        }
        public void Dispose()
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

        public virtual void spin(CallbackQueue queue = null)
        {

        }
        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }

    public class AsyncSpinner : IDisposable
    {
        public AsyncSpinner(int tc)
        {
        }
        public AsyncSpinner(int tc, CallbackQueue queue)
        {
        }
        public void Start()
        {
        }
        public void Stop()
        {
        }
        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
