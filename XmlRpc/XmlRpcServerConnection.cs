using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace XmlRpc
{
	class XmlRpcServerConnection : XmlRpcSource
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

		HttpListener httpListener;
		 // The XmlRpc server that accepted this connection

		// Possible IO states for the connection
		enum ServerConnectionState { READ_HEADER, READ_REQUEST, WRITE_RESPONSE };
		ServerConnectionState _connectionState;

		// Request headers
		string _header;

		// Number of bytes expected in the request body (parsed from header)
		int _contentLength;

		// Request body
		string _request;

		// Response
		string _response;

		// Number of bytes of the response written so far
		int _bytesWritten;

		// Whether to keep the current client connection open for further requests
		bool _keepAlive;

		// The server delegates handling client requests to a serverConnection object.
		public XmlRpcServerConnection(Socket fd, XmlRpcServer server, bool deleteOnClose /*= false*/) 
		//: base(fd, deleteOnClose)
		{
			XmlRpcUtil.log(2,"XmlRpcServerConnection: new socket %d.", fd);
			this.server = server;
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
				if ( ! writeResponse()) return 0;

			return (_connectionState == ServerConnectionState.WRITE_RESPONSE) 
				? XmlRpcDispatch.EventType.WritableEvent : XmlRpcDispatch.EventType.ReadableEvent;
		}


bool readHeader()
{
  // Read available data
  bool eof;
  if ( ! XmlRpcSocket::nbRead(this->getfd(), _header, &eof)) {
    // Its only an error if we already have read some data
    if (_header.Length > 0)
      XmlRpcUtil.error("XmlRpcServerConnection::readHeader: error while reading header (%s).",XmlRpcSocket.getErrorMsg().c_str());
    return false;
  }

  XmlRpcUtil.log(4, "XmlRpcServerConnection::readHeader: read %d bytes.", _header.Length);
  char *hp = (char*)_header.c_str();  // Start of header
  char *ep = hp + _header.Length;   // End of string
  char *bp = 0;                       // Start of body
  char *lp = 0;                       // Start of content-length value
  char *kp = 0;                       // Start of connection value

  for (char *cp = hp; (bp == 0) && (cp < ep); ++cp) {
	if ((ep - cp > 16) && (strncasecmp(cp, "Content-length: ", 16) == 0))
	  lp = cp + 16;
	else if ((ep - cp > 12) && (strncasecmp(cp, "Connection: ", 12) == 0))
	  kp = cp + 12;
	else if ((ep - cp > 4) && (strncmp(cp, "\r\n\r\n", 4) == 0))
	  bp = cp + 4;
	else if ((ep - cp > 2) && (strncmp(cp, "\n\n", 2) == 0))
	  bp = cp + 2;
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
  if (_header.find("HTTP/1.0") != std::string::npos) {
    if (kp == 0 || strncasecmp(kp, "keep-alive", 10) != 0)
      _keepAlive = false;           // Default for HTTP 1.0 is to close the connection
  } else {
    if (kp != 0 && strncasecmp(kp, "close", 5) == 0)
      _keepAlive = false;
  }
  XmlRpcUtil.log(3, "KeepAlive: %d", _keepAlive);


  _header = ""; 
  _connectionState = ServerConnectionState.READ_REQUEST;
  return true;    // Continue monitoring this source
}

bool readRequest()
{
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

  // Otherwise, parse and dispatch the request
  XmlRpcUtil.log(3, "XmlRpcServerConnection::readRequest read %d bytes.", _request.Length);
  //XmlRpcUtil.log(5, "XmlRpcServerConnection::readRequest:\n%s\n", _request.c_str());

  _connectionState = ServerConnectionState.WRITE_RESPONSE;

  return true;    // Continue monitoring this source
}


bool writeResponse()
{
  if (_response.Length == 0) {
    executeRequest();
    _bytesWritten = 0;
    if (_response.Length == 0) {
      XmlRpcUtil.error("XmlRpcServerConnection::writeResponse: empty response.");
      return false;
    }
  }

  // Try to write the response
  if ( ! XmlRpcSocket.nbWrite(socket, _response, &_bytesWritten)) {
    XmlRpcUtil.error("XmlRpcServerConnection::writeResponse: write error (%s).",XmlRpcSocket.getErrorMsg());
    return false;
  }
  XmlRpcUtil.log(3, "XmlRpcServerConnection::writeResponse: wrote %d of %d bytes.", _bytesWritten, _response.Length);

  // Prepare to read the next request
  if (_bytesWritten == int(_response.Length)) {
    _header = "";
    _request = "";
    _response = "";
    _connectionState = ServerConnectionState.READ_HEADER;
  }

  return _keepAlive;    // Continue monitoring this source if true
}

// Run the method, generate _response string
void executeRequest()
{
  XmlRpcValue parms = new XmlRpcValue(), resultValue = new XmlRpcValue();
  string methodName = parseRequest(parms);
  XmlRpcUtil.log(2, "XmlRpcServerConnection::executeRequest: server calling method '{0}'", methodName);

  try {

    if ( ! executeMethod(methodName, parms, resultValue) &&
         ! executeMulticall(methodName, parms, resultValue))
      generateFaultResponse(methodName + ": unknown method name");
    else
      generateResponse(resultValue.toXml());

  } catch (XmlRpcException fault) {
    XmlRpcUtil.log(2, "XmlRpcServerConnection::executeRequest: fault {0}.", fault.Message); 
    generateFaultResponse(fault.Message, fault.getCode());
  }
}

// Parse the method name and the argument values from the request.
string parseRequest(XmlRpcValue parms)
{
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

  return methodName;
}

// Execute a named method with the specified params.
bool executeMethod(string methodName, XmlRpcValue parms, XmlRpcValue result)
{
  XmlRpcServerMethod method = server.FindMethod(methodName);

  if ( method == null) return false;

  method.Execute(parms, result);

  // Ensure a valid result value
  if ( ! result.Valid)
      result.Set("");

  return true;
}

// Execute multiple calls and return the results in an array.
bool executeMulticall(string methodNameRoot, XmlRpcValue parms, XmlRpcValue result)
{
	if (methodNameRoot != SYSTEM_MULTICALL) return false;

  // There ought to be 1 parameter, an array of structs
  if (parms.Length != 1 || parms[0].Type != XmlRpcValue.ValueType.TypeArray)
    throw new XmlRpcException(SYSTEM_MULTICALL + ": Invalid argument (expected an array)");

  int nc = parms[0].Length;
  result.SetArray(nc);

  for (int i=0; i<nc; ++i) {

    if ( ! parms[0][i].hasMember(METHODNAME) ||
         ! parms[0][i].hasMember(PARAMS)) {
      result[i].Set(FAULTCODE,-1);
      result[i].Set(FAULTSTRING,SYSTEM_MULTICALL + ": Invalid argument (expected a struct with members methodName and params)");
      continue;
    }

    string methodName = parms[0][i][METHODNAME].GetString();
    XmlRpcValue methodParams = parms[0][i][PARAMS];

    XmlRpcValue resultValue = new XmlRpcValue();
    resultValue.SetArray(1);
    try {
      if ( ! executeMethod(methodName, methodParams, resultValue[0]) &&
           ! executeMulticall(methodName, parms, resultValue[0]))
      {
        result[i].Set(FAULTCODE,-1);
        result[i].Set(FAULTSTRING, methodName + ": unknown method name");
      }
      else
        result[i] = resultValue;

    } catch (XmlRpcException fault) {
        result[i].Set(FAULTCODE, 0);
        result[i].Set(FAULTSTRING,fault.Message);
    }
  }

  return true;
}


// Create a response from results xml
void generateResponse(string resultXml)
{
  string RESPONSE_1 = "<?xml version=\"1.0\"?>\r\n<methodResponse><params><param>\r\n\t";
  string RESPONSE_2 = "\r\n</param></params></methodResponse>\r\n";

  string body = RESPONSE_1 + resultXml + RESPONSE_2;
  string header = generateHeader(body);

  _response = header + body;
  XmlRpcUtil.log(5, "XmlRpcServerConnection::generateResponse:\n{0}\n", _response); 
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


		void generateFaultResponse(string errorMsg, int errorCode)
		{
		  string RESPONSE_1 = "<?xml version=\"1.0\"?>\r\n<methodResponse><fault>\r\n\t";
		  string RESPONSE_2 = "\r\n</fault></methodResponse>\r\n";

		  XmlRpcValue faultStruct = new XmlRpcValue();
		  faultStruct.Set(FAULTCODE, errorCode);
		  faultStruct.Set(FAULTSTRING, errorMsg);
		  string body = RESPONSE_1 + faultStruct.toXml() + RESPONSE_2;
		  string header = generateHeader(body);

		  _response = header + body;
		}
	}
}
