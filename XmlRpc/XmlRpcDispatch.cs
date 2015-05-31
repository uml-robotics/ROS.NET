// File: XmlRpcDispatch.cs
// Project: XmlRpc_Wrapper
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

//#define REFDEBUG
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;

#endregion

namespace XmlRpc
{
    public class XmlRpcDispatch //: IDisposable
    {
        #region EventType enum

        [Flags]
        public enum EventType
        {
			NoEvent = 0,
            ReadableEvent = 1,
            WritableEvent = 2,
            Exception = 4
        }

        #endregion
/*
        #region P/Invoke

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcDispatch_Create", CallingConvention = CallingConvention.Cdecl)
        ]
        private static extern IntPtr create();

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcDispatch_Close", CallingConvention = CallingConvention.Cdecl)]
        private static extern void close(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcDispatch_AddSource",
            CallingConvention = CallingConvention.Cdecl)]
        private static extern void addsource(IntPtr target, IntPtr source, uint eventMask);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcDispatch_RemoveSource",
            CallingConvention = CallingConvention.Cdecl)]
        private static extern void removesource(IntPtr target, IntPtr source);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcDispatch_SetSourceEvents",
            CallingConvention = CallingConvention.Cdecl)]
        private static extern void setsourceevents(IntPtr target, IntPtr source, uint eventMask);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcDispatch_Work", CallingConvention = CallingConvention.Cdecl)]
        private static extern void work(IntPtr target, double msTime);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcDispatch_Exit", CallingConvention = CallingConvention.Cdecl)]
        private static extern void exit(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcDispatch_Clear", CallingConvention = CallingConvention.Cdecl)]
        private static extern void clear(IntPtr target);

        #endregion
		*/

		class DispatchRecord
		{
			public XmlRpcSource client;
			public XmlRpcDispatch.EventType mask;
		}

		List<DispatchRecord> sources = new List<DispatchRecord>();

        #region Reference Tracking + unmanaged pointer management

//        private IntPtr __instance;
		/*
        public void Dispose()
        {
            Shutdown();
        }*/

#if USE_BULLSHIT
        private static Dictionary<IntPtr, int> _refs = new Dictionary<IntPtr, int>();
        private static object reflock = new object();
#if REFDEBUG
        private static Thread refdumper;
        private static void dumprefs()
        {
            while (true)
            {
                Dictionary<IntPtr, int> dainbrammage = null;
                lock (reflock)
                {
                    dainbrammage = new Dictionary<IntPtr, int>(_refs);
                }
                Console.WriteLine("REF DUMP");
                foreach (KeyValuePair<IntPtr, int> reff in dainbrammage)
                {
                    Console.WriteLine("\t" + reff.Key + " = " + reff.Value);
                }
                Thread.Sleep(500);
            }
        }
#endif

        [DebuggerStepThrough]
        public static XmlRpcDispatch LookUp(IntPtr ptr)
        {
            if (ptr != IntPtr.Zero)
            {
                AddRef(ptr);
                return new XmlRpcDispatch(ptr);
            }
            return null;
        }


        [DebuggerStepThrough]
        private static void AddRef(IntPtr ptr)
        {
#if REFDEBUG
            if (refdumper == null)
            {
                refdumper = new Thread(dumprefs);
                refdumper.IsBackground = true;
                refdumper.Start();
            }
#endif
            lock (reflock)
            {
                if (!_refs.ContainsKey(ptr))
                {
#if REFDEBUG
                    Console.WriteLine("Adding a new reference to: " + ptr + " (" + 0 + "==> " + 1 + ")");
#endif
                    _refs.Add(ptr, 1);
                }
                else
                {
#if REFDEBUG
                    Console.WriteLine("Adding a new reference to: " + ptr + " (" + _refs[ptr] + "==> " + (_refs[ptr] + 1) + ")");
#endif
                    _refs[ptr]++;
                }
            }
        }

        [DebuggerStepThrough]
        private static void RmRef(ref IntPtr ptr)
        {
            lock (reflock)
            {
                if (_refs.ContainsKey(ptr))
                {
#if REFDEBUG
                    Console.WriteLine("Removing a reference to: " + ptr + " (" + _refs[ptr] + "==> " + (_refs[ptr] - 1) + ")");
#endif
                    _refs[ptr]--;
                    if (_refs[ptr] <= 0)
                    {
#if REFDEBUG
                        Console.WriteLine("KILLING " + ptr + " BECAUSE IT'S A NOT VERY NICE!");
#endif
                        _refs.Remove(ptr);
                        close(ptr);
                        XmlRpcUtil.Free(ptr);
                    }
                }
                ptr = IntPtr.Zero;
            }
        }

