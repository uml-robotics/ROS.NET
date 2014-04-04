using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace rosmaster
{
    public static class Names
    {
        private static readonly String SEP = "/";
        private static readonly String PRIV_NAME = "~";

        /*public String get_ros_namespace(String env = null)
        {
            if(env == null)
                env = "";
            return make_global_ns(env.get(ROS_NAMESPACE, GLOBALNS));
        }

        public static String make_caller_id()
        {
            return make_global_ns(ns_join(get_ros_namespace(), name));
        }*/

        public static String make_global_ns(String name)
        {
            if (is_private(name))
                name = SEP + name;
            if (!is_global(name))
                name = SEP + name;
            if (!name.EndsWith(SEP))
                name = name + SEP;
            return name;
        }

        public  static Boolean is_global(String name)
        {
            return name.Length >0 && name.StartsWith(SEP);
        }

        public static Boolean is_private(String name)
        {
            return name.Length >0 && name.StartsWith(PRIV_NAME);
        }

        public static String ns(String name)
        {
            if (name=="")
            {
                return SEP;
            }
            if (name.EndsWith(SEP))
                return SEP;
            return name.Split('/')[0] + "/";
        }

        public static String ns_join(String ns, String name)
        {
            if(is_private(name) || is_global(name))
                return name;
            if(ns == PRIV_NAME)
                return PRIV_NAME + name;
            if(ns == "")
                return name;
            if(ns.EndsWith("/"))
                return ns + name;
            return ns + SEP + name;
        }

        public static String canonicalize_name(String name)
        {
            if (name == "" || name == SEP)
                return name;

            String[] str = name.Split('/');
            String rtn = "";
            for (int i = 0; i < str.Length; i++)
            {
                if(str[i].Length > 1)
                    rtn += "/" + str[i];
            }
            return rtn;
        }

        public static String resolve_name(String name, String _namespace, Dictionary<String,String> remappings = null)
        {
            if (name.Length == 0)
                return ns(_namespace);
            String resolved_name;
            name = canonicalize_name(name);
            if(name.StartsWith("/"))
                resolved_name = name;
            else if(is_private(name))
                resolved_name = canonicalize_name(_namespace + SEP + name.Replace("~",""));
            else
                resolved_name = ns(_namespace + name);

            if (remappings != null && remappings.ContainsKey(resolved_name))
                return remappings[resolved_name];
            else
                return resolved_name;
        }

    }
}
