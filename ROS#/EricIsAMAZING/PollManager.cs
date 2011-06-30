using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XmlRpc_Wrapper;
using m = Messages;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace EricIsAMAZING
{
    public class PollManager
    {
        public PollManager()
        {
            poll_set = new PollSet();
        }

        public TcpTransport tcpserver_transport;
        public bool shutting_down;
        private Thread thread;
        public PollSet poll_set;
        public delegate void Poll_Signal();
        public event Poll_Signal poll_signal;
        public object signal_mutex = new object();
        public void addPollThreadListener(Poll_Signal poll)
        {
            lock (signal_mutex)
            {
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

                poll_set.update(100);
            }
        }


        private static PollManager _instance;
        public static PollManager Instance()
        {
            if (_instance == null) _instance = new PollManager();
            return _instance;
        }
        public void Start()
        {
            Console.WriteLine("POLEMANAGER STARTED! YOUR MOM MANAGES POLE!");
            shutting_down = false;
            thread = new Thread(new ThreadStart(threadFunc));
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
