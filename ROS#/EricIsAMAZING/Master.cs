#region USINGZ

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using XmlRpc_Wrapper;

#endregion

namespace EricIsAMAZING
{
    public static class master
    {
        public static int port;
        public static string host = "";
        public static string uri = "";
        public static TimeSpan retryTimeout = TimeSpan.FromSeconds(0);

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
                throw new Exception("FAILED TO SPLIT THE URI!");
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
            DateTime startTime = DateTime.Now;
            string master_host = host;
            int master_port = port;
            XmlRpcClient client = XmlRpcManager.Instance.getXMLRPCClient(master_host, master_port, "/");
            bool printed = false;
            bool slept = false;
            bool ok = true;
            do
            {
                bool b = client.Execute(method, request, response);

                ok = !ROS.shutting_down && !XmlRpcManager.Instance.shutting_down;

                if (!b && ok)
                {
                    if (!printed && wait_for_master)
                    {
                        Console.WriteLine("[{0}] FAILED TO CONTACT MASTER AT [{1}:{2}]. {3}", method, master_host,
                                          master_port, (wait_for_master ? "Retrying..." : ""));
                        printed = true;
                    }

                    if (!wait_for_master)
                    {
                        XmlRpcManager.Instance.releaseXMLRPCClient(client);
                        return false;
                    }

                    if (DateTime.Now.Subtract(startTime) > retryTimeout)
                    {
                        Console.WriteLine("[{0}] Timed out trying to connect to the master after [{1}] seconds", method,
                                          retryTimeout.TotalSeconds);
                        XmlRpcManager.Instance.releaseXMLRPCClient(client);
                        return false;
                    }
                    slept = true;
                    Thread.Sleep(50);
                }
                else
                {
                    if (!XmlRpcManager.Instance.validateXmlrpcResponse(method, response, ref payload))
                    {
                        Console.WriteLine("Received a response? I think?... but it FAILED VALIDATION!");
                        XmlRpcManager.Instance.releaseXMLRPCClient(client);
                        return false;
                    }
                    break;
                }
            } while (ok);

            if (ok && slept)
            {
                Console.WriteLine(string.Format("CONNECTED TO MASTER AT [{0}:{1}]", master_host, master_port));
            }
            XmlRpcManager.Instance.releaseXMLRPCClient(client);
            return true;
        }
    }
}