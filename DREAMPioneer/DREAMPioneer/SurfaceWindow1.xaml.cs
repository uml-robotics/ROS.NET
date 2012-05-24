#region License stuff

// Eric McCann - 2011
// University of Massachusetts Lowell
// 
// 
// The DREAMController is intellectual property of the University of Massachusetts lowell, and is patent pending.
// 
// Your rights to distribute, videotape, etc. any works that make use of the DREAMController are entirely contingent on the specific terms of your licensing agreement.
// 
// Feel free to edit any of the supplied samples, or reuse the code in other projects that make use of the DREAMController. They are provided as a resource.
// 
// 
// For license-related questions, contact:
// 	Kerry Lee Andken
// 	kerrylee_andken@uml.edu
// m
// For technical questions, contact:
// 	Eric McCann
// 	emccann@cs.uml.edu
// 	
// 

#endregion

#region USINGZ

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using DREAMController;
using GenericTouchTypes;
//using WPFImageHelper;
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
using EM3MTouchLib;
using System.ComponentModel;

#endregion

namespace DREAMPioneer
{

    
    /// <summary>
    ///   Interaction logic for SurfaceWindow1.xaml
    /// </summary>
    public partial class SurfaceWindow1 : Window//, INotifyPropertyChanged
    {
        private const string ROS_MASTER_URI = "http://robot-brain-1:11311/";
        public static SurfaceWindow1 current;
        private SortedList<int, SlipAndSlide> captureVis = new SortedList<int, SlipAndSlide>();
        private JoystickManager joymgr;
        private NodeHandle node;

        private Messages.geometry_msgs.Twist t;
        private cm.ptz pt;
        private EM3MTouch em3m;
        private DateTime currtime;
        private tf_node _tf;

        private Publisher<gm.Twist> joyPub;
        private Publisher<cm.ptz> servosPub;
        private Subscriber<TypedMessage<sm.LaserScan>> laserSub;
        private Subscriber<TypedMessage<tf.tfMessage>> tfSub;

        private Subscriber<TypedMessage<cm.cgeo>> tpub;
        private Subscriber<TypedMessage<gm.TransformStamped>> tsub;
        /// <summary>
        ///   Default constructor.
        /// </summary>
        public SurfaceWindow1()
        {
            current = this;
            InitializeComponent();

            em3m = new EM3MTouch();
            if (!em3m.Connect())
            {
                Console.WriteLine("IT'S BORKED OH NO!");
            }
            else
            {
                em3m.DownEvent += Down;
                em3m.ChangedEvent += Changed;
                em3m.UpEvent += Up;
                
            }
            //_tf = new tf_node();
        }

        private void rosStart()
        {            
            ROS.ROS_MASTER_URI = "http://10.0.2.41:11311";
            Console.WriteLine("CONNECTING TO ROS_MASTER URI: " + ROS.ROS_MASTER_URI);
            ROS.ROS_HOSTNAME = "10.0.2.163";
            ROS.Init(new string[0], "DREAM");
            node = new NodeHandle();            
            t = new gm.Twist { angular = new gm.Vector3 { x = 0, y = 0, z = 0 }, linear = new gm.Vector3 { x = 1, y = 0, z = 0 } };
            pt = new cm.ptz { x = 0, y = 0, CAM_MODE = ptz.CAM_ABS };
            joyPub = node.advertise<gm.Twist>("/robot_brain_1/virtual_joystick/cmd_vel", 1000);
            servosPub = node.advertise<cm.ptz>("/robot_brain_1/servos", 1000);
            laserSub = node.subscribe<sm.LaserScan>("/robot_brain_1/filtered_scan", 1000, laserCallback);
            //tsub = node.subscribe<gm.TransformStamped>("/wtf", 1000, zomgCallback);
            //tpub = node.subscribe<cm.cgeo>("/tf", 1000, cmCallback);
            //tfSub = node.subscribe<tf.tfMessage>("/tf", 1000, tfCallback);
            //robotsub = node.subscribe<gm.PolygonStamped>("/robot_brain_1/robot_brain_1/move_base/local_costmap/robot_footprint" , 1000, robotCallback);
            //wtfsub = node.subscribe<m.Time>("/wtf", 1000, wtfCallback);
            currtime = DateTime.Now; 
        }

        public void zomgCallback(TypedMessage<gm.TransformStamped> msg)
        {
            Console.WriteLine(msg.data.transform.rotation.x);
        }
        public void cmCallback(TypedMessage<cm.cgeo> msg)
        {
            Console.WriteLine(msg.data.vec);
        }
        private void tfCallback(TypedMessage<tf.tfMessage> msg)
        {
            Console.WriteLine("GOT TF!");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            joymgr = new JoystickManager(this, MainCanvas);
            Console.WriteLine("Loading Window");
            joymgr.Joystick += joymgr_Joystick;
            joymgr.UpEvent += JoymgrFireUpEvent;
            joymgr.SetDesiredControlPanelWidthToHeightRatios(1, 1.333);
            
            //tell the joystick manager HOW TO INITIALIZE YOUR CUSTOM CONTROL PANELS... and register for events, if your control panel fires them
            //EVERY JOYSTICK'S CONTROL PANEL IS (now) A NEW INSTANCE! TO MAKE VALUES PERSIST, USE STATICS OR A STORAGE CLASS!
            joymgr.InitControlPanels(
                () =>
                {
                    LeftControlPanel lp = new LeftControlPanel();
                    lp.Init(this);
                    return (ControlPanel)lp;
                },
                () =>
                {
                    RightControlPanel rp = new RightControlPanel();
                    rp.Init(this);
                    return (ControlPanel)rp;
                });



            rosStart();
        }

