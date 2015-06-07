// File: XmlRpcValue.cs
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;

#endregion

namespace XmlRpc
{	
    //TODO: OPERATOR GARBAGE?
	[Serializable]
    public class XmlRpcValue// : IDisposable
    {
		public enum ValueType
		{
			TypeInvalid,
			TypeBoolean,
			TypeInt,
			TypeDouble,
			TypeString,
			TypeDateTime,
			TypeBase64,
			TypeArray,
			TypeStruct,
			TypeIDFK
		}

        [DebuggerStepThrough]
        public XmlRpcValue()
        {
           _type = ValueType.TypeInvalid;
        }

		public void Dump()
		{
			// Dunno what to do here
		}

        [DebuggerStepThrough]
        public XmlRpcValue(params object[] initialvalues)
            : this()
        {
			SetArray(initialvalues.Length);
            for (int i = 0; i < initialvalues.Length; i++)
            {
                int ires = 0;
                double dres = 0;
                bool bres = false;
                if (initialvalues[i] == null)
                    Set(i, "");
                else if (initialvalues[i] is string)
                    Set(i, initialvalues[i].ToString());
                else if (initialvalues[i] is int && int.TryParse(initialvalues[i].ToString(), out ires))
                    Set(i, ires);
                else if (initialvalues[i] is double && double.TryParse(initialvalues[i].ToString(), out dres))
                    Set(i, dres);
                else if (initialvalues[i] is bool && bool.TryParse(initialvalues[i].ToString(), out bres))
                    Set(i, bres);
                else
                    Set(i, initialvalues[i].ToString());
            }
        }

        [DebuggerStepThrough]
        public XmlRpcValue(bool value)
        {
			/*
            __instance = create(value);
            AddRef(__instance);*/
			asBool = value;
			_type = ValueType.TypeBoolean;
        }

        [DebuggerStepThrough]
        public XmlRpcValue(int value)
        {
			asInt = value;			
			_type = ValueType.TypeInt;            
        }

        [DebuggerStepThrough]
        public XmlRpcValue(double value)
        {
			asDouble = value;
			_type = ValueType.TypeDouble;
        }

        [DebuggerStepThrough]
        public XmlRpcValue(string value)
        {
			asString = value;
			_type = ValueType.TypeString;
        }
		/*
        [DebuggerStepThrough]
        public XmlRpcValue(XmlRpcValue value)
            : this(value.instance)
        {
			this._type = value._type;

			switch (_type) 
			{    // Ensure there is a valid value for the type
			case ValueType.TypeString:   asString = ""; 
				break;
			case ValueType.TypeDateTime: 
				asTime = value.asTime;
				break;
			case ValueType.TypeBase64:
				Array.Copy(value.asBinary, asBinary, value.asBinary.Length);
				break;
			case ValueType.TypeArray:
				asArray = new ValueArray();   
				break;
			case ValueType.TypeStruct:   asStruct = new ValueStruct(); 
				break;
			}
			this.variantValue = value.variantValue;
        }*/
		/*
        [DebuggerStepThrough]
        public XmlRpcValue(IntPtr existingptr)
        {
            if (existingptr == IntPtr.Zero)
                throw new Exception("SUCK IS CONTAGEOUS!");
            __instance = existingptr;
            AddRef(existingptr);
        }
		*/
		static string VALUE_TAG = "value";
		static string BOOLEAN_TAG = "boolean";
		static string DOUBLE_TAG = "double";
		static string INT_TAG = "int";
		static string I4_TAG = "i4";
		static string STRING_TAG = "string";
		static string DATETIME_TAG = "dateTime.iso8601";
		static string BASE64_TAG = "base64";
		static string ARRAY_TAG = "array";
		static string DATA_TAG = "data";
		static string STRUCT_TAG = "struct";
		static string MEMBER_TAG = "member";
		static string NAME_TAG = "name";
		/*
		static string VALUE_TAG     = "<value>";
		static string VALUE_ETAG    = "</value>";

		static string BOOLEAN_TAG   = "<boolean>";
		static string BOOLEAN_ETAG  = "</boolean>";
		static string DOUBLE_TAG    = "<double>";
		static string DOUBLE_ETAG   = "</double>";
		static string INT_TAG       = "<int>";
		static string I4_TAG        = "<i4>";
		static string I4_ETAG       = "</i4>";
		static string STRING_TAG    = "<string>";
		static string DATETIME_TAG  = "<dateTime.iso8601>";
		static string DATETIME_ETAG = "</dateTime.iso8601>";
		static string BASE64_TAG    = "<base64>";
		static string BASE64_ETAG   = "</base64>";

		static string ARRAY_TAG     = "<array>";
		static string DATA_TAG      = "<data>";
		static string DATA_ETAG     = "</data>";
		static string ARRAY_ETAG    = "</array>";

		static string STRUCT_TAG    = "<struct>";
		static string MEMBER_TAG    = "<member>";
		static string NAME_TAG      = "<name>";
		static string NAME_ETAG     = "</name>";
		static string MEMBER_ETAG   = "</member>";
		static string STRUCT_ETAG   = "</struct>";*/

