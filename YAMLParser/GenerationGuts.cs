#region USINGZ

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using String = Messages.std_msgs.String;
using System.Threading;

#endregion

namespace Messages
{
    public class SrvsFile
    {
        private string GUTS;
        public string GeneratedDictHelper;
        private bool HasHeader;
        public string Name;
        public string Namespace = "Messages";
        public List<SingleType> Stuff = new List<SingleType>();
        public string backhalf;
        public string classname;
        private List<string> def = new List<string>();
        public string dimensions = "";
        public string fronthalf;
        private string memoizedcontent;
        private bool meta;
        public MsgsFile Request, Response;
        public SrvsFile(string filename)
        {
            string[] lines = File.ReadAllLines(filename);
            string[] sp = filename.Replace(YAMLParser.Program.inputdir, "").Replace(".srv", "").Split('\\');
            classname = sp[sp.Length - 1];
            Namespace += "." + filename.Replace(YAMLParser.Program.inputdir, "").Replace(".srv", "");
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
            def = new List<string>(lines);
            int mid = 0;
            bool found = false;
            List<string> request = new List<string>(), response = new List<string>();
            for (; mid < lines.Length; mid++)
            {
                if (lines[mid].Contains("---"))
                {
                    found = true;
                    continue;
                }
                if (found)
                    response.Add(lines[mid]);
                else
                    request.Add(lines[mid]);
            }
            Request = new MsgsFile(filename, true, request, "\t");
            Response = new MsgsFile(filename, false, response, "\t");
        }

        public void Write(string outdir)
        {
            string[] chunks = Name.Split('.');
            for (int i = 0; i < chunks.Length - 1; i++)
                outdir += "\\" + chunks[i];
            if (!Directory.Exists(outdir))
                Directory.CreateDirectory(outdir);
            string localcn = classname;
            localcn = classname.Replace("Request", "").Replace("Response", "");
            File.WriteAllText(outdir + "\\" + localcn + ".cs", ToString());
            Thread.Sleep(10);
        }

