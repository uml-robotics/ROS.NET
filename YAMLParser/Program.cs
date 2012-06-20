#region USINGZ

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Messages;

#endregion

namespace YAMLParser
{
    internal class Program
    {
        public static List<MsgsFile> msgsFiles = new List<MsgsFile>();
        public static List<SrvsFile> srvFiles = new List<SrvsFile>();
        public static string backhalf;
        public static string fronthalf;

        public static string outputdir = "..\\..\\..\\Messages";

        private static void Main(string[] args)
        {
            if (args.Length >= 1)
            {
                outputdir = args[0];
            }
            List<string> paths = new List<string>();
            List<string> pathssrv = new List<string>();
            List<string> std = new List<string>();
            List<string> srv = new List<string>();
            Console.WriteLine
                (
                    "Generatinc C# classes for ROS Messages:\n\tstd_msgs\t\t(in namespace \"Messages\")\n\tgeometry_msgs\t\t(in namespace \"Messages.geometry_msgs\")\n\tnav_msgs\t\t(in namespace \"Messages.nav_msgs\")");
            if (!Directory.Exists("ROS_MESSAGES"))
            {
                Console.WriteLine("the ROS_MESSAGES folder must be in the same folder as the executable!");
                Console.WriteLine("Press Enter");
                Console.ReadLine();
            }
            std.AddRange(Directory.GetFiles("ROS_MESSAGES", "*.msg"));
            srv.AddRange(Directory.GetFiles("ROS_MESSAGES", "*.srv"));
            foreach (string dir in Directory.GetDirectories("ROS_MESSAGES"))
            {
                std.AddRange(Directory.GetFiles(dir, "*.msg"));
                srv.AddRange(Directory.GetFiles(dir, "*.srv"));
            }
            if (args.Length == 1)
            {
                paths.AddRange(Directory.GetFiles(".", "*.msg"));
                pathssrv.AddRange(Directory.GetFiles(".", "*.srv"));
            }
            else
            {
                for (int i = 1; i < args.Length; i++)
                {
                    if (args[i].Contains(".msg"))
                        paths.Add(args[i]);
                    else if (args[i].Contains(".srv"))
                        pathssrv.Add(args[i]);
                    else
                    {
                        string[] paths2 = Directory.GetFiles(args[i], "*.msg");
                        if (paths2.Length != 0)
                            paths.AddRange(paths2);
                        string[] paths3 = Directory.GetFiles(args[i], "*.srv");
                        if (paths3.Length != 0)
                            pathssrv.AddRange(paths3);
                    }
                }
            }
            foreach (string path in std)
            {
                msgsFiles.Add(new MsgsFile(path));
            }
            foreach (string path in srv)
            {
                srvFiles.Add(new SrvsFile(path));
            }
            if (paths.Count > 0)
            {
                Console.WriteLine("Custom messages being parsed+generated:");
                foreach (string path in paths)
                {
                    Console.WriteLine("\t" + path.Replace(".\\", ""));
                    msgsFiles.Add(new MsgsFile(path));
                }
            }
            if (pathssrv.Count > 0)
            {
                Console.WriteLine("Custom services being parsed+generated:");
                foreach (string path in pathssrv)
                {
                    Console.WriteLine("\t" + path.Replace(".\\", ""));
                    srvFiles.Add(new SrvsFile(path));
                }
            }
            if (std.Count + paths.Count + srv.Count + pathssrv.Count > 0)
            {
                MakeTempDir();
                GenerateFiles(msgsFiles, srvFiles);
                GenerateProject(msgsFiles, srvFiles);
                BuildProject();
            }
            else
            {
                Console.WriteLine("YOU SUCK AND I HOPE YOU DIE!!!!");
            }
            Console.ReadLine();
        }

        public static void MakeTempDir()
        {
            if (!Directory.Exists(outputdir)) Directory.CreateDirectory(outputdir);
            else
            {
                foreach (string s in Directory.GetFiles(outputdir, "*.cs")) File.Delete(s);
                foreach (string s in Directory.GetDirectories(outputdir))
                    if (s != "Properties")
                        Directory.Delete(s, true);
            }
        }

        public static void GenerateFiles(List<MsgsFile> files, List<SrvsFile> srvfiles)
        {
            foreach (MsgsFile file in files)
            {
                file.Write(outputdir);
            }
            foreach (SrvsFile file in srvfiles)
            {
                file.Write(outputdir);
            }
            File.WriteAllText(outputdir + "\\MessageTypes.cs", ToString());
        }

