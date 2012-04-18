//#define NNTest
#define DONT_AUTO_CHANGE_SERVICES
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Media.Media3D;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Diagnostics;


namespace EM3MTouchLib
{
    public class EM3MTouch
    {
        #region Constants
        //REAL SCREEN
        private const int VID = 0x0596;
        private const int PID = 0x0502;

        //MOUSE FOR TESTING
        //private const int VID = 0x1532;
        //private const int PID = 0x0013;
        #endregion
        #region Non-Constants
        private int HorizontalResolution = 0;
        private int VerticalResolution = 0;
        public SortedList<int, TouchEventArgs> Touches = new SortedList<int, TouchEventArgs>();
        private bool[] Down = new bool[60];
        public HIDDevice TouchScreen;
        #endregion
        #region Event Declarations
        public delegate void TouchDelegate(TouchEventArgs e);
        public event TouchDelegate DownEvent;
        public event TouchDelegate UpEvent;
        public event TouchDelegate ChangedEvent;
        public delegate void EmptyDelegate();
        public event EmptyDelegate FoundScreens;
        #endregion
        private bool PHISHSURFACE = false;
        private int PRIMARYSCREENINDEX = -1;
        private int SECONDARYSCREENINDEX = -1;
        public bool MULTISCREEN = false;
        public Point OFFSET = new Point(0, 1050);
        public EM3MTouch() { }
        public EM3MTouch(Window window) : this(window.ActualWidth, window.ActualHeight) { }
        public EM3MTouch(double h, double v) : this((int)Math.Floor(h), (int)Math.Floor(v)) { }
        public EM3MTouch(int hRes, int vRes)
        {
            me = this;
            HorizontalResolution = hRes;
            VerticalResolution = vRes;
        }
        PrimaryScreenFinder screenFinder = null;
        private string[] _THINGSTOKILL = new string[] { "wisptis", "TabTip", "InputPersonalization" };
        private string[] _THINGSTOKILLpaths = new string[] { "C:\\Windows\\System32\\", "C:\\Windows\\System32\\", "C:\\Program Files\\Common Files\\Microsoft Shared\\ink" };
        private string _SERVICETOKILL = "TabletInputService";
        private bool _CHANGEDSERVICESTATUS = false;
        private List<int> _KilledProcesses = new List<int>();
        private void ProcessKiller(string name)
        {
            Process[] ps = Process.GetProcessesByName(name);
            if (ps.Length == 0) return;
            for (int i = 0; i < ps.Length; i++)
            {
                ps[i].Kill();
                _KilledProcesses.Add(i);
            }
            Console.WriteLine("Ended Process:  " + name);
        }


