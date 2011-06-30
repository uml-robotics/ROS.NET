#region USINGZ

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

#endregion

namespace YAMLParser
{
    internal class Program
    {
        public static string outputdir
        {
            get { return "YAMLProjectDir"; }
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
            List<MsgsFile> msgsFiles = new List<MsgsFile>();
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
    }

    public class MsgsFile
    {
        private bool HasHeader;
        private bool KnownSize = true;
        public string Name;
        public string Namespace = "Messages";
        public Queue<SingleType> Stuff = new Queue<SingleType>();
        public string backhalf;
        public string classname;
        public string fronthalf;
        private string memoizedcontent;

        public MsgsFile(string filename)
        {
            if (!filename.Contains(".msg"))
                throw new Exception("" + filename + " IS NOT A VALID MSG FILE!");
            string[] sp = filename.Replace("ROS_MESSAGES", "").Replace(".msg", "").Split('\\');
            classname = sp[sp.Length - 1];
            Namespace += "." + filename.Replace("ROS_MESSAGES", "").Replace(".msg", "");
            Namespace = Namespace.Replace("\\", ".").Replace("..", ".");
            string[] sp2 = Namespace.Split('.');
            Namespace = "";
            for (int i = 0; i < sp2.Length - 2; i++)
                Namespace += sp2[i] + ".";
            Namespace += sp2[sp2.Length - 2];
            //THIS IS BAD!
            classname = classname.Replace("/", ".");
            Name = Namespace.Replace("Messages", "").TrimStart('.') + "." + classname;
            Name = Name.TrimStart('.');
            classname = Name.Split('.').Length > 1 ? Name.Split('.')[1] : Name;
            Namespace = Namespace.Trim('.');
            List<string> lines = new List<string>(File.ReadAllLines(filename));
            lines = lines.Where((st) => (!st.Contains('#') || st.Split('#')[0].Length != 0)).ToList();
            for (int i = 0; i < lines.Count; i++)
                lines[i] = lines[i].Split('#')[0].Trim();
            lines = lines.Where((st) => (st.Length > 0)).ToList();
            for (int i = 0; i < lines.Count; i++)
            {
                SingleType test = KnownStuff.WhatItIs(lines[i]);
                if (test != null)
                    Stuff.Enqueue(test);
            }
        }

        public override string ToString()
        {
            bool wasnull = false;
            if (fronthalf == null)
            {
                wasnull = true;
                fronthalf = "";
                backhalf = "";
                    //"\t\tpublic byte[] Serialize()\n\t\t{\n\t\t\treturn SerializationHelper.Serialize<Data>(data);\n\t}\n}\n";
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
                        fronthalf += "using Messages.geometry_msgs;\nusing Messages.nav_msgs;\n\n";
                        fronthalf += "namespace " + Namespace + "\n";
                        continue;
                    }
                    if (!hitvariablehole)
                        fronthalf += lines[i] + "\n";
                    else
                        backhalf += lines[i] + "\n";
                }
            }

            if (memoizedcontent == null)
            {
                memoizedcontent = "";
                while (Stuff.Count > 0)
                {
                    SingleType thisthing = Stuff.Dequeue();
                    if (thisthing.Type == "Header") HasHeader = true;
                    if (!thisthing.KnownSize) KnownSize = false;
                    memoizedcontent += "\t" + thisthing.output + "\n";
                }
            }
            if (wasnull)
            {
                fronthalf += "\tpublic class " + classname + " : IRosMessage\n\t{\n\t\tpublic Data data;\n\n\t\tpublic " + classname + "(" + classname + ".Data d)\n\t\t{\n" + (HasHeader
                        ? "\t\t\tHasHeader = true;\n"
                        : "\t\t\tHasHeader = false;\n") +
                   (KnownSize
                        ? "\t\t\tKnownSize = true;\n"
                        : "\t\t\tKnownSize = false;\n") + "\t\t\tdata = d;\n\t\t}\n" +
                             "\n\t\tpublic " + classname +
                             "(byte[] SERIALIZEDSTUFF)\n\t\t\t : base(SERIALIZEDSTUFF)\n\t\t{\n"+(HasHeader
                        ? "\t\t\tHasHeader = true;\n"
                        : "\t\t\tHasHeader = false;\n") +
                   (KnownSize
                        ? "\t\t\tKnownSize = true;\n"
                        : "\t\t\tKnownSize = false;\n") +"\t\t}\n";//\t{\n\t\t\tdata = SerializationHelper.Deserialize<Data>(SERIALIZEDSTUFF);\n\t\t}\n";
            }
            string ret = fronthalf +
                   /*(objects.Length > 0
                ? "\t\tpublic object[] Data\n\t\t{\n\t\t\tget { return new[] {" + objects + "}; }\n\t\t}\n" : 
                "\t\tpublic object[] Data\n\t\t{\n\t\t\tget { return new object[0]; }\n\t\t}\n")+*/
                   /*(types.Length>0 ? ("\n\t\tpublic Type[] dataTypes = new []{"+types+"\t\t};\n") : "") +*/
                   "\n\t\t[StructLayout(LayoutKind.Sequential, Pack = 1)]\n\t\tpublic struct Data\n\t\t{\n" +
                   memoizedcontent + "\t\t}\n\t}\n" +
                   backhalf;
            return ret;
        }

