using System;
using System.Collections.Generic;
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

		Socket socket;

		NetworkStream stream;
		StreamReader reader;
		StreamWriter writer;

		// The server delegates handling client requests to a serverConnection object.
		public XmlRpcServerConnection(Socket fd, XmlRpcServer server, bool deleteOnClose /*= false*/) 
		//: base(fd, deleteOnClose)
		{
			XmlRpcUtil.log(2,"XmlRpcServerConnection: new socket %d.", fd);
			this.server = server;
			this.socket = fd;
			//this.socket.Blocking = false;
			stream = new NetworkStream(socket);
			reader = new StreamReader(stream);
			writer = new StreamWriter(stream);
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

			char[] data = new char[1024];
			int dataLen = 0;
			//string data = "";
			try{
				//using(StreamReader reader = new StreamReader(stream))
				{
					if (reader.Peek() > 0)
					{
						dataLen = reader.Read(data, 0, data.Length);
						//data = reader.Read();
					}
					else
					{
						return false;
					}
				}
			}
			catch(Exception ex)
			{ 
				XmlRpcUtil.error("XmlRpcServerConnection::readHeader: error while reading header ({0}).",ex.Message);
			}
			if (dataLen > 0)
			{
				System.Text.StringBuilder sb = new StringBuilder();
				sb.Append(data, 0, dataLen);
				_header += sb;
			}
			if (_header.Length < 10)
				return false;
			HTTPHeader header = new HTTPHeader(_header);

			if (header.IndexHeaderEnd == 0)
				return false;
			
			if(int.TryParse(header.HTTPField[(int)HTTPHeaderField.Content_Length], out this._contentLength))
			{
				_request = _header.Substring(header.IndexHeaderEnd+4);
			}

			
			/*
			if ( ! XmlRpcSocket::nbRead(this->getfd(), _header, &eof)) {
			// Its only an error if we already have read some data
			if (_header.Length > 0)
				XmlRpcUtil.error("XmlRpcServerConnection::readHeader: error while reading header (%s).",XmlRpcSocket.getErrorMsg().c_str());
			return false;
			}*/

			/*


			XmlRpcUtil.log(4, "XmlRpcServerConnection::readHeader: read {0} bytes.", data.Length);
			int hp = (char*)_header.c_str();  // Start of header
			int ep = hp + _header.Length;   // End of string
			int bp = 0;                       // Start of body
			int lp = 0;                       // Start of content-length value
			int kp = 0;                       // Start of connection value
			char *hp = (char*)_header.c_str();  // Start of header
			char *ep = hp + _header.Length;   // End of string
			char *bp = 0;                       // Start of body
			char *lp = 0;                       // Start of content-length value
			char *kp = 0;                       // Start of connection value
			for(int i = 0; i < _header.Length; i++)
			{
				_header.IndexOf(i, 
				for (char *cp = hp; (bp == 0) && (cp < ep); ++cp) 
				{
				if ((ep - cp > 16) && (strncasecmp(cp, "Content-length: ", 16) == 0))
					lp = cp + 16;
				else if ((ep - cp > 12) && (strncasecmp(cp, "Connection: ", 12) == 0))
					kp = cp + 12;
				else if ((ep - cp > 4) && (strncmp(cp, "\r\n\r\n", 4) == 0))
					bp = cp + 4;
				else if ((ep - cp > 2) && (strncmp(cp, "\n\n", 2) == 0))
					bp = cp + 2;
				}
			}

			// If we haven't gotten the entire header yet, return (keep reading)
			if (bp == 0) {
				// EOF in the middle of a request is an error, otherwise its ok
				if (eof) {
					XmlRpcUtil.log(4, "XmlRpcServerConnection::readHeader: EOF");
					if (_header.Length > 0)
					XmlRpcUtil.error("XmlRpcServerConnection::readHeader: EOF while reading header");
					return false;   // Either way we close the connection
				}
    
				return true;  // Keep reading
			}

			// Decode content length
			if (lp == 0) {
				XmlRpcUtil.error("XmlRpcServerConnection::readHeader: No Content-length specified");
				return false;   // We could try to figure it out by parsing as we read, but for now...
			}

			_contentLength = atoi(lp);
			if (_contentLength <= 0) {
				XmlRpcUtil.error("XmlRpcServerConnection::readHeader: Invalid Content-length specified (%d).", _contentLength);
				return false;
			}
  	
			XmlRpcUtil.log(3, "XmlRpcServerConnection::readHeader: specified content length is %d.", _contentLength);

			// Otherwise copy non-header data to request buffer and set state to read request.
			_request = bp;

			// Parse out any interesting bits from the header (HTTP version, connection)
			_keepAlive = true;

			if(_header.IndexOf("HTTP/1.0") != -1)
			{

			}
			if (_header.find("HTTP/1.0") != std::string::npos) 
			{
				if (kp == 0 || strncasecmp(kp, "keep-alive", 10) != 0)
					_keepAlive = false;           // Default for HTTP 1.0 is to close the connection
			} else {
				if (kp != 0 && strncasecmp(kp, "close", 5) == 0)
					_keepAlive = false;
			}
			*/
			XmlRpcUtil.log(3, "KeepAlive: {0}", _keepAlive);


			_header = ""; 
			_connectionState = ServerConnectionState.READ_REQUEST;
			return true;    // Continue monitoring this source
		}

		bool readRequest()
		{
			/*
			// If we dont have the entire request yet, read available data
			if (_request.Length < _contentLength) {
				bool eof;
				if ( ! XmlRpcSocket::nbRead(this->getfd(), _request, &eof)) {
					XmlRpcUtil.error("XmlRpcServerConnection::readRequest: read error (%s).",XmlRpcSocket::getErrorMsg().c_str());
					return false;
				}

				// If we haven't gotten the entire request yet, return (keep reading)
				if (_request.Length < _contentLength) {
					if (eof) {
					XmlRpcUtil.error("XmlRpcServerConnection::readRequest: EOF while reading request");
					return false;   // Either way we close the connection
					}
					return true;
				}
			}
			*/
			int left = this._contentLength - _request.Length;
			if (left > 0)
			{
				char[] data = new char[left];
				try
				{
					{
						if (reader.Peek() > 0)
						{
							int res = reader.Read(data, 0, left);
							//data = reader.ReadToEnd();
						}
						else
						{
							return false;
						}
					}
				}
				catch (Exception ex)
				{
					XmlRpcUtil.error("XmlRpcServerConnection::readHeader: error while reading header ({0}).", ex.Message);
					return false;
				}
				_request += data.ToString() ;
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
				//using (StreamWriter writer = new StreamWriter(stream))
				{
				
						// Try to write the response
						writer.Write(response);
						writer.Flush();
						_bytesWritten = response.Length;
				
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
			return this.socket;
		}
	}
}
