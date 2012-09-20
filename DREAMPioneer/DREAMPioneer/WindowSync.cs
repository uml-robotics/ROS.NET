#region USINGZ

using System.Windows.Controls;
using GenericTypes_Surface_Adapter;
using Messages.whosonfirst;
using Messages.wpf_msgs;
using ROS_ImageWPF;
using SpeechLib;
using System.Diagnostics;
using System;
using System.Linq;
using System.Collections.Generic;
#if SURFACEWINDOW
using GenericTypes_Surface_Adapter;
using Microsoft.Surface;
using Microsoft.Surface.Core;
using ContactEventArgs = Microsoft.Surface.Core.ContactEventArgs;
using Contact = Microsoft.Surface.Core.Contact;
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
using WindowState = Messages.wpf_msgs.WindowState;
using cm = Messages.custom_msgs;
using tf = Messages.tf;
using System.Text;
using System.ComponentModel;
using otherTimer = System.Timers;

#endregion
namespace DREAMPioneer
{
    public class WindowStatus : EMVoodoo
    {
        public string callerID;
        public int manualRobot;
        public Rectangle POVbox;
        Point lastTL;
        Size lastSize;
        public void DeInit()
        {
            BeginInvoke(() =>
                {
                    if (POVbox != null && POVbox.Parent != null)
                    {
                        window.MainCanvas.Children.Remove(POVbox);
                        POVbox = null;
                    }
                });
            manualRobot = -1;
            lastTL = new Point();
            lastSize = new Size();
        }
        public void UpdateBox(Messages.wpf_msgs.WindowState state)
        {
            UpdateBox(state.topleft, state.bottomright, state.width, state.height);
        }
        public void UpdateBox(Point2 topleft, Point2 bottomright, double width, double height)
        {
            BeginInvoke(() =>
                {
                    WindowPositionStuff pos = window.map.WhereYouAt(window);
                    double w = bottomright.x - topleft.x;
                    double h = bottomright.y - topleft.y;
                    double scaleXY = (w / pos.size.Width) * window.Width;
                    if (POVbox == null)
                    {
                        POVbox = new Rectangle { Stroke = Brushes.CadetBlue, StrokeThickness = 4 };
                        window.MainCanvas.Children.Add(POVbox);
                    }
                    Point newTL = new Point(topleft.x - pos.TL.X, topleft.y - pos.TL.Y);
                    Size newSize = new Size(width / scaleXY, height / scaleXY);
                    if (newTL.X != lastTL.X || newTL.Y != lastTL.Y)
                    {
                        lastTL = newTL;
                        POVbox.SetValue(Canvas.LeftProperty, newTL.X);
                        POVbox.SetValue(Canvas.TopProperty, newTL.Y);
                    }
                    if (newSize.Width != lastSize.Width || newSize.Height != lastSize.Height)
                    {
                        lastSize = newSize;
                        POVbox.Width = newSize.Width;
                        POVbox.Height = newSize.Height;
                    }
                });
        }
    }
    public class RobotState : EMVoodoo
    {
        public int robotNum;
        public ROSData data;
        public static Dictionary<int, RobotState> robots = new Dictionary<int, RobotState>();
        private bool _commandable;
        private string owner;
        public static void Init(int r, ROSData data)
        {
            if (!robots.ContainsKey(r))
                robots.Add(r, new RobotState(r, data));
        }
        public bool Commandable
        {
            get
            {
                return _commandable;
            }
        }
        public RobotState(int r, ROSData data)
        {
            robotNum = r;
            this.data = data;
        }

        public void OwnerChanged(string o)
        {
            if (o == null || o == WindowSync.MY_CALLER_ID)
                _commandable = true;
            owner = o;
        }
    }

