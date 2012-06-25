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
using Point = System.Drawing.Point;
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
        //pixels per meter, and meters per pixel respectively. This is whatever you have the map set to on the ROS side
        private static float PPM = 0.02868f;
        private static float MPP = 1.0f / PPM;
        public float xPos;
        public float yPos;

        public string TopicName
        {
            get { return GetValue(TopicProperty) as string; }
            set { SetValue(TopicProperty, value); }
        }

        private Thread waitforinit;
        private static NodeHandle imagehandle;
        private Subscriber<gm.PolygonStamped> robotsub;
        private Subscriber<gm.PoseStamped> goalsub;

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

            goalsub = imagehandle.subscribe<gm.PoseStamped>("/robot_brain_1/move_base_simple/goal",1,(j)=>
                 Dispatcher.BeginInvoke(new Action(() =>
                 {
                     double x = (j.pose.position.x  ) * (double)MPP;
                     double y = (j.pose.position.y ) * (double)MPP;
                     updateGoal(x, y);
                 })));

            robotsub = imagehandle.subscribe<gm.PolygonStamped>(TopicName, 1, (i) =>
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    gm.Vector3 vec;
                    gm.Quaternion quat;
                    tf_node.transformFrame("/robot_brain_1/odom","/robot_brain_1/map",out vec,out quat);
                    
                    float x = (i.polygon.points[0].x - 0.19f + (float)vec.x ) * MPP;
                    float y = (i.polygon.points[0].y - 0.19f + (float)vec.y) * MPP;
                    updatePOS(x,y);
                })), "*");
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