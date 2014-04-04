#region Imports
#define INSTANT_DETECTION_DEATH

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
        TiltSliderUC.TSUC tilt = new TiltSliderUC.TSUC();

        int[] tilt_prev = new int[4] {0,0,0,0};
        Publisher<m.Bool> mast_pub;
        Publisher<m.Int32>[] tilt_pub;

        Publisher<m.Bool> ArmON;

        // controller
        GamePadState currentState;
        // nodes
        NodeHandle nh;

        Publisher<m.Byte> multiplexPub;
        Publisher<gm.Twist> velPub;
        Publisher<am.ArmMovement> armPub;
        Publisher<Messages.rock_publisher.recalibrateMsg> recalPub0;
        Publisher<Messages.rock_publisher.recalibrateMsg> recalPub1;
        Publisher<Messages.rock_publisher.recalibrateMsg> recalPub2;
        Publisher<Messages.rock_publisher.recalibrateMsg> recalPub3;

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
            controllerUpdater = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, 10) };
            controllerUpdater.Tick += Link;
            controllerUpdater.Start();

            new Thread(() =>
            {
                // ROS stuff
                ROS.ROS_MASTER_URI = "http://10.10.10.206:11311";
                ROS.ROS_HOSTNAME = "10.10.10.219";
                ROS.Init(new string[0], "The_UI_" + System.Environment.MachineName.Replace("-", "__"));
                nh = new NodeHandle();
                Dispatcher.Invoke(new Action(() =>
                {
                    battvolt.startListening(nh);
                    EStop.startListening(nh);
                    MotorGraph.startListening(nh);
                    
                }));
                velPub = nh.advertise<gm.Twist>("/cmd_vel", 1);
                multiplexPub = nh.advertise<m.Byte>("/cam_select", 1);
                armPub = nh.advertise<am.ArmMovement>("/arm/movement", 1);
                ArmON = nh.advertise<m.Bool>("arm/on", 1);
                mast_pub = nh.advertise<m.Bool>("raise_camera_mast", 1);

                tilt_pub = new Publisher<m.Int32>[4];

                recalPub0 = nh.advertise<Messages.rock_publisher.recalibrateMsg>("/camera0/recalibrate", 4);
                recalPub1 = nh.advertise<Messages.rock_publisher.recalibrateMsg>("/camera1/recalibrate", 4);
                recalPub2 = nh.advertise<Messages.rock_publisher.recalibrateMsg>("/camera2/recalibrate", 4);
                recalPub3 = nh.advertise<Messages.rock_publisher.recalibrateMsg>("/camera3/recalibrate", 4);

                for (int i = 0; i < 4; i++)
                {
                    tilt_pub[i] = nh.advertise<m.Int32>("camera" + i + "/tilt", 1);
                }

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

#if !INSTANT_DETECTION_DEATH
                while (ROS.ok)
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        for (int i = 0; i < 4; ++i)
                        {
                            detectors[i].churnAndBurn();
                        }
                    }));
                    Thread.Sleep(100);
                }