        public static void GenerateProject(List<MsgsFile> files, List<SrvsFile> srvfiles)
        {
            if (!Directory.Exists(outputdir + "\\Properties"))
                Directory.CreateDirectory(outputdir + "\\Properties");
            File.WriteAllLines
                (outputdir + "\\Properties\\AssemblyInfo.cs",
                 File.ReadAllLines(Environment.CurrentDirectory + "\\TemplateProject\\AssemblyInfo._cs"));

            string[] lines = File.ReadAllLines(Environment.CurrentDirectory + "\\TemplateProject\\Messages._csproj");
            string output = "";
            for (int i = 0; i < lines.Length; i++)
            {
                output += "" + lines[i] + "\n";
                if (lines[i].Contains("<Compile Include="))
                {
                    foreach (MsgsFile m in files)
                    {
                        output += "\t<Compile Include=\"" + m.Name.Replace('.', '\\') + ".cs\" />\n";
                    }
                    foreach (SrvsFile m in srvFiles)
                    {
                        output += "\t<Compile Include=\"" + m.Name.Replace('.', '\\') + ".cs\" />\n";
                    }
                    output += "\t<Compile Include=\"SerializationHelper.cs\" />\n";
                    output += "\t<Compile Include=\"MessageTypes.cs\" />\n";
                    File.Copy("TemplateProject\\SerializationHelper.cs", outputdir + "\\SerializationHelper.cs");
                }
            }
            File.WriteAllText(outputdir + "\\Messages.csproj", output);
        }

        public static void BuildProject()
        {
            string VCDir = "";
            foreach (
                string dir in
                    Directory.GetDirectories
                        (Environment.GetFolderPath(Environment.SpecialFolder.Windows) +
                         (Environment.Is64BitOperatingSystem
                              ? "\\Microsoft.NET\\Framework64\\"
                              : "\\Microsoft.NET\\Framework")))
            {
                string[] tmp = dir.Split('\\');
                if (tmp[tmp.Length - 1].Contains("v4.0"))
                {
                    VCDir = dir;
                    break;
                }
            }
            string F = VCDir + "\\msbuild.exe";
            Console.WriteLine("\n\nBUILDING GENERATED PROJECT WITH MSBUILD!");
            string args = "/nologo \"" + outputdir+"\\Messages.csproj\"";
            Process proc = new Process();
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.FileName = F;
            proc.StartInfo.Arguments = args;
            proc.Start();
            string output = proc.StandardOutput.ReadToEnd();
            string error = proc.StandardError.ReadToEnd();
            if (File.Exists(outputdir + "\\bin\\Debug\\Messages.dll"))
            {
                Console.WriteLine("\n\nGenerated DLL has been copied to:\n\t"+outputdir+"\\Messages.dll\n\n");
                if (File.Exists(outputdir + "\\Messages.dll"))
                    File.Delete(outputdir + "\\Messages.dll");
                File.Copy(outputdir + "\\bin\\Debug\\Messages.dll", outputdir + "\\Messages.dll");
            }
            else
            {
                if (output.Length > 0)
                    Console.WriteLine(output);
                if (error.Length > 0)
                    Console.WriteLine(error);
                Console.WriteLine("AMG BUILD FAIL!");
            }
        }

        public new static string ToString()
        {
            if (fronthalf == null)
            {
                fronthalf = "";
                backhalf = "";
                string[] lines = File.ReadAllLines("TemplateProject\\PlaceHolder._cs");
                bool hitvariablehole = false;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains("$$DOLLADOLLABILLS"))
                    {
                        hitvariablehole = true;
                        continue;
                    }
                    if (lines[i].Contains("namespace"))
                    {
                        fronthalf +=
                            "using Messages;\nusing Messages.std_msgs;\nusing Messages.rosgraph_msgs;\nusing Messages.custom_msgs;\nusing Messages.geometry_msgs;\nusing Messages.nav_msgs;\nusing String=Messages.std_msgs.String;\nusing Messages.roscsharp;\n\n";
                        fronthalf += "namespace " + "Messages" + "\n";
                        continue;
                    }
                    if (!hitvariablehole)
                        fronthalf += lines[i] + "\n";
                    else
                        backhalf += lines[i] + "\n";
                }
            }
            fronthalf +=
                "\tpublic static class TypeHelper\n\t{\n\t\tpublic static System.Type GetType(string name)\n\t\t{\n\t\t\treturn System.Type.GetType(name, true, true);\n\t\t}\n";
            List<MsgsFile> everything = new List<MsgsFile>(msgsFiles);
            foreach (SrvsFile sf in srvFiles)
            {
                everything.Add(sf.Request);
                everything.Add(sf.Response);
            }

