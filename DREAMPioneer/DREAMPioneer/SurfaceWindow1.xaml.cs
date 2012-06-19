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
        public static SurfaceWindow1 current;
        private SortedList<int, SlipAndSlide> captureVis = new SortedList<int, SlipAndSlide>();
        private SortedList<int, Touch> captureOldVis = new SortedList<int, Touch>();
        private JoystickManager joymgr;
        private NodeHandle node;

        private Messages.geometry_msgs.Twist t;
        private cm.ptz pt;
        private EM3MTouch em3m;
        private DateTime currtime;

        private Publisher<gm.Twist> joyPub;
        private Publisher<cm.ptz> servosPub;
        private Subscriber<TypedMessage<sm.LaserScan>> laserSub;

        private ScaleTransform scale;
        private TranslateTransform translate;

        private double width;
        private double height;
        private DateTime n;
        private Touch lastt;

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
            width = 1078;
            height = 212;
        }

        private void rosStart()
        {
            ROS.ROS_MASTER_URI = "http://10.0.2.182:11311";
            Console.WriteLine("CONNECTING TO ROS_MASTER URI: " + ROS.ROS_MASTER_URI);
          //  ROS.ROS_HOSTNAME = "10.0.2.163";
            ROS.Init(new string[0], "DREAM");
            node = new NodeHandle();            
            t = new gm.Twist { angular = new gm.Vector3 { x = 0, y = 0, z = 0 }, linear = new gm.Vector3 { x = 1, y = 0, z = 0 } };
            pt = new cm.ptz { x = 0, y = 0, CAM_MODE = ptz.CAM_ABS };
            joyPub = node.advertise<gm.Twist>("/robot_brain_1/virtual_joystick/cmd_vel", 1000);
            servosPub = node.advertise<cm.ptz>("/robot_brain_1/servos", 1000);
            laserSub = node.subscribe<sm.LaserScan>("/robot_brain_1/filtered_scan", 1000, laserCallback);
            currtime = DateTime.Now;
            width = 0;
            height = 0;
            tf_node.init();
            Dispatcher.Invoke(new Action(() =>
            {
                lastt = new Touch();
                scale = new ScaleTransform();
                translate = new TranslateTransform();

                TransformGroup group = new TransformGroup();
                group.Children.Add(scale);
                group.Children.Add(translate);
                SubCanvas.RenderTransform = group;
                n = DateTime.Now;
                lastupdown = DateTime.Now;
            }));
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


            new Thread(rosStart).Start();
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
                t.linear.x = ry / -10.0;
                t.angular.z = rx / -10.0;
                joyPub.publish(t);
            }
            else
            {
                if(currtime.Ticks + (long)(Math.Pow(10,6)) <= ( DateTime.Now.Ticks ))
                {
                    pt.x = (float)(rx /* 10.0*/);
                    pt.y = (float)(ry * 1 /* -10.0*/);
                    pt.CAM_MODE = ptz.CAM_ABS;
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

        public double distance(Touch c1, Touch c2)
        {
            return distance(c1.Position, c2.Position);
        }

        public static double distance(Point q, Point p)
        {
            return distance(q.X, q.Y, p.X, p.Y);
        }

        public static double distance(double x2, double y2, double x1, double y1)
        {
            return Math.Sqrt(
                (x2 - x1) * (x2 - x1)
                + (y2 - y1) * (y2 - y1));
        }

        DateTime lastupdown = DateTime.Now;

        public void moveStuff(Touch e)
        {
            n = DateTime.Now;
            //false;//=
            bool SITSTILL =  (n.Subtract(lastupdown).TotalMilliseconds <= 250);
            
            Console.WriteLine(SITSTILL);
            if ( distance(e, captureOldVis[e.Id] ) > 10 && !SITSTILL)
            {
                lastupdown = DateTime.Now;
                scale.ScaleY = 1;
                scale.ScaleX = 1;
                    translate.X += (e.Position.X - captureOldVis[e.Id].Position.X);
                    translate.Y += (e.Position.Y - captureOldVis[e.Id].Position.Y);
                if(captureOldVis.Count > 1)
                {
                    //scale.ScaleX += .1;//map.Width += (e.Position.X - captureOldVis[e.Id].Position.X);
                    //scale.ScaleY += .1;//map.Height += (e.Position.Y - captureOldVis[e.Id].Position.Y);
                }


            } if (captureOldVis.ContainsKey(e.Id))
                captureOldVis.Remove(e.Id);
            captureOldVis.Add(e.Id, e);
     
        }
        public void zoomStuff()
        {

        }

        private void Down(Touch e)
        {
            joymgr.Down(e, (t, b) =>
                                        {
                                            if (!b)
                                            {
                                                captureVis.Add(t.Id, new SlipAndSlide(t));
                                                if(!captureOldVis.ContainsKey(e.Id) )
                                                    captureOldVis.Add(t.Id, e);
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

                                                switch(captureOldVis.Count)
                                                {
                                                    case 1:
                                                        break;
                                                    default:
                                                        moveStuff(t);
                                                        break;
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
                                                captureOldVis.Remove(t.Id);
                                            }
                                        }
                                        else
                                        {
                                            if (captureVis.ContainsKey(t.Id))
                                            {
                                                captureVis.Remove(captureVis[t.Id].DIEDIEDIE());
                                                captureOldVis.Remove(t.Id);
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