#endif
            }).Start();
        }

        // close ros when application closes
        protected override void OnClosed(EventArgs e)
        {
            ROS.shutdown();
            base.OnClosed(e);
        }

        bool engaged = false;

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
                double left_x = -currentState.ThumbSticks.Left.X;            
                
                if (_adr)
                {
                    //left_x *= -1;
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

                if (!engaged && (Math.Abs(right_y) > .1 || Math.Abs(right_x) > .1 ))
                {
                    engaged = true;
                    ArmON.publish(new m.Bool() { data = true });
                    Arm_Engaged.Content = "Arm Engaged";
                    Arm_Engaged.Background = Brushes.White;
                    Arm_Engaged.Foreground = Brushes.Green;
                }

                //if trigger is not pressed, send close signal ( -1 ).  Th goal is to have the gripper
                // going to a close state when the right trigger is not being pressed.
                if (right_trigger == 0)
                    right_trigger = -1;

                /*Console.WriteLine( "joy_right_x: " + right_x.ToString());
                Console.WriteLine( "joy_right_y: " + right_y.ToString());
                Console.WriteLine( "right trigger: " + right_trigger.ToString());*/

                am.ArmMovement armmove = new am.ArmMovement();

                armmove.pan_motor_velocity = right_x;
                armmove.tilt_motor_velocity = right_y;
                armmove.gripper_open = (right_trigger >= 0.5);

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
            maincameramask = (byte)Math.Round(Math.Pow(2.0, MainCameraTabControl.SelectedIndex));
            //Console.WriteLine("************************Camera Selected: " + MainCameraTabControl.SelectedIndex.ToString() + "********************************");

            Tilt_Slider.Value = tilt_prev[MainCameraTabControl.SelectedIndex];

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
                //Console.WriteLine("" + b.ToString() + " pressed");

                switch (b)
                {
                    case Buttons.Start: break;
                    case Buttons.Back:  break;
                    case Buttons.DPadDown: _trans.DPadButton( UberSlider.VerticalUberSlider.DPadDirection.Down, true);  break; //rockCounter.DPadButton(RockCounterUC.RockCounter.DPadDirection.Down, true);

                    case Buttons.DPadUp: _trans.DPadButton(UberSlider.VerticalUberSlider.DPadDirection.Up, true); break; //rockCounter.DPadButton(RockCounterUC.RockCounter.DPadDirection.Up, true); break;

                    case Buttons.DPadLeft: _rot.DPadButton( UberSlider.UberSlider.DPadDirection.Left, true); break;  // rockCounter.DPadButton(RockCounterUC.RockCounter.DPadDirection.Left, true); break;
                    case Buttons.DPadRight: _rot.DPadButton( UberSlider.UberSlider.DPadDirection.Right, true); break;//rockCounter.DPadButton(RockCounterUC.RockCounter.DPadDirection.Right, true); break;
                    case Buttons.RightStick: RightStickButton(); break;
                    case Buttons.RightShoulder: tilt_change(1); break;
                    case Buttons.LeftShoulder: tilt_change(0); break;
                }
            }
            else
                knownToBeDown.Remove(b);
        }

        public void tilt_change(int i)
        {
            if (i == 1 && Tilt_Slider.Value < 36000) Tilt_Slider.Value += 3600;
            if (i == 0 && Tilt_Slider.Value > -36000) Tilt_Slider.Value += -3600;
        }

        // right stick function; reset timer
        public void RightStickButton()
        {
            // if right stick is clicked/pressed
            if (currentState.Buttons.RightStick == ButtonState.Pressed && engaged)
            {
                engaged = false;
                ArmON.publish(new m.Bool { data = false });
                Arm_Engaged.Content = "Arm NOT Engaged";
                Arm_Engaged.Background = Brushes.Black;
                Arm_Engaged.Foreground = Brushes.Red;
            }
        }

        private void Tilt_Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int tilt = (int)Tilt_Slider.Value;
            tilt_prev[MainCameraTabControl.SelectedIndex] = tilt;
            Tilt_Lvl.Content = tilt.ToString();
            if (tilt_pub != null) tilt_pub[MainCameraTabControl.SelectedIndex].publish(new Int32 { data = tilt });
        }

        private void Raise_Mast_Click(object sender, RoutedEventArgs e)
        {
            mast_pub.publish(new m.Bool() { data = true });
            //Raise_mast.Visibility = Visibility.Hidden;
            Raise_mast.Width = 164;
            Raise_mast.Height = 50;
            Canvas.SetLeft(Raise_mast, 1217);
            Canvas.SetTop(Raise_mast, 41);
            Raise_mast.FontSize = 16;
        }

        //
        // User recalibration starts here
        //
        System.Windows.Point mouseDownPoint;
        System.Windows.Point mousePos;
        System.Windows.Shapes.Rectangle mouseBox;
        bool leftButtonDown = false;
        bool leftButtonDownInBounds = false;
        int boxColor = 0;
        Brush brushColor = Brushes.Blue;

        private int whichIsIt(object sender)
        {
            ROS_ImageWPF.CompressedImageControl c = (sender as ROS_ImageWPF.CompressedImageControl);
            if (c == null) return -1;
            for (int i = 0; i < mainImages.Length; i++)
                if (mainImages[i] == c)
                    return i;
            return -1;
        }

        private void PublishRecalibration(int tab_index)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                recalibrateMsg theMsg = new recalibrateMsg();
                theMsg.data = new imgData();
                theMsg.img = new sm.CompressedImage();
                theMsg.data.cameraID = tab_index;
                theMsg.data.width = (int)mouseBox.Width;
                theMsg.data.height = (int)mouseBox.Height;
                theMsg.data.x = (int)(double)mouseBox.GetValue(Canvas.LeftProperty); // ew gross
                theMsg.data.y = (int)(double)mouseBox.GetValue(Canvas.TopProperty);
                theMsg.data.color = new m.ColorRGBA();
                theMsg.data.color.r = boxColor;
                theMsg.data.color.g = boxColor;
                theMsg.data.color.b = boxColor;
                theMsg.data.color.a = boxColor;
                //theMsg.img = (sender as ROS_ImageWPF.CompressedImageControl).latestFrame;
                switch (tab_index)
                {
                    case 0:
                        theMsg.img = camImage0.latestFrame;
                        recalPub0.publish(theMsg);
                        break;
                    case 1:
                        theMsg.img = camImage1.latestFrame;
                        recalPub1.publish(theMsg);
                        break;
                    case 2:
                        theMsg.img = camImage2.latestFrame;
                        recalPub2.publish(theMsg);
                        break;
                    case 3:
                        theMsg.img = camImage3.latestFrame;
                        recalPub3.publish(theMsg);
                        break;
                }
                //ROS.spinOnce(nh);
            }));
        }
                
        #region drawing boxes with mouse
        /*private void DrawUserDrawnBox(int whichImage, System.Windows.Point mousePosition)
        {
                mouseBox = new System.Windows.Shapes.Rectangle()
                {
                    Width = Math.Abs(mousePosition.X - mouseDownPoint.X),
                    Height = Math.Abs(mousePosition.Y - mouseDownPoint.Y),
                    Stroke = brushColor,
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
        }*/

        private void UserControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            /*int cam = whichIsIt(sender);
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
            }*/
        }

        /*private System.Windows.Point ForceMousePositionToBeInBounds(System.Windows.Point mouse_pos)
        {
            if (mouse_pos.X < 0) mouse_pos.X = 0;
            if (mouse_pos.X > 874) mouse_pos.X = 864; // so the box won't clip with the border
            if (mouse_pos.Y < 0) mouse_pos.Y = 0;
            if (mouse_pos.Y > 518) mouse_pos.Y = 488; // same as above
            return mouse_pos;
        }*/

        private void UserControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
           /* leftButtonDown = false;
            if (leftButtonDownInBounds)
            {
                leftButtonDownInBounds = false;
                mousePos = ForceMousePositionToBeInBounds(e.GetPosition(sender as ROS_ImageWPF.CompressedImageControl));
                DrawUserDrawnBox(whichIsIt(sender), mousePos);
                // create/send the message
                //PublishRecalibration(sender);
            }
            mainImages[whichIsIt(sender)].ReleaseMouseCapture();*/
        }

        private void UserControl_MouseMove(object sender, MouseEventArgs e)
        {
            /*if (leftButtonDown && leftButtonDownInBounds)
            {
                mousePos = ForceMousePositionToBeInBounds(e.GetPosition(sender as ROS_ImageWPF.CompressedImageControl));
                DrawUserDrawnBox(whichIsIt(sender), mousePos);
            }*/
        }
        #endregion

        private void progressBar1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

        #region radio buttons
        /*private void RadioButton_Checked_B(object sender, RoutedEventArgs e)
        {
            boxColor = 0;
            brushColor = Brushes.Blue;
        }

        private void RadioButton_Checked_G(object sender, RoutedEventArgs e)
        {
            boxColor = 1;
            brushColor = Brushes.Green;
        }

        private void RadioButton_Checked_R(object sender, RoutedEventArgs e)
        {
            boxColor = 2;
            brushColor = Brushes.Red;
        }

        private void RadioButton_Checked_O(object sender, RoutedEventArgs e)
        {
            boxColor = 3;
            brushColor = Brushes.Orange;
        }

        private void RadioButton_Checked_P(object sender, RoutedEventArgs e)
        {
            boxColor = 4;
            brushColor = Brushes.Purple;
        }

        private void RadioButton_Checked_Y(object sender, RoutedEventArgs e)
        {
            boxColor = 5;
            brushColor = Brushes.Yellow;
        }

        #endregion

        #region send & clear
        private void Button_Click_Send(object sender, RoutedEventArgs e)
        {
            // hopefully not broken
            PublishRecalibration(MainCameraTabControl.SelectedIndex);
            camRect0.Children.Clear();
            camRect1.Children.Clear();
            camRect2.Children.Clear();
            camRect3.Children.Clear();
        }

        private void Button_Click_Clear(object sender, RoutedEventArgs e)
        {
            camRect0.Children.Clear();
            camRect1.Children.Clear();
            camRect2.Children.Clear();
            camRect3.Children.Clear();
        }

        #endregion

        #region restore default calibrations

        recalibrateMsg MakeBogusRestoreMessage()
        {
            recalibrateMsg theMsg = new recalibrateMsg();
            theMsg.data = new imgData();
            theMsg.img = new sm.CompressedImage();
            theMsg.data.cameraID = -1;
            theMsg.data.width = -1;
            theMsg.data.height = -1;
            theMsg.data.x = -1;
            theMsg.data.y = -1;
            theMsg.data.color = new m.ColorRGBA();
            theMsg.data.color.r = -1;
            theMsg.data.color.g = -1;
            theMsg.data.color.b = -1;
            theMsg.data.color.a = -1;
            return theMsg;
        }

        private void Button_Click_Restore0(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                recalPub0.publish(MakeBogusRestoreMessage());
            }));
        }

        private void Button_Click_Restore1(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                recalPub1.publish(MakeBogusRestoreMessage());
            }));
        }

        private void Button_Click_Restore2(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                recalPub2.publish(MakeBogusRestoreMessage());
            }));
        }

        private void Button_Click_Restore3(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                recalPub3.publish(MakeBogusRestoreMessage());
            }));
        }*/
        #endregion
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
#if INSTANT_DETECTION_DEATH
			primary.Dispatcher.Invoke(new Action(() => {
                foreach(System.Windows.Shapes.Rectangle r in boxesOnScreen.Values)
                {    
                        if (!primary.EraseABox(r))
                            secondary.EraseABox(r);
                }
                boxesOnScreen.Clear();
        	}));
#endif
            foreach (imgData box in detections.rockData)
            {

                System.Windows.Point tl = new System.Windows.Point(box.x, box.y);
                DateTime dt = DateTime.Now;
                // if it's on this helper's camera, add it to the list of boxes on screen, and draw the box on the correct window
               /* if (box.cameraID == cameraNumber) // && cameraNumber is selected as Primary
                {*/
			        primary.Dispatcher.Invoke(new Action(() =>
                    {
                              if (!boxesOnScreen.ContainsKey(dt))	                      
                                  boxesOnScreen.Add(dt, primary.DrawABox(tl, box.width, box.height, 864, 480, box.color));
        	        }));
                    secondary.Dispatcher.Invoke(new Action(() =>
                    {
                        if (!boxesOnScreen.ContainsKey(dt))
                            boxesOnScreen.Add(dt, secondary.DrawABox(tl, box.width, box.height, 864, 480, box.color));
                    }));
                /*}
                else
                {
                    Console.WriteLine("OFFSCREEN DETECTION ON CAMERA " + cameraNumber);
                }*/
            }
        }
    }
}