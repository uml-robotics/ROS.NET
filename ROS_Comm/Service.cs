// File: Service.cs
// Project: ROS_C-Sharp
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 04/28/2015
// Updated: 02/10/2016

#region USINGZ

using System;
using System.Collections;
using System.Threading;

#endregion

namespace Ros_CSharp
{
    public static class Service
    {
        public static bool exists(string service_name, bool print_failure_reason)
        {
            string mapped_name = names.resolve(service_name);

            string host = "";
            int port = 0;

            if (ServiceManager.Instance.lookUpService(mapped_name, ref host, ref port))
            {
                TcpTransport transport = new TcpTransport();

                IDictionary m = new Hashtable
                {
                    {"probe", "1"},
                    {"md5sum", "*"},
                    {"callerid", this_node.Name},
                    {"service", mapped_name}
                };

                byte[] headerbuf = null;
                int size = 0;
                Header h = new Header();
                h.Write(m, ref headerbuf, ref size);

                if (transport.connect(host, port))
                {
                    byte[] sizebuf = BitConverter.GetBytes(size);

                    transport.write(sizebuf, 0, sizebuf.Length);
                    transport.write(headerbuf, 0, size);

                    return true;
                }
                if (print_failure_reason)
                {
                    ROS.Info("waitForService: Service[{0}] could not connect to host [{1}:{2}], waiting...", mapped_name, host, port);
                }
            }
            else if (print_failure_reason)
            {
                ROS.Info("waitForService: Service[{0}] has not been advertised, waiting...", mapped_name);
            }
            return false;
        }

        public static bool waitForService(string service_name, TimeSpan ts)
        {
            string mapped_name = names.resolve(service_name);
            DateTime start_time = DateTime.Now;
            bool printed = false;
            while (ROS.ok)
            {
                if (exists(service_name, !printed))
                {
                    break;
                }
                printed = true;
                if (ts >= TimeSpan.Zero)
                {
                    if (DateTime.Now.Subtract(start_time) > ts)
                        return false;
                }
                Thread.Sleep(ROS.WallDuration);
            }

            if (printed && ROS.ok)
            {
                ROS.Info("waitForService: Service[{0}] is now available.", mapped_name);
            }
            return true;
        }

        public static bool waitForService(string service_name, int timeout)
        {
            return waitForService(service_name, TimeSpan.FromMilliseconds(timeout));
        }
    }
}