using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml;

namespace XmlRpc
{
	
	public class XmlRpcServerConnection : XmlRpcSource
	{
		// Static data
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
		XmlRpcServer server;

		 // The XmlRpc server that accepted this connection

		// Possible IO states for the connection
		enum ServerConnectionState 
		{ 
			READ_HEADER, 
			READ_REQUEST,
			WRITE_RESPONSE 
		};
		ServerConnectionState _connectionState;

		// Request headers
		string _header;

		// Number of bytes expected in the request body (parsed from header)
		int _contentLength;

		char[] _rawRequest;

		// Request body
		string _request;

		// Response
		//string _response;

		// Number of bytes of the response written so far
		int _bytesWritten;

		// Whether to keep the current client connection open for further requests
		bool _keepAlive;

		TcpClient socket;
		
		// The server delegates handling client requests to a serverConnection object.
		public XmlRpcServerConnection(TcpClient fd, XmlRpcServer server, bool deleteOnClose /*= false*/) 
		//: base(fd, deleteOnClose)
		{
			XmlRpcUtil.log(2,"XmlRpcServerConnection: new socket %d.", fd);
			this.server = server;
			this.socket = fd;
			_connectionState = ServerConnectionState.READ_HEADER;
			this.KeepOpen = true;
			_keepAlive = true;
		}


		~XmlRpcServerConnection()
		{
			XmlRpcUtil.log(4,"XmlRpcServerConnection dtor.");
			server.removeConnection(this);
		}

		// Handle input on the server socket by accepting the connection
		// and reading the rpc request. Return true to continue to monitor
		// the socket for events, false to remove it from the dispatcher.
		public override XmlRpcDispatch.EventType HandleEvent(XmlRpcDispatch.EventType eventType)
		//unsigned handleEvent(unsigned /*eventType*/)
		{
			if (_connectionState == ServerConnectionState.READ_HEADER)
				if ( ! readHeader()) return 0;
			if (_connectionState == ServerConnectionState.READ_REQUEST)
				if ( ! readRequest()) return 0;

			if (_connectionState == ServerConnectionState.WRITE_RESPONSE)
				if ( ! writeResponse(_request)) return 0;

			return (_connectionState == ServerConnectionState.WRITE_RESPONSE) 
				? XmlRpcDispatch.EventType.WritableEvent : XmlRpcDispatch.EventType.ReadableEvent;
		}

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
			if(int.TryParse(value, out this._contentLength))
			{
				_request = _header.Substring(header.IndexHeaderEnd+4);
			}
		
			XmlRpcUtil.log(3, "KeepAlive: {0}", _keepAlive);
			_header = ""; 
			_connectionState = ServerConnectionState.READ_REQUEST;
			return true;    // Continue monitoring this source
		}

		public override void Close()
		{
			XmlRpcUtil.log(3, "XmlRpcServerConnection is closing");
			if (this.socket != null)
			{
				this.socket.Close();
				this.socket = null;
			}
		}

		bool readRequest()
		{
			if (this._request == null)
				this._request = "";
			int left = this._contentLength - _request.Length;
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
						Debug.WriteLine("XmlRpcServerConnection::readRequest: Stream was closed");
						return false;
					}
				}
				catch (Exception ex)
				{
					XmlRpcUtil.error("XmlRpcServerConnection::readRequest: error while reading the rest of data ({0}).", ex.Message);
					return false;
				}
				_request += System.Text.Encoding.Default.GetString(data, 0, dataLen);
			}
			// Otherwise, parse and dispatch the request
			XmlRpcUtil.log(3, "XmlRpcServerConnection::readRequest read {0} bytes.", _request.Length);

			_connectionState = ServerConnectionState.WRITE_RESPONSE;

			return true;    // Continue monitoring this source
		}

		bool writeResponse(string request)
		{
			string response = server.executeRequest(request);
			if (response.Length == 0) 
			{
				XmlRpcUtil.error("XmlRpcServerConnection::writeResponse: empty response.");
				return false;
			}
			try
			{
				MemoryStream memstream = new MemoryStream();
				using (StreamWriter writer = new StreamWriter(memstream))
				{
					writer.Write(response);
					_bytesWritten = response.Length;
				}
				var stream = socket.GetStream();
				try
				{
					var buffer = memstream.GetBuffer();
					stream.Write(buffer, 0, buffer.Length);
				}
				catch (Exception ex)
				{
					Debug.WriteLine(string.Format("Exception while writing response: {0}", ex.Message));
				}
			}
			catch (Exception ex)
			{
				XmlRpcUtil.error("XmlRpcServerConnection::writeResponse: write error ({0}).", ex.Message);
				return false;
			}
			XmlRpcUtil.log(3, "XmlRpcServerConnection::writeResponse: wrote {0} of {0} bytes.", _bytesWritten, response.Length);

			// Prepare to read the next request
			if (_bytesWritten == response.Length) 
			{
				_header = "";
				_request = "";
				response = "";
				_connectionState = ServerConnectionState.READ_HEADER;
			}

			return _keepAlive;    // Continue monitoring this source if true
		}

		public override Socket getSocket()
		{
			return this.socket.Client;
		}
	}
}
