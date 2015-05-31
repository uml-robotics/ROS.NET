// File: XmlRpcClient.cs
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
using System.Xml;

#endregion

namespace XmlRpc
{
    [DebuggerStepThrough]
    public class XmlRpcClient : XmlRpcSource //: IDisposable
    {
		/*
		static string REQUEST_BEGIN = "<?xml version=\"1.0\"?>\r\n<methodCall><methodName>";
		static string REQUEST_END_METHODNAME = "</methodName>\r\n";
		static string PARAMS_TAG = "<params>";
		static string PARAMS_ETAG = "</params>";
		static string PARAM_TAG = "<param>";
		static string PARAM_ETAG =  "</param>";
		static string REQUEST_END = "</methodCall>\r\n";
		static string METHODRESPONSE_TAG = "<methodResponse>";
		static string FAULT_TAG = "<fault>";*/

		TcpClient socket;
		//NetworkStream stream;
		StreamReader reader;
		StreamWriter writer;
		/*
        public void SegFault()
        {
            if (__instance == IntPtr.Zero)
            {
                throw new Exception("BOOM");
            }
        }*/

        public string HostUri = "";

        [DebuggerStepThrough]
        public XmlRpcClient(string HostName, int Port, string Uri)
        {
			createImpl(HostName, Port, Uri);
			//socket.GetStream();
        }

        [DebuggerStepThrough]
        public XmlRpcClient(string HostName, int Port)
            : this(HostName, Port, "/")
        {
        }

		void createImpl(string host, int port, string uri)
		{
			XmlRpcUtil.log(1, "XmlRpcClient new client: host %s, port %d.", host, port);

			_host = host;
			_port = port;
			if (uri != null)
				_uri = uri;
			else
				_uri = "/RPC2";
			_connectionState = ConnectionState. NO_CONNECTION;
			_executing = false;
			_eof = false;

			// Default to keeping the connection open until an explicit close is done
			setKeepOpen();
		}

        [DebuggerStepThrough]
        public XmlRpcClient(string WHOLESHEBANG)
        {
            if (!WHOLESHEBANG.Contains("://")) 
				throw new Exception("INVALID ARGUMENT DIE IN A FIRE!");
            WHOLESHEBANG = WHOLESHEBANG.Remove(0, WHOLESHEBANG.IndexOf("://") + 3);
            WHOLESHEBANG.Trim('/');
            string[] chunks = WHOLESHEBANG.Split(':');
            string hn = chunks[0];
            string[] chunks2 = chunks[1].Split('/');
            int p = int.Parse(chunks2[0]);
            string u = "/";
            if (chunks2.Length > 1 && chunks2[1].Length != 0)
                u = chunks2[1];

			createImpl(hn, p, u);
        }

        #region public get passthroughs

        public bool IsConnected
        {
            [DebuggerStepThrough] get { return socket != null && socket.Connected; }
        }
		
        public string Host
        {
            [DebuggerStepThrough] get { return _host; }
        }

        public string Uri
        {
            [DebuggerStepThrough] get { return _uri; }
        }

        public int Port
        {
            [DebuggerStepThrough] get { return _port; }
        }

        public string Request
        {
            [DebuggerStepThrough] get { return _request; }
        }

        public string Header
        {
            [DebuggerStepThrough] get { return _header; }
        }

        public string Response
        {
            [DebuggerStepThrough] get { return _response; }
        }

        public int SendAttempts
        {
			[DebuggerStepThrough]
			get { return _sendAttempts; }
        }

        public int BytesWritten
        {
			[DebuggerStepThrough]
			get { return _bytesWritten; }
        }

        public bool Executing
        {
			[DebuggerStepThrough]
			get { return _executing; }
        }

        public bool EOF
        {
			[DebuggerStepThrough]
			get { return _eof; }
        }

        public int ContentLength
        {
            [DebuggerStepThrough] get { return this._contentLength; }
        }
		/*
        public IntPtr XmlRpcDispatch
        {
            [DebuggerStepThrough] get { return getxmlrpcdispatch(instance); }
        }*/

        #endregion

        #region public function passthroughs