		// Format strings
		string _doubleFormat = "%.16g";

		// Type tag and values
		ValueType _type;

		public class tm 
		{
			public int tm_sec;     /* seconds after the minute - [0,59] */
			public int tm_min;     /* minutes after the hour - [0,59] */
			public int tm_hour;    /* hours since midnight - [0,23] */
			public int tm_mday;    /* day of the month - [1,31] */
			public int tm_mon;     /* months since January - [0,11] */
			public int tm_year;    /* years since 1900 */
			public int tm_wday;    /* days since Sunday - [0,6] */
			public int tm_yday;    /* days since January 1 - [0,365] */
			public int tm_isdst;   /* daylight savings time flag */
        };
		
		public string asString;
		public tm asTime;
		public int asInt;
		public bool asBool;
		public double asDouble;
		public byte[] asBinary;
		public XmlRpcValue[] asArray;
		public Dictionary<string, XmlRpcValue> asStruct;

		// Clean up
		void invalidate()
		{
			_type = ValueType.TypeInvalid;
			asStruct = null;
			asArray = null;
			asString = null;
			asBinary = null;
			asBool = false;
			asTime = null;
		}

  
		
  // Type checking
		/*
	void assertTypeOrInvalid(ValueType t)
	{
		if (_type == ValueType.TypeInvalid)
		{
			_type = t;
			switch (_type) {    // Ensure there is a valid value for the type
			case ValueType.TypeString:   asString = ""; 
				break;
			case ValueType.TypeDateTime: asTime = new tm();
				break;
			case ValueType.TypeBase64:   asBinary = new char[1];
				break;
			case ValueType.TypeArray:    asArray = new ValueArray();   
				break;
			case ValueType.TypeStruct:   asStruct = new ValueStruct(); 
				break;
			default:           _
				value.asBinary = 0; break;
			}
		}
		else if (_type != t)
			throw XmlRpcException("type error");
	}*/
		/*
	void assertArray(int size)
	{
		if (_type != ValueType.TypeArray)
			throw new XmlRpcException("type error: expected an array");
		else if (this.asArray.Length < size)
			throw new XmlRpcException("range error: array index too large");
	}
		*/
		
	void assertArray(int size)
	{
		if (_type == ValueType.TypeInvalid) 
		{
			_type = ValueType.TypeArray;
			asArray = new XmlRpcValue[size];
		} else if (_type == ValueType.TypeArray) 
		{
			if (asArray.Length < size)
				Array.Resize<XmlRpcValue>(ref this.asArray, size);
		} else
			throw new XmlRpcException("type error: expected an array");
	}

	void assertStruct()
	{
		if (_type == ValueType.TypeInvalid) {
			_type = ValueType.TypeStruct;
			asStruct = new  Dictionary<string, XmlRpcValue>();
		} else if (_type != ValueType.TypeStruct)
			throw new XmlRpcException("type error: expected a struct");
	}

  // Predicate for tm equality
	static bool tmEq(tm t1, tm t2) 
	{
	return t1.tm_sec == t2.tm_sec && t1.tm_min == t2.tm_min &&
			t1.tm_hour == t2.tm_hour && t1.tm_mday == t2.tm_mday &&
			t1.tm_mon == t2.tm_mon && t1.tm_year == t2.tm_year;
	}
	
		public override bool Equals(object obj)
		{
			XmlRpcValue other = (XmlRpcValue)obj;
	  
			if (_type != other._type)
				return false;

			switch (_type) {
				case ValueType.TypeBoolean:  return asBool == other.asBool;
				case ValueType.TypeInt:      return asInt == other.asInt;
				case ValueType.TypeDouble:   return asDouble == other.asDouble;
				case ValueType.TypeDateTime: return tmEq(asTime, other.asTime);
				case ValueType.TypeString:   return asString.Equals(other.asString);
				case ValueType.TypeBase64:   return asBinary == other.asBinary;
				case ValueType.TypeArray:    return asArray == other.asArray;

				// The map<>::operator== requires the definition of value< for kcc
				case ValueType.TypeStruct:   //return *_value.asStruct == *other._value.asStruct;
				{
					if (asStruct.Count != other.asStruct.Count)
					return false;
					var aenum = asStruct.GetEnumerator();
					var benum = other.asStruct.GetEnumerator();			
			
					while (aenum.MoveNext() && benum.MoveNext()) 
					{
						if (!aenum.Current.Value.Equals(benum.Current.Value))
							return false;
					}
					return true;
				}
				default: break;
			}
			return true;    // Both invalid values ...
		}
	
