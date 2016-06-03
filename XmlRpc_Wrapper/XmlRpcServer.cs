// File: XmlRpcServer.cs
// Project: XmlRpc_Wrapper
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 03/16/2016
// Updated: 03/17/2016

#region USINGZ

//#define REFDEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Xml;

#endregion

namespace XmlRpc_Wrapper
{
#if !TRACE
    [DebuggerStepThrough]
#endif

    public class XmlRpcServer : XmlRpcSource
    {
        public void Shutdown()
        {
            _disp.Clear();
        }

        private static string SYSTEM_MULTICALL = "system.multicall";
        private static string METHODNAME = "methodName";
        private static string PARAMS = "params";

        private static string FAULTCODE = "faultCode";
        private static string FAULTSTRING = "faultString";
        private static string LIST_METHODS = "system.listMethods";
        private static string METHOD_HELP = "system.methodHelp";
        private static string MULTICALL = "system.multicall";
        private XmlRpcDispatch _disp = new XmlRpcDispatch();
        // Whether the introspection API is supported by this server
        private bool _introspectionEnabled;
        private XmlRpcServerMethod _listMethods;
        private XmlRpcServerMethod _methodHelp;
        private Dictionary<string, XmlRpcServerMethod> _methods = new Dictionary<string, XmlRpcServerMethod>();
        private int _port;
        private TcpListener listener;
        private AutoResetEvent accept_token = new AutoResetEvent(true);

        public int Port
        {
            get { return _port; }
        }

        public XmlRpcDispatch Dispatch
        {
            get { return _disp; }
        }

        public void AddMethod(XmlRpcServerMethod method)
        {
            _methods.Add(method.name, method);
        }

        public void RemoveMethod(XmlRpcServerMethod method)
        {
            foreach (var rec in _methods)
            {
                if (method == rec.Value)
                {
                    _methods.Remove(rec.Key);
                    break;
                }
            }
        }

        public void RemoveMethod(string name)
        {
            _methods.Remove(name);
        }

        public void Work(double msTime)
        {
            _disp.Work(msTime);
        }

        public void Exit()
        {
            _disp.Exit();
        }