        public bool CheckIdentity(string host, int port, string uri)
        {
			/*
extern XMLRPC_API unsigned char XmlRpcClient_CheckIdent(XmlRpcClient* instance, const char* host, int port, const char* uri)
{
    if (instance == NULL) return 0;
    return (instance->getPort() == port && instance->getUri().compare(uri) == 0 && instance->getHost().compare(host) == 0) ? 1 : 0;
}
			*/
			return this._host.Equals(host) && this._port == port && this._uri.Equals(uri);// checkident(instance, host, port, uri);
        }

		
        public bool Execute(string method, XmlRpcValue parameters, XmlRpcValue result)
        {
            bool r = execute(method, parameters, result);
            return r;
        }

		public void SegFault()
		{
			// 
		}

        public bool ExecuteNonBlock(string method, XmlRpcValue parameters)
        {
			return executeNonBlock(method, parameters);
        }

        public bool ExecuteCheckDone(XmlRpcValue result)
        {
			//return executeCheckDone( result);
			return true;
        }
		/*
		public UInt16 HandleEvent(XmlRpcDispatch.EventType eventType)
        {
            return handleEvent(eventType);
        }
		*/
        #endregion
/*
        #region P/Invoke

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_Create", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr create
            (
            [In] [Out] [MarshalAs(UnmanagedType.LPStr)] string host,
            int port,
            [In] [Out] [MarshalAs(UnmanagedType.LPStr)] string uri);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_Close", CallingConvention = CallingConvention.Cdecl)]
        private static extern void close(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_Execute", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool execute
            (IntPtr target,
                [In] [Out] [MarshalAs(UnmanagedType.LPStr)] string method,
                IntPtr parameters,
                IntPtr result);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_ExecuteNonBlock",
            CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool executenonblock
            (IntPtr target,
                [In] [Out] [MarshalAs(UnmanagedType.LPStr)] string method, IntPtr parameters);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_ExecuteCheckDone",
            CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool executecheckdone([In] [Out] IntPtr target, [In] [Out] IntPtr result);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_HandleEvent",
            CallingConvention = CallingConvention.Cdecl)]
        private static extern UInt16 handleevent(IntPtr target, UInt16 eventType);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_IsFault", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool isconnected(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_GetHost", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr gethost(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_GetUri", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr geturi(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_GetPort", CallingConvention = CallingConvention.Cdecl)]
        private static extern int getport(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_GetRequest", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr getrequest(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_GetHeader", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr getheader(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_GetResponse", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr getresponse(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_GetSendAttempts",
            CallingConvention = CallingConvention.Cdecl)]
        private static extern int getsendattempts(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_GetBytesWritten",
            CallingConvention = CallingConvention.Cdecl)]
        private static extern int getbyteswritten(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_GetExecuting",
            CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool getexecuting(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_GetEOF", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool geteof(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_CheckIdent", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool checkident(IntPtr target, [In] [Out] [MarshalAs(UnmanagedType.LPStr)] string host, int port, [In] [Out] [MarshalAs(UnmanagedType.LPStr)] string uri);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_GetContentLength",
            CallingConvention = CallingConvention.Cdecl)]
        private static extern int getcontentlength(IntPtr Target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcClient_GetXmlRpcDispatch",
            CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr getxmlrpcdispatch(IntPtr target);

        #endregion
 */

		// Static data
		static string REQUEST_BEGIN = "<?xml version=\"1.0\"?>\r\n<methodCall><methodName>";
		static string REQUEST_END_METHODNAME = "</methodName>\r\n";
		static string PARAMS_TAG = "<params>";
		static string PARAMS_ETAG = "</params>";
		static string PARAM_TAG = "<param>";
		static string PARAM_ETAG =  "</param>";
		static string REQUEST_END = "</methodCall>\r\n";
		static string METHODRESPONSE_TAG = "<methodResponse>";
		static string FAULT_TAG = "<fault>";

		// Server location
		int _port;
		string _host;
		string _uri;

		string getHost() { return _host; }
		string getUri()  { return _uri; }
		int getPort() { return _port; }
    
		// The xml-encoded request, http header of response, and response xml
		string _request;
		string _header;
		string _response;

		// Number of times the client has attempted to send the request
		int _sendAttempts;