	// Works for strings, binary data, arrays, and structs.
	public int Length
	{
		get
		{
			switch (_type) 
			{
				case ValueType.TypeString: return asString.Length;
				case ValueType.TypeBase64: return asBinary.Length;
				case ValueType.TypeArray:  return asArray.Length;
				case ValueType.TypeStruct: return asStruct.Count;
				default: break;
			}

			XmlRpcUtil.log(4, "Trying to get size of something without a size! -- type={0}", _type);
			throw new XmlRpcException("type error");
		}
	}

	public void Copy(XmlRpcValue other)
	{
		switch (_type)
		{
			case ValueType.TypeBoolean: asBool = other.asBool; break;
			case ValueType.TypeInt: asInt = other.asInt; break;
			case ValueType.TypeDouble: asDouble = other.asDouble; break;
			case ValueType.TypeDateTime: asTime = other.asTime; break;
			case ValueType.TypeString: asString = other.asString; break;
			case ValueType.TypeBase64: asBinary = other.asBinary; break;
			case ValueType.TypeArray: asArray = other.asArray; break;

			// The map<>::operator== requires the definition of value< for kcc
			case ValueType.TypeStruct:   //return *_value.asStruct == *other._value.asStruct;
				asStruct = other.asStruct;
				break;
		}
	}

	// Checks for existence of struct member
	public bool hasMember(string name)
	{
		return _type == ValueType.TypeStruct && asStruct.ContainsKey(name);
	}

	void parseString(XmlNode node)
	{
		this._type = ValueType.TypeString;
		this.asString = node.InnerText;
	}
	void parseBool(XmlNode node)
	{
	}

	//bool fromXml(string valueXml, out int offset)
	public bool fromXml(XmlNode value)
	{
		//int val = offset;
		//offset = 0;
		try
		{
			//XmlElement value = node["value"];
			if (value == null)
				return false;
			/*
			foreach (var parser in parsers)
			{
				XmlElement el = value[parser.Key];
				if (el == null)
					continue;
				parser.Value(el);
				//if( != null)
			}*/
			string tex = value.InnerText;
			XmlElement val;
			if ((val = value[BOOLEAN_TAG]) != null)
			{
				this._type = ValueType.TypeBoolean;
				int tmp = 0;
				if (!int.TryParse(tex, out tmp))
					return false;
				if(tmp != 0 && tmp != 1)
					return false;
				this.asBool = (tmp == 0 ? false : true);
			}
			else if ((val = value[I4_TAG]) != null)
			{
				this._type = ValueType.TypeInt;
				return int.TryParse(tex, out asInt);
			}
			else if ((val = value[INT_TAG]) != null)
			{
				this._type = ValueType.TypeInt;
				return int.TryParse(tex, out asInt);
			}
			else if ((val = value[DOUBLE_TAG]) != null)
			{
				this._type = ValueType.TypeDouble;
				return double.TryParse(tex, out this.asDouble);
			}
			else if ((val = value[DATETIME_TAG]) != null)
			{
				// TODO: implement
			}
			else if ((val = value[BASE64_TAG]) != null)
			{
				// TODO: implement
			}
			else if ((val = value[STRING_TAG]) != null)
			{
				this._type = ValueType.TypeString;
				this.asString = tex;
			}
			else if ((val = value[ARRAY_TAG]) != null)
			{
				var data = val[DATA_TAG];
				if (data == null)
					return false;
				var selection = data.SelectNodes(VALUE_TAG);
				SetArray(selection.Count);
				for (int i = 0; i < selection.Count; i++)
				{
					var xmlValue = new XmlRpcValue();
					if (!xmlValue.fromXml(selection[i]))
						return false;
					asArray[i] = xmlValue;
				}
			}
			else if ((val = value[STRUCT_TAG]) != null)
			{
				// TODO: implement
			}
		}
		catch (Exception ex)
		{
			return false;
		}
		return true;
	}

