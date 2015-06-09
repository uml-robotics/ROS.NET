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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Xml;

#endregion

namespace XmlRpc
{
	public class XmlRpcServer : XmlRpcSource
    {
        #region Reference Tracking + unmanaged pointer management
		public void Shutdown()
		{
			_disp.Clear();
			//Shutdown(__instance);
		}

        #endregion


		int _port;

		static string METHODNAME_TAG = "<methodName>";
		static string PARAMS_TAG = "<params>";
		static string PARAMS_ETAG = "</params>";
		static string PARAM_TAG = "<param>";
		static string PARAM_ETAG = "</param>";

		static string SYSTEM_MULTICALL = "system.multicall";
		static string METHODNAME = "methodName";
		static string PARAMS = "params";

		static string FAULTCODE = "faultCode";
		static string FAULTSTRING = "faultString";

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
            {
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
        }

        public void RemoveMethod(string name)
        {
			this._methods.Remove(name);
        }

		IAsyncResult asyncRequest = null;

        public void Work(double msTime)
        {
			XmlRpcUtil.log(6, "XmlRpcServer::work: waiting for a connection");
			/*
			if (asyncRequest == null)
			{
				asyncRequest = httpListener.BeginGetContext((IAsyncResult result) => { this.onRequest(result); }, this);
			}
			else
			{
				//
				return;
			}*/
			_disp.Work(msTime);
        }

		public void onRequest(IAsyncResult result)
		{
			HttpListener listener = (HttpListener)result.AsyncState;
			HttpListenerContext context = listener.EndGetContext(result);
			HttpListenerRequest request = context.Request;

			string responseData = "";
			
			using (StreamReader reader = new StreamReader(request.InputStream))
			{
				string requestData = reader.ReadToEnd();
				XmlRpcValue parms = new XmlRpcValue();
				responseData = this.parseRequest(parms, requestData);
				//parseResponse(result, _response);
			}
			
			// Obtain a response object.
			HttpListenerResponse response = context.Response;

			using (System.IO.Stream output = response.OutputStream)
			{
				/// TODO: write response here
			}
			this.asyncRequest = null;
		}

        public void Exit()
        {
			_disp.Exit();
        }

		public XmlRpcServerMethod FindMethod(string name)
        {
			if (this._methods.ContainsKey(name))
				return this._methods[name];
			return null;
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
			this._port = port;
			
			
			IPAddress address = new IPAddress(0); // INADDR_ANY
			try
			{
				this._port = port;
				listener = new TcpListener(address, port);
				listener.Start(backlog);
				this._port = ((IPEndPoint)listener.Server.LocalEndPoint).Port;
				_disp.AddSource(this, XmlRpcDispatch.EventType.ReadableEvent);
				XmlRpcUtil.log(2, "XmlRpcServer::bindAndListen: server listening on port {0}", this._port);
				/*
				listener.Stop();
				string prefix = String.Format("http://localhost:{0}/", port);
				try
				{
					httpListener = new HttpListener();
					httpListener.Prefixes.Add(prefix);

					httpListener.Start();
					return true;
				}
				catch (Exception ex)
				{
				}*/
				return true;
			}
			catch (Exception ex)
			{
				throw ex;
			}
			return true;
		}

		HttpListener httpListener;