		// Number of bytes of the request that have been written to the socket so far
		int _bytesWritten;

		// True if we are currently executing a request. If you want to multithread,
		// each thread should have its own client.
		bool _executing;

		// True if the server closed the connection
		bool _eof;

		// True if a fault response was returned by the server
		bool _isFault;

		// Number of bytes expected in the response body (parsed from response header)
		int _contentLength;

		// Event dispatcher
		XmlRpcDispatch _disp;

		bool testConnection()
		{	
			if (((int)_connectionState & (int)ConnectionState.NO_CONNECTION) != 0)
				if (! setupConnection())
					return false;
			return ((int)_connectionState & (int)ConnectionState.NO_CONNECTION) != 0;	
		}

		//done and works
		void Initialize(string host, int port, string uri/*=0*/)
		{
			XmlRpcUtil.log(1, "XmlRpcClient new client: host %s, port %d.", host, port);

			_host = host;
			_port = port;
			if (uri != null)
				_uri = uri;
			else
				_uri = "/RPC2";
			_connectionState = ConnectionState.NO_CONNECTION;
			_executing = false;
			_eof = false;

			// Default to keeping the connection open until an explicit close is done
			setKeepOpen();
		}

			/*
		XmlRpcClient::~XmlRpcClient()
		{
		  this->close();
		}*/

		// Close the owned fd
		void close()
		{
		  XmlRpcUtil.log(4, "XmlRpcClient::close.");
		  _connectionState = ConnectionState.NO_CONNECTION;
		
		  _disp.Exit();
		  //_disp.removeSource(this);
		  //XmlRpcSource::close();
			if(socket != null)
			{
				socket.Close();
				reader = null;
				writer = null;
			}
		}

	
		// Execute the named procedure on the remote server.
		// Params should be an array of the arguments for the method.
		// Returns true if the request was sent and a result received (although the result
		// might be a fault).
		bool execute(string method, XmlRpcValue parameters, XmlRpcValue result)
		{
		  XmlRpcUtil.log(1, "XmlRpcClient::execute: method %s (_connectionState %d).", method, _connectionState);
		  result = null;
		  // This is not a thread-safe operation, if you want to do multithreading, use separate
		  // clients for each thread. If you want to protect yourself from multiple threads
		  // accessing the same client, replace this code with a real mutex.
		  if (_executing)
			return false;

		  _executing = true;
		  //ClearFlagOnExit cf(_executing);

		  _sendAttempts = 0;
		  _isFault = false;

		  if ( ! setupConnection())
			return false;

		  if ( ! generateRequest(method, parameters))
			return false;

		  //result.clear();
		  double msTime = -1.0;   // Process until exit is called
		  _disp.Work(msTime);

		  if (_connectionState != ConnectionState.IDLE )
			return false;
		  parseResponse(result);

		  XmlRpcUtil.log(1, "XmlRpcClient::execute: method %s completed.", method);
		  _response = "";
		  return true;
		}

		// Execute the named procedure on the remote server, non-blocking.
		// Params should be an array of the arguments for the method.
		// Returns true if the request was sent and a result received (although the result
		// might be a fault).
		bool executeNonBlock(string method, XmlRpcValue parameters)
		{
			XmlRpcUtil.log(1, "XmlRpcClient::execute: method %s (_connectionState %d).", method, _connectionState);

			// This is not a thread-safe operation, if you want to do multithreading, use separate
			// clients for each thread. If you want to protect yourself from multiple threads
			// accessing the same client, replace this code with a real mutex.
			if (_executing)
				return false;

			_executing = true;
			//ClearFlagOnExit cf(_executing);

			_sendAttempts = 0;
			_isFault = false;

			if ( ! setupConnection())
				return false;

			if ( ! generateRequest(method, parameters))
				return false;

			return true;
		}

		bool executeCheckDone(XmlRpcValue result)
		{
			//result.clear();
			// Are we done yet?
			if (_connectionState != ConnectionState.IDLE)
				return false;

			if (!parseResponse(result))
			{
			// Hopefully the caller can determine that parsing failed.
			}
			//XmlRpcUtil::log(1, "XmlRpcClient::execute: method %s completed.", method);
			_response = "";
			return true;
		}