	public string toXml()
	{
		XmlWriterSettings settings = new XmlWriterSettings();
		settings.OmitXmlDeclaration = true;
		settings.ConformanceLevel = ConformanceLevel.Fragment;
		settings.CloseOutput = false;
		StringWriter strm = new StringWriter();
		XmlWriter writer = XmlWriter.Create(strm, settings);

		XmlDocument doc = new XmlDocument();
		toXml(doc, doc);
		doc.WriteContentTo(writer);
		writer.Close();
		string result = strm.ToString();
		return result;
	}
	public XmlNode toXml(XmlDocument doc, XmlNode parent)
	{
		XmlElement root = doc.CreateElement(VALUE_TAG);

		//value.
		XmlElement el = null;
		switch (this._type)
		{
			case ValueType.TypeBoolean:
				el = doc.CreateElement(BOOLEAN_TAG);
				el.AppendChild(doc.CreateTextNode(asBool.ToString()));
				break;
			case ValueType.TypeInt:
				el = doc.CreateElement(INT_TAG);
				el.AppendChild(doc.CreateTextNode(asInt.ToString()));
				break;
			case ValueType.TypeDouble:
				el = doc.CreateElement(BOOLEAN_TAG);
				el.AppendChild(doc.CreateTextNode(asDouble.ToString()));
				break;
			case ValueType.TypeDateTime:
				el = doc.CreateElement(DATETIME_TAG);
				el.AppendChild(doc.CreateTextNode(asTime.ToString()));
				break;
			case ValueType.TypeString: 
				//asString = other.asString; 
				el = doc.CreateElement(STRING_TAG);
				el.AppendChild(doc.CreateTextNode(asString));
				break;
			case ValueType.TypeBase64: 
				//asBinary = other.asBinary; 
				el = doc.CreateElement(BASE64_TAG);
				var base64 = System.Convert.ToBase64String(asBinary);
				el.AppendChild(doc.CreateTextNode(base64));
				break;
			case ValueType.TypeArray:
				el = doc.CreateElement(DATA_TAG);
				for (int i = 0; i < Size; i++)
				{
					asArray[i].toXml(doc, el);
				}
				break;
			case ValueType.TypeStruct:
				el = doc.CreateElement(STRUCT_TAG);
				foreach (var record in this.asStruct)
				{
					var member = doc.CreateElement(MEMBER_TAG);
					var name = doc.CreateElement(NAME_TAG);
					name.AppendChild(doc.CreateTextNode(record.Key));
					member.AppendChild(name);
					record.Value.toXml(doc, member);
					el.AppendChild(member);
				}
				//throw new NotImplementedException("Struct serialization is not implemented");
				break;
		}

		if(el != null)
			root.AppendChild(el);

		parent.AppendChild(root);
		return root;
	}
#if MANUAL_SERIALIZATION
		 // Set the value from xml. The chars at *offset into valueXml 
  // should be the start of a <value> tag. Destroys any existing value.
  bool fromXml(string valueXml, out int offset)
  {
    int savedOffset = offset;

    invalidate();
    if ( ! XmlRpcUtil.nextTagIs(VALUE_TAG, valueXml, offset))
      return false;       // Not a value, offset not updated

	int afterValueOffset = offset;
    string typeTag = XmlRpcUtil.getNextTag(valueXml, offset);
    bool result = false;
    if (typeTag == BOOLEAN_TAG)
      result = boolFromXml(valueXml, out offset);
    else if (typeTag == I4_TAG || typeTag == INT_TAG)
      result = intFromXml(valueXml, out offset);
    else if (typeTag == DOUBLE_TAG)
      result = doubleFromXml(valueXml, out offset);
    else if (typeTag == null || typeTag.Equals(STRING_TAG))
      result = stringFromXml(valueXml, out offset);
    else if (typeTag == DATETIME_TAG)
      result = timeFromXml(valueXml, out offset);
    else if (typeTag == BASE64_TAG)
      result = binaryFromXml(valueXml, out offset);
    else if (typeTag == ARRAY_TAG)
      result = arrayFromXml(valueXml, out offset);
    else if (typeTag == STRUCT_TAG)
      result = structFromXml(valueXml, out offset);
    // Watch for empty/blank strings with no <string>tag
    else if (typeTag == VALUE_ETAG)
    {
      offset = afterValueOffset;   // back up & try again
      result = stringFromXml(valueXml, out offset);
    }

    if (result)  // Skip over the </value> tag
      XmlRpcUtil.findTag(VALUE_ETAG, valueXml, offset);
    else        // Unrecognized tag after <value>
      offset = savedOffset;

    return result;
  }
	// Encode the Value in xml
	string toXml()
	{
		//string ret;
		switch (_type) {
			case ValueType.TypeBoolean:  return boolToXml();
			case ValueType.TypeInt:      return intToXml();
			case ValueType.TypeDouble:   return doubleToXml();
			case ValueType.TypeString:   return stringToXml();
			case ValueType.TypeDateTime: return timeToXml();
			case ValueType.TypeBase64:   return binaryToXml();
			case ValueType.TypeArray:    return arrayToXml();
			case ValueType.TypeStruct:   return structToXml();
			default: break;
		}
		XmlRpcUtil.log(4, "calling ToXml on invalid type");
		return "";   // Invalid value
	}

