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
using System.Diagnostics;
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
using Messages.rock_publisher;
using System.IO;
using am = Messages.sample_acquisition;
using CameraSlidersUC;

// for threading
using System.Windows.Threading;

// for controller; don't forget to include Microsoft.Xna.Framework in References
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

// for timer
using System.Timers;

#endregion

namespace WpfApplication1
{
    public partial class MainWindow : Window
    {
        CameraSlidersUC.CSUC obj = new CameraSlidersUC.CSUC();
        //checks is rocks restored
        bool rocks_restored = false;

        // controller
        GamePadState currentState;

        // nodes
        NodeHandle nh;

        // timer near end, timer ended
        bool end, near;

        // initialize timer values; 1 full hour
        int hours = 1, minutes = 0, seconds = 0;

        Publisher<m.Byte> multiplexPub;
        Publisher<gm.Twist> velPub;
        Publisher<am.ArmMovement> armPub;

        TabItem[] mainCameras, subCameras;
        public ROS_ImageWPF.CompressedImageControl[] mainImages;
        public ROS_ImageWPF.SlaveImage[] subImages;

        DispatcherTimer controllerUpdater;

        private const int back_cam = 1;
        private const int front_cam = 0;
        //Stopwatch
        Stopwatch sw = new Stopwatch();

        // 
        private DetectionHelper[] detectors;

        private bool _adr;

        private void adr(bool status)
        {
            if (_adr != status)
            {
                _adr = status;
                mainImages[back_cam].Transform(status ? -1 : 1, 1);
                subImages[back_cam].Transform(status ? -1 : 1, 1);
                subImages[front_cam].Transform(status ? 1 : -1, 1);
            }
        }
        
        // initialize stuff for MainWindow
        public MainWindow()
        {
            InitializeComponent();
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            controllerUpdater = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, 100) };
            controllerUpdater.Tick += Link;
            controllerUpdater.Start();