        public XmlRpcServerMethod FindMethod(string name)
        {
            if (_methods.ContainsKey(name))
                return _methods[name];
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
            IPAddress address = new IPAddress(0); // INADDR_ANY
            try
            {
                _port = port;
                listener = new TcpListener(address, port);
                listener.Start(backlog);
                _port = ((IPEndPoint)listener.Server.LocalEndPoint).Port;
                _disp.AddSource(this, XmlRpcDispatch.EventType.ReadableEvent);
                //listener.BeginAcceptTcpClient(new AsyncCallback(acceptClient), listener);
                XmlRpcUtil.log(XmlRpcUtil.XMLRPC_LOG_LEVEL.WARNING, "XmlRpcServer::bindAndListen: server listening on port {0}", _port);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return true;
        }

        // Handle input on the server socket by accepting the connection
        // and reading the rpc request.
        public override XmlRpcDispatch.EventType HandleEvent(XmlRpcDispatch.EventType eventType)
        {
            acceptConnection();
            return XmlRpcDispatch.EventType.ReadableEvent; // Continue to monitor this fd
        }

        // Accept a client connection request and create a connection to
        // handle method calls from the client.
        private void acceptConnection()
        {
            bool p = true;
// ReSharper disable once CSharpWarnings::CS0665
            while (p = listener.Pending())
            {
                try
                {
                    _disp.AddSource(createConnection(listener.AcceptSocket()), XmlRpcDispatch.EventType.ReadableEvent);
                    XmlRpcUtil.log(XmlRpcUtil.XMLRPC_LOG_LEVEL.WARNING, "XmlRpcServer::acceptConnection: creating a connection");
                }
                catch (SocketException ex)
                {
                    XmlRpcUtil.error("XmlRpcServer::acceptConnection: Could not accept connection ({0}).", ex.Message);
                    Thread.Sleep(10);
                }
            }
        }

        // Create a new connection object for processing requests from a specific client.
        private XmlRpcServerConnection createConnection(Socket s)
        {
            // Specify that the connection object be deleted when it is closed
            return new XmlRpcServerConnection(s, this, true);
        }

        public void removeConnection(XmlRpcServerConnection sc)
        {
            _disp.RemoveSource(sc);
        }


        // Stop processing client requests
        private void exit()
        {
            _disp.Exit();
        }


        // Close the server socket file descriptor and stop monitoring connections
        private void shutdown()
        {
            // This closes and destroys all connections as well as closing this socket
            _disp.Clear();
        }


        // Introspection support


        // Specify whether introspection is enabled or not. Default is enabled.
        public void enableIntrospection(bool enabled)
        {
            if (_introspectionEnabled == enabled)
                return;

            _introspectionEnabled = enabled;

            if (enabled)
            {
                if (_listMethods == null)
                {
                    _listMethods = new ListMethods(this);
                    _methodHelp = new MethodHelp(this);
                }
                else
                {
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


        private void listMethods(XmlRpcValue result)
        {
            int i = 0;
            result.SetArray(_methods.Count + 1);

            foreach (var rec in _methods)
            {
                result.Set(i++, rec.Key);
            }

            // Multicall support is built into XmlRpcServerConnection
            result.Set(i, MULTICALL);
        }

        // Run the method, generate _response string
        public string executeRequest(string _request)
        {
            string _response = "";
            XmlRpcValue parms = new XmlRpcValue(), resultValue = new XmlRpcValue();
            string methodName = parseRequest(parms, _request);
            XmlRpcUtil.log(XmlRpcUtil.XMLRPC_LOG_LEVEL.WARNING, "XmlRpcServerConnection::executeRequest: server calling method '{0}'", methodName);

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
                XmlRpcUtil.log(XmlRpcUtil.XMLRPC_LOG_LEVEL.WARNING, "XmlRpcServerConnection::executeRequest: fault {0}.", fault.Message);
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
            XmlRpcUtil.log(XmlRpcUtil.XMLRPC_LOG_LEVEL.SPEW, "XmlRpcServerConnection::generateResponse:\n{0}\n", result);
            return result;
        }

        // Parse the method name and the argument values from the request.
        private string parseRequest(XmlRpcValue parms, string _request)
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
                XmlNodeList xmlFault = xmldoc.GetElementsByTagName("fault");
                if (xmlParameters.Count == 0)
                {
                    XmlRpcUtil.error("Error in XmlRpcServer::parseRequest: Invalid request - no methodResponse. Request:\n{0}", _request);
                    return null;
                }

                parms.SetArray(xmlParameters.Count);

                for (int i = 0; i < xmlParameters.Count; i++)
                {
                    var value = new XmlRpcValue();
                    value.fromXml(xmlParameters[i]["value"]);
                    parms.asArray[i] = value;
                }

                if (xmlFault.Count > 0 && parms.fromXml(xmlFault[0]))
                {
                    XmlRpcUtil.log(XmlRpcUtil.XMLRPC_LOG_LEVEL.WARNING, "Read fault on response for request:\n{0}\nFAULT: {1}", _request, parms.ToString());
                }
            }

            return methodName;
        }

        // Prepend http headers
        private string generateHeader(string body)
        {
            return string.Format(
                "HTTP/1.1 200 OK\r\n" +
                "Server: {0}\r\n" +
                "Content-Type: text/xml\r\n" +
                "Content-length: {1}\r\n\r\n",
                XmlRpcUtil.XMLRPC_VERSION,
                body.Length);
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

        private class ListMethods : XmlRpcServerMethod
        {
            public
                ListMethods(XmlRpcServer s)
                : base(LIST_METHODS, null, s)
            {
                FUNC = execute;
            }

            private void execute(XmlRpcValue parms, XmlRpcValue result)
            {
                server.listMethods(result);
            }

            private string help()
            {
                return "List all methods available on a server as an array of strings";
            }
        };


        // Retrieve the help string for a named method
        private class MethodHelp : XmlRpcServerMethod
        {
            public MethodHelp(XmlRpcServer s)
                : base(METHOD_HELP, null, s)
            {
                FUNC = execute;
            }

            private void execute(XmlRpcValue parms, XmlRpcValue result)
            {
                if (parms[0].Type != XmlRpcValue.ValueType.TypeString)
                    throw new XmlRpcException(METHOD_HELP + ": Invalid argument type");

                XmlRpcServerMethod m = server.FindMethod(parms[0].GetString());
                if (m == null)
                    throw new XmlRpcException(METHOD_HELP + ": Unknown method name");

                result.Set(m.Help());
            }

            public override string Help()
            {
                return ("Retrieve the help string for a named method");
            }
        };
    }
}