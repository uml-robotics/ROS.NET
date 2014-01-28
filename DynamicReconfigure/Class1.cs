using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Messages.dynamic_reconfigure;
using Ros_CSharp;

namespace DynamicReconfigure
{
    public delegate void ConfigCallback(Config newconfig);

    public delegate void DescriptionCallback(ConfigDescription newdescription);

    public class DynamicReconfigureInterface
    {
        public event ConfigCallback ConfigEvent;
        public event DescriptionCallback DescriptionEvent;
        private ServiceServer setServer;
        private Subscriber<Config> configSub;
        private Subscriber<ConfigDescription> descSub;
        private NodeHandle nh;

        public DynamicReconfigureInterface(string name, int timeout = 0, ConfigCallback ccb = null, DescriptionCallback dcb = null)
        {
            if (ccb != null)
                ConfigEvent += ccb;
            if (dcb != null)
                DescriptionEvent += dcb;

            nh = new NodeHandle(name);

            configSub = nh.subscribe<Config>(names.resolve(name, "parameter_updates"), 1, (m) => { if (ConfigEvent != null) ConfigEvent(m); });
            descSub = nh.subscribe<ConfigDescription>(names.resolve(name, "parameter_descriptionss"), 1, (m) => { if (DescriptionEvent != null) DescriptionEvent(m); });
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
          /*  setServer = nh.advertiseService<Config, Config>("set_parameters", (Config req, ref Config res) =>
            {
                Console.WriteLine("HOLY FUCKSTICK");
                res = req;
                return true; 
            });*/
        }
    }
}
