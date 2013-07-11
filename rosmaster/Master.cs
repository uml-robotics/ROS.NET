using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using XmlRpcClient = XmlRpc_Wrapper.XmlRpcClient;
using XmlRpcManager = Ros_CSharp.XmlRpcManager;
using XmlRpc_Wrapper;
using System.Runtime.InteropServices;

namespace rosmaster
{
    class Master
    {
        private String _ROS_MASTER_URI;
        private String _uri;
        private XmlRpcManager master_node;
        private Master_API.ROSMasterHandler handler;

        public Master(String ROS_MASTER_URI)
        {
            _ROS_MASTER_URI = ROS_MASTER_URI;
            //create handler?

//          Start the ROS Master. 

            //handler = new rosmaster.Master_API.ROSMasterHandler();
            //master_node = new XmlRpcManager();//roslib.xmlrpc.XmlRpcNode(self.port, handler) 
            //master_node.start();
    
        }

        public void start()
        {
            //creatre handler??
            Console.WriteLine("Master started.... ");
            
            handler = new Master_API.ROSMasterHandler();
            
            master_node = new XmlRpcManager();

            bindings();
            
            master_node.Start(_ROS_MASTER_URI);

            while (master_node.uri == "")
            {
                Thread.Sleep(10);
            }

            Console.WriteLine("Master connected! FUCK YEAH " + master_node.uri);
            _uri = master_node.uri;
        }

        public void stop()
        {
            if (master_node != null)
            {
                //master_node.shutdown();
                master_node = null;
            }
        }

        public bool ok()
        {
            if (master_node != null)
                return true;
            else 
                return false;
        }


        public void bindings()
        {
            master_node.bind("registerPublisher", tobind(new Func< String,String,String,String, XmlRpcValue>(registerPublisher)));
            master_node.bind("unregisterPublisher", tobind(new Func<String, String, String, XmlRpcValue>(unregisterPublisher)));
            master_node.bind("registerSubscriber", tobind(new Func<String, String, String, String, XmlRpcValue>(registerSubscriber)));
            master_node.bind("unregisterSubscriber", tobind(new Func<String, String, String, XmlRpcValue>(unregisterSubscriber)));
            master_node.bind("getPublications", getPublications);
            //master_node.bind("getSubscriptions", tobind(new Func<String, String, String, String, XmlRpcValue>(getSubscriptions)));
            master_node.bind("getPublishedTopics", getPublishedTopics);
            master_node.bind("publisherUpdate", pubUpdate);
            master_node.bind("requestTopic", requestTopic);
            master_node.bind("getTopicTypes", getTopicTypes);
            master_node.bind("getSystemState", getSystemState);

            master_node.bind("lookupService", tobind(new Func<String, String, XmlRpcValue>(lookupService)));
            master_node.bind("unregisterService", tobind(new Func<String, String, String, XmlRpcValue>(unregisterService)));
            master_node.bind("registerService", tobind(new Func<String, String, String, String, XmlRpcValue>(registerService)));


            master_node.bind("hasParam", tobind(new Func<String, String, XmlRpcValue>(hasParam)));
            master_node.bind("setParam", tobind(new Func<String, String,XmlRpcValue, XmlRpcValue>(setParam)));
            master_node.bind("getParam", tobind(new Func<String, String, XmlRpcValue>(getParam)));
           // master_node.bind("deleteParam", tobind(new Func<XmlRpcValue>(deleteParam)));
            master_node.bind("paramUpdate", paramUpdate);
            //master_node.bind("subscribeParam", tobind(new Func<String, String, String, String, XmlRpcValue>(subscribeParam)));
            master_node.bind("getParamNames", tobind(new Func<String, XmlRpcValue>(getParamNames)));

            master_node.bind("lookupNode", tobind(new Func<String, String, XmlRpcValue>(lookupNode)));
            //master_node.bind("getBusStats", tobind(new Func<String, String, String, String, XmlRpcValue>(getBusStats)));
            //master_node.bind("getBusInfo", tobind(new Func<String, String, String, String, XmlRpcValue>(getBusInfo)));

            //master_node.bind("Time", tobind(new Func<String, String, String, String, XmlRpcValue>(Time)));
            //master_node.bind("Duration", tobind(new Func<String, String, String, String, XmlRpcValue>(Duration)));
            //master_node.bind("get_rostime", tobind(new Func<String, String, String, String, XmlRpcValue>(get_rostime)));
            //master_node.bind("get_time", tobind(new Func<String, String, String, String, XmlRpcValue>(get_time)));
        }