		string socketError;
		string getSocketError()
		{
			return "UnknownError";
		}
		// Possible IO states for the connection
		enum ConnectionState 
		{
			NO_CONNECTION, 
			CONNECTING, 
			WRITE_REQUEST, 
			READ_HEADER, 
			READ_RESPONSE, 
			IDLE 
		};
		ConnectionState _connectionState;
		// XmlRpcSource interface implementation
		// Handle server responses. Called by the event dispatcher during execute.
		//XmlRpcDispatch.EventType handleEvent(XmlRpcDispatch.EventType eventType)
		public override XmlRpcDispatch.EventType HandleEvent(XmlRpcDispatch.EventType eventType)
		{
			if (eventType == XmlRpcDispatch.EventType.Exception)
			{
				if (_connectionState == ConnectionState.WRITE_REQUEST && _bytesWritten == 0)
					XmlRpcUtil.error("Error in XmlRpcClient::handleEvent: could not connect to server ({0}).",
									getSocketError());
				else
					XmlRpcUtil.error("Error in XmlRpcClient::handleEvent (state {0}): {1}.",
									_connectionState, getSocketError());
				return 0;
			}

			if (_connectionState == ConnectionState.WRITE_REQUEST)
			if ( ! writeRequest()) return 0;

			if (_connectionState == ConnectionState.READ_HEADER)
			if ( ! readHeader()) return 0;

			if (_connectionState == ConnectionState.READ_RESPONSE)
			if ( ! readResponse()) return 0;

			// This should probably always ask for Exception events too
			return (_connectionState == ConnectionState.WRITE_REQUEST) 
				? XmlRpcDispatch.EventType.WritableEvent : XmlRpcDispatch.EventType.ReadableEvent;
		}

		HttpWebRequest httpClient;


		// Create the socket connection to the server if necessary
		bool setupConnection()
		{
		  // If an error occurred last time through, or if the server closed the connection, close our end
			if ((_connectionState != ConnectionState.NO_CONNECTION && _connectionState != ConnectionState.IDLE) || _eof)
			close();

		  _eof = false;
		  if (_connectionState == ConnectionState.NO_CONNECTION)
			if (! doConnect()) 
			  return false;

		  // Prepare to write the request
		  _connectionState = ConnectionState.WRITE_REQUEST;
		  _bytesWritten = 0;

		  // Notify the dispatcher to listen on this source (calls handleEvent when the socket is writable)
		  _disp.RemoveSource(this);       // Make sure nothing is left over
		  _disp.AddSource(this, XmlRpcDispatch.EventType.WritableEvent | XmlRpcDispatch.EventType.Exception);

		  return true;
		}

	// Connect to the xmlrpc server
	bool doConnect()
	{
		/*
	  int fd = XmlRpcSocket::socket();
	  if (fd < 0)
	  {
		XmlRpcUtil.error("Error in XmlRpcClient::doConnect: Could not create socket (%s).", XmlRpcSocket::getErrorMsg().c_str());
		return false;
	  }

	  XmlRpcUtil.log(3, "XmlRpcClient::doConnect: fd %d.", fd);
	  this->setfd(fd);

	  // Don't block on connect/reads/writes
	  if ( ! XmlRpcSocket.setNonBlocking(fd))
	  {
		this->close();
		XmlRpcUtil.error("Error in XmlRpcClient::doConnect: Could not set socket to non-blocking IO mode (%s).", XmlRpcSocket::getErrorMsg().c_str());
		return false;
	  }*/

		socket = new TcpClient(_host, _port);
	
		if ( socket.Connected)
		{
			this.close();
			XmlRpcUtil.error("Error in XmlRpcClient::doConnect: Could not connect to server (%s).", this.getSocketError());
			return false;
		}
		return true;
	}
	public void Shutdown()
	{
		// TODO: implement closing a socket
	}
		// Encode the request to call the specified method with the specified parameters into xml
		bool generateRequest(string methodName, XmlRpcValue parameters)
		{
		  string body = REQUEST_BEGIN;
		  body += methodName;
		  body += REQUEST_END_METHODNAME;

		  // If params is an array, each element is a separate parameter
		  if (parameters.Valid) 
		  {
			body += PARAMS_TAG;
			if (parameters.Type == XmlRpcValue.ValueType.TypeArray)
			{
			  for (int i=0; i < parameters.Length; ++i) {
				body += PARAM_TAG;
				body += parameters[i].toXml();
				body += PARAM_ETAG;
			  }
			}
			else
			{
			  body += PARAM_TAG;
			  body += parameters.toXml();
			  body += PARAM_ETAG;
			}
      
			body += PARAMS_ETAG;
		  }
		  body += REQUEST_END;

		  string header = generateHeader(body);
		  XmlRpcUtil.log(4, "XmlRpcClient::generateRequest: header is {0} bytes, content-length is {1}.", 
						  header.Length, body.Length);

		  _request = header + body;
		  return true;
		}

