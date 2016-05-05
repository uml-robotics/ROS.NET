// File: XmlRpcUtil.cs
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

#endregion

namespace XmlRpc_Wrapper
{
#if !TRACE
    [DebuggerStepThrough]
#endif

    public class XmlRpcException : Exception
    {
        private int errorCode = -1;

        public XmlRpcException(string msg, int errCode = -1)
            : base(msg)
        {
            errorCode = errCode;
        }

        public int getCode()
        {
            return errorCode;
        }
    }

    public enum HTTPHeaderField
    {
        Accept = 0,
        Accept_Charset = 1,
        Accept_Encoding = 2,
        Accept_Language = 3,
        Accept_Ranges = 4,
        Authorization = 5,
        Cache_Control = 6,
        Connection = 7,
        Cookie = 8,
        Content_Length = 9,
        Content_Type = 10,
        Date = 11,
        Expect = 12,
        From = 13,
        Host = 14,
        If_Match = 15,
        If_Modified_Since = 16,
        If_None_Match = 17,
        If_Range = 18,
        If_Unmodified_Since = 19,
        Max_Forwards = 20,
        Pragma = 21,
        Proxy_Authorization = 22,
        Range = 23,
        Referer = 24,
        TE = 25,
        Upgrade = 26,
        User_Agent = 27,
        Via = 28,
        Warn = 29,
        Age = 30,
        Allow = 31,
        Content_Encoding = 32,
        Content_Language = 33,
        Content_Location = 34,
        Content_Disposition = 35,
        Content_MD5 = 36,
        Content_Range = 37,
        ETag = 38,
        Expires = 39,
        Last_Modified = 40,
        Location = 41,
        Proxy_Authenticate = 42,
        Refresh = 43,
        Retry_After = 44,
        Server = 45,
        Set_Cookie = 46,
        Trailer = 47,
        Transfer_Encoding = 48,
        Vary = 49,
        Warning = 50,
        WWW_Authenticate = 51,
        HEADER_VALUE_MAX_PLUS_ONE = 52
    };

    /// <summary>
    ///     Does HTTP header parsing
    ///     Taken from ... somewhere.
    /// </summary>
#if !TRACE
    [DebuggerStepThrough]
#endif
    internal class HTTPHeader
    {
        [Flags]
        internal enum STATUS
        {
            UNINITIALIZED,
            PARTIAL_HEADER,
            COMPLETE_HEADER
        }

        private Dictionary<HTTPHeaderField, string> m_StrHTTPField = new Dictionary<HTTPHeaderField, string>();
        private byte[] m_byteData = new byte[4096];
        private string m_headerSoFar = "";

        #region PROPERTIES

        internal STATUS m_headerStatus { get; private set; }

        public string Header
        {
            get { return m_headerSoFar; }
        }

        public Dictionary<HTTPHeaderField, string> HTTPField
        {
            get { return m_StrHTTPField; }
        }

        public byte[] Data
        {
            get { return m_byteData; }
            set { m_byteData = value; }
        }

        public string DataString { get; private set; }

        public int ContentLength
        {
            get
            {
                int ret = -1;
                string value;
                if (m_StrHTTPField.TryGetValue(HTTPHeaderField.Content_Length, out value) && int.TryParse(value, out ret))
                    return ret;
                return -1;
            }
        }

        public bool ContentComplete
        {
            get
            {
                int contentlength = ContentLength;
                if (contentlength <= 0) return false;

                return DataString != null && DataString.Length >= contentlength;
            }
        }

        #endregion

        #region CONSTRUCTEUR

        /// <summary>
        ///     Constructeur par d?faut - non utilis?
        /// </summary>
        private HTTPHeader()
        {
            DataString = "";
            m_headerStatus = STATUS.UNINITIALIZED;
        }

        public HTTPHeader(string HTTPRequest) : this()
        {
            Append(HTTPRequest);
        }