        public override string ToString()
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
                            "\nusing Messages.std_msgs;\nusing Messages.geometry_msgs;\nusing Messages.nav_msgs;\nusing String=Messages.std_msgs.String;\n\n"; //\nusing Messages.roscsharp;
                        fronthalf += "namespace " + Namespace + "\n";
                        continue;
                    }
                    if (!hitvariablehole)
                        fronthalf += lines[i] + "\n";
                    else
                        backhalf += lines[i] + "\n";
                }
            }

            GUTS = fronthalf + "\n\t\tpublic class " + classname + "\n\t\t{" + Request.GetSrvHalf() + Response.GetSrvHalf() + "\t\t}" + "\n" +
                   backhalf;
            return GUTS;
        }
    }

    public class MsgsFile
    {
        private string GUTS;
        public string GeneratedDictHelper;
        private bool HasHeader;
        public string Name;
        public string Namespace = "Messages";
        public List<SingleType> Stuff = new List<SingleType>();
        public string backhalf;
        public string classname;
        private List<string> def = new List<string>();
        public string dimensions = "";
        public string fronthalf;
        private string memoizedcontent;
        private bool meta;
        public ServiceMessageType serviceMessageType = ServiceMessageType.Not;
        public static Dictionary<string, string> resolver;
        public MsgsFile(string filename, bool isrequest, List<string> lines)
        :this(filename, isrequest,lines,  "")
        {}

        public MsgsFile(string filename, bool isrequest, List<string> lines, string extraindent)
        {
            if (resolver == null) resolver = new Dictionary<string, string>();
            serviceMessageType = isrequest ? ServiceMessageType.Request : ServiceMessageType.Response;
            filename = filename.Replace(".srv", ".msg");
            if (!filename.Contains(".msg"))
                throw new Exception("" + filename + " IS NOT A VALID SRV FILE!");
            string[] sp = filename.Replace(YAMLParser.Program.inputdir, "").Replace(".msg", "").Split('\\');
            classname = sp[sp.Length - 1];
            Namespace += "." + filename.Replace(YAMLParser.Program.inputdir, "").Replace(".msg", "");
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
            classname += (isrequest ? "Request" : "Response");
            Namespace = Namespace.Trim('.');
            def = new List<string>();
            for (int i = 0; i < lines.Count; i++)
            {
                def.Add(lines[i]);
                if (Name.ToLower() == "string")
                    lines[i].Replace("String", "string");
                SingleType test = KnownStuff.WhatItIs(lines[i], extraindent);
                if (test != null)
                    Stuff.Add(test);
            }
        }

        public MsgsFile(string filename)
        :this(filename,"")
        {
        }
        
        public MsgsFile(string filename, string extraindent )
        {
            if (resolver == null) resolver = new Dictionary<string, string>();
            if (!filename.Contains(".msg"))
                throw new Exception("" + filename + " IS NOT A VALID MSG FILE!");
            string[] sp = filename.Replace(YAMLParser.Program.inputdir, "").Replace(".msg", "").Split('\\');
            classname = sp[sp.Length - 1];
            Namespace += "." + filename.Replace(YAMLParser.Program.inputdir, "").Replace(".msg", "");
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
            if (!resolver.Keys.Contains(classname) && Namespace != "Messages.std_msgs")
                resolver.Add(classname, Namespace+"."+classname);
            List<string> lines = new List<string>(File.ReadAllLines(filename));
            lines = lines.Where((st) => (!st.Contains('#') || st.Split('#')[0].Length != 0)).ToList();
            for (int i = 0; i < lines.Count; i++)
                lines[i] = lines[i].Split('#')[0].Trim();
            //lines = lines.Where((st) => (st.Length > 0)).ToList();

            lines.ForEach(new Action<string>((s) =>
            {
                if (s.Contains('#') && s.Split('#')[0].Length != 0)
                    s = s.Split('#')[0];
                if (s.Contains('#'))
                    s = "";
            }));
            lines = lines.Where((st) => (st.Length > 0)).ToList();


            def = new List<string>();
            for (int i = 0; i < lines.Count; i++)
            {
                def.Add(lines[i]);
                if (Name.ToLower() == "string")
                    lines[i].Replace("String", "string");
                SingleType test = KnownStuff.WhatItIs(lines[i], extraindent);
                if (test != null)
                    Stuff.Add(test);
            }
        }

        public string GetSrvHalf()
        {
            string wholename = classname.Replace("Request", ".Request").Replace("Response", ".Response"); ;
            classname = classname.Contains("Request") ? "Request" : "Response";
            if (memoizedcontent == null)
            {
                memoizedcontent = "";
                for (int i = 0; i < Stuff.Count; i++)
                { 
                    SingleType thisthing = Stuff[i];
                    if (thisthing.Type == "Header")
                    {
                        HasHeader = true;
                    }
                    else if (classname == "String")
                    {
                        thisthing.input = thisthing.input.Replace("String", "string");
                        thisthing.Type = thisthing.Type.Replace("String", "string");
                        thisthing.output = thisthing.output.Replace("String", "string");
                    }
                    else if (classname == "Time")
                    {
                        thisthing.input = thisthing.input.Replace("Time", "TimeData");
                        thisthing.Type = thisthing.Type.Replace("Time", "TimeData");
                        thisthing.output = thisthing.output.Replace("Time", "TimeData");
                    }
                    else if (classname == "Duration")
                    {
                        thisthing.input = thisthing.input.Replace("Duration", "TimeData");
                        thisthing.Type = thisthing.Type.Replace("Duration", "TimeData");
                        thisthing.output = thisthing.output.Replace("Duration", "TimeData");
                    }
                    meta |= thisthing.meta;
                    memoizedcontent += "\t" + thisthing.output + "\n";
                }
                if (classname.ToLower() == "string")
                {
                    memoizedcontent +=
                        "\n\n\t\t\t\t\tpublic String(string s){ data = s; }\n\t\t\t\t\tpublic String(){ data = \"\"; }\n\n";
                }
                else if (classname == "Time")
                {
                    memoizedcontent +=
                        "\n\n\t\t\t\t\tpublic Time(uint s, uint ns) : this(new TimeData{ sec=s, nsec = ns}){}\n\t\t\t\t\tpublic Time(TimeData s){ data = s; }\n\t\t\t\t\tpublic Time() : this(0,0){}\n\n";
                }
                else if (classname == "Duration")
                {
                    memoizedcontent +=
                        "\n\n\t\t\t\t\tpublic Duration(uint s, uint ns) : this(new TimeData{ sec=s, nsec = ns}){}\n\t\t\t\t\tpublic Duration(TimeData s){ data = s; }\n\t\t\t\t\tpublic Duration() : this(0,0){}\n\n";
                }
                while (memoizedcontent.Contains("DataData"))
                    memoizedcontent = memoizedcontent.Replace("DataData", "Data");
            }
            string ns = Namespace.Replace("Messages.", "");
            if (ns == "Messages")
                ns = "";
            GeneratedDictHelper = "";
            foreach (SingleType S in Stuff)
                GeneratedDictHelper += MessageFieldHelper.Generate(S);
            GUTS = fronthalf + "\n\t\t\tpublic class " + classname + "\n\t\t\t{\n" + memoizedcontent + "\t\t\t}" + "\n" +
                   backhalf;
            return GUTS;
        }

        public override string ToString()
        {
            bool wasnull = false;
            if (fronthalf == null)
            {
                wasnull = true;
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
                            "using Messages.std_msgs;using String=Messages.std_msgs.String;\n\n"; //\nusing Messages.roscsharp;
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
                for (int i = 0; i < Stuff.Count; i++)
                {
                    SingleType thisthing = Stuff[i];
                    if (thisthing.Type == "Header")
                    {
                        HasHeader = true;
                    }
                    else if (classname == "String")
                    {
                        thisthing.input = thisthing.input.Replace("String", "string");
                        thisthing.Type = thisthing.Type.Replace("String", "string");
                        thisthing.output = thisthing.output.Replace("String", "string");
                    }
                    else if (classname == "Time")
                    {
                        thisthing.input = thisthing.input.Replace("Time", "TimeData");
                        thisthing.Type = thisthing.Type.Replace("Time", "TimeData");
                        thisthing.output = thisthing.output.Replace("Time", "TimeData");
                    }
                    else if (classname == "Duration")
                    {
                        thisthing.input = thisthing.input.Replace("Duration", "TimeData");
                        thisthing.Type = thisthing.Type.Replace("Duration", "TimeData");
                        thisthing.output = thisthing.output.Replace("Duration", "TimeData");
                    }
                    meta |= thisthing.meta;
                    memoizedcontent += "\t" + thisthing.output + "\n";
                }
                string ns = Namespace.Replace("Messages.", "");
                if (ns == "Messages")
                    ns = "";
                while (memoizedcontent.Contains("DataData"))
                    memoizedcontent = memoizedcontent.Replace("DataData", "Data");
                //if (GeneratedDictHelper == null)
                //    GeneratedDictHelper = TypeInfo.Generate(classname, ns, HasHeader, meta, def, Stuff);
                GeneratedDictHelper = "\n\t\t\t\t";
                string GeneratedDeserializationCode = "";
                bool literal = false;
                StringBuilder DEF = new StringBuilder();
                foreach (string s in def)
                    DEF.AppendLine(s);

                if (!meta && Stuff.Count == 1 && Stuff[0].Name == "data")
                {
                    Console.WriteLine(DEF);
                }

                for (int i = 0; i < Stuff.Count; i++)
                    GeneratedDeserializationCode += GenerateDeserializationCode(Stuff[i]);
            }
                for (int i = 0; i < Stuff.Count; i++)
                {
                    if (resolver.ContainsKey(Stuff[i].Type))
                        Stuff[i].refinalize(resolver[Stuff[i].Type]);
                    GeneratedDictHelper += ((i > 0) ? "}, \n\t\t\t\t{" : "") + MessageFieldHelper.Generate(Stuff[i]);
                }
                GUTS = (serviceMessageType != ServiceMessageType.Response ? fronthalf : "") + "\n" + memoizedcontent + "\n" +
                       (serviceMessageType != ServiceMessageType.Request ? backhalf : "");
                if (classname.ToLower() == "string")
                {
                    GUTS = GUTS.Replace("$NULLCONSTBODY", "if (data == null)\n\t\t\tdata = \"\";\n");
                    GUTS = GUTS.Replace("$EXTRACONSTRUCTOR", "\n\t\tpublic $WHATAMI(string d) : base($MYMSGTYPE, $MYMESSAGEDEFINITION, $MYHASHEADER, $MYISMETA, new Dictionary<string, MsgFieldInfo>$MYFIELDS)\n\t\t{\n\t\t\tdata = d;\n\t\t}\n");
                }
                else if (classname == "Time" || classname == "Duration")
                {
                    GUTS = GUTS.Replace("$EXTRACONSTRUCTOR", "\n\t\tpublic $WHATAMI(TimeData d) : base($MYMSGTYPE, $MYMESSAGEDEFINITION, $MYHASHEADER, $MYISMETA, new Dictionary<string, MsgFieldInfo>$MYFIELDS)\n\t\t{\n\t\t\tdata = d;\n\t\t}\n");
                }
                GUTS = GUTS.Replace("$WHATAMI", classname);
                GUTS = GUTS.Replace("$MYISMETA", meta.ToString().ToLower());
                GUTS = GUTS.Replace("$MYMSGTYPE", "MsgTypes." + Namespace.Replace("Messages.", "") + "__" + classname);
                for (int i = 0; i < def.Count; i++)
                {
                    while (def[i].Contains("\t"))
                        def[i] = def[i].Replace("\t", " ");
                    while (def[i].Contains("\n\n"))
                        def[i] = def[i].Replace("\n\n", "\n");
                    def[i] = def[i].Replace('\t', ' ');
                    while (def[i].Contains("  "))
                        def[i] = def[i].Replace("  ", " ");
                    def[i] = def[i].Replace(" = ", "=");
                }
                GUTS = GUTS.Replace("$MYMESSAGEDEFINITION", "@\"" + def.Aggregate("", (current, d) => current + (d + "\n")).Trim('\n') + "\"");
                GUTS = GUTS.Replace("$MYHASHEADER", HasHeader.ToString().ToLower());
                GUTS = GUTS.Replace("$MYFIELDS", GeneratedDictHelper.Length > 5 ? "{{" + GeneratedDictHelper + "}}" : "()");
                GUTS = GUTS.Replace("$NULLCONSTBODY", "");
                GUTS = GUTS.Replace("$EXTRACONSTRUCTOR", "");
            
            return GUTS;
        }

        const string stfmat = "name: {0}\n\ttype: {1}\n\trostype: {2}\n\tisliteral: {3}\n\tisconst: {4}\n\tconstvalue: {5}\n\tisarray: {6}\n\tlength: {7}\n\tismeta: {8}\n";
        public string GenerateDeserializationCode(SingleType st)
        {
            //Console.WriteLine(stfmat, st.Name, st.Type, st.rostype, st.IsLiteral, st.Const, st.ConstValue, st.IsArray, st.length, st.meta);
            //if (!st.IsLiteral)
            //    Console.WriteLine(st.Name);
            return "";
        }

        public void Write(string outdir)
        {
            string[] chunks = Name.Split('.');
            for (int i = 0; i < chunks.Length - 1; i++)
                outdir += "\\" + chunks[i];
            if (!Directory.Exists(outdir))
                Directory.CreateDirectory(outdir);
            string localcn = classname;
            if (serviceMessageType != ServiceMessageType.Not)
                localcn = classname.Replace("Request", "").Replace("Response", "");
            if (serviceMessageType == ServiceMessageType.Response)
                File.AppendAllText(outdir + "\\" + localcn + ".cs", ToString());
            else
                File.WriteAllText(outdir + "\\" + localcn + ".cs", ToString());
            Thread.Sleep(10);
        }
    }

    public static class KnownStuff
    {
        public static Dictionary<string, string> KnownTypes = new Dictionary<string, string>
                                                                  {
                                                                      {"float64", "double"},
                                                                      {"float32", "Single"},
                                                                      {"uint64", "ulong"},
                                                                      {"uint32", "uint"},
                                                                      {"uint16", "ushort"},
                                                                      {"uint8", "byte"},
                                                                      {"int64", "long"},
                                                                      {"int32", "int"},
                                                                      {"int16", "short"},
                                                                      {"int8", "sbyte"},
                                                                      {"byte", "byte"},
                                                                      {"bool", "bool"},
                                                                      {"char", "char"},
                                                                      {"time", "Time"},
                                                                      {"string", "String"},
                                                                      {"duration", "Duration"}
                                                                  };

        public static SingleType WhatItIs(string s, string extraindent)
        {
            string[] pieces = s.Split('/');
            if (pieces.Length > 1)
            {
                s = pieces[pieces.Length - 1];
            }
            return WhatItIs(new SingleType(s, extraindent));
        }

        public static SingleType WhatItIs(SingleType t)
        {
            foreach (KeyValuePair<string, string> test in KnownTypes)
            {
                if (t.Test(test))
                {
                    t.rostype = t.Type;
                    return t.Finalize(test);
                }
            }
            return t.Finalize(t.input.Split(' '), false);
        }
    }

    public class SingleType
    {
        public bool Const;
        public string ConstValue = "";
        public bool IsArray;
        public bool IsLiteral;
        public string Name;
        public string Type;
        private string[] backup;
        public string input;
        public string length = "";
        public bool meta;
        public string output;
        public string rostype = "";
        public string lowestindent = "\t\t";

        public SingleType(string s)
        :this( s,  "")
        {}

        public SingleType(string s, string extraindent)
        {
            lowestindent += extraindent;
            if (s.Contains('[') && s.Contains(']'))
            {
                string front = "";
                string back = "";
                string[] parts = s.Split('[');
                front = parts[0];
                parts = parts[1].Split(']');
                length = parts[0];
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
            rostype = PARTS[0];
            if (!KnownStuff.KnownTypes.ContainsKey(rostype))
                meta = true;
            PARTS[0] = csharptype.Value;
            return Finalize(PARTS, true);
        }

        public SingleType Finalize(string[] s, bool isliteral)
        {
            backup = new string[s.Length];
            Array.Copy(s, backup, s.Length);
            bool isconst = false;
            IsLiteral = isliteral;
            string type = s[0];
            string name = s[1];
            string otherstuff = "";
            if (name.Contains('='))
            {
                string[] parts = name.Split('=');
                isconst = true;
                name = parts[0];
                otherstuff = " = " + parts[1];
            }
            for (int i = 2; i < s.Length; i++)
                otherstuff += " " + s[i];
            if (otherstuff.Contains('=')) isconst = true;
            if (!IsArray)
            {
                if (otherstuff.Contains('=') && type == "string")
                {
                    otherstuff = otherstuff.Replace("\\", "\\\\");
                    otherstuff = otherstuff.Replace("\"", "\\\"");
                    string[] split = otherstuff.Split('=');
                    otherstuff = split[0] + " = \"" + split[1] + "\"";
                }
                if (otherstuff.Contains('=') && type == "bool")
                {
                    otherstuff = otherstuff.Replace("0", "false").Replace("1", "true");
                }
                if (otherstuff.Contains('=') && type == "byte")
                {
                    otherstuff = otherstuff.Replace("-1", "255");
                }
                if (MsgsFile.resolver.Keys.Contains(type))
                {
                    type = MsgsFile.resolver[type];
                }
                output = lowestindent + "public " + (isconst ? "const " : "") + type + " " + name + otherstuff + ";";
                Const = isconst;
                if (otherstuff.Contains("="))
                {
                    string[] chunks = otherstuff.Split('=');
                    ConstValue = chunks[chunks.Length - 1].Trim();
                    ;
                }
            }
            else
            {
                if (length.Length > 0)
                {
                    IsLiteral = type != "string";
                    output = lowestindent + "public " + type + "[] " + name + " = new " + type + "[" + length + "];";
                }
                else
                    output = lowestindent + "public " + "" + type + "[] " + name + otherstuff + ";";
                if (otherstuff.Contains('='))
                {
                    string[] split = otherstuff.Split('=');
                    otherstuff = split[0] + " = (" + type + ")" + split[1];
                }
            }
            Type = type;
            if (!KnownStuff.KnownTypes.ContainsKey(rostype))
                meta = true;
            Name = name.Length == 0 ? otherstuff.Trim() : name;
            return this;
        }

        public void refinalize(string REALTYPE)
        {
            bool isconst = false;
            string type = REALTYPE;
            string name = backup[1];
            string otherstuff = "";
            if (name.Contains('='))
            {
                string[] parts = name.Split('=');
                isconst = true;
                name = parts[0];
                otherstuff = " = " + parts[1];
            }
            for (int i = 2; i < backup.Length; i++)
                otherstuff += " " + backup[i];
            if (otherstuff.Contains('=')) isconst = true;
            if (!IsArray)
            {
                if (otherstuff.Contains('=') && type == "string")
                {
                    otherstuff = otherstuff.Replace("\\", "\\\\");
                    otherstuff = otherstuff.Replace("\"", "\\\"");
                    string[] split = otherstuff.Split('=');
                    otherstuff = split[0] + " = \"" + split[1] + "\"";
                }
                if (otherstuff.Contains('=') && type == "bool")
                {
                    otherstuff = otherstuff.Replace("0", "false").Replace("1", "true");
                }
                if (otherstuff.Contains('=') && type == "byte")
                {
                    otherstuff = otherstuff.Replace("-1", "255");
                }
                if (MsgsFile.resolver.Keys.Contains(type))
                {
                    type = MsgsFile.resolver[type];
                }
                output = lowestindent + "public " + (isconst ? "const " : "") + type + " " + name + otherstuff + ";";
                Const = isconst;
                if (otherstuff.Contains("="))
                {
                    string[] chunks = otherstuff.Split('=');
                    ConstValue = chunks[chunks.Length - 1].Trim();
                }
            }
            else
            {
                if (length.Length != 0)
                {
                    IsLiteral = type != "string";
                    output = lowestindent + "public " + type + "[" + length + "] " + name + " = new " + type + "[" + length + "];";
                }
                else
                    output = lowestindent + "public " + "" + type + "[] " + name + otherstuff + ";";
                if (otherstuff.Contains('='))
                {
                    string[] split = otherstuff.Split('=');
                    otherstuff = split[0] + " = (" + type + ")" + split[1];
                }
            }
            Type = type;
            if (!KnownStuff.KnownTypes.ContainsKey(rostype))
                meta = true;
            Name = name.Length == 0 ? otherstuff.Trim() : name;
        }
    }

    public static class MessageFieldHelper
    {
        public static string Generate(SingleType members)
        {
            return System.String.Format
                ("\"{0}\", new MsgFieldInfo(\"{0}\", {1}, {2}, {3}, \"{4}\", {5}, \"{6}\", {7})",
                 members.Name,
                 members.IsLiteral.ToString().ToLower(),
                 ("typeof(" + members.Type + ")"),
                 members.Const.ToString().ToLower(),
                 members.ConstValue,
                 members.IsArray.ToString().ToLower(),
                 members.length,
                 //FIX MEEEEEEEE
                 members.meta.ToString().ToLower());
        }
    }
}
