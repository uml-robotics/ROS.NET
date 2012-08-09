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
        public Point PositionInWindow
        {
            get
            {                
                Point ret = myRobot.robot.TranslatePoint(new Point(myRobot.robot.Width / 2, myRobot.robot.Height / 2), SurfaceWindow1.current);
                return ret;
            }
        }
        public double RadiusInWindow
        {
            get
            {
                Point p = myRobot.robot.TranslatePoint(new Point(myRobot.robot.Width / 2, myRobot.robot.Height / 2), SurfaceWindow1.current);
                Point q = myRobot.robot.TranslatePoint(new Point(0, myRobot.robot.Height / 2), SurfaceWindow1.current);
                double dx = p.X - q.X;
                dx *= dx;
                double dy = p.Y - q.Y;
                dy *= dy;
                return Math.Sqrt(dx + dy);
            }
        }

        
    

        public int RobotNumber;
        public string Name;
        public static NodeHandle node;
        public static string manualCamera, manualLaser, manualPTZ, manualVelocity;
        public static int ManualNumber = -1;
        public Messages.geometry_msgs.Twist t;
        public static Publisher<gm.Twist> joyPub;
        public static Publisher<cm.ptz> servosPub;
        public Publisher<gm.PoseWithCovarianceStamped> initialPub;
        public static Subscriber<sm.LaserScan> laserSub;
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

        Subscriber<cm.robotMortality> GhostWhisperer;
        private DateTime LastBeat;
        public Timer Dethklok; 
        bool IsItAlive = true;

        public void CheckMortality(object state)
        {
            if (DateTime.Now.Subtract(LastBeat).TotalMilliseconds > 2000)
            {
                if (IsItAlive)
                {
                    Console.WriteLine("He was an asshole, anyways... ("+Name+")");
                    IsItAlive = false;
                    window.current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            //UNDO WPF STUFF
                            myRobot.robot.Visibility = Visibility.Hidden;
                            RobotControl.DoneCheck(RobotNumber);
                            if (window.current.selectedList.Contains(RobotNumber))
                                window.current.RemoveSelected(RobotNumber, null, "Robot Died");
                            if (ManualNumber == RobotNumber)
                                window.current.changeManual(-1);
                        }));
                }
            }
            else
            {
                if (!IsItAlive)
                {
                    Console.WriteLine("Oh hey... we weren't talking about you... I promise. (" + Name + ")");
                    IsItAlive = true;
                    window.current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        myRobot.robot.Visibility = Visibility.Visible;
                    }));
                }
            }
        }

        public void JagerBombs(bool FUCKINGSHOWERINTHATSHIT)
        {
                LastBeat = DateTime.Now;
                goalPub = node.advertise<gm.PoseArray>(Name + "/goal_list", 10);
                androidSub = node.subscribe<m.String>(Name + "/androidControl", 1, androidCallback);
                GhostWhisperer = node.subscribe<cm.robotMortality>(Name + "/status", 1, Heartbeat);
        }

        public ROSData(NodeHandle n, int i)
            : this(n, i, null)
        {

        }
        public ROSData(NodeHandle n, int i, specificAndroidDelegate android)
        {
            RobotNumber = i;
            specificAndroidEvent += android;
            if (node == null)
                if (n == null)
                    node = new NodeHandle();
                else
                    node = n;
            Name = "/robot_brain_" + (i);
            /*
            manualCamera = Name + Name + "/rgb/image_color/compressed";
            manualLaser = "fakelaser";
            manualPTZ = Name + "/servos";
            manualVelocity = "fakevel";

            t = new gm.Twist { angular = new gm.Vector3 { x = 0, y = 0, z = 0 }, linear = new gm.Vector3 { x = 0, y = 0, z = 0 } };
            joyPub = node.advertise<gm.Twist>(manualVelocity, 1);

            pt = new cm.ptz { x = 0, y = 0, CAM_MODE = ptz.CAM_REL };
            servosPub = node.advertise<cm.ptz>(manualPTZ, 1);
            servosPub.publish(pt);*/

            goal = new gm.PoseArray { poses = new gm.Pose[20] };            

            window.current.Dispatcher.Invoke(new Action(() =>
                {
                    myRobot = new RobotControl(RobotNumber);
                    myRobot.Background = Brushes.Transparent;
                    myRobot.TopicName = Name + "/move_base/local_costmap/robot_footprint";
                    //myRobot.TopicName = Name + "/local_costmap/robot_footprint";
                    window.current.SubCanvas.Children.Add(myRobot);
                }));

            JagerBombs(true);

            Dethklok = new Timer(CheckMortality,null,500,500);
            numRobots++;

        }

        public void Heartbeat(cm.robotMortality Life)
        {
            LastBeat = DateTime.Now;
        }

        public static void unSub()
        {
            joyPub.publish(new Messages.geometry_msgs.Twist { linear = new Messages.geometry_msgs.Vector3 { x = 0 }, angular = new Messages.geometry_msgs.Vector3 { z = 0 } });
            joyPub.shutdown();
            joyPub = null;
            servosPub.publish(new ptz { x = 0, y = 0, CAM_MODE = ptz.CAM_ABS });
            servosPub.shutdown();
            servosPub = null;
            laserSub.shutdown();
            laserSub = null;
            ROS_ImageWPF.CompressedImageControl.newTopicName = ROSData.manualCamera;
        }
        public static void reSub()
        {
            joyPub = node.advertise<gm.Twist>(manualVelocity, 10, true);
            servosPub = node.advertise<cm.ptz>(manualPTZ, 10);
            ROS_ImageWPF.CompressedImageControl.newTopicName = ROSData.manualCamera;             
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
