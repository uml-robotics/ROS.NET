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
    public class DynamicReconfigureInterface
    {
        private string name;
        private int timeout = 0;
        private ServiceServer setServer;
        private Subscriber<Config> configSub;
        private Subscriber<ConfigDescription> descSub;
        private NodeHandle nh;
        private ConfigDescription latestDescription;
        private Action<ConfigDescription> descriptionCallback;

        public DynamicReconfigureInterface(NodeHandle n, string name, int timeout = 0)
        {
            nh = n;
            this.name = name;
            this.timeout = timeout;
        }

        private Dictionary<string, List<Action<int>>> intcbs = new Dictionary<string, List<Action<int>>>();
        private Dictionary<string, List<Action<bool>>> boolcbs = new Dictionary<string, List<Action<bool>>>();
        private Dictionary<string, List<Action<string>>> strcbs = new Dictionary<string, List<Action<string>>>();
        private Dictionary<string, List<Action<double>>> doublecbs = new Dictionary<string, List<Action<double>>>();
        private Dictionary<string, int> lastint = new Dictionary<string, int>();
        private Dictionary<string, bool> lastbool = new Dictionary<string, bool>();
        private Dictionary<string, string> laststring = new Dictionary<string, string>();
        private Dictionary<string, double> lastdouble = new Dictionary<string, double>();
        public void Subscribe(string paramname, Action<int> act)
        {
            lock (this)
            {
                if (!intcbs.ContainsKey(paramname))
                    intcbs[paramname] = new List<Action<int>>();
                if (lastint.ContainsKey(paramname))
                    act(lastint[paramname]);
                intcbs[paramname].Add(act);
            }
        }
        public void Subscribe(string paramname, Action<bool> act)
        {
            lock (this)
            {
                if (!boolcbs.ContainsKey(paramname))
                    boolcbs[paramname] = new List<Action<bool>>();
                if (lastbool.ContainsKey(paramname))
                    act(lastbool[paramname]);
                boolcbs[paramname].Add(act);
            }
        }
        public void Subscribe(string paramname, Action<string> act)
        {
            lock (this)
            {
                if (!strcbs.ContainsKey(paramname))
                    strcbs[paramname] = new List<Action<string>>();
                if (laststring.ContainsKey(paramname))
                    act(laststring[paramname]);
                strcbs[paramname].Add(act);
            }
        }
        public void Subscribe(string paramname, Action<double> act)
        {
            lock (this)
            {
                if (!doublecbs.ContainsKey(paramname))
                    doublecbs[paramname] = new List<Action<double>>();
                if (lastdouble.ContainsKey(paramname))
                    act(lastdouble[paramname]);
                doublecbs[paramname].Add(act);
            }
        }

        private void ConfigCallback(Config m)
        {
            lock (this)
            {
                foreach (BoolParameter bp in m.bools)
                {
                    lastbool[bp.name.data] = bp.value;
                    if (boolcbs.ContainsKey(bp.name.data))
                    {
                        boolcbs[bp.name.data].ForEach((a) => a(bp.value));
                    }
                }
                foreach (IntParameter ip in m.ints)
                {
                    lastint[ip.name.data] = ip.value;
                    if (intcbs.ContainsKey(ip.name.data))
                    {
                        intcbs[ip.name.data].ForEach((a) => a(ip.value));
                    }
                }
                foreach (DoubleParameter dp in m.doubles)
                {
                    lastdouble[dp.name.data] = dp.value;
                    if (doublecbs.ContainsKey(dp.name.data))
                    {
                        doublecbs[dp.name.data].ForEach((a) => a(dp.value));
                    }
                }
                foreach (StrParameter sp in m.strs)
                {
                    laststring[sp.name.data] = sp.value.data;
                    if (strcbs.ContainsKey(sp.name.data))
                    {
                        strcbs[sp.name.data].ForEach((a) => a(sp.value.data));
                    }
                }
            }
        }

        public void DescribeParameters(Action<ConfigDescription> pda)
        {
            descriptionCallback = pda;
            if (latestDescription != null)
                pda(latestDescription);
        }

        private void DescriptionCallback(ConfigDescription m)
        {
            latestDescription = m;
            if (descriptionCallback != null)
                descriptionCallback(latestDescription);
        }

        public void SubscribeForUpdates()
        {
            if (configSub != null)
                configSub.shutdown();
            if (descSub != null)
                descSub.shutdown();
            string configtop = names.resolve(name, "parameter_updates");
            string paramtop = names.resolve(name, "parameter_descriptions");
            try
            {
                configSub = nh.subscribe<Config>(configtop, 1, ConfigCallback);
                descSub = nh.subscribe<ConfigDescription>(paramtop, 1, DescriptionCallback);
            }
            catch (InvalidCastException ice)
            {
                Console.WriteLine(ice);
            }
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
                res.config = req.config;
                return true;
            });
        }

        private object padlock = new object();

        public void Set(string key, string value)
        {
            new Action(() =>
            {
                lock (padlock)
                {
                    Reconfigure.Request req = new Reconfigure.Request {config = new Config() {strs = new[] {new StrParameter {name = new Messages.std_msgs.String(key), value = new Messages.std_msgs.String(value)}}}};
                    Reconfigure.Response resp = new Reconfigure.Response();
                    if (!nh.serviceClient<Reconfigure.Request, Reconfigure.Response>(names.resolve(name, "set_parameters")).call(req, ref resp))
                        Console.WriteLine("SET FAILED!");
                }
            }).BeginInvoke((iar) => { }, null);
        }

        public void Set(string key, int value)
        {
            new Action(() =>
            {
                lock (padlock)
                {
                    Reconfigure.Request req = new Reconfigure.Request {config = new Config() {ints = new[] {new IntParameter {name = new Messages.std_msgs.String(key), value = value}}}};
                    Reconfigure.Response resp = new Reconfigure.Response();
                    if (!nh.serviceClient<Reconfigure.Request, Reconfigure.Response>(names.resolve(name, "set_parameters")).call(req, ref resp))
                        Console.WriteLine("SET FAILED!");
                }
            }).BeginInvoke((iar) => { }, null);
        }

        public void Set(string key, double value)
        {
            new Action(() =>
            {
                lock (padlock)
                {
                    Reconfigure.Request req = new Reconfigure.Request {config = new Config() {doubles = new[] {new DoubleParameter {name = new Messages.std_msgs.String(key), value = value}}}};
                    Reconfigure.Response resp = new Reconfigure.Response();
                    if (!nh.serviceClient<Reconfigure.Request, Reconfigure.Response>(names.resolve(name, "set_parameters")).call(req, ref resp))
                        Console.WriteLine("SET FAILED!");
                }
            }).BeginInvoke((iar) => { }, null);
        }

        public void Set(string key, bool value)
        {
            new Action(() =>
            {
                lock (padlock)
                {
                    Reconfigure.Request req = new Reconfigure.Request {config = new Config() {bools = new[] {new BoolParameter {name = new Messages.std_msgs.String(key), value = value}}}};
                    Reconfigure.Response resp = new Reconfigure.Response();
                    if (!nh.serviceClient<Reconfigure.Request, Reconfigure.Response>(names.resolve(name, "set_parameters")).call(req, ref resp))
                        Console.WriteLine("SET FAILED!");
                }
            }).BeginInvoke((iar) => { }, null);
        }
    }
}
