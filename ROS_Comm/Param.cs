// File: Param.cs
// Project: ROS_C-Sharp
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 11/06/2013
// Updated: 07/23/2014

#region USINGZ

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using XmlRpc;

#endregion

namespace Ros_CSharp
{
    public delegate void ParamDelegate(string key, XmlRpcValue value);

    public delegate void ParamStringDelegate(string key, string value);

    public delegate void ParamDoubleDelegate(string key, double value);

    public delegate void ParamIntDelegate(string key, int value);

    public delegate void ParamBoolDelegate(string key, bool value);

    [DebuggerStepThrough]
    public static class Param
    {
        public static Dictionary<string, XmlRpcValue> parms = new Dictionary<string, XmlRpcValue>();
        public static object parms_mutex = new object();
        public static List<string> subscribed_params = new List<string>();
        private static Dictionary<string, List<ParamStringDelegate>> StringCallbacks = new Dictionary<string, List<ParamStringDelegate>>();
        private static Dictionary<string, List<ParamIntDelegate>> IntCallbacks = new Dictionary<string, List<ParamIntDelegate>>();
        private static Dictionary<string, List<ParamDoubleDelegate>> DoubleCallbacks = new Dictionary<string, List<ParamDoubleDelegate>>();
        private static Dictionary<string, List<ParamBoolDelegate>> BoolCallbacks = new Dictionary<string, List<ParamBoolDelegate>>();
        private static Dictionary<string, List<ParamDelegate>> Callbacks = new Dictionary<string, List<ParamDelegate>>();

        public static void Subscribe(string key, ParamBoolDelegate del)
        {
            if (!BoolCallbacks.ContainsKey(key))
                BoolCallbacks.Add(key, new List<ParamBoolDelegate>());
            BoolCallbacks[key].Add(del);
            update(key, getParam(key, true));
        }

        public static void Subscribe(string key, ParamIntDelegate del)
        {
            if (!IntCallbacks.ContainsKey(key))
                IntCallbacks.Add(key, new List<ParamIntDelegate>());
            IntCallbacks[key].Add(del);
            update(key, getParam(key, true));
        }

        public static void Subscribe(string key, ParamDoubleDelegate del)
        {
            if (!DoubleCallbacks.ContainsKey(key))
                DoubleCallbacks.Add(key, new List<ParamDoubleDelegate>());
            DoubleCallbacks[key].Add(del);
            update(key, getParam(key, true));
        }

        public static void Subscribe(string key, ParamStringDelegate del)
        {
            if (!StringCallbacks.ContainsKey(key))
                StringCallbacks.Add(key, new List<ParamStringDelegate>());
            StringCallbacks[key].Add(del);
            update(key, getParam(key, true));
        }

        public static void Subscribe(string key, ParamDelegate del)
        {
            if (!Callbacks.ContainsKey(key))
                Callbacks.Add(key, new List<ParamDelegate>());
            Callbacks[key].Add(del);
            update(key, getParam(key, true));
        }

        /// <summary>
        ///     Sets the paramater on the parameter server
        /// </summary>
        /// <param name="key">Name of the parameter</param>
        /// <param name="val">Value of the paramter</param>
        public static void set(string key, XmlRpcValue val)
        {
            string mapped_key = names.resolve(key);
            XmlRpcValue parm = new XmlRpcValue(), response = new XmlRpcValue(), payload = new XmlRpcValue();
            parm.Set(0, this_node.Name);
            parm.Set(1, mapped_key);
            parm.Set(2, val);
            lock (parms_mutex)
            {
                if (master.execute("setParam", parm, ref response, ref payload, true))
                {
                    if (subscribed_params.Contains(mapped_key))
                        parms.Add(mapped_key, val);
                }
            }
        }

        /// <summary>
        ///     Sets the paramater on the parameter server
        /// </summary>
        /// <param name="key">Name of the parameter</param>
        /// <param name="val">Value of the paramter</param>
        public static void set(string key, string val)
        {
            string mapped_key = names.resolve(key);
            XmlRpcValue parm = new XmlRpcValue(), response = new XmlRpcValue(), payload = new XmlRpcValue();
            parm.Set(0, this_node.Name);
            parm.Set(1, mapped_key);
            parm.Set(2, val);
            lock (parms_mutex)
            {
                if (master.execute("setParam", parm, ref response, ref payload, true))
                {
                    if (subscribed_params.Contains(mapped_key))
                        parms.Add(mapped_key, parm);
                }
            }
        }

        /// <summary>
        ///     Sets the paramater on the parameter server
        /// </summary>
        /// <param name="key">Name of the parameter</param>
        /// <param name="val">Value of the paramter</param>
        public static void set(string key, double val)
        {
            string mapped_key = names.resolve(key);
            XmlRpcValue parm = new XmlRpcValue(), response = new XmlRpcValue(), payload = new XmlRpcValue();
            parm.Set(0, this_node.Name);
            parm.Set(1, mapped_key);
            parm.Set(2, val);
            lock (parms_mutex)
            {
                if (master.execute("setParam", parm, ref response, ref payload, true))
                {
                    if (subscribed_params.Contains(mapped_key))
                        parms.Add(mapped_key, parm);
                }
            }
        }