		// Prepend http headers
		string generateHeader(string body)
		{
		  string header = "POST " + _uri + " HTTP/1.1\r\nUser-Agent: ";
		  header += XmlRpcUtil.XMLRPC_VERSION;
		  header += "\r\nHost: ";
		  header += _host;

		  //char[]buff = new char [40];
		  string buff = String.Format(":{0}\r\n", _port);
		  //sprintf(buff,":%d\r\n", _port);


		  header += buff;
		  header += "Content-Type: text/xml\r\nContent-length: ";
		  buff = String.Format("{0}\r\n\r\n", body.Length);
		  //sprintf(buff,"%d\r\n\r\n", (int)body.size());

		  return header + buff;
		}

		bool writeRequest()
		{
			if (_bytesWritten == 0)
				XmlRpcUtil.log(5, "XmlRpcClient::writeRequest (attempt {0}):\n{1}\n", _sendAttempts+1, _request);

			// Try to write the request
			try
			{
				writer.Write(_request);
			}
			catch(System.IO.IOException ex)
			{	
				XmlRpcUtil.error("Error in XmlRpcClient::writeRequest: write error ({0}).",ex.Message);
				return false;
			}
    
			XmlRpcUtil.log(3, "XmlRpcClient::writeRequest: wrote %d of %d bytes.", _bytesWritten, _request.Length);

			// Wait for the result
			if (_bytesWritten == _request.Length) 
			{
			_header = "";
			_response = "";
			_connectionState = ConnectionState.READ_HEADER;
			}
			return true;
		}

		bool _keepOpen;
// Read the header from the response
		bool readHeader()
		{
#if USE_BULLSHIT
		  // Read available data
			try
			{
				_header = reader.ReadToEnd();
			}
			catch (System.IO.IOException ex)
			{
				// If we haven't read any data yet and this is a keep-alive connection, the server may
				// have timed out, so we try one more time.
				if (_keepOpen && _header.Length == 0 && _sendAttempts++ == 0)
				{
					XmlRpcUtil.log(4, "XmlRpcClient::readHeader: re-trying connection");
					Close();
					//XmlRpcSource.close();
					_connectionState = ConnectionState.NO_CONNECTION;
					_eof = false;
					return setupConnection();
				}

				XmlRpcUtil.error("Error in XmlRpcClient::readHeader: error while reading header ({0}) on fd %d.",
								  getSocketError());
				return false;
			}

			XmlRpcUtil.log(4, "XmlRpcClient::readHeader: client has read %d bytes", _header.Length);

			char *hp = (char*)_header.c_str();  // Start of header
			char *ep = hp + _header.Length;   // End of string
			char *bp = 0;                       // Start of body
			char *lp = 0;                       // Start of content-length value

			for (char *cp = hp; (bp == 0) && (cp < ep); ++cp) {
			if ((ep - cp > 16) && (strncasecmp(cp, "Content-length: ", 16) == 0))
				lp = cp + 16;
			else if ((ep - cp > 4) && (strncmp(cp, "\r\n\r\n", 4) == 0))
				bp = cp + 4;
			else if ((ep - cp > 2) && (strncmp(cp, "\n\n", 2) == 0))
				bp = cp + 2;
			}

			// If we haven't gotten the entire header yet, return (keep reading)
			if (bp == 0) {
				if (_eof)          // EOF in the middle of a response is an error
				{
					XmlRpcUtil.error("Error in XmlRpcClient::readHeader: EOF while reading header");
					return false;   // Close the connection
				}
    
				return true;  // Keep reading
			}

			// Decode content length
			if (lp == 0) {
				XmlRpcUtil.error("Error XmlRpcClient::readHeader: No Content-length specified");
				return false;   // We could try to figure it out by parsing as we read, but for now...
			}

			_contentLength = atoi(lp);
			if (_contentLength <= 0) {
				XmlRpcUtil.error("Error in XmlRpcClient::readHeader: Invalid Content-length specified (%d).", _contentLength);
				return false;
			}
  	
			XmlRpcUtil.log(4, "client read content length: %d", _contentLength);

			// Otherwise copy non-header data to response buffer and set state to read response.
			_response = bp;
#endif
			_header = "";   // should parse out any interesting bits from the header (connection, etc)...
			_connectionState = ConnectionState.READ_RESPONSE;

			return true;    // Continue monitoring this source
		}