    public class WindowSync : EMVoodoo
    {
        public static string MY_CALLER_ID;
        private NodeHandle nodeHandle;
        private Publisher<Dibs> hellopub;
        private Subscriber<Dibs> hellosub;
        private Publisher<Dibs> manualpub;
        private Subscriber<Dibs> manualsub;
        private Publisher<Dibs> goodbyepub;
        private Subscriber<Dibs> goodbyesub;
        private Subscriber<Messages.move_base_msgs.MoveBaseActionGoal>[] goalsubs;
        private Publisher<Waypoints> wppub;
        private Subscriber<Waypoints> wpsub;
        private Publisher<WindowState> winpub;
        private Subscriber<WindowState> winsub;
        private static float winw, winh;
        public Dictionary<string, WindowStatus> otherwindows = new Dictionary<string, WindowStatus>();
        Thread stateThread, spinThread;
        public Dictionary<int, RobotState> robots
        {
            get
            {
                return RobotState.robots;
            }
        }

        private void receivedHello(string boostmobile, Dibs m)
        {
            if (boostmobile != MY_CALLER_ID)
            {
                Console.WriteLine("OTHER CLIENT " + boostmobile + " SAID HELLO AND THAT ITS MANUAL IS " + m.manualRobot);
                if (!otherwindows.ContainsKey(boostmobile))
                    otherwindows.Add(boostmobile, new WindowStatus { manualRobot = m.manualRobot });
                int r = otherwindows[boostmobile].manualRobot;
                if (r > 0)
                    robots[r].OwnerChanged(null);
                //TODO DO SOMETHING WHEN ANOTHER CLIENT CONNECTS
            }
        }

        private void receivedGoodbye(string boostmobile, Dibs m)
        {
            if (boostmobile != MY_CALLER_ID)
            {
                Console.WriteLine("OTHER CLIENT " + boostmobile + " SAID GOODBYE AND THAT ITS MANUAL IS " + m.manualRobot);
                //TODO INDICATE THE OTHER CLIENT IS CONTROLLING THIS ROBOT
                if (!otherwindows.ContainsKey(boostmobile))
                {
                    int r = otherwindows[boostmobile].manualRobot;
                    otherwindows[boostmobile].DeInit();
                    otherwindows.Remove(boostmobile);
                    if (r > 0)
                        robots[r].OwnerChanged(null);
                }
            }
        }

        private void receivedManualChange(string boostmobile, Dibs m)
        {
            if (boostmobile != MY_CALLER_ID)
            {
                Console.WriteLine("OTHER CLIENT " + boostmobile + " CHANGED MANUAL TO " + m.manualRobot);
                if (!otherwindows.ContainsKey(boostmobile))
                    otherwindows.Add(boostmobile, new WindowStatus { manualRobot = m.manualRobot });
                else
                    otherwindows[boostmobile].manualRobot = m.manualRobot;
                //TODO INDICATE THE OTHER CLIENT IS CONTROLLING THIS ROBOT
            }
        }

        private void goalSent(int robotindex, Messages.move_base_msgs.MoveBaseActionGoal msg)
        {
            string boostmobile = (string)(msg.connection_header["callerid"]);
            if (boostmobile != MY_CALLER_ID)
            {
                Console.WriteLine(robotindex + " received a new individual destination from " + boostmobile);
                //TODO SOMETHING... ITD BE COOL IF ROBOTS FOLLOWING OTHER USER'S DOTS HAD DOTS WITH LOWER OPACITY?
            }
        }

        private void receivedWaypoints(string boostmobile, Waypoints m)
        {
            if (boostmobile != MY_CALLER_ID)
            {
                StringBuilder sb = new StringBuilder("OTHER CLIENT " + boostmobile + " SENT " + m.path.Length + " WAYPOINTS TO ");
                foreach (int i in m.robots)
                {
                    sb.AppendLine("\t" + i);
                }
                Console.WriteLine(sb);
            }
        }

        private void receivedWindowState(string boostmobile, WindowState m)
        {
            if (boostmobile != MY_CALLER_ID)
            {
                Console.WriteLine("OTHER CLIENT " + boostmobile + " SENT IITS WINDOW STATE!");
                otherwindows[boostmobile].UpdateBox(m);
            }
        }

