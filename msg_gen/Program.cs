using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace msg_gen
{
    internal class Program
    {
        private static string[] msg_gen_folder_names = new[]
        {
            "msg",
            "srv",
            "msgs",
            "srvs"
        };

        private static string getPackageName(string path, out string targetmsgpath)
        {   
            if (File.Exists(path))
            {
                string[] chunks = path.Split('\\');
                string foldername = chunks[chunks.Length - 2];
                if (msg_gen_folder_names.Contains(foldername))
                    foldername = chunks[chunks.Length - 3];
                targetmsgpath = foldername+"\\"+chunks[chunks.Length-1];
                return foldername;
            }
            targetmsgpath = null;
            throw new Exception("Fail");
        }

        private static string getPackagePath(string basedir, string msgpath, out string targetmsgpath)
        {
            string m = null;
            string p = getPackageName(msgpath, out m);
            targetmsgpath = basedir+"\\"+m;
            return basedir + "\\" + p;
        }

        private static void explode(ref List<string> m, ref List<string> s, string path)
        {
            m.AddRange(Directory.EnumerateFiles(path, "*.msg", SearchOption.AllDirectories).Except(m).ToList());
            s.AddRange(Directory.EnumerateFiles(path, "*.srv", SearchOption.AllDirectories).Except(s).ToList());
        }

        static void Main(string[] args)
        {
            string directory = Environment.GetEnvironmentVariable("TMP") + "\\msgs_flat";
            try
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
            catch(System.IO.DirectoryNotFoundException dnfe)
            {
                Console.WriteLine(dnfe);
            }
            Directory.CreateDirectory(directory);
            List<string> msgs = new List<string>();
            List<string> srvs = new List<string>();

            //solution directory (where the reference to msg_gen is) is passed -- or assumed to be in a file in the same directory as the executable (which would be the case when msg_gen is directly run in the debugger
            if (args.Length == 0)
            {
                Console.WriteLine("MsgGen needs to receive a list of paths to recursively find messages in order to work.");
                Environment.Exit(1);
            }
            foreach (string arg in args)
            {
                explode(ref msgs, ref srvs, new DirectoryInfo(arg).FullName);
            }

            foreach (string s in msgs.Concat(srvs))
            {
                string dest = null;
                string p = getPackagePath(directory, s, out dest);
                if (!Directory.Exists(p))
                    Directory.CreateDirectory(p);
                Console.WriteLine(s + " ==> " + dest);
                if (dest != null)
                    File.Copy(s, dest, true);
            }
        }
    }
}