		TcpListener listener;
		// Event dispatcher
		XmlRpcDispatch _disp = new XmlRpcDispatch();
		// Whether the introspection API is supported by this server
		bool _introspectionEnabled;
		// Collection of methods. This could be a set keyed on method name if we wanted...
		Dictionary<string, XmlRpcServerMethod> _methods = new Dictionary<string,XmlRpcServerMethod>();
		// system methods
		XmlRpcServerMethod _listMethods;
		XmlRpcServerMethod _methodHelp;

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
				Socket s = this.listener.AcceptSocket();//XmlRpcSocket::accept(this->getfd());
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
		public void enableIntrospection(bool enabled)
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
		// Run the method, generate _response string
		public string executeRequest(string _request)
		{
			string _response = "";
			XmlRpcValue parms = new XmlRpcValue(), resultValue = new XmlRpcValue();
			string methodName = parseRequest(parms, _request);
			XmlRpcUtil.log(2, "XmlRpcServerConnection::executeRequest: server calling method '{0}'", methodName);

			try
			{

				if (!executeMethod(methodName, parms, resultValue) &&
						!executeMulticall(methodName, parms, resultValue))
					_response = generateFaultResponse(methodName + ": unknown method name");
				else
					_response = generateResponse(resultValue.toXml());

			}
			catch (XmlRpcException fault)
			{
				XmlRpcUtil.log(2, "XmlRpcServerConnection::executeRequest: fault {0}.", fault.Message);
				_response = generateFaultResponse(fault.Message, fault.getCode());
			}
			return _response;
		}
		// Execute a named method with the specified params.
		public bool executeMethod(string methodName, XmlRpcValue parms, XmlRpcValue result)
		{
			XmlRpcServerMethod method = FindMethod(methodName);

			if (method == null) return false;

			method.Execute(parms, result);

			// Ensure a valid result value
			if (!result.Valid)
				result.Set("");

			return true;
		}
		// Create a response from results xml
		public string generateResponse(string resultXml)
		{
			string RESPONSE_1 = "<?xml version=\"1.0\"?>\r\n<methodResponse><params><param>\r\n\t";
			string RESPONSE_2 = "\r\n</param></params></methodResponse>\r\n";

			string body = RESPONSE_1 + resultXml + RESPONSE_2;
			string header = generateHeader(body);
			string result = header + body;
			XmlRpcUtil.log(5, "XmlRpcServerConnection::generateResponse:\n{0}\n", result);
			return result;
			
		}
		// Parse the method name and the argument values from the request.
		string parseRequest(XmlRpcValue parms, string _request)
		{
			bool success = true;
			string methodName = "unknown";
			//XmlRpcValue result = null;
			using (XmlReader reader = XmlReader.Create(new StringReader(_request)))
			{
				XmlDocument xmldoc = new XmlDocument();
				xmldoc.Load(reader);
				// Parse response xml into result
				//int offset = 0;
				XmlNodeList xmlMethodNameList = xmldoc.GetElementsByTagName("methodName");
				if (xmlMethodNameList.Count > 0)
				{
					XmlNode xmlMethodName = xmlMethodNameList[0];
					methodName = xmlMethodName.InnerText;
				}

				XmlNodeList xmlParameters = xmldoc.GetElementsByTagName("param");

				parms.SetArray(xmlParameters.Count);

				for (int i = 0; i < xmlParameters.Count; i++)
				{
					var value = new XmlRpcValue();
					value.fromXml(xmlParameters[i]["value"]);
					parms.asArray[i] = value;
				}
				/*
				XmlNode responseNode = xmlParameters[0];

				//if (!XmlRpcUtil.findTag(METHODRESPONSE_TAG, _response, out offset))
				if (xmlParameters.Count == 0)
				{
					XmlRpcUtil.error("Error in XmlRpcServer::parseRequest: Invalid request - no methodResponse. Request:\n{0}", _request);
					//return false;
				}

				XmlElement pars = responseNode["params"];
				XmlElement fault = responseNode["fault"];

				//result = new XmlRpcValue();
				if (pars != null)
				{
					bool isArray = false;
					var selection = pars.SelectNodes("param");
					if (selection.Count > 1)
					{
						parms.SetArray(selection.Count);
						int i = 0;
						foreach (XmlNode par in selection)
						{
							var value = new XmlRpcValue();
							value.fromXml(par["value"]);
							parms[i++] = value;
						}
					}
					else if (selection.Count == 1)
					{
						parms.fromXml(selection[0]["value"]);
					}
					else
						success = false;
				}
				else if (fault != null && parms.fromXml(fault))
				{
					success = false;
				}
				else
				{
					XmlRpcUtil.error("Error in XmlRpcServer::parseRequest: Invalid response - no param or fault tag. Request:\n{0}", _request);
				}*/
				//_request = "";
			}
			
			/*
			int offset = 0;   // Number of chars parsed from the request

			string methodName = XmlRpcUtil.parseTag(METHODNAME_TAG, _request, offset);

			if (methodName.Length > 0 && XmlRpcUtil.findTag(PARAMS_TAG, _request, offset))
			{
				int nArgs = 0;
				while (XmlRpcUtil.nextTagIs(PARAM_TAG, _request, offset)) {
					parms[nArgs++] = new XmlRpcValue(_request, offset);
					XmlRpcUtil.nextTagIs(PARAM_ETAG, _request, offset);
				}	

				XmlRpcUtil.nextTagIs(PARAMS_ETAG, _request, &offset);
			}
			*/
			return methodName;
		}
		// Prepend http headers
		string generateHeader(string body)
		{
			string header = "HTTP/1.1 200 OK\r\nServer: ";
			header += XmlRpcUtil.XMLRPC_VERSION;
			header += "\r\nContent-Type: text/xml\r\nContent-length: ";

			string buffLen = String.Format("{0}\r\n\r\n", body.Length);
			//char buffLen[40];

			//sprintf(buffLen,"%d\r\n\r\n", (int)body.size());

			return header + buffLen;
		}

