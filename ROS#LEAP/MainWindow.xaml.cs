#region Imports

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System;
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
using System.Text;
using Leap;
using Vector = Leap.Vector;

#endregion


namespace CompressedImageView
{
    public interface ILeapAPIIsBadLol
    {
        void OnInit();
        void OnConnect();
        void OnExit ();
        void OnDisconnect();
        void OnFrame(Leap.Frame f);
    }
    public class LeapAPIIsBadLol : Listener
    {
        private ILeapAPIIsBadLol SOBAD;
        private Controller _controller;
        public Controller Init(ILeapAPIIsBadLol lol)
        {
            SOBAD = lol;
            _controller = new Controller();
            _controller.AddListener(this);
            return _controller;
        }
        public override void OnInit(Controller controller) { SOBAD.OnInit(); }
        public override void OnConnect(Controller controller) { SOBAD.OnConnect(); }
        public override void OnDisconnect(Controller controller) { SOBAD.OnDisconnect(); }
        public override void OnExit(Controller controller) { SOBAD.OnExit(); }
        public override void OnFrame(Controller controller) { SOBAD.OnFrame(controller.Frame()); }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, ILeapAPIIsBadLol
    {
        private Leap.Controller controller;
        private LeapAPIIsBadLol leapapi;

#region ex-LEAPsample
	    public void OnInit ()
	    {
		    Console.WriteLine("Initialized");
	    }

        public void OnConnect()
        {
            Console.WriteLine("Connected");
            controller.EnableGesture(Gesture.GestureType.TYPECIRCLE);
        }

	    public void OnDisconnect ()
	    {
            //Note: not dispatched when running in a debugger.
		    Console.WriteLine("Disconnected");
	    }

	    public void OnExit ()
	    {
		    Console.WriteLine("Exited");
	    }

        private bool enabled;
        private bool firsties;
        private double startpx=0, startpy=0, startpz=0;
        private double startrr=0, startry=0, startrp=0;
	    public void OnFrame (Leap.Frame frame)
	    {
            StringBuilder sb = new System.Text.StringBuilder();

		    if (enabled && !frame.Hands.IsEmpty) {
			    // Get the first hand
			    Hand hand = frame.Hands [0];

			    // Check if the hand has any fingers
			    FingerList fingers = hand.Fingers;
			    if (!fingers.IsEmpty) {
				    // Calculate the hand's average finger tip position
				    Vector avgPos = Vector.Zero;
				    foreach (Finger finger in fingers) {
					    avgPos += finger.TipPosition;
				    }
				    avgPos /= fingers.Count;
				    sb.AppendFormat("Hand has {0} fingers, average finger tip position: {1}\n",fingers.Count,avgPos);
			    }

			    // Get the hand's sphere radius and palm position
			    sb.AppendFormat("Hand sphere radius: {0} mm, palm position: {1}\n", hand.SphereRadius.ToString("n2"), hand.PalmPosition);

			    // Get the hand's normal vector and direction
			    Vector normal = hand.PalmNormal;
			    Vector direction = hand.Direction;

                if (!firsties)
                {
                    firsties = true;
                    startrr = normal.Roll;
                    startrp = direction.Pitch;
                    startry = direction.Yaw;
                    startpx = hand.StabilizedPalmPosition.x;
                    startpy = hand.StabilizedPalmPosition.y;
                    startpz = hand.StabilizedPalmPosition.z;
                }
                else
                {
                    holyCrap(startpx - hand.StabilizedPalmPosition.x, startpy - hand.StabilizedPalmPosition.y, startpz - hand.StabilizedPalmPosition.z,
                        startrr - normal.Roll, startrp - direction.Pitch, startry - direction.Yaw);
                }

			    // Calculate the hand's pitch, roll, and yaw angles
                sb.AppendFormat("Hand pitch: (RPY) = ( {0}, {1}, {2} ) degrees\n",
                    normal.Roll * 180.0f / (float)Math.PI,
                    direction.Pitch * 180.0f / (float)Math.PI,
                    direction.Yaw * 180.0f / (float)Math.PI);
		    }

		    // Get gestures
		    GestureList gestures = frame.Gestures ();
		    for (int i = 0; i < gestures.Count; i++) {
			    Gesture gesture = gestures [i];

			    switch (gesture.Type) {
			    case Gesture.GestureType.TYPECIRCLE:
				    CircleGesture circle = new CircleGesture (gesture);

                        // Calculate clock direction using the angle between circle normal and pointable
				    string clockwiseness;
				    if (circle.Pointable.Direction.AngleTo (circle.Normal) <= Math.PI / 4) {
					    //Clockwise if angle is less than 90 degrees
					    clockwiseness = "clockwise";
                        enabled = true;
                        firsties = false;
				    } else {
					    clockwiseness = "counterclockwise";
                        enabled = false;
				    }

				    float sweptAngle = 0;

                        // Calculate angle swept since last frame
				    if (circle.State != Gesture.GestureState.STATESTART) {
					    CircleGesture previousUpdate = new CircleGesture (controller.Frame (1).Gesture (circle.Id));
					    sweptAngle = (circle.Progress - previousUpdate.Progress) * 360;
				    }

				    sb.AppendFormat("Circle id: {0}, {1}, progress: {2}, radius: {3}, angle: {4}, {5}\n", 
                        circle.Id,
                        circle.State,
                        circle.Progress,
                        circle.Radius,
                        sweptAngle,
                        clockwiseness);
				    break;
			    case Gesture.GestureType.TYPESWIPE:
				    SwipeGesture swipe = new SwipeGesture (gesture);
				    sb.AppendFormat("Swipe id: {0}, {1}, position: {2}, direction: {3}, speed: {4}\n", 
                        swipe.Id,
                        swipe.State,
                        swipe.Position,
                        swipe.Direction,
                        swipe.Speed);
				    break;
			    case Gesture.GestureType.TYPEKEYTAP:
				    KeyTapGesture keytap = new KeyTapGesture (gesture);
				    sb.AppendFormat("Tap id: {0}, {1}, position: {2}, direction: {3}\n",
                        keytap.Id,
                        keytap.State,
                        keytap.Position,
                        keytap.Direction);
				    break;
			    case Gesture.GestureType.TYPESCREENTAP:
				    ScreenTapGesture screentap = new ScreenTapGesture (gesture);
                    sb.AppendFormat("Tap id: {0}, {1}, position: {2}, direction: {3}\n",
                        screentap.Id,
                        screentap.State,
                        screentap.Position,
                        screentap.Direction);
				    break;
			    default:
				    sb.AppendFormat("Unknown gesture type.\n");
				    break;
			    }
		    }
            if (enabled)
            {
                if (!frame.Hands.IsEmpty || !frame.Gestures().IsEmpty)
                {
                    sb.Append("\n");
                }
                Console.Write(sb);
            }
	    }

        private Subscriber<Messages.baxter_core_msgs.EndpoingState> initialsub;
        private Publisher<gm.PoseStamped> pub;

        private gm.Pose initial = null;

        private void holyCrap(double x, double y, double z, double r, double p, double yaw)
        {
            if (initial != null)
            {
                gm.PoseStamped ps = new gm.PoseStamped() { pose = new gm.Pose() { position = new gm.Point() { x = initial.position.x + x / 100, y = initial.position.y + y / 100, z = initial.position.z + z / 100 }, orientation = new gm.Quaternion() { w = initial.orientation.w, x = initial.orientation.x, y = initial.orientation.y, z = initial.orientation.z } } };
                pub.publish(ps);
                Console.WriteLine(ps.pose.position.x + "," + ps.pose.position.y + "," + ps.pose.position.z);
            }
        }

        private void posecb(Messages.baxter_core_msgs.EndpoingState es)
        {
            if (initial == null)
            {
                initial = es.pose;
            }
        }
#endregion

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ROS.ROS_MASTER_URI = "http://baxter:11311";
            ROS.ROS_HOSTNAME = "10.0.6.9";
            ROS.Init(new string[0], "ROSLEAP");
            leapapi = new LeapAPIIsBadLol();
            controller = leapapi.Init(this);
            NodeHandle nh = new NodeHandle();
            pub = nh.advertise<gm.PoseStamped>("/right_pose", 1);
            initialsub = nh.subscribe<Messages.baxter_core_msgs.EndpoingState>("/robot/limb/right/endpoint_state", 1, posecb);
        }

        protected override void OnClosed(EventArgs e)
        {
            ROS.shutdown();
            base.OnClosed(e);
        }
    }
}
