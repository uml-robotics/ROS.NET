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

//#define REFDEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

#endregion

namespace XmlRpc
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
    //TODO: OPERATOR GARBAGE?
	[Serializable]
    public class XmlRpcValue : IDisposable
    {
#if STUPID_XML_STUFF
        [DebuggerStepThrough]
        public XmlRpcValue()
        {
           _type = ValueType.TypeInvalid;
        }

        [DebuggerStepThrough]
        public XmlRpcValue(params object[] initialvalues)
            : this()
        {
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
		static string STRUCT_ETAG   = "</struct>";

		// Format strings
		string _doubleFormat = "%.16g";

		// Type tag and values
		ValueType _type;

		public struct tm 
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
		public char[] asBinary;
		public XmlRpcValue[] asArray;
		public Dictionary<string, XmlRpcValue> asStruct;

		// Clean up
		void invalidate()
		{
			/*
			switch (_type) 
			{
				case TypeString:    delete _value.asString; break;
				case TypeDateTime:  delete _value.asTime;   break;
				case TypeBase64:    delete _value.asBinary; break;
				case TypeArray:     delete _value.asArray;  break;
				case TypeStruct:    delete _value.asStruct; break;
				default: break;
			}*/
			_type = ValueType.TypeInvalid;
			//_value.asBinary = 0;
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

	void assertArray(int size)
	{
		if (_type != ValueType.TypeArray)
			throw new XmlRpcException("type error: expected an array");
		else if (this.asArray.Length < size)
			throw new XmlRpcException("range error: array index too large");
	}


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
			throw XmlRpcException("type error: expected an array");
	}

	void assertStruct()
	{
		if (_type == ValueType.TypeInvalid) {
			_type = ValueType.TypeStruct;
			asStruct = new  Dictionary<string, XmlRpcValue>();
		} else if (_type != ValueType.TypeStruct)
			throw XmlRpcException("type error: expected a struct");
	}


  // Operators
/*
  XmlRpcValue operator=(XmlRpcValue const& rhs)
  {
    if (this != &rhs)
    {
      invalidate();
      _type = rhs._type;
      switch (_type) {
        case ValueType.TypeBoolean:  _value.asBool = rhs._value.asBool; break;
        case ValueType.TypeInt:      _value.asInt = rhs._value.asInt; break;
        case ValueType.TypeDouble:   _value.asDouble = rhs._value.asDouble; break;
        case TypeDateTime: _value.asTime = new struct tm(*rhs._value.asTime); break;
        case TypeString:   _value.asString = new std::string(*rhs._value.asString); break;
        case TypeBase64:   _value.asBinary = new BinaryData(*rhs._value.asBinary); break;
        case TypeArray:    _value.asArray = new ValueArray(*rhs._value.asArray); break;
        case TypeStruct:   _value.asStruct = new ValueStruct(*rhs._value.asStruct); break;
        default:           _value.asBinary = 0; break;
      }
    }
    return *this;
  }*/


  // Predicate for tm equality
	static bool tmEq(tm t1, tm t2) 
	{
	return t1.tm_sec == t2.tm_sec && t1.tm_min == t2.tm_min &&
			t1.tm_hour == t2.tm_hour && t1.tm_mday == t1.tm_mday &&
			t1.tm_mon == t2.tm_mon && t1.tm_year == t2.tm_year;
	}
	
  bool Equals(object obj)
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

			
          
          ValueStruct::const_iterator it1=_value.asStruct->begin();
          ValueStruct::const_iterator it2=other._value.asStruct->begin();
          while (it1 != _value.asStruct->end()) {
            const XmlRpcValue& v1 = it1->second;
            const XmlRpcValue& v2 = it2->second;
            if ( ! (v1 == v2))
              return false;
            it1++;
            it2++;
          }
          return true;
        }
      default: break;
    }
    return true;    // Both invalid values ...
  }
		/*
  bool XmlRpcValue::operator!=(XmlRpcValue const& other) const
  {
    return !(*this == other);
  }*/


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

			XmlRpcUtil.log(4, "Trying to get size of something without a size! -- type=%d", _type);
			throw new XmlRpcException("type error");
		}
	}

	// Checks for existence of struct member
	bool hasMember(string name)
	{
		return _type == ValueType.TypeStruct && asStruct.ContainsKey(name);
	}

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
      result = boolFromXml(valueXml, offset);
    else if (typeTag == I4_TAG || typeTag == INT_TAG)
      result = intFromXml(valueXml, offset);
    else if (typeTag == DOUBLE_TAG)
      result = doubleFromXml(valueXml, offset);
    else if (typeTag.empty() || typeTag == STRING_TAG)
      result = stringFromXml(valueXml, offset);
    else if (typeTag == DATETIME_TAG)
      result = timeFromXml(valueXml, offset);
    else if (typeTag == BASE64_TAG)
      result = binaryFromXml(valueXml, offset);
    else if (typeTag == ARRAY_TAG)
      result = arrayFromXml(valueXml, offset);
    else if (typeTag == STRUCT_TAG)
      result = structFromXml(valueXml, offset);
    // Watch for empty/blank strings with no <string>tag
    else if (typeTag == VALUE_ETAG)
    {
      offset = afterValueOffset;   // back up & try again
      result = stringFromXml(valueXml, offset);
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
		string ret;
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
    std::string xml = VALUE_TAG;
    xml += BOOLEAN_TAG;
    xml += (_value.asBool ? "1" : "0");
    xml += BOOLEAN_ETAG;
    xml += VALUE_ETAG;
    return xml;
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
    char buf[256];
    snprintf(buf, sizeof(buf)-1, "%d", _value.asInt);
    buf[sizeof(buf)-1] = 0;
    std::string xml = VALUE_TAG;
    xml += I4_TAG;
    xml += buf;
    xml += I4_ETAG;
    xml += VALUE_ETAG;
    return xml;
  }

  // Double
  bool doubleFromXml(string valueXml, out int offset)
  {
    const char* valueStart = valueXml.c_str() + *offset;
    char* valueEnd;
    double dvalue = strtod(valueStart, &valueEnd);
    if (valueEnd == valueStart)
      return false;

    _type = TypeDouble;
    _value.asDouble = dvalue;
    *offset += int(valueEnd - valueStart);
    return true;
  }

  string doubleToXml()
  {
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
  }

  // String
  bool XmlRpcValue::stringFromXml(std::string const& valueXml, int* offset)
  {
    size_t valueEnd = valueXml.find('<', *offset);
    if (valueEnd == std::string::npos)
      return false;     // No end tag;

    _type = TypeString;
    _value.asString = new std::string(XmlRpcUtil::xmlDecode(valueXml.substr(*offset, valueEnd-*offset)));
    *offset += int(_value.asString->length());
    return true;
  }

  std::string XmlRpcValue::stringToXml() const
  {
    std::string xml = VALUE_TAG;
    //xml += STRING_TAG; optional
    xml += XmlRpcUtil::xmlEncode(*_value.asString);
    //xml += STRING_ETAG;
    xml += VALUE_ETAG;
    return xml;
  }

  // DateTime (stored as a struct tm)
  bool XmlRpcValue::timeFromXml(std::string const& valueXml, int* offset)
  {
    size_t valueEnd = valueXml.find('<', *offset);
    if (valueEnd == std::string::npos)
      return false;     // No end tag;

    std::string stime = valueXml.substr(*offset, valueEnd-*offset);

    struct tm t;
    if (sscanf(stime.c_str(),"%4d%2d%2dT%2d:%2d:%2d",&t.tm_year,&t.tm_mon,&t.tm_mday,&t.tm_hour,&t.tm_min,&t.tm_sec) != 6)
      return false;

    t.tm_isdst = -1;
    _type = TypeDateTime;
    _value.asTime = new struct tm(t);
    *offset += int(stime.length());
    return true;
  }

  std::string XmlRpcValue::timeToXml() const
  {
    struct tm* t = _value.asTime;
    char buf[20];
    snprintf(buf, sizeof(buf)-1, "%4d%02d%02dT%02d:%02d:%02d", 
      t->tm_year,t->tm_mon,t->tm_mday,t->tm_hour,t->tm_min,t->tm_sec);
    buf[sizeof(buf)-1] = 0;

    std::string xml = VALUE_TAG;
    xml += DATETIME_TAG;
    xml += buf;
    xml += DATETIME_ETAG;
    xml += VALUE_ETAG;
    return xml;
  }


  // Base64
  bool XmlRpcValue::binaryFromXml(std::string const& valueXml, int* offset)
  {
    size_t valueEnd = valueXml.find('<', *offset);
    if (valueEnd == std::string::npos)
      return false;     // No end tag;

    _type = TypeBase64;
    std::string asString = valueXml.substr(*offset, valueEnd-*offset);
    _value.asBinary = new BinaryData();
    // check whether base64 encodings can contain chars xml encodes...

    // convert from base64 to binary
    int iostatus = 0;
	  base64<char> decoder;
    std::back_insert_iterator<BinaryData> ins = std::back_inserter(*(_value.asBinary));
		decoder.get(asString.begin(), asString.end(), ins, iostatus);

    *offset += int(asString.length());
    return true;
  }


  std::string XmlRpcValue::binaryToXml() const
  {
    // convert to base64
    std::vector<char> base64data;
    int iostatus = 0;
	  base64<char> encoder;
    std::back_insert_iterator<std::vector<char> > ins = std::back_inserter(base64data);
		encoder.put(_value.asBinary->begin(), _value.asBinary->end(), ins, iostatus, base64<>::crlf());

    // Wrap with xml
    std::string xml = VALUE_TAG;
    xml += BASE64_TAG;
    xml.append(base64data.begin(), base64data.end());
    xml += BASE64_ETAG;
    xml += VALUE_ETAG;
    return xml;
  }


  // Array
  bool XmlRpcValue::arrayFromXml(std::string const& valueXml, int* offset)
  {
    if ( ! XmlRpcUtil::nextTagIs(DATA_TAG, valueXml, offset))
      return false;

    _type = TypeArray;
    _value.asArray = new ValueArray;
    XmlRpcValue v;
    while (v.fromXml(valueXml, offset))
      _value.asArray->push_back(v);       // copy...

    // Skip the trailing </data>
    (void) XmlRpcUtil::nextTagIs(DATA_ETAG, valueXml, offset);
    return true;
  }


  // In general, its preferable to generate the xml of each element of the
  // array as it is needed rather than glomming up one big string.
  string arrayToXml() const
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
    _value.asStruct = new ValueStruct;

    while (XmlRpcUtil::nextTagIs(MEMBER_TAG, valueXml, offset)) {
      // name
      const std::string name = XmlRpcUtil::parseTag(NAME_TAG, valueXml, offset);
      // value
      XmlRpcValue val(valueXml, offset);
      if ( ! val.valid()) {
        invalidate();
        return false;
      }
      const std::pair<const std::string, XmlRpcValue> p(name, val);
      _value.asStruct->insert(p);

      (void) XmlRpcUtil::nextTagIs(MEMBER_ETAG, valueXml, offset);
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
      xml += XmlRpcUtil::xmlEncode(it->first);
      xml += NAME_ETAG;
      xml += it->second.toXml();
      xml += MEMBER_ETAG;
    }

    xml += STRUCT_ETAG;
    xml += VALUE_ETAG;
    return xml;
  }

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
  }
		/*
        #region P/Invoke

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

        #endregion
		*/
        public TypeEnum Type
        {
            [DebuggerStepThrough]
            get
            {
                int balls = gettype(instance);
                if (balls < 0 || balls >= ValueTypeHelper._typearray.Length)
                {
                    return TypeEnum.TypeInvalid;
                }
                return ValueTypeHelper._typearray[balls];
            }
            [DebuggerStepThrough]
            set
            {
                SegFault();
                settype(instance, (int) value);
            }
        }

        public bool Valid
        {
            [DebuggerStepThrough]
            get
            {
                SegFault();
                return valid(__instance);
            }
        }

        public int Size
        {
            [DebuggerStepThrough]
            get
            {
                SegFault();
                if (!Valid || Type == TypeEnum.TypeInvalid || Type == TypeEnum.TypeIDFK)
                {
                    return 0;
                }
                if (Type != TypeEnum.TypeString && Type != TypeEnum.TypeStruct && Type != TypeEnum.TypeArray)
                    return 0;
                return getsize(instance);
            }
            [DebuggerStepThrough]
            set
            {
                SegFault();
                setsize(instance, value);
            }
        }

        [DebuggerStepThrough]
        public void Set<T>(T t)
        {
            if ("" is T)
            {
                set(instance, (string) (object) t);
            }
            else if (0 is T)
            {
                set(instance, (int) (object) t);
            }
            else if (this is T)
            {
                set(instance, ((XmlRpcValue) (object) t).instance);
            }
            else if (true is T)
            {
                set(instance, (bool) (object) t);
            }
            else if (0d is T)
            {
                set(instance, (double) (object) t);
            }
        }

        [DebuggerStepThrough]
        public void Set<T>(int key, T t)
        {
            this[key].Set(t);
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
            IntPtr nested = get(instance, key);
            return LookUp(nested);
        }

        [DebuggerStepThrough]
        private XmlRpcValue Get(string key)
        {
            IntPtr nested = get(instance, key);
            return LookUp(nested);
        }

        [DebuggerStepThrough]
        public int GetInt()
        {
            SegFault();
            return getint(__instance);
        }

        [DebuggerStepThrough]
        public string GetString()
        {
            SegFault();
            return Marshal.PtrToStringAnsi(getstring(__instance));
        }

        [DebuggerStepThrough]
        public bool GetBool()
        {
            SegFault();
            return getbool(__instance);
        }

        [DebuggerStepThrough]
        public double GetDouble()
        {
            SegFault();
            return getdouble(__instance);
        }

        [DebuggerStepThrough]
        public override string ToString()
        {
            if (__instance == IntPtr.Zero)
                return "(NULL)";
            string s = Marshal.PtrToStringAnsi(tostring(instance));
            return s;
        }
#endif
    }

    
}