#region License Stuff

// Eric McCann - 2011
// University of Massachusetts Lowell
//  
//  
// The DREAMController is intellectual property of the University of Massachusetts lowell, and is patent pending.
//  
// Your rights to distribute, videotape, etc. any works that make use of the DREAMController are entirely contingent on the specific terms of your licensing agreement.
// 
// Feel free to edit any of the supplied samples, or reuse the code in other projects that make use of the DREAMController. They are provided as a resource.
//  
//  
// For license-related questions, contact:
//  	Kerry Lee Andken
//  	kerrylee_andken@uml.edu
//  
// For technical questions, contact:
//  	Eric McCann
//  	emccann@cs.uml.edu
//  	
//  	

#endregion

#region USINGZ

using System;
using System.Collections.Generic;
using System.Threading;

#endregion

namespace Ros_CSharp
{
    /// <summary>
    ///   The timer manager.
    /// </summary>
    public class TimerManager
    {
        /// <summary>
        ///   The heardof.
        /// </summary>
        private Dictionary<Timer, TimerStuff> heardof = new Dictionary<Timer, TimerStuff>();

        /// <summary>
        ///   The make timer.
        /// </summary>
        /// <param name = "t">
        ///   The t.
        /// </param>
        /// <param name = "cb">
        ///   The cb.
        /// </param>
        /// <param name = "d">
        ///   The d.
        /// </param>
        /// <param name = "p">
        ///   The p.
        /// </param>
        public void MakeTimer(ref Timer t, TimerCallback cb, int d, int p)
        {
            MakeTimer(ref t, cb, null, d, p);
        }

        public void RemoveTimer(ref Timer t)
        {
            if (t == null) return;
            StopTimer(ref t);
            if (heardof.ContainsKey(t))
            {
                heardof.Remove(t);
            }
            t = null;
        }

        /// <summary>
        ///   The make timer.
        /// </summary>
        /// <param name = "t">
        ///   The t.
        /// </param>
        /// <param name = "cb">
        ///   The cb.
        /// </param>
        /// <param name = "state">
        ///   The state.
        /// </param>
        /// <param name = "d">
        ///   The d.
        /// </param>
        /// <param name = "p">
        ///   The p.
        /// </param>
        public void MakeTimer(ref Timer t, TimerCallback cb, object state, int d, int p)
        {
            t = new Timer(cb, state, Timeout.Infinite, Timeout.Infinite);
            heardof.Add(t, new TimerStuff(cb, d, p));
        }

        /// <summary>
        ///   The start timer.
        /// </summary>
        /// <param name = "t">
        ///   The t.
        /// </param>
        /// <param name = "cb">
        ///   The cb.
        /// </param>
        /// <param name = "d">
        ///   The d.
        /// </param>
        /// <param name = "p">
        ///   The p.
        /// </param>
        public void StartTimer(ref Timer t, TimerCallback cb, int d, int p)
        {
            MakeTimer(ref t, cb, d, p);
            StartTimer(ref t);
        }

        /// <summary>
        ///   The start timer.
        /// </summary>
        /// <param name = "t">
        ///   The t.
        /// </param>
        /// <exception cref = "Exception">
        /// </exception>
        public void StartTimer(ref Timer t)
        {
            if (!heardof.ContainsKey(t)) throw new Exception("MAKE A TIMER FIRST!");
            if (heardof[t].running) return;
            t.Change(heardof[t].delay, heardof[t].period);
            heardof[t].running = true;
        }

        /// <summary>
        ///   The stop timer.
        /// </summary>
        /// <param name = "t">
        ///   The t.
        /// </param>
        /// <exception cref = "Exception">
        /// </exception>
        public void StopTimer(ref Timer t)
        {
            if (!heardof.ContainsKey(t)) throw new Exception("MAKE A TIMER FIRST!");
            if (!heardof[t].running) return;
            t.Change(Timeout.Infinite, Timeout.Infinite);
            heardof[t].running = false;
        }

        /// <summary>
        ///   The is running.
        /// </summary>
        /// <param name = "t">
        ///   The t.
        /// </param>
        /// <returns>
        ///   The is running.
        /// </returns>
        /// <exception cref = "Exception">
        /// </exception>
        public bool IsRunning(ref Timer t)
        {
            if (!heardof.ContainsKey(t)) throw new Exception("MAKE A TIMER FIRST!");
            return heardof[t].running;
        }
    }

    /// <summary>
    ///   The timer stuff.
    /// </summary>
    public class TimerStuff
    {
        /// <summary>
        ///   The callback.
        /// </summary>
        public TimerCallback callback;

        /// <summary>
        ///   The delay.
        /// </summary>
        public int delay;

        /// <summary>
        ///   The period.
        /// </summary>
        public int period;

        /// <summary>
        ///   The running.
        /// </summary>
        public bool running;

        /// <summary>
        ///   Initializes a new instance of the <see cref = "TimerStuff" /> class.
        /// </summary>
        /// <param name = "cb">
        ///   The cb.
        /// </param>
        /// <param name = "d">
        ///   The d.
        /// </param>
        /// <param name = "p">
        ///   The p.
        /// </param>
        public TimerStuff(TimerCallback cb, int d, int p)
        {
            callback = cb;
            delay = d;
            period = p;
        }
    }
}