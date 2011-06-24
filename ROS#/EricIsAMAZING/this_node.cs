using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EricIsAMAZING
{
    public static class this_node
    {
        public static string Name = "empty";
        public static string Namespace = "";
        public static List<string> AdvertisedTopics()
        {
            return TopicManager.Instance().getAdvertisedTopics();
        }
        
        public static void Init(string n, IDictionary remappings, int options=0)
        {
            Name = n;
            bool disable_anon = false;
            if (remappings.Contains("__name"))
            {
                Name = (string)remappings["__name"];
                disable_anon = true;
            }
            if (remappings.Contains("__ns"))
            {
                Namespace = (string)remappings["__ns"];
            }
            if (Namespace == "") Namespace = "/";

            long walltime = DateTime.Now.Subtract(System.Diagnostics.Process.GetCurrentProcess().StartTime).Ticks;
            names.Init(remappings);
            if (Name.Contains("/"))
                throw new Exception("NAMES CANT HAVE SLASHES, WENCH!");
            if (Name.Contains("~"))
                throw new Exception("NAMES CANT HAVE SQUIGGLES, WENCH!");
            Name = names.resolve(Namespace, Name);
            if ((options & (int)InitOption.AnonymousName) == (int)InitOption.AnonymousName && !disable_anon)
            {
                int lbefore = Name.Length;
                Name += "_" + walltime;
                if (Name.Length - lbefore > 201)
                    Name = Name.Remove(lbefore + 201);
            }
        }
    }
}
