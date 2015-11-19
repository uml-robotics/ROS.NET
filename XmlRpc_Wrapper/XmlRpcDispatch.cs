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

namespace XmlRpc_Wrapper
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

		class DispatchRecord
		{
			public XmlRpcSource client;
			public XmlRpcDispatch.EventType mask;
		}

		List<DispatchRecord> sources = new List<DispatchRecord>();

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
				Socket.Select(checkRead, checkWrite, checkExc, (int)(timeout * 1000000.0));
				//nEvents = select(maxFd+1, &inFd, &outFd, &excFd, &tv);
			}

			int nEvents = checkRead.Count + checkWrite.Count + checkExc.Count;

			if (nEvents == 0)
				return;
			List<DispatchRecord> sourcesCopy = new List<DispatchRecord>();
			lock (this.sources)
			{
				foreach (var record in this.sources)
					sourcesCopy.Add(record);
			}
			// Process events
			foreach (var record in sourcesCopy)
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
					// TODO: should we close it right here?
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
        {
			/// TODO: Do something reasonable here?
			/*
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
			return 0.001 * Environment.TickCount;
		}
    }
}