        /// <summary>
        ///     Sets the paramater on the parameter server
        /// </summary>
        /// <param name="key">Name of the parameter</param>
        /// <param name="val">Value of the paramter</param>
        public static void set(string key, int val)
        {
            string mapped_key = names.resolve(key);
            XmlRpcValue parm = new XmlRpcValue(), response = new XmlRpcValue(), payload = new XmlRpcValue();
            parm.Set(0, this_node.Name);
            parm.Set(1, mapped_key);
            parm.Set(2, val);
            lock (parms_mutex)
            {
                if (master.execute("setParam", parm, ref response, ref payload, true))
                {
                    if (subscribed_params.Contains(mapped_key))
                        parms.Add(mapped_key, parm);
                }
            }
        }

        /// <summary>
        ///     Sets the paramater on the parameter server
        /// </summary>
        /// <param name="key">Name of the parameter</param>
        /// <param name="val">Value of the paramter</param>
        public static void set(string key, bool val)
        {
            string mapped_key = names.resolve(key);
            XmlRpcValue parm = new XmlRpcValue(), response = new XmlRpcValue(), payload = new XmlRpcValue();
            parm.Set(0, this_node.Name);
            parm.Set(1, mapped_key);
            parm.Set(2, val);
            lock (parms_mutex)
            {
                if (master.execute("setParam", parm, ref response, ref payload, true))
                {
                    if (subscribed_params.Contains(mapped_key))
                        parms.Add(mapped_key, parm);
                }
            }
        }

        /// <summary>
        ///     Gets the parameter from the parameter server
        /// </summary>
        /// <param name="key">Name of the parameter</param>
        /// <returns></returns>
        internal static XmlRpcValue getParam(String key, bool use_cache = false)
        {
            string mapped_key = names.resolve(key);
            XmlRpcValue payload = new XmlRpcValue();
            if (!getImpl(mapped_key, ref payload, use_cache))
                payload = null;
            return payload;
        }