        public IntPtr instance
        {
            [DebuggerStepThrough]
            get
            {
                if (__instance == IntPtr.Zero)
                {
                    Console.WriteLine("UH OH MAKING A NEW INSTANCE IN instance.get!");
                    __instance = create();
                    AddRef(__instance);
                }
                return __instance;
            }
            [DebuggerStepThrough]
            set
            {
                if (__instance != IntPtr.Zero)
                    RmRef(ref __instance);
                if (value != IntPtr.Zero)
                    AddRef(value);
                __instance = value;
            }
        }

#endif //refcounted bullshit
		/*
        public void Shutdown()
        {
            if (Shutdown(__instance)) Dispose();
        }
		
        public static bool Shutdown(IntPtr ptr)
        {
            if (ptr != IntPtr.Zero)
            {
                RmRef(ref ptr);
                return (ptr == IntPtr.Zero);
            }
            return true;
        }*/

        #endregion
		/*
        [DebuggerStepThrough]
        public XmlRpcDispatch()
        {
            //instance = create();
        }
		
        [DebuggerStepThrough]
        public XmlRpcDispatch(IntPtr otherref)
        {
            if (otherref != IntPtr.Zero)
                instance = otherref;
        }*/

		public void SegFault()
		{
			// 
		}

		public void AddSource(XmlRpcSource source, XmlRpcDispatch.EventType eventMask)
        {
			sources.Add(new DispatchRecord(){client = source, mask = eventMask});
            //addsource(instance, source.instance, (uint) eventMask);
        }

		public void RemoveSource(XmlRpcSource source)
        {
			foreach(var record in sources)
			{
				if (record.client == source)
				{
					sources.Remove(record);
					break;
				}
			}
            //source.SegFault();
			//this.reRemoveSource(source);
            //removesource(instance, source.instance);
        }

		public void SetSourceEvents(XmlRpcSource source, XmlRpcDispatch.EventType eventMask)
        {
			foreach (var record in sources)
			{
				if (record.client == source)
				{
					record.mask |= eventMask;
				}
			}
			//this.SetSourceEvents(source, eventMask);
        }

		void CheckSources(List<DispatchRecord> sources, double timeout, List<XmlRpcSource> toRemove)
		{
			XmlRpcDispatch.EventType defaultMask = XmlRpcDispatch.EventType.ReadableEvent | XmlRpcDispatch.EventType.WritableEvent | XmlRpcDispatch.EventType.Exception;

			ArrayList checkRead = new ArrayList();
			ArrayList checkWrite = new ArrayList();
			ArrayList checkExc = new ArrayList();

			foreach (var src in this.sources)
			{
				Socket sock = src.client.getSocket();
				if (sock == null)
					continue;
				var mask = src.mask;
				if ((mask & XmlRpcDispatch.EventType.ReadableEvent) != 0)
					checkRead.Add(sock);// FD_SET(fd, &inFd);
				if ((mask & XmlRpcDispatch.EventType.WritableEvent) != 0)
					checkWrite.Add(sock);
				//FD_SET(fd, &outFd);
				if ((mask & XmlRpcDispatch.EventType.Exception) != 0)
					checkExc.Add(sock);
			}

			// Check for events

			if (timeout < 0.0)
				Socket.Select(checkRead, checkWrite, checkExc, -1);
			else
			{
				//struct timeval tv;
				//tv.tv_sec = (int)floor(timeout);
				//tv.tv_usec = ((int)floor(1000000.0 * (timeout-floor(timeout)))) % 1000000;
				Socket.Select(checkRead, checkWrite, checkExc, (int)(timeout * 1000000));
				//nEvents = select(maxFd+1, &inFd, &outFd, &excFd, &tv);
			}

			int nEvents = checkRead.Count + checkWrite.Count + checkExc.Count;

			if (nEvents == 0)
				return;
			// Process events
			foreach (var record in this.sources)
			{
				XmlRpcSource src = record.client;
				XmlRpcDispatch.EventType newMask = defaultMask;// (unsigned) -1;
				Socket sock = src.getSocket();
				if (sock == null)
					continue;	// Seems like this is serious error
				// If you select on multiple event types this could be ambiguous
				if (checkRead.Contains(sock))
					newMask &= src.HandleEvent(XmlRpcDispatch.EventType.ReadableEvent);
				if (checkWrite.Contains(sock))
					newMask &= src.HandleEvent(XmlRpcDispatch.EventType.WritableEvent);
				if (checkExc.Contains(sock))
					newMask &= src.HandleEvent(XmlRpcDispatch.EventType.Exception);

				// Find the source again.  It may have moved as a result of the way
				// that sources are removed and added in the call stack starting
				// from the handleEvent() calls above.
				/*
				for (thisIt=_sources.begin(); thisIt != _sources.end(); thisIt++)
				{
					if(thisIt->getSource() == src)
					break;
				}
				if(thisIt == _sources.end())
				{
					XmlRpcUtil::error("Error in XmlRpcDispatch::work: couldn't find source iterator");
					continue;
				}*/

				if (newMask == EventType.NoEvent)
				{
					//_sources.erase(thisIt);  // Stop monitoring this one
					toRemove.Add(src);
					//this.RemoveSource(src);
					//if (!src.getKeepOpen())
					//	src.Close();
				}
				else if (newMask != defaultMask)
				{
					record.mask = newMask;
				}
			}
		}

