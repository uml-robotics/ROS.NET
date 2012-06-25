#region

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
//using Point = System.Drawing.Point;
using Point = System.Windows.Point;
using Size = System.Windows.Size;
using d = System.Drawing;
using Messages;
using Messages.custom_msgs;
using Ros_CSharp;
using XmlRpc_Wrapper;
using Int32 = Messages.std_msgs.Int32;
using PixelFormat = System.Windows.Media.PixelFormat;
//using String = Messages.std_msgs.String;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;
using sm = Messages.sensor_msgs;
using Rectangle = System.Windows.Shapes.Rectangle;
using System.ComponentModel;
using System.Windows.Data;
#endregion

namespace ROS_ImageWPF
{
    public partial class RobotControl : UserControl
    {
        //pixels per meter, and meters per pixel respectively. This is whatever you have the map set to on the ROS side. These variables are axtually wrong, PPM is meters per pixel. Will fix...
        private static float PPM = 0.02868f;
        private static float MPP = 1.0f / PPM;
        public double xPos;
        public double yPos;
        public double transx;
        public double transy;
        public double scalex;
        public double scaley;
        public bool sendnext;

        public List<Point> waypoint = new List<Point>();

        private Publisher<gm.PoseStamped> goalPub;
        public string TopicName
        {
            get { return GetValue(TopicProperty) as string; }
            set { SetValue(TopicProperty, value); }
        }


        private Thread waitforinit;
        private static NodeHandle imagehandle;
        private Subscriber<TypedMessage<gm.PolygonStamped>> robotsub;
        private Subscriber<TypedMessage<gm.PoseStamped>> goalsub;
        private Subscriber<TypedMessage<gm.PoseWithCovarianceStamped>> robotposesub;
        public static readonly DependencyProperty TopicProperty = DependencyProperty.Register(
            "Topic",
            typeof(string),
            typeof(RobotControl),
            new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.None, (obj, args) =>
                {
                    if (obj is RobotControl)
                    {
                       RobotControl target = obj as RobotControl;
                        target.TopicName = (string)args.NewValue;
                        if (!ROS.isStarted())
                        {
                            if (target.waitforinit == null)
                                target.waitforinit = new Thread(new ThreadStart(target.waitfunc));
                            if (!target.waitforinit.IsAlive)
                            {
                                target.waitforinit.Start();
                            }
                        }
                        else
                            target.SetupTopic();
                    }
                }));

        private void waitfunc()
        {
            while (!ROS.initialized)
            {
                Thread.Sleep(100);
            }
            Dispatcher.BeginInvoke(new Action(SetupTopic));
        }

        private void SetupTopic()
        {
            
            if (imagehandle == null)
                imagehandle = new NodeHandle();
            if (robotsub != null)
                robotsub.shutdown();
            updatePOS(30.0 * MPP, 3.3 * MPP);
            

            goalPub = imagehandle.advertise<gm.PoseStamped>("/robot_brain_1/move_base_simple/goal", 1000);

            robotposesub = imagehandle.subscribe<gm.PoseWithCovarianceStamped>("/robot_brain_1/amcl_pose",1,(k)=>
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    double x = (k.data.pose.pose.position.x) * (double)MPP;
                    double y = (k.data.pose.pose.position.y) * (double)MPP;
                    updatePOS(x, y);
                    
                })));

            goalsub = imagehandle.subscribe<gm.PoseStamped>("/robot_brain_1/move_base_simple/goal",1,(j)=>
                 Dispatcher.BeginInvoke(new Action(() =>
                 {
                     double x = (j.data.pose.position.x  ) * (double)MPP;
                     double y = (j.data.pose.position.y ) * (double)MPP;
                     updateGoal(x, y);
                 })));


            robotsub = imagehandle.subscribe<gm.PolygonStamped>(TopicName, 1, (i) =>
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    gm.Vector3 vec;
                    gm.Quaternion quat;
                    tf_node.transformFrame("/robot_brain_1/odom","/robot_brain_1/map",out vec,out quat);
                    float x = ((float)vec.x ) * MPP;
                    float y = ( (float)vec.y ) * MPP;
                    if (waypoint.Count > 0)
                    {
                        Point p = new Point(x, y);
                        if (compare(p, waypoint[0]))
                        {
                            waypoint.RemoveAt(0);
                            sendnext = true;
                        }
                        if (waypoint.Count > 0 && sendnext)
                        {
                            //Console.WriteLine((waypoint[0].X - transx) / scalex * PPM + " " + (waypoint[0].Y - transy) / scaley * PPM);
                            goalPub.publish(new gm.PoseStamped { header = new m.Header { frame_id = new m.String { data = "/robot_brain_1/map" } }, 
                                pose = new gm.Pose { position = new gm.Point { x = (waypoint[0].X - transx) / scalex * PPM, y = (waypoint[0].Y - transy) / scaley * PPM, z = 0 }, 
                                    orientation = new gm.Quaternion { w = 1, x = 0, y = 0, z = 0 } } });
                            sendnext = false;
                        }
                    }
                    //updatePOS(x,y);
                })), "*");
        }

        public void updateWaypoints(List<Point> wayp, double x, double y, double xx, double yy)
        {

            lock (waypoint)
            {
                foreach (Point p in wayp)
                {
                    waypoint.Add(p);
                }
            }
            transx = x;
            transy = y;
            scalex = xx;
            scaley = yy;
            sendnext = true;
        }

        private void updatePOS(float x, float y)
        {
            if (x + y > 0 || x + y < 0)
            {
                xPos = x;
                yPos = y;
                robot.Margin = new Thickness { Left = x, Bottom = 0, Right = 0, Top = y };
            }
        }

        private void updatePOS(double x, double y)
        {
            if (x + y > 0 || x + y < 0)
            {
                xPos = x;
                yPos = y;
                robot.Margin = new Thickness { Left = x, Bottom = 0, Right = 0, Top = y };
            }
        }

        private bool compare(Point pos, Point waypoint)
        {
            if (distance(pos, waypoint) < 40)
                return true;
            else return false;
        }
        public double distance(Point q, Point p)
        {
            return distance(q.X + transx, q.Y + transy, p.X , p.Y);
        }

        public double distance(double x2, double y2, double x1, double y1)
        {
            return Math.Sqrt(
                (x2 - x1) * (x2 - x1)
                + (y2 - y1) * (y2 - y1));
        }

        public void SetColor(System.Windows.Media.SolidColorBrush color)
        {
            robot.Fill = color;
        }
        public void SetOpacity(Double opa)
        {
            //robot.Opacity = opa;
        }

        private void updateGoal(double x, double y)
        {
            goal.Margin = new Thickness { Left = x, Bottom = 0, Right = 0, Top = y };
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref = "ImageControl" /> class. 
        ///   constructor... nothing fancy
        /// </summary>
        public RobotControl()
        {
            InitializeComponent();
        }

        #region Events

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string strPropertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(strPropertyName));
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            //image.Transform = new ScaleTransform(1, -1, ActualWidth / 2, ActualHeight / 2);
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //image.Transform = new ScaleTransform(1, -1, ActualWidth / 2, ActualHeight / 2);
        }
        #endregion
    }
}