        private static bool safeGet<T>(string key, ref T dest, object def = null)
        {
            try
            {
                XmlRpcValue v = getParam(key);
                if (v == null)
                {
                    if (def == null)
                        return false;
                    dest = (T) def;
                    return true;
                }
                dest = v.Get<T>();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool get(string key, ref XmlRpcValue dest)
        {
            return safeGet(key, ref dest);
        }

        public static bool get(string key, ref bool dest)
        {
            return safeGet(key, ref dest);
        }

        public static bool get(string key, ref bool dest, bool def)
        {
            return safeGet(key, ref dest, def);
        }

        public static bool get(string key, ref int dest)
        {
            return safeGet(key, ref dest);
        }

        public static bool get(string key, ref int dest, int def)
        {
            return safeGet(key, ref dest, def);
        }

        public static bool get(string key, ref double dest)
        {
            return safeGet(key, ref dest);
        }

        public static bool get(string key, ref double dest, double def)
        {
            return safeGet(key, ref dest, def);
        }

        public static bool get(string key, ref string dest, string def = null)
        {
            return safeGet(key, ref dest, dest);
        }

        public static List<string> list()
        {
            List<string> ret = new List<string>();
            XmlRpcValue parm = new XmlRpcValue(), result = new XmlRpcValue(), payload = new XmlRpcValue();
            parm.Set(0, this_node.Name);
            if (!master.execute("getParamNames", parm, ref result, ref payload, false))
                return ret;
			if (result.Size != 3 || result[0].GetInt() != 1 || result[2].Type != XmlRpcValue.ValueType.TypeArray)
            {
                Console.WriteLine("Expected a return code, a description, and a list!");
                return ret;
            }
            for (int i = 0; i < payload.Size; i++)
            {
                ret.Add(payload[i].GetString());
            }
            return ret;
        }

        /// <summary>
        ///     Checks if the paramter exists.
        /// </summary>
        /// <param name="key">Name of the paramerer</param>
        /// <returns></returns>
        public static bool has(string key)
        {
            XmlRpcValue parm = new XmlRpcValue(), result = new XmlRpcValue(), payload = new XmlRpcValue();
            parm.Set(0, this_node.Name);
            parm.Set(1, names.resolve(key));
            if (!master.execute("hasParam", parm, ref result, ref payload, false))
                return false;
            return payload.Get<bool>();
        }

        /// <summary>
        ///     Deletes a parameter from the parameter server.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool del(string key)
        {
            string mapped_key = names.resolve(key);
            lock (parms_mutex)
            {
                if (subscribed_params.Contains(key))
                {
                    subscribed_params.Remove(key);
                    if (parms.ContainsKey(key))
                        parms.Remove(key);
                }
            }

            XmlRpcValue parm = new XmlRpcValue(), result = new XmlRpcValue(), payload = new XmlRpcValue();
            parm.Set(0, this_node.Name);
            parm.Set(1, mapped_key);
            if (!master.execute("deleteParam", parm, ref result, ref payload, false))
                return false;
            return true;
        }

        public static void init(IDictionary remapping_args)
        {
            foreach (object o in remapping_args.Keys)
            {
                string name = (string) o;
                string param = (string) remapping_args[o];
                if (name.Length < 2) continue;
                if (name[0] == '_' && name[1] != '_')
                {
                    string local_name = "~" + name.Substring(1);
                    int i = 0;
                    bool success = int.TryParse(param, out i);
                    if (success)
                    {
                        set(names.resolve(local_name), i);
                        continue;
                    }
                    double d = 0;
                    success = double.TryParse(param, out d);
                    if (success)
                    {
                        set(names.resolve(local_name), d);
                        continue;
                    }
                    bool b = false;
                    success = bool.TryParse(param.ToLower(), out b);
                    if (success)
                    {
                        set(names.resolve(local_name), b);
                        continue;
                    }
                    set(names.resolve(local_name), param);
                }
            }
            XmlRpcManager.Instance.bind("paramUpdate", paramUpdateCallback);
        }

        /// <summary>
        ///     Manually update the value of a parameter
        /// </summary>
        /// <param name="key">Name of parameter</param>
        /// <param name="v">Value to update param to</param>
        public static void update(string key, XmlRpcValue v)
        {
            if (v == null)
                return;
            string clean_key = names.clean(key);
            lock (parms_mutex)
            {
                if (!parms.ContainsKey(clean_key))
                    parms.Add(clean_key, v);
                else
                    parms[clean_key] = v;
                if (BoolCallbacks.ContainsKey(clean_key))
                {
                    foreach (ParamBoolDelegate b in BoolCallbacks[clean_key])
                        b.Invoke(clean_key, new XmlRpcValue(v).GetBool());
                }
                if (IntCallbacks.ContainsKey(clean_key))
                {
                    foreach (ParamIntDelegate b in IntCallbacks[clean_key])
                        b.Invoke(clean_key, new XmlRpcValue(v).GetInt());
                }
                if (DoubleCallbacks.ContainsKey(clean_key))
                {
                    foreach (ParamDoubleDelegate b in DoubleCallbacks[clean_key])
                        b.Invoke(clean_key, new XmlRpcValue(v).GetDouble());
                }
                if (StringCallbacks.ContainsKey(clean_key))
                {
                    foreach (ParamStringDelegate b in StringCallbacks[clean_key])
                        b.Invoke(clean_key, new XmlRpcValue(v).GetString());
                }
                if (Callbacks.ContainsKey(clean_key))
                {
                    foreach (ParamDelegate b in Callbacks[clean_key])
                        b.Invoke(clean_key, new XmlRpcValue(v));
                }
            }
        }

        /// <summary>
        ///     Fired when a parameter gets updated
        /// </summary>
        /// <param name="parm">Name of parameter</param>
        /// <param name="result">New value of parameter</param>
        public static void paramUpdateCallback(IntPtr parm, IntPtr result)
        {
            XmlRpcValue val = XmlRpcValue.LookUp(parm);
            val.Set(0, 1);
            val.Set(1, "");
            val.Set(2, 0);
            update(XmlRpcValue.LookUp(parm)[1].Get<string>(), XmlRpcValue.LookUp(parm)[2]);
        }

        public static bool getImpl(string key, ref XmlRpcValue v, bool use_cache)
        {
            string mapped_key = names.resolve(key);

            if (use_cache)
            {
                lock (parms_mutex)
                {
                    if (subscribed_params.Contains(mapped_key))
                    {
                        if (parms.ContainsKey(mapped_key))
                        {
                            if (parms[mapped_key].Valid)
                            {
                                v = parms[mapped_key];
                                return true;
                            }
                            return false;
                        }
                    }
                    else
                    {
                        subscribed_params.Add(mapped_key);
                        XmlRpcValue parm = new XmlRpcValue(), result = new XmlRpcValue(), payload = new XmlRpcValue();
                        parm.Set(0, this_node.Name);
                        parm.Set(1, XmlRpcManager.Instance.uri);
                        parm.Set(2, mapped_key);
                        if (!master.execute("subscribeParam", parm, ref result, ref payload, false))
                        {
                            subscribed_params.Remove(mapped_key);
                            use_cache = false;
                        }
                    }
                }
            }

            XmlRpcValue parm2 = new XmlRpcValue(), result2 = new XmlRpcValue();
            parm2.Set(0, this_node.Name);
            parm2.Set(1, mapped_key);

            bool ret = master.execute("getParam", parm2, ref result2, ref v, false);

            if (use_cache)
            {
                lock (parms_mutex)
                {
                    parms.Add(mapped_key, v);
                }
            }

            return ret;
        }
    }
}