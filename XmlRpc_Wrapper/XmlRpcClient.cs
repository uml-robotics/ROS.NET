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

namespace XmlRpc_Wrapper
{
    [DebuggerStepThrough]
    public class XmlRpcClient : XmlRpcSource //: IDisposable
    {
		TcpClient socket;

		// Static data
		static string REQUEST_BEGIN = "<?xml version=\"1.0\"?>\r\n<methodCall><methodName>";
		static string REQUEST_END_METHODNAME = "</methodName>\r\n";
		static string PARAMS_TAG = "<params>";
		static string PARAMS_ETAG = "</params>";
		static string PARAM_TAG = "<param>";
		static string PARAM_ETAG = "</param>";
		static string REQUEST_END = "</methodCall>\r\n";
		
        public string HostUri = "";

        [DebuggerStepThrough]
        public XmlRpcClient(string HostName, int Port, string Uri)
        {
			Initialize(HostName, Port, Uri);
			//socket.GetStream();
        }

        [DebuggerStepThrough]
        public XmlRpcClient(string HostName, int Port)
            : this(HostName, Port, "/")
        {
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

			Initialize(hn, p, u);
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

        #endregion

        #region public function passthroughs

        public bool CheckIdentity(string host, int port, string uri)
        {
			return this._host.Equals(host) && this._port == port && this._uri.Equals(uri);// checkident(instance, host, port, uri);
        }

		// Execute the named procedure on the remote server.
		// Params should be an array of the arguments for the method.
		// Returns true if the request was sent and a result received (although the result
		// might be a fault).
        public bool Execute(string method, XmlRpcValue parameters, XmlRpcValue result)
        {
			XmlRpcUtil.log(1, "XmlRpcClient::Execute: method {0} (_connectionState {0}).", method, _connectionState);
			lock (this)
			{
				//result = null;
				// This is not a thread-safe operation, if you want to do multithreading, use separate
				// clients for each thread. If you want to protect yourself from multiple threads
				// accessing the same client, replace this code with a real mutex.
				if (_executing)
					return false;

				_executing = true;
				//ClearFlagOnExit cf(_executing);

				_sendAttempts = 0;
				_isFault = false;

				if (!setupConnection())
				{
					_executing = false;
					return false;
				}

				if (!generateRequest(method, parameters))
				{
					_executing = false;
					return false;
				}

				double msTime = -1.0;
				this._disp.Work(msTime);

				if (_connectionState != ConnectionState.IDLE || !parseResponse(result, _response))
				{
					_executing = false;
					return false;
				}

				XmlRpcUtil.log(1, "XmlRpcClient::execute: method {0} completed.", method);
				_response = "";
				_executing = false;
			}
			_executing = false;
			return true;
        }

		// Execute the named procedure on the remote server, non-blocking.
		// Params should be an array of the arguments for the method.
		// Returns true if the request was sent and a result received (although the result
		// might be a fault).
        public bool ExecuteNonBlock(string method, XmlRpcValue parameters)
        {
			XmlRpcUtil.log(1, "XmlRpcClient::ExecuteNonBlock: method {0} (_connectionState {0}.", method, _connectionState);

			// This is not a thread-safe operation, if you want to do multithreading, use separate
			// clients for each thread. If you want to protect yourself from multiple threads
			// accessing the same client, replace this code with a real mutex.

			XmlRpcValue result = new XmlRpcValue();
			if (_executing)
				return false;

			_executing = true;

			_sendAttempts = 0;
			_isFault = false;

			if (!setupConnection())
			{
				_executing = false;
				return false;
			}

			if (!generateRequest(method, parameters))
			{
				_executing = false;
				return false;
			}

			_executing = false;
			return true;
        }

        public bool ExecuteCheckDone(XmlRpcValue result)
        {
			//result.clear();
			// Are we done yet?
			if (_connectionState != ConnectionState.IDLE)
				return false;
			if (!parseResponse(result, _response))
			{
				// Hopefully the caller can determine that parsing failed.
			}
			XmlRpcUtil.log(1, "XmlRpcClient::execute: method completed.");
			_response = "";
			return true;
        }
		
        #endregion

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

		//HttpWebRequest webRequester;

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
		XmlRpcDispatch _disp = new XmlRpcDispatch();

		public override void Close()
		{
			XmlRpcUtil.log(1, "XmlRpcClient::Close()");
		}

		public override Socket getSocket()
		{
			return this.socket != null ? this.socket.Client : null;
		}
		//done and works
		void Initialize(string host, int port, string uri/*=0*/)
		{
			XmlRpcUtil.log(1, "XmlRpcClient new client: host {0}, port {1}.", host, port);

			_host = host;
			_port = port;
			if (uri != null)
				_uri = uri;
			else
				_uri = "/RPC2";

			_connectionState = ConnectionState.CONNECTING;
			_executing = false;
			_eof = false;
			
			if (doConnect())
			{
				_connectionState = ConnectionState.IDLE;
			}

			// Default to keeping the connection open until an explicit close is done
			setKeepOpen();
		}

		// Close the owned fd
		public void close()
		{
			XmlRpcUtil.log(4, "XmlRpcClient::close.");
			_connectionState = ConnectionState.NO_CONNECTION;
		
			_disp.Exit();
			//_disp.removeSource(this);
			//XmlRpcSource::close();
			if(socket != null)
			{
				socket.Close();
				//reader = null;
				//writer = null;
			}
		}

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
			if (socket == null)
			{
				try
				{
					socket = new TcpClient(_host, _port);
				}
				catch (SocketException ex)
				{
					return false;
				}
			}
			if (!socket.Connected)
			{
				this.close();
				XmlRpcUtil.error("Error in XmlRpcClient::doConnect: Could not connect to server ({0}).", this.getSocketError());
				return false;
			}
			else
			{
				//writer = new StreamWriter(socket.GetStream());
				//reader = new StreamReader(socket.GetStream());
			}
			return true;
		}

