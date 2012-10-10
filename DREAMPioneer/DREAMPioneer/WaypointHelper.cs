using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Ros_CSharp;
using System.Threading;
using System.Windows.Media;
using window = DREAMPioneer.SurfaceWindow1;

namespace DREAMPioneer
{
    public class WaypointHelper
    {
        public static uint GoalCounter;
        public static WaypointPubSubs[] PubSubs;
        public string ID;
        public GoalDot goalDot;
        public Point point;
        public Point realpoint;
        public byte status;
        public List<int> robotswhohavethiswaypoint = new List<int>();
        
                                    
        public class WaypointPubSubs
        {
            public string Name;
            public static NodeHandle node;
            public Subscriber<Messages.actionlib_msgs.GoalStatusArray> goalsub;
            public Publisher<Messages.move_base_msgs.MoveBaseActionGoal> goalPub;        
            public Publisher<Messages.actionlib_msgs.GoalID> goalCanceler;
            public void Initialize(string name)
            {
                Name = name;
                if (node == null)
                {
                    node = new NodeHandle();
                    new Thread(() =>
                    {
                        while (ROS.ok)
                        {
                            ROS.spinOnce(node);
                            Thread.Sleep(100);
                        }
                    }).Start();
                }
                goalPub = node.advertise<Messages.move_base_msgs.MoveBaseActionGoal>(name + "/move_base/goal", 1);
                goalCanceler = node.advertise<Messages.actionlib_msgs.GoalID>(name + "/move_base/cancel", 1);
            }
            public void SubSetup(string name, CallbackDelegate<Messages.actionlib_msgs.GoalStatusArray> handler)
            {
                goalsub = node.subscribe<Messages.actionlib_msgs.GoalStatusArray>(name, 1, handler);
            }
        }
        public static void Publish(Point p, int r, uint c)
        {
            //PubSubs[r].goalPub.publish(
                RobotControl.TwoInAMillion[r].RobotInfowned[r].myList.Enqueue(new Messages.move_base_msgs.MoveBaseActionGoal()
                {
                    header = new Messages.std_msgs.Header()
                    {
                        frame_id = new Messages.std_msgs.String(PubSubs[r].Name + "/map"),
                        stamp = ROS.GetTime(),
                        seq = (uint)c
                    },
                    goal_id = new Messages.actionlib_msgs.GoalID()
                    {
                        stamp = ROS.GetTime(),
                        id = new Messages.std_msgs.String("" + c)
                    },
                    goal = new Messages.move_base_msgs.MoveBaseGoal()
                    {
                        target_pose = new Messages.geometry_msgs.PoseStamped()
                        {
                            header = new Messages.std_msgs.Header()
                            {
                                frame_id = new Messages.std_msgs.String(PubSubs[r].Name + "/map"),
                                stamp = ROS.GetTime(),
                                seq = (uint)c
                            },
                            pose = new Messages.geometry_msgs.Pose()
                            {
                                position = new Messages.geometry_msgs.Point()
                                {
                                    x = p.X * (double)ROS_ImageWPF.MapControl.MPP,
                                    y = p.Y * (double)ROS_ImageWPF.MapControl.MPP,
                                    z = 0
                                },
                                orientation = new Messages.geometry_msgs.Quaternion() { x = 0, y = 0, z = 0, w = 1 }
                            }
                        }
                    }
                }
                );
        }
        public static void Publish(int r, Messages.move_base_msgs.MoveBaseActionGoal m)
        {
            m.header.stamp = ROS.GetTime();
            m.goal.target_pose.header.stamp = ROS.GetTime();
            PubSubs[r].goalPub.publish(m);
        }

        public static void Publish(List<Point> wayp, params int[] indeces)
        {
            CommonList DisList = new CommonList(wayp);
            
            RobotControl.OneInAMillion.Add(DisList);
            foreach (int i in indeces)
            {
                if (!RobotControl.TwoInAMillion.ContainsKey(i))
                    RobotControl.TwoInAMillion.Add(i, DisList);
                else
                {
                    RobotControl.TwoInAMillion[i] = DisList;
                }
            }

            foreach (int ID in indeces)
            {
                SurfaceWindow1.current.Dispatcher.Invoke(new Action(() =>
                {
                    window.current.ROSStuffs[ID].myRobot.robot.setArrowColor(RobotColor.getMyColor(ID));
                    DisList.RobotInfowned.Add(ID, new Robot_Info(ID, DisList.P_List, RobotColor.getMyColor(ID), ID + 1));
                }));                
            } 

            foreach (Point p in wayp)
            {
                string id = ""+GoalCounter;
                WaypointHelper wh = WaypointHelper.LookUp(id) ?? new WaypointHelper(id, p);
                wh.robotswhohavethiswaypoint.AddRange(indeces);
                foreach (int i in indeces)
                    Publish(p, i, GoalCounter);
                GoalCounter++;
                wh.goalDot = new GoalDot(window.current.DotCanvas, p, window.current.joymgr.DPI, window.current.MainCanvas, Brushes.Yellow);

                if (!DisList.Dots.Contains(wh.goalDot))
                    {
                        DisList.Dots.Add(wh.goalDot);
                    }
                else                
                    window.current.Dispatcher.BeginInvoke(new Action(()=>
                    {
                        window.current.DotCanvas.Children.Remove(wh.goalDot);
                    }));
            }
            foreach (int ID in indeces)
            {
                Publish(ID, DisList.RobotInfowned[ID].myList.Peek());
            }
        }

        private static Dictionary<string, WaypointHelper> _waypoints = new Dictionary<string, WaypointHelper>();
        public static Dictionary<string, WaypointHelper> waypoints 
        {
            get
            {
                return _waypoints;
            }
            set
            {
                _waypoints = value;
            }
            
        }
        
        public static WaypointHelper LookUp(string ID)
        {
            lock (_waypoints)
                if (_waypoints.ContainsKey(ID))
                    return _waypoints[ID];
            return null;
        }
        public static void CancelAll(int r)
        {
            lock (_waypoints)
            {
                foreach (string wh in _waypoints.Keys)
                    Cancel(r, wh);
            }
        }
        public static void Cancel(int r, string ID)
        {
            PubSubs[r].goalCanceler.publish(new Messages.actionlib_msgs.GoalID { id = new Messages.std_msgs.String(ID), stamp = ROS.GetTime() });            
        }        
        public static void Init(int numtotal, int thisrobotnum, string name)
        {
            if (PubSubs == null || numtotal > PubSubs.Length)
                PubSubs = new WaypointPubSubs[numtotal+1]; //index 0 is empty... just to keep things sane.
            PubSubs[thisrobotnum] = new WaypointPubSubs();
            PubSubs[thisrobotnum].Initialize(name);
        }
        public WaypointHelper() { }
        public WaypointHelper(string ID, Point reference) : this(ID, reference, null) { }
        public WaypointHelper(string ID, Point reference, GoalDot gd)
        {
            this.ID = ID;
            point = reference;
            realpoint = new Point(point.X * (double)ROS_ImageWPF.MapControl.MPP, point.Y * (double)ROS_ImageWPF.MapControl.MPP);
            goalDot = gd;
            lock (_waypoints)
            {
                _waypoints.Add(ID, this);                
            }
        }
    }
}
