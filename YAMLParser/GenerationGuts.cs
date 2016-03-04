// File: GenerationGuts.cs
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
using System.Linq;
using System.Text;
using System.Threading;
using YAMLParser;

#endregion

namespace FauxMessages
{
    public class SrvsFile
    {
        private string GUTS;
        public string GeneratedDictHelper;
        public bool HasHeader;
        public string Name;
        public string Namespace = "Messages";
        public MsgsFile Request, Response;
        public List<SingleType> Stuff = new List<SingleType>();
        public string backhalf;
        public string classname;
        private List<string> def = new List<string>();
        public string dimensions = "";
        public string fronthalf;
        private string memoizedcontent;
        private bool meta;
        public string requestbackhalf;
        public string requestfronthalf;
        public string responsebackhalf;
        public string resposonebackhalf;

        public SrvsFile(MsgFileLocation filename)
        {
            //read in srv file
            string[] lines = File.ReadAllLines(filename.Path);
            classname = filename.basename;
            Namespace += "." + filename.package;
            Name = filename.package + "." + filename.basename;

            //def is the list of all lines in the file
            def = new List<string>();
            int mid = 0;
            bool found = false;
            List<string> request = new List<string>(), response = new List<string>();
            //Search through for the "---" separator between request and response
            for (; mid < lines.Length; mid++)
            {
                lines[mid] = lines[mid].Replace("\"", "\\\"");
                if (lines[mid].Contains('#'))
                {
                    lines[mid] = lines[mid].Substring(0, lines[mid].IndexOf('#'));
                }
                lines[mid] = lines[mid].Trim();
                if (lines[mid].Length == 0)
                {
                    continue;
                }
                def.Add(lines[mid]);
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
            //treat request and response like 2 message files, each with a partial definition and extra stuff tagged on to the classname
            Request = new MsgsFile(new MsgFileLocation(filename.Path.Replace(".srv",".msg"), filename.searchroot), true, request, "\t");
            Response = new MsgsFile(new MsgFileLocation(filename.Path.Replace(".srv", ".msg"), filename.searchroot), false, response, "\t");
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
            File.WriteAllText(outdir + "\\" + localcn + ".cs", ToString().Replace("FauxMessages", "Messages"));
            Thread.Sleep(10);
        }

        public override string ToString()
        {
            if (requestfronthalf == null)
            {
                requestfronthalf = "";
                requestbackhalf = "";
                string[] lines = File.ReadAllLines("TemplateProject\\SrvPlaceHolder._cs");
                int section = 0;
                for (int i = 0; i < lines.Length; i++)
                {
                    //read until you find public class request... do everything once.
                    //then, do it again response
                    if (lines[i].Contains("$$REQUESTDOLLADOLLABILLS"))
                    {
                        section++;
                        continue;
                    }
                    if (lines[i].Contains("namespace"))
                    {
                        requestfronthalf +=
                          "\nusing Messages.std_msgs;\nusing String=System.String;\nusing Messages.geometry_msgs;\nusing Messages.nav_msgs;\n\n"; //\nusing Messages.roscsharp;
                        requestfronthalf += "namespace " + Namespace + "\n";
                        continue;
                    }
                    if (lines[i].Contains("$$RESPONSEDOLLADOLLABILLS"))
                    {
                        section++;
                        continue;
                    }
                    switch (section)
                    {
                        case 0:
                            requestfronthalf += lines[i] + "\n";
                            break;
                        case 1:
                            requestbackhalf += lines[i] + "\n";
                            break;
                        case 2:
                            responsebackhalf += lines[i] + "\n";
                            break;
                    }
                }
            }

            GUTS = requestfronthalf + Request.GetSrvHalf() + requestbackhalf + Response.GetSrvHalf() + "\n" +
                   responsebackhalf;
            /***********************************/
            /*       CODE BLOCK DUMP           */
            /***********************************/

            #region definitions

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
                def[i] = def[i].Replace("\"", "\"\"");
            }
            StringBuilder md = new StringBuilder();
            StringBuilder reqd = new StringBuilder();
            StringBuilder resd = null;
            foreach (string s in def)
            {
                if (s == "---")
                {
                    //only put this string in md, because the subclass defs don't contain it
                    md.AppendLine(s);

                    //we've hit the middle... move from the request to the response by making responsedefinition not null.
                    resd = new StringBuilder();
                    continue;
                }

                //add every line to MessageDefinition for whole service
                md.AppendLine(s);

                //before we hit ---, add lines to request Definition. Otherwise, add them to response.
                if (resd == null)
                    reqd.AppendLine(s);
                else
                    resd.AppendLine(s);
            }
            string MessageDefinition = md.ToString().Trim();
            string RequestDefinition = reqd.ToString().Trim();
            string ResponseDefinition = resd.ToString().Trim();

            #endregion

            #region THE SERVICE

            GUTS = GUTS.Replace("$WHATAMI", classname);
            GUTS = GUTS.Replace("$MYSRVTYPE", "SrvTypes." + Namespace.Replace("Messages.", "") + "__" + classname);
            GUTS = GUTS.Replace("$MYSERVICEDEFINITION", "@\"" + MessageDefinition + "\"");

            #endregion

            #region request

            string RequestDict = Request.GenFields();
            meta = Request.meta;
            GUTS = GUTS.Replace("$REQUESTMYISMETA", meta.ToString().ToLower());
            GUTS = GUTS.Replace("$REQUESTMYMSGTYPE", "MsgTypes." + Namespace.Replace("Messages.", "") + "__" + classname);
            GUTS = GUTS.Replace("$REQUESTMYMESSAGEDEFINITION", "@\"" + RequestDefinition + "\"");
            GUTS = GUTS.Replace("$REQUESTMYHASHEADER", Request.HasHeader.ToString().ToLower());
            GUTS = GUTS.Replace("$REQUESTMYFIELDS", RequestDict.Length > 5 ? "{{" + RequestDict + "}}" : "()");
            GUTS = GUTS.Replace("$REQUESTNULLCONSTBODY", "");
            GUTS = GUTS.Replace("$REQUESTEXTRACONSTRUCTOR", "");

            #endregion

            #region response

            string ResponseDict = Response.GenFields();
            GUTS = GUTS.Replace("$RESPONSEMYISMETA", Response.meta.ToString().ToLower());
            GUTS = GUTS.Replace("$RESPONSEMYMSGTYPE", "MsgTypes." + Namespace.Replace("Messages.", "") + "__" + classname);
            GUTS = GUTS.Replace("$RESPONSEMYMESSAGEDEFINITION", "@\"" + ResponseDefinition + "\"");
            GUTS = GUTS.Replace("$RESPONSEMYHASHEADER", Response.HasHeader.ToString().ToLower());
            GUTS = GUTS.Replace("$RESPONSEMYFIELDS", ResponseDict.Length > 5 ? "{{" + ResponseDict + "}}" : "()");
            GUTS = GUTS.Replace("$RESPONSENULLCONSTBODY", "");
            GUTS = GUTS.Replace("$RESPONSEEXTRACONSTRUCTOR", "");

            #endregion

            /********END BLOCK**********/
            return GUTS;
        }
    }