		public void Shutdown()
		{
			Close();
		}

		string generateRequestStr(string methodName, XmlRpcValue parameters)
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
					for (int i = 0; i < parameters.Length; ++i)
					{
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
			return body;
		}
		// Encode the request to call the specified method with the specified parameters into xml
		bool generateRequest(string methodName, XmlRpcValue parameters)
		{
			string body = generateRequestStr(methodName, parameters);

			string header = generateHeader(body);
			XmlRpcUtil.log(4, "XmlRpcClient::generateRequest: header is {0} bytes, content-length is {1}.", header.Length, body.Length);

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

			string buff = String.Format(":{0}\r\n", _port);
			//sprintf(buff,":%d\r\n", _port);


			header += buff;
			header += "Content-Type: text/xml\r\nContent-length: ";
			buff = String.Format("{0}\r\n\r\n", body.Length);

			return header + buff;
		}

		bool writeRequest()
		{
			if (_bytesWritten == 0)
				XmlRpcUtil.log(5, "XmlRpcClient::writeRequest (attempt {0}):\n{1}\n", _sendAttempts+1, _request);
			// Try to write the request
			try
			{
				if (!socket.Connected)
					XmlRpcUtil.error("XmlRpcClient::writeRequest not connected");
				MemoryStream memstream = new MemoryStream();
				using (StreamWriter writer = new StreamWriter(memstream))
				{
					writer.Write(_request);
					_bytesWritten = _request.Length;
				}
				var stream = socket.GetStream();
				try
				{
					var buffer = memstream.GetBuffer();
					stream.Write(buffer, 0, buffer.Length);
				}
				catch (Exception ex)
				{
					Debug.WriteLine(string.Format("Exception while writing request: {0}", ex.Message));
				}
				_bytesWritten = _request.Length;
			}
			catch(System.IO.IOException ex)
			{	
				XmlRpcUtil.error("Error in XmlRpcClient::writeRequest: write error ({0}).",ex.Message);
				return false;
			}
    
			XmlRpcUtil.log(3, "XmlRpcClient::writeRequest: wrote {0} of {1} bytes.", _bytesWritten, _request.Length);

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

		bool readHeader()
		{
			// Read available data
			bool eof;

			byte[] data = new byte[1024];
			int dataLen = 0;
			//string data = "";
			try
			{
				var stream = socket.GetStream();
				dataLen = stream.Read(data, 0, 1024);
			}
			catch (SocketException ex)
			{
				XmlRpcUtil.error("XmlRpcServerConnection::readHeader: error while reading header ({0}).", ex.Message);
				return false;
			}
			catch (Exception ex)
			{
				XmlRpcUtil.error("XmlRpcServerConnection::readHeader: error while reading header ({0}).", ex.Message);
				return false;
			}
			/// If it is disconnect
			if (dataLen == 0)
				return false;
			if (dataLen > 0)
			{
				_header += System.Text.Encoding.Default.GetString(data, 0, dataLen);
			}
			if (_header.Length < 10)
				return false;
			HTTPHeader header = new HTTPHeader(_header);

			if (header.IndexHeaderEnd == 0)
				return false;
			var value = header.HTTPField[(int)HTTPHeaderField.Content_Length];
			if (int.TryParse(value, out this._contentLength))
			{
				_response = _header.Substring(header.IndexHeaderEnd + 4);
			}

			//XmlRpcUtil.log(3, "KeepAlive: {0}", _keepAlive);
			_header = "";
			_connectionState = ConnectionState.READ_RESPONSE;
			return true;    // Continue monitoring this source
		}

		bool readResponse()
		{
			if (this._response == null)
				this._response = "";
			int left = this._contentLength - _response.Length;
			int dataLen = 0;
			if (left > 0)
			{
				byte[] data = new byte[left];
				try
				{
					var stream = socket.GetStream();
					dataLen = stream.Read(data, 0, left);
					if (dataLen == 0)
					{
						Debug.WriteLine("XmlRpcClient::readResponse: Stream was closed");
						return false;
					}
				}
				catch (Exception ex)
				{
					XmlRpcUtil.error("XmlRpcClient::readResponse: error while reading the rest of data ({0}).", ex.Message);
					return false;
				}
				_response += System.Text.Encoding.Default.GetString(data, 0, dataLen);
			}
			// Otherwise, parse and dispatch the request
			XmlRpcUtil.log(3, "XmlRpcClient::readResponse read {0} bytes.", _request.Length);

			_connectionState = ConnectionState.IDLE;

			return false;    // Continue monitoring this source
		}

		// Convert the response xml into a result value
		bool parseResponse(XmlRpcValue result, string _response)
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
				if (pars != null)
				{
					bool isArray = false;
					var selection = pars.SelectNodes("param");
					if (selection.Count > 1)
					{
						result.SetArray(selection.Count);
						int i = 0;
						foreach (XmlNode par in selection)
						{
							var value = new XmlRpcValue();
							value.fromXml(par["value"]);
							result[i++] = value;
						}
					}
					else if(selection.Count == 1)
					{
						result.fromXml(selection[0]["value"]);
					}
					else
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
			}
			return success;
		}
    }
}