using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XmlRpc_Wrapper;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace rosmaster
{
    public class ParamServer
    {
        public void _get_param_names(ref List<String> names, String key, Dictionary<String, Param> d)
        {
            foreach (KeyValuePair<String, Param> pair in d)
            {
                if (!pair.Value.isNotANamespace)
                {
                    _get_param_names(ref names, Names.ns_join(key, pair.Key), pair.Value);
                }
                else
                {
                    names.Add(Names.ns_join(key, pair.Key));
                }
            }
        }

        public void _compute_all_keys()
        {
            //TODO: This isn't necessary atm....
        }

        public void compute_param_updates()
        {
            //TODO: This isn't necessary atm....
        }
 }
        public class ParamDictionary : ParamServer
        {
            private Param parameters;
            private RegistrationManager reg_manager;
            public ParamDictionary(RegistrationManager _reg_manager)
            {
                reg_manager = _reg_manager;
                parameters = new Param();
            }

            public List<String> get_param_names()
            {
                List<String> param_names = new List<String>();
                _get_param_names(ref param_names, "/", parameters);
                return param_names;
            }

            public String search_param(String ns, String key)
            {
                if (key == "" || Names.is_private(key))
                    throw new Exception("KEY IS EMPTY OR PRIVATE. GTFO");
                if (!Names.is_global(ns))
                    throw new Exception("KEY NOT IN GLOBAL NS? WUT?");
                if (has_param(key))
                    return key;
                else
                    return null;

                //we only search for the first namespace in the key to check for a match
                String[] key_namespaces = key.Split('/');
                String key_ns = key_namespaces[0];

                //corner case: test initial namespace first
                String search_key = Names.ns_join(ns, key_ns);
                if (has_param(search_key))
                    return Names.ns_join(ns, key); //resolve full key

                String[] namespaces = key.Split('/');

                for (int i = namespaces.Length; i > 1; i--)
                {
                    search_key = "/" + String.Join("/", namespaces, 0, i) + "/" + String.Join("/", key_ns);
                    if (has_param(search_key))
                    { //we have a mach on the namespace of the key, so compose a full key and return it
                        String full_key = "/" + String.Join("/", namespaces, 0, i) + "/" + String.Join("/", key);
                        return full_key;
                    }
                }
                return null;
            }

            public XmlRpcValue get_param(String key)
            {
                Param val = parameters;//ObjectCopy.Clone(parameters);// new Param(parameters);

                if (key != "/")
                {
                    var namespaces = key.Split('/').Select(tag => tag.Trim()).Where(tag => !String.IsNullOrEmpty(tag));
                    foreach (String ns in namespaces)
                    {
                        if (val.ContainsKey(ns)) //TODO: THIS
                        {
                            if (val[ns].isNotANamespace)
                            {
                                if (val[ns].type == XmlRpc_Wrapper.XmlRpcValue.ValueType.TypeString)
                                    return new XmlRpcValue(val[ns].jordanString);
                                else if (val[ns].type == XmlRpc_Wrapper.XmlRpcValue.ValueType.TypeInt)
                                    return new XmlRpcValue(val[ns].jordanInt);
                                else if (val[ns].type == XmlRpc_Wrapper.XmlRpcValue.ValueType.TypeDouble)
                                    return new XmlRpcValue(val[ns].jordanDouble);
                                else
                                    throw new Exception("FUCKKKKKKKKKKKKKKKKKKKKKKK");
                            }
                            else
                                val = val[ns];
                            
                                
                        }
                    }
                }
                return (XmlRpcValue)val.get();
            }

            public void set_param(String key, XmlRpcValue value, Func<Dictionary<String, Tuple<String, XmlRpc_Wrapper.XmlRpcValue>>, int> notify_task = null)
            {
                
                if (key == "/") //create root
                {
                    parameters = new Param(value);
                }
                else //add branch
                {
                    String[] namespaces = key.Split('/');
                    String value_key = namespaces.Last();
                    namespaces = namespaces.Take(namespaces.Length - 1).ToArray();
                    Dictionary<String, Param> d = parameters;

                    foreach (String ns in namespaces) //descend tree to the node we are setting
                    {
                        if (ns != "")
                        {
                            if (!d.ContainsKey(ns))
                            {
                                Param new_parameters = new Param();
                                d.Add(ns, new_parameters);
                                d = new_parameters;
                            }
                            else
                            {
                                var val = d[ns];
                                if (val.isNotANamespace)
                                    d[ns] = val = new Param();
                                d = val;
                            }
                        }
                    }
                    if (value.Type == XmlRpc_Wrapper.XmlRpcValue.ValueType.TypeString)
                    {
                        d[value_key] = new Param(value.GetString());
                    }
                    else if (value.Type == XmlRpc_Wrapper.XmlRpcValue.ValueType.TypeInt)
                    {
                        d[value_key] = new Param(value.GetInt());
                    }
                    else if (value.Type == XmlRpc_Wrapper.XmlRpcValue.ValueType.TypeDouble)
                    {
                        d[value_key] = new Param(value.GetDouble());
                    }
                    else d[value_key] = new Param(value);
                }
                if (notify_task != null)
                {
                    //Boolean updates = compute_param_updates(reg_manager.param_subscribers, key, value)
                    //if(updates)
                    //notify_task(updates);
                    // TODO : ADD NOTIFY TASK DEALY
                }
            }

            /// <summary>
            /// Subscribe to a param
            /// </summary>
            /// <param name="key">Param key</param>
            /// <param name="caller_id">Caller id</param>
            /// <param name="caller_api">Caller api</param>
            /// <returns></returns>
            public XmlRpcValue subscribe_param(String key, String caller_id, String caller_api)
            {
                if (key != "/")
                    key = Names.canonicalize_name(key) + "/";
                XmlRpcValue val;
                try
                {
                    val = get_param(key);
                }
                catch (KeyNotFoundException e)
                {
                    val = new XmlRpcValue();
                }
                reg_manager.register_param_subscriber(key, caller_id, caller_api);
                return val;
            }

            /// <summary>
            /// Unsubscribe from a parameter
            /// </summary>
            /// <param name="key">Parameter Key</param>
            /// <param name="caller_id">ID of the caller</param>
            /// <param name="caller_api">API of the caller</param>
            /// <returns>Error code, 1 if successful</returns>
            public ReturnStruct unsubscribe_param(String key, String caller_id, String caller_api) //USED TO BE TUPPLE
            {
                if (key != "/")
                    key = Names.canonicalize_name(key) + "/";
                return reg_manager.unregister_param_subscriber(key, caller_id, caller_api);

            }

            /// <summary>
            /// Delete the parameter in the parameter dictionary
            /// </summary>
            /// <param name="key">Parameter key</param>
            /// <param name="notify_task">Function to call with subscirber updates. Updates are in the form 
            /// [(subscribers, param_key, param_Value)*] The empty dictionary represents an unset parameter</param>
            public void delete_param(String key, Func<Dictionary<String, Tuple<String, XmlRpc_Wrapper.XmlRpcValue>>, int> notify_task = null)
            {
                if (key == "/")
                    throw new Exception("CANNOT DELETE ROOT OF NODE.");

                String[] namespaces = key.Split('/');
                String value_key = namespaces.Last();
                namespaces = namespaces.Take(namespaces.Length - 1).ToArray();
                Dictionary<String, Param> d = parameters;


                foreach (String ns in namespaces) //descend tree to the node we are setting
                    {
                        if (!d.ContainsKey(ns))
                        {
                            throw new KeyNotFoundException("WHATCHA BE DOING? YOU CAN'T DELETE THINGS THAT DON'T EXIST SILLY");
                        }
                        else
                        {
                            d = d[ns];
                        }
                    }
                if (d.ContainsKey(value_key))
                    throw new KeyNotFoundException("GO AWAY");

                d.Remove(value_key);
                
                if(notify_task != null)
                {
                    //Boolean updates = compute_param_updates(reg_manager.param_subscribers, key, value)
                    //if(updates)
                    //notify_task(updates);
                    // TODO : ADD NOTIFY TASK DEALY
                }

            }

            /// <summary>
            /// Test for parameter existence
            /// </summary>
            /// <param name="key"> parameter key</param>
            /// <returns>True if parameter is set, False otherwise</returns>
            public Boolean has_param(String key)
            {
                try
                {
                    get_param(key);
                    return true;
                }
                catch (KeyNotFoundException e)
                {
                    return false;
                }

            }


        }

        [Serializable]
        public class Param : Dictionary<string, Param>
        {

            public bool isNotANamespace
            {
                get
                {
                    return Count == 0 && jordanString != null && jordanInt != null && jordanDouble != null;
                }
            }

            //private string _value;
            public XmlRpc_Wrapper.XmlRpcValue _value = null;
            public XmlRpc_Wrapper.XmlRpcValue.ValueType type;
            
            public string getString()
            {
                if (type == null)
                    return null;
                if (type == XmlRpc_Wrapper.XmlRpcValue.ValueType.TypeString)
                    return jordanString;
                else if (type == XmlRpc_Wrapper.XmlRpcValue.ValueType.TypeInt)
                    return _value.ToString();
                else if (type == XmlRpc_Wrapper.XmlRpcValue.ValueType.TypeDouble)
                    return _value.ToString();

                return null;
            }
            public XmlRpcValue getParam(string k)
            {
                return this._value;
            }

            public object get(string k = null)
            {
                if (k != null)
                {
                    return (object)getParam(k);
                }
                else
                    return (object)getString();
            }
            public String jordanString = "";
            public int jordanInt;
            public double jordanDouble;
            public Param()
            {

            }
            public Param(XmlRpc_Wrapper.XmlRpcValue val)
            {
                _value = val;
            }

            public Param(String sigh)
            {
                jordanString = sigh;
                type = XmlRpc_Wrapper.XmlRpcValue.ValueType.TypeString;
            }
            public Param(int sigh)
            {
                jordanInt = sigh;
                type = XmlRpc_Wrapper.XmlRpcValue.ValueType.TypeInt;
            }
            public Param(double sigh)
            {
                jordanDouble = sigh;
                type = XmlRpc_Wrapper.XmlRpcValue.ValueType.TypeDouble;
            }
            public Param(string key, Param val)
            {
                this[key] = val;
            }

            public Param(SerializationInfo info, StreamingContext context)

      : base(info, context) {

    }

            /*public Object Clone()
            {

                Param cloned = new Param();
                foreach (KeyValuePair<String, Param> pair in this)
                {
                    cloned.Add(pair.Key, pair.Value);
                }
                if(this.isNotANamespace)
                    
                return this;
            }*/

            public Object Clone(Param p)
            {
                return this;
            }
        }
    }