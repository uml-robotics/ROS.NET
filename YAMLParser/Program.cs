// File: Program.cs
// Project: YAMLParser
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 04/28/2015
// Updated: 10/07/2015

#region USINGZ

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using FauxMessages;

#endregion

namespace YAMLParser
{
    internal class Program
    {
        public static List<MsgsFile> msgsFiles = new List<MsgsFile>();
        public static List<SrvsFile> srvFiles = new List<SrvsFile>();
        public static string backhalf;
        public static string fronthalf;
        public static string outputdir = "Messages";
        public static string name = "Messages";
        public static string outputdir_secondpass = "TempSecondPass";
#if !NOT_ON_TOP_OF_ITSELF
        public static string outputdir_firstpass = outputdir;
        public static string name_firstpass = name;
#else
        public static string outputdir_firstpass = "..\\..\\..\\TempMessages";
        public static string name_firstpass = "TempMessages";
#endif

        private static void Main(string[] args)
        {
            string solutiondir;
            bool interactive = false; //wait for ENTER press when complete
            int firstarg = 0;
            if (args.Length >= 1)
            {
                if (args[0].Trim().Equals("-i"))
                {
                    interactive = true;
                    firstarg++;
                }
            }
            string yamlparser_parent = "";
            DirectoryInfo di = new DirectoryInfo(Environment.CurrentDirectory);
            while (di.Name != "YAMLParser")
            {
                di = Directory.GetParent(di.FullName);
            }
            di = Directory.GetParent(di.FullName);
            yamlparser_parent = di.FullName;
            if (args.Length - firstarg >= 1)
            {
                solutiondir = new DirectoryInfo(args[firstarg]).FullName;
            }
            else
            {
                solutiondir = yamlparser_parent;
            }

            outputdir = solutiondir + "\\" + outputdir;
            outputdir_firstpass = solutiondir + "\\" + outputdir_firstpass;
            outputdir_secondpass = solutiondir + "\\" + outputdir_secondpass;
            List<MsgFileLocation> paths = new List<MsgFileLocation>();
            List<MsgFileLocation> pathssrv = new List<MsgFileLocation>();
            Console.WriteLine("Generatinc C# classes for ROS Messages...\n");
            for (int i = firstarg; i < args.Length; i++)
            {
                string d = new DirectoryInfo(args[i]).FullName;
                Console.WriteLine("Spelunking in " + d);
                MsgFileLocator.findMessages(paths, pathssrv, d);
            }
            foreach (MsgFileLocation path in paths)
            {
                msgsFiles.Add(new MsgsFile(path));
            }
            foreach (MsgFileLocation path in pathssrv)
            {
                srvFiles.Add(new SrvsFile(path));
            }
            if (paths.Count + pathssrv.Count > 0)
            {
                MakeTempDir();
                foreach (MsgsFile m in msgsFiles)
                {
                    foreach (SingleType s in m.Stuff)
                    {
                        s.refinalize(m, s.Type);
                    }
                }
                GenerateFiles(msgsFiles, srvFiles);
                GenerateProject(msgsFiles, srvFiles, false);
                GenerateProject(msgsFiles, srvFiles, true);
                BuildProject();
                Finalize(solutiondir);
            }
            else
            {
                Console.WriteLine("Usage:         YAMLParser.exe <SolutionFolder> [... other directories to search]\n      The Messages dll will be output to <SolutionFolder>/Messages/Messages.dll");
                if (interactive)
                    Console.ReadLine();
                Environment.Exit(1);
            }
            if (interactive)
            {
                Console.WriteLine("Finished. Press enter.");
                Console.ReadLine();
            }
        }

