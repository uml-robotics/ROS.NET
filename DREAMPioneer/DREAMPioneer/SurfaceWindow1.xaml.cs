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

#endregion


namespace DREAMPioneer
{
    /// <summary>
    ///   Interaction logic for SurfaceWindow1.xaml
    /// </summary>
    /// 
    public class touchTimer
    {
        System.Timers.Timer aTimer;
        public int id;
        public bool selected;
        public touchTimer(int _id)
        {
            id = _id;
            setTimer();
        }


        private void setTimer()
        {
            Console.WriteLine("SETTING TIMER");
            selected = false;
            aTimer = new System.Timers.Timer();
            aTimer.Elapsed += new otherTimer.ElapsedEventHandler(OnTimedEvent);
            aTimer.Interval = 2000;
            aTimer.Enabled = true;
        }

        private void disposeTimer()
        {
            Console.WriteLine("TIMER FINISHED");
            aTimer.Enabled = false;
        }

        private void OnTimedEvent(object source, otherTimer.ElapsedEventArgs e)
        {
            selected = true;
            disposeTimer();
        }

    }
   
    public partial class SurfaceWindow1 : Window//, INotifyPropertyChanged
    {
        public static SurfaceWindow window;
        private IntPtr _winhandle = IntPtr.Zero;
        public IntPtr WindowHandle
        {
            get
            {
                if (_winhandle == IntPtr.Zero)
                {
                    _winhandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
                }
                return _winhandle;
            }
        }

        private double dZoom
        {
            get
            {
                if (scale == null) return 0;
                return scale.ScaleX;
            }

            set
            {
                if (scale != null)
                {
                    dotscale.ScaleX = value;
                    dotscale.ScaleY = value;
                    scale.ScaleX= value;
                    scale.ScaleY = value;
                }
            }
        }

        /// <summary>
        ///   The d max zoom.
        /// </summary>
        private const double dMaxZoom = 5;


        /// <summary>
        ///   The d min zoom.
        /// </summary>
        private const double dMinZoom = .1;


        /// <summary>
        ///   Amount to change dZoom by for each zoom increment
        /// </summary>
        private double dZoomIncrements
        {
            get
            {
                double x = scale.ScaleX;
                if (x <.5)
                    return 0.001;
                else if (x < 1)
                    return 0.002;
                else if (x < 2)
                    return 0.003;
                else if (x < 3)
                    return 0.004;
                return 0.005;
            }
        }

        /// <summary>
        ///   Distance between contacts required before a change is interpreted as a zoom event
        /// </summary>
        private double zoomDistanceThreshold = 2;

        /// <summary>
        ///   duh
        /// </summary>
        private double previousZoomDistance;


         private Point DRAG_START = new Point(-1, -1);

        private const string ROS_MASTER_URI = "http://robot-brain-1:11311/";
        public static SurfaceWindow1 current;
        private SortedList<int, SlipAndSlide> captureVis = new SortedList<int, SlipAndSlide>();
        //private SortedList<int, Touch> FREE = new SortedList<int, Touch>();
        private JoystickManager joymgr;
        private NodeHandle node
        { 
            get { return ROSStuffs[2].node; }
            set { ROSStuffs[2].node = value; }
        }

        private Messages.geometry_msgs.Twist t;
        private cm.ptz pt
        { 
            get { return ROSStuffs[2].pt; }
            set { ROSStuffs[2].pt = value; }
        }

#if SURFACEWINDOW
        private Microsoft.Surface.Core.ContactTarget contactTarget;
        private bool applicationLoadCompleteSignalled;

        private UserOrientation currentOrientation = UserOrientation.Bottom;
        private Matrix screenTransform = Matrix.Identity;
        private Matrix inverted;

        // application state: Activated, Previewed, Deactivated,
        // start in Activated state
        private bool isApplicationActivated = true;
        private bool isApplicationPreviewed;
        private object em3m;
#else
        private EM3MTouch em3m;
#endif
        private DateTime currtime;

        private Publisher<gm.Twist> joyPub
        { 
            get { return ROSStuffs[2].joyPub; }
            set { ROSStuffs[2].joyPub = value; }
        }
        private Publisher<cm.ptz> servosPub
        {
            get { return ROSStuffs[2].servosPub; }
            set { ROSStuffs[2].servosPub = value; }
        }
        private Publisher<gm.PoseWithCovarianceStamped> initialPub
    {
    get { return ROSStuffs[2].initialPub; }
    set { ROSStuffs[2].initialPub = value; }
        }
        private Subscriber<sm.LaserScan> laserSub
    {
    get { return ROSStuffs[2].laserSub; }
    set { ROSStuffs[2].laserSub = value; }
        }
        private Publisher<gm.PoseStamped> goalPub
    {
    get { return ROSStuffs[2].goalPub; }
    set { ROSStuffs[2].goalPub = value; }
        }
        private Subscriber<m.String> androidSub
    {
    get { return ROSStuffs[2].androidSub; }
    set { ROSStuffs[2].androidSub = value; }
        }
        private gm.PoseWithCovarianceStamped pose
    {
    get { return ROSStuffs[2].pose; }
    set { ROSStuffs[2].pose = value; }
        }
        private gm.PoseStamped goal
    {
    get { return ROSStuffs[2].goal; }
    set { ROSStuffs[2].goal = value; }
        }

        private ScaleTransform scale;
        private TranslateTransform translate;
        public ScaleTransform dotscale;
        public TranslateTransform dottranslate;

        private int android = -1;
        private bool touchHold;

        private Timer YellowTimer;
        private Timer GreenTimer;

        SortedDictionary<int, string> _namespace;
        public SortedDictionary<int, ROSData> ROSStuffs = new SortedDictionary<int, ROSData>();

        int numRobots;
        int manualRobot;
        int androidRobot;

        string manualVelocity;
        string manualCamera;
        string manualPTZ;
        string manualLaser;

        string androidVelocity;
        string androidCamera;
        string androidPTZ;
        string androidLaser;

        System.Timers.Timer aTimer;

        private TimerManager timers = new TimerManager();

        // <summary>
        ///   Should contain all the robots in existance.
        /// </summary>
        public SortedList<int, DREAMPioneer.RobotControl> robots = new SortedList<int, DREAMPioneer.RobotControl>();

        /// <summary>
        ///   The yellow dots.
        /// </summary>
        private List<DREAMPioneer.RobotControl> YellowDots = new List<DREAMPioneer.RobotControl>();

        /// <summary>
        ///   The green dots.
        /// </summary>
        private List<DREAMPioneer.RobotControl> GreenDots = new List<DREAMPioneer.RobotControl>();

        /// <summary>
        ///   The time in seconds after which the 'double tap' gesture can be considered activated.
        /// </summary>
        private const int TimeDT = 600;

        private DateTime n;
        private Touch lastt;
        private static float PPM = 0.02868f;
        private static float MPP = 1.0f / PPM;

        touchTimer[] ttime;

        /// <summary>
        ///   The waypoint dots.
        /// </summary>
        private List<Waypoint> waypointDots = new List<Waypoint>();
       

        /// <summary>
        ///   The lasso points.
        /// </summary>
        /// TODO Mark: add summary information to these variables
        private List<Point> lassoPoints = new List<Point>();

        /// <summary>
        ///   The lasso line.
        /// </summary>
        private Polyline lassoLine = new Polyline
        {
            Stroke = Brushes.AntiqueWhite,
            StrokeThickness = 3,
            StrokeDashOffset = 2,
            StrokeDashArray = new DoubleCollection(new double[] { 2, 1 })
        };

#if SURFACEWINDOW
        #region Initialization

