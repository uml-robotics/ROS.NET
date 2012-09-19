#region USINGZ

using System.Threading;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;
using System;

#endregion

namespace Ros_CSharp
{
    public class PollManager
    {
        #region Delegates

        public delegate void Poll_Signal();

        #endregion

        private static PollManager _instance;
        public PollSet poll_set;

        public bool shutting_down;
        public object signal_mutex = new object();
        public TcpTransport tcpserver_transport;
        private Thread thread;

        public PollManager()
        {
            poll_set = new PollSet();
        }

        public static PollManager Instance
        {
            get
            {
                if (_instance == null) _instance = new PollManager();
                return _instance;
            }
        }

        public event Poll_Signal poll_signal;

        public void addPollThreadListener(Poll_Signal poll)
        {
            lock (signal_mutex)
            {
                Console.WriteLine("Adding pollthreadlistener " + poll.Method.ToString());
                poll_signal += poll;
                if (poll_signal != null)
                    poll_signal();
            }
        }

        private void signal()
        {
            lock (signal_mutex)
            {
                if (poll_signal != null)
                    poll_signal();
            }
        }

        public void removePollThreadListener(Poll_Signal poll)
        {
            lock (signal_mutex)
            {
                Console.WriteLine("Removing pollthreadlistener " + poll.Method.ToString());
                poll_signal -= poll;
                if (poll_signal != null)
                    poll_signal();
            }
        }

        private void threadFunc()
        {
            while (!shutting_down)
            {
                signal();

                if (shutting_down) return;

                poll_set.update(10);
            }
        }


        public void Start()
        {
            shutting_down = false;
            thread = new Thread(threadFunc);
            thread.IsBackground = true;
            thread.Start();
        }

        public void shutdown()
        {
            shutting_down = true;
            thread.Join();
            poll_signal = null;
        }
    }
}