        public static void MakeTempDir()
        {
            if (!Directory.Exists(outputdir)) Directory.CreateDirectory(outputdir);
            else
            {
                foreach (string s in Directory.GetFiles(outputdir, "*.cs"))
                    try
                    {
                        File.Delete(s);
                        Thread.Sleep(100);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                foreach (string s in Directory.GetDirectories(outputdir))
                    if (s != "Properties")
                        try
                        {
                            Directory.Delete(s, true);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
            }
            if (!Directory.Exists(outputdir_firstpass)) Directory.CreateDirectory(outputdir_firstpass);
            else
            {
                foreach (string s in Directory.GetFiles(outputdir_firstpass, "*.cs"))
                    try
                    {
                        File.Delete(s);
                        Thread.Sleep(100);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                foreach (string s in Directory.GetDirectories(outputdir_firstpass))
                    if (s != "Properties")
                        try
                        {
                            Directory.Delete(s, true);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
            }
        }

        public static void GenerateFiles(List<MsgsFile> files, List<SrvsFile> srvfiles)
        {
            foreach (MsgsFile file in files)
            {
                file.Write(outputdir);
#if !!NOT_ON_TOP_OF_ITSELF
                file.Write(outputdir_firstpass);
#endif
                Thread.Sleep(10);
            }
            foreach (SrvsFile file in srvfiles)
            {
                file.Write(outputdir);
#if !!NOT_ON_TOP_OF_ITSELF
                file.Write(outputdir_firstpass);
#endif
                Thread.Sleep(10);
            }
#if !!NOT_ON_TOP_OF_ITSELF
            File.WriteAllText(outputdir_firstpass + "\\MessageTypes.cs", ToString().Replace("FauxMessages",""));
#endif
            File.WriteAllText(outputdir + "\\MessageTypes.cs", ToString().Replace("FauxMessages", "Messages"));
            Thread.Sleep(100);
        }

        public static void GenerateProject(List<MsgsFile> files, List<SrvsFile> srvfiles, bool istemp)
        {
            if (!Directory.Exists((istemp ? outputdir_firstpass : outputdir) + "\\Properties"))
                Directory.CreateDirectory((istemp ? outputdir_firstpass : outputdir) + "\\Properties");
            Thread.Sleep(10);
            string[] l = File.ReadAllLines(Environment.CurrentDirectory + "\\TemplateProject\\AssemblyInfo._cs");
            Thread.Sleep(10);
            File.WriteAllLines((istemp ? outputdir_firstpass : outputdir) + "\\Properties\\AssemblyInfo.cs", l);
            Thread.Sleep(100);
            string[] lines = File.ReadAllLines(Environment.CurrentDirectory + "\\TemplateProject\\" + (istemp ? name_firstpass : name) + "._csproj");
            string output = "";
            for (int i = 0; i < lines.Length; i++)
            {
#if FOR_UNITY
                if (lines[i].Contains("TargetFrameworkProfile"))
                    output += "<TargetFrameworkProfile>Unity Full v3.5</TargetFrameworkProfile>\n";
                else
#endif
                {
                    output += "" + lines[i] + "\n";
                }

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
                    output += "\t<Compile Include=\"Interfaces.cs\" />\n";
                    output += "\t<Compile Include=\"MessageTypes.cs\" />\n";
                }
            }
            File.Copy("TemplateProject\\SerializationHelper.cs", (istemp ? outputdir_firstpass : outputdir) + "\\SerializationHelper.cs", true);
            File.Copy("TemplateProject\\Interfaces.cs", (istemp ? outputdir_firstpass : outputdir) + "\\Interfaces.cs", true);
            File.WriteAllText((istemp ? outputdir_firstpass : outputdir) + "\\.gitignore", "*");
            File.WriteAllText((istemp ? (outputdir_firstpass + "\\" + (istemp ? name_firstpass : name) + ".csproj") : (outputdir + "\\" + (istemp ? name_firstpass : name) + ".csproj")), output);
            Thread.Sleep(100);
        }

        private static string __where_be_at_my_vc____is;

        public static string VCDir
        {
            get
            {
                if (__where_be_at_my_vc____is != null) return __where_be_at_my_vc____is;
                foreach (string possibledir in new[] {"\\Microsoft.NET\\Framework64\\", "\\Microsoft.NET\\Framework"})
                {
                    foreach (string possibleversion in new[] {"v3.5", "v4.0"})
                    {
                        if (!Directory.Exists(Environment.GetFolderPath(Environment.SpecialFolder.System) + "\\.." + possibledir)) continue;
                        foreach (string dir in Directory.GetDirectories(Environment.GetFolderPath(Environment.SpecialFolder.System) + "\\.." + possibledir))
                        {
                            if (!Directory.Exists(dir)) continue;
                            string[] tmp = dir.Split('\\');
                            if (tmp[tmp.Length - 1].Contains(possibleversion))
                            {
                                __where_be_at_my_vc____is = dir;
                                return __where_be_at_my_vc____is;
                            }
                        }
                    }
                }
                return __where_be_at_my_vc____is;
            }
        }

        public static void BuildProject()
        {
            BuildProject("BUILDING GENERATED PROJECT WITH MSBUILD!");
        }

        public static void BuildProject(string spam)
        {
            string F = VCDir + "\\msbuild.exe";
            if (!File.Exists(F))
            {
                Exception up = new Exception("ALL OVER YOUR FACE\n" + F);
                throw up;
            }
            Console.WriteLine("\n\n" + spam);
            string args = "/nologo \"" + outputdir_firstpass + "\\" + name_firstpass + ".csproj\"";
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
            if (File.Exists(outputdir_firstpass + "\\bin\\Debug\\" + name_firstpass + ".dll"))
            {
                Console.WriteLine("\n\nGenerated DLL has been copied to:\n\t" + outputdir_firstpass + "\\" + name_firstpass + ".dll\n\n");
                File.Copy(outputdir_firstpass + "\\bin\\Debug\\" + name_firstpass + ".dll", outputdir_firstpass + "\\" + name_firstpass + ".dll", true);
                Thread.Sleep(100);
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

        public static void Finalize(string solutiondir)
        {
            //copy unmodified SecondPass up to solution directory
            if (Directory.Exists(outputdir_secondpass))
                Directory.Delete(outputdir_secondpass, true);
            FileUtils.DirectoryCopy(Environment.CurrentDirectory + "\\..\\..\\..\\SecondPass\\", outputdir_secondpass);
            string projfile = outputdir_secondpass+"\\SecondPass.csproj";

            //modify it for this solution's specific Messages project location
            File.WriteAllText(projfile,File.ReadAllText(projfile).Replace("$(SolutionDir)", solutiondir));

            string F = VCDir + "\\msbuild.exe";
            string args = "/nologo \"" + outputdir_secondpass + "\\SecondPass.csproj\"";
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
            string output2 = "", error2 = "";
#if !SINGLE_PASS
            if (File.Exists(outputdir_secondpass + "\\bin\\Debug\\SecondPass.exe"))
            {
                Process proc2 = new Process();
                proc2.StartInfo.RedirectStandardOutput = true;
                proc2.StartInfo.RedirectStandardError = true;
                proc2.StartInfo.UseShellExecute = false;
                proc2.StartInfo.CreateNoWindow = true;
#if !!NOT_ON_TOP_OF_ITSELF
                proc2.StartInfo.Arguments = "..\\..\\..\\TempMessages\\";
#else
                proc2.StartInfo.Arguments = solutiondir+"\\Messages\\";
#endif
                proc2.StartInfo.FileName = outputdir_secondpass + "\\bin\\Debug\\SecondPass.exe";
                proc2.Start();
                output2 = proc2.StandardOutput.ReadToEnd();
                error2 = proc2.StandardError.ReadToEnd();
                BuildProject("REBUILDING THE REFINED GENERATED CODE!");
                if (Directory.Exists(outputdir_secondpass))
                    Directory.Delete(outputdir_secondpass, true);
            }
            else
            {
                if (output.Length > 0)
                    Console.WriteLine(output);
                if (error.Length > 0)
                    Console.WriteLine(error);
                Console.WriteLine("AMG BUILD FAIL!");
            }
            if (output2.Length > 0)
                Console.WriteLine(output2);
            if (error2.Length > 0)
                Console.WriteLine(error2);
            proc = new Process {StartInfo = {RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true, FileName = F, Arguments = "/nologo \"" + outputdir + "\\Messages.csproj\""}};
            proc.Start();
            string output3 = proc.StandardOutput.ReadToEnd();
            string error3 = proc.StandardError.ReadToEnd();
            if (output3.Length > 0)
                Console.WriteLine(output3);
            if (error3.Length > 0)
                Console.WriteLine(error3);
#endif
            Console.WriteLine("FINAL PASS DONE");
        }

        private static string uberpwnage;

        public new static string ToString()
        {
            if (uberpwnage == null)
            {
                if (fronthalf == null)
                {
                    fronthalf = "using Messages;\n\nnamespace Messages\n{\n";
                    backhalf = "\n}";
                }

                List<MsgsFile> everything = new List<MsgsFile>(msgsFiles);
                foreach (SrvsFile sf in srvFiles)
                {
                    everything.Add(sf.Request);
                    everything.Add(sf.Response);
                }
                fronthalf += "\n\tpublic enum MsgTypes\n\t{";
                fronthalf += "\n\t\tUnknown,";
                string srvs = "\n\t\tUnknown,";
                for (int i = 0; i < everything.Count; i++)
                {
                    fronthalf += "\n\t\t";
                    if (everything[i].classname == "Request" || everything[i].classname == "Response")
                    {
                        if (everything[i].classname == "Request")
                        {
                            srvs += "\n\t\t" + everything[i].Name.Replace(".", "__") + ",";
                        }
                        everything[i].Name += "." + everything[i].classname;
                    }
                    fronthalf += everything[i].Name.Replace(".", "__");
                    if (i < everything.Count - 1)
                        fronthalf += ",";
                }
                fronthalf += "\n\t}\n";
                srvs = srvs.TrimEnd(',');
                fronthalf += "\n\tpublic enum SrvTypes\n\t{";
                fronthalf += srvs + "\n\t}\n";
                uberpwnage = fronthalf + backhalf;
            }
            return uberpwnage;
        }

        public static void GenDict(string dictname, string keytype, string valuetype, ref string appendto, int start, int end,
            Func<int, string> genKey)
        {
            GenDict(dictname, keytype, valuetype, ref appendto, start, end, genKey, null, null);
        }

        public static void GenDict(string dictname, string keytype, string valuetype, ref string appendto, int start, int end,
            Func<int, string> genKey, Func<int, string> genVal)
        {
            GenDict(dictname, keytype, valuetype, ref appendto, start, end, genKey, genVal, null);
        }


        public static void GenDict
            (string dictname, string keytype, string valuetype, ref string appendto, int start, int end,
                Func<int, string> genKey, Func<int, string> genVal, string DEFAULT)
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