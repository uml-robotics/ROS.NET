// File: TimerManager.cs
// Project: ROS_C-Sharp
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 11/06/2013
// Updated: 07/23/2014

#region USINGZ

using System;
using System.Collections.Generic;
using System.Threading;

#endregion

namespace Ros_CSharp
{
    /// <summary>
    ///     The timer manager.
    /// </summary>
    public class TimerManager : IDisposable
    {
        /// <summary>
        ///     The heardof.
        /// </summary>
        private Dictionary<Timer, TimerStuff> heardof = new Dictionary<Timer, TimerStuff>();

        public void Dispose()
        {
            IList<Timer> ts = new List<Timer>(heardof.Keys);
            for (int i = 0; i < ts.Count; i++)
            {
                Timer x = ts[i];
                RemoveTimer(ref x);
            }
            ts.Clear();
            heardof.Clear();
        }

        /// <summary>
        ///     The make timer.
        /// </summary>
        /// <param name="t">
        ///     The t.
        /// </param>
        /// <param name="cb">
        ///     The cb.
        /// </param>
        /// <param name="d">
        ///     The d.
        /// </param>
        /// <param name="p">
        ///     The p.
        /// </param>
        public void MakeTimer(ref Timer t, TimerCallback cb, int d, int p)
        {
            MakeTimer(ref t, cb, null, d, p);
        }

        public void RemoveTimer(ref Timer t)
        {
            if (t == null) return;
            //StopTimer(ref t);
            if (heardof.ContainsKey(t))
            {
                heardof.Remove(t);
            }
            WaitHandle wh = new AutoResetEvent(false);
            t.Dispose(wh);
            t = null;
        }

        /// <summary>
        ///     The make timer.
        /// </summary>
        /// <param name="t">
        ///     The t.
        /// </param>
        /// <param name="cb">
        ///     The cb.
        /// </param>
        /// <param name="state">
        ///     The state.
        /// </param>
        /// <param name="d">
        ///     The d.
        /// </param>
        /// <param name="p">
        ///     The p.
        /// </param>
        public void MakeTimer(ref Timer t, TimerCallback cb, object state, int d, int p)
        {
            t = new Timer(cb, state, Timeout.Infinite, Timeout.Infinite);
            heardof.Add(t, new TimerStuff(cb, d, p));
        }

        /// <summary>
        ///     The start timer.
        /// </summary>
        /// <param name="t">
        ///     The t.
        /// </param>
        /// <param name="cb">
        ///     The cb.
        /// </param>
        /// <param name="d">
        ///     The d.
        /// </param>
        /// <param name="p">
        ///     The p.
        /// </param>
        public void StartTimer(ref Timer t, TimerCallback cb, int d, int p)
        {
            MakeTimer(ref t, cb, d, p);
            StartTimer(ref t);
        }

        /// <summary>
        ///     The start timer.
        /// </summary>
        /// <param name="t">
        ///     The t.
        /// </param>
        /// <exception cref="Exception">
        /// </exception>
        public void StartTimer(ref Timer t)
        {
            if (t == null || !heardof.ContainsKey(t)) throw new Exception("MAKE A TIMER FIRST!");
            if (heardof[t].running) return;
            t.Change(heardof[t].delay, heardof[t].period);
            heardof[t].running = true;
        }

        /// <summary>
        ///     The stop timer.
        /// </summary>
        /// <param name="t">
        ///     The t.
        /// </param>
        /// <exception cref="Exception">
        /// </exception>
        public void StopTimer(ref Timer t)
        {
            if (!heardof.ContainsKey(t)) throw new Exception("MAKE A TIMER FIRST!");
            if (!heardof[t].running) return;
            if (t != null)
            {
                try
                {
                    t.Change(Timeout.Infinite, Timeout.Infinite);
                    heardof[t].running = false;
                }
                catch
                {
                }
            }
        }

        /// <summary>
        ///     The is running.
        /// </summary>
        /// <param name="t">
        ///     The t.
        /// </param>
        /// <returns>
        ///     The is running.
        /// </returns>
        /// <exception cref="Exception">
        /// </exception>
        public bool IsRunning(ref Timer t)
        {
            if (t == null) return false;
            if (!heardof.ContainsKey(t)) throw new Exception("MAKE A TIMER FIRST!");
            return heardof[t].running;
        }
    }

    /// <summary>
    ///     The timer stuff.
    /// </summary>
    public class TimerStuff
    {
        /// <summary>
        ///     The callback.
        /// </summary>
        public TimerCallback callback;

        /// <summary>
        ///     The delay.
        /// </summary>
        public int delay;

        /// <summary>
        ///     The period.
        /// </summary>
        public int period;

        /// <summary>
        ///     The running.
        /// </summary>
        public bool running;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TimerStuff" /> class.
        /// </summary>
        /// <param name="cb">
        ///     The cb.
        /// </param>
        /// <param name="d">
        ///     The d.
        /// </param>
        /// <param name="p">
        ///     The p.
        /// </param>
        public TimerStuff(TimerCallback cb, int d, int p)
        {
            callback = cb;
            delay = d;
            period = p;
        }
    }
}