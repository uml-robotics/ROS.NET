#region USINGZ

using System;
using System.Collections.Generic;
using System.Threading;
using Messages.dynamic_reconfigure;
using Messages.std_msgs;
using Ros_CSharp;
using String = Messages.std_msgs.String;

#endregion

namespace DynamicReconfigure
{
    public class DynamicReconfigureInterface : IDisposable
    {
        private Dictionary<string, List<Action<bool>>> boolcbs = new Dictionary<string, List<Action<bool>>>();
        private Subscriber<Config> configSub;
        private Subscriber<ConfigDescription> descSub;
        private Action<ConfigDescription> descriptionCallback;

        private Dictionary<string, List<Action<double>>> doublecbs = new Dictionary<string, List<Action<double>>>();
        private Dictionary<string, List<Action<int>>> intcbs = new Dictionary<string, List<Action<int>>>();
        private Dictionary<string, bool> lastbool = new Dictionary<string, bool>();
        private Dictionary<string, double> lastdouble = new Dictionary<string, double>();
        private Dictionary<string, int> lastint = new Dictionary<string, int>();
        private Dictionary<string, string> laststring = new Dictionary<string, string>();
        private ConfigDescription latestDescription;
        private string name;

        public string Namespace
        {
            get { return name; }
        }

        private string set_service_name;
        private NodeHandle nh;
        private object padlock = new object();
        private ServiceServer setServer; //TODO: implement configuration parsing, giblet generation, and implement a dynamic_reconfigure server interface
        private Dictionary<string, List<Action<string>>> strcbs = new Dictionary<string, List<Action<string>>>();
        private int timeout;

        public DynamicReconfigureInterface(NodeHandle n, string name, int timeout = 0)
        {
            nh = n;
            this.name = name;
            this.set_service_name = names.resolve(name, "set_parameters");
            this.timeout = timeout;
        }

        public void Subscribe(string paramname, Action<int> act)
        {
            int? last = null;
            lock (this)
            {
                if (!intcbs.ContainsKey(paramname))
                    intcbs[paramname] = new List<Action<int>>();
                if (lastint.ContainsKey(paramname))
                    last = lastint[paramname];
                intcbs[paramname].Add(act);
            }
            if (last != null)
                act((int)last);
        }

        public void Subscribe(string paramname, Action<bool> act)
        {
            bool? last = null;
            lock (this)
            {
                if (!boolcbs.ContainsKey(paramname))
                    boolcbs[paramname] = new List<Action<bool>>();
                if (lastbool.ContainsKey(paramname))
                    last = lastbool[paramname];
                boolcbs[paramname].Add(act);
            }
            if (last != null)
                act((bool)last);
        }

        public void Subscribe(string paramname, Action<string> act)
        {
            string last = null;
            lock (this)
            {
                if (!strcbs.ContainsKey(paramname))
                    strcbs[paramname] = new List<Action<string>>();
                if (laststring.ContainsKey(paramname))
                    last = laststring[paramname];
                strcbs[paramname].Add(act);
            }
            if (last != null)
                act(last);
        }

        public void Subscribe(string paramname, Action<double> act)
        {
            double? last = null;
            lock (this)
            {
                if (!doublecbs.ContainsKey(paramname))
                    doublecbs[paramname] = new List<Action<double>>();
                if (lastdouble.ContainsKey(paramname))
                    last = lastdouble[paramname];
                doublecbs[paramname].Add(act);
            }
            if (last != null)
                act((double)last);
        }

