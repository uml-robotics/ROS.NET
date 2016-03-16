// File: XmlRpcValue.cs
// Project: XmlRpc_Wrapper
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 11/18/2015
// Updated: 02/10/2016

#region USINGZ

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;

#endregion

namespace XmlRpc_Wrapper
{
    //TODO: OPERATOR GARBAGE?
    [Serializable]
    public class XmlRpcValue // : IDisposable
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
        private static string VALUE_TAG = "value";
        private static string BOOLEAN_TAG = "boolean";
        private static string DOUBLE_TAG = "double";
        private static string INT_TAG = "int";
        private static string I4_TAG = "i4";
        private static string STRING_TAG = "string";
        private static string DATETIME_TAG = "dateTime.iso8601";
        private static string BASE64_TAG = "base64";
        private static string ARRAY_TAG = "array";
        private static string DATA_TAG = "data";
        private static string STRUCT_TAG = "struct";
        private static string MEMBER_TAG = "member";
        private static string NAME_TAG = "name";

        // Format strings
        private string _doubleFormat = "%.16g";

        // Type tag and values
        private ValueType _type;
        public XmlRpcValue[] asArray;
        public byte[] asBinary;

        public bool asBool;
        public double asDouble;
        public int asInt;
        public string asString;
        public Dictionary<string, XmlRpcValue> asStruct;
        public tm asTime;

        public XmlRpcValue()
        {
            _type = ValueType.TypeInvalid;
        }

        public XmlRpcValue(params Object[] initialvalues)
            : this()
        {
            SetArray(initialvalues.Length);
            for (int i = 0; i < initialvalues.Length; i++)
            {
                setFromObject(i, initialvalues[i]);
            }
        }

        private void setFromObject(int i, object o)
        {
            int ires = 0;
            double dres = 0;
            bool bres = false;
            if (o == null || o is string)
                Set(i, o != null ? o.ToString() : "");
            else if (o is int && int.TryParse(o.ToString(), out ires))
                Set(i, ires);
            else if (o is double && double.TryParse(o.ToString(), out dres))
                Set(i, dres);
            else if (o is bool && bool.TryParse(o.ToString(), out bres))
                Set(i, bres);
            else
                Set(i, o.ToString());
        }
                catch (InvalidProgramException ex)
                {
                    //mono seems to disagree with either the 'is' operator OR with some aspect of the generics on the Set execution path
                    //It throws an IllegalProgramException - invalid IL code: IL_002e: isinst    0x1b00
                    XmlRpcUtil.error("XmlRpcValue(object[{0}])\t index={1} type={2} threw exception\n{3}", initialvalues.Length, i, initialvalues[i]==null ? "NULL" : ""+initialvalues[i].GetType(), ex);
                    throw ex;
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

        public int Length
        {
            get
            {
                switch (_type)
                {
                    case ValueType.TypeString:
                        return asString.Length;
                    case ValueType.TypeBase64:
                        return asBinary.Length;
                    case ValueType.TypeArray:
                        return asArray.Length;
                    case ValueType.TypeStruct:
                        return asStruct.Count;
                    default:
                        break;
                }

                XmlRpcUtil.log(XmlRpcUtil.XMLRPC_LOG_LEVEL.DEBUG, "Trying to get size of something without a size! -- type={0}", _type);
                throw new XmlRpcException("type error");
            }
        }

        public bool Valid
        {
            [DebuggerStepThrough]
            get { return _type != ValueType.TypeInvalid; }
        }

        public ValueType Type
        {
            [DebuggerStepThrough]
            get { return _type; }
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
                if (Type == ValueType.TypeString)
                    return asString.Length;
                if (Type == ValueType.TypeStruct)
                    return asStruct.Count;
                return 0;
            }
        }

        public XmlRpcValue this[int key]
        {
            [DebuggerStepThrough]
            get
            {
                EnsureArraySize(key);
                return Get(key);
            }
            [DebuggerStepThrough]
            set
            {
                EnsureArraySize(key);
                Set(key, value);
            }
        }

        public XmlRpcValue this[string key]
        {
            [DebuggerStepThrough]
            get { return Get(key); }
            [DebuggerStepThrough]
            set { Set(key, value); }
        }