        /// <summary>
        /// Moves and sizes the window to cover the input surface.
        /// </summary>
        private void SetWindowOnSurface()
        {
            System.Diagnostics.Debug.Assert(WindowHandle != System.IntPtr.Zero,
                "Window initialization must be complete before SetWindowOnSurface is called");
            if (WindowHandle == System.IntPtr.Zero)
                return;
            Left = Microsoft.Surface.Core.InteractiveSurface.DefaultInteractiveSurface.Left;
            Top = Microsoft.Surface.Core.InteractiveSurface.DefaultInteractiveSurface.Top;
            Width = Microsoft.Surface.Core.InteractiveSurface.DefaultInteractiveSurface.Width;
            Height = Microsoft.Surface.Core.InteractiveSurface.DefaultInteractiveSurface.Height;
        }

        /// <summary>
        /// Initializes the surface input system. This should be called after any window
        /// initialization is done, and should only be called once.
        /// </summary>
        private void InitializeSurfaceInput()
        {
            System.Diagnostics.Debug.Assert(WindowHandle != System.IntPtr.Zero,
                "Window initialization must be complete before InitializeSurfaceInput is called");
            if (WindowHandle == System.IntPtr.Zero)
                return;
            System.Diagnostics.Debug.Assert(contactTarget == null,
                "Surface input already initialized");
            if (contactTarget != null)
                return;

            // Create a target for surface input.
            contactTarget = new ContactTarget(WindowHandle, EventThreadChoice.OnBackgroundThread);
            contactTarget.EnableInput();
        }

        /// <summary>
        /// Reset the application's orientation and transform based on the current launcher orientation.
        /// </summary>
        private void ResetOrientation()
        {
            UserOrientation newOrientation = ApplicationLauncher.Orientation;

            if (newOrientation == currentOrientation) { return; }

            currentOrientation = newOrientation;

            if (currentOrientation == UserOrientation.Top)
            {
                screenTransform = inverted;
            }
            else
            {
                screenTransform = Matrix.Identity;
            }
        }

        #endregion
#endif

        public SurfaceWindow1()
        {
            current = this;
            InitializeComponent();
#if SURFACEWINDOW
            new Thread(() =>
            {
                while (WindowHandle == IntPtr.Zero)
                {
                    Thread.Sleep(10);
                }
                try
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        Width = 1024;
                        Height = 768;
                        SetWindowOnSurface();
                        InitializeSurfaceInput();

                        contactTarget.ContactAdded += new EventHandler<Microsoft.Surface.Core.ContactEventArgs>(surfaceDown);
                        contactTarget.ContactChanged += new EventHandler<Microsoft.Surface.Core.ContactEventArgs>(surfaceChanged);
                        contactTarget.ContactRemoved += new EventHandler<Microsoft.Surface.Core.ContactEventArgs>(surfaceUp);

                        // Set the application's orientation based on the current launcher orientation
                        currentOrientation = ApplicationLauncher.Orientation;

                        /*// Subscribe to surface application activation events
                        ApplicationLauncher.ApplicationActivated += OnApplicationActivated;
                        ApplicationLauncher.ApplicationPreviewed += OnApplicationPreviewed;
                        ApplicationLauncher.ApplicationDeactivated += OnApplicationDeactivated;*/

                        if (currentOrientation == UserOrientation.Top)
                        {
                            screenTransform = inverted;
                        }
                    }));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }).Start();