	// Boolean
	bool boolFromXml(string valueXml, out int offset)
	{
		const char* valueStart = valueXml.c_str() + *offset;
		char* valueEnd;
		long ivalue = strtol(valueStart, &valueEnd, 10);

		if (valueEnd == valueStart || (ivalue != 0 && ivalue != 1))
			return false;

		_type = ValueType.TypeBoolean;
		_value.asBool = (ivalue == 1);
		*offset += int(valueEnd - valueStart);
		return true;
	}

  string boolToXml()
  {
	  /*
    std::string xml = VALUE_TAG;
    xml += BOOLEAN_TAG;
    xml += (_value.asBool ? "1" : "0");
    xml += BOOLEAN_ETAG;
    xml += VALUE_ETAG;
    return xml;*/

	  return String.Format("{0}{1}{2}{3}{4}", VALUE_TAG, BOOLEAN_TAG, (asBool ? "1" : "0"), BOOLEAN_ETAG, VALUE_ETAG);
  }

  // Int
  bool intFromXml(string valueXml, out int offset)
  {
    const char* valueStart = valueXml.c_str() + *offset;
    char* valueEnd;
    long ivalue = strtol(valueStart, &valueEnd, 10);
    if (valueEnd == valueStart)
      return false;

    _type = TypeInt;
    _value.asInt = int(ivalue);
    *offset += int(valueEnd - valueStart);
    return true;
  }

  string intToXml()
  {
	  /*
    char buf[256];
    snprintf(buf, sizeof(buf)-1, "%d", _value.asInt);
    buf[sizeof(buf)-1] = 0;
    std::string xml = VALUE_TAG;
    xml += I4_TAG;
    xml += buf;
    xml += I4_ETAG;
    xml += VALUE_ETAG;
	
    return xml;*/
	return String.Format("{0}{1}{2}{3}{4}", VALUE_TAG, I4_TAG, asInt, I4_ETAG, VALUE_ETAG);
  }

  // Double
  bool doubleFromXml(string valueXml, out int offset)
  {
	  /// TODO: reimplement
	 /*
    const char* valueStart = valueXml.c_str() + *offset;
    char* valueEnd;
    double dvalue = strtod(valueStart, &valueEnd);
    if (valueEnd == valueStart)
      return false;

    _type = TypeDouble;
    _value.asDouble = dvalue;
    *offset += int(valueEnd - valueStart);*/
    return true;
  }

	string doubleToXml()
	{
			/// TODO: reimplement
			/*
		std::stringstream ss;
		ss.imbue(std::locale::classic()); // ensure we're using "C" locale for formatting floating-point (1.4 vs. 1,4, etc.)
		ss.precision(17);
		ss << _value.asDouble;

		string xml = VALUE_TAG;
		xml += DOUBLE_TAG;
		xml += ss.str();
		xml += DOUBLE_ETAG;
		xml += VALUE_ETAG;
		return xml;
		*/
		return String.Format("{0}{1}{2}{3}{4}", VALUE_TAG, DOUBLE_TAG, asDouble, DOUBLE_ETAG, VALUE_ETAG);
	}

  // String
	bool stringFromXml(string valueXml, out int offset)
	{
		int valueEnd = valueXml.IndexOf('<', offset);
		if (valueEnd == -1)
			return false;     // No end tag;

		_type = ValueType.TypeString;
		asString = new string(XmlRpcUtil.xmlDecode(valueXml.Substring(offset, valueEnd-offset)));
		offset += asString.Length;
		return true;
	}

	string stringToXml()
	{
		string xml = VALUE_TAG;
		//xml += STRING_TAG; optional
		xml += XmlRpcUtil.xmlEncode(asString);
		//xml += STRING_ETAG;
		xml += VALUE_ETAG;
		return xml;
	}

