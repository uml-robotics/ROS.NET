using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Ros_CSharp;
using System.Threading;

namespace DREAMPioneer
{
    public class WaypointHelper
    {
        private static uint GoalCounter;
        public static WaypointPubSubs[] PubSubs;
        public string ID;
        public GoalDot goalDot;
        public Point point;
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
                            Thread.Sleep(1);
                        }
                    }).Start();
                }
                goalPub = node.advertise<Messages.move_base_msgs.MoveBaseActionGoal>(name + "/move_base/goal", 1);
                goalCanceler = node.advertise<Messages.actionlib_msgs.GoalID>(name + "/move_base/cancel", 1);
            }
            public void SubSetup(string name, CallbackDelegate<Messages.actionlib_msgs.GoalStatusArray> handler)
            {
                goalsub = node.subscribe<Messages.actionlib_msgs.GoalStatusArray>(name + "/move_base/status", 1, handler);
            }
        }
        public static void Publish(Point p, int r, uint c)
        {
            PubSubs[r].goalPub.publish(
                new Messages.move_base_msgs.MoveBaseActionGoal()
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
        public static void Publish(List<Point> wayp, params int[] indeces)
        {
            foreach (Point p in wayp)
            {
                string id = ""+GoalCounter;
                WaypointHelper wh = WaypointHelper.LookUp(id) ?? new WaypointHelper(id, p);
                wh.robotswhohavethiswaypoint.AddRange(indeces);
                foreach (int ID in indeces)
                {
                    Publish(p, ID, GoalCounter);
                }                
                GoalCounter++;
            } 
        }
        private static Dictionary<string, WaypointHelper> _waypoints = new Dictionary<string, WaypointHelper>();
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
        public WaypointHelper(string ID, Point reference) : this(ID, reference, null) { }
        public WaypointHelper(string ID, Point reference, GoalDot gd)
        {
            this.ID = ID;
            point = reference;
            goalDot = gd;
            lock (_waypoints)
            {
                _waypoints.Add(ID, this);                
            }
        }
    }
}