#else
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
#endif
        }

        void surfaceDown(object sender, Microsoft.Surface.Core.ContactEventArgs e)
        {
            Down(GenericTypes_Surface_Adapter.SurfaceAdapter.Down(e));
        }
        void surfaceChanged(object sender, Microsoft.Surface.Core.ContactEventArgs e)
        {
            Changed(GenericTypes_Surface_Adapter.SurfaceAdapter.Change(e));
        }
        void surfaceUp(object sender, Microsoft.Surface.Core.ContactEventArgs e)
        {
            Up(GenericTypes_Surface_Adapter.SurfaceAdapter.Up(e));
        }

        private byte[] CMP = new byte[] { 10, 0, 2 };
        private const string DEFAULT_HOSTNAME = "10.0.2.47";
        private void rosStart()
        {
            ROS.ROS_MASTER_URI = "http://10.0.2.42:11311";
            Console.WriteLine("CONNECTING TO ROS_MASTER URI: " + ROS.ROS_MASTER_URI);
            ROS.ROS_HOSTNAME = DEFAULT_HOSTNAME;
            System.Net.IPAddress[] FUCKYOUDEBUGGER = System.Net.Dns.GetHostAddresses(Environment.MachineName);
                foreach (System.Net.IPAddress addr in FUCKYOUDEBUGGER)
                {
                    if (addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        byte[] myballs = addr.GetAddressBytes();
                        for (int i = 0; i < CMP.Length; i++)
                        {
                            if (myballs[i] != CMP[i])
                            {
                                ROS.ROS_HOSTNAME = DEFAULT_HOSTNAME;
                                break;
                            }
                            else
                                ROS.ROS_HOSTNAME = "";
                        }
                        if (ROS.ROS_HOSTNAME == DEFAULT_HOSTNAME)
                            continue;
                        ROS.ROS_HOSTNAME = string.Format("{0}.{1}.{2}.{3}", myballs[0], myballs[1], myballs[2], myballs[3]);
                        break;
                    }
                }
            
            //**********************//
                ROS.Init(new string[0], "DREAM");
                
                //node = new NodeHandle();

                //manualCamera = "/robot_brain_1/camera/rgb/image_color";
                //manualLaser = "fakelaser";
                //manualPTZ = "/robot_brain_1/servos";
                //manualVelocity = "fakevel";

                //t = new gm.Twist { angular = new gm.Vector3 { x = 0, y = 0, z = 0 }, linear = new gm.Vector3 { x = 0, y = 0, z = 0 } };
                //joyPub = node.advertise<gm.Twist>(manualVelocity, 1);

                //pt = new cm.ptz { x = 0, y = 0, CAM_MODE = ptz.CAM_REL };
                //servosPub = node.advertise<cm.ptz>(manualPTZ, 1);

                //goal = new gm.PoseStamped() { header = new m.Header { frame_id = new String("/robot_brain_1/map") }, pose = new gm.Pose { position = new gm.Point { x = 1, y = 1, z = 0 }, orientation = new gm.Quaternion { w = 0, x = 0, y = 0, z = 0 } } };
                //goalPub = node.advertise<gm.PoseStamped>("/robot_brain_1/goal", 10);

                ////Deprecated until I make an abstraction in ros that can publish transforms
                ////pose = new gm.PoseWithCovarianceStamped() { header = new m.Header { frame_id = new String("/robot_brain_1/map") }, pose = new gm.PoseWithCovariance { pose = new gm.Pose { orientation = new gm.Quaternion { w = .015, x = 0, y = 0, z = 1 }, position = new gm.Point { x = 29.9, y = 3.5, z = 0 } }, covariance = new double[] { .25, 0, 0, 0, 0, 0, 0, .25, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, .06853891945200942 } } };
                ////initialPub = node.advertise<gm.PoseWithCovarianceStamped>("/robot_brain_1/initialpose",1000);

                //laserSub = node.subscribe<sm.LaserScan>(manualLaser, 1, laserCallback);
                //androidSub = node.subscribe<m.String>("/robot_brain_1/androidControl", 1, androidCallback);
            //**********************//
            currtime = DateTime.Now;
            tf_node.init();
            lastt = new Touch();

            Dispatcher.Invoke(new Action(() =>
                {
                    TransformGroup group = new TransformGroup();
                    TransformGroup dotgroup = new TransformGroup();
                    scale = new ScaleTransform(1,1);
                    translate = new TranslateTransform(0,0);
                    dotscale = new ScaleTransform(1,1);
                    dottranslate = new TranslateTransform(0,0);
                    group.Children.Add(scale);
                    group.Children.Add(translate);
                    dotgroup.Children.Add(dotscale);
                    dotgroup.Children.Add(dottranslate);
                    SubCanvas.RenderTransform = group;
                    DotCanvas.RenderTransform = dotgroup;
                }));

            n = DateTime.Now;
            lastupdown = DateTime.Now;
            manualRobot = -1;
            androidRobot = -1;
            for (int i = 0; i < 1; i++)
            {
                AddRobot();
            }

            touchHold = false;
            timers.StartTimer(ref YellowTimer, YellowTimer_Tick, 0, 10);
            timers.StartTimer(ref GreenTimer, GreenTimer_Tick, 0, 5);
            timers.MakeTimer(ref RM5Timer, RM5Timer_Tick, TimeDT, Timeout.Infinite);
            //timers.MakeTimer(ref touchHold, TouchHold, 0, 200);

            _namespace = new SortedDictionary<int, string>();
            _namespace.Add(0, "/robot_brain_1");
            _namespace.Add(1, "/robot_brain_2");
            _namespace.Add(2, "/robot_brain_3");
            _namespace.Add(3, "");
            _namespace.Add(4,"") ;
            ttime = new touchTimer[40];

            // for( int i =0; i< ttime.Length; i++)
            // {
            //ttime[i] = new touchTimer();
            // }

            //= new touchTimer();

            for (int i = 0; i < turboFingering.Length; i++)
                timers.MakeTimer(ref turboFingering[i], fingerHandler, i, 600, Timeout.Infinite);
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
            DONTGCMEPLZZOMG = new Thread(rosStart);
            DONTGCMEPLZZOMG.Start();
        }
        private Thread DONTGCMEPLZZOMG;

        private void changeManual(string vel, string ptz, string cam, string urg)
        {
            int i = 2;
            manualVelocity = vel;
            manualPTZ = ptz;
            manualCamera = cam;
            manualLaser = urg;

            joyPub = node.advertise<gm.Twist>(manualVelocity, 1);
            servosPub = node.advertise<cm.ptz>(manualPTZ, 1);
            laserSub = node.subscribe<sm.LaserScan>(manualLaser, 1, laserCallback);
            ROS_ImageWPF.ImageControl.newTopicName = manualCamera;
            /*
             Dispatcher.BeginInvoke(new Action(() => {
                RightControlPanel rcp = joymgr.RightPanel as RightControlPanel;
                if (rcp != null)
                {
                    
                }
            }));*/
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
                Messages.geometry_msgs.Twist tempTwist = new Messages.geometry_msgs.Twist();
                tempTwist.linear = new Messages.geometry_msgs.Vector3();
                tempTwist.angular = new Messages.geometry_msgs.Vector3();
                tempTwist.linear.x = 0;// ry / -200.0;
                tempTwist.angular.z = rx / -200.0;
                joyPub.publish(tempTwist);
            }
            else
            {
                if (currtime.Ticks + (long)(Math.Pow(10, 6)) <= (DateTime.Now.Ticks))
                {
                    pt.x = (float)(rx / 10.0);
                    pt.y = (float)(ry / -10.0);
                    pt.CAM_MODE = ptz.CAM_REL;
                    //servosPub.publish(pt);
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

        public void androidCallback(m.String str)
        {
            //android = str.data.data;

            if (str.data == "robot_brain_1")
                android = 0;
            else if (str.data == "robot_brain_2")
                android = 1;
            else if (str.data == "robot_brain_3")
                android = 2;
            else android = -1;
        }

        public void laserCallback(sm.LaserScan laserScan)
        {
            double[] scan = new double[laserScan.ranges.Length];
            for (int i = 0; i < laserScan.ranges.Length; i++)
            {
                if (i - 1 >= 0)
                {
                    if (laserScan.ranges[i] < 0.3f)
                        scan[i] = scan[i - 1];
                    else
                        scan[i] = laserScan.ranges[i];
                }
                else
                {
                    if (laserScan.ranges[i] < 0.3f)
                        scan[i] = laserScan.ranges[i + 1];
                    else
                        scan[i] = laserScan.ranges[i];
                }
            }


            Dispatcher.BeginInvoke(new Action(() =>
            {
                LeftControlPanel lcp = joymgr.LeftPanel as LeftControlPanel;
                if (lcp != null)
                    lcp.newRangeCanvas.SetLaser(scan, laserScan.angle_increment, laserScan.angle_min);
            }));
        }

        public void AddRobot()
        {
            lock (ROSStuffs)
                if (!ROSStuffs.ContainsKey(2))
                    ROSStuffs.Add(2, new ROSData(new NodeHandle(), 2));

            robots.Add(0, ROSStuffs[2].myRobot);
            numRobots += 1;
        }

        public double distance(Touch c1, Touch c2)
        {
            return distance(c1.Position, c2.Position);
        }

        public static double distance(Point q, Point p)
        {
            return distance(q.X, q.Y, p.X, p.Y);
        }

        /// <summary>
        ///   This function will return the id of the robot that is closest to the point passed to it. 
        ///   This robot should also be within the MaxNearnessDistance range. If no robot is found then
        ///   -1 is returned.
        /// </summary>
        /// <param name = "p">
        ///   This is the point that from which the distance to the robots is measured.
        /// </param>
        /// <returns>
        ///   ID of the robot that passes the nearness criterion and is closest to point p.
        /// </returns>
        private int CloseToRobot(Point p)
        {
            double distance;
           // double closestDist = robots[0].robot.Width / scale.ScaleX;
            int id = -1;

            for (int i = 0; i < robots.Count; i++)
            {
                distance = Math.Sqrt(Math.Pow(p.X - robots[i].xPos, 2) + Math.Pow(p.Y - robots[i].yPos, 2));

                // Find the shortest distance and record the id and distance of that robot.
                //if (distance < closestDist)
                //{
                //    closestDist = distance;
                //    id = i;
                //}
            }

            return id;
        }

        public static double distance(double x2, double y2, double x1, double y1)
        {
            return Math.Sqrt(
                (x2 - x1) * (x2 - x1)
                + (y2 - y1) * (y2 - y1));
        }

        /// <summary>
        ///   event handlers for yellow pulsing lights
        /// </summary>
        /// <param name = "sender">
        /// </param>
        private void YellowTimer_Tick(object sender)
        {
            yellowCurrent += yellowDelta;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                foreach (DREAMPioneer.RobotControl el in YellowDots)
                    el.SetOpacity(yellowCurrent);
            }));
            if (((yellowDelta > 0) && (yellowCurrent >= yellowMax))
                || ((yellowDelta < 0) && (yellowCurrent <= yellowMin)))
                yellowDelta = 0 - yellowDelta;
        }

        /// <summary>
        ///   The green timer_ tick.
        /// </summary>
        /// <param name = "sender">
        ///   The sender.
        /// </param>
        private void GreenTimer_Tick(object sender)
        {
            greenCurrent += greenDelta;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                foreach (DREAMPioneer.RobotControl el in GreenDots)
                    el.SetOpacity(greenCurrent);
            }));
            if (((greenDelta > 0) && (greenCurrent >= greenMax))
                || ((greenDelta < 0) && (greenCurrent <= greenMin)))
                greenDelta = 0 - greenDelta;
        }

        /// <summary>
        ///   This list contains the ids of all the robots that are currently selected.
        /// </summary>
        private List<int> selectedList = new List<int>();

        /// <summary>
        ///   Hold onto your butt for this one...
        /// </summary>
        /// <param name = "i">
        ///   the number of the robot whose light you want to start pulsing
        /// </param>
        public void PulseYellow(int i)
        {
            NoPulse(i);
            AddYellow(i);
        }

        /// <summary>
        ///   The pulse yellow.
        /// </summary>
        public void PulseYellow()
        {
            for (int i = 0; i < 2/* ADD REAL*/; i++)
                PulseYellow(i);
        }

        /// <summary>
        ///   The pulse green.
        /// </summary>
        /// <param name = "i">
        ///   The i.
        /// </param>
        public void PulseGreen(int i)
        {
            if (i == -1)
                throw new Exception();
            if (robots.ContainsKey(i))
            {
                if (!GreenDots.Contains(robots[i]))
                {
                    NoPulse(i);
                    AddGreen(i);
                }
            }
        }

        /// <summary>
        ///   The no pulse.
        /// </summary>
        public void NoPulse()
        {
            for (int i = 0; i < numRobots; i++)
            {
                NoPulse(i);
            }
        }

        /// <summary>
        ///   stops the pulsethread, so the light stops pulsing... also makes the light colorless ("off")
        /// </summary>
        /// <param name = "i">
        /// </param>
        public void NoPulse(int i)
        {
            if (i < 0 || i >= numRobots || !robots.ContainsKey(i))
                return;
            RemoveYellow(i);
            RemoveGreen(i);
        }

        /// <summary>
        ///   The pulse green.
        /// </summary>
        public void PulseGreen()
        {
            for (int i = 0; i < numRobots; i++)
                PulseGreen(i);
        }

        /// <summary>
        ///   add a dot to the yellow list and start the yellow timer if it is stopped
        /// </summary>
        /// <param name = "i">
        /// </param>
        public void AddYellow(int i)
        {
            robots[i].SetColor(Brushes.Yellow);
            YellowDots.Add(robots[i]);
            timers.StartTimer(ref YellowTimer);
        }

        /// <summary>
        ///   add a dot to the yellow list and start the yellow timer if it is stopped
        /// </summary>
        /// <param name = "i">
        /// </param>
        public void AddGreen(int i)
        {
            Brush Tmp_Brush;
            lock (GoalDot.ColorInUse)
            {
                if (GoalDot.ColorInUse.ContainsKey(i))
                {
                    Tmp_Brush = GoalDot.ColorInUse[i];
                    GoalDot.ColorInUse.Remove(i);
                    for (int c = 1; c <= GoalDot.ColorInUse.Count + 1; c++)
                        if (!GoalDot.ColorInUse.ContainsKey(-c))
                        {
                            GoalDot.ColorInUse.Add((-c), Tmp_Brush);
                            break;
                        }
                   robots[i].robot.ChangeIconColors(robots[i].robot.circles.IndexOf(robots[i].robot.Border.Stroke));
                }
            }
            bool Done = false;
            foreach (CommonList CL in RobotControl.OneInAMillion)
            {

                foreach (Robot_Info RI in CL.RoboInfo)
                    if (RI.RoboNum == i && !RI.done)
                    {
                        Done = true;
                        RI.done = true;
                        RI.CurrentLength = CL.P_List.Count;
                        foreach (Robot_Info DoneCheck in CL.RoboInfo)
                            if (!DoneCheck.done)
                                Done = false;

                        if (Done)
                        {
                            foreach (GoalDot GD in CL.Dots)
                                DotCanvas.Children.Remove(GD);
                            CL.P_List.Clear();
                            CL.RoboInfo.Clear();
                            lock (RobotControl.OneInAMillion)
                                RobotControl.OneInAMillion.Remove(CL);
                            break;
                        }
                    }
                if (Done) break;
            }
            robots[i].SetColor(Brushes.Green);
            GreenDots.Add(robots[i]);
            timers.StartTimer(ref GreenTimer);
        }

        /// <summary>
        ///   remove a dot from the yellow list and stop the yellow timer if it is empty afterwards
        ///   make the circle black and 0.3 opacity for testing reasons
        /// </summary>
        /// <param name = "i">
        /// </param>
        public void RemoveYellow(int i)
        {
            if (YellowDots.Contains(robots[i]))
            {
                YellowDots.Remove(robots[i]);
                robots[i].SetColor(Brushes.Transparent);
                if (YellowDots.Count == 0)
                    timers.StopTimer(ref YellowTimer);
            }
        }

        /// <summary>
        ///   The remove green.
        /// </summary>
        /// <param name = "i">
        ///   The i.
        /// </param>
        public void RemoveGreen(int i)
        {
            if (GreenDots.Contains(robots[i]))
            {
                GreenDots.Remove(robots[i]);
                robots[i].SetColor(Brushes.Transparent);
                if (GreenDots.Count == 0)
                    timers.StopTimer(ref GreenTimer);
            }
        }

        /// <summary>
        ///   The finger handler.
        /// </summary>
        /// <param name = "state">
        ///   The state.
        /// </param>
        private void fingerHandler(object state)
        {
            int robot = (int)state;
            if (!timers.IsRunning(ref turboFingering[robot]))
                return;
            timers.StopTimer(ref turboFingering[robot]);
        }

        public void AddSelected(int robot, Touch e)
        {
            Console.WriteLine("SELECTING " + robot);
            if (manualRobot != robot && android != robot && !selectedList.Contains(robot))
            {
                selectedList.Add(robot);
                PulseYellow(robot);
            }
        }

        DateTime lastupdown = DateTime.Now;

        public void moveStuff(Dictionary<int, Touch> cc)
        {
            n = DateTime.Now;
            bool SITSTILL = (n.Subtract(lastupdown).TotalMilliseconds > 1);
            lastupdown = DateTime.Now;
            bool zoomed = false;
            double dTmp = 0;
            double XSum = 0, YSum = 0;
            for (int i = 0; i < cc.Count; i++)
            {
                XSum += cc[cc.Keys.ElementAt(i)].Position.X;
                YSum += cc[cc.Keys.ElementAt(i)].Position.Y;
                if (i < cc.Count - 1)
                    dTmp += distance(cc[cc.Keys.ElementAt(i)], cc[cc.Keys.ElementAt(i + 1)]);
            }
            
            if (Math.Abs(previousZoomDistance - dTmp) > zoomDistanceThreshold)
            {
                if ((previousZoomDistance - dTmp) > zoomDistanceThreshold)
                {
                    if (!SITSTILL)
                    {
                        if (dZoom + dZoomIncrements > dMaxZoom)
                        {
                            dZoom = dMaxZoom;
                        }
                        else
                        {
                            dZoom = dZoom - dZoomIncrements;
                        }
                    }
                    previousZoomDistance = dTmp;

                }
                else if ((dTmp - previousZoomDistance) > zoomDistanceThreshold)
                {
                    if (!SITSTILL)
                    {
                        if (dZoom - dZoomIncrements < dMinZoom)
                        {
                            dZoom = dMinZoom;
                        }
                        else
                        {
                            dZoom = dZoom + dZoomIncrements;
                        }
                       
                    }
                    previousZoomDistance = dTmp;
                }
            }
                Point p = new Point(XSum / cc.Count, YSum / cc.Count);
                Point drag = new Point(DRAG_START.X - p.X, DRAG_START.Y - p.Y);
                DRAG_START = p;
                if (!SITSTILL){
                    translate.X -= drag.X / scale.ScaleY;
                    translate.Y -= drag.Y / scale.ScaleY;
                    dottranslate.X -= drag.X / dotscale.ScaleY;
                    dottranslate.Y -= drag.Y / dotscale.ScaleY;
                }
                    
         }
            
        

        /// <summary>
        ///   Gets FREE.
        /// </summary>
        public Dictionary<int, Touch> FREE
        {
            get { return joymgr.FreeTouches; }
        }

        private void ChangeState(RMState s)
        {
            state = s;

            if (s == RMState.Start)
            {
                selectedList.Clear();
                lock (waypointDots)
                {
                    foreach (Waypoint wp in waypointDots)
                    {
                        DotCanvas.Children.Remove(wp.dot);
                    }
                    waypointDots.Clear();
                    Waypoint.PointLocations.Clear();
                }
            }
        }



        /// <summary>
        ///   The last waypoint dot.
        /// </summary>
        private Point lastWaypointDot;

        /// <summary>
        ///   The add waypoint dot.
        /// </summary>
        /// <param name = "p">
        ///   The p.
        /// </param>
        private void AddWaypointDot(Point p)
        {
            if (Math.Abs((distance(lastWaypointDot, p))) / scale.ScaleX > (joymgr.DPI / 43) * 10)
            {
                lock (waypointDots)
                    foreach(Point point in Waypoint.PointLocations)
                        if (p == point) return;
                lastWaypointDot = p;                
                lock (waypointDots)
                    waypointDots.Add(new Waypoint(DotCanvas,p,joymgr.DPI,MainCanvas,dotscale,dottranslate,Brushes.Yellow));
            }
            else
            {}
            /*if (waypointDots.Count > 10)
                clearAllDot();*/
        }
        private void clearAllDot()
        {
            List<Point> temp = new List<Point>();

            foreach (Point p in Waypoint.PointLocations)
            {
                temp.Add(p);
            }
            foreach (Waypoint wp in waypointDots)
            {
                DotCanvas.Children.Remove(wp.dot);
            }
            waypointDots.Clear();
            Waypoint.PointLocations.Clear();
        }

        public void AddGoalDots(List<Point> p_list, List<GoalDot> GDL, Brush b)
        {
            List<GoalDot> TempList = new List<GoalDot>(GDL);
            GDL.Clear();
            foreach (Point p in p_list)
            {
                GDL.Add(new GoalDot(DotCanvas, p, joymgr.DPI, MainCanvas, dotscale, dottranslate, b));
            }
            foreach (GoalDot GD in TempList)
            {
                GDL.Add(GD);
                GD.BeenHere = false;
            }
        }
        
        /// <summary>
        ///   The set goal.
        /// </summary>
        /// <param name = "i">
        ///   The i.
        /// </param>
        /// <param name = "sim">
        ///   The sim.
        /// </param>
        public void SetGoal(int r, List<Point> PList, CommonList CL, Robot_Info RI)
        {


            RI.CurrentLength = PList.Count;
            RI.Next = PList.First();

            List<Point> WindowEQ = new List<Point>(PList);
            




            foreach (Robot_Info LengthCheck in CL.RoboInfo)
            {
                if (LengthCheck.CurrentLength > RI.CurrentLength && LengthCheck.Position < RI.Position)
                {
                    int temp = RI.Position;
                    RI.Position = LengthCheck.Position;
                    LengthCheck.Position = temp;
                }
                else if (LengthCheck.CurrentLength > RI.CurrentLength && LengthCheck.Position == RI.Position)
                {
                    if (LengthCheck.Position != CL.RoboInfo.Count)
                        LengthCheck.Position++;
                    else
                        RI.Position--;
                }
//                foreach (Robot_Info CheckOthers in CL.RoboInfo)
//                    if (LengthCheck.CurrentLength == CheckOthers.CurrentLength)
//                    {
//                        if (Math.Abs(distance(LengthCheck.Next, LengthCheck.Location)) >
//Math.Abs(distance(CheckOthers.Next,  CheckOthers.Location)) && LengthCheck.Position < CheckOthers.Position)
//                        {
//                            int temp = CheckOthers.Position;
//                            CheckOthers.Position = LengthCheck.Position;
//                            LengthCheck.Position = temp;
//                        }
//                    }
            }

            List<GoalDot> Handled = new List<GoalDot>();
            List<GoalDot> UnHandled = new List<GoalDot>(CL.Dots);

            PassBack(CL.RoboInfo, 1, UnHandled, Handled);






        }

        public void PassBack(List<Robot_Info> RI, int Position, List<GoalDot> UnHandled, List<GoalDot> Handled)
        {
            int already_done = Handled.Count;
            int donecount = 0;
            foreach (Robot_Info DoneCheck in RI)
                if (DoneCheck.done) donecount++;
            foreach (Robot_Info PosCheck in RI)
                if (PosCheck.Position == Position)
                {
                    if (!PosCheck.done)
                    {
                        for (int i = 0; i < PosCheck.CurrentLength - already_done - 1; i++)
                        {
                            if (UnHandled.Count == 0)
                                return;

                            if (i == PosCheck.CurrentLength - already_done - 2) UnHandled[(UnHandled.Count - 1)].NextOne = true;
                            else UnHandled[(UnHandled.Count - 1)].NextOne = false;
                            UnHandled[(UnHandled.Count - 1)].NextC1.Fill = PosCheck.Color;
                            UnHandled[(UnHandled.Count - 1)].BeenThereC2.Fill = PosCheck.Color;
                            Handled.Add(UnHandled[(UnHandled.Count - 1)]);
                            UnHandled.Remove(UnHandled[(UnHandled.Count - 1)]);

                        }
                        const int const_thingy = 10;
                        if (!(PosCheck.Position == RI.Count - donecount))
                            PassBack(RI, Position + 1, UnHandled, Handled);
                        else

                            for (int i = 0; i < UnHandled.Count; i++)
                            {
                                if (i < UnHandled.Count - const_thingy)
                                {
                                    UnHandled[i].BeenThereC1.Visibility = Visibility.Hidden;
                                    UnHandled[i].BeenThereC2.Visibility = Visibility.Hidden;
                                }
                                else
                                {
                                    UnHandled[i].BeenThereC2.Fill = PosCheck.Color;
                                    UnHandled[i].BeenHere = true;
                                }
                            }
                        return;


                    }
                    else
                        PassBack(RI, Position + 1, UnHandled, Handled);
                }




        }



        private Timer[] turboFingering = new Timer[40];

        /// <summary>
        ///   The turbo finger count.
        /// </summary>
        private int[] turboFingerCount = new int[40];

        private bool ToggleSelected(int robot, Touch e)
        {
            bool res = true;
            if (robot == -1)
                return false;
            ttime[robot] = new touchTimer(robot);
            if (!timers.IsRunning(ref turboFingering[robot]))
            {
                if (selectedList.Contains(robot))
                    RemoveSelected(robot, e);
                else
                    AddSelected(robot, e);
                turboFingerCount[robot] = 0;
            }
            else
            {
                timers.StopTimer(ref turboFingering[robot]);
            }
            if (selectedList.Count == 0)
            {
                res = false;
            }
            timers.StartTimer(ref turboFingering[robot]);
            turboFingerCount[robot]++;
            //touchHold = new otherTimer.Timer(2000);
            //timers.StartTimer(ref checkHold);


            return res;
        }

        private bool turnedIntoDrag;

        private void RemoveSelected(int robot, Touch e)
        {
            selectedList.Remove(robot);
            NoPulse(robot);
        }
        /// <summary>
        ///   Returns the id of the robot that was responsible for the contact event.
        /// </summary>
        /// <param name = "e">
        ///   Contact event in question.
        /// </param>
        /// <returns>
        ///   ID of the robot in question. -1 if no robot was involved.
        /// </returns>
        private int robotsCD(Touch e)
        {
            if (scale == null || translate == null) return -1;
            Double xPos = ((e.Position.X - translate.X) / scale.ScaleX * PPM);
            Double yPos = ((e.Position.Y - translate.Y) / scale.ScaleY * PPM);
            for (int i = 0; i < numRobots; i++)
            {
                Double _xPos = ((robots[i].xPos) * PPM);
                Double _yPos = ((robots[i].yPos) * PPM);
                Double Width = robots[i].robot.Width * scale.ScaleX * PPM;
                if (distance(xPos, yPos, _xPos, _yPos) < Width / 2)
                {
                    return i;
                }
            }
            return -1;
        }

        private bool robotSelected(int n)
        {
            if (n > -1)
            {
                AddYellow(n);
                return true;
            } return false;
        }

        private void Down(Touch e)
        {
            joymgr.Down(e, (t, b) =>
                                        {
                                            if (!b)
                                            {

                                                captureVis.Add(t.Id, new SlipAndSlide(t));
                                                captureVis[t.Id].dot.Stroke = Brushes.White;

                                                ZoomDown(e);
                                                int index = robotsCD(e);
                                                if (selectedList.Count == 0 && (state == RMState.State2 || state == RMState.State4))
                                                    ChangeState(RMState.Start);

                                                switch (state)
                                                {
                                                    case RMState.Start:

                                                        // If the CD was on a robot then ...
                                                        if (index != -1)
                                                        {
                                                            ToggleSelected(index, e);
                                                            ChangeState(RMState.State1);
                                                        }

                                    // If the CD was on gound then...
                                                        else if ((index = CloseToRobot(e.Position)) != -1)
                                                        {
                                                            AddSelected(index, e);
                                                            ChangeState(RMState.State1);
                                                        }
                                                        break;
                                                    case RMState.State1:
                                                        break;
                                                    case RMState.State2:
                                                        if (index != -1) // If the CD was on a robot then ...
                                                        {
                                                            ToggleSelected(index, e);
                                                            ChangeState(RMState.State1);
                                                        }
                                                        else // If the CD was on gound then...
                                                        {
                                                            //index = CloseToRobot(e.Position); // Was the contact close enough to a robot?
                                                            if ((index = CloseToRobot(e.Position)) != -1)
                                                            {
                                                                AddSelected(index, e);
                                                                ChangeState(RMState.State3);
                                                            }
                                                            else // CD was far from any robot.
                                                            {
                                                                RM5Start(e.Position);
                                                                ChangeState(RMState.State3);
                                                            }
                                                        }
                                                        break;
                                                    case RMState.State3:

                                                        break;
                                                    case RMState.State4:
                                                        if (index != -1) // If the CD was on a robot then, start movement and reset to 1 robot selected.
                                                        {
                                                            // Reset to state RM1. Select the current robot.
                                                            if (!selectedList.Contains(index))
                                                            {
                                                                NoPulse();
                                                                HandOutWaypoints(); //"selected an unselected robot"
                                                                selectedList.Clear();
                                                                AddSelected(index, e);//"Tap"
                                                            }

                                                            ChangeState(RMState.State1);
                                                        }
                                                        else if ((index = CloseToRobot(e.Position)) != -1)
                                                        {
                                                            if (!selectedList.Contains(index))
                                                            {
                                                                NoPulse();
                                                                HandOutWaypoints(); //"selected an unselected robot"
                                                                selectedList.Clear();
                                                                AddSelected(index, e);
                                                            }

                                                            ChangeState(RMState.State1);
                                                        }
                                                        else // If the CD was on gound then...
                                                        {

                                                            RM5Start(e.Position);
                                                            ChangeState(RMState.State3);
                                                        }

                                                        break;
                                                    case RMState.State5:
                                                        break;
                                                    default:
                                                        break;
                                                }
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

                                                    int index = robotsCD(e);
                                                    Dictionary<int, Touch> cc = FREE;

                                                    if (FREE.Count == 1 && (state == RMState.State3 || em3m == null && state == RMState.State5 && (robotsCD(t) == -1 /* && menu != null && !Contacts.GetContactsOver(menu).Contains(args.Contact) */)))
                                                    {
                                                        {
                                                            WPDrag++;

                                                            if (state == RMState.State3)
                                                            {
                                                                turnedIntoDrag = true;
                                                                RM5Drag(t.Position);
                                                            }

                                                            if (state == RMState.State5)
                                                            {
                                                                AddWaypointDot(t.Position);
                                                            }
                                                        }
                                                    }

                                                    if (cc.Count > 1)
                                                    {
                                                        moveStuff(cc);
                                                        if (!cleanedUpDragPoints.Contains(e.Id))
                                                        {
                                                            if (lassoPoints.Contains(e.Position))
                                                                lassoPoints.Remove(e.Position);
                                                            if (lassoLine.Points.Contains(e.Position))
                                                                lassoPoints.Remove(e.Position);
                                                            cleanedUpDragPoints.Add(e.Id);
                                                        }
                                                        return;
                                                    }

                                                    switch (state)
                                                    {
                                                        case RMState.Start:
                                                            if (cc.Count == 1 && (lassoId == -1 || lassoId == e.Id) && index == -1 &&
                                                                selectedList.Count == 0)
                                                            {
                                                                lassoPoints.Add(e.Position);
                                                                if (lassoId == -1)
                                                                {
                                                                    startLasso(e);
                                                                }
                                                                else
                                                                {
                                                                    addToLasso(e.Position);
                                                                }
                                                            }
                                                            break;
                                                        case RMState.State1:
                                                            if (cc.Count > 1) break;
                                                            /* indexdistfuck idf = DistToNearestGestureObject(e.Position); // NEED TO FIX

                                                            if (selectedList.Count > 0 && selectedList.Contains(idf.index) &&
                                                                (idf.distance > ScaleDotToCameraHeight()))
                                                            {
                                                                AddWaypointDot(e.Position);
                                                                if (robots[idf.index].GetColor() == Brushes.Blue)
                                                                    PulseYellow(idf.index);
                                                                ChangeState(RMState.State5);
                                                                break;
                                                            }
                                                            else */
                                                            if ((lassoId == -1 || lassoId == e.Id) && index == -1 &&
                                                                          selectedList.Count == 0)
                                                            {
                                                                lassoPoints.Add(e.Position);
                                                                if (lassoId == -1)
                                                                {
                                                                    startLasso(e);
                                                                }
                                                                else
                                                                {
                                                                    addToLasso(e.Position);
                                                                }
                                                            }

                                                            break;
                                                        case RMState.State2:
                                                            break;
                                                        case RMState.State3:
                                                            if (cc.Count == 1 && index == -1)
                                                            {
                                                                if (selectedList.Count > 0)
                                                                {
                                                                    WPDrag++;
                                                                    turnedIntoDrag = true;
                                                                    RM5Drag(e.Position);
                                                                }
                                                                else
                                                                {
                                                                    RM5End();
                                                                    ChangeState(RMState.State1);
                                                                }
                                                            }

                                                            break;
                                                        case RMState.State4:
                                                            break;
                                                        case RMState.State5:
                                                            if (cc.Count == 1 && index == -1)
                                                            {
                                                                WPDrag++;
                                                                AddWaypointDot(e.Position);
                                                            }
                                                            break;
                                                        default:
                                                            break;
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
                                            if (cleanedUpDragPoints.Contains(e.Id)) cleanedUpDragPoints.Remove(e.Id);
                                            zoomUp();
                                            List<int> beforeLasso = new List<int>();
                                            List<int> newSelection = new List<int>();
                                            int index = robotsCD(e);
                                           
                                            if(index != -1)
                                            {
                                                if (manualRobot == -1)
                                                {
                                                    if (ttime[index] != null && ttime[index].selected)
                                                    //if (!timers.IsRunning(ref turboFingering[index]))
                                                    {
                                                        manualRobot = index;
                                                        if (selectedList.Contains(index))
                                                            selectedList.RemoveAt(index);
                                                        PulseGreen(manualRobot);
                                                        changeManual(_namespace[1] + "/virtual_joystick/cmd_vel", _namespace[1] + "/servos", _namespace[1] + "/camera/rgb/image_color", _namespace[1] + "/scan");
                                                    }
                                                }
                                                else {
                                                    if (ttime[index] != null && ttime[index].selected)
                                                    {
                                                        manualRobot = -1;
                                                        NoPulse(index);
                                                    }
                                                }
                                                
                                            }
                                            switch (state)
                                            {
                                                case RMState.Start:
                                                    beforeLasso.AddRange(selectedList);
                                                    FinishLasso(e);

                                                    newSelection = selectedList.Except(beforeLasso).ToList();
                                                    if (newSelection.Count > 0)
                                                    {
                                                        ChangeState(RMState.State2);//, "CU (lasso done)"
                                                    }

                                                    break;
                                                case RMState.State1:
                                                    beforeLasso.AddRange(selectedList);
                                                    FinishLasso(e);
                                                    newSelection = selectedList.Except(beforeLasso).ToList();
                                                    if (newSelection.Count > 0)
                                                    {
                                                        ChangeState(RMState.State2); // "CU (lasso done)"
                                                    }
                                                    else
                                                    {
                                                        ChangeState(RMState.State2); // "CU"

                                                    }

                                                    break;
                                                case RMState.State2:
                                                    break;
                                                case RMState.State3:
                                                    if (WPDrag > 0)
                                                    {
                                                        WPDrag = 0;
                                                    }

                                                    ChangeState(RMState.State4); // "CU"
                                                    break;
                                                case RMState.State4:
                                                    break;
                                                case RMState.State5:
                                                    if (WPDrag > 0)
                                                    {
                                                        turnedIntoDrag = false;
                                                        WPDrag = 0;
                                                    }

                                                    if (robotsCD(e) == -1)
                                                    {
                                                        ChangeState(RMState.State4);// "CU-G"
                                                    }

                                                    break;
                                                default:
                                                    break;
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

        #region LassoUtility

        private int lassoId = -1;

        /// <summary>
        ///   The start lasso.
        /// </summary>
        /// <param name = "p">
        ///   The p.
        /// </param>
        private void startLasso(Touch e)
        {
            lassoId = e.Id;
            Point p = e.Position;


            // Start adding the new ones.
            lassoLine.Points.Add(p);
            if (lassoLine.Parent == null)
                MainCanvas.Children.Add(lassoLine);

            // Console.WriteLine("Oh yeah!");
        }

        /// <summary>
        ///   The add to lasso.
        /// </summary>
        /// <param name = "p">
        ///   The p.
        /// </param>
        private void addToLasso(Point p)
        {
            lassoLine.Points.Add(p);
        }

        /// <summary>
        ///   The end lasso.
        /// </summary>
        private void endLasso()
        {
            lassoId = -1;
            lassoPoints.Clear();
            lassoLine.Points.Clear();
            MainCanvas.Children.Remove(lassoLine);
        }

        /// <summary>
        ///   The finish lasso.
        /// </summary>
        private void FinishLasso(Touch e)
        {
            if (e.Id != lassoId) { endLasso(); return; }
            lassoId = -1;
            if (lassoPoints.Count != 0)
            {
                if (distance(lassoPoints[0], lassoPoints[lassoPoints.Count - 1]) < 100)
                {
                    for (int i = 0; i < numRobots; i++)
                    {
                        if (!robots.ContainsKey(i)) continue;
                        Point p = new Point((robots[i].xPos + (translate.X)), (robots[i].yPos + (translate.Y)));
                        if (PointInPoly(lassoPoints, p, SubCanvas))
                        {
                            if (!selectedList.Exists(item => item == i))
                            {
                                selectedList.Add(i);
                                PulseYellow(i);
                            }
                        }
                    }
                }
            }

            lassoPoints.Clear();
            endLasso();
        }

        /// <summary>
        ///   The point in poly.
        /// </summary>
        /// <param name = "polygonPoints">
        ///   The polygon points.
        /// </param>
        /// <param name = "testPoint">
        ///   The test point.
        /// </param>
        /// <returns>
        ///   The point in poly.
        /// </returns>
        public static bool PointInPoly(List<Point> polygonPoints, Point testPoint,System.Windows.Controls.Canvas canv)
        {
            
            bool isIn = false;
            int i, j = 0;
            //for (i = 0; i < polygonPoints.Count; i++)
            //{
            //    polygonPoints[i] = SurfaceWindow1.current.MainCanvas.TranslatePoint(polygonPoints[i], canv);
            //     Console.WriteLine(polygonPoints[i].X + "," +polygonPoints[i].Y);
            //}
            for (i = 0, j = polygonPoints.Count - 1; i < polygonPoints.Count; j = i++)
            {
                if (
                    (((polygonPoints[i].Y <= testPoint.Y) && (testPoint.Y < polygonPoints[j].Y)) ||
                     ((polygonPoints[j].Y <= testPoint.Y) && (testPoint.Y < polygonPoints[i].Y))) &&
                    (testPoint.X <
                     (polygonPoints[j].X - polygonPoints[i].X) * (testPoint.Y - polygonPoints[i].Y) /
                     (polygonPoints[j].Y - polygonPoints[i].Y) + polygonPoints[i].X)
                    )
                {
                    isIn = !isIn;
                }
            }

            return isIn;
        }

        #endregion

        public void HandOutWaypoints()
        {
            lock (waypointDots)
            {
                if (waypointDots.Count == 0)
                    return;
            }

            List<Point> waypoints = new List<Point>(Waypoint.PointLocations);
            int[] sel;
            Action asyncWaypointStuff;
            double newx;
            double newy;
            double newwx;
            double newwy;
            lock (translate)
            {
                newx = translate.X;
                newy = translate.Y;
            }
            lock (scale)
            {
                newwx = scale.ScaleX;
                newwy = scale.ScaleY;
            }
            Brush Tmp_Brush;
            foreach (int r in selectedList)
            {
                lock (GoalDot.ColorInUse)
                {
                    if (GoalDot.ColorInUse.ContainsKey(r))
                    {
                        Tmp_Brush = GoalDot.ColorInUse[r];
                        GoalDot.ColorInUse.Remove(r);
                        for (int i = 1; i <= GoalDot.ColorInUse.Count + 1; i++)
                            if (!GoalDot.ColorInUse.ContainsKey(-i))
                            {
                                GoalDot.ColorInUse.Add((-i), Tmp_Brush);
                                break;
                            }
                       robots[r].robot.ChangeIconColors(robots[r].robot.circles.IndexOf(robots[r].robot.Border.Stroke));
                    }
                }
                bool Done = false;
                foreach (CommonList CL in RobotControl.OneInAMillion)
                {

                    foreach (Robot_Info RI in CL.RoboInfo)
                        if (RI.RoboNum == r && !RI.done)
                        {
                            Done = true;
                            RI.done = true;
                            RI.CurrentLength = CL.P_List.Count;
                            foreach (Robot_Info DoneCheck in CL.RoboInfo)
                                if (!DoneCheck.done)
                                    Done = false;

                            if (Done)
                            {
                                foreach (GoalDot GD in CL.Dots)
                                   DotCanvas.Children.Remove(GD);
                                CL.P_List.Clear();
                                CL.RoboInfo.Clear();
                                lock (RobotControl.OneInAMillion)
                                    RobotControl.OneInAMillion.Remove(CL);
                                break;
                            }
                        }
                    if (Done) break;
                }

            }

            foreach (int r in selectedList)
                for (int i = 0; i < GoalDot.ColorInUse.Count; i++)
                    if (GoalDot.ColorInUse.ElementAt(i).Key < 0)
                    {
                        Tmp_Brush = GoalDot.ColorInUse.ElementAt(i).Value;
                        GoalDot.ColorInUse.Remove(GoalDot.ColorInUse.ElementAt(i).Key);
                        GoalDot.ColorInUse.Add(r, Tmp_Brush);
                        robots[r].robot.setArrowColor(Tmp_Brush);
                        break;
                    }
            lock (waypointDots)
            {
                
                sel = selectedList.ToArray();
                asyncWaypointStuff = new Action(() =>
                {
                    foreach (int k in sel)
                    {
                        robots[k].updateWaypoints(waypoints, newx, newy, newwx, newwy);
                    }
                });


                
                foreach (Waypoint wp in waypointDots)
                {
                   DotCanvas.Children.Remove(wp.dot);
                }
                waypointDots.Clear();
                Waypoint.PointLocations.Clear();
            }
            asyncWaypointStuff.BeginInvoke((iar) =>
            {
                waypoints.Clear();
                waypoints = null;
                             
                sel = null;
            }, null);

            NoPulse();
            selectedList.Clear();
        }
        public List<Point> Get_Offset(List<Point> PIn)
        {
            List<Point> POut = new List<Point>();

            Dispatcher.Invoke(new Action(() =>
            {
                foreach (Point p in PIn)
                {
                    POut.Add(new Point(((p.X + dottranslate.X) * dotscale.ScaleX) - (DotCanvas.Width * scale.ScaleX / 2) + (SubCanvas.ActualWidth / 2)
                        , ((p.Y + dottranslate.Y) * scale.ScaleY) - (DotCanvas.Width * scale.ScaleY / 2) + (SubCanvas.ActualHeight / 2)));
                }
            }));
            return POut;

        }
        public void EndState(string s)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                WPDrag = 0;
                ChangeState(RMState.Start);
                lock (waypointDots)
                {
                    foreach (Waypoint wp in waypointDots)
                        DotCanvas.Children.Remove(wp.dot);
                    waypointDots.Clear();
                    Waypoint.PointLocations.Clear();
                }
                NoPulse();
                selectedList.Clear();
            }));
        }


        private void ZoomDown(Touch e)
        {
            if (FREE.Count == 1)
            {
                lassoDontThrowAway.AddRange(lassoPoints.Except(lassoDontThrowAway));
            }
            if (FREE.Count > 1)
            {
                foreach (Touch c in FREE.Values)
                {
                    if (!cleanedUpDragPoints.Contains(c.Id))
                    {
                        if (lassoPoints.Contains(c.Position))
                            lassoPoints.Remove(c.Position);
                        if (lassoLine.Points.Contains(c.Position))
                            lassoPoints.Remove(c.Position);
                        cleanedUpDragPoints.Add(c.Id);
                    }
                }
            }
        }

        private void zoomUp()
        {
            if (FREE.Count > 1)
            {
                lassoPoints = lassoPoints.Intersect(lassoDontThrowAway.AsEnumerable()).ToList();
            }
        }


        // List<Point> waypointList = new List<Point>();
        /// <summary>
        ///   Contains the list of all the waypoints that the robots currently selected will have to go to.
        /// </summary>
        /// <summary>
        ///   The menu that will be shown whenever wanted!.
        /// </summary>
        private int WPDrag;
        private Timer RM5Timer;
        private Point lastTouch;

        /// <summary>
        ///   The lasso dont throw away.
        /// </summary>
        private List<Point> lassoDontThrowAway = new List<Point>();

        private List<int> cleanedUpDragPoints = new List<int>();
        /// <summary>
        ///   The r m 5 drag.
        /// </summary>
        /// <param name = "last">
        ///   The last.
        /// </param>
        public void RM5Drag(Point last)
        {
            if (state != RMState.State5)
                ChangeState(RMState.State5);
            if (timers.IsRunning(ref RM5Timer))
            {
                AddWaypointDot(lastTouch);
                RM5End();
            }

            lastTouch = last;
            timers.StartTimer(ref RM5Timer);
        }

        /// <summary>
        ///   The r m 5 start.
        /// </summary>
        /// <param name = "last">
        ///   The last.
        /// </param>
        public void RM5Start(Point last)
        {
            if (!turnedIntoDrag && Math.Abs(distance(last, lastTouch)) < DTDistance && timers.IsRunning(ref RM5Timer))
            {
                RM5DoIt();
                RM5End();
                return;
            }
            else
            {
                ChangeState(RMState.State3);
                if (timers.IsRunning(ref RM5Timer))
                {
                    AddWaypointDot(lastTouch);
                    RM5End();
                }

                lastTouch = last;
                timers.StartTimer(ref RM5Timer);
            }
        }

        private double DTDistance
        {
            get
            {
                if (joymgr != null)
                    return 0.5 * joymgr.DPI;
                return 30;
            }
        }

        /// <summary>
        ///   The r m 5 do it.
        /// </summary>
        private void RM5DoIt()
        {
            AddWaypointDot(lastTouch);
            HandOutWaypoints();
            EndState("CD-G (t<dt && d<dD)");
        }

        /// <summary>
        ///   The r m 5 timer_ tick.
        /// </summary>
        /// <param name = "sender">
        ///   The sender.
        /// </param>
        private void RM5Timer_Tick(object sender)
        {
            RM5End();
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!turnedIntoDrag && captureVis.Count >= 1)
                {
                    AddWaypointDot(lastTouch);
                }
            }));
        }

        /// <summary>
        ///   The r m 5 end.
        /// </summary>
        public void RM5End()
        {
            timers.StopTimer(ref RM5Timer);
        }

        /// <summary>
        ///   The lasso line dash style.
        /// </summary>
        private DoubleCollection lassoLineDashStyle = new DoubleCollection(2);


        /// <summary>
        ///   The yellow current.
        /// </summary>
        private double yellowCurrent = 0.2;

        /// <summary>
        ///   The yellow min.
        /// </summary>
        private double yellowMin = 0.2;

        /// <summary>
        ///   The yellow max.
        /// </summary>
        private double yellowMax = 0.7;

        /// <summary>
        ///   The yellow delta.
        /// </summary>
        private double yellowDelta = 0.02;

        /// <summary>
        ///   The green current.
        /// </summary>
        private double greenCurrent = 0.1;

        /// <summary>
        ///   The green min.
        /// </summary>
        private double greenMin = 0.1;

        /// <summary>
        ///   The green max.
        /// </summary>
        private double greenMax = 0.9;

        /// <summary>
        ///   The green delta.
        /// </summary>
        private double greenDelta = 0.05;

        /// <summary>
        ///   These are the possible states for the robot movement FSM.
        /// </summary>
        public enum RMState
        {
            /// <summary>
            ///   The start.
            /// </summary>
            Start = 0,

            /// <summary>
            ///   The r m 1.
            /// </summary>
            State1,

            /// <summary>
            ///   The r m 2.
            /// </summary>
            State2,

            /// <summary>
            ///   The r m 3.
            /// </summary>
            State3,

            /// <summary>
            ///   The r m 4.
            /// </summary>
            State4,

            /// <summary>
            ///   The r m 7.
            /// </summary>
            State5
        } ;

        /// <summary>
        ///   The RM FSM state.
        /// </summary>
        private RMState state = RMState.Start;


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