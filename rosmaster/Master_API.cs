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
            public void setParam(String caller_id, String key, XmlRpc_Wrapper.XmlRpcValue value) 
            {
                key = Names.resolve_name(key,caller_id);
                param_server.set_param(key, value, _notify_param_subscribers);
            }
            public void getParam() { }
            public void searchParam() { }
            public void subscribeParam() { }
            public void unsubscribeParam() { }
            public void hasParam() { }
            public void getParamNames() { }

            #endregion




            #region NOTIFICATION ROUTINES
            public void _notify() { }
            public int _notify_param_subscribers(Dictionary<String, Tuple<String, XmlRpc_Wrapper.XmlRpcValue>> updates) { return 1; }
            public void _param_update_task() { }
            public void _notify_topic_subscribers() { }
            public void _notify_service_update() { }

            #endregion


            #region SERVICE PROVIDER
            public void registerServvice() { }
            public void lookupService() { }
            public void unregisterService() { }

            #endregion



            #region PUBLISH/SUBSCRIBE

            public void registerSubscriber() { }
            public void unregisterSubscriber() { }
            public void registerPublisher() { }
            public void unregisterPublisher() { }

            #endregion


            #region GRAPH STATE API
            public void lookupNode() { }
            public void getPublishedTopics() { }
            public void getTopicTypes() { }
            public void getSystemState() { }

            #endregion
        }
    }
}