        private Thread GentleMurderer;
        private void GentleMurder()
        {
            GentleMurderer = new Thread(new ThreadStart(GentlyMurderProcessesAndServices));
            GentleMurderer.Priority = ThreadPriority.Highest;            
            GentleMurderer.Start();
        }
        private void GentlyMurderProcessesAndServices()
        {
#if AUTO_CHANGE_SERVICES
            Process.Start("sc", "start " + _SERVICETOKILL);
           Process.Start("sc", "stop " + _SERVICETOKILL);
           Console.WriteLine("Stopped Service:  " + _SERVICETOKILL);
            foreach (string s in _THINGSTOKILL)
            {
                ProcessKiller(s);
            }
            _CHANGEDSERVICESTATUS = true;            
#endif
        }
        private void GentleResurect()
        {
            GentleMurderer = new Thread(new ThreadStart(GentlyResurectProcessesAndServices));
            GentleMurderer.Priority = ThreadPriority.Highest;
            GentleMurderer.Start();
        }
        private void GentlyResurectProcessesAndServices()
        {
#if AUTO_CHANGE_SERVICES
            if (!_CHANGEDSERVICESTATUS)
                return;
            _CHANGEDSERVICESTATUS = false;
            Process.Start("sc", "start " + _SERVICETOKILL);            
            Console.WriteLine("Retarted Service:  " + _SERVICETOKILL);            
#endif
        }
        public bool Connect()
        {
            GentleMurder();
            if (DownEvent == null || UpEvent == null || ChangedEvent == null)
            {
                Exception up = new Exception("Register Event Handlers before calling Start()");
                throw up; //teehee
            }
                try
                {
                    TouchScreen = new HIDDevice();
                    int numscreens = TouchScreen.FindThatShit();
                    Console.WriteLine("Found " + numscreens + " screens.");
                    if (numscreens > 1)
                    {
                        MULTISCREEN = true;
                        VerticalResolution /= numscreens;
                        Thread formThread = new Thread(new ThreadStart(ShowForm));
                        formThread.SetApartmentState(ApartmentState.STA);
                        formThread.Start();
                    }
                    Console.WriteLine((TouchScreen == null) ? "No Dice" : "Kick the tires and light the fires");
                    TouchScreen.Finger += new HIDDevice.TouchDelegate(TouchScreen_Finger);
                    TouchScreen.FingerUp += new HIDDevice.TouchDelegate(TouchScreen_FingerUp);
                    TouchScreen.OnDeviceRemoved += new EventHandler(TouchScreen_OnDeviceRemoved);
                    return (numscreens > 0);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            
            return false;
        }

        public delegate void politeShutdown(string reason);
        public event politeShutdown PolitelyShutdown;

        public void Shutdown()
        {
            GentlyResurectProcessesAndServices();
            TouchScreen.Finger -= new HIDDevice.TouchDelegate(TouchScreen_Finger);
            TouchScreen.FingerUp -= new HIDDevice.TouchDelegate(TouchScreen_FingerUp);
            TouchScreen.OnDeviceRemoved -= new EventHandler(TouchScreen_OnDeviceRemoved);
            TouchScreen.Die();
        }

        void TouchScreen_OnDeviceRemoved(object sender, EventArgs e)
        {
            Console.WriteLine("OH NOEZ SCREENZORX UNPLUGGEDXORZ!");
            Shutdown();
            if (PolitelyShutdown != null) PolitelyShutdown("Screen unplugged");            
        }

        void ShowForm()
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.Run((screenFinder = new PrimaryScreenFinder()));
        }
           
        public void phisher_Touched(int Id, double X, double Y, double W, double H, double A, int Type)
        {
            PHISHSURFACE = true;
            if (Type == 2)
                TouchScreen_FingerUp(Calibrate(new TouchEventArgs(Id, (X), (Y), (W), (H), A)));
            else
                TouchScreen_Finger(Calibrate(new TouchEventArgs(Id, (X), (Y), (W), (H), A)));
        }
        TouchEventArgs Calibrate(TouchEventArgs e)
        {
            Point p = e.Contact.Position;
            if (!PHISHSURFACE)
            {                
                p.X *= (((double)HorizontalResolution) / ((double)0x7FFF));
                p.Y *= (((double)VerticalResolution) / ((double)0x7FFF));
                if (MULTISCREEN == true && PRIMARYSCREENINDEX != -1 && SECONDARYSCREENINDEX != -1)
                {
                    p.Y *= 2;
                    if (e.SCREEN == SECONDARYSCREENINDEX)
                    {
                        p.X += OFFSET.X;
                        p.Y += OFFSET.Y;
                        e.Contact.Id += 20;
                    }
                }
                e.Contact.Position = p;
                e.Contact.Width *= (((double)HorizontalResolution) / ((double)0x7FFF));
                e.Contact.Height *= (((double)VerticalResolution) / ((double)0x7FFF));
            }
            else
            {                
                p.X *= (((double)HorizontalResolution) / ((double)1024));
                p.Y *= (((double)VerticalResolution) / ((double)768));
                e.Contact.Position = p;
                e.Contact.Width *= (((double)HorizontalResolution) / ((double)1024));
                e.Contact.Height *= (((double)VerticalResolution) / ((double)768));
            }
            return e;
        }

