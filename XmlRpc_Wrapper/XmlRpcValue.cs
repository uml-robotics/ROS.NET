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

namespace XmlRpc_Wrapper
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
            switch (other._type)
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
            _type = other._type;
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
                else
                {
                    this._type = ValueType.TypeString;
                    this.asString = tex;
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
                    foreach (var record in this.asStruct)
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

            if(el != null)
                root.AppendChild(el);

            parent.AppendChild(root);
            return root;
        }

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
            else if (t is XmlRpcValue)
            {
                Copy(t as XmlRpcValue);
            }
        }

        public void EnsureArraySize(int size)
        {
            if (_type != ValueType.TypeInvalid && _type != ValueType.TypeArray)
                throw new Exception("Converting to array existing value");
            if (asArray != null)
            {
                if (asArray.Length < size+1)
                    Array.Resize(ref asArray, size+1);
            }
            else
                asArray = new XmlRpcValue[size+1];
            this._type = ValueType.TypeArray;
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
        
        [DebuggerStepThrough]
        public void Set<T>(int key, T t)
        {
            EnsureArraySize(key);
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