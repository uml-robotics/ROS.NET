// File: TimerManager.cs
// Project: ROS_C-Sharp
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 04/28/2015
// Updated: 02/10/2016

#region USINGZ

using System;
using System.Collections.Generic;
using System.Threading;

#endregion

namespace Ros_CSharp
{
    /// <summary>
    ///     Timer management utility class
    /// </summary>
    public class TimerManager : IDisposable
    {
        /// <summary>
        ///     Holds on to known timer instances
        /// </summary>
        private HashSet<WrappedTimer> heardof = new HashSet<WrappedTimer>();

        /// <summary>
        ///     clean up shop
        /// </summary>
        public void Dispose()
        {
            lock (heardof)
            {
                //be extra super sure they're all dead
                foreach (WrappedTimer t in heardof)
                {
                    t.Dispose();
                }
                heardof.Clear();
            }
        }

        /// <summary>
        ///     Wrap and start timer a with added functionality, and make sure it dies with this TimerManager
        ///     This DOES NOT START IT.
        /// </summary>
        /// <param name="cb">
        ///     The callback of the wrapped timer
        /// </param>
        /// <param name="d">
        ///     The delay it should have
        /// </param>
        /// <param name="p">
        ///     The period it should have
        /// </param>
        public WrappedTimer MakeTimer(TimerCallback cb, int d = Timeout.Infinite, int p = Timeout.Infinite)
        {
            WrappedTimer wt = new WrappedTimer(cb, d, p);
            MakeTimer(wt);
            return wt;
        }

        /// <summary>
        ///     Wrap a timer a with added functionality, and make sure it dies with this TimerManager
        ///     This DOES NOT START IT.
        /// </summary>
        /// <param name="cb">
        ///     The callback of the wrapped timer
        /// </param>
        /// <param name="d">
        ///     The delay it should have
        /// </param>
        /// <param name="p">
        ///     The period it should have
        /// </param>
        public WrappedTimer StartTimer(TimerCallback cb, int d = Timeout.Infinite, int p = Timeout.Infinite)
        {
            WrappedTimer wt = MakeTimer(cb, d, p);
            wt.Start();
            return wt;
        }

        /// <summary>
        ///     Add a wrapped timer to the hashset
        /// </summary>
        /// <param name="t">the wrapped timer</param>
        public void MakeTimer(WrappedTimer t)
        {
            lock (heardof)
            {
                if (heardof.Contains(t))
                    throw new Exception("The same timer cannot be tracked twice");
                heardof.Add(t);
            }
        }

        /// <summary>
        ///     Stop tracking a timer, and kill it
        /// </summary>
        /// <param name="t">The timer to forget and kill</param>
        public void RemoveTimer(ref WrappedTimer t)
        {
            lock (heardof)
            {
                if (heardof.Contains(t))
                    heardof.Remove(t);
            }
            t.Dispose();
            t = null;
        }
    }

    /// <summary>
    ///     Wrap the System.Threading.Timer with useful functions and state information
    /// </summary>
    public class WrappedTimer : IDisposable
    {
        //variable backing for properties
        private int _delay = Timeout.Infinite;
        private int _period = Timeout.Infinite;
        private bool _running;

        private TimerCallback cb;

        /// <summary>
        ///     Tastes like it smells
        /// </summary>
        internal Timer timer;

        /// <summary>
        ///     Instantiate the wrapper
        /// </summary>
        /// <param name="t">A timer</param>
        /// <param name="d">Its delay</param>
        /// <param name="p">Its period</param>
        public WrappedTimer(TimerCallback cb, int d, int p)
        {
            //add a callback between the caller and the timer, so non-periodic timers state becomes false right before the one time their callback happens
            //(If a timer's period is Timeout.Infinite, it will fire once delay ms after Start is called. If start is recalled before then, nothing changes.
            //      To reset the time to the next pending callback before the callback happens, use Restart)
            this.cb = o =>
                          {
                              if (_period == Timeout.Infinite)
                                  _running = false;
                              cb(o);
                          };
            timer = new Timer(this.cb, null, Timeout.Infinite, Timeout.Infinite);
            _delay = d;
            _period = p;
        }

        /// <summary>
        ///     This timer's delay
        /// </summary>
        public int delay
        {
            get { return _delay; }
            set
            {
                if (timer == null)
                    throw new Exception("This wrapper is no longer with the living");
                if (_delay != value && running)
                    timer.Change(value, _period);
                _delay = value;
            }
        }

        /// <summary>
        ///     This timer's period
        /// </summary>
        public int period
        {
            get { return _period; }
            set
            {
                if (timer == null)
                    throw new Exception("This wrapper is no longer with the living");
                if (_period != value && running)
                    timer.Change(_delay, value);
                _period = value;
            }
        }

        /// <summary>
        ///     Is it running
        /// </summary>
        public bool running
        {
            get { return _running; }
            set
            {
                if (timer == null)
                    throw new Exception("This wrapper is no longer with the living");
                if (value && !_running) Start();
                if (!value && _running) Stop();
            }
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (timer == null) return;
            WaitHandle wh = new AutoResetEvent(false);
            timer.Dispose(wh);
            timer = null;
        }


        /// <summary>
        ///     Starts the timer with this wrapper's set delay and period.
        /// </summary>
        public void Start()
        {
            if (timer == null)
                throw new Exception("This wrapper is no longer with the living");
            if (running) return;
            try
            {
                timer.Change(_delay, _period);
                _running = true;
            }
            catch (Exception ex)
            {
                EDB.WriteLine("Error starting timer: " + ex);
            }
        }

        /// <summary>
        ///     Sets this timers delay and period, and immediately starts it
        /// </summary>
        /// <param name="d"></param>
        /// <param name="p"></param>
        public void Start(int d, int p)
        {
            if (timer == null)
                throw new Exception("This wrapper is no longer with the living");
            _delay = d;
            _period = p;
            try
            {
                timer.Change(_delay, _period);
                _running = d != Timeout.Infinite && p != Timeout.Infinite;
            }
            catch (Exception ex)
            {
                EDB.WriteLine("Error starting timer: " + ex);
            }
        }

        /// <summary>
        ///     Stops then Resets the timer, causing any time spent waiting for the next callback to be reset
        /// </summary>
        public void Restart()
        {
            Stop();
            Start();
        }

        /// <summary>
        ///     Stops the timer from firing, while remembering its last set state and period
        /// </summary>
        public void Stop()
        {
            if (timer == null)
                throw new Exception("This wrapper is no longer with the living");
            if (!running) return;
            try
            {
                timer.Change(Timeout.Infinite, Timeout.Infinite);
                _running = false;
            }
            catch (Exception ex)
            {
                EDB.WriteLine("Error starting timer: " + ex);
            }
        }
    }
}