        private void JoymgrFireUpEvent(Touch e)
        {
            if (captureVis.ContainsKey(e.Id))
                captureVis.Remove(captureVis[e.Id].DIEDIEDIE());
        }

        private void joymgr_Joystick(bool RightJoystick, double rx, double ry)
        {
            if (!RightJoystick)
            {
                t.linear.x = ry / -100.0;
                t.angular.z = rx / -100.0;
                joyPub.publish(t);
            }
            else
            {
                if(currtime.Ticks + (long)(Math.Pow(10,6)) <= ( DateTime.Now.Ticks ))
                {
                    pt.x = (float)(rx/ 10.0);
                    pt.y = (float)(ry/ -10.0);
                    pt.CAM_MODE = ptz.CAM_REL;
                    servosPub.publish(pt);
                    currtime = DateTime.Now;
                }
            }
        }

        private void joymgr_Button(int action, bool down)
        {
           // Console.WriteLine("" + action + " = " + down);
        }

        protected override void OnClosed(EventArgs e)
        {
            if (joymgr != null)
            {
                joymgr.Close();
                joymgr = null;
            }
            base.OnClosed(e);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Q && e.KeyStates == KeyStates.Down)
                Close();
        }

        public void robotCallback(TypedMessage<gm.PolygonStamped> poly)
        {
            
            //xPos = (poly.data.polygon.points[0].x /*- 0.19f*/) * 100;
            //yPos = (poly.data.polygon.points[0].y /*- 0.19f*/) * 100;
        }

        public void videoCallback(TypedMessage<sm.Image> image)
        {
            
            d.Size size = new d.Size();
            size.Height = (int)image.data.height;
            size.Width = (int)image.data.width;

//            t1 = t2;
//            t2 = DateTime.Now;
//            double fps = 1000.0 / (t2.Subtract(t1).Milliseconds);
//            Console.WriteLine(fps);

            Dispatcher.BeginInvoke(new Action(() => { RightControlPanel rcp = joymgr.RightPanel as RightControlPanel; if (rcp != null)
            
                rcp.webcam.UpdateImage(image.data.data, new System.Windows.Size(size.Width, size.Height), false); }));

            //Console.WriteLine("UPDATING ZE IMAGES!");
        }
        public void wtfCallback(TypedMessage<nm.OccupancyGrid> pos)
        {
            Console.WriteLine("WE COOL!");
        }
        public void laserCallback(TypedMessage<sm.LaserScan> laserScan)
        {
            double[] scan = new double[laserScan.data.ranges.Length];
            for (int i = 0; i < laserScan.data.ranges.Length; i++)
            {
                scan[i] = laserScan.data.ranges[i];
            }
            Dispatcher.BeginInvoke(new Action(() => {
                LeftControlPanel lcp = joymgr.LeftPanel as LeftControlPanel;
                if (lcp != null)
                    lcp.newRangeCanvas.SetLaser(scan, laserScan.data.angle_increment, laserScan.data.angle_min); 
            }));
        }

        private void pan()
        {

        }

        private void zoom()
        {

        }
        
        private void Down(Touch e)
        {
            joymgr.Down(e, (t, b) =>
                                        {
                                            if (!b)
                                            {
                                                captureVis.Add(t.Id, new SlipAndSlide(t));
                                                captureVis[t.Id].dot.Stroke = Brushes.White;
                                            }
                                        });
        }

        private void Changed(Touch e)
        {
            joymgr.Change(e, (t, b) =>
                                            {
                                                if (!b)
                                                {
                                                    if (captureVis.ContainsKey(t.Id))
                                                    {
                                                        captureVis[t.Id].dot.Stroke = Brushes.White;
                                                        captureVis[t.Id].Update(t.Position);
                                                    }
                                                }
                                                else
                                                {
                                                    if (captureVis.ContainsKey(t.Id))
                                                    {
                                                        captureVis[t.Id].dot.Stroke = Brushes.Red;
                                                    }
                                                }
                                            });
        }

        private void Up(Touch e)
        {
            joymgr.Up(e, (t, b) =>
                                    {
                                        if (!b)
                                        {
                                            if (captureVis.ContainsKey(t.Id))
                                            {
                                                captureVis.Remove(captureVis[t.Id].DIEDIEDIE());
                                            }
                                        }
                                        else
                                        {
                                            if (captureVis.ContainsKey(t.Id))
                                            {
                                                captureVis.Remove(captureVis[t.Id].DIEDIEDIE());
                                            }
                                        }
                                    });
        }

        #region Nested type: SlipAndSlide

        public class SlipAndSlide
        {
            public Ellipse dot;
            private int id;
            private TranslateTransform trans = new TranslateTransform();

            public SlipAndSlide(Touch e)
            {
                id = e.Id;
                dot = new Ellipse
                          {
                              Stroke = Brushes.White,
                              StrokeThickness = 3,
                              Width = 50,
                              Height = 50,
                              RenderTransform = trans,
                              IsHitTestVisible = false
                          };
                current.MainCanvas.Children.Add(dot);
                Update(e.Position);
            }

            public int DIEDIEDIE()
            {
                current.MainCanvas.Children.Remove(dot);
                return id;
            }

            public void Update(Point p)
            {
                trans.X = (p.X - 25);
                trans.Y = (p.Y - 25);
            }
        }

        #endregion
    }
}