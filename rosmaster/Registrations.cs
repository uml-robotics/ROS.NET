using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace rosmaster
{
    public class Registrations
    {
        public static readonly int TOPIC_SUBSCRIPTIONS = 1;
        public static readonly int TOPIC_PUBLICATIONS = 2;
        public static readonly int SERVICE = 3;
        public static readonly int PARAM_SUBSCRIPTIONS = 4;

        public static readonly String[] TYPE = { "TOPIC_SUBSCRIPTIONS", "TOPIC_PUBLICATIONS", "SERVICE", "PARAM_SUBSCRIPTIONS" };

        /// <summary>
        /// Key:   Value:
        /// </summary>
        public Dictionary<String, List<List<String>>> map;
        public Dictionary<String, List<String>> service_api_map;
        //public Tuple<String, String> key;


        public int type;
        public List<List<String>> providers;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type_">  </param>
        public Registrations(int type_)
        {
            if ( type_ == TOPIC_SUBSCRIPTIONS || type_ == TOPIC_PUBLICATIONS || type_ == SERVICE || type_ == PARAM_SUBSCRIPTIONS)
            {
                type = type_;
                map = new Dictionary<String, List<List<String>>>();
                //service_api_map = new Dictionary<String, Tuple<String ,String>>();
                //providers = Tuple<String, String>();
            }
            else throw new Exception("TYPE UNSUPPORTED: " + type_);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool __nonzero__()
        {
            return map.Count != 0;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Dictionary<String, List<List<String>>>.Enumerator IterKeys()
        {
            return map.GetEnumerator();
            //return new IEnumerator<Dictionary<int, String>(map);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        public String get_service_api(String service)
        {
            if (service_api_map != null && service_api_map.ContainsKey(service))
            {
                return service_api_map[service][1];
            }
            else 
                return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public List<String> get_apis(String key)
        {
            if (map.ContainsKey(key))
                return map[key][1];
            else
                return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool __contains__(String key)
        {
            return map.ContainsKey(key);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public List<List<String>> __getitem__(String key)
        {
            return map[key];
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool has_key(String key)
        {
            return map.ContainsKey(key);
        }

        public List<List<String>> get_state()
        {
            List<List<String>> retval = new List<List<String>>();

            foreach (KeyValuePair<String, List<List<String>>> pair in map)
            {
                List<String> value = new List<String>();
                value.Add(pair.Key);
               // foreach(String s in pair.Value)
                    value.AddRange(pair.Value[0]);
                    value.AddRange(pair.Value[1]);
                retval.Add(value);
            }
            return retval;
        }


        public void register(String key, String caller_id, String caller_api, String service_api=null)
        {
            List<String> tmplist = new List<String>();
            tmplist.Add(caller_id);
            tmplist.Add(caller_api);

            if (map.ContainsKey(key) && service_api == null)
            {
                providers = map[key];
                if (!providers.Contains(tmplist) )
                {
                    providers.Add(tmplist); //CHANGEING ID || API FOR EXISTING TOPIC? IF SOMETHING BREAKS, THIS IS IT
                }
            }
            else
            {

                 map[key] = providers = new List<List<String>>();
                 map[key].Add(tmplist);
                 //map.Add(key,providers.First());
            }

            if(service_api != null)
            {
                if (service_api_map == null)
                {
                    service_api_map = new Dictionary<string,List<String>>();
                }

                service_api_map[key] = tmplist;
            }else if(type == Registrations.SERVICE)
            {
                throw new Exception("service_api must be specified for Registrations.SERVICE");
                //raise rosmaster.exceptions.InternalException("service_api must be specified for Registrations.SERVICE");
            }
        }

        public ReturnStruct unregister(String key, String caller_id, String caller_api, String service_api = null)
        {
            String msg;
            int val;
            if (service_api != null)
            {
                if (service_api_map == null)
                {
                    msg = String.Format("[{0}] is not a provider of [{1}]",caller_id,key);
                    val = 0;
                    return new ReturnStruct(1, msg, new XmlRpc_Wrapper.XmlRpcValue(val));
                }
                List<String> tmplist = new List<string>();
                tmplist.Add(caller_id);
                tmplist.Add(caller_api);
                if (service_api_map[key] != tmplist)
                {
                    msg = String.Format("[{0}] is no longer the current service api handle for [{1}]", service_api, key);
                    val = 0;
                    return new ReturnStruct(1, msg, new XmlRpc_Wrapper.XmlRpcValue(val));
                }
                else
                {
                    service_api_map.Remove(key);// = null;
                    map.Remove(key);// [key] = null;
                }
                msg = String.Format("Unregistered [{0}] as provider of [{1}]", caller_id, key);
                val = 1;
                return new ReturnStruct(1, msg, new XmlRpc_Wrapper.XmlRpcValue(val));
            }else if(type == Registrations.SERVICE)
            {
                throw new Exception("service_api must be specified for Registrations.SERVICE");
                //RAISE THE ROOF
            }
            else
            {
                providers = map[key];
                List<String> tmplist = new List<string>();
                tmplist.Add(caller_id);
                tmplist.Add(caller_api);
                if (providers.Contains(tmplist) )
                {
                    map.Remove(key);
                    //providers.Remove(new Tuple<String, String>(caller_id, caller_api));

                    msg = String.Format("Unregistered [{0}] as provider of [{1}]", caller_id, key);
                    val = 1;
                    return new ReturnStruct(1, msg, new XmlRpc_Wrapper.XmlRpcValue(val));
                }
                msg = String.Format("[{0}] is not a known provider of [{1}]", caller_id, key);
                val = 0;
                return new ReturnStruct(1, msg, new XmlRpc_Wrapper.XmlRpcValue(val));
            }

        }

        public void unregister_all( String caller_id)
        {
            List<String> dead_keys = new List<String>();

            foreach (String key in map.Keys)
            {
                providers = map[key];
                List<List<String>> to_remove = new List<List<string>>();// = new Tuple<String,String>();

                foreach (List<String> l in map[key])
                {
                    if (l[0] == caller_id)
                    {
                        to_remove.Add(l);
                    }
                }

                foreach (List<String> l in to_remove)
                {
                    map[key].Remove(l);
                }

                if (!map.ContainsKey(key))
                {
                    dead_keys.Add(key);
                }

            }
            foreach (String k in dead_keys)
            {
                map.Remove(k);
            }

            if (type == Registrations.SERVICE && service_api_map != null)
            {
                dead_keys = new List<String>();

                foreach (String key in service_api_map.Keys)
                {
                    if (service_api_map[key][0] == caller_id)
                    {
                        dead_keys.Add(key);
                    }

                }
                foreach (String key in dead_keys)
                {
                    service_api_map.Remove(key);
                }
            }
        }
    }

    public class RegistrationManager
    {
        /*
         *     Stores registrations for Master. 
         *     RegistrationManager is not threadsafe, so access must be externally locked as appropriate 
         */

        public Registrations publishers;
        public Registrations subscribers;
        public Registrations services;
        public Registrations param_subscribers;

        /// <summary>
        /// Param 1 = caller_id
        /// Param 2 = NodeRef
        /// </summary>
        Dictionary<String, NodeRef> nodes;

        public RegistrationManager()
        {
            nodes = new Dictionary<string, NodeRef>();
            //thread_pool

            publishers = new Registrations(Registrations.TOPIC_PUBLICATIONS);
            subscribers = new Registrations(Registrations.TOPIC_SUBSCRIPTIONS);
            services = new Registrations(Registrations.SERVICE);
            param_subscribers = new Registrations(Registrations.PARAM_SUBSCRIPTIONS);
        }

        public bool reverse_lookup(String caller_api)
        {

            return true;
        }

        public NodeRef get_node(String caller_id)
        {
            if(nodes.ContainsKey(caller_id))
                return nodes[caller_id];
            else return null;
        }

        public void _register(Registrations r, String key, String caller_id, String caller_api, String service_api = null)
        {
            bool changed = false;
            NodeRef node_ref = _register_node_api(caller_id, caller_api, ref changed);
            node_ref.add(r.type,key);

            if (changed)
            {
                publishers.unregister_all(caller_id);
                subscribers.unregister_all(caller_id);
                services.unregister_all(caller_id);
                param_subscribers.unregister_all(caller_id);
            }
            r.register(key, caller_id, caller_api, service_api);
        }

        public ReturnStruct _unregister(Registrations r, String key, String caller_id, String caller_api, String service_api = null)
        {
            ReturnStruct ret;
            if (nodes.ContainsKey(caller_id))
            {
                NodeRef node_ref = nodes[caller_id];
                ret = r.unregister(key, caller_id, caller_api, service_api);
                if (ret.statusCode == 1)
                {
                    node_ref.remove(r.type, key);
                }
                if (node_ref.is_empty())
                {
                    nodes.Remove(caller_id);
                }
            }
            else
            {
                ret = new ReturnStruct(0, String.Format("[{0}] is not a registered node",caller_id), new XmlRpc_Wrapper.XmlRpcValue(1));
            }
            return ret; // new ReturnStruct(code, msg, new XmlRpc_Wrapper.XmlRpcValue(ret));
        }

        public void register_service(String service, String caller_id, String caller_api, String service_api)
        {
            _register(services, service, caller_id, caller_api, service_api);
        }

        public void register_publisher(String topic, String caller_id, String caller_api)
        {
            _register(publishers, topic, caller_id, caller_api);
        }

        public void register_subscriber(String topic, String caller_id, String caller_api)
        {
            _register(subscribers, topic, caller_id, caller_api);
        }

        public void register_param_subscriber(String param, String caller_id, String caller_api)
        {
            _register(param_subscribers, param, caller_id, caller_api);
        }

        public ReturnStruct unregister_service(String service, String caller_id, String service_api)
        {

            String caller_api = "";
            return _unregister(services, service, caller_id, caller_api, service_api);
            //return new ReturnStruct(ret, msg);

        }

        public ReturnStruct unregister_subscriber(String topic, String caller_id, String caller_api)
        {
            return _unregister(subscribers, topic, caller_id, caller_api);
        }

        public ReturnStruct unregister_publisher(String topic, String caller_id, String caller_api)
        {
            return _unregister(publishers, topic, caller_id, caller_api);
        }

        public ReturnStruct unregister_param_subscriber(String param, String caller_id, String caller_api)
        {
            return _unregister(param_subscribers, param, caller_id, caller_api);
        }

        public NodeRef _register_node_api(String caller_id, String caller_api, ref bool rtn)
        {
            NodeRef node_ref = null;
                if(nodes.ContainsKey(caller_id))
                 node_ref= nodes[caller_id];

            String bumped_api = "";
            if (node_ref != null)
            {
                if (node_ref.api == caller_api)
                {
                    rtn = false;
                    return node_ref;
                }
                else
                {
                    bumped_api = node_ref.api;
                    //thread_pool.queue_task(bumped_api, shutdown_node_task, (bumped_api, caller_id, "new node registered with same name"))
                }
            }
            node_ref = new NodeRef(caller_id, caller_api);
            nodes[caller_id] = node_ref;

            rtn = (bumped_api.Length > 0);
            return node_ref;
        }


    }

    /*
     * """ 
       Container for node registration information. Used in master's 
       self.nodes data structure.  This is effectively a reference 
       counter for the node registration information: when the 
       subscriptions and publications are empty the node registration can 
       be deleted. 
       """ */
    public class NodeRef
    {
        public String id;
        public String api;

        private List<String> param_subscriptions;
        private List<String> topic_subscriptions;
        private List<String> topic_publications;
        private List<String> services;

        public NodeRef()
        {

        }
        public NodeRef(String _id, String _api)
        {
            id = _id;
            api = _api;

            param_subscriptions = new List<string>();
            topic_publications = new List<string>();
            topic_subscriptions = new List<string>();
            services = new List<string>();
        }

        public void clear()
        {
            param_subscriptions = new List<string>();
            topic_publications = new List<string>();
            topic_subscriptions = new List<string>();
            services = new List<string>();

        }

        public bool is_empty()
        {
            return ( (param_subscriptions.Count + topic_subscriptions.Count + topic_publications.Count + services.Count) == 0);
        }

        public void add(int type_, String key)
        {
            if (type_ == Registrations.TOPIC_SUBSCRIPTIONS)
            {
                if ( !topic_subscriptions.Contains(key))
                {
                    topic_subscriptions.Add(key);
                }
            }
            else if (type_ == Registrations.TOPIC_PUBLICATIONS)
            {
                if (!topic_publications.Contains(key))
                {
                    topic_publications.Add(key);
                }
            }
            else if (type_ == Registrations.SERVICE)
            {
                if (!services.Contains(key))
                {
                    services.Add(key);
                }
            }
            else if (type_ == Registrations.PARAM_SUBSCRIPTIONS)
            {
                if (!param_subscriptions.Contains(key))
                {
                    param_subscriptions.Add(key);
                }
            }
            else
            {
                throw new Exception("FAILURE ADDING EXCEPTION! HALP!");
            }
        }

        public void remove(int type_, String key)
        {
            if (type_ == Registrations.TOPIC_SUBSCRIPTIONS)
            {
                if (topic_subscriptions.Contains(key))
                {
                    topic_subscriptions.Remove(key);
                }
            }
            else if (type_ == Registrations.TOPIC_PUBLICATIONS)
            {
                if (topic_publications.Contains(key))
                {
                    topic_publications.Remove(key);
                }
            }
            else if (type_ == Registrations.SERVICE)
            {
                if (services.Contains(key))
                {
                    services.Remove(key);
                }
            }
            else if (type_ == Registrations.PARAM_SUBSCRIPTIONS)
            {
                if (param_subscriptions.Contains(key))
                {
                    param_subscriptions.Remove(key);
                }
            }
            else
            {
                throw new Exception("FAILURE REMOVING EXCEPTION! HALP!");
            }
        }

        public void shutdown_node_task(String api, int caller_id, String reason)
        {
            //XmlRpcManager m = new XmlRpcManager();
            //m.shutdown();
        }


    }
}
