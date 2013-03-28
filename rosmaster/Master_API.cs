using System;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace rosmaster
{
    public class Master_API
    {
        public readonly int STATUS = 0;
        public readonly int MSG = 1;
        public readonly int VAL = 2;

        public class ROSMasterHandler
        {
            #region Master Implementation

            private String uri;
            private bool done = false;

            RegistrationManager reg_manager;
            Registrations publishers;
            Registrations subscribers;
            Registrations services;
            Registrations param_subscribers;

            //public Dictionary<String, String> topic_types;

            Dictionary<String, String> topic_types; // {topicName : type}

            ParamDictionary param_server;


            public void mloginfo() { }
            public void mlogwarn() { }
            public void apivalidate() { }
            public void publisher_update_task(String topic, String pub_uri) 
            {
                //XmlRpcManager manager = new XmlRpcManager();
            }
            public void service_update_task() { }

            public ROSMasterHandler()
            {
                reg_manager = new RegistrationManager();

                 publishers = reg_manager.publishers;
                 subscribers =  reg_manager.subscribers;
                 services = reg_manager.services;
                 param_subscribers = reg_manager.param_subscribers;

                 topic_types = new Dictionary<String, String>();
                 param_server = new rosmaster.ParamDictionary(reg_manager);

            }

            public void _shutdown(String reason="")
            {
                //TODO:THREADING
                done = true;
            }
            public void _ready(String _uri)
            {
                uri = _uri;
            }
            public Boolean _ok()
            {
                return !done;
            }
            public void shutdown(String caller_id, String msg = "")
            {

            }
            public String getUri(String caller_id)
            {
                return uri;
            }
            public int getPid(String caller_id)
            {
                return Process.GetCurrentProcess().Id;
            }
            #endregion

            #region PARAMETER SERVER ROUTINES


            public int deleteParam(String caller_id, String key) 
            {
                try
                {
                    key = Names.resolve_name(key, caller_id);
                    param_server.delete_param(key, _notify_param_subscribers);
                    return 1;
                }
                catch (KeyNotFoundException e) { return -1; }
            }
            public void setParam(String caller_id, String key, Param value) 
            {
                key = Names.resolve_name(key,caller_id);
                param_server.set_param(key, value, _notify_param_subscribers);
            }

            /// <summary>
            /// Returns Param if it exists, null if it doesn't
            /// </summary>
            /// <param name="caller_id"></param>
            /// <param name="key"></param>
            /// <returns>Dictionary</Dictionary></returns>
            public Dictionary<String,Param> getParam(String caller_id, String key) 
            {
                try
                {
                    key = Names.resolve_name(key, caller_id);
                    return param_server.get_param(key);
                }
                catch (Exception e)
                {
                    return null;
                }
            }
            public String searchParam(String caller_id, String key) 
            {
                String search_key = param_server.search_param(caller_id,key);
                return search_key;
            }
            public Dictionary<String, Param> subscribeParam(String caller_id, String caller_api, String key) 
            {
                key = Names.resolve_name(key,caller_id);

                try
                {
                    return param_server.subscribe_param(key, caller_id, caller_api);
                }catch(Exception e)
                {
                    return null;
                }
            }

            public int unsubscribeParam(String caller_id, String caller_api, String key) 
            {
                key = Names.resolve_name(key, caller_id);
                return param_server.unsubscribe_param(key, caller_id, caller_api);
            }

            public bool hasParam(String caller_id, String key) 
            {
                key = Names.resolve_name(key, caller_id);
                return param_server.has_param(key);
            }

            public List<String> getParamNames(String caller_id) 
            {
                return param_server.get_param_names();
            }

            #endregion




            #region NOTIFICATION ROUTINES
            public void _notify() { }
            public int _notify_param_subscribers(Dictionary<String, Tuple<String, XmlRpc_Wrapper.XmlRpcValue>> updates) 
            { 
                return 1; 
            }

            public void _param_update_task(String caller_id, String caller_api, String param_key, XmlRpc_Wrapper.XmlRpcValue param_value) 
            { 

            }

            public void _notify_topic_subscribers(String topic, List<String> pub_uris) 
            { 
                //TODO: THIS
            }

            public void _notify_service_update(String service, String service_api) 
            {

            }

            #endregion


            #region SERVICE PROVIDER
            public void registerServvice() { }
            public void lookupService() { }
            public void unregisterService() { }

            #endregion



            #region PUBLISH/SUBSCRIBE

            public int registerSubscriber(String caller_id, String topic, String topic_type, String caller_api) 
            {
                reg_manager.register_subscriber(topic, caller_id, caller_api);

                if (!topic_types.ContainsValue(topic_type))
                    topic_types.Add(topic, topic_type);
                List<String> puburis = publishers.get_apis(topic);
                return 1;

            }
            public int unregisterSubscriber(String caller_id, String topic, String caller_api) 
            {
                String msg = "";
                int variable = 0;
                reg_manager.unregister_subscriber(topic, caller_id, caller_api, ref msg, ref variable);
                return 1;
            }
            public int registerPublisher(String caller_id, String topic, String topic_type, String caller_api) 
            {
                reg_manager.register_publisher(topic, caller_id, caller_api);
                if (!topic_types.ContainsValue(topic_type))
                    topic_types.Add(topic, topic_type);
                List<String> puburis = publishers.get_apis(topic);
                _notify_topic_subscribers(topic, puburis);
                List<String> sub_uris =  subscribers.get_apis(topic);

                return 1;
            }
            public int unregisterPublisher(String caller_id, String topic, String caller_api) 
            {
                String msg = "";
                int variable = 0;
                reg_manager.unregister_publisher(topic, caller_id, caller_api, ref msg, ref variable);
                return 1;
            }

            #endregion


            #region GRAPH STATE API
            public void lookupNode() { }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="caller_id"></param>
            /// <param name="subgraph"></param>
            /// <returns></returns>
            public List<String> getPublishedTopics(String caller_id, String subgraph) 
            {
                if (subgraph != "" && !subgraph.EndsWith("/"))
                    subgraph = subgraph + "/";
                Dictionary<String, List<String>> e = new Dictionary<String, List<String>>(publishers.map);
                List<String> rtn = new List<String>();
                foreach (var d in e)
                {
                    if (d.Key.StartsWith(subgraph))
                        foreach(String s in d.Value)
                            rtn.Add(s);
                }
                return rtn;
            }
            public Dictionary<String,String> getTopicTypes(String caller_id) 
            {
                return new Dictionary<String,String>(topic_types);
            }

            public List<String> getSystemState(String caller_id) 
            {
                List<String> rtn = new List<String>();
                rtn = publishers.get_state();
                rtn.AddRange(subscribers.get_state());
                rtn.AddRange(services.get_state());
                return rtn;
            }

            #endregion
        }
    }
}
