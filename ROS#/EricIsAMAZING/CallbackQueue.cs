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

    public class CallbackQueue : IDisposable
    {
        bool enabled = false;
        public delegate void notification(bool en);
        public event notification NotifyAll;
        internal void notify_all()
        {
            if (NotifyAll != null)
                NotifyAll(enabled);
        }
        public Queue<CallbackInterface> Callback_Queue = new Queue<CallbackInterface>();
        public Dictionary<UInt64, List<CallbackInterface>> Callback_ByOwnerID = new Dictionary<ulong, List<CallbackInterface>>();
        object chillthefuckout = new object();
        public void AddCallback(CallbackInterface cb, ulong owner_id)
        {
            lock (chillthefuckout)
            {
                Callback_Queue.Enqueue(cb);
                if (!Callback_ByOwnerID.ContainsKey(owner_id))
                    Callback_ByOwnerID.Add(owner_id, new List<CallbackInterface>(new[] { cb }));
                else if (!Callback_ByOwnerID[owner_id].Contains(cb))
                    Callback_ByOwnerID[owner_id].Add(cb);
            }
        }

        public void removeByID(UInt64 owner_id)
        {
            lock (chillthefuckout)
            {
                List<CallbackInterface> cbis = Callback_ByOwnerID[owner_id];
                Callback_ByOwnerID.Remove(owner_id);
                Callback_Queue = new Queue<CallbackInterface>(Callback_Queue.Except(cbis));
            }
        }

        public void Enable()
        {
            lock (chillthefuckout)
            {
                enabled = true;
                notify_all();
            }
        }

        public void Disable()
        {
            lock (chillthefuckout)
            {
                enabled = false;
                notify_all();
            }
        }

        public void Dispose()
        {
            lock (chillthefuckout)
            {
                Disable();
            }
        }
        
        public void Clear()
        {
            lock (chillthefuckout)
            {
                Callback_ByOwnerID.Clear();
                Callback_Queue.Clear();
            }
        }

        public bool IsEmpty
        {
            get
            {
                lock (chillthefuckout)
                {
                    return Callback_Queue.Count == 0;
                }
            }
        }

        public bool IsEnabled { get { return enabled; } }

        public void callAvailable(double duration)
        {

        }
    }

    public enum CallOneResult
    {
        Called,TryAgain,Disabled,Empty
    }
}
