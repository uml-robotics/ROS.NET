#region USINGZ

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using XmlRpc_Wrapper;

#endregion

namespace Ros_CSharp
{
    public static class master
    {
        public static int port;
        public static string host = "";
        public static string uri = "";
        public static TimeSpan retryTimeout = TimeSpan.FromSeconds(0);
        private static bool firstsucces;

        internal static void init(IDictionary remapping_args)
        {
            if (remapping_args.Contains("__master"))
            {
                uri = (string) remapping_args["__master"];
                ROS.ROS_MASTER_URI = uri;
            }
            if (uri == "")
                uri = ROS.ROS_MASTER_URI;
            if (!network.splitURI(uri, ref host, ref port))
            {
                port = 11311;
            }
        }

        public static bool check()
        {
            XmlRpcValue args = new XmlRpcValue(), result = new XmlRpcValue(), payload = new XmlRpcValue();
            args.Set(0, this_node.Name);
            return execute("getPid", args, ref result, ref payload, false);
        }

        public static bool getTopics(ref TopicInfo[] topics)
        {
            List<TopicInfo> topicss = new List<TopicInfo>();
            XmlRpcValue args = new XmlRpcValue(), result = new XmlRpcValue(), payload = new XmlRpcValue();
            args.Set(0, this_node.Name);
            args.Set(1, "");
            if (!execute("getPublishedTopics", args, ref result, ref payload, true))
                return false;
            topicss.Clear();
            for (int i = 0; i < payload.Size; i++)
                topicss.Add(new TopicInfo(payload[i][0].Get<string>(), payload[i][1].Get<string>()));
            topics = topicss.ToArray();
            return true;
        }

        public static bool getNodes(ref string[] nodes)
        {
            List<string> names = new List<string>();
            XmlRpcValue args = new XmlRpcValue(), result = new XmlRpcValue(), payload = new XmlRpcValue();
            args.Set(0, this_node.Name);

            if (!execute("getSystemState", args, ref result, ref payload, true))
            {
                return false;
            }
            for (int i = 0; i < payload.Size; i++)
            {
                for (int j = 0; j < payload[i].Size; j++)
                {
                    XmlRpcValue val = payload[i][j][1];
                    for (int k = 0; k < val.Size; k++)
                    {
                        string name = val[k].Get<string>();
                        names.Add(name);
                    }
                }
            }
            nodes = names.ToArray();
            return true;
        }


        public static bool execute(string method, XmlRpcValue request, ref XmlRpcValue response, ref XmlRpcValue payload,
                                   bool wait_for_master)
        {
            try
            {
                DateTime startTime = DateTime.Now;
                string master_host = host;
                int master_port = port;
                XmlRpcClient client = XmlRpcManager.Instance.getXMLRPCClient(master_host, master_port, "/");
                bool printed = false;
                bool slept = false;
                bool ok = true;
                while (!client.IsConnected && !ROS.shutting_down && !XmlRpcManager.Instance.shutting_down ||
                       !(ok = client.Execute(method, request, response) && XmlRpcManager.Instance.validateXmlrpcResponse(method, response, ref payload)))
                {
                    if (!printed)
                    {
                        EDB.WriteLine("[{0}] FAILED TO CONTACT MASTER AT [{1}:{2}]. {3}", method, master_host,
                                      master_port, (wait_for_master ? "Retrying..." : ""));
                        printed = true;
                    }

                    if (!wait_for_master)
                    {
                        XmlRpcManager.Instance.releaseXMLRPCClient(client);
                        return false;
                    }

                    if (retryTimeout.TotalSeconds > 0 && DateTime.Now.Subtract(startTime) > retryTimeout)
                    {
                        EDB.WriteLine("[{0}] Timed out trying to connect to the master after [{1}] seconds", method,
                                      retryTimeout.TotalSeconds);
                        XmlRpcManager.Instance.releaseXMLRPCClient(client);
                        return false;
                    }
                    slept = true;
                    Thread.Sleep(10);
                }
                if (ok && !firstsucces)
                {
                    firstsucces = true;
                    EDB.WriteLine(string.Format("CONNECTED TO MASTER AT [{0}:{1}]", master_host, master_port));
                }
                XmlRpcManager.Instance.releaseXMLRPCClient(client);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return false;
        }
    }
}