            GenDict
                ("TypeInformation", "MsgTypes", "TypeInfo", ref fronthalf, 0, everything.Count,
                 (i) => string.Format("{0}", everything[i].GeneratedDictHelper));

            //GenDict("Types", "MsgTypes", "Type", ref fronthalf, 0, types.Count,
            //    (i) => string.Format("MsgTypes.{0}{1}", (namespaces[i].Length > 0 ? (namespaces[i] + "__") : ""), types[i]),
            //    (i) => string.Format("typeof(TypedMessage<{0}{1}>)", (namespaces[i].Length > 0 ? namespaces[i] + "." : ""), types[i]));


            //fronthalf += "\n\n\t\t\tpublic static Dictionary<MsgTypes, string> MessageDefinitions = new Dictionary<MsgTypes, string>\n\t\t{";
            //fronthalf += "\n\t\t\t{MsgTypes.Unknown, \"IDFK\"},\n";
            //for (int i = 0; i < MessageDefs.Count; i++)
            //{
            //    fronthalf += "\t\t\t{MsgTypes." + (namespaces[i].Length > 0 ? (namespaces[i] + "__") : "") +  types[i] + ", \n\t\t\t@\"\n";
            //    foreach (string s in MessageDefs[i])
            //        fronthalf += "" + s.Trim() + "\n";
            //    fronthalf += "\t\t\t\"}";
            //    if (i < MessageDefs.Count - 1)
            //        fronthalf += ",\n";
            //}
            //fronthalf += "};\n";
            //fronthalf += "\n\t\tpublic static Dictionary<MsgTypes, bool> IsMetaType = new Dictionary<MsgTypes, bool>()\n\t\t{";
            //fronthalf += "\n\t\t\t{MsgTypes.Unknown, false},";
            //for (int i = 0; i < types.Count; i++)
            //{
            //    fronthalf += "\n\t\t\t";
            //    fronthalf += "{MsgTypes." + (namespaces[i].Length > 0 ? (namespaces[i] + "__") : "") + types[i] + ", " + ismetas[i].ToString().ToLower() + "}";
            //    if (i < types.Count - 1)
            //        fronthalf += ",";
            //}
            //fronthalf += "\n\t\t};";
            fronthalf += "\t}\n\n\tpublic enum MsgTypes\n\t{";
            fronthalf += "\n\t\tUnknown,";
            for (int i = 0; i < everything.Count; i++)
            {
                fronthalf += "\n\t\t";
                if (everything[i].classname == "Request" || everything[i].classname == "Response")
                    everything[i].Name += "." + everything[i].classname;
                fronthalf += everything[i].Name.Replace(".", "__");
                if (i < everything.Count - 1)
                    fronthalf += ",";
            }
            fronthalf += "\n\t}\n";
            string ret = fronthalf + backhalf;
            return ret;
        }

        public static void GenDict
            (string dictname, string keytype, string valuetype, ref string appendto, int start, int end,
             Func<int, string> genKey, Func<int, string> genVal = null, string DEFAULT = null)
        {
            appendto +=
                string.Format("\n\t\tpublic static Dictionary<{1}, {2}> {0} = new Dictionary<{1}, {2}>()\n\t\t{{",
                              dictname, keytype, valuetype);
            if (DEFAULT != null)
                appendto += "\n\t\t\t{" + DEFAULT + ",\n";
            for (int i = start; i < end; i++)
            {
                if (genVal != null)
                    appendto += string.Format("\t\t\t{{{0}, {1}}}{2}", genKey(i), genVal(i), (i < end - 1 ? ",\n" : ""));
                else
                    appendto += string.Format("\t\t\t{{{0}}}{1}", genKey(i), (i < end - 1 ? ",\n" : ""));
            }
            appendto += "\n\t\t};";
        }
    }
}