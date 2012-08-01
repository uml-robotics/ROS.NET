#region USINGZ

using System;
using System.Linq;
using System.Collections.Generic;
#if SURFACEWINDOW
using GenericTypes_Surface_Adapter;
using Microsoft.Surface;
using Microsoft.Surface.Core;
using Microsoft.Surface.Presentation;
using Microsoft.Surface.Presentation.Controls;
#else
using EM3MTouchLib;
#endif
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using DREAMController;
using GenericTouchTypes;
using System.IO;
using System.Threading;
using Messages;
using Messages.custom_msgs;
using Ros_CSharp;
using XmlRpc_Wrapper;
using Int32 = Messages.std_msgs.Int32;
using String = Messages.std_msgs.String;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;
using sm = Messages.sensor_msgs;
using d = System.Drawing;
using Touch = GenericTouchTypes.Touch;
using cm = Messages.custom_msgs;
using tf = Messages.tf;
using System.Text;
using System.ComponentModel;
using otherTimer = System.Timers;
using System.Windows.Threading;
using window = DREAMPioneer.SurfaceWindow1;

#endregion

namespace DREAMPioneer
{
    public class ROSData
    {
        public int RobotNumber;
        public string Name;
        public NodeHandle node;
        public static string manualCamera, manualLaser, manualPTZ, manualVelocity;
        public static int ManualNumber;
        public Messages.geometry_msgs.Twist t;
        public Publisher<gm.Twist> joyPub;
        public Publisher<cm.ptz> servosPub;
        public Publisher<gm.PoseWithCovarianceStamped> initialPub;
        public Subscriber<sm.LaserScan> laserSub;
        public Publisher<gm.PoseArray> goalPub;
        public Subscriber<m.String> androidSub;
        public gm.PoseWithCovarianceStamped pose;
        public gm.PoseArray goal;
        public cm.ptz pt;
        public Subscriber<gm.PolygonStamped> robotsub;
        public Subscriber<gm.PoseArray> goalsub;
        public Subscriber<nm.Odometry> robotposesub;

        public RobotControl myRobot;
        public static int numRobots;
     

        public ROSData(NodeHandle n, int i)
            : this(n, i,null)
        {

        }
        public ROSData(NodeHandle n, int i,specificAndroidDelegate android)
        {
            RobotNumber = i;
            specificAndroidEvent += android;
            ROS.Init(new string[0], "DREAM");
            if (n == null)
                node = new NodeHandle();
            else
                node = n;
            Name = "/robot_brain_" + (i);
            manualCamera = Name + Name + "/rgb/image_color";
            manualLaser = "fakelaser";
            manualPTZ = Name + "/servos";
            manualVelocity = "fakevel";

            t = new gm.Twist { angular = new gm.Vector3 { x = 0, y = 0, z = 0 }, linear = new gm.Vector3 { x = 0, y = 0, z = 0 } };
            joyPub = node.advertise<gm.Twist>(manualVelocity, 1);

            pt = new cm.ptz { x = 0, y = 0, CAM_MODE = ptz.CAM_REL };
            servosPub = node.advertise<cm.ptz>(manualPTZ, 1);
            servosPub.publish(pt);

            goal = new gm.PoseArray { poses = new gm.Pose[20] };
            goalPub = node.advertise<gm.PoseArray>(Name + "/goal_list", 10);
            

            //Deprecated until I make an abstraction in ros that can publish transforms
            //pose = new gm.PoseWithCovarianceStamped()
            //{
            //    header = new m.Header { frame_id = new String("/robot_brain_2/map") },
            //    pose = new gm.PoseWithCovariance
            //    {
            //        pose = new gm.Pose { orientation = new gm.Quaternion { w = .015, x = 0, y = 0, z = 1 }, position = new gm.Point { x = 29.9, y = 3.5, z = 0 } },
            //        covariance = new double[] { .25, 0, 0, 0, 0, 0, 0, .25, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, .06853891945200942 }
            //    }
            //};

            initialPub = node.advertise<gm.PoseWithCovarianceStamped>(Name + "/initialpose", 1000);
            androidSub = node.subscribe<m.String>(Name + "/androidControl", 1, androidCallback);


            window.current.Dispatcher.Invoke(new Action(() =>
                {
                    myRobot = new RobotControl(RobotNumber);
                    myRobot.Background = Brushes.Transparent;
                    //myRobot.TopicName = Name + "/move_base/local_costmap/robot_footprint";
                    myRobot.TopicName = Name + "/local_costmap/robot_footprint";
                    window.current.SubCanvas.Children.Add(myRobot);
                    
               
                }));

            numRobots++;
            
        }

        
        public event specificAndroidDelegate specificAndroidEvent;
        public void androidCallback(m.String str)
        {
            if (specificAndroidEvent != null)
                specificAndroidEvent(RobotNumber, str);
        }

        public delegate void specificVideoDelegate(int r, sm.Image image);
        public delegate void specificLaserDelegate(int r, sm.LaserScan laserScan);
        public delegate void specificAndroidDelegate(int r, m.String str);

    }
}
