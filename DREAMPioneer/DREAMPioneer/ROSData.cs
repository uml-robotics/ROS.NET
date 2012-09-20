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
        public Publisher<gm.Twist> joyPub;
        public static Publisher<cm.ptz> servosPub;
        public Publisher<gm.PoseWithCovarianceStamped> initialPub;
        public static Subscriber<sm.LaserScan> laserSub;
        //public Subscriber<m.String> androidSub;
        public gm.PoseWithCovarianceStamped pose;
       
        public cm.ptz pt;
        public Subscriber<gm.PolygonStamped> robotsub;
        /*public Subscriber<Messages.actionlib_msgs.GoalStatusArray> goalsub { get {return WaypointHelper.PubSubs[RobotNumber].goalsub; }}
        public Publisher<Messages.move_base_msgs.MoveBaseActionGoal> goalPub { get { return WaypointHelper.PubSubs[RobotNumber].goalPub; } }
        public Publisher<Messages.actionlib_msgs.GoalID> goalCanceler { get { return WaypointHelper.PubSubs[RobotNumber].goalCanceler; } }*/
#if !TRANSPORMS
        public Subscriber<gm.PoseWithCovarianceStamped> robotposesub;
#else
        public Subscriber<nm.Odometry> robotposesub;
#endif

        public RobotControl myRobot;
        public static int numRobots;

        Subscriber<cm.robotMortality> GhostWhisperer;
        private DateTime LastBeat = DateTime.Now;
        public Timer Dethklok;
        bool IsItAlive = true;
        public bool OnLast = false;

        public void CheckMortality(object state)
        {
            if (!ROS.ok)
            {
                if (Dethklok != null)
                {
                    Dethklok.Dispose();
                    Dethklok = null;
                }
                return;
            }
            if (IsItAlive && DateTime.Now.Subtract(LastBeat).TotalMilliseconds >= 5000)
            {
                IsItAlive = false;
                    Console.WriteLine("BWUHBYE... (" + Name + ")");
                    window.current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            //UNDO WPF STUFF
                            myRobot.robot.Visibility = Visibility.Hidden;
                            myRobot.Visibility = Visibility.Hidden;
                            RobotControl.DoneCheck(RobotNumber);
                            if (window.current.selectedList.Contains(RobotNumber))
                                window.current.RemoveSelected(RobotNumber, null, "Robot Died");
                            if (ManualNumber == RobotNumber)
                                window.current.changeManual(-1);
                        }));                    
            }
            else if (!IsItAlive && DateTime.Now.Subtract(LastBeat).TotalMilliseconds < 5000)
            {
                    IsItAlive = true;
                    if (joyPub != null)
                    {
                        CheckIn();

                    }
                    window.current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        myRobot.Visibility = Visibility.Visible;
                        myRobot.robot.Visibility = Visibility.Visible;
                    }));

                    Console.WriteLine("Oh hey... we weren't talking about you... I promise. (" + Name + ")");
            }
            
        }

        private bool CheckingIn;
        public void CheckIn()
        { 
            Messages.geometry_msgs.Twist tempTwist = new Messages.geometry_msgs.Twist();
            tempTwist.linear = new Messages.geometry_msgs.Vector3();
            tempTwist.angular = new Messages.geometry_msgs.Vector3();
            tempTwist.linear.x = 0;
            tempTwist.angular.z = .5 ;
            joyPub.publish(tempTwist);
            CheckingIn = true;
            Console.WriteLine("***Checking in robot number: " + RobotNumber + "***"); 
        }
    
        public void endCheckIn()
        {
            if (CheckingIn)
            {
            Messages.geometry_msgs.Twist tempTwistStop = new Messages.geometry_msgs.Twist();
            tempTwistStop.linear = new Messages.geometry_msgs.Vector3();
            tempTwistStop.angular = new Messages.geometry_msgs.Vector3();
            tempTwistStop.linear.x = 0;
            tempTwistStop.angular.z = 0;
            joyPub.publish(tempTwistStop);
            CheckingIn = false;
            Console.WriteLine("***Ending check in for robot number: " + RobotNumber + "***"); 
        }
            
        }

        public void JagerBombs(bool FUCKINGSHOWERINTHATSHIT)
        {
            WaypointHelper.Init(SurfaceWindow1.MAX_NUMBER_OF_ROBOTS, RobotNumber, Name);

            LastBeat = DateTime.Now.Subtract(new TimeSpan(0, 0, 6));            
            //androidSub = node.subscribe<m.String>(Name + "/androidControl", 1, androidCallback);
            GhostWhisperer = node.subscribe<cm.robotMortality>(Name + "/status", 1, Heartbeat);
        }

        public ROSData(NodeHandle n, int i)
            : this(n, i, null)
        {

        }
        public ROSData(NodeHandle n, int i, specificAndroidDelegate android)
        {
            RobotNumber = i;
            //specificAndroidEvent += android;
            if (node == null)
                if (n == null)
                    node = new NodeHandle();
                else
                    node = n;
            Name = "/robot_brain_" + (i);
            joyPub = node.advertise<gm.Twist>(Name + "/virtual_joystick/cmd_vel", 1, true);

            
            /*
            manualCamera = Name + Name + "/rgb/image_color/compressed";
            manualLaser = "fakelaser";
            manualPTZ = Name + "/servos";
            manualVelocity = "fakevel";

            t = new gm.Twist { angular = new gm.Vector3 { x = 0, y = 0, z = 0 }, linear = new gm.Vector3 { x = 0, y = 0, z = 0 } };
            

            pt = new cm.ptz { x = 0, y = 0, CAM_MODE = ptz.CAM_REL };
            servosPub = node.advertise<cm.ptz>(manualPTZ, 1);
            servosPub.publish(pt);*/

            

            window.current.Dispatcher.BeginInvoke(new Action(() =>
            {
                    myRobot = new RobotControl(RobotNumber);
                    myRobot.robot.SetColor(Brushes.Transparent);
                    myRobot.TopicName = Name + "/move_base/local_costmap/robot_footprint";
                    //myRobot.TopicName = Name + "/local_costmap/robot_footprint";
                    window.current.SubCanvas.Children.Add(myRobot);
                }));

            JagerBombs(true);

            Dethklok = new Timer(CheckMortality, null, 0, 50);
            numRobots++;

        }

        public void Heartbeat(cm.robotMortality Life)
        {
          LastBeat = DateTime.Now;
        }

        public static void unSub()
        {
            SurfaceWindow1.current.ROSStuffs[ROSData.ManualNumber].joyPub.publish(new Messages.geometry_msgs.Twist { 
                linear = new Messages.geometry_msgs.Vector3 { x = 0 }, 
                angular = new Messages.geometry_msgs.Vector3 { z = 0 } 
            });
            SurfaceWindow1.current.ROSStuffs[ROSData.ManualNumber].joyPub.shutdown();
            SurfaceWindow1.current.ROSStuffs[ROSData.ManualNumber].joyPub = null;
            servosPub.publish(new ptz { x = 0, y = 0, CAM_MODE = ptz.CAM_ABS });
            servosPub.shutdown();
            servosPub = null;
            laserSub.shutdown();
            laserSub = null;
            ROS_ImageWPF.CompressedImageControl.newTopicName = ROSData.manualCamera;
        }
        public static void reSub()
        {
            SurfaceWindow1.current.ROSStuffs[ROSData.ManualNumber].joyPub = node.advertise<gm.Twist>(manualVelocity, 1, true);
            servosPub = node.advertise<cm.ptz>(manualPTZ, 1, true);
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