  // DateTime (stored as a struct tm)
  bool timeFromXml(string valueXml, out int offset)
  {
    size_t valueEnd = valueXml.find('<', *offset);
    if (valueEnd == std::string::npos)
      return false;     // No end tag;

    string stime = valueXml.substr(*offset, valueEnd-*offset);

    tm t = new tm();
    if (sscanf(stime.c_str(),"%4d%2d%2dT%2d:%2d:%2d",&t.tm_year,&t.tm_mon,&t.tm_mday,&t.tm_hour,&t.tm_min,&t.tm_sec) != 6)
      return false;

    t.tm_isdst = -1;
    _type = TypeDateTime;
    asTime = t;//new tm(t);
    offset += stime.Length;
    return true;
  }

  string timeToXml()
  {
    var t = asTime;
    

	string buf = String.Format("{0:d4}{1:d2}{1:d2}T{1:d2}:{1:d2}:{1:d2}", t.tm_year,t.tm_mon,t.tm_mday,t.tm_hour,t.tm_min,t.tm_sec);
	//char buf[20];
    //snprintf(buf, sizeof(buf)-1, "%4d%02d%02dT%02d:%02d:%02d", t.tm_year,t.tm_mon,t.tm_mday,t.tm_hour,t.tm_min,t.tm_sec);
    buf[sizeof(buf)-1] = 0;

    string xml = VALUE_TAG;
    xml += DATETIME_TAG;
    xml += buf;
    xml += DATETIME_ETAG;
    xml += VALUE_ETAG;
    return xml;
  }


  // Base64
  bool binaryFromXml(string valueXml, out int offset)
  {
    size_t valueEnd = valueXml.find('<', *offset);
    if (valueEnd == std::string::npos)
      return false;     // No end tag;

    _type = TypeBase64;
    string asString = valueXml.substr(*offset, valueEnd-*offset);
    asBinary = new BinaryData();
    // check whether base64 encodings can contain chars xml encodes...

    // convert from base64 to binary
    int iostatus = 0;
	  base64<char> decoder;
    std::back_insert_iterator<BinaryData> ins = std::back_inserter(*(_value.asBinary));
		decoder.get(asString.begin(), asString.end(), ins, iostatus);

    offset += asString.Length;
    return true;
  }


  string binaryToXml()
  {
    // convert to base64
    std::vector<char> base64data;
    int iostatus = 0;
	  base64<char> encoder;
    std::back_insert_iterator<std::vector<char> > ins = std::back_inserter(base64data);
		encoder.put(_value.asBinary->begin(), _value.asBinary->end(), ins, iostatus, base64<>::crlf());

    // Wrap with xml
    string xml = VALUE_TAG;
    xml += BASE64_TAG;
    xml.append(base64data.begin(), base64data.end());
    xml += BASE64_ETAG;
    xml += VALUE_ETAG;
    return xml;
  }


  // Array
  bool arrayFromXml(string valueXml, out int offset)
  {
    if ( ! XmlRpcUtil.nextTagIs(DATA_TAG, valueXml, offset))
      return false;

    _type = TypeArray;
    _value.asArray = new ValueArray;
    XmlRpcValue v;
    while (v.fromXml(valueXml, offset))
      _value.asArray->push_back(v);       // copy...

    // Skip the trailing </data>
    XmlRpcUtil.nextTagIs(DATA_ETAG, valueXml, offset);
    return true;
  }


  // In general, its preferable to generate the xml of each element of the
  // array as it is needed rather than glomming up one big string.
  string arrayToXml()
  {
    string xml = VALUE_TAG;
    xml += ARRAY_TAG;
    xml += DATA_TAG;

    int s = int(_value.asArray->size());
    for (int i=0; i<s; ++i)
       xml += _value.asArray->at(i).toXml();

    xml += DATA_ETAG;
    xml += ARRAY_ETAG;
    xml += VALUE_ETAG;
    return xml;
  }


  // Struct
  bool structFromXml(string valueXml, out int offset)
  {
    _type = TypeStruct;
    asStruct = new Dictionary<string,XmlRpcValue>();

    while (XmlRpcUtil.nextTagIs(MEMBER_TAG, valueXml, offset)) {
      // name
      string name = XmlRpcUtil.parseTag(NAME_TAG, valueXml, offset);
      // value
      XmlRpcValue val = new XmlRpcValue(valueXml, offset);
      if ( ! val.valid()) {
        invalidate();
        return false;
      }
      //const std::pair<const std::string, XmlRpcValue> p(name, val);
      asStruct.Add(name, val);

      XmlRpcUtil.nextTagIs(MEMBER_ETAG, valueXml, offset);
    }
    return true;
  }


