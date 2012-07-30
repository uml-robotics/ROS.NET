
#region

using System;
using System.Collections.Generic;
//using System.Drawing;
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
using window = DREAMPioneer.SurfaceWindow1;
#endregion

namespace DREAMPioneer
{
    public partial class RobotControl : UserControl
    {
        public double xPos;
        public double yPos;
        public double transx;
        public double transy;
        public double scalex;
        public double scaley;
        public bool sendnext;


        public static List<CommonList> OneInAMillion = new List<CommonList>();

        private Publisher<gm.PoseArray> goalPub;
        public string TopicName
        {
            get { return GetValue(TopicProperty) as string; }
            set { SetValue(TopicProperty, value); }
        }


        private Thread waitforinit;
        private static NodeHandle imagehandle;
        private Subscriber<gm.PolygonStamped> robotsub;
        private Subscriber<gm.PoseArray> goalsub;
        private Subscriber<nm.Odometry> robotposesub;
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
            SetupTopic(2);
        }
        private void SetupTopic(int index)
        {
            lock(window.current.ROSStuffs)
            if (!window.current.ROSStuffs.ContainsKey(index))
                window.current.ROSStuffs.Add(index, new ROSData(new NodeHandle(), index));

            ROSData myData = window.current.ROSStuffs[index];
            if (imagehandle == null)
                imagehandle = new NodeHandle();
            if (robotsub != null)
                robotsub.shutdown();
            robot.SetSize(10, 10);
            updatePOS(0, 0);


            myData.goalPub = imagehandle.advertise<Messages.custom_msgs.arrayofdeez>(myData.Name + "/goal_list", 1000);

            myData.robotposesub = imagehandle.subscribe<nm.Odometry>(myData.Name + "/odom", 1, (k) =>
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    gm.Vector3 vec;
                    gm.Quaternion quat;
                    tf_node.transformFrame(myData.Name + "/odom", myData.Name + "/map", out vec, out quat);
                    double x = (vec.x) * (double)ROS_ImageWPF.MapControl.PPM;
                    double y = (vec.y) * (double)ROS_ImageWPF.MapControl.PPM;
                    double t = convert(quat).z;
                    //Console.WriteLine(t +"\t"+quat.w+"\t"+quat.z);
                    updatePOS(x, y, t);
                    
                })),"*");

            /*new Thread(() =>
                {
                    while (true)
                    {

                        Thread.Sleep(100);
                    }
                });*/

            myData.goalsub = imagehandle.subscribe<Messages.custom_msgs.arrayofdeez>(myData.Name + "/goal_progress", 1, (j) =>
                 Dispatcher.BeginInvoke(new Action(() =>
                 {
                     int i = 0;
                     List<Point> points = new List<Point>(j.nuts.Length);
                     foreach (gm.Pose P in j.nuts)
                     {
                         double x = (P.position.x) * (double)ROS_ImageWPF.MapControl.PPM;
                         double y = (P.position.y) * (double)ROS_ImageWPF.MapControl.PPM;
                         points.Add(new Point(x,y));
                     }
                     updateGoal(points,i);
                 })));


            myData.robotsub = imagehandle.subscribe<gm.PolygonStamped>(TopicName, 1, (i) =>
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    gm.Vector3 vec;
                    gm.Quaternion quat;
                    tf_node.transformFrame(myData.Name + "/odom", myData.Name + "/map", out vec, out quat);
                    float x = (i.polygon.points[0].x - 0.19f + (float)vec.x) * ROS_ImageWPF.MapControl.PPM;
                    float y = (i.polygon.points[0].y - 0.19f + (float)vec.y) * ROS_ImageWPF.MapControl.PPM;
                    Point p = new Point(x, y);                                  
                    foreach(CommonList CL in OneInAMillion)
                    {
                        
                        if (CL.P_List.Count > 0 && compare(p, CL.P_List[0]))
                        {
                            CL.P_List.RemoveAt(0);
                            sendnext = true;
                        }
                        if (CL.P_List.Count > 0 && sendnext)
                        {
                            //Console.WriteLine((waypoint[0].X - transx) / scalex * PPM + " " + (waypoint[0].Y - transy) / scaley * PPM);
                          goalPub.publish(new gm.PoseArray
                            {
                                header = new m.Header { frame_id = new m.String { data = myData.Name + "/map" } },
                                poses = new gm.PoseArray().poses
                            });
                            sendnext = false;
                        }
                    }
                    updatePOS(x,y);
                    })),"*");
        }

       public gm.Vector3 convert(gm.Quaternion q) {
            gm.Vector3 ret = new gm.Vector3();
            double sqw = q.w*q.w;
            double sqx = q.x*q.x;
            double sqy = q.y*q.y;
            double sqz = q.z*q.z;
            double unit = sqx + sqy + sqz + sqw; //normalization coefficient
            double test = q.x*q.y + q.z*q.w;
        if (Math.Abs(test) > Math.Abs(0.499*unit)) { // singularity at north pole
                int sign  = Math.Sign(test);
                ret.y = 2*sign*Math.Atan2(q.x,q.w);
                ret.z = sign*Math.PI/2;
              ret.x = 0;
         return ret;
 }
        ret.y = Math.Atan2(2*q.y*q.w-2*q.x*q.z , sqx - sqy - sqz + sqw);
        ret.z = Math.Asin(2*test/unit);
        ret.x = Math.Atan2(2 * q.x * q.w - 2 * q.y * q.z, -sqx + sqy - sqz + sqw);
    return ret;
}
        public void updateWaypoints(List<Point> wayp, double x, double y, double xx, double yy)
        {
            
            Dispatcher.Invoke(new Action(() =>
                {
                    CheckUnique(wayp, robot.ID);
                }));
            
            transx = x;
            transy = y;
            scalex = xx;
            scaley = yy;
            sendnext = true;
        }
        public void updatePOS(float x, float y)
        {
            updatePOS(x, y, 0);
        }

        public void updatePOS(float x, float y, float t)
        {
            //if (x + y > 0 || x + y < 0) // <- this appears strange, what are you tring to test for here? coule you use (x+y != 0)?
            {
                xPos = x + window.current.map.Width / 2;
                yPos = y + window.current.map.Height / 2;
                Canvas.SetLeft(robot, x - robot.Width / 2 + window.current.map.Width/2);
                Canvas.SetTop(robot, y - robot.Height / 2 + window.current.map.Height/2);
                robot.SetSize(10, 10);
                robot.Theta = t;
            }
        }

        public void updatePOS(double x, double y)
        {
            updatePOS(x, y, 0);
        }
        public void updatePOS(double x, double y, double t)
        {
           //if (x + y > 0 || x + y < 0)  // <- this appears strange, what are you tring to test for here? coule you use (x+y != 0)?
            {
                xPos = x + window.current.map.Width / 2;
                yPos = y + window.current.map.Height / 2;
                Canvas.SetLeft(robot, x - robot.Width / 2 + window.current.map.Width/2);
                Canvas.SetTop(robot, y - robot.Height / 2 + window.current.map.Height/2);
                robot.SetSize(10, 10);
                robot.Theta = t;

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
            return distance(q.X + transx, q.Y + transy, p.X, p.Y);
        }

        public double distance(double x2, double y2, double x1, double y1)
        {
            return Math.Sqrt(
                (x2 - x1) * (x2 - x1)
                + (y2 - y1) * (y2 - y1));
        }

        public void SetColor(System.Windows.Media.SolidColorBrush color)
        {
            robot.Dot.Fill = color;
           
        }
        public void SetOpacity(Double opa)
        {
            robot.Dot.Opacity = opa;
         
        }

        private void updateGoal(List<Point> points, int index)
        {
            window.current.Dispatcher.Invoke(new Action(() =>
                {
                    bool unique = CheckUnique(points, index);
                }));
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref = "ImageControl" /> class. 
        ///   constructor... nothing fancy
        /// </summary>
        public RobotControl()
        {
            InitializeComponent();
            robot.ID = 0; //TEMP CODE
        }

         

        public bool CheckUnique(List<Point> P_List, int R)
        {
            
            if (P_List.Count == 0) return false;
            CommonList DisList;
            if (OneInAMillion.Count == 0)
            {
                //IT IS UNIQUE
                
                DisList = new CommonList(P_List, R,robot.Arrow.Fill, 1);
                OneInAMillion.Add(DisList);

                window.current.AddGoalDots(P_List, DisList.Dots, robot.Arrow.Fill);
                if (DisList.Dots.Count >= 2)
                    DisList.Dots[0].NextOne = true;
                foreach (Robot_Info RI in DisList.RoboInfo)
                    if (RI.RoboNum == R)
                        window.current.SetGoal(R, P_List, DisList, RI);

                return true;
            }

            foreach (CommonList CL in OneInAMillion)
            {

                //It's not unique beacause a path already exists for this robot.

                foreach (Robot_Info RI in CL.RoboInfo)
                    if (RI.RoboNum == R && !RI.done)
                    {

                        window.current.SetGoal(R, P_List, CL, RI);


                        return false;
                    }

                int j;

                if (P_List.Count < CL.P_List.Count)
                {
                    j = CL.P_List.Count - P_List.Count;
                    for (int i = P_List.Count - 1; i > 0; i--)
                    {
                        if (i == 1)
                            if (P_List[i] == CL.P_List[i + j])
                            {

                                //NOT UNIQUE Shorter



                                CL.RoboInfo.Add(new Robot_Info(R, P_List.Count, robot.Arrow.Fill, CL.RoboInfo[CL.RoboInfo.Count - 1].Position + 1));
                                foreach (Robot_Info RI in CL.RoboInfo)
                                    if (RI.RoboNum == R)
                                       window.current.SetGoal(R, P_List, CL, RI);

                                return false;
                            }

                            else if (P_List[i] != CL.P_List[i + j]) break;
                    }
                }
                else
                {
                    j = P_List.Count - CL.P_List.Count;
                    for (int i = CL.P_List.Count - 1; i > 0; i--)
                    {
                        if (i == 1)
                            if (P_List[i + j] == CL.P_List[i])
                            {
                                //NOT UNIQUE Longer


                                CL.RoboInfo.Add(new Robot_Info(R, P_List.Count, robot.Arrow.Fill, CL.RoboInfo[CL.RoboInfo.Count - 1].Position + 1));
                               //List<Point> Better_List = new List<Point>(P_List.Except<Point>(CL.P_List));

                                CL.P_List.Clear();
                                CL.P_List = P_List;
                               // window.current.AddGoalDots(Better_List, CL.Dots, robot.Arrow.Fill);
                                foreach (Robot_Info RI in CL.RoboInfo)
                                    if (RI.RoboNum == R)
                                        window.current.SetGoal(R, P_List, CL, RI);
                                return false;
                            }
                            else if (P_List[i + j] != CL.P_List[i]) break;
                    }
                }
            }
            //IT IS UNIQUE


            DisList = new CommonList(P_List, R, robot.Arrow.Fill, 1);
            OneInAMillion.Add(DisList);

            window.current.AddGoalDots(P_List, DisList.Dots, robot.Arrow.Fill);
            DisList.Dots[1].NextOne = true;
            foreach (Robot_Info RI in DisList.RoboInfo)
                if (RI.RoboNum == R)
                    window.current.SetGoal(R, P_List, DisList, RI);

            return true;
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