        private static EM3MTouch me;
        public static void KillMe(Touch suicidee)
        {
            me.Kill(suicidee);
        }
        private void Kill(Touch suicidee)
        {
            try
            {
                TouchScreen_FingerUp(new TouchEventArgs(suicidee));
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception trying to kill stale contact in window-land: \n" + e);                
            }
        }

        void TouchScreen_FingerUp(TouchEventArgs e)
        {
            //Console.WriteLine("UP in TOUCHLIB = " + e);
            if (!MULTISCREEN || MULTISCREEN && PRIMARYSCREENINDEX != -1 && SECONDARYSCREENINDEX != -1)
            {
                try
                {
                    TouchEventArgs t = Calibrate(e);
                    Touch.IHateSeeingYouLeaveButYourButtIsAttractive(t.Contact.Id);
                    if (Touches.ContainsKey(t.Contact.Id))
                    {
                        Touches.Remove(t.Contact.Id);
                        UpEvent(t);
                    }
                }
                catch (Exception ex) { Console.WriteLine(ex); }
#if NNTest
            NearestNeighbor();
#endif
            }
        }

        void TouchScreen_Finger(TouchEventArgs e)
        {
            if (! MULTISCREEN || MULTISCREEN && PRIMARYSCREENINDEX != -1 && SECONDARYSCREENINDEX != -1)
            {
                try
                {
                    TouchEventArgs t = Calibrate(e);
                    //if (Touches.ContainsKey(t.Contact.Id) && Touches[t.Contact.Id].Contact.Position == t.Contact.Position)                    
                    //    return;                    
                    Touch.Update(t.Contact);
                    Touch.CheckYourselfBeforeYouWreckYourselfBecauseShotgunBulletsAreBadForYourHealth(t.Contact.Id);
                    if (!Touches.ContainsKey(t.Contact.Id))
                    {
#if NNTest
                    clearondown = false;
                    points.Add(e.Contact.Position);
#endif
                        Touches.Add(t.Contact.Id, t);
                        DownEvent(t);
                    }
                    else
                    {
#if NNTest
                    points.Add(e.Contact.Position);
#endif
                        Touches[t.Contact.Id] = t;
                        ChangedEvent(t);
                    }
                }
                catch (Exception ex) { Console.WriteLine("Exception in TouchScreen_Finger: " + ex); }
                return;
            }
            if (MULTISCREEN && PRIMARYSCREENINDEX == -1)
            {
                PRIMARYSCREENINDEX = e.SCREEN;
                screenFinder.Invoke(new Action(() => screenFinder.Top = screenFinder.Location.Y + 1050));
                return;
            }
            if (MULTISCREEN && PRIMARYSCREENINDEX != -1 && SECONDARYSCREENINDEX == -1 && e.SCREEN != PRIMARYSCREENINDEX)
            {
                SECONDARYSCREENINDEX = e.SCREEN;
                screenFinder.Invoke(new Action(() => screenFinder.Close()));
                screenFinder = null;
                if (FoundScreens!=null) FoundScreens();
                return;
            }            
        }
        
#if NNTest
        bool clearondown = false;
        void NearestNeighbor()
        {
            if (clearondown)
                return;
            clearondown = true;
            int pointcount = points.Count;
            DateTime start = DateTime.Now;
            List<Point> P = new List<Point>(points);
            double closest = double.PositiveInfinity;
            points.Clear();
            for (int i = 0; i < P.Count; i++)
            {
                List<Point> O = new List<Point>(P);
                O.RemoveAll((item) => item == P[i]);
                for (int j = 0; j < O.Count; j++)
                {
                    double d = DistanceBetween(P[i], O[j]);
                    if (d < closest)
                        closest = d;
                }
            }
            Console.WriteLine(string.Format("Minimum Nearest Neighbor Distance (Last {0:0} UnCalibrated Points) = {1:0.0###}       (calc took {2:0}ms)", pointcount, closest, DateTime.Now.Subtract(start).TotalMilliseconds));            
        }
        double DistanceBetween(Point a, Point b)
        {
            return Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)));
        }
        List<Point> points = new List<Point>();
#endif
    }
}