		public void Work(double timeout)
        {
			_endTime = (timeout < 0.0) ? -1.0 : (getTime() + timeout);
			_doClear = false;
			_inWork = true;

			List<XmlRpcSource> toRemove = new List<XmlRpcSource>();
			while (sources.Count > 0)
			{
				var sourcesCopy = sources.GetRange(0, sources.Count);
				CheckSources(sourcesCopy, timeout, toRemove);

				foreach (var src in toRemove)
				{
					this.RemoveSource(src);
					if (!src.KeepOpen)
						src.Close();
				}
				// TODO: check for the rest events
				/*
				// Construct the sets of descriptors we are interested in
				fd_set inFd, outFd, excFd;
					FD_ZERO(&inFd);
					FD_ZERO(&outFd);
					FD_ZERO(&excFd);

				int maxFd = -1;     // Not used on windows
				SourceList::iterator it;
				for (it=_sources.begin(); it!=_sources.end(); ++it) {
					int fd = it->getSource()->getfd();
					if (it->getMask() & ReadableEvent) FD_SET(fd, &inFd);
					if (it->getMask() & WritableEvent) FD_SET(fd, &outFd);
					if (it->getMask() & Exception)     FD_SET(fd, &excFd);
					if (it->getMask() && fd > maxFd)   maxFd = fd;
				}

				// Check for events
				int nEvents;
				if (timeout < 0.0)
					nEvents = select(maxFd+1, &inFd, &outFd, &excFd, NULL);
				else 
				{
					struct timeval tv;
					tv.tv_sec = (int)floor(timeout);
					tv.tv_usec = ((int)floor(1000000.0 * (timeout-floor(timeout)))) % 1000000;
					nEvents = select(maxFd+1, &inFd, &outFd, &excFd, &tv);
				}

				if (nEvents < 0)
				{
					if(errno != EINTR)
					XmlRpcUtil::error("Error in XmlRpcDispatch::work: error in select (%d).", nEvents);
					_inWork = false;
					return;
				}

				// Process events
				for (it=_sources.begin(); it != _sources.end(); )
				{
					SourceList::iterator thisIt = it++;
					XmlRpcSource* src = thisIt->getSource();
					int fd = src->getfd();
					unsigned newMask = (unsigned) -1;
					if (fd <= maxFd) {
					// If you select on multiple event types this could be ambiguous
					if (FD_ISSET(fd, &inFd))
						newMask &= src->handleEvent(ReadableEvent);
					if (FD_ISSET(fd, &outFd))
						newMask &= src->handleEvent(WritableEvent);
					if (FD_ISSET(fd, &excFd))
						newMask &= src->handleEvent(Exception);

					// Find the source again.  It may have moved as a result of the way
					// that sources are removed and added in the call stack starting
					// from the handleEvent() calls above.
					for (thisIt=_sources.begin(); thisIt != _sources.end(); thisIt++)
					{
						if(thisIt->getSource() == src)
						break;
					}
					if(thisIt == _sources.end())
					{
						XmlRpcUtil::error("Error in XmlRpcDispatch::work: couldn't find source iterator");
						continue;
					}

					if ( ! newMask) {
						_sources.erase(thisIt);  // Stop monitoring this one
						if ( ! src->getKeepOpen())
						src->close();
					} else if (newMask != (unsigned) -1) {
						thisIt->getMask() = newMask;
					}
					}
				}
				*/
				if (_doClear)
				{
					var closeList = sources;
					this.sources = new List<DispatchRecord>();
					foreach (var it in closeList)
					{
						it.client.Close();
					}

					_doClear = false;
				}

				// Check whether end time has passed
				if (0 <= _endTime && getTime() > _endTime)
				  break;
			}
			_inWork = false;
            //work(instance, msTime);
        }

        public void Exit()
        {/*
            try
            {
                exit(instance);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }*/
        }

        public void Clear()
        {
            try
            {
                //clear(instance);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

		double _endTime = 0.0;
		bool _doClear;
		bool _inWork;

		public double getTime()
		{
			return 0;
		}
    }
}