        public void Write()
        {
            string outdir = Program.outputdir;
            string[] chunks = Name.Split('.');
            for (int i = 0; i < chunks.Length - 1; i++)
                outdir += "\\" + chunks[i];
            if (!Directory.Exists(outdir))
                Directory.CreateDirectory(outdir);
            File.WriteAllText(outdir + "\\" + classname + ".cs", ToString());
        }
    }

    public static class KnownStuff
    {
        public static Dictionary<string, string> KnownTypes = new Dictionary<string, string>
                                                                  {
                                                                      {"float64", "double"},
                                                                      {"float32", "double"},
                                                                      {"uint64", "uint"},
                                                                      {"uint32", "uint"},
                                                                      {"uint16", "uint"},
                                                                      {"uint8", "byte"},
                                                                      {"int64", "int"},
                                                                      {"int32", "int"},
                                                                      {"int16", "int"},
                                                                      {"int8", "byte"},
                                                                      {"byte", "byte"},
                                                                      {"bool", "bool"},
                                                                      {"string", "string"},
                                                                      {"time", "DateTime"},
                                                                      {"duration", "TimeSpan"},
                                                                      {"char", "char"}
                                                                  };

        public static SingleType WhatItIs(string s)
        {
            string[] pieces = s.Split('/');
            if (pieces.Length > 1)
            {
                s = pieces[pieces.Length - 1];
            }
            return WhatItIs(new SingleType(s));
        }

        public static SingleType WhatItIs(SingleType t)
        {
            foreach (KeyValuePair<string, string> test in KnownTypes)
            {
                if (t.Test(test))
                    return t.Finalize(test);
            }
            return t.Finalize(t.input.Split(' '), false);
        }
    }

    public class SingleType
    {
        public bool IsArray;
        public bool KnownSize;
        public string Name;
        public string Type;
        public string input;
        public string lengths = "";
        public string output;

        public SingleType(string s)
        {
            if (s.Contains('[') && s.Contains(']'))
            {
                string front = "";
                string back = "";
                string[] parts = s.Split('[');
                front = parts[0];
                parts = parts[1].Split(']');
                lengths = parts[0];
                back = parts[1];
                IsArray = true;
                s = front + back;
            }
            input = s;
        }

        public bool Test(KeyValuePair<string, string> candidate)
        {
            return (input.Split(' ')[0].ToLower().Equals(candidate.Key));
        }

        public SingleType Finalize(KeyValuePair<string, string> csharptype)
        {
            string[] PARTS = input.Split(' ');
            PARTS[0] = csharptype.Value;
            return Finalize(PARTS, true);
        }

        public SingleType Finalize(string[] s, bool knownsize)
        {
            bool isconst = false;
            KnownSize = knownsize;
            string type = s[0];
            string name = s[1];
            string othershit = "";
            if (name.Contains('='))
            {
                string[] parts = name.Split('=');
                isconst = true;
                name = parts[0];
                othershit = " = " + parts[1];
            }
            for (int i = 2; i < s.Length; i++)
                othershit += " " + s[i];
            if (othershit.Contains('=')) isconst = true;
            if (!IsArray)
            {
                if (othershit.Contains('=') && type == "string")
                {
                    othershit = othershit.Replace("\\", "\\\\");
                    othershit = othershit.Replace("\"", "\\\"");
                    string[] split = othershit.Split('=');
                    othershit = split[0] + " = \"" + split[1] + "\"";
                }
                if (othershit.Contains('=') && type == "bool")
                {
                    othershit = othershit.Replace("0", "false").Replace("1", "true");
                }
                if (othershit.Contains('=') && type == "byte")
                {
                    othershit = othershit.Replace("-1", "255");
                }
                /*if (othershit.Contains('='))
                {
                    string[] split = othershit.Split('=');
                    othershit = split[0] + " = (" + type + ")" + split[1];
                }*/
                output = "\t\tpublic " +(isconst?"const ":"")+ type + (KnownSize ? "" : ".Data") + " " + name + othershit + ";";
            }
            else
            {
                if (lengths.Length > 0)
                {
                    KnownSize = true;
                    string commas = "";
                    for (int i = 0; i < lengths.Count((c) => c == ','); i++) commas += ",";
                    output = "\t\tpublic " + type + "[" + commas + "] " + name + " = new " + type + "[" + lengths + "];";
                }
                if (othershit.Contains('='))
                {
                    string[] split = othershit.Split('=');
                    othershit = split[0] + " = (" + type + ")" + split[1];
                }
                output = "\t\tpublic " + type + (KnownSize ? "" : ".Data") + "[] " + name + othershit + ";";
            }
            Type = type;
            if (name.Length == 0)
                Name = othershit.Trim();
            else
                Name = name;
            return this;
        }
    }
}