        public XMLRPCFunc tobind<A>(Func<A, XmlRpcValue> act)
        {
            return (IntPtr parms, IntPtr result) =>
            {
                XmlRpcValue res = XmlRpcValue.Create(ref result), parm = XmlRpcValue.Create(ref parms);
                res = act(parm[0].Get<A>());
            };
        }

        public XMLRPCFunc tobind<A,B>(Func<A, B, XmlRpcValue> act)
        {
            return (IntPtr parms, IntPtr result) =>
            {
                XmlRpcValue res = XmlRpcValue.Create(ref result), parm = XmlRpcValue.Create(ref parms);
                res = act(parm[0].Get<A>(), parm[1].Get<B>());
            };
        }

        public XMLRPCFunc tobind<A,B,C>(Func<A, B, C, XmlRpcValue> act)
        {
            return (IntPtr parms, IntPtr result) =>
            {
                XmlRpcValue res = XmlRpcValue.Create(ref result), parm = XmlRpcValue.Create(ref parms);
                res = act(parm[0].Get<A>(), parm[1].Get<B>(), parm[2].Get<C>());
            };
        }

        public XMLRPCFunc tobind<A,B,C,D>(Func<A, B, C, D, XmlRpcValue> act)
        {
            return (IntPtr parms, IntPtr result) =>
            {
                XmlRpcValue res = XmlRpcValue.Create(ref result), parm = XmlRpcValue.Create(ref parms);
                res = act( parm[0].Get<A>(), parm[1].Get<B>(), parm[2].Get<C>(),parm[3].Get<D>());
            };
        }

        public Action<IntPtr> responseStr(IntPtr target, int code, string msg, string response)
        {
            return (p) =>
            {
                XmlRpcValue v = XmlRpcValue.LookUp(p);
                v.Set(0, code);
                v.Set(1, msg);
                v.Set(2, response);
            };
        }

        public Action<IntPtr> responseInt(int code, string msg, int response)
        {
            return (p) =>
            {
                XmlRpcValue v = XmlRpcValue.LookUp(p);
                v.Set(0, code);
                v.Set(1, msg);
                v.Set(2, response);
            };
        }

        public Action<IntPtr> responseBool(int code, string msg, bool response)
        {
            return (p) =>
            {
                XmlRpcValue v = XmlRpcValue.LookUp(p);
                v.Set(0, code);
                v.Set(1, msg);
                v.Set(2, response);
            };
        }


        #region services

        public XmlRpcValue lookupService(String caller_id, String service)
        {


            XmlRpcValue res = new XmlRpcValue(); //.Create(ref result), parm = XmlRpcValue.Create(ref parms);

            //String caller_id = parm[0].GetString();
            //String service = parm[1].GetString();

            ReturnStruct r = handler.lookupService(caller_id, service);

            res.Set(0, r.statusCode);
            res.Set(1, r.statusMessage);
            String s = r.value.GetString();
            res.Set(2, r.value);
            return res;
        }

        public XmlRpcValue registerService(String caller_id, String service, String service_api, String caller_api)
        {
            XmlRpcValue res = new XmlRpcValue();//XmlRpcValue.Create(ref result), parm = XmlRpcValue.Create(ref parms);

            //Node Name
            //Full service 
            //api->rpc
            //otherapi?
            /*String caller_id = parm[0].GetString();
            String service = parm[1].GetString();
            String service_api  = parm[2].GetString();
            String caller_api = parm[3].GetString();
            */
            //String topic = parm[0].GetString();
            ReturnStruct r = handler.registerService(caller_id, service, service_api, caller_api);

            res.Set(0, r.statusCode);
            res.Set(1, r.statusMessage);
            res.Set(2, r.value);
            return res;
        }