            new Thread(() =>
            {
                // ROS stuff
                ROS.ROS_MASTER_URI = "http://10.0.3.5:11311";
                ROS.Init(new string[0], "The_UI_" + System.Environment.MachineName.Replace("-", "__"));
                nh = new NodeHandle();
                Dispatcher.Invoke(new Action(() =>
                {
                    armGauge.startListening(nh);
                    battvolt.startListening(nh);
                    EStop.startListening(nh);
                }));

                velPub = nh.advertise<gm.Twist>("/cmd_vel", 1);
                multiplexPub = nh.advertise<m.Byte>("/cam_select", 1);
                armPub = nh.advertise<am.ArmMovement>("/arm/movement", 1);

                new Thread(() =>
                {
                    while (!ROS.shutting_down)
                    {
                        ROS.spinOnce(ROS.GlobalNodeHandle);
                        Thread.Sleep(10);
                    }
                }).Start();

                Dispatcher.Invoke(new Action(() =>
                {
                    mainCameras = new TabItem[] { MainCamera1, MainCamera2, MainCamera3, MainCamera4 };
                    subCameras = new TabItem[] { SubCamera1, SubCamera2, SubCamera3, SubCamera4 };
                    mainImages = new ROS_ImageWPF.CompressedImageControl[] { camImage0, camImage1, camImage2, camImage3 };
                    subImages = new ROS_ImageWPF.SlaveImage[] { camImageSlave0, camImageSlave1, camImageSlave2, camImageSlave3 };
                    for (int i = 0; i < mainCameras.Length; i++)
                    {
                        mainImages[i].AddSlave(subImages[i]);
                    }
                    subCameras[1].Focus();
                    adr(false);

                    // instantiating some global helpers
                    detectors = new DetectionHelper[4];
                    for (int i = 0; i < 4; ++i)
                    {
                        detectors[i] = new DetectionHelper(nh, i, this);
                    }
                }));

                // drawing boxes ????
                int x = 0, y = 0;
                while (ROS.ok)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        for (int i = 0; i < 4; ++i)
                        {
                            detectors[i].churnAndBurn();
                        }
                        DateTime dt = DateTime.Now;
                        if (!detectors[0].boxesOnScreen.ContainsKey(dt))
                            detectors[0].boxesOnScreen.Add(dt, mainImages[0].DrawABox(new System.Windows.Point((x++) % (int)Math.Round(mainImages[0].ActualWidth), (y++) % (int)Math.Round(mainImages[0].ActualHeight)), 50, 50, mainImages[0].ActualWidth, mainImages[0].ActualHeight));
                    }));
                    Thread.Sleep(10);
                }
            }).Start();
        }

        // close ros when application closes
        protected override void OnClosed(EventArgs e)
        {
            ROS.shutdown();
            base.OnClosed(e);
        }

        // timer delegate
        public void Timer(object dontcare, EventArgs alsodontcare)
        {
            // display timer
            TimerTextBlock.Text = "Elapsed: " + hours.ToString("D2") + ':' + minutes.ToString("D2") + ':' + seconds.ToString("D2");

            // change timer textblock to yellow when near = true;
            if (near == true)
                TimerTextBlock.Foreground = Brushes.Yellow;

            // change timer textblock to red when end = true
            if (end == true)
            {
                TimerTextBlock.Foreground = Brushes.Red;

                // display end of time in timer textblock
                TimerStatusTextBlock.Text = "END OF TIME";
                // display this in timer status textblock when end is true
                TimerTextBlock.Text = "Press Right Stick to restart";
                TimerStatusTextBlock.Foreground = Brushes.Red;
            }
        }

        // controller link dispatcher
        public void Link(object sender, EventArgs dontcare)
        {
            // get state of player one
            currentState = GamePad.GetState(PlayerIndex.One);

            // if controller is connected...
            if (currentState.IsConnected)
            {
                // ...say its connected in the textbloxk, color it green cuz its good to go
                LinkTextBlock.Text = "Controller: Connected";
                LinkTextBlock.Foreground = Brushes.Green;

                foreach (Buttons b in Enum.GetValues(typeof(Buttons)))
                {
                    Button(b);
                }
                double left_y = currentState.ThumbSticks.Left.Y;
                double left_x = currentState.ThumbSticks.Left.X;            
                
                if (_adr)
                {
                    left_x *= -1;
                    left_y *= -1;
                }
                gm.Twist vel = new gm.Twist { linear = new gm.Vector3 { x = left_y * _trans.Value }, angular = new gm.Vector3 { z = left_x * _rot.Value } };
                if(velPub != null)
                    velPub.publish(vel);
                
                //arm controls via joystick are done here.
                double right_y = currentState.ThumbSticks.Right.Y;

                // this is inverted to reflect mikes arm driver.  right requires a negative number, not the default positive value
                double right_x = -1 * currentState.ThumbSticks.Right.X; 
                double right_trigger = currentState.Triggers.Right;

                //if trigger is not pressed, send close signal ( -1 ).  Th goal is to have the gripper
                // going to a close state when the right trigger is not being pressed.
                if (right_trigger == 0)
                    right_trigger = -1;

                Console.WriteLine( "joy_right_x: " + right_x.ToString());
                Console.WriteLine( "joy_right_y: " + right_y.ToString());
                Console.WriteLine( "right trigger: " + right_trigger.ToString());

                am.ArmMovement armmove = new am.ArmMovement();

                armmove.velocity = true;
                armmove.position = false;

                armmove.pan_motor_velocity = right_x;
                armmove.tilt_motor_velocity = right_y;
                armmove.cable_motor_velocity = right_trigger;

                if (armPub != null)
                    armPub.publish(armmove);

            }
            // unless if controller is not connected...
            else if (!currentState.IsConnected)
            {
                // ...have program complain controller is disconnected
                LinkTextBlock.Text = "Controller: Disconnected";
                LinkTextBlock.Foreground = Brushes.Red;
            }
        }

        private byte maincameramask, secondcameramask;

        // when MainCameraControl tabs are selected
        private void MainCameraTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            obj.MainCameraSliderTabControl.SetValue(TabControl.SelectedIndexProperty, MainCameraTabControl.SelectedIndex);

            maincameramask = (byte)Math.Round(Math.Pow(2.0, MainCameraTabControl.SelectedIndex));

            //enter ADR?
            if (MainCameraTabControl.SelectedIndex == back_cam)
            {
                SubCameraTabControl.SelectedIndex = front_cam;
                adr(true);
            }
            else
                adr(false);

            JimCarry();
        }

        // When SubCameraTabControl tabs are selected
        private void SubCameraTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            secondcameramask = (byte)Math.Round(Math.Pow(2.0, SubCameraTabControl.SelectedIndex));
            JimCarry();
        }


        //What an odd name to give a function...
        private void JimCarry()
        {
            if (multiplexPub == null) return;
            m.Byte msg = new m.Byte { data = (byte)(maincameramask | secondcameramask) };
            Console.WriteLine("SENDING CAM SELECT: " + msg.data);
            multiplexPub.publish(msg);
        }

        private List<Buttons> knownToBeDown = new List<Buttons>();
        public void Button(Buttons b)
        {
            // if a is pressed
            if (currentState.IsButtonDown(b))
            {
                if (knownToBeDown.Contains(b))
                    return;
                knownToBeDown.Add(b);
                Console.WriteLine("" + b.ToString() + " pressed");

                switch (b)
                {
                    case Buttons.Start:
                        if (end == false)
                        {
                            Console.WriteLine("Start timer");
                            // display that timer is ticking
                            TimerStatusTextBlock.Text = "Ticking...";
                            // display in blue
                            TimerStatusTextBlock.Foreground = Brushes.Blue;
                        }
                        break;
                    case Buttons.Back:
                        if (end == false)
                        {
                            Console.WriteLine("Pause timer");
                            // display that timer is paused
                            TimerStatusTextBlock.Text = "Paused";
                            // display in yellow
                            TimerStatusTextBlock.Foreground = Brushes.Yellow;
                        }
                        break;
                    case Buttons.DPadDown: rockCounter.DPadButton(RockCounterUC.RockCounter.DPadDirection.Down, true); break;
                    case Buttons.DPadUp: rockCounter.DPadButton(RockCounterUC.RockCounter.DPadDirection.Up, true); break;
                    case Buttons.DPadLeft: rockCounter.DPadButton(RockCounterUC.RockCounter.DPadDirection.Left, true); break;
                    case Buttons.DPadRight: rockCounter.DPadButton(RockCounterUC.RockCounter.DPadDirection.Right, true); break;
                    case Buttons.RightStick: RightStickButton(); break;
                }
            }
            else
                knownToBeDown.Remove(b);
        }

        // right stick function; reset timer
        public void RightStickButton()
        {
            // if right stick is clicked/pressed
            if (currentState.Buttons.RightStick == ButtonState.Pressed)
            {
                // if timer is at the end
                if (end == true)
                {
                    Console.WriteLine("Right stick pressed");
                    // reset hours to one
                    hours = 1;
                    // reset minutes to 0
                    minutes = 0;
                    // reset seconds to 0
                    seconds = 0;
                    // reset near to false
                    near = false;
                    // reset end to false
                    end = false;
                    // display timer info in green 
                    TimerStatusTextBlock.Foreground = Brushes.Green;
                    // display timer info as ready
                    TimerStatusTextBlock.Text = "Ready";
                    // display timer in black
                    TimerTextBlock.Foreground = Brushes.Black;
                }
            }
        }

        //
        // User recalibration box-drawing starts here
        //
        System.Windows.Point mouseDownPoint;
        System.Windows.Point mousePos;
        System.Windows.Shapes.Rectangle mouseBox;
        bool leftButtonDown = false;
        bool leftButtonDownInBounds = false;

        private void PublishRecalibration(object sender)
        {
            // what do
            Publisher<Messages.rock_publisher.recalibrateMsg> recalPub = nh.advertise<Messages.rock_publisher.recalibrateMsg>("/recalibration", 1);
            recalibrateMsg theMsg = new recalibrateMsg();
            theMsg.data.cameraID = whichIsIt(sender);
            theMsg.data.width = (int) mouseBox.Width;
            theMsg.data.height = (int) mouseBox.Height;
            theMsg.data.x = (int) mouseBox.GetValue(Canvas.LeftProperty);
            theMsg.data.y = (int) mouseBox.GetValue(Canvas.TopProperty);
            // theMsg.data.color = what;
            theMsg.img = (sender as ROS_ImageWPF.CompressedImageControl).latestFrame;
        }

        private int whichIsIt(object sender)
        {
            ROS_ImageWPF.CompressedImageControl c = (sender as ROS_ImageWPF.CompressedImageControl);
            if (c == null) return -1;
            for (int i = 0; i < mainImages.Length; i++)
                if (mainImages[i] == c)
                    return i;
            return -1;
        }

        private void DrawUserDrawnBox(int whichImage, System.Windows.Point mousePosition)
        {
                mouseBox = new System.Windows.Shapes.Rectangle()
                {
                    Width = Math.Abs(mousePosition.X - mouseDownPoint.X),
                    Height = Math.Abs(mousePosition.Y - mouseDownPoint.Y),
                    Stroke = Brushes.DarkBlue,
                    StrokeThickness = 3,
                    Opacity = 0.5
                };
                mouseBox.SetValue(Canvas.LeftProperty, (mousePosition.X < mouseDownPoint.X) ? mousePosition.X : mouseDownPoint.X);
                mouseBox.SetValue(Canvas.TopProperty, (mousePosition.Y < mouseDownPoint.Y) ? mousePosition.Y : mouseDownPoint.Y);

                // keep refeshing the image of the box
                switch (whichImage)
                {
                    case 0:
                        camRect0.Children.Clear();
                        camRect0.Children.Add(mouseBox);
                        break;
                    case 1:
                        camRect1.Children.Clear();
                        camRect1.Children.Add(mouseBox);
                        break;
                    case 2:
                        camRect2.Children.Clear();
                        camRect2.Children.Add(mouseBox);
                        break;
                    case 3:
                        camRect3.Children.Clear();
                        camRect3.Children.Add(mouseBox);
                        break;
                }
        }

        private void UserControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            int cam = whichIsIt(sender);
            if (cam >= 0)
            {
                leftButtonDown = true;
                System.Windows.Point mouse_pos = e.GetPosition(sender as ROS_ImageWPF.CompressedImageControl);
                if (mouse_pos.X > 0 && mouse_pos.X < 874 && mouse_pos.Y > 0 && mouse_pos.Y < 518)
                {
                    leftButtonDownInBounds = true;
                    mouseDownPoint = e.GetPosition(sender as ROS_ImageWPF.CompressedImageControl);
                    mainImages[cam].CaptureMouse();
                }
                else leftButtonDownInBounds = false;
            }
        }

        private System.Windows.Point ForceMousePositionToBeInBounds(System.Windows.Point mouse_pos)
        {
            if (mouse_pos.X < 0) mouse_pos.X = 0;
            if (mouse_pos.X > 874) mouse_pos.X = 864; // so the box won't clip with the border
            if (mouse_pos.Y < 0) mouse_pos.Y = 0;
            if (mouse_pos.Y > 518) mouse_pos.Y = 488; // same as above
            return mouse_pos;
        }

        private void UserControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            leftButtonDown = false;
            if (leftButtonDownInBounds)
            {
                leftButtonDownInBounds = false;
                mousePos = ForceMousePositionToBeInBounds(e.GetPosition(sender as ROS_ImageWPF.CompressedImageControl));
                DrawUserDrawnBox(whichIsIt(sender), mousePos);
                // create/send the message
                // PublishRecalibration(sender);
            }
            mainImages[whichIsIt(sender)].ReleaseMouseCapture();
        }

        private void UserControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (leftButtonDown && leftButtonDownInBounds)
            {
                mousePos = ForceMousePositionToBeInBounds(e.GetPosition(sender as ROS_ImageWPF.CompressedImageControl));
                DrawUserDrawnBox(whichIsIt(sender), mousePos);
            }
        }

        private void UserControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            switch (whichIsIt(sender))
            {
                case 0:
                    camRect0.Children.Clear();
                    break;
                case 1:
                    camRect1.Children.Clear();
                    break;
                case 2:
                    camRect2.Children.Clear();
                    break;
                case 3:
                    camRect3.Children.Clear();
                    break;
            }
        }


        
    }

    // we get one of these helpers for each camera
    // what excellent service
    public class DetectionHelper {
        public SortedList<DateTime, System.Windows.Shapes.Rectangle> boxesOnScreen = new SortedList<DateTime,System.Windows.Shapes.Rectangle>(); // we need to read detections and add them to this in the callback
        int cameraNumber; // 0, 1, 2, or 3
        Subscriber<imgDataArray> sub;
        ROS_ImageWPF.CompressedImageControl primary;
        ROS_ImageWPF.SlaveImage secondary;

        public DetectionHelper(NodeHandle node, int cameraNumber, MainWindow w) {
            sub = node.subscribe<imgDataArray>("/camera" + cameraNumber + "/detects", 1000, detectCallback);
            this.cameraNumber = cameraNumber;
            primary = w.mainImages[cameraNumber];
            secondary = w.subImages[cameraNumber];
        }

        public void churnAndBurn() {
            while (boxesOnScreen.Count > 0 && DateTime.Now.Subtract(boxesOnScreen.Keys[0]).TotalMilliseconds > 1000)
            {
                if (!primary.EraseABox(boxesOnScreen[boxesOnScreen.Keys[0]]))
                    secondary.EraseABox(boxesOnScreen[boxesOnScreen.Keys[0]]);
                boxesOnScreen.RemoveAt(0);
            }
        }

        // ugh this function is blocking right now
        void detectCallback(imgDataArray detections)
        {
            foreach (imgData box in detections.rockData)
            {
                System.Windows.Point tl = new System.Windows.Point(box.x, box.y);
                DateTime dt = DateTime.Now;
                // if it's on this helper's camera, add it to the list of boxes on screen, and draw the box on the correct window
                if (box.cameraID == cameraNumber) // && cameraNumber is selected as Primary
                {
			 primary.Dispatcher.BeginInvoke(new Action(() =>
                         {
                              if (!boxesOnScreen.ContainsKey(dt))	                      
                                  boxesOnScreen.Add(dt, primary.DrawABox(tl, box.width, box.height, 864, 480));
        	         }));

                }
                else if (box.cameraID == cameraNumber) // && cameraNumber is selected as Secondary
                {
                         secondary.Dispatcher.BeginInvoke(new Action(() =>
                         {
                              if (!boxesOnScreen.ContainsKey(dt))	                      
                                  boxesOnScreen.Add(dt, secondary.DrawABox(tl, box.width, box.height, 864, 480));
        	         }));


                }
            }
        }
    }
}