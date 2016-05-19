// File: XmlRpcServerConnection.cs
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

using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;

#endregion

namespace XmlRpc_Wrapper
{
    /// <summary>
    ///     Incoming connection to XmlRpcServer
    /// </summary>
    public class XmlRpcServerConnection : XmlRpcSource
    {
        private int _bytesWritten;
        private ServerConnectionState _connectionState;

        // Whether to keep the current client connection open for further requests
        private bool _keepAlive;

        // Request headers
        private HTTPHeader header;

        // The XmlRpc server that accepted this connection
        private XmlRpcServer server;

        private Socket socket;
        private NetworkStream stream;

        // The server delegates handling client requests to a serverConnection object.
        public XmlRpcServerConnection(Socket fd, XmlRpcServer server, bool deleteOnClose /*= false*/)
            //: base(fd, deleteOnClose)
        {
            XmlRpcUtil.log(XmlRpcUtil.XMLRPC_LOG_LEVEL.INFO, "XmlRpcServerConnection: new socket {0}.", fd.RemoteEndPoint.ToString());
            this.server = server;
            socket = fd;
            stream = new NetworkStream(socket,true);
            _connectionState = ServerConnectionState.READ_HEADER;
            KeepOpen = true;
            _keepAlive = true;
        }

        public override NetworkStream getStream()
        {
            return stream;
        }

        // Handle input on the server socket by accepting the connection
        // and reading the rpc request. Return true to continue to monitor
        // the socket for events, false to remove it from the dispatcher.
        public override XmlRpcDispatch.EventType HandleEvent(XmlRpcDispatch.EventType eventType)
        {
            if (_connectionState == ServerConnectionState.READ_HEADER)
                if (! readHeader(ref header)) return 0;
            if (_connectionState == ServerConnectionState.READ_REQUEST)
                if (! readRequest()) return 0;

            if (_connectionState == ServerConnectionState.WRITE_RESPONSE)
                if (! writeResponse(header.DataString)) return 0;

            return (_connectionState == ServerConnectionState.WRITE_RESPONSE)
                ? XmlRpcDispatch.EventType.WritableEvent : XmlRpcDispatch.EventType.ReadableEvent;
        }

        internal override bool readHeader(ref HTTPHeader header)
        {
            if (base.readHeader(ref header))
            {
                if (header.m_headerStatus == HTTPHeader.STATUS.COMPLETE_HEADER)
                {
                    XmlRpcUtil.log(XmlRpcUtil.XMLRPC_LOG_LEVEL.DEBUG, "KeepAlive: {0}", _keepAlive);
                    _connectionState = ServerConnectionState.READ_REQUEST;
                }

                return true;
            }

            return false;
        }

        public override void Close()
        {
            XmlRpcUtil.log(XmlRpcUtil.XMLRPC_LOG_LEVEL.INFO, "XmlRpcServerConnection is closing");
            if (socket != null)
            {
                socket.Close(100);
                socket = null;
            }
            server.removeConnection(this);
        }

        private bool readRequest()
        {
            int left = header.ContentLength - header.DataString.Length;
            int dataLen = 0;
            if (left > 0)
            {
                byte[] data = new byte[left];
                try
                {
                    dataLen = stream.Read(data, 0, left);
                    if (dataLen == 0)
                    {
                        XmlRpcUtil.error("XmlRpcServerConnection::readRequest: Stream was closed");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    XmlRpcUtil.error("XmlRpcServerConnection::readRequest: error while reading the rest of data ({0}).", ex.Message);
                    return false;
                }
                header.Append(Encoding.ASCII.GetString(data, 0, dataLen));
            }
            // Otherwise, parse and dispatch the request
            XmlRpcUtil.log(XmlRpcUtil.XMLRPC_LOG_LEVEL.DEBUG, "XmlRpcServerConnection::readRequest read {0} bytes.", dataLen);

            if (!header.ContentComplete)
            {
                return false;
            }
            _connectionState = ServerConnectionState.WRITE_RESPONSE;

            return true; // Continue monitoring this source
        }

        private bool writeResponse(string request)
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
                try
                {
                    var buffer = memstream.GetBuffer();
                    stream.Write(buffer, 0, buffer.Length);
                }
                catch (Exception ex)
                {
                    XmlRpcUtil.error(string.Format("Exception while writing response: {0}", ex.Message));
                }
            }
            catch (Exception ex)
            {
                XmlRpcUtil.error("XmlRpcServerConnection::writeResponse: write error ({0}).", ex.Message);
                return false;
            }

            /*catch (Exception ex)
            {
                XmlRpcUtil.error("XmlRpcServerConnection::writeResponse: write error ({0}).", ex.Message);
                return false;
            }*/

            XmlRpcUtil.log(XmlRpcUtil.XMLRPC_LOG_LEVEL.DEBUG, "XmlRpcServerConnection::writeResponse: wrote {0} of {0} bytes.", _bytesWritten, response.Length);

            // Prepare to read the next request
            if (_bytesWritten == response.Length)
            {
                response = "";
                _connectionState = ServerConnectionState.READ_HEADER;
            }

            return _keepAlive; // Continue monitoring this source if true
        }

        public override Socket getSocket()
        {
            return socket;
        }

        private enum ServerConnectionState
        {
            READ_HEADER,
            READ_REQUEST,
            WRITE_RESPONSE
        };
    }
}