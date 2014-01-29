using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Messages.dynamic_reconfigure;
using Messages.std_msgs;
using Ros_CSharp;

namespace DynamicReconfigure
{
    public delegate void ConfigDelegate(Config newconfig);

    public delegate void DescriptionDelegate(ConfigDescription newdescription);

    public class DynamicReconfigureInterface
    {
        private string name;
        private int timeout = 0;
        public event ConfigDelegate ConfigEvent;
        public event DescriptionDelegate DescriptionEvent;
        private ServiceServer setServer;
        private Subscriber<Config> configSub;
        private Subscriber<ConfigDescription> descSub;
        private NodeHandle nh;

        public DynamicReconfigureInterface(NodeHandle n, string name, int timeout = 0, ConfigDelegate ccb = null, DescriptionDelegate dcb = null)
        {
            nh = n;
            this.name = name;
            this.timeout = timeout;
            if (ccb != null)
                ConfigEvent += ccb;
            if (dcb != null)
                DescriptionEvent += dcb;
        }

        private void ConfigCallback(Config m)
        {
            if (ConfigEvent != null) ConfigEvent(m);

        }

        private void DescriptionCallback(ConfigDescription m)
        {
            if (DescriptionEvent != null) DescriptionEvent(m);
        }

        public void SubscribeForUpdates()
        {
            if (configSub != null)
                configSub.shutdown();
            if (descSub != null)
                descSub.shutdown();
            string configtop = names.resolve(name, "parameter_updates");
            string paramtop = names.resolve(name, "parameter_descriptions");
            configSub = nh.subscribe<Config>(configtop, 1, ConfigCallback);
            descSub = nh.subscribe<ConfigDescription>(paramtop, 1, DescriptionCallback);
        }

        public void AdvertiseReconfigureService()
        {
            string sn = names.resolve(name, "set_parameters");
            if (timeout == 0)
            {
                try
                {
                    Service.waitForService(sn, 1);
                }
                catch
                {
                    Service.waitForService(sn, timeout);
                }
            }
            else
            {
                Service.waitForService(sn, timeout);
            }
            setServer = nh.advertiseService(sn, (Reconfigure.Request req, ref Reconfigure.Request res) => 
            {
                Console.WriteLine("HOLY FUCKSTICK");
                res.config = req.config;
                return true;
            });
        }

        public void Set(string key, string value)
        {
            Reconfigure.Request req = new Reconfigure.Request { config = new Config() { strs = new[] { new StrParameter { name = new Messages.std_msgs.String(key), value = new Messages.std_msgs.String(value) } } } };
            Reconfigure.Response resp = new Reconfigure.Response();
            if (nh.serviceClient<Reconfigure.Request, Reconfigure.Response>(names.resolve(name, "set_parameters")).call(req, ref resp))
                Console.WriteLine("SET SUCCESSFUL!");
            else
                Console.WriteLine("SET FAILED!");
        }

        public void Set(string key, int value)
        {
            Reconfigure.Request req = new Reconfigure.Request { config = new Config() { ints = new[] { new IntParameter { name = new Messages.std_msgs.String(key), value = value } } } };
            Reconfigure.Response resp = new Reconfigure.Response();
            if (nh.serviceClient<Reconfigure.Request, Reconfigure.Response>(names.resolve(name, "set_parameters")).call(req, ref resp))
                Console.WriteLine("SET SUCCESSFUL!");
            else
                Console.WriteLine("SET FAILED!");
        }

        public void Set(string key, double value)
        {
            Reconfigure.Request req = new Reconfigure.Request { config = new Config() { doubles = new[] { new DoubleParameter { name = new Messages.std_msgs.String(key), value = value } } } };
            Reconfigure.Response resp = new Reconfigure.Response();
            if (nh.serviceClient<Reconfigure.Request, Reconfigure.Response>(names.resolve(name, "set_parameters")).call(req, ref resp))
                Console.WriteLine("SET SUCCESSFUL!");
            else
                Console.WriteLine("SET FAILED!");
        }

        public void Set(string key, bool value)
        {
            Reconfigure.Request req = new Reconfigure.Request { config = new Config() { bools = new[] { new BoolParameter { name = new Messages.std_msgs.String(key), value = value } } } };
            Reconfigure.Response resp = new Reconfigure.Response();
            if (nh.serviceClient<Reconfigure.Request, Reconfigure.Response>(names.resolve(name, "set_parameters")).call(req, ref resp))
                Console.WriteLine("SET SUCCESSFUL!");
            else
                Console.WriteLine("SET FAILED!");
        }
    }
}