  // In general, its preferable to generate the xml of each element
  // as it is needed rather than glomming up one big string.
  string structToXml()
  {
    string xml = VALUE_TAG;
    xml += STRUCT_TAG;

    ValueStruct::const_iterator it;
    for (it=_value.asStruct->begin(); it!=_value.asStruct->end(); ++it) {
      xml += MEMBER_TAG;
      xml += NAME_TAG;
      xml += XmlRpcUtil.xmlEncode(it->first);
      xml += NAME_ETAG;
      xml += it->second.toXml();
      xml += MEMBER_ETAG;
    }

    xml += STRUCT_ETAG;
    xml += VALUE_ETAG;
    return xml;
  }
#endif
	/*
  // Write the value without xml encoding it
  ostream write(ostream os)
{
    switch (_type) {
      default:           break;
      case ValueType.TypeBoolean:  os << _value.asBool; break;
      case TypeInt:      os << _value.asInt; break;
      case TypeDouble:   os << _value.asDouble; break;
      case TypeString:   os << *_value.asString; break;
      case TypeDateTime:
        {
          struct tm* t = _value.asTime;
          char buf[20];
          snprintf(buf, sizeof(buf)-1, "%4d%02d%02dT%02d:%02d:%02d", 
            t->tm_year,t->tm_mon,t->tm_mday,t->tm_hour,t->tm_min,t->tm_sec);
          buf[sizeof(buf)-1] = 0;
          os << buf;
          break;
        }
      case TypeBase64:
        {
          int iostatus = 0;
          std::ostreambuf_iterator<char> out(os);
          base64<char> encoder;
          encoder.put(_value.asBinary->begin(), _value.asBinary->end(), out, iostatus, base64<>::crlf());
          break;
        }
      case TypeArray:
        {
          int s = int(_value.asArray->size());
          os << '{';
          for (int i=0; i<s; ++i)
          {
            if (i > 0) os << ',';
            _value.asArray->at(i).write(os);
          }
          os << '}';
          break;
        }
      case TypeStruct:
        {
          os << '[';
          ValueStruct::const_iterator it;
          for (it=_value.asStruct->begin(); it!=_value.asStruct->end(); ++it)
          {
            if (it!=_value.asStruct->begin()) os << ',';
            os << it->first << ':';
            it->second.write(os);
          }
          os << ']';
          break;
        }
      
    }
    
    return os;
  }*/
		
        #region P/Invoke
	/*
        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Create1", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr create();

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Create2", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr create([MarshalAs(UnmanagedType.I1)] bool value);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Create3", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr create(int value);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Create4", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr create(double value);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Create5", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr create([In] [Out] [MarshalAs(UnmanagedType.LPStr)] string value);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Create6", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr create(IntPtr rhs);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Valid", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool valid(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_SetType", CallingConvention = CallingConvention.Cdecl)]
        private static extern int settype(IntPtr target, int type);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Type", CallingConvention = CallingConvention.Cdecl)]
        private static extern int gettype(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Size", CallingConvention = CallingConvention.Cdecl)]
        private static extern int getsize(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_SetSize", CallingConvention = CallingConvention.Cdecl)]
        private static extern void setsize(IntPtr target, int size);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_HasMember", CallingConvention = CallingConvention.Cdecl)
        ]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool hasmember(IntPtr target, [In] [Out] [MarshalAs(UnmanagedType.LPStr)] string name);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Set1", CallingConvention = CallingConvention.Cdecl)]
        private static extern void set(IntPtr target, [In] [Out] [MarshalAs(UnmanagedType.LPStr)] string value);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Set3", CallingConvention = CallingConvention.Cdecl)]
        private static extern void set(IntPtr target, IntPtr value);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Set5", CallingConvention = CallingConvention.Cdecl)]
        private static extern void set(IntPtr target, int value);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Set7", CallingConvention = CallingConvention.Cdecl)]
        private static extern void set(IntPtr target, [MarshalAs(UnmanagedType.I1)] bool value);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Set9", CallingConvention = CallingConvention.Cdecl)]
        private static extern void set(IntPtr target, double value);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Get1", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr get(IntPtr target, int key);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Get2", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr get(IntPtr target, [In] [Out] [MarshalAs(UnmanagedType.LPStr)] string key);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_GetInt0", CallingConvention = CallingConvention.Cdecl)]
        private static extern int getint(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_GetString0", CallingConvention = CallingConvention.Cdecl
            )]
        private static extern IntPtr getstring(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_GetBool0", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        private static extern bool getbool(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_GetDouble0", CallingConvention = CallingConvention.Cdecl
            )]
        private static extern double getdouble(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_Dump", CallingConvention = CallingConvention.Cdecl)]
        private static extern void dump(IntPtr target);

        [DllImport("XmlRpcWin32.dll", EntryPoint = "XmlRpcValue_ToString", CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr tostring(IntPtr target);
		*/
		#endregion