    public class MsgsFile
    {
        private const string stfmat = "name: {0}\n\ttype: {1}\n\trostype: {2}\n\tisliteral: {3}\n\tisconst: {4}\n\tconstvalue: {5}\n\tisarray: {6}\n\tlength: {7}\n\tismeta: {8}\n";
        public static Dictionary<string, List<string>> resolver;
        private string GUTS;
        public string GeneratedDictHelper;
        public bool HasHeader;
        public string Name;
        public string Namespace = "Messages";
        public List<SingleType> Stuff = new List<SingleType>();
        public string backhalf;
        public string classname;
        private List<string> def = new List<string>();
        public string dimensions = "";
        public string fronthalf;
        private string memoizedcontent;
        public bool meta;
        public ServiceMessageType serviceMessageType = ServiceMessageType.Not;

        public MsgsFile(MsgFileLocation filename, bool isrequest, List<string> lines)
            : this(filename, isrequest, lines, "")
        {
        }

        //specifically for SRV halves
        public
            MsgsFile(MsgFileLocation filename, bool isrequest, List<string> lines, string extraindent)
        {
            if (resolver == null) resolver = new Dictionary<string, List<string>>();
            serviceMessageType = isrequest ? ServiceMessageType.Request : ServiceMessageType.Response;
            //Parse The file name to get the classname;
            classname = filename.basename;
            //Parse for the Namespace
            Namespace += "." + filename.package;
            Name = filename.package + "." + classname;
            classname += (isrequest ? "Request" : "Response");
            Namespace = Namespace.Trim('.');
            def = new List<string>();
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Trim().Length == 0)
                {
                    continue;
                }
                def.Add(lines[i]);
                SingleType test = KnownStuff.WhatItIs(this, lines[i], extraindent);
                if (test != null)
                    Stuff.Add(test);
            }
        }

        public MsgsFile(MsgFileLocation filename)
            : this(filename, "")
        {
        }

        public MsgsFile(MsgFileLocation filename, string extraindent)
        {
            if (resolver == null) resolver = new Dictionary<string, List<string>>();
            if (!filename.Path.Contains(".msg"))
                throw new Exception("" + filename + " IS NOT A VALID MSG FILE!");
            classname = filename.basename;
            //Parse for the Namespace
            Namespace += "." + filename.package;
            Name = filename.package + "." + classname;
            Namespace = Namespace.Trim('.');
            if (!resolver.Keys.Contains(classname) && Namespace != "Messages.std_msgs")
                resolver.Add(classname, new List<string> {Namespace + "." + classname});
            else if (Namespace != "Messages.std_msgs")
                resolver[classname].Add(Namespace + "." + classname);
            List<string> lines = new List<string>(File.ReadAllLines(filename.Path));
            lines = lines.Where(st => (!st.Contains('#') || st.Split('#')[0].Length != 0)).ToList();
            for (int i = 0; i < lines.Count; i++)
                lines[i] = lines[i].Split('#')[0].Trim();
            //lines = lines.Where((st) => (st.Length > 0)).ToList();

            lines.ForEach(s =>
                              {
                                  if (s.Contains('#') && s.Split('#')[0].Length != 0)
                                      s = s.Split('#')[0];
                                  if (s.Contains('#'))
                                      s = "";
                              });
            lines = lines.Where(st => (st.Length > 0)).ToList();


            def = new List<string>();
            for (int i = 0; i < lines.Count; i++)
            {
                def.Add(lines[i]);
                SingleType test = KnownStuff.WhatItIs(this, lines[i], extraindent);
                if (test != null)
                    Stuff.Add(test);
            }
        }

        public void resolve(string package, ref string type)
        {
            if (resolver.Keys.Contains(type))
            {
                string same_pkg = null;

                if (resolver[type].Count > 1)
                {
                    for (int i = 0; i < resolver[type].Count; i++)
                    {
                        if (package.Length > 0 && resolver[type][i].Contains(package))
                        {
                            type = resolver[type][i];
                            return;
                        }
                        if (resolver[type][i].Contains(Namespace))
                        {
                            same_pkg = resolver[type][i];
                        }
                    }
                    if (same_pkg != null)
                    {
                        type = same_pkg;
                        return;
                    }
                    throw new Exception("Could not resolve " + type);
                }
                type = resolver[type][0];
            }
        }

        public string GetSrvHalf()
        {
            string wholename = classname.Replace("Request", ".Request").Replace("Response", ".Response");
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
                    /*else if (classname == "String")
                    {
                        thisthing.input = thisthing.input.Replace("String", "string");
                        thisthing.Type = thisthing.Type.Replace("String", "string");
                        thisthing.output = thisthing.output.Replace("String", "string");
                    }*/
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
                /*if (classname.ToLower() == "string")
                {
                    memoizedcontent +=
                        "\n\n\t\t\t\t\tpublic String(string s){ data = s; }\n\t\t\t\t\tpublic String(){ data = \"\"; }\n\n";
                }
                else*/ if (classname == "Time")
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
            GUTS = fronthalf + memoizedcontent + "\n" +
                   backhalf;
            return GUTS;
        }

        public string GenFields()
        {
            string ret = "\n\t\t\t\t";
            for (int i = 0; i < Stuff.Count; i++)
            {
                Stuff[i].refinalize(this, Stuff[i].Type);
                ret += ((i > 0) ? "}, \n\t\t\t\t{" : "") + MessageFieldHelper.Generate(Stuff[i]);
            }
            return ret;
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
                            "using Messages.std_msgs;\nusing String=System.String;\n\n"
                            ;
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
                    thisthing.input = thisthing.input.Replace("String", "string");
                    thisthing.Type = thisthing.Type.Replace("String", "string");
                    thisthing.output = thisthing.output.Replace("String", "string");
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
                GeneratedDictHelper = GenFields();
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
                File.AppendAllText(outdir + "\\" + localcn + ".cs", ToString().Replace("FauxMessages", "Messages"));
            else
                File.WriteAllText(outdir + "\\" + localcn + ".cs", ToString().Replace("FauxMessages", "Messages"));
            Thread.Sleep(10);
        }
    }

    public static class KnownStuff
    {
        private static char[] spliter = {' '};

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
            {"string", "string"},
            {"duration", "Duration"}
        };

        public static string GetConstTypesAffix(string type)
        {
            switch (type.ToLower())
            {
                case "decimal":
                    return "m";
                    break;
                case "single":
                case "float":
                    return "f";
                    break;
                case "long":
                    return "l";
                    break;
                case "ulong":
                    return "ul";
                    break;
                case "uint":
                    return "u";
                    break;
                default:
                    return "";
            }
        }

        public static SingleType WhatItIs(MsgsFile parent, string s, string extraindent)
        {
            string[] pieces = s.Split('/');
            string package = parent.Namespace.Replace("Messages.", "");
            if (pieces.Length > 1)
            {
                for (int i = 0; i < pieces.Length - 1; i++)
                {
                    if (i > 0 && i < pieces.Length - 2)
                        package += "/";
                    package += pieces[i];
                }
                s = pieces[pieces.Length - 1];
            }
            return WhatItIs(parent, new SingleType(package, s, extraindent));
        }

        public static SingleType WhatItIs(MsgsFile parent, SingleType t)
        {
            foreach (KeyValuePair<string, string> test in KnownTypes)
            {
                if (t.Test(test))
                {
                    t.rostype = t.Type;
                    return t.Finalize(parent, test);
                }
            }
            return t.Finalize(parent, t.input.Split(spliter, StringSplitOptions.RemoveEmptyEntries), false);
        }
    }

    public class SingleType
    {
        public bool Const;
        public string ConstValue = "";
        public bool IsArray;
        public bool IsLiteral;
        public string Name;
        public string Package;
        public string Type;
        private string[] backup;
        public string input;
        public string length = "";
        public string lowestindent = "\t\t";
        public bool meta;
        public string output;
        public string rostype = "";

        public SingleType(string s)
            : this("", s, "")
        {
        }

        public SingleType(string package, string s, string extraindent)
        {
            Package = package;
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

        public SingleType Finalize(MsgsFile parent, KeyValuePair<string, string> csharptype)
        {
            string[] PARTS = input.Split(' ');
            rostype = PARTS[0];
            if (!KnownStuff.KnownTypes.ContainsKey(rostype))
                meta = true;
            PARTS[0] = csharptype.Value;
            return Finalize(parent, PARTS, true);
        }

        public SingleType Finalize(MsgsFile parent, string[] s, bool isliteral)
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
            parent.resolve(Package, ref type);
            if (!IsArray)
            {
                if (otherstuff.Contains('=') && type.Equals("string", StringComparison.CurrentCultureIgnoreCase))
                {
                    otherstuff = otherstuff.Replace("\\", "\\\\");
                    otherstuff = otherstuff.Replace("\"", "\\\"");
                    string[] split = otherstuff.Split('=');
                    otherstuff = split[0] + " = " + split[1].Trim() + "";
                }
                if (otherstuff.Contains('=') && type == "bool")
                {
                    otherstuff = otherstuff.Replace("0", "false").Replace("1", "true");
                }
                if (otherstuff.Contains('=') && type == "byte")
                {
                    otherstuff = otherstuff.Replace("-1", "255");
                }
                Const = isconst;
                bool wantsconstructor = false;
                if (otherstuff.Contains("="))
                {
                    string[] chunks = otherstuff.Split('=');
                    ConstValue = chunks[chunks.Length - 1].Trim();
                    if (type.Equals("string", StringComparison.InvariantCultureIgnoreCase))
                    {
                        otherstuff = chunks[0] + " = \"" + chunks[1].Trim() + "\"";
                    }
                }
                string prefix = "", suffix = "";
                if (isconst)
                {
                    if (!type.Equals("string", StringComparison.InvariantCultureIgnoreCase))
                    {
                        prefix = "const ";
                    }
                }
                if (otherstuff.Contains('='))
                    if (wantsconstructor)
                        if (type == "string")
                            suffix = " = \"\"";
                        else
                            suffix = " = new " + type + "()";
                    else
                        suffix = KnownStuff.GetConstTypesAffix(type);
                output = lowestindent + "public " + prefix + type + " " + name + otherstuff + suffix + ";";
            }
            else
            {
                if (length.Length > 0)
                    IsLiteral = true;
                if (otherstuff.Contains('='))
                {
                    string[] split = otherstuff.Split('=');
                    otherstuff = split[0] + " = (" + type + ")" + split[1];
                }
                if (length.Length > 0)
                    output = lowestindent + "public " + type + "[] " + name + otherstuff + " = new " + type + "[" + length + "];";
                else
                    output = lowestindent + "public " + "" + type + "[] " + name + otherstuff + ";";
            }
            Type = type;
            if (!KnownStuff.KnownTypes.ContainsKey(rostype))
                meta = true;
            Name = name.Length == 0 ? otherstuff.Trim() : name;
            if (Name.Contains('='))
            {
                Name = Name.Substring(0, Name.IndexOf("=")).Trim();
            }
            return this;
        }

        public void refinalize(MsgsFile parent, string REALTYPE)
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
            parent.resolve(Package, ref type);
            if (!IsArray)
            {
                if (otherstuff.Contains('=') && type.Equals("string", StringComparison.CurrentCultureIgnoreCase))
                {
                    otherstuff = otherstuff.Replace("\\", "\\\\");
                    otherstuff = otherstuff.Replace("\"", "\\\"");
                    string[] split = otherstuff.Split('=');
                    otherstuff = split[0] + " = \"" + split[1].Trim() + "\"";
                }
                if (otherstuff.Contains('=') && type == "bool")
                {
                    otherstuff = otherstuff.Replace("0", "false").Replace("1", "true");
                }
                if (otherstuff.Contains('=') && type == "byte")
                {
                    otherstuff = otherstuff.Replace("-1", "255");
                }
                Const = isconst;
                bool wantsconstructor = false;
                if (otherstuff.Contains("="))
                {
                    string[] chunks = otherstuff.Split('=');
                    ConstValue = chunks[chunks.Length - 1].Trim();
                    if (type.Equals("string", StringComparison.InvariantCultureIgnoreCase))
                    {
                        otherstuff = chunks[0] + " = \"" + chunks[1].Trim().Replace("\"", "") + "\"";
                    }
                }
                else if (!type.Equals("String"))
                {
                    wantsconstructor = true;
                }
                string prefix = "", suffix = "";
                if (isconst)
                {
                    if (!type.Equals("string", StringComparison.InvariantCultureIgnoreCase))
                    {
                        prefix = "const ";
                    }
                }
                if (otherstuff.Contains('='))
                    if (wantsconstructor)
                        if (type == "string")
                            suffix = " = \"\"";
                        else
                            suffix = " = new " + type + "()";
                    else
                        suffix = KnownStuff.GetConstTypesAffix(type);
                output = lowestindent + "public " + prefix + type + " " + name + otherstuff + suffix + ";";
            }
            else
            {
                if (length.Length != 0)
                    IsLiteral = true; //type != "string";
                if (otherstuff.Contains('='))
                {
                    string[] split = otherstuff.Split('=');
                    otherstuff = split[0] + " = (" + type + ")" + split[1];
                }
                if (length.Length != 0)
                    output = lowestindent + "public " + type + "[] " + name + otherstuff + " = new " + type + "[" + length + "];";
                else
                    output = lowestindent + "public " + "" + type + "[] " + name + otherstuff + ";";
            }
            Type = type;
            if (!KnownStuff.KnownTypes.ContainsKey(rostype))
                meta = true;
            Name = name.Length == 0 ? otherstuff.Split('=')[0].Trim() : name;
        }
    }

    public static class MessageFieldHelper
    {
        public static string Generate(SingleType members)
        {
            string mt = "MsgTypes.Unknown";
            if (members.meta)
            {
                string t = members.Type.Replace("Messages.", "");
                if (!t.Contains('.'))
                    t = "std_msgs." + t;
                mt = "MsgTypes." + t.Replace(".", "__");
            }
            return String.Format
                ("\"{0}\", new MsgFieldInfo(\"{0}\", {1}, {2}, {3}, \"{4}\", {5}, \"{6}\", {7}, {8})",
                    members.Name,
                    members.IsLiteral.ToString().ToLower(),
                    ("typeof(" + members.Type + ")"),
                    members.Const.ToString().ToLower(),
                    members.ConstValue.TrimStart('"').TrimEnd('"'),
                    //members.Type.Equals("string", StringComparison.InvariantCultureIgnoreCase) ? ("new String("+members.ConstValue+")") : ("\""+members.ConstValue+"\""),
                    members.IsArray.ToString().ToLower(),
                    members.length,
                    //FIX MEEEEEEEE
                    members.meta.ToString().ToLower(),
                    mt);
        }
    }

    public class MsgFieldInfo
    {
        public string ConstVal;
        public bool IsArray;
        public bool IsConst;
        public bool IsLiteral;
        public bool IsMetaType;
        public int Length = -1;
        public string Name;
        public Type Type;
        public MsgTypes message_type;

#if !TRACE
        [DebuggerStepThrough]
#endif
        public MsgFieldInfo(string name, bool isliteral, Type type, bool isconst, string constval, bool isarray,
            string lengths, bool meta, MsgTypes mt)
        {
            Name = name;
            IsArray = isarray;
            Type = type;
            IsLiteral = isliteral;
            IsMetaType = meta;
            IsConst = isconst;
            ConstVal = constval;
            if (lengths == null) return;
            if (lengths.Length > 0)
            {
                Length = int.Parse(lengths);
            }
            message_type = mt;
        }
    }


    public enum ServiceMessageType
    {
        Not,
        Request,
        Response
    }
}