		bool readResponse()
		{
			// If we dont have the entire response yet, read available data
			if (_response.Length < _contentLength) 
			{
				try
				{
					_response = reader.ReadToEnd();
				}
				catch (IOException ex)
				{
					XmlRpcUtil.error("Error in XmlRpcClient::readResponse: read error ({0}).", ex.Message);
					return false;
				}
				/*
				if ( ! XmlRpcSocket.nbRead(_response, ref _eof)) 
				{
					
				}*/

				// If we haven't gotten the entire _response yet, return (keep reading)
				if (_response.Length < _contentLength) 
				{
					if (_eof) 
					{
						XmlRpcUtil.error("Error in XmlRpcClient::readResponse: EOF while reading response");
						return false;
					}
					return true;
				}
			}

			// Otherwise, parse and return the result
			XmlRpcUtil.log(3, "XmlRpcClient::readResponse (read {0} bytes)", _response.Length);
			XmlRpcUtil.log(5, "response:\n{0}", _response);

			_connectionState = ConnectionState.IDLE;

			return false;    // Stop monitoring this source (causes return from work)
		}


		// Convert the response xml into a result value
		bool parseResponse(XmlRpcValue result)
		{
			bool success = true;
			//XmlRpcValue result = null;
			using (XmlReader reader = XmlReader.Create(new StringReader(_response)))
			{
				XmlDocument response = new XmlDocument();
				response.Load(reader);
				// Parse response xml into result
				//int offset = 0;
				XmlNodeList resp = response.GetElementsByTagName("methodResponse");
				XmlNode responseNode = resp[0];

				//if (!XmlRpcUtil.findTag(METHODRESPONSE_TAG, _response, out offset))
				if (resp.Count == 0)
				{
					XmlRpcUtil.error("Error in XmlRpcClient::parseResponse: Invalid response - no methodResponse. Response:\n{0}", _response);
					return false;
				}

				XmlElement pars = responseNode["params"];
				XmlElement fault = responseNode["fault"];
				//result = new XmlRpcValue();
				if (pars != null && !result.fromXml(pars))
				{
					success = false;
				}
				else if (fault != null && result.fromXml(fault))
				{
					success = false;
				}
				else
				{
					XmlRpcUtil.error("Error in XmlRpcClient::parseResponse: Invalid response - no param or fault tag. Response:\n{0}", _response);
				}
				_response = "";
				/*
				// Expect either <params><param>... or <fault>...
				if ((XmlRpcUtil.nextTagIs(PARAMS_TAG, _response, out offset) &&
					XmlRpcUtil.nextTagIs(PARAM_TAG, _response, out offset)) ||
					(XmlRpcUtil.nextTagIs(FAULT_TAG, _response, out offset) && (_isFault = true)))
				{
					if (!result.fromXml(resp[0]))
					{
						XmlRpcUtil.error("Error in XmlRpcClient::parseResponse: Invalid response value. Response:\n{0}", _response);
						_response = "";
						return false;
					}
				}
				else
				{
					XmlRpcUtil.error("Error in XmlRpcClient::parseResponse: Invalid response - no param or fault tag. Response:\n{0}", _response);
					_response = "";
					return false;
				}
				_response = "";*/
				//return result.valid();
			}
			return success;
		}
    }
}