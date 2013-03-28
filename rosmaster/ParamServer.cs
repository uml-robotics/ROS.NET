using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace rosmaster
{
    public class ParamServer
    {
        public void _get_param_names(ref List<String> names, String key, Dictionary<String, Param> d)
        {
            foreach (KeyValuePair<String, Param> pair in d)
            {
                if (pair.Value.isANamespace)
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
            private Dictionary<String, Param> parameters;
            private RegistrationManager reg_manager;
            public ParamDictionary(RegistrationManager _reg_manager)
            {
                reg_manager = _reg_manager;
                parameters = new Dictionary<string, Param>();
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

            public Dictionary<String, Param> get_param(String key)
            {
                var val = parameters;

                if (key != "/")
                {
                    var namespaces = key.Split('/').Select(tag => tag.Trim()).Where(tag => !String.IsNullOrEmpty(tag));
                    foreach (String ns in namespaces)
                    {
                        val = val[ns];
                    }
                }
                return val;
            }

            public void set_param(String key, Param value, Func<Dictionary<String, Tuple<String, XmlRpc_Wrapper.XmlRpcValue>>, int> notify_task = null)
            {
                if (key == "/") //create root
                {
                    parameters = value;
                }
                else //add branch
                {
                    String[] namespaces = key.Split('/');
                    String value_key = namespaces.Last();
                    namespaces = namespaces.Take(namespaces.Length - 1).ToArray();
                    Dictionary<String, Param> d = parameters;

                    foreach (String ns in namespaces) //descend tree to the node we are setting
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
                            if (!val.isANamespace)
                                d[ns] = val = new Param();
                            d = val;
                        }
                    }
                    d[value_key] = value;
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
            public Dictionary<String, Param> subscribe_param(String key, String caller_id, String caller_api)
            {
                if (key != "/")
                    key = Names.canonicalize_name(key) + "/";
                Dictionary<String, Param> val;
                try
                {
                    val = get_param(key);
                }
                catch (KeyNotFoundException e)
                {
                    val = new Dictionary<string, Param>();
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
            public int unsubscribe_param(String key, String caller_id, String caller_api) //USED TO BE TUPPLE
            {
                int rtn = 0;
                String str = "";
                if (key != "/")
                    key = Names.canonicalize_name(key) + "/";
                return reg_manager.unregister_param_subscriber(key, caller_id, caller_api, ref str, ref rtn);

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

        public class Param : Dictionary<string, Param>
        {
            public bool isANamespace
            {
                get
                {
                    return Count == 0 && _value != null;
                }
            }

            //private string _value;
            private XmlRpc_Wrapper.XmlRpcValue _value = null;
            
            public string getString()
            {
                if (_value.Type == XmlRpc_Wrapper.TypeEnum.TypeString)
                    return _value.ToString();
                else if (_value.Type == XmlRpc_Wrapper.TypeEnum.TypeInt)
                    return _value.ToString();

                return null;
            }
            public Param getParam(string k)
            {
                return this[k];
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

            public Param()
            {

            }
            public Param(XmlRpc_Wrapper.XmlRpcValue val)
            {
                _value = val;
            }
            public Param(string key, Param val)
            {
                this[key] = val;
            }
        }
    }