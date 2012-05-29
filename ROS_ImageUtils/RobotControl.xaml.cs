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
         private float _xPos;
        private float _yPos;
        private Thickness pos;
        public float xPos
        {
            get { return _xPos; }
            set 
            {
                _xPos = value;
                pos.Left = _xPos;
                this.OnPropertyChanged("pos");
            }
        }
        public float yPos
        {
            get { return _yPos; }
            set 
            { 
                _yPos = value;
                pos.Bottom = _xPos;
                this.OnPropertyChanged("pos");
            }
        } 

        public string TopicName
        {
            get { return GetValue(TopicProperty) as string; }
            set { SetValue(TopicProperty, value); }
        }



        private Thread waitforinit;
        private static NodeHandle imagehandle;
        private Subscriber<TypedMessage<gm.PolygonStamped>> robotsub;


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
            
            robotsub = imagehandle.subscribe<gm.PolygonStamped>(TopicName, 1, (i) =>
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    //tf_node tf = new tf_node();
                    gm.Vector3 vec ;//= new gm.Vector3();
                    gm.Quaternion quat;// = new gm.Quaternion();
                    tf_node.transformFrame("/robot_brain_1/base_link","/robot_brain_1/map",out vec,out quat);
                    
                    //Console.WriteLine(i.data.polygon.points[0].y);
                    float x = i.data.polygon.points[0].x - 0.19f + (float)vec.x;
                    float y = i.data.polygon.points[0].y - 0.19f + (float)vec.y;
                    updatePOS(x,y);
                })), "*");
        }

        private void updatePOS(float x, float y)
        {
            robot.Margin = new Thickness { Left = x, Bottom = 0, Right = 0, Top = y }; //(400,0,0,0);
            
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