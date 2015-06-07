// File: XmlRpcServer.cs
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

#endregion

namespace XmlRpc
{
	public class XmlRpcServer : XmlRpcSource
    {
		/*
        protected IntPtr __instance;

        public IntPtr instance
        {
            [DebuggerStepThrough]
            get { return __instance; }
            [DebuggerStepThrough]
            set
            {
                if (__instance != IntPtr.Zero)
                    RmRef(ref __instance);
                if (value != IntPtr.Zero)
                    AddRef(value);
                __instance = value;
            }
        }*/

        #region Reference Tracking + unmanaged pointer management
		/*
        public void Dispose()
        {
            //Shutdown();
            //RmRef(ref __instance);
        }

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
        public static XmlRpcServer LookUp(IntPtr ptr)
        {
            if (ptr != IntPtr.Zero)
            {
                AddRef(ptr);
                return new XmlRpcServer(ptr);
            }
            return null;
        }
        [DebuggerStepThrough]
        private new static void AddRef(IntPtr ptr)
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
        private new static void RmRef(ref IntPtr ptr)
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
                        shutdown(ptr);
                        XmlRpcUtil.Free(ptr);
                    }
                }
                ptr = IntPtr.Zero;
            }
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

		public void Shutdown()
		{
			_disp.Clear();
			//Shutdown(__instance);
		}

        #endregion
		/*
        [DebuggerStepThrough]
        public XmlRpcServer()
        {
            instance = create();
        }
		
        [DebuggerStepThrough]
        public XmlRpcServer(IntPtr copy)
        {
            if (copy != IntPtr.Zero)
            {
                instance = copy;
            }
        }*/

        public int Port
        {
            [DebuggerStepThrough]
            get
            {
                //SegFault();
                return _port;
            }
        }

        public XmlRpcDispatch Dispatch
        {
            [DebuggerStepThrough]
            get
            {/*
                //SegFault();
                if (_dispatch == null)
                {
                    IntPtr ret = getdispatch(instance);
                    if (ret == IntPtr.Zero)
                        return null;
                    _dispatch = XmlRpcDispatch.LookUp(ret);
                }
                return _dispatch;*/
				return _disp;
            }
        }

        //private XmlRpcDispatch _dispatch;

        #region P/Invoke
		/*
        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcServer_Create", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr create();

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcServer_AddMethod", CallingConvention = CallingConvention.Cdecl
            )]
        private static extern void addmethod(IntPtr target, IntPtr method);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcServer_RemoveMethod",
            CallingConvention = CallingConvention.Cdecl)]
        private static extern void removemethod(IntPtr target, IntPtr method);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcServer_RemoveMethodByName",
            CallingConvention = CallingConvention.Cdecl)]
        private static extern void removemethodbyname(IntPtr target,
            [In] [Out] [MarshalAs(UnmanagedType.LPStr)] string method);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcServer_FindMethod",
            CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr findmethod(IntPtr target, [In] [Out] [MarshalAs(UnmanagedType.LPStr)] string name);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcServer_BindAndListen",
            CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool bindandlisten(IntPtr target, int port, int backlog);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcServer_Work", CallingConvention = CallingConvention.Cdecl)]
        private static extern void work(IntPtr target, double msTime);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcServer_Exit", CallingConvention = CallingConvention.Cdecl)]
        private static extern void exit(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcServer_Shutdown", CallingConvention = CallingConvention.Cdecl)
        ]
        private static extern void shutdown(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcServer_GetPort", CallingConvention = CallingConvention.Cdecl)]
        private static extern int getport(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcServer_GetDispatch",
            CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr getdispatch(IntPtr target);
		*/
        #endregion

		public void AddMethod(XmlRpcServerMethod method)
        {
			this._methods.Add(method.name, method);
            //SegFault();
            //addmethod(instance, method.instance);
        }

		public void RemoveMethod(XmlRpcServerMethod method)
        {
			foreach (var rec in this._methods)
			{
				if (method == rec.Value)
				{
					this._methods.Remove(rec.Key);
					break;
				}
			}
			//this._methods.Remove(method.name);
           // SegFault();
            //removemethod(instance, method.instance);
        }

        public void RemoveMethod(string name)
        {
            //SegFault();
			this._methods.Remove(name);
            //removemethodbyname(instance, name);
        }

        public void Work(double msTime)
        {
            //SegFault();
			XmlRpcUtil.log(6, "XmlRpcServer::work: waiting for a connection");
			_disp.Work(msTime);
            //work(instance, msTime);
        }

        public void Exit()
        {
            //SegFault();
			_disp.Exit();
            //exit(instance);
        }

		public XmlRpcServerMethod FindMethod(string name)
        {
            //SegFault();
			if (this._methods.ContainsKey(name))
				return this._methods[name];
			return null;
            //IntPtr ret = findmethod(instance, name);
            //if (ret == IntPtr.Zero) return null;
            //return XMLRPCCallWrapper.LookUp(ret);
        }

        public bool BindAndListen(int port)
        {
            return BindAndListen(port, 5);
        }

		public override Socket getSocket()
		{
			return listener != null ? listener.Server : null;
		}