	public bool Valid
	{
		[DebuggerStepThrough]
		get
		{
			return this._type != ValueType.TypeInvalid;
		}
	}
	
	public ValueType Type
	{
		[DebuggerStepThrough]
		get
		{
			return _type;
		}
	}

	public int Size
	{
		[DebuggerStepThrough]
		get
		{
			if (!Valid || Type == ValueType.TypeInvalid || Type == ValueType.TypeIDFK)
			{
				return 0;
			}
			if (Type != ValueType.TypeString && Type != ValueType.TypeStruct && Type != ValueType.TypeArray)
				return 0;
			if (Type == ValueType.TypeArray)
				return asArray.Length;
			else if (Type == ValueType.TypeString)
				return asString.Length;
			else if (Type == ValueType.TypeStruct)
				return asStruct.Count;
			return 0;
		}
	}

	[DebuggerStepThrough]
	public void Set<T>(T t)
	{
		if (t is string)
		{
			this._type = ValueType.TypeString;
			this.asString = (string)(object)t;
		}
		else if (0 is T)
		{
			this._type = ValueType.TypeInt;
			this.asInt = (int)(object)t;
		}
		else if (this is T)
		{
			Copy(t as XmlRpcValue);
		}
		else if (t is bool)
		{
			asBool = (bool)(object)t;
			this._type = ValueType.TypeBoolean;
		}
		else if (0d is T)
		{
			asDouble = (double)(object)t;
			this._type = ValueType.TypeDouble;
		}
	}
	public void EnsureArraySize(int size)
	{
		if (asArray != null && asArray.Length < size)
			Array.Resize(ref asArray, size);
	}
	
		public XmlRpcValue this[int key]
		{
			[DebuggerStepThrough]
			get 
			{
				EnsureArraySize(key+1);
				return Get(key); 
			}
			[DebuggerStepThrough]
			set { Set(key, value); }
		}

		public XmlRpcValue this[string key]
		{
			[DebuggerStepThrough]
			get { return Get(key); }
			[DebuggerStepThrough]
			set { Set(key, value); }
		}
		
		[DebuggerStepThrough]
		public void Set<T>(int key, T t)
		{
			if (asArray[key] == null)
			{
				asArray[key] = new XmlRpcValue();
			}
			this[key].Set(t);
		}

		public void SetArray(int maxSize)
		{
			this._type = ValueType.TypeArray;
			this.asArray = new XmlRpcValue[maxSize];
		}

		[DebuggerStepThrough]
		public void Set<T>(string key, T t)
		{
			this[key].Set(t);
		}

		[DebuggerStepThrough]
		public T Get<T>() // where T : class, new()
		{
			if (!Valid)
			{
				Console.WriteLine("Trying to get something with an invalid size... BAD JUJU!\n\t" + this);
			}
			else if ("" is T)
			{
				return (T) (object) GetString();
			}
			else if (0 is T)
			{
				return (T) (object) GetInt();
			}
			else if (this is T)
			{
				return (T) (object) this;
			}
			else if (true is T)
			{
				return (T) (object) GetBool();
			}
			else if (0d is T)
			{
				return (T) (object) GetDouble();
			}
			Console.WriteLine("I DUNNO WHAT THAT IS!");
			return default(T);
		}

		[DebuggerStepThrough]
		private T Get<T>(int key)
		{
			return this[key].Get<T>();
		}

		[DebuggerStepThrough]
		private T Get<T>(string key)
		{
			return this[key].Get<T>();
		}

		[DebuggerStepThrough]
		private XmlRpcValue Get(int key)
		{
			//IntPtr nested = get(instance, key);
			//return LookUp(nested);
			return asArray[key];
		}

		[DebuggerStepThrough]
		private XmlRpcValue Get(string key)
		{
			//IntPtr nested = get(instance, key);
			if (asStruct.ContainsKey(key))
				return asStruct[key];
			return null;
		}

		[DebuggerStepThrough]
		public int GetInt()
		{
			return asInt;
		}

		[DebuggerStepThrough]
		public string GetString()
		{
			return asString;
		}

		[DebuggerStepThrough]
		public bool GetBool()
		{
			return asBool;
		}

		[DebuggerStepThrough]
		public double GetDouble()
		{
			return asDouble;
		}
		/*
		[DebuggerStepThrough]
		public override string ToString()
		{
			if (__instance == IntPtr.Zero)
				return "(NULL)";
			string s = Marshal.PtrToStringAnsi(tostring(instance));
			return s;
		}*/
	}
}