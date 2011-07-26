#region USINGZ

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Messages;

#endregion

namespace YAMLParser
{
    internal class Program
    {
        public static List<MsgsFile> msgsFiles = new List<MsgsFile>();
        public static string backhalf;
        public static string fronthalf;

        public static string outputdir
        {
            get { return "..\\..\\..\\Messages"; }
        }

        private static void Main(string[] args)
        {
            List<string> paths = new List<string>();
            List<string> std = new List<string>();
            Console.WriteLine
                (
                    "Generatinc C# classes for ROS Messages:\n\tstd_msgs\t\t(in namespace \"Messages\")\n\tgeometry_msgs\t\t(in namespace \"Messages.geometry_msgs\")\n\tnav_msgs\t\t(in namespace \"Messages.nav_msgs\")");
            std.AddRange(Directory.GetFiles("ROS_MESSAGES", "*.msg"));
            foreach (string dir in Directory.GetDirectories("ROS_MESSAGES"))
            {
                std.AddRange(Directory.GetFiles(dir, "*.msg"));
            }
            if (args.Length == 0)
            {
                paths.AddRange(Directory.GetFiles(".", "*.msg"));
            }
            else
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].Contains(".msg"))
                        paths.Add(args[i]);
                    else
                    {
                        string[] paths2 = Directory.GetFiles(args[i], "*.msg");
                        if (paths2.Length != 0)
                            paths.AddRange(paths2);
                    }
                }
            }
            foreach (string path in std)
            {
                msgsFiles.Add(new MsgsFile(path));
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
            if (std.Count + paths.Count > 0)
            {
                MakeTempDir();
                GenerateFiles(msgsFiles);
                GenerateProject(msgsFiles);
                BuildProject();
                //Console.WriteLine("Press enter to finish...");
                //Console.ReadLine();
            }
            else
            {
                Console.WriteLine("YOU SUCK AND I HOPE YOU DIE!!!!");
                Console.ReadLine();
            }
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

        public static void GenerateFiles(List<MsgsFile> files)
        {
            foreach (MsgsFile m in files)
            {
                m.Write();
            }

            File.WriteAllText(outputdir + "\\MessageTypes.cs", ToString());
        }

        public static void GenerateProject(List<MsgsFile> files)
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
            string args = "/nologo \"" + Environment.CurrentDirectory + "\\YAMLProjectDir\\Messages.csproj\"";
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
                Console.WriteLine("\n\nGenerated DLL has been copied to:\n\t.\\Messages.dll\n\n");
                if (File.Exists(Environment.CurrentDirectory + "\\Messages.dll"))
                    File.Delete(Environment.CurrentDirectory + "\\Messages.dll");
                File.Copy(outputdir + "\\bin\\Debug\\Messages.dll", Environment.CurrentDirectory + "\\Messages.dll");
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
                        fronthalf += "using Messages;\nusing Messages.std_msgs;\nusing Messages.geometry_msgs;\nusing Messages.nav_msgs;\n\n";
                        fronthalf += "namespace " + "Messages" + "\n";
                        continue;
                    }
                    if (!hitvariablehole)
                        fronthalf += lines[i] + "\n";
                    else
                        backhalf += lines[i] + "\n";
                }
            }
            fronthalf += "\tpublic static class TypeHelper\n\t{";

            GenDict("TypeInformation", "MsgTypes", "TypeInfo", ref fronthalf, 0, msgsFiles.Count,
                (i) => string.Format("{0}", msgsFiles[i].GeneratedDictHelper));

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
            for (int i = 0; i < msgsFiles.Count; i++)
            {
                fronthalf += "\n\t\t";
                fronthalf += msgsFiles[i].Name.Replace(".", "__");
                if (i < msgsFiles.Count - 1)
                    fronthalf += ",";
            }
            fronthalf += "\n\t}\n";
            string ret = fronthalf + backhalf;
            return ret;
        }

        public static void GenDict(string dictname, string keytype, string valuetype, ref string appendto, int start, int end, Func<int, string> genKey, Func<int, string> genVal = null, string DEFAULT=null)
        {
            appendto += string.Format("\n\t\tpublic static Dictionary<{1}, {2}> {0} = new Dictionary<{1}, {2}>()\n\t\t{{", dictname, keytype, valuetype);
            if (DEFAULT != null)
                appendto += "\n\t\t\t{"+DEFAULT+",\n";
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