        public XmlRpcValue unregisterService(String caller_id, String service, String service_api)
        {
            XmlRpcValue res = new XmlRpcValue();//XmlRpcValue.Create(ref result), parm = XmlRpcValue.Create(ref parms);

           /* String caller_id = parm[0].GetString();
            String service = parm[1].GetString();
            String service_api = parm[2].GetString();
            */
            ReturnStruct r = handler.unregisterService(caller_id, service, service_api);

            res.Set(0, r.statusCode);
            res.Set(1, r.statusMessage);
            res.Set(2, r.value);
            return res;
        }
#endregion

        #region Topic Subscription/Publication

        public void getTopicTypes([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        {
            XmlRpcValue res = XmlRpcValue.Create(ref result), parm = XmlRpcValue.Create(ref parms);

            String topic = parm[0].GetString();

            String caller_id = parm[1].GetString();
            Dictionary<String, String> types = handler.getTopicTypes(topic);

            XmlRpcValue value = new XmlRpcValue();
            int index = 0;
            foreach (KeyValuePair<String, String> pair in types)
            {
                XmlRpcValue payload = new XmlRpcValue();
                payload.Set(0, pair.Key);
                payload.Set(1, pair.Value);
                value.Set(index, payload);
            }

            res.Set(0, 1);
            res.Set(1, "getTopicTypes");
            res.Set(2, value);
        }

        /// <summary>
        /// Returns list of all publications
        /// </summary>
        /// <param name="parms"></param>
        /// <param name="result"></param>
        public void getPublications([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        {
            XmlRpcValue res = XmlRpcValue.Create(ref result);
            res.Set(0, 1); //length
            res.Set(1, "publications"); //response too
            XmlRpcValue response = new XmlRpcValue(); //guts, new value here

            //response.Size = 0;
            List<List<String>> current = handler.getPublishedTopics("", "");

            for (int i = 0; i < current.Count; i += 2)
            {
                XmlRpcValue pub = new XmlRpcValue();
                pub.Set(0, current[0]);
                current.RemoveAt(0);
                pub.Set(1, current[0]);
                current.RemoveAt(0);
                response.Set(i, pub);
            }
            res.Set(2, response);
        }


        /// <summary>
        /// No clue.
        /// </summary>
        /// <param name="parms"></param>
        /// <param name="result"></param>
        public void requestTopic([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        {
            throw new Exception("NOT IMPLEMENTED YET!");
            XmlRpcValue res = XmlRpcValue.Create(ref result), parm = XmlRpcValue.Create(ref parms);
        }

        /// <summary>
        /// Notify subscribers of an update??
        /// </summary>
        /// <param name="parms"></param>
        /// <param name="result"></param>
        public void pubUpdate([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        {
            throw new Exception("NOT IMPLEMENTED YET!");
            //mlRpcValue parm = XmlRpcValue.Create(ref parms);
            //List<string> pubs = new List<string>();
            //for (int idx = 0; idx < parm[2].Size; idx++)
            //    pubs.Add(parm[2][idx].Get<string>());
            //if (pubUpdate(parm[1].Get<string>(), pubs))
            //    XmlRpcManager.Instance.responseInt(1, "", 0)(result);
            //else
            //{
            //    EDB.WriteLine("Unknown Error");
            //    XmlRpcManager.Instance.responseInt(0, "Unknown Error or something", 0)(result);
            //}



            //EDB.WriteLine("TopicManager is updating publishers for " + topic);
            //Subscription sub = null;
            //lock (subs_mutex)
            //{
            //    if (shutting_down) return false;
            //    foreach (Subscription s in subscriptions)
            //    {
            //        if (s.name != topic || s.IsDropped)
            //            continue;
            //        sub = s;
            //        break;
            //    }
            //}
            //if (sub != null)
            //    return sub.pubUpdate(pubs);
            //else
            //    EDB.WriteLine("got a request for updating publishers of topic " + topic +
            //                  ", but I don't have any subscribers to that topic.");
            //return false;
        }


        /// <summary>
        /// Returns list of all, publishers, subscribers, and services
        /// </summary>
        /// <param name="parms"></param>
        /// <param name="result"></param>
        public void getSystemState([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        {
            XmlRpcValue res = XmlRpcValue.Create(ref result), parm = XmlRpcValue.Create(ref parms);
            res.Set(0, 1);
            res.Set(1, "getSystemState");
            List<List<List<String>>> systemstatelist = handler.getSystemState("");//parm.GetString()

            XmlRpcValue listoftypes = new XmlRpcValue();

            XmlRpcValue listofvalues = new XmlRpcValue();

            int index = 0;

            foreach (List<List<String>> types in systemstatelist) //publisher, subscriber, services
            {
                int bullshitindex = 0;
                XmlRpcValue typelist;
                XmlRpcValue bullshit = new XmlRpcValue();
                if (types.Count > 0)
                {
                    foreach (List<String> l in types)
                    {
                        int typeindex = 0;
                        typelist = new XmlRpcValue();
                        //XmlRpcValue value = new XmlRpcValue();
                        typelist.Set(typeindex++, l[0]);
                        XmlRpcValue payload = new XmlRpcValue();
                        for (int i = 1; i < l.Count; i++)
                        {
                            payload.Set(i - 1, l[i]);
                        }

                        typelist.Set(typeindex++, payload);
                        //typelist.Set(typeindex++, value);
                        bullshit.Set(bullshitindex++, typelist);
                    }
                }
                else
                {
                    typelist = new XmlRpcValue();
                    bullshit.Set(bullshitindex++, typelist);
                }


                listoftypes.Set(index++, bullshit);
            }

            res.Set(2, listoftypes);
        }

        /// <summary>
        /// Get a list of all published topics
        /// </summary>
        /// <param name="parms"></param>
        /// <param name="result"></param>
        public void getPublishedTopics([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        {
            XmlRpcValue res = XmlRpcValue.Create(ref result), parm = XmlRpcValue.Create(ref parms);
            List<List<String>> publishedtopics = handler.getPublishedTopics("", "");
            res.Set(0, 1);
            res.Set(1, "current system state");

            XmlRpcValue listofvalues = new XmlRpcValue();
            int index = 0;
            foreach (List<String> l in publishedtopics)
            {
                XmlRpcValue value = new XmlRpcValue();
                value.Set(0, l[0]); //Topic Name
                value.Set(1, l[1]); // Topic type
                listofvalues.Set(index, value);
                index++;
            }
            res.Set(2, listofvalues);
        }


        /// <summary>
        /// Register a new publisher to a topic
        /// </summary>
        /// <param name="parms"></param>
        /// <param name="result"></param>
        public XmlRpcValue registerPublisher(String caller_id, String topic, String type, String caller_api)
        {
            //XmlRpcValue res = XmlRpcValue.Create(ref result), parm = XmlRpcValue.Create(ref parms);

            /*String caller_id = parm[0].GetString();
            String topic = parm[1].GetString();
            String type = parm[2].GetString();
            String caller_api = parm[3].GetString(); //hostname
                        */
            XmlRpcValue res = new XmlRpcValue();
            Console.WriteLine("PUBLISHING: " + caller_id + " : " + caller_api);

            handler.registerPublisher(caller_id, topic, type, caller_api);
            res.Set(0, 1);
            res.Set(1, "GOOD JOB!");
            res.Set(2, new XmlRpcValue(""));
            return res;
        }

        /// <summary>
        /// Unregister an existing publisher
        /// </summary>
        /// <param name="parms"></param>
        /// <param name="result"></param>
        public XmlRpcValue unregisterPublisher(String caller_id, String topic, String caller_api)
        {
            XmlRpcValue res = new XmlRpcValue();//XmlRpcValue.Create(ref result), parm = XmlRpcValue.Create(ref parms);

            /*String caller_id = parm[0].GetString();
            String topic = parm[1].GetString();
            String caller_api = parm[2].GetString();*/
            Console.WriteLine("UNPUBLISHING: " + caller_id + " : " + caller_api);

            int ret = handler.unregisterPublisher(caller_id, topic, caller_api);
            res.Set(0, ret);
            res.Set(1, "unregistered " + caller_id + "as provder of " + topic);
            return res;
        }

        /// <summary>
        /// Register a new subscriber
        /// </summary>
        /// <param name="parms"></param>
        /// <param name="result"></param>
        public XmlRpcValue registerSubscriber(String caller_id, String topic,String type, String caller_api)
        {
            XmlRpcValue res = new XmlRpcValue();//XmlRpcValue.Create(ref result), parm = XmlRpcValue.Create(ref parms);

            /*String caller_id = parm[0].GetString();
            String topic = parm[1].GetString();
            String type = parm[2].GetString();
            String caller_api = parm[3].GetString(); //hostname
        */
            handler.registerSubscriber(caller_id, topic, type, caller_api);
            res.Set(0, 1);
            res.Set(1, "GOOD JOB!");
            res.Set(2, new XmlRpcValue(""));
            return res;
        }

        /// <summary>
        /// Unregister an existing subscriber
        /// </summary>
        /// <param name="parms"></param>
        /// <param name="result"></param>
        public XmlRpcValue unregisterSubscriber(String caller_id, String topic, String caller_api)
        {
            XmlRpcValue res = new XmlRpcValue();//XmlRpcValue.Create(ref result), parm = XmlRpcValue.Create(ref parms);

            /*String caller_id = parm[0].GetString();
            String topic = parm[1].GetString();
            String caller_api = parm[2].GetString(); */

            Console.WriteLine("UNSUBSCRIBING: " + caller_id + " : " + caller_api);

            int ret = handler.unregisterSubscriber(caller_id, topic, caller_api);
            res.Set(0, ret);
            res.Set(1, "unregistered " + caller_id + "as provder of " + topic);
            return res;
        }


        #endregion

        #region Misc

        public XmlRpcValue lookupNode(String topic, String caller_id)
        {
            XmlRpcValue res = new XmlRpcValue();//XmlRpcValue.Create(ref result), parm = XmlRpcValue.Create(ref parms);

            //String topic = parm[0].GetString();
            //String caller_id = parm[1].GetString();
            String api = handler.lookupNode(caller_id, topic);

           // if(api == "")
         //       res.Set(0, 0);
           // else
            res.Set(0, 1);
            res.Set(1, "lookupNode");
            res.Set(2, api);
            return res;
        }



        /// <summary>
        /// Get BUS status??? WUT
        /// </summary>
        /// <param name="parms"></param>
        /// <param name="result"></param>
        public XmlRpcValue getBusStatus([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        {
            throw new Exception("NOT IMPLEMENTED YET!");
        }

        /// <summary>
        /// Get BUS info??? WUT
        /// </summary>
        /// <param name="parms"></param>
        /// <param name="result"></param>
        public XmlRpcValue getBusInfo([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        {
            throw new Exception("NOT IMPLEMENTED YET!");
        }

        #endregion

        #region Time
        public XmlRpcValue getTime([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        {
            throw new Exception("NOT IMPLEMENTED YET!");
        }
        /// <summary>
        /// Get a list of all subscriptions
        /// </summary>
        /// <param name="parms"></param>
        /// <param name="result"></param>
        public XmlRpcValue getSubscriptions([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        {
            throw new Exception("NOT IMPLEMENTED YET!");
        }
        #endregion



        #region Parameter

        /// <summary>
        /// Check whether a parameter exists
        /// </summary>
        /// <param name="parms"></param>
        /// <param name="result"></param>
        public XmlRpcValue hasParam(String caller_id, String topic)
        {
            XmlRpcValue res = new XmlRpcValue();//XmlRpcValue.Create(ref result), parm = XmlRpcValue.Create(ref parms);
            res.Set(0, 1);
            res.Set(1, "hasParam");

            //String caller_id = parm[0].GetString();
            //String topic = parm[1].GetString();

            res.Set(2, handler.hasParam(caller_id, topic));
            return res;
        }

        /// <summary>
        /// Set a new parameter
        /// </summary>
        /// <param name="parms"></param>
        /// <param name="result"></param>
        public XmlRpcValue setParam(String caller_api, String topic, XmlRpcValue value)
        {
            XmlRpcValue res = new XmlRpcValue();//XmlRpcValue.Create(ref result), parm = XmlRpcValue.Create(ref parms);
            res.Set(0, 1);
            res.Set(1,"setParam");

            //String caller_api = parm[0].GetString();
            //String topic = parm[1].GetString();
            //XmlRpcValue value = parm[2];
            handler.setParam(caller_api, topic,value);
            res.Set(2, "parameter " + topic + " set");
            return res;
        }



        /// <summary>
        /// Retrieve a value for an existing parameter, if it exists
        /// </summary>
        /// <param name="parms"></param>
        /// <param name="result"></param>
        public XmlRpcValue getParam(String caller_id, String topic)
        {
            XmlRpcValue res = new XmlRpcValue();//XmlRpcValue.Create(ref result), parm = XmlRpcValue.Create(ref parms);
            res.Set(0, 1);
            res.Set(1, "getParam");

            //String caller_id = parm[0].GetString();
            //String topic = parm[1].GetString();

            // value = new XmlRpcValue();
             XmlRpcValue value = handler.getParam(caller_id, topic);
            //value
             // String vi = v.getString();
             if (value == null)
             {
                 res.Set(0, 0);
                 res.Set(1, "Parameter "+ topic+" is not set");
                 value = new XmlRpcValue("");
             }
            res.Set(2,value);
            return res;
        }

        /// <summary>
        /// Delete a parameter, if it exists
        /// </summary>
        /// <param name="parms"></param>
        /// <param name="result"></param>
        public XmlRpcValue deleteParam()
        {
            throw new Exception("NOT IMPLEMENTED YET!");
            //XmlRpcValue parm = new XmlRpcValue(), result = new XmlRpcValue(), payload = new XmlRpcValue();
            //parm.Set(0, this_node.Name);
            //parm.Set(1, mapped_key);
            //if (!master.execute("deleteParam", parm, ref result, ref payload, false))
            //    return false;
            //return true;
        }

        public XmlRpcValue getParamNames(String caller_id)
        {
            XmlRpcValue res = new XmlRpcValue();//XmlRpcValue.Create(ref result), parm = XmlRpcValue.Create(ref parms);
            res.Set(0, 1);
            res.Set(1, "getParamNames");

            //String caller_id = parm[0].GetString();
            List<String> list = handler.getParamNames(caller_id);

            XmlRpcValue response = new XmlRpcValue();
            int index = 0;
            foreach (String s in list)
            {
                response.Set(index++, s);
            }

            res.Set(2, response);
            return res;
        }



        /// <summary>
        /// Notify of new parameter updates
        /// </summary>
        /// <param name="parm"></param>
        /// <param name="result"></param>
        public static void paramUpdate(IntPtr parm, IntPtr result)
        {
            XmlRpcValue val = XmlRpcValue.LookUp(parm);
            val.Set(0, 1);
            val.Set(1, "");
            val.Set(2, 0);
            //update(XmlRpcValue.LookUp(parm)[1].Get<string>(), XmlRpcValue.LookUp(parm)[2]);
        }

        /// <summary>
        /// Subscribe to a param value
        /// </summary>
        /// <param name="parms"></param>
        /// <param name="result"></param>
        public XmlRpcValue Param([In] [Out] IntPtr parms, [In] [Out] IntPtr result)
        {
            throw new Exception("NOT IMPLEMENTED YET!");
            //return new XmlRpcValue;
        }

        #endregion


    }
}