        private void ConfigCallback(Config m)
        {
            Dictionary<List<Action<bool>>, bool> lbcbs = new Dictionary<List<Action<bool>>,bool>();
            Dictionary<List<Action<int>>, int> ibcbs = new Dictionary<List<Action<int>>,int>();
            Dictionary<List<Action<double>>, double> dbcbs = new Dictionary<List<Action<double>>,double>();
            Dictionary<List<Action<string>>, string> sbcbs = new Dictionary<List<Action<string>>,string>();
            lock (this)
            {
                foreach (BoolParameter bp in m.bools)
                {
                    lastbool[bp.name] = bp.value;
                    if (boolcbs.ContainsKey(bp.name))
                    {
                        lbcbs.Add(boolcbs[bp.name], bp.value);
                        //boolcbs[bp.name.data].ForEach(a => a(bp.value));
                    }
                }
                foreach (IntParameter ip in m.ints)
                {
                    lastint[ip.name] = ip.value;
                    if (intcbs.ContainsKey(ip.name))
                    {
                        ibcbs.Add(intcbs[ip.name], ip.value);
                        //intcbs[ip.name.data].ForEach(a => a(ip.value));
                    }
                }
                foreach (DoubleParameter dp in m.doubles)
                {
                    lastdouble[dp.name] = dp.value;
                    if (doublecbs.ContainsKey(dp.name))
                    {
                        dbcbs.Add(doublecbs[dp.name], dp.value);
                        //doublecbs[dp.name.data].ForEach(a => a(dp.value));
                    }
                }
                foreach (StrParameter sp in m.strs)
                {
                    laststring[sp.name] = sp.value;
                    if (strcbs.ContainsKey(sp.name))
                    {
                        sbcbs.Add(strcbs[sp.name], sp.value);
                        //strcbs[sp.name.data].ForEach(a => a(sp.value.data));
                    }
                }
            }
            foreach (KeyValuePair<List<Action<bool>>, bool> kvp in lbcbs)
                kvp.Key.ForEach((b) => b(kvp.Value));
            foreach (KeyValuePair<List<Action<int>>, int> kvp in ibcbs)
                kvp.Key.ForEach((b) => b(kvp.Value));
            foreach (KeyValuePair<List<Action<double>>, double> kvp in dbcbs)
                kvp.Key.ForEach((b) => b(kvp.Value));
            foreach (KeyValuePair<List<Action<string>>, string> kvp in sbcbs)
                kvp.Key.ForEach((b) => b(kvp.Value));
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
                EDB.WriteLine(ice);
            }
        }

        public void AdvertiseReconfigureService()
        {
            if (timeout == 0)
            {
                try
                {
                    Service.waitForService(set_service_name, 1);
                }
                catch
                {
                    Service.waitForService(set_service_name, timeout);
                }
            }
            else
            {
                Service.waitForService(set_service_name, timeout);
            }
            setServer = nh.advertiseService(set_service_name, (Reconfigure.Request req, ref Reconfigure.Request res) =>
            {
                res.config = req.config;
                return true;
            });
        }

        private bool wait()
        {
            return Service.waitForService(set_service_name, TimeSpan.FromSeconds(1.0));
        }

        private ServiceClient<Reconfigure.Request, Reconfigure.Response> _cli;
        private ServiceClient<Reconfigure.Request, Reconfigure.Response> cli {
            get {
                lock (this)
                {
                    if (_cli == null)
                    {
                        if (!ROS.ok || ROS.shutting_down || !wait())
                        {
                            return null;
                        }
                        _cli = nh.serviceClient<Reconfigure.Request, Reconfigure.Response>(set_service_name, true);
                    }
                }
                return _cli;
            }
        }

        private bool set(Config conf, ref string deets)
        {
            Reconfigure.Request req = new Reconfigure.Request {config = conf};
            Reconfigure.Response resp = new Reconfigure.Response();
            ServiceClient<Reconfigure.Request, Reconfigure.Response> localcli;
            lock (this)
            {
                if (cli == null)
                    return false;
                localcli = cli;
            }
            bool result = localcli.call(req, ref resp);
            if (!result && deets != null)
                deets = "call failed!";
            localcli = null;
            return result;
        }

        private void _set(Config config)
        {
            string reason = "";
            if (!set(config, ref reason))
            {
                lock (this)
                    if (_cli != null)
                    {
                        _cli.shutdown();
                        _cli = null;
                    }
                if (!set(config, ref reason))
                    EDB.WriteLine("SET FAILED\n" + reason);
            }
        }

        private void Set(Config config, bool synchronous = false)
        {
            if (synchronous)
                _set(config);
            else
                new Action<Config>(_set).BeginInvoke(config,(iar) => {}, null);
        }

        public void Set(string key, string value, bool synchronous = false)
        {
            Set(new Config {strs = new[] {new StrParameter {name = key, value = value}}}, synchronous);
        }

        public void Set(string key, int value, bool synchronous = false)
        {
            Set(new Config {ints = new[] {new IntParameter {name = key, value = value}}}, synchronous);
        }

        public void Set(string key, double value, bool synchronous = false)
        {
            Set(new Config {doubles = new[] {new DoubleParameter {name = key, value = value}}}, synchronous);
        }

        public void Set(string key, bool value, bool synchronous = false)
        {
            Set(new Config {bools = new[] {new BoolParameter {name = key, value = value}}}, synchronous);
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (_cli != null)
            {
                _cli.shutdown();
                _cli = null;
            }
            if (setServer != null)
            {
                setServer.shutdown();
                setServer = null;
            }
        }

        #endregion
    }
}