        public WindowSync(string myCallerId)
        {
            // TODO: Complete member initialization
            MY_CALLER_ID = myCallerId;
            nodeHandle = new NodeHandle();
            spinThread = new Thread(() =>
                {
                    while (ROS.ok)
                    {
                        ROS.spinOnce(nodeHandle);
                        Thread.Sleep(1);
                    }
                });
            spinThread.Start();
            #region pubsubsubsubsubsubs
            hellopub = nodeHandle.advertise<Dibs>("/hello", 1, true);
            hellosub = nodeHandle.subscribe<Dibs>("/hello", 1, (m) => receivedHello((string)m.connection_header["callerid"], m));
            goodbyepub = nodeHandle.advertise<Dibs>("/goodbye", 1, true);
            goodbyesub = nodeHandle.subscribe<Dibs>("/goodbye", 1, (m) => receivedGoodbye((string)m.connection_header["callerid"], m));
            manualpub = nodeHandle.advertise<Dibs>("/manual", 1, true);
            manualsub = nodeHandle.subscribe<Dibs>("/manual", 1, (m) => receivedManualChange((string)m.connection_header["callerid"], m));
            wppub = nodeHandle.advertise<Waypoints>("/waypoints", 10, true);
            wpsub = nodeHandle.subscribe<Waypoints>("/waypoints", 10, (m) => receivedWaypoints((string)m.connection_header["callerid"], m));
            winsub = nodeHandle.subscribe<WindowState>("/windowstate", 10, (m) => receivedWindowState((string)m.connection_header["callerid"], m));
            winpub = nodeHandle.advertise<WindowState>("/windowstate", 10, true);
            #endregion
            stateThread = new Thread(() =>
                {
                    while (ROS.ok)
                    {
                        WindowState ws = new WindowState();
                        if (winw == 0 || winh == 0)
                            Invoke(() =>
                                {
                                    winw = (float)window.Width;
                                    winh = (float)window.Height;
                                });
                        WindowPositionStuff pos = null;
                        Invoke(() => pos = window.map.WhereYouAt(window));
                        ws.height = winw;
                        ws.height = winh;
                        if (pos != null)
                        {
                            ws.topleft = new Point2 { x = pos.TL.X, y = pos.TL.Y };
                            ws.bottomright = new Point2 { x = pos.BR.X, y = pos.BR.Y };
                        }
                        winpub.publish(ws);
                        Thread.Sleep(10);
                    }
                });
            stateThread.Start();

            hellopub.publish(new Dibs { manualRobot = -1 });

            goalsubs = new Subscriber<Messages.move_base_msgs.MoveBaseActionGoal>[SurfaceWindow1.MAX_NUMBER_OF_ROBOTS];
            for (int i = 0; i < SurfaceWindow1.MAX_NUMBER_OF_ROBOTS; i++)
            {
                int scoped = i;
                goalsubs[i] = nodeHandle.subscribe<Messages.move_base_msgs.MoveBaseActionGoal>("/robot_brain_" + (i + 1) + "/move_base/goal", 1, (m) => goalSent(scoped, m));
            }
        }

        internal void PublishWaypoints(List<Point> points, params int[] indexes)
        {
            wppub.publish(Convert(points, indexes));
        }

        internal void PublishManual(int manualRobot)
        {
            manualpub.publish(new Dibs { manualRobot = manualRobot });
        }
        private Waypoints Convert(List<Point> points, params int[] indexes)
        {
            Waypoints wp = new Waypoints { path = Convert(points) };
            wp.robots = new int[indexes.Length];
            indexes.CopyTo(wp.robots, 0);
            return wp;
        }
        private Point2[] Convert(List<Point> points)
        {
            return points.Select(Convert).ToArray();
        }
        private Point2 Convert(Point p)
        {
            return new Point2 { x = p.X, y = p.Y };
        }

        internal void Goodbye()
        {
            goodbyepub.publish(new Dibs { manualRobot = -1 });
        }
    }

    public class EMVoodoo
    {
        #region Dispatcher Helpers
        protected static SurfaceWindow1 window
        {
            get
            {
                return SurfaceWindow1.current;
            }
        }
        protected static void BeginInvoke(windowact b)
        {
            window.Dispatcher.BeginInvoke(b);
        }
        protected static void Invoke(windowact b)
        {
            window.Dispatcher.Invoke(b);
        }

        protected delegate void windowact();
        #endregion
    }
}
