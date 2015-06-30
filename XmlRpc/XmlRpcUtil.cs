// File: XmlRpcUtil.cs
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

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

#endregion

namespace XmlRpc
{
	public class XmlRpcException : Exception
	{
		public XmlRpcException(string msg, int errCode = -1)
			:base(msg)
		{
			this.errorCode = errCode;
		}

		public int getCode()
		{
			return errorCode;
		}

		int errorCode = -1;
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
		WWW_Authenticate = 51
	};

	/// <summary>
	/// Does HTTP header parsing
	/// </summary>
	class HTTPHeader
	{
		#region PROPERTIES
		private string[] m_StrHTTPField = new string[52];
		private byte[] m_byteData = new byte[4096];

		public string[] HTTPField
		{
			get { return m_StrHTTPField; }
			set { m_StrHTTPField = value; }
		}
		public byte[] Data
		{
			get { return m_byteData; }
			set { m_byteData = value; }
		}
		#endregion
		// convertion
		System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();

		public int LastIndex = 0;
		public int IndexHeaderEnd = 0;

		#region CONSTRUCTEUR
		/// <summary>
		/// Constructeur par défaut - non utilisé
		/// </summary>
		private HTTPHeader()
		{ }

		public HTTPHeader(string HTTPRequest)
		{
			try
			{
				IndexHeaderEnd = 0;
				string Header;

				// Si la taille de requête est supérieur ou égale à 1460, alors toutes la chaine est l'entête http
				if (HTTPRequest.Length >= 1460)
				{
					Header = HTTPRequest;
				}
				else
				{
					IndexHeaderEnd = HTTPRequest.IndexOf("\r\n\r\n");
					Header = HTTPRequest.Substring(0, IndexHeaderEnd);
					Data = encoding.GetBytes(HTTPRequest.Substring(IndexHeaderEnd + 4));
				}

				HTTPHeaderParse(Header);
			}
			catch (Exception)
			{ }
		}

		public HTTPHeader(byte[] ByteHTTPRequest)
		{
			string HTTPRequest = encoding.GetString(ByteHTTPRequest);
			try
			{
				//int IndexHeaderEnd;
				string Header;

				// Si la taille de requête est supérieur ou égale à 1460, alors toutes la chaine est l'entête http
				if (HTTPRequest.Length >= 1460)
					Header = HTTPRequest;
				else
				{
					IndexHeaderEnd = HTTPRequest.IndexOf("\r\n\r\n");
					Header = HTTPRequest.Substring(0, IndexHeaderEnd);
					Data = encoding.GetBytes(HTTPRequest.Substring(IndexHeaderEnd + 4));
				}

				HTTPHeaderParse(Header);
			}
			catch (Exception)
			{ }
		}
		#endregion

		#region METHODES
		private void HTTPHeaderParse(string Header)
		{
			#region HTTP HEADER REQUEST & RESPONSE

			HTTPHeaderField HHField;
			string HTTPfield, buffer;
			int Index;
			foreach (int IndexHTTPfield in Enum.GetValues(typeof(HTTPHeaderField)))
			{
				HHField = (HTTPHeaderField)IndexHTTPfield;
				HTTPfield = "\n" + HHField.ToString().Replace('_', '-') + ": "; //Ajout de \n devant pour éviter les doublons entre cookie et set_cookie
				// Si le champ n'est pas présent dans la requête, on passe au champ suivant
				Index = Header.IndexOf(HTTPfield, StringComparison.OrdinalIgnoreCase);
				if (Index == -1)
					continue;

				buffer = Header.Substring(Index + HTTPfield.Length);
				Index = buffer.IndexOf("\r\n");
				if (Index == -1)
					m_StrHTTPField[IndexHTTPfield] = buffer.Trim();
				else
					m_StrHTTPField[IndexHTTPfield] = buffer.Substring(0, Index).Trim();

				Console.WriteLine("Index = " + IndexHTTPfield + " | champ = " + HTTPfield.Substring(1) + " " + m_StrHTTPField[IndexHTTPfield]);
			}

			// Affichage de tout les champs
			/*for (int j = 0; j < m_StrHTTPField.Length; j++)
			{
				HHField = (HTTPHeaderField)j;
				Console.WriteLine("m_StrHTTPField[" + j + "]; " + HHField + " = " + m_StrHTTPField[j]);
			}
			*/
			#endregion

		}
		#endregion
	}

    public static class XmlRpcUtil
	{
		public static string XMLRPC_VERSION = "XMLRPC++ 0.7";
		public static void error(string format, params object[] list)
		{
			Debug.WriteLine(String.Format(format, list));
		}

		public static void log(int level, string format, params object[] list)
		{
			Debug.WriteLine(String.Format(format, list));
		}
		/*
        private static printint _PRINTINT;
        private static printstr _PRINTSTR;

        private static void thisishowawesomeyouare(string s)
        {
            Debug.WriteLine("XMLRPC NATIVE OUT: " + s);
        }

        public static void ShowOutputFromXmlRpcPInvoke(printstr handler = null)
        {
            if (handler == null)
                handler = thisishowawesomeyouare;
            if (handler != _PRINTSTR)
            {
                _PRINTSTR = thisishowawesomeyouare;
                SetAwesomeFunctionPtr(_PRINTSTR);
            }
        }

        #region bad voodoo

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void printint(int val);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void printstr(string s);

        [DllImport("XmlRpcWin32.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int IntegerEcho(int val);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "IntegerEchoFunctionPtr", CallingConvention = CallingConvention.Cdecl
            )]
        private static extern void IntegerEchoFunctionPtr([MarshalAs(UnmanagedType.FunctionPtr)] printint callback);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "IntegerEchoRepeat", CallingConvention = CallingConvention.Cdecl)]
        private static extern byte IntegerEchoRepeat(int val);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "SetStringOutFunc", CallingConvention = CallingConvention.Cdecl)]
        private static extern void SetAwesomeFunctionPtr(
            [MarshalAs(UnmanagedType.FunctionPtr)] printstr callback);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "StringPassingTest", CallingConvention = CallingConvention.Cdecl)]
        private static extern void StringTest([In] [Out] [MarshalAs(UnmanagedType.LPStr)] string str);


        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcGiblets_Free", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Free(IntPtr val);

        #endregion*/
    }
}