        public void Dump()
        {
            // Dunno what to do here
        }

        // Clean up
        private void invalidate()
        {
            _type = ValueType.TypeInvalid;
            asStruct = null;
            asArray = null;
            asString = null;
            asBinary = null;
            asBool = false;
            asTime = null;
        }

        private void assertArray(int size)
        {
            if (_type == ValueType.TypeInvalid)
            {
                _type = ValueType.TypeArray;
                asArray = new XmlRpcValue[size];
            }
            else if (_type == ValueType.TypeArray)
            {
                if (asArray.Length < size)
                    Array.Resize(ref asArray, size);
            }
            else
                throw new XmlRpcException("type error: expected an array");
        }

        private void assertStruct()
        {
            if (_type == ValueType.TypeInvalid)
            {
                _type = ValueType.TypeStruct;
                asStruct = new Dictionary<string, XmlRpcValue>();
            }
            else if (_type != ValueType.TypeStruct)
                throw new XmlRpcException("type error: expected a struct");
        }

        // Predicate for tm equality
        private static bool tmEq(tm t1, tm t2)
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

            switch (_type)
            {
                case ValueType.TypeBoolean:
                    return asBool == other.asBool;
                case ValueType.TypeInt:
                    return asInt == other.asInt;
                case ValueType.TypeDouble:
                    return asDouble == other.asDouble;
                case ValueType.TypeDateTime:
                    return tmEq(asTime, other.asTime);
                case ValueType.TypeString:
                    return asString.Equals(other.asString);
                case ValueType.TypeBase64:
                    return asBinary == other.asBinary;
                case ValueType.TypeArray:
                    return asArray == other.asArray;

                // The map<>::operator== requires the definition of value< for kcc
                case ValueType.TypeStruct: //return *_value.asStruct == *other._value.asStruct;
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
                default:
                    break;
            }
            return true; // Both invalid values ...
        }

        // Works for strings, binary data, arrays, and structs.

        public void Copy(XmlRpcValue other)
        {
            switch (other._type)
            {
                case ValueType.TypeBoolean:
                    asBool = other.asBool;
                    break;
                case ValueType.TypeInt:
                    asInt = other.asInt;
                    break;
                case ValueType.TypeDouble:
                    asDouble = other.asDouble;
                    break;
                case ValueType.TypeDateTime:
                    asTime = other.asTime;
                    break;
                case ValueType.TypeString:
                    asString = other.asString;
                    break;
                case ValueType.TypeBase64:
                    asBinary = other.asBinary;
                    break;
                case ValueType.TypeArray:
                    asArray = other.asArray;
                    break;

                // The map<>::operator== requires the definition of value< for kcc
                case ValueType.TypeStruct: //return *_value.asStruct == *other._value.asStruct;
                    asStruct = other.asStruct;
                    break;
            }
            _type = other._type;
        }

        // Checks for existence of struct member
        public bool hasMember(string name)
        {
            return _type == ValueType.TypeStruct && asStruct.ContainsKey(name);
        }

        private void parseString(XmlNode node)
        {
            _type = ValueType.TypeString;
            asString = node.InnerText;
        }

        public bool fromXml(XmlNode value)
        {
            //int val = offset;
            //offset = 0;
            try
            {
                //XmlElement value = node["value"];
                if (value == null)
                    return false;

                string tex = value.InnerText;
                XmlElement val;
                if ((val = value[BOOLEAN_TAG]) != null)
                {
                    _type = ValueType.TypeBoolean;
                    int tmp = 0;
                    if (!int.TryParse(tex, out tmp))
                        return false;
                    if (tmp != 0 && tmp != 1)
                        return false;
                    asBool = (tmp == 0 ? false : true);
                }
                else if ((val = value[I4_TAG]) != null)
                {
                    _type = ValueType.TypeInt;
                    return int.TryParse(tex, out asInt);
                }
                else if ((val = value[INT_TAG]) != null)
                {
                    _type = ValueType.TypeInt;
                    return int.TryParse(tex, out asInt);
                }
                else if ((val = value[DOUBLE_TAG]) != null)
                {
                    _type = ValueType.TypeDouble;
                    return double.TryParse(tex, out asDouble);
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
                    _type = ValueType.TypeString;
                    asString = tex;
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
                else
                {
                    _type = ValueType.TypeString;
                    asString = tex;
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
            XmlElement el = null;
            switch (_type)
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
                    var base64 = Convert.ToBase64String(asBinary);
                    el.AppendChild(doc.CreateTextNode(base64));
                    break;
                case ValueType.TypeArray:
                    el = doc.CreateElement(ARRAY_TAG);
                    var elData = doc.CreateElement(DATA_TAG);
                    el.AppendChild(elData);
                    for (int i = 0; i < Size; i++)
                    {
                        asArray[i].toXml(doc, elData);
                    }
                    break;
                case ValueType.TypeStruct:
                    el = doc.CreateElement(STRUCT_TAG);
                    foreach (var record in asStruct)
                    {
                        var member = doc.CreateElement(MEMBER_TAG);
                        var name = doc.CreateElement(NAME_TAG);
                        name.AppendChild(doc.CreateTextNode(record.Key));
                        member.AppendChild(name);
                        record.Value.toXml(doc, member);
                        el.AppendChild(member);
                    }
                    break;
            }

            if (el != null)
                root.AppendChild(el);

            parent.AppendChild(root);
            return root;
        }

        public void Set<T>(T t)
        {
            if (t is string)
            {
                _type = ValueType.TypeString;
                asString = (string)(object)t;
            }
            else if (0 is T)
            {
                _type = ValueType.TypeInt;
                asInt = (int)(object)t;
            }
            else if (this is T)
            {
                Copy(t as XmlRpcValue);
            }
            else if (t is bool)
            {
                asBool = (bool)(object)t;
                _type = ValueType.TypeBoolean;
            }
            else if (0d is T)
            {
                asDouble = (double)(object)t;
                _type = ValueType.TypeDouble;
            }
            else if (t is XmlRpcValue)
            {
                Copy(t as XmlRpcValue);
            }
        }

        public void EnsureArraySize(int size)
        {
            if (_type != ValueType.TypeInvalid && _type != ValueType.TypeArray)
                throw new Exception("Converting to array existing value");
            int before = 0;
            if (asArray != null)
            {
                before = asArray.Length;
                if (asArray.Length < size + 1)
                    Array.Resize(ref asArray, size + 1);
            }
            else
                asArray = new XmlRpcValue[size + 1];
            for (int i = before; i < asArray.Length; i++)
                asArray[i] = new XmlRpcValue();
            _type = ValueType.TypeArray;
        }

        public void Set<T>(int key, T t)
        {
            EnsureArraySize(key);
            if (asArray[key] == null)
            {
                asArray[key] = new XmlRpcValue();
            }
            this[key].Set<T>(t);
        }

        public void SetArray(int maxSize)
        {
            _type = ValueType.TypeArray;
            asArray = new XmlRpcValue[maxSize];
        }

        [DebuggerStepThrough]
        public void Set<T>(string key, T t)
        {
            this[key].Set<T>(t);
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
                return (T)(object)GetString();
            }
            else if (0 is T)
            {
                return (T)(object)GetInt();
            }
            else if (this is T)
            {
                return (T)(object)this;
            }
            else if (true is T)
            {
                return (T)(object)GetBool();
            }
            else if (0d is T)
            {
                return (T)(object)GetDouble();
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
            return asArray[key];
        }

        [DebuggerStepThrough]
        private XmlRpcValue Get(string key)
        {
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

        public class tm
        {
            public int tm_hour; /* hours since midnight - [0,23] */
            public int tm_isdst; /* daylight savings time flag */
            public int tm_mday; /* day of the month - [1,31] */
            public int tm_min; /* minutes after the hour - [0,59] */
            public int tm_mon; /* months since January - [0,11] */
            public int tm_sec; /* seconds after the minute - [0,59] */
            public int tm_wday; /* days since Sunday - [0,6] */
            public int tm_yday; /* days since January 1 - [0,365] */
            public int tm_year; /* years since 1900 */
        };

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
