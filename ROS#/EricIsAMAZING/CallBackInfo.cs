using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EricIsAMAZING
{

    public class CallbackInfo : CallbackQueue
    {}
    public class CallbackQueue
    {
        public Queue<CallbackInterface> Callback_Queue = new Queue<CallbackInterface>();
        public Dictionary<UInt64, List<CallbackInterface>> Callback_ByOwnerID = new Dictionary<ulong, List<CallbackInterface>>();
        object chillthefuckout = new object();
        public void AddCallback(CallbackInterface cb, ulong owner_id)
        {
            lock (chillthefuckout)
            {
                Callback_Queue.Enqueue(cb);
                if (!Callback_ByOwnerID.ContainsKey(owner_id))
                    Callback_ByOwnerID.Add(owner_id, new List<CallbackInterface>(new[] {cb}));
                else if (!Callback_ByOwnerID[owner_id].Contains(cb))
                    Callback_ByOwnerID[owner_id].Add(cb);
            }
        }

        public void removeByID(UInt64 owner_id)
        {
            lock(chillthefuckout)
            {
                List<CallbackInterface> cbis = Callback_ByOwnerID[owner_id];
                Callback_ByOwnerID.Remove(owner_id);
                Callback_Queue = new Queue<CallbackInterface>(Callback_Queue.Except(cbis));
            }
        }
    }
}