        /// <summary>
        ///     Either HTTPRequest contains the header AND some data, or it contains part or all of the header. Accumulate pieces
        ///     of the header in case it spans multiple reads.
        /// </summary>
        /// <param name="HTTPRequest"></param>
        /// <returns></returns>
        public STATUS Append(string HTTPRequest)
        {
            if (m_headerStatus != STATUS.COMPLETE_HEADER)
            {
                int betweenHeaderAndData = HTTPRequest.IndexOf("\r\n\r\n", StringComparison.OrdinalIgnoreCase);
                if (betweenHeaderAndData > 0)
                {
                    m_headerStatus = STATUS.COMPLETE_HEADER;
                    //found the boundary between header and data
                    m_headerSoFar += HTTPRequest.Substring(0, betweenHeaderAndData);
                    HTTPHeaderParse(m_headerSoFar);

                    //shorten the request so we can fall through
                    HTTPRequest = HTTPRequest.Substring(betweenHeaderAndData + 4);
                    //
                    // FALL THROUGH to header complete case
                    //
                }
                else
                {
                    m_headerSoFar += HTTPRequest;
                    m_headerStatus = STATUS.PARTIAL_HEADER;
                    HTTPHeaderParse(m_headerSoFar);
                    return m_headerStatus;
                }
            }

            if (m_headerStatus == STATUS.COMPLETE_HEADER)
            {
                if (ContentComplete)
                {
                    //this isn't right... restart with empty header and see if it works
                    m_headerStatus = STATUS.UNINITIALIZED;
                    Data = new byte[0];
                    DataString = "";
                    m_headerSoFar = "";
                    m_StrHTTPField.Clear();
                    return Append(HTTPRequest);
                }

                DataString += HTTPRequest;
                if (ContentComplete)
                {
                    Data = Encoding.ASCII.GetBytes(DataString);
                    XmlRpcUtil.log(XmlRpcUtil.XMLRPC_LOG_LEVEL.INFO, "DONE READING CONTENT");
                }
            }
            return m_headerStatus;
        }

        public HTTPHeader(byte[] ByteHTTPRequest) : this(Encoding.ASCII.GetString(ByteHTTPRequest))
        {
        }

        #endregion

        #region HTTP Header parsing stuff

        private Dictionary<HTTPHeaderField, string> HeaderFieldToStrings = new Dictionary<HTTPHeaderField, string>();

        private void HTTPHeaderParse(string Header)
        {
            #region HTTP HEADER REQUEST & RESPONSE

            HTTPHeaderField HHField;
            string HTTPfield = null;
            int Index;
            string buffer;
            for (int f = (int) HTTPHeaderField.Accept; f < (int) HTTPHeaderField.HEADER_VALUE_MAX_PLUS_ONE; f++)
            {
                HHField = (HTTPHeaderField) f;
                HTTPfield = null;
                if (!HeaderFieldToStrings.TryGetValue(HHField, out HTTPfield) || HTTPField == null)
                {
                    HTTPfield = "\n" + HHField.ToString().Replace('_', '-') + ": ";
                    HeaderFieldToStrings.Add(HHField, HTTPfield);
                }

                // Si le champ n'est pas pr?sent dans la requ?te, on passe au champ suivant
                Index = Header.IndexOf(HTTPfield, StringComparison.OrdinalIgnoreCase);
                if (Index == -1)
                    continue;

                buffer = Header.Substring(Index + HTTPfield.Length);
                Index = buffer.IndexOf("\r\n", StringComparison.OrdinalIgnoreCase);
                if (Index == -1)
                    m_StrHTTPField[HHField] = buffer.Trim();
                else
                    m_StrHTTPField[HHField] = buffer.Substring(0, Index).Trim();

                if (m_StrHTTPField[HHField].Length == 0)
                {
                    XmlRpcUtil.log(XmlRpcUtil.XMLRPC_LOG_LEVEL.WARNING, "HTTP HEADER: field \"{0}\" has a length of 0", HHField.ToString());
                }
                XmlRpcUtil.log(XmlRpcUtil.XMLRPC_LOG_LEVEL.DEBUG, "HTTP HEADER: Index={0} | champ={1} = {2}", f, HTTPfield.Substring(1), m_StrHTTPField[HHField]);
            }

            #endregion
        }

        #endregion
    }

#if !TRACE
    [DebuggerStepThrough]
#endif

    public static class XmlRpcUtil
    {
        public enum XMLRPC_LOG_LEVEL
        {
            CRITICAL = 0,
            ERROR = 1,
            WARNING = 2,
            INFO = 3,
            DEBUG = 4,
            SPEW = 5
        }

        public static string XMLRPC_VERSION = "XMLRPC++ 0.7";
        private static XMLRPC_LOG_LEVEL MINIMUM_LOG_LEVEL = XMLRPC_LOG_LEVEL.ERROR;

        public static void SetLogLevel(XMLRPC_LOG_LEVEL level)
        {
            MINIMUM_LOG_LEVEL = level;
        }

        public static void SetLogLevel(int level)
        {
            SetLogLevel((XMLRPC_LOG_LEVEL) level);
        }

        public static void error(string format, params object[] list)
        {
#if ENABLE_MONO
            UnityEngine.Debug.LogWarning(String.Format(format, list));
#else
            Debug.WriteLine(String.Format(format, list));
#endif
        }

        public static void log(int level, string format, params object[] list)
        {
            log((XMLRPC_LOG_LEVEL) level, format, list);
        }

        public static void log(XMLRPC_LOG_LEVEL level, string format, params object[] list)
        {
            if (level <= MINIMUM_LOG_LEVEL)
#if ENABLE_MONO
                UnityEngine.Debug.Log(String.Format(format, list));
#else
                Debug.WriteLine(String.Format(format, list));
#endif
        }
    }
}