		public string generateFaultResponse(string errorMsg, int errorCode = -1)
		{
			string RESPONSE_1 = "<?xml version=\"1.0\"?>\r\n<methodResponse><fault>\r\n\t";
			string RESPONSE_2 = "\r\n</fault></methodResponse>\r\n";

			XmlRpcValue faultStruct = new XmlRpcValue();
			faultStruct.Set(FAULTCODE, errorCode);
			faultStruct.Set(FAULTSTRING, errorMsg);
			string body = RESPONSE_1 + faultStruct.toXml() + RESPONSE_2;
			string header = generateHeader(body);

			return header + body;
		}
		// Execute multiple calls and return the results in an array.
		public bool executeMulticall(string methodNameRoot, XmlRpcValue parms, XmlRpcValue result)
		{
			if (methodNameRoot != SYSTEM_MULTICALL) return false;

			// There ought to be 1 parameter, an array of structs
			if (parms.Length != 1 || parms[0].Type != XmlRpcValue.ValueType.TypeArray)
				throw new XmlRpcException(SYSTEM_MULTICALL + ": Invalid argument (expected an array)");

			int nc = parms[0].Length;
			result.SetArray(nc);

			for (int i = 0; i < nc; ++i)
			{
				if (!parms[0][i].hasMember(METHODNAME) ||
						!parms[0][i].hasMember(PARAMS))
				{
					result[i].Set(FAULTCODE, -1);
					result[i].Set(FAULTSTRING, SYSTEM_MULTICALL + ": Invalid argument (expected a struct with members methodName and params)");
					continue;
				}

				string methodName = parms[0][i][METHODNAME].GetString();
				XmlRpcValue methodParams = parms[0][i][PARAMS];

				XmlRpcValue resultValue = new XmlRpcValue();
				resultValue.SetArray(1);
				try
				{
					if (!executeMethod(methodName, methodParams, resultValue[0]) &&
						!executeMulticall(methodName, parms, resultValue[0]))
					{
						result[i].Set(FAULTCODE, -1);
						result[i].Set(FAULTSTRING, methodName + ": unknown method name");
					}
					else
						result[i] = resultValue;

				}
				catch (XmlRpcException fault)
				{
					result[i].Set(FAULTCODE, 0);
					result[i].Set(FAULTSTRING, fault.Message);
				}
			}

			return true;
		}
	}
}