        public bool BindAndListen(int port, int backlog)
        {
			IPAddress address = new IPAddress(0); // INADDR_ANY
			try
			{
				this._port = port;
				listener = new TcpListener(address, port);
				listener.Start(backlog);
				_disp.AddSource(this, XmlRpcDispatch.EventType.ReadableEvent);

				this._port = ((IPEndPoint)listener.Server.LocalEndPoint).Port;
				XmlRpcUtil.log(2, "XmlRpcServer::bindAndListen: server listening on port {0}", this._port);
				//listener.
				//SegFault();
				//return bindandlisten(instance, port, backlog);
				return true;
			}
			catch (Exception ex)
			{
				throw ex;
			}
			return false;
		}
		TcpListener listener;
		// Event dispatcher
		XmlRpcDispatch _disp = new XmlRpcDispatch();
		// Whether the introspection API is supported by this server
		bool _introspectionEnabled;
		// Collection of methods. This could be a set keyed on method name if we wanted...
		Dictionary<string, XmlRpcServerMethod> _methods;
		// system methods
		XmlRpcServerMethod _listMethods;
		XmlRpcServerMethod _methodHelp;

		/*
        [DebuggerStepThrough]
        public new void SegFault()
        {
            if (instance == IntPtr.Zero)
                throw new Exception("This isn't really a segfault, but your pointer is invalid, so it would have been!");
        }*/
		
		// Handle input on the server socket by accepting the connection
		// and reading the rpc request.
		override public XmlRpcDispatch.EventType HandleEvent(XmlRpcDispatch.EventType eventType)
		{
		  acceptConnection();
		  return XmlRpcDispatch.EventType.ReadableEvent;		// Continue to monitor this fd
		}


		// Accept a client connection request and create a connection to
		// handle method calls from the client.
		void acceptConnection()
		{	
			try{
				Socket s = this.getSocket().Accept();//XmlRpcSocket::accept(this->getfd());
				//XmlRpcUtil.log(2, "XmlRpcServer::acceptConnection: socket %d", s);

				XmlRpcUtil.log(2, "XmlRpcServer::acceptConnection: creating a connection");
				_disp.AddSource(this.createConnection(s), XmlRpcDispatch.EventType.ReadableEvent);
			}
			catch(SocketException ex)
			{
			//if (s == null)
			//{
			//this->close();
				XmlRpcUtil.error("XmlRpcServer::acceptConnection: Could not accept connection ({0}).", ex.Message);
			//}
			}
				/*
			else if (s!XmlRpcSocket.setNonBlocking(s))
			{
			XmlRpcSocket::close(s);
			XmlRpcUtil.error("XmlRpcServer::acceptConnection: Could not set socket to non-blocking input mode (%s).", XmlRpcSocket::getErrorMsg().c_str());
			}*/
			//else  // Notify the dispatcher to listen for input on this source when we are in work()
			{
			
			}
		}

		HttpListener httpListener;

		void onRequest(HttpListenerContext context)
		{
			//httpListener.S
		}

		// Create a new connection object for processing requests from a specific client.
		XmlRpcServerConnection createConnection(Socket s)
		{
			// Specify that the connection object be deleted when it is closed
			return new XmlRpcServerConnection(s, this, true);
		}


		public void removeConnection(XmlRpcServerConnection sc)
		{
		  _disp.RemoveSource(sc);
		}


		// Stop processing client requests
		void exit()
		{
		  _disp.Exit();
		}


		// Close the server socket file descriptor and stop monitoring connections
		void shutdown()
		{
		  // This closes and destroys all connections as well as closing this socket
		  _disp.Clear();
		}


// Introspection support
static string LIST_METHODS = "system.listMethods";
static string METHOD_HELP = "system.methodHelp";
static string MULTICALL = "system.multicall";


// List all methods available on a server
class ListMethods : XmlRpcServerMethod
{
public
  ListMethods(XmlRpcServer s)
		: base(LIST_METHODS, null, s) { this.FUNC = execute; }

	void execute(XmlRpcValue parms, XmlRpcValue result)
	{
		server.listMethods(result);
	}

	string help() { return "List all methods available on a server as an array of strings"; }
};


// Retrieve the help string for a named method
class MethodHelp : XmlRpcServerMethod
{
public MethodHelp(XmlRpcServer s) : base(METHOD_HELP, null, s) 
{
	this.FUNC = execute;
}

  void execute(XmlRpcValue parms, XmlRpcValue result)
  {
    if (parms[0].Type != XmlRpcValue.ValueType.TypeString)
      throw new XmlRpcException(METHOD_HELP + ": Invalid argument type");

	XmlRpcServerMethod m = server.FindMethod(parms[0].GetString());
    if (m == null)
      throw new XmlRpcException(METHOD_HELP + ": Unknown method name");

	result.Set(m.Help());
  }

  public override string Help() { return ("Retrieve the help string for a named method"); }
};

    
// Specify whether introspection is enabled or not. Default is enabled.
void enableIntrospection(bool enabled)
{
  if (_introspectionEnabled == enabled)
    return;

  _introspectionEnabled = enabled;

  if (enabled)
  {
    if ( _listMethods == null)
    {
      _listMethods = new ListMethods(this);
      _methodHelp = new MethodHelp(this);
    } else {
      AddMethod(_listMethods);
      AddMethod(_methodHelp);
    }
  }
  else
  {
    RemoveMethod(LIST_METHODS);
    RemoveMethod(METHOD_HELP);
  }
}


void listMethods(XmlRpcValue result)
{
  int i = 0;
  result.SetArray(_methods.Count+1);

  foreach (var rec in _methods)
  {
	  result.Set(i++, rec.Key);
  }
	/*
  for (MethodMap::iterator it=_methods.begin(); it != _methods.end(); ++it)
    result[i++] = it->first;*/

  // Multicall support is built into XmlRpcServerConnection
  result.Set(i, MULTICALL);
}

		int _port;
    }
}