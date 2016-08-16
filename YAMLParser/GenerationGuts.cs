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
            string contents = ToString();
            if (contents != null)
                File.WriteAllText(outdir + "\\" + localcn + ".cs", contents.Replace("FauxMessages", "Messages"));
        }

        public override string ToString()
        {
            if (requestfronthalf == null)
            {
                requestfronthalf = "";
                requestbackhalf = "";
                string[] lines = Templates.SrvPlaceHolder.Split('\n');
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

#region MD5
            GUTS = GUTS.Replace("$REQUESTMYMD5SUM", MD5.Sum(Request));
            GUTS = GUTS.Replace("$RESPONSEMYMD5SUM", MD5.Sum(Response));
            string GeneratedReqDeserializationCode = "", GeneratedReqSerializationCode = "", GeneratedResDeserializationCode = "", GeneratedResSerializationCode = "", GeneratedReqRandomizationCode = "", GeneratedResRandomizationCode = "", GeneratedReqEqualizationCode = "", GeneratedResEqualizationCode = "";
            //TODO: service support
            GeneratedReqEqualizationCode += string.Format("{0}.Request other = (Messages.{0}.Request)____other;\n", Request.Name);
            GeneratedResEqualizationCode += string.Format("{0}.Response other = (Messages.{0}.Response)____other;\n", Response.Name);
            for (int i = 0; i < Request.Stuff.Count; i++)
            {
                GeneratedReqDeserializationCode += Request.GenerateDeserializationCode(Request.Stuff[i], 1);
                GeneratedReqSerializationCode += Request.GenerateSerializationCode(Request.Stuff[i], 1);
                GeneratedReqRandomizationCode += Request.GenerateRandomizationCode(Request.Stuff[i], 1);
                GeneratedReqEqualizationCode += Request.GenerateEqualityCode(Request.Stuff[i], 1);
            }
            for (int i = 0; i < Response.Stuff.Count; i++)
            {
                GeneratedResDeserializationCode += Response.GenerateDeserializationCode(Response.Stuff[i], 1);
                GeneratedResSerializationCode += Response.GenerateSerializationCode(Response.Stuff[i], 1);
                GeneratedResRandomizationCode += Response.GenerateRandomizationCode(Response.Stuff[i], 1);
                GeneratedResEqualizationCode += Response.GenerateEqualityCode(Response.Stuff[i], 1);
            }
            GUTS = GUTS.Replace("$REQUESTSERIALIZATIONCODE", GeneratedReqSerializationCode);
            GUTS = GUTS.Replace("$REQUESTDESERIALIZATIONCODE", GeneratedReqDeserializationCode);
            GUTS = GUTS.Replace("$REQUESTRANDOMIZATIONCODE", GeneratedReqRandomizationCode);
            GUTS = GUTS.Replace("$REQUESTEQUALITYCODE", GeneratedReqEqualizationCode);
            GUTS = GUTS.Replace("$RESPONSESERIALIZATIONCODE", GeneratedResSerializationCode);
            GUTS = GUTS.Replace("$RESPONSEDESERIALIZATIONCODE", GeneratedResDeserializationCode);
            GUTS = GUTS.Replace("$RESPONSERANDOMIZATIONCODE", GeneratedResRandomizationCode);
            GUTS = GUTS.Replace("$RESPONSEEQUALITYCODE", GeneratedResEqualizationCode);

            string md5 = MD5.Sum(this);
            if (md5 == null)
                return null;
            GUTS = GUTS.Replace("$MYSRVMD5SUM", md5);
#endregion

            /********END BLOCK**********/
            return GUTS;
        }
    }

    public class MsgsFile
    {
        private const string stfmat = "\tname: {0}\n\t\ttype: {1}\n\t\ttrostype: {2}\n\t\tisliteral: {3}\n\t\tisconst: {4}\n\t\tconstvalue: {5}\n\t\tisarray: {6}\n\t\tlength: {7}\n\t\tismeta: {8}";

        public class ResolvedMsg
        {
            public string OtherType;
            public MsgsFile Definer;
        }

        public static Dictionary<string, Dictionary<string, List<ResolvedMsg>>> resolver = new Dictionary<string, Dictionary<string, List<ResolvedMsg>>>();
        
        private string GUTS;
        public string GeneratedDictHelper;
        public bool HasHeader;
        public string Name;
        public string Namespace = "Messages";
        public List<SingleType> Stuff = new List<SingleType>();
        public string backhalf;
        public string classname;
        private List<string> def = new List<string>();
        public string Package;

        public string Definition
        {
            get { return !def.Any() ? "" : def.Aggregate((old, next) => "" + old + "\n" + next); }
        }

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
            if (resolver == null)
                resolver = new Dictionary<string, Dictionary<string, List<ResolvedMsg>>>();
            serviceMessageType = isrequest ? ServiceMessageType.Request : ServiceMessageType.Response;
            //Parse The file name to get the classname;
            classname = filename.basename;
            //Parse for the Namespace
            Namespace += "." + filename.package;
            Name = filename.package + "." + classname;
            classname += (isrequest ? "Request" : "Response");
            Namespace = Namespace.Trim('.');
            Package = filename.package;
            if (!resolver.Keys.Contains(Package))
                resolver.Add(Package, new Dictionary<string, List<ResolvedMsg>>());
            if (!resolver[Package].ContainsKey(classname))
                resolver[Package].Add(classname, new List<ResolvedMsg> { new ResolvedMsg { OtherType = Namespace + "." + classname, Definer = this } });
            else
                resolver[Package][classname].Add(new ResolvedMsg { OtherType = Namespace + "." + classname, Definer = this });
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
            if (resolver == null)
                resolver = new Dictionary<string, Dictionary<string,List<ResolvedMsg>>>();
            if (!filename.Path.Contains(".msg"))
                throw new Exception("" + filename + " IS NOT A VALID MSG FILE!");
            classname = filename.basename;
            Package = filename.package;
            //Parse for the Namespace
            Namespace += "." + filename.package;
            Name = filename.package + "." + classname;
            Namespace = Namespace.Trim('.');
            if (!resolver.Keys.Contains(Package))
                resolver.Add(Package, new Dictionary<string, List<ResolvedMsg>>());
            if (!resolver[Package].ContainsKey(classname))
                resolver[Package].Add(classname, new List<ResolvedMsg> { new ResolvedMsg{OtherType = Namespace + "." + classname, Definer = this} });
            else
                resolver[Package][classname].Add(new ResolvedMsg{OtherType = Namespace + "." + classname, Definer = this});
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

        public void resolve(MsgsFile parent, SingleType st)
        {
            if (st.Type == null)
            {
                KnownStuff.WhatItIs(parent, st);
            }
            List<string> prefixes = new List<string>(new[] { "", "std_msgs", "geometry_msgs", "actionlib_msgs"});
            if (st.Type.Contains("/"))
            {
                string[] pieces = st.Type.Split('/');
                st.Package = pieces[0];
                st.Type = pieces[1];
            }
            if (!string.IsNullOrEmpty(st.Package))
                prefixes[0] = st.Package;
            foreach (string p in prefixes)
            {
                if (resolver.Keys.Contains(p))
                {
                    if (resolver[p].ContainsKey(st.Type))
                    {
                        if (resolver[p][st.Type].Count == 1)
                        {
                            st.Package = p;
                            st.Definer = resolver[p][st.Type][0].Definer;
                        }
                        else if (resolver[p][st.Type].Count>1)
                            throw new Exception("Could not resolve " + st.Type);
                    }
                }
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
            {
                resolve(this, S);
                GeneratedDictHelper += MessageFieldHelper.Generate(S);
            }
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
                string[] lines = Templates.MsgPlaceHolder.Split('\n');
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
            string GeneratedDeserializationCode = "", GeneratedSerializationCode = "", GeneratedRandomizationCode = "", GeneratedEqualityCode="";
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
                bool literal = false;
                StringBuilder DEF = new StringBuilder();
                foreach (string s in def)
                    DEF.AppendLine(s);
                Debug.WriteLine("============\n"+this.classname);
            }
            GUTS = (serviceMessageType != ServiceMessageType.Response ? fronthalf : "") + "\n" + memoizedcontent + "\n" +
                   (serviceMessageType != ServiceMessageType.Request ? backhalf : "");
            if (classname.ToLower() == "string")
            {
                GUTS = GUTS.Replace("$NULLCONSTBODY", "if (data == null)\n\t\t\tdata = \"\";\n");
                GUTS = GUTS.Replace("$EXTRACONSTRUCTOR", "\n\t\tpublic $WHATAMI(string d)\n\t\t{\n\t\t\tdata = d;\n\t\t}\n");
            }
            else if (classname == "Time" || classname == "Duration")
            {
                GUTS = GUTS.Replace("$EXTRACONSTRUCTOR", "\n\t\tpublic $WHATAMI(TimeData d)\n\t\t{\n\t\t\tdata = d;\n\t\t}\n");
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
            string md5 = MD5.Sum(this);
            if (md5 == null) return null;

            GeneratedEqualityCode += string.Format("{0} other = (Messages.{0})____other;\n", Name);
            for (int i = 0; i < Stuff.Count; i++)
            {
                GeneratedDeserializationCode += this.GenerateDeserializationCode(Stuff[i]);
                GeneratedSerializationCode += this.GenerateSerializationCode(Stuff[i]);
                GeneratedRandomizationCode += this.GenerateRandomizationCode(Stuff[i]);
                GeneratedEqualityCode += this.GenerateEqualityCode(Stuff[i]);
                //equality TOO
            }
            GUTS = GUTS.Replace("$SERIALIZATIONCODE", GeneratedSerializationCode);
            GUTS = GUTS.Replace("$DESERIALIZATIONCODE", GeneratedDeserializationCode);
            GUTS = GUTS.Replace("$RANDOMIZATIONCODE", GeneratedRandomizationCode);
            GUTS = GUTS.Replace("$EQUALITYCODE", GeneratedEqualityCode);
            GUTS = GUTS.Replace("$MYMD5SUM", md5);

            return GUTS;
        }

        /// <summary>
        /// How many 4-space "tabs" to prepend
        /// </summary>
        private const int LEADING_WHITESPACE = 3;
        private string GenerateSerializationForOne(string type, string name, SingleType st, int extraTabs=0)
        {
            string leadingWhitespace = "";
            for (int i = 0; i < LEADING_WHITESPACE + extraTabs; i++)
                leadingWhitespace += "    ";
            type = KnownStuff.GetNamespacedType(st, type);
            if (type == "Time" || type == "Duration")
            {
                return string.Format(@"
{0}//{1}
{0}pieces.Add(BitConverter.GetBytes({1}.data.sec));
{0}pieces.Add(BitConverter.GetBytes({1}.data.nsec));", leadingWhitespace, name);
            }
            else if (type == "TimeData")
                return string.Format(@"
{0}//{1}
{0}pieces.Add(BitConverter.GetBytes({1}.sec));
{0}pieces.Add(BitConverter.GetBytes({1}.nsec));", leadingWhitespace, name);
            else if (type == "byte")
            {
                return string.Format(@"
{0}//{1}
{0}pieces.Add(new[] {{ (byte){1} }});", leadingWhitespace, name); ;
            }
            else if (type == "string")
            {
                return string.Format(@"
{0}//{1}
{0}if ({1} == null)
{0}    {1} = """";
{0}scratch1 = Encoding.ASCII.GetBytes((string){1});
{0}thischunk = new byte[scratch1.Length + 4];
{0}scratch2 = BitConverter.GetBytes(scratch1.Length);
{0}Array.Copy(scratch1, 0, thischunk, 4, scratch1.Length);
{0}Array.Copy(scratch2, thischunk, 4);
{0}pieces.Add(thischunk);", leadingWhitespace, name);
            }
            else if (type == "bool")
            {
                return string.Format(@"
{0}//{1}
{0}thischunk = new byte[1];
{0}thischunk[0] = (byte) ((bool){1} ? 1 : 0 );
{0}pieces.Add(thischunk);", leadingWhitespace, name);
            }
            else if (st.IsLiteral)
            {
                return string.Format(@"
{0}//{1}
{0}scratch1 = new byte[Marshal.SizeOf(typeof({2}))];
{0}h = GCHandle.Alloc(scratch1, GCHandleType.Pinned);
{0}Marshal.StructureToPtr({1}, h.AddrOfPinnedObject(), false);
{0}h.Free();
{0}pieces.Add(scratch1);", leadingWhitespace, name, type);
            }
            else
            {
                return string.Format(@"
{0}//{1}
{0}if ({1} == null)
{0}    {1} = new {2}();
{0}pieces.Add({1}.Serialize(true));", leadingWhitespace, name, type);
            }
        }
        public string GenerateSerializationCode(SingleType st, int extraTabs = 0)
        {
            string leadingWhitespace = "";
            for (int i = 0; i < LEADING_WHITESPACE + extraTabs; i++)
                leadingWhitespace += "    ";
            if (st.Const)
                return "";
            if (!st.IsArray)
            {
                return GenerateSerializationForOne(st.Type, st.Name, st, extraTabs);
            }

            int arraylength = -1;
            string ret = string.Format(@"
{0}//{2}
{0}hasmetacomponents |= {1};", leadingWhitespace, st.meta.ToString().ToLower(), st.Name);
            string completetype = KnownStuff.GetNamespacedType(st);
            ret += string.Format(@"
{0}if ({1} == null)
{0}    {1} = new {2}[0];", leadingWhitespace, st.Name, completetype);
            if (string.IsNullOrEmpty(st.length) || !int.TryParse(st.length, out arraylength) || arraylength == -1)
            {
                ret += string.Format(@"
{0}pieces.Add(BitConverter.GetBytes({1}.Length));", leadingWhitespace, st.Name);
            }
            //special case arrays of bytes
            if (st.Type == "byte")
            {
                ret += string.Format(@"
{0}pieces.Add(({2}[]){1});", leadingWhitespace, st.Name, st.Type);
            }
            else
            {
                ret += string.Format(@"
{0}for (int i=0;i<{1}.Length; i++) {{{2}
{0}}}", leadingWhitespace, st.Name, GenerateSerializationForOne(st.Type, st.Name + "[i]", st, extraTabs + 1));
            }
            return ret;
        }

        public string GenerateDeserializationCode(SingleType st, int extraTabs = 0)
        {
            string leadingWhitespace = "";
            for (int i = 0; i < LEADING_WHITESPACE + extraTabs; i++)
                leadingWhitespace += "    ";
            // this happens  for each member of the outer message
            // after concluding, make sure part of the string is "currentIndex += <amount read while deserializing this thing>"
            // start of deserializing piece referred to by st is currentIndex (its value at time of call to this fn)"

            string pt = KnownStuff.GetNamespacedType(st);
            if(st.Const)
            {
                return "";
            }
            else if(!st.IsArray)
            {
                return GenerateDeserializationForOne(st.Type, st.Name, st, extraTabs);
            }

            string ret = string.Format(@"
{0}//{2}
{0}hasmetacomponents |= {1};", leadingWhitespace, st.meta.ToString().ToLower(), st.Name);
            int arraylength = -1;
            string arraylengthstr = "arraylength";

            //If the object is an array, send each object to be processed individually, then add them to the string

            //handle fixed length fields?
            if (!string.IsNullOrEmpty(st.length) && int.TryParse(st.length, out arraylength) && arraylength != -1)
            {
                arraylengthstr = "" + arraylength;
            }
            else
            {
                ret += string.Format(@"
{0}arraylength = BitConverter.ToInt32(SERIALIZEDSTUFF, currentIndex);
{0}currentIndex += Marshal.SizeOf(typeof(System.Int32));", leadingWhitespace);
            }
            ret += string.Format(@"
{0}if ({1} == null)
{0}    {1} = new {2}[{3}];
{0}else
{0}    Array.Resize(ref {1}, {3});", leadingWhitespace, st.Name, pt, arraylengthstr);

            //special case arrays of bytes
            if (st.Type == "byte")
            {
                ret += string.Format(@"
{0}Array.Copy(SERIALIZEDSTUFF, currentIndex, {1}, 0, {1}.Length);
{0}currentIndex += {1}.Length;", leadingWhitespace, st.Name);
            }
            else
            {
                ret += string.Format(@"
{0}for (int i=0;i<{1}.Length; i++) {{{2}
{0}}}", leadingWhitespace, st.Name, GenerateDeserializationForOne(pt, st.Name + "[i]", st, extraTabs + 1));
            }
            return ret;
        }

        private string GenerateDeserializationForOne(string type, string name, SingleType st, int extraTabs = 0)
        {
            string leadingWhitespace = "";
            for (int i = 0; i < LEADING_WHITESPACE + extraTabs; i++)
                leadingWhitespace += "    ";
            string pt = KnownStuff.GetNamespacedType(st);
            if (type == "Time" || type == "Duration")
            {
                return string.Format(@"
{0}//{1}
{0}{1} = new {2}(new TimeData(
{0}        BitConverter.ToUInt32(SERIALIZEDSTUFF, currentIndex),
{0}        BitConverter.ToUInt32(SERIALIZEDSTUFF, currentIndex+Marshal.SizeOf(typeof(System.Int32)))));
{0}currentIndex += 2*Marshal.SizeOf(typeof(System.Int32));", leadingWhitespace, name, pt);
            }
            else if (type == "TimeData")
                return string.Format(@"
{0}//{1}
{0}{1}.sec = BitConverter.ToUInt32(SERIALIZEDSTUFF, currentIndex);
{0}currentIndex += Marshal.SizeOf(typeof(System.Int32));
{0}{1}.nsec  = BitConverter.ToUInt32(SERIALIZEDSTUFF, currentIndex);
{0}currentIndex += Marshal.SizeOf(typeof(System.Int32));", leadingWhitespace, name);
            else if (type == "byte")
            {
                return string.Format(@"
{0}//{1}
{0}{1}=SERIALIZEDSTUFF[currentIndex++];", leadingWhitespace, name);
            }
            else if (type == "string")
            {
                return string.Format(@"
{0}//{1}
{0}{1} = """";
{0}piecesize = BitConverter.ToInt32(SERIALIZEDSTUFF, currentIndex);
{0}currentIndex += 4;
{0}{1} = Encoding.ASCII.GetString(SERIALIZEDSTUFF, currentIndex, piecesize);
{0}currentIndex += piecesize;", leadingWhitespace, name);
            }
            else if (type == "bool")
            {
                return string.Format(@"
{0}//{1}
{0}{1} = SERIALIZEDSTUFF[currentIndex++]==1;", leadingWhitespace, name);
            }
            else if (st.IsLiteral)
            {
                string ret = string.Format(@"
{0}//{2}
{0}piecesize = Marshal.SizeOf(typeof({1}));
{0}h = IntPtr.Zero;
{0}if (SERIALIZEDSTUFF.Length - currentIndex != 0)
{0}{{
{0}    h = Marshal.AllocHGlobal(piecesize);
{0}    Marshal.Copy(SERIALIZEDSTUFF, currentIndex, h, piecesize);
{0}}}
{0}if (h == IntPtr.Zero) throw new Exception(""Alloc failed"");
{0}{2} = ({1})Marshal.PtrToStructure(h, typeof({1}));
{0}Marshal.FreeHGlobal(h);
{0}currentIndex+= piecesize;", leadingWhitespace, pt, name);
               
                return ret;
            }
            else
            {
                return string.Format(@"
{0}//{1}
{0}{1} = new {2}(SERIALIZEDSTUFF, ref currentIndex);", leadingWhitespace, name, pt);
            }
        }

        public string GenerateRandomizationCode(SingleType st, int extraTabs = 0)
        {
            string leadingWhitespace = "";
            for (int i = 0; i < LEADING_WHITESPACE + extraTabs; i++)
                leadingWhitespace += "    ";
            // this happens  for each member of the outer message
            // after concluding, make sure part of the string is "currentIndex += <amount read while deserializing this thing>"
            // start of deserializing piece referred to by st is currentIndex (its value at time of call to this fn)"

            string pt = KnownStuff.GetNamespacedType(st);
            if (st.Const)
            {
                return "";
            }
            else if (!st.IsArray)
            {
                return GenerateRandomizationCodeForOne(st.Type, st.Name, st, extraTabs);
            }

            string ret = string.Format(@"
{0}//{1}", leadingWhitespace, st.Name);
            int arraylength = -1;
            string arraylengthstr = "arraylength";

            //If the object is an array, send each object to be processed individually, then add them to the string

            //handle fixed length fields?
            if (!string.IsNullOrEmpty(st.length) && int.TryParse(st.length, out arraylength) && arraylength != -1)
            {
                arraylengthstr = "" + arraylength;
            }
            else
            {
                ret += string.Format(@"
{0}arraylength = rand.Next(10);", leadingWhitespace);
            }
            ret += string.Format(@"
{0}if ({1} == null)
{0}    {1} = new {2}[{3}];
{0}else
{0}    Array.Resize(ref {1}, {3});", leadingWhitespace, st.Name, pt, arraylengthstr);

            ret += string.Format(@"
{0}for (int i=0;i<{1}.Length; i++) {{{2}
{0}}}", leadingWhitespace, st.Name, GenerateRandomizationCodeForOne(pt, st.Name + "[i]", st, extraTabs + 1));
            return ret;
        }
        private string GenerateRandomizationCodeForOne(string type, string name, SingleType st, int extraTabs = 0)
        {
            string leadingWhitespace = "";
            for (int i = 0; i < LEADING_WHITESPACE + extraTabs; i++)
                leadingWhitespace += "    ";
            string pt = KnownStuff.GetNamespacedType(st);
            if (type == "Time" || type == "Duration")
            {
                return string.Format(@"
{0}//{1}
{0}{1} = new {2}(new TimeData(
{0}        Convert.ToUInt32(rand.Next()),
{0}        Convert.ToUInt32(rand.Next())));", leadingWhitespace, name, pt);
            }
            else if (type == "TimeData")
                return string.Format(@"
{0}//{1}
{0}{1}.sec = Convert.ToUInt32(rand.Next());
{0}{1}.nsec  = Convert.ToUInt32(rand.Next());", leadingWhitespace, name);
            else if (type == "byte")
            {
                return string.Format(@"
{0}//{1}
{0}myByte = new byte[1];
{0}rand.NextBytes(myByte);
{0}{1}= myByte[0];", leadingWhitespace, name);
            }
            else if (type == "string")
            {
                return string.Format(@"
{0}//{1}
{0}strlength = rand.Next(100) + 1;
{0}strbuf = new byte[strlength];
{0}rand.NextBytes(strbuf);  //fill the whole buffer with random bytes
{0}for (int x = 0; x < strlength; x++)
{0}    if (strbuf[x] == 0) //replace null chars with non-null random ones
{0}        strbuf[x] = (byte)(rand.Next(254) + 1);
{0}strbuf[strlength - 1] = 0; //null terminate
{0}{1} = Encoding.ASCII.GetString(strbuf);", leadingWhitespace, name);
            }
            else if (type == "bool")
            {
                return string.Format(@"
{0}//{1}
{0}{1} = rand.Next(2) == 1;", leadingWhitespace, name);
            }
            else if (type == "int")
            {
                return string.Format(@"
{0}//{1}
{0}{1} = rand.Next();", leadingWhitespace, name);
            }
            else if (type == "uint")
            {
                return string.Format(@"
{0}//{1}
{0}{1} = (uint)rand.Next();", leadingWhitespace, name);
            }
            else if (type == "double")
            {
                return string.Format(@"
{0}//{1}
{0}{1} = (rand.Next() + rand.NextDouble());", leadingWhitespace, name);
            }
            else if (type == "float" || type == "Float64" || type == "Single")
            {
                return string.Format(@"
{0}//{1}
{0}{1} = (float)(rand.Next() + rand.NextDouble());", leadingWhitespace, name);
            }
            else if (type == "Int16" || type == "Short" || type == "short")
            {
                return string.Format(@"
{0}//{1}
{0}{1} = (System.Int16)rand.Next(System.Int16.MaxValue + 1);", leadingWhitespace, name);
            }
            else if (type == "UInt16" || type == "ushort" || type == "UShort")
            {
                return string.Format(@"
{0}//{1}
{0}{1} = (System.UInt16)rand.Next(System.UInt16.MaxValue + 1);", leadingWhitespace, name);
            }
            else if (type == "SByte" || type == "sbyte")
            {
                return string.Format(@"
{0}//{1}
{0}{1} = (SByte)(rand.Next(255) - 127);", leadingWhitespace, name);
            }
            else if (type == "UInt64" || type == "ULong" || type == "ulong")
            {
                return string.Format(@"
{0}//{1}
{0}{1} = (System.UInt64)((uint)(rand.Next() << 32)) | (uint)rand.Next();", leadingWhitespace, name);
            }
            else if (type == "Int64" || type == "Long" || type == "long")
            {
                return string.Format(@"
{0}//{1}
{0}{1} = (System.Int64)(rand.Next() << 32) | rand.Next();", leadingWhitespace, name);
            }
            else if (type == "char")
            {
                return string.Format(@"
{0}//{1}
{0}{1} = (char)(byte)(rand.Next(254) + 1);", leadingWhitespace, name);
            }
            else if (st.IsLiteral)
            {
                throw new Exception(st.Type + " needs to be handled SPECIFICALLY!");
            }
            else
            {
                return string.Format(@"
{0}//{1}
{0}{1} = new {2}();
{0}{1}.Randomize();", leadingWhitespace, name, pt);
            }
        }

        public string GenerateEqualityCode(SingleType st, int extraTabs = 0)
        {
            string leadingWhitespace = "";
            for (int i = 0; i < LEADING_WHITESPACE + extraTabs; i++)
                leadingWhitespace += "    ";

            if (st.IsArray)
            {
                return string.Format(@"
{0}if ({1}.Length != other.{1}.Length)
{0}    return false;
{0}for (int __i__=0; __i__ < {1}.Length; __i__++)
{0}{{{2}
{0}}}", leadingWhitespace, st.Name, GenerateEqualityCodeForOne(st.Type, st.Name + "[__i__]", st, extraTabs + 1));
            }
            else
                return GenerateEqualityCodeForOne(st.Type, st.Name, st, extraTabs);
        }
        private string GenerateEqualityCodeForOne(string type, string name, SingleType st, int extraTabs = 0)
        {
            string leadingWhitespace = "";
            for (int i = 0; i < LEADING_WHITESPACE + extraTabs; i++)
                leadingWhitespace += "    ";
            if (st.IsLiteral)
                if (st.Const)
                    return "";
                else if (type == "TimeData")
                    return string.Format(@"
{0}ret &= {1}.sec == other.{1}.sec;
{0}ret &= {1}.nsec == other.{1}.nsec;", leadingWhitespace, name);
                else
                    return string.Format(@"
{0}ret &= {1} == other.{1};", leadingWhitespace, name);
            else
                return string.Format(@"
{0}ret &= {1}.Equals(other.{1});", leadingWhitespace, name);
            // and that's it
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
            string contents = ToString();
            if (contents == null)
                return;
            if (serviceMessageType == ServiceMessageType.Response)
                File.AppendAllText(outdir + "\\" + localcn + ".cs", contents.Replace("FauxMessages", "Messages"));
            else
                File.WriteAllText(outdir + "\\" + localcn + ".cs", contents.Replace("FauxMessages", "Messages"));
        }
    }

    public static class KnownStuff
    {
        private static char[] spliter = {' '};
        
        /// <summary>
        /// Message namespaces known to ALL messages (in their header's using lines)
        /// </summary>
        private const string STATIC_NAMESPACE_STRING = "using Messages.std_msgs;\nusing String=System.String;\nusing Messages.geometry_msgs;\nusing Messages.nav_msgs;";

        /// <summary>
        /// Returns the namespaced type name (if neccessary)
        /// </summary>
        /// <param name="st">This thing's SingleType</param>
        /// <param name="type">(optional) the type string</param>
        /// <returns>duh</returns>
        public static string GetNamespacedType(SingleType st, string type = null)
        {
            if (type == null)
                type = st.Type;
            if (!KnownTypes.ContainsKey(st.rostype) && !type.Contains(st.Package) && !STATIC_NAMESPACE_STRING.Contains("Messages." + st.Package))
                return string.Format("Messages.{0}.{1}", st.Package, type);
            return type;
        }

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
            string package = parent.Package;
            if (pieces.Length == 2)
            {
                package = pieces[0];
                s = pieces[1];
            }
            SingleType st = new SingleType(package, s, extraindent);
            parent.resolve(parent, st);
            WhatItIs(parent, st);
            return st;
        }

        public static void WhatItIs(MsgsFile parent, SingleType t)
        {
            foreach (KeyValuePair<string, string> test in KnownTypes)
            {
                if (t.Test(test))
                {
                    t.rostype = t.Type;
                    SingleType.Finalize(parent, t, test);
                    return;
                }
            }
            t.Finalize(parent, t.input.Split(spliter, StringSplitOptions.RemoveEmptyEntries), false);
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
        public MsgsFile Definer;

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

        public static void Finalize(MsgsFile parent, SingleType thing, KeyValuePair<string, string> csharptype)
        {
            string[] PARTS = thing.input.Split(' ');
            thing.rostype = PARTS[0];
            if (!KnownStuff.KnownTypes.ContainsKey(thing.rostype))
                thing.meta = true;
            PARTS[0] = csharptype.Value;
            thing.Finalize(parent, PARTS, true);
        }

        public void Finalize(MsgsFile parent, string[] s, bool isliteral)
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
                string t = KnownStuff.GetNamespacedType(this, type);
                output = lowestindent + "public " + prefix + t + " " + name + otherstuff + suffix + ";";
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
                string t = KnownStuff.GetNamespacedType(this, type);
                if (length.Length > 0)
                    output = lowestindent + "public " + t + "[] " + name + otherstuff + " = new " + type + "[" + length + "];";
                else
                    output = lowestindent + "public " + "" + t + "[] " + name + otherstuff + ";";
            }
            Type = type;
            parent.resolve(parent, this);
            if (!KnownStuff.KnownTypes.ContainsKey(rostype))
            {
                meta = true;
            }
            Name = name.Length == 0 ? otherstuff.Trim() : name;
            if (Name.Contains('='))
            {
                Name = Name.Substring(0, Name.IndexOf("=")).Trim();
            }
        }

        public void refinalize(MsgsFile parent, string REALTYPE)
        {
            bool isconst = false;
            Type = REALTYPE;
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
            parent.resolve(parent, this);
            if (!IsArray)
            {
                if (otherstuff.Contains('=') && Type.Equals("string", StringComparison.CurrentCultureIgnoreCase))
                {
                    otherstuff = otherstuff.Replace("\\", "\\\\");
                    otherstuff = otherstuff.Replace("\"", "\\\"");
                    string[] split = otherstuff.Split('=');
                    otherstuff = split[0] + " = \"" + split[1].Trim() + "\"";
                }
                if (otherstuff.Contains('=') && Type == "bool")
                {
                    otherstuff = otherstuff.Replace("0", "false").Replace("1", "true");
                }
                if (otherstuff.Contains('=') && Type == "byte")
                {
                    otherstuff = otherstuff.Replace("-1", "255");
                }
                Const = isconst;
                bool wantsconstructor = false;
                if (otherstuff.Contains("="))
                {
                    string[] chunks = otherstuff.Split('=');
                    ConstValue = chunks[chunks.Length - 1].Trim();
                    if (Type.Equals("string", StringComparison.InvariantCultureIgnoreCase))
                    {
                        otherstuff = chunks[0] + " = \"" + chunks[1].Trim().Replace("\"", "") + "\"";
                    }
                }
                else if (!Type.Equals("String"))
                {
                    wantsconstructor = true;
                }
                string prefix = "", suffix = "";
                if (isconst)
                {
                    if (!Type.Equals("string", StringComparison.InvariantCultureIgnoreCase))
                    {
                        prefix = "const ";
                    }
                }
                if (otherstuff.Contains('='))
                    if (wantsconstructor)
                        if (Type == "string")
                            suffix = " = \"\"";
                        else
                            suffix = " = new " + Type + "()";
                    else
                        suffix = KnownStuff.GetConstTypesAffix(Type);
                string t = KnownStuff.GetNamespacedType(this, Type);
                output = lowestindent + "public " + prefix + t + " " + name + otherstuff + suffix + ";";
            }
            else
            {
                if (length.Length != 0)
                    IsLiteral = true; //type != "string";
                if (otherstuff.Contains('='))
                {
                    string[] split = otherstuff.Split('=');
                    otherstuff = split[0] + " = (" + Type + ")" + split[1];
                }
                string t = KnownStuff.GetNamespacedType(this, Type);
                if (length.Length != 0)
                    output = lowestindent + "public " + t + "[] " + name + otherstuff + " = new " + t + "[" + length + "];";
                else
                    output = lowestindent + "public " + "" + t + "[] " + name + otherstuff + ";";
            }
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
            string pt = KnownStuff.GetNamespacedType(members);
            if (members.meta)
            {
                string t = members.Type.Replace("Messages.", "");
                if (!t.Contains('.'))
                    t = members.Definer.Package + "." + t;
                mt = "MsgTypes." + t.Replace(".", "__");
            }
            return String.Format
                ("\"{0}\", new MsgFieldInfo(\"{0}\", {1}, {2}, {3}, \"{4}\", {5}, \"{6}\", {7}, {8})",
                    members.Name,
                    members.IsLiteral.ToString().ToLower(),
                    ("typeof(" + pt + ")"),
                    members.Const.ToString().ToLower(),
                    members.ConstValue.TrimStart('"').TrimEnd('"'),
                    //members.Type.Equals("string", StringComparison.InvariantCultureIgnoreCase) ? ("new String("+members.ConstValue+")") : ("\""+members.ConstValue+"\""),
                    members.IsArray.ToString().ToLower(),
                    members.length,
                    //FIX MEEEEEEEE
                    members.meta.ToString().ToLower(),
                    mt);
        }
        public static KeyValuePair<string, MsgFieldInfo> Instantiate(SingleType member)
        {
            string mt = "MsgTypes.Unknown";
            if (member.meta)
            {
                string t = member.Type.Replace("Messages.", "");
                if (!t.Contains('.'))
                    t = "std_msgs." + t;
                mt = "MsgType." + t.Replace(".", "__");
            }
            return new KeyValuePair<string, MsgFieldInfo>(member.Name, new MsgFieldInfo(member.Name, member.IsLiteral, member.Type, member.Const, member.ConstValue, member.IsArray, member.length, member.meta));
        }

        public static Dictionary<string, MsgFieldInfo> Instantiate(IEnumerable<SingleType> stuff)
        {
            return stuff.Select(Instantiate).ToDictionary(field => field.Key, field => field.Value);
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
        public string Type;

#if !TRACE
        [DebuggerStepThrough]
#endif
        public MsgFieldInfo(string name, bool isliteral, string type, bool isconst, string constval, bool isarray,
            string lengths, bool meta)
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
        }
    }


    public enum ServiceMessageType
    {
        Not,
        Request,
        Response
    }
}
