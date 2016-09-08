// File: RobotDescriptionParser.cs
// Project: Listener
// 
// A temporary example hack for Jordan's NASA Valkyrie + Unity + ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Ros_CSharp;
using Collada141;
using System.Threading;
using System.IO;
using System.Reflection;

namespace MeshLib
{
    public class RobotDescriptionParser
    {
        public XDocument RobotDescription { get; private set; }

        private Dictionary<string, string> hardcoded_package_paths;
        private string tf_prefix;
        private string robot_description_param;
        private string robotdescription;
        private NodeHandle nh;

        public RobotDescriptionParser(string robot_description_param, string tf_prefix = null, Dictionary<string,string> hardcoded_package_paths = null)
        {
            this.robot_description_param = robot_description_param;
            this.tf_prefix = tf_prefix;
            this.hardcoded_package_paths = hardcoded_package_paths;
            new Thread(() => {
                while (!ROS.isStarted() || !ROS.ok) {
                    if (ROS.shutting_down)
                        break;
                    Thread.Sleep(100);
                }
                if (!ROS.shutting_down)
                {
                    nh = new NodeHandle();
                    Load();
                }
            }) { IsBackground = true }.Start();
        }

        public bool Load()
        {
            if (Param.get(robot_description_param, ref robotdescription))
            {
                return Parse();
            }
            return false;
        }

        /// <summary>
        /// TODO
        /// </summary>
        /// <param name="pkgName"></param>
        /// <returns>Directory containing the package</returns>
        private string Resolve(string pkgName)
        {
            return Directory.GetCurrentDirectory() + "\\" + pkgName;
        }

        private Collada141.COLLADA Load(string meshLocation)
        {
            if (meshLocation.Contains("package://"))
            {
                string trimmed;
                string pkg = (trimmed = meshLocation.Replace("package://", "")).Split('/')[0];
                string relpath = trimmed.Replace(pkg, "");
                string pkgLocation = Resolve(pkg);
                return COLLADA.Load(pkgLocation + "/" + relpath);
            }
            else
                throw new NotImplementedException("Unhandled mesh location type");
        }

        private Collada141.COLLADA Load(XElement meshElement)
        {
            COLLADA model = null;
            if (meshElement.Name == "uri")
            {
                return Load(meshElement.Value);
            }
            XAttribute attr = meshElement.Attribute(XName.Get("filename"));
            if (attr != null)
                model = Load(attr.Value);
            else
            {
                foreach (XElement element in meshElement.Elements())
                {
                    model = Load(element);
                    if (model != null)
                        break;
                }
            }
            return model;
        }

        /// <summary>
        /// Dig through the collada scene introspectively hunting for matrices and rotates
        /// TODO: flip the axes / vertices appropriately for ROS->UNITY differences
        /// </summary>
        /// <param name="items"></param>
        private void FindMatrices(object[] items)
        {
            List<object> frontier = new List<object>();
            if (items.Length == 0)
                return;
            foreach (object item in items)
            {
                if (item.GetType() == typeof(rotate))
                {
                    rotate r = (rotate)item;
                    Console.WriteLine("Found rotate: " + r.Values.Aggregate("", (v, x) => v + " " + x));
                    continue;
                }
                else if (item.GetType() == typeof(matrix))
                {
                    matrix m = (matrix)item;
                    Console.WriteLine("Found matrix: " + m.Values.Aggregate("", (v, x) => v + " " + x));
                    continue;
                }
                foreach (PropertyInfo fi in item.GetType().GetProperties())
                {
                    if (fi.PropertyType.IsArray)
                    {
                        Array arr = (Array)fi.GetValue(item, null);
                        if (arr != null)
                            foreach (var v in arr)
                                if (v != null)
                                    frontier.Add(v);
                    }
                    else if (!fi.PropertyType.Namespace.StartsWith("System"))
                    {
                        var indexes = fi.GetIndexParameters();
                        object o;
                        if (indexes != null && indexes.Length > 0)
                        {
                            Console.WriteLine(indexes.Length);
                            object[] index = new object[indexes.Length];
                            for (int i = 0; i < indexes.Length; i++)
                            {
                                if (indexes[i].ParameterType == typeof(string))
                                    index[i] = "";
                                else
                                    index[i] = Activator.CreateInstance(indexes[i].ParameterType);
                            }
                            o = fi.GetValue(item, index);
                        }
                        else
                        {
                            o = fi.GetValue(item, null);
                        }
                        if (o != null)
                        {
                            frontier.Add(o);
                        }
                    }
                }
            }
            FindMatrices(frontier.ToArray());
        }

        /// <summary>
        /// recursively find meshes in robot description
        /// </summary>
        /// <param name="elements"></param>
        /// <returns>success?</returns>
        private bool Parse(IEnumerable<XElement> elements = null)
        {
            if (elements == null)
            {
                RobotDescription = XDocument.Parse(this.robotdescription);
                if (RobotDescription != null && RobotDescription.Root != null)
                {
                    return Parse(RobotDescription.Elements());
                }
                return false;
            }
            bool success = true;
            foreach (XElement element in elements)
            {
                if (element.Name == "mesh")
                {
                    COLLADA model = Load(element);
                    FindMatrices(model.Items);
                }
                success &= Parse(element.Elements());
            }
            return success;
        }
    }
}
