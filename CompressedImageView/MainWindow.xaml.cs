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
using Messages.arm_status_msgs;
using Messages.rock_publisher;

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

        // loop delegate function
        public delegate void LoopDelegate();

        // timer variable
        private static System.Timers.Timer aTimer;

        // controller
        GamePadState currentState;

        // nodes
        NodeHandle nh;

        // timer near end, timer ended
        bool end, near;

        // left vibration motor value, right vibration motor value
        float leftMotor, rightMotor;

        // initialize timer values; 1 full hour
        int hours = 1, minutes = 0, seconds = 0;

        // initialize rock count values to 0
        int red = 0, orange = 0, yellow = 0, green = 0, blue = 0, purple = 0;

        // mutex for ring, mutex for switching main camera and sub camera
        bool ringIsFree = true, swapCam = true;

        // ring buffer for rocks, main camera index, sub camera index
        int rockRing = 0, incrementValue, mainCam, subCam;

        // initialize stuff for MainWindow
        public MainWindow()
        {
            InitializeComponent();

            // timer ticks 10000 times
            aTimer = new System.Timers.Timer(10000);
            // timer runs UpdateTimer function when ticked
            aTimer.Elapsed += new ElapsedEventHandler(UpdateTimer);
            // timer ticks every 1000ms = 1s
            aTimer.Interval = 1000;
        }

        // when timer is ticked, do this stuff
        private void UpdateTimer(object source, ElapsedEventArgs e)
        {
            // if 0 minutes, take 60 from hours
            if (minutes == 0)
            {
                hours--;
                minutes = 60;
            }

            // if 0 seconds, pull 60 from minutes
            if (seconds == 0)
            {
                minutes--;
                seconds = 60;
            }

            // decrements seconds for counting down
            seconds--;

            // if at 00:59:50, timer is near end
            if ((minutes == 59) && (seconds == 55))
            {
                near = true;
            }

            // if at 00:50:00, timer is at end
            // disable timer
            if ((minutes == 59) && (seconds == 50))
            {
                aTimer.Enabled = false;
                end = true;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // dispatcher for timer
            Window1.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new LoopDelegate(Timer));

            // dispatcher for controller link
            Window1.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new LoopDelegate(Link));

            // ROS stuff
            ROS.ROS_MASTER_URI = "http://10.0.3.88:11311";
            ROS.Init(new string[0], "Image_Test");
            nh = new NodeHandle();
            armGauge.startListening(nh);
            battvolt.startListening(nh);

            new Thread(() =>
            {
                while (!ROS.shutting_down)
                {
                    ROS.spinOnce(ROS.GlobalNodeHandle);
                    Thread.Sleep(10);
                }
            }).Start();

            SubCamera2.Focus();
        }

        // close ros when application closes
        protected override void OnClosed(EventArgs e)
        {
            ROS.shutdown();
            base.OnClosed(e);
        }

        // timer delegate
        public void Timer()
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

            // run dispatcher again, forever
            Window1.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new LoopDelegate(Timer));
        }

        // controller link dispatcher
        public void Link()
        {
            // get state of player one
            currentState = GamePad.GetState(PlayerIndex.One);

            // if controller is connected...
            if (currentState.IsConnected)
            {
                // ...say its connected in the textbloxk, color it green cuz its good to go
                LinkTextBlock.Text = "Controller: Connected";
                LinkTextBlock.Foreground = Brushes.Green;

                // press left and right shoulders, and back and start to close application by controller
                if ((currentState.Buttons.Back == ButtonState.Pressed) &&
                    (currentState.Buttons.Start == ButtonState.Pressed) &&
                    (currentState.Buttons.LeftShoulder == ButtonState.Pressed) &&
                    (currentState.Buttons.RightShoulder == ButtonState.Pressed)
                    )
                {
                    Close();
                }

                // dispatcher for trigger buttons
                Window1.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new LoopDelegate(LeftTriggerButton));
                Window1.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new LoopDelegate(RightTriggerButton));

                // dispatcher for shoulder buttons
                Window1.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new LoopDelegate(LeftShoulderButton));
                Window1.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new LoopDelegate(RightShoulderButton));

                // dispatcher for back and start
                Window1.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new LoopDelegate(BackButton));
                Window1.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new LoopDelegate(StartButton));

                // dispatcher for left stick
                Window1.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new LoopDelegate(LeftStickButton));

                // dispatcher for y, b, a, x buttons
                Window1.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new LoopDelegate(YButton));
                Window1.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new LoopDelegate(XButton));
                Window1.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new LoopDelegate(BButton));
                Window1.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new LoopDelegate(AButton));

                // dispatcher for right stick
                Window1.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new LoopDelegate(RightStickButton));

                // dispatcher for dpad buttons
                Window1.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new LoopDelegate(DPadUpButton));
                Window1.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new LoopDelegate(DPadLeftButton));
                Window1.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new LoopDelegate(DPadRightButton));
                Window1.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new LoopDelegate(DPadDownButton));
            }
            // unless if controller is not connected...
            else if (!currentState.IsConnected)
            {
                // ...have program complain controller is disconnected
                LinkTextBlock.Text = "Controller: Disconnected";
                LinkTextBlock.Foreground = Brushes.Red;
            }

            // run dispatcher again, forever
            Window1.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new LoopDelegate(Link));
        }

        // when MainCameraControl tabs are selected
        private void MainCameraTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // switch tab item index (0 to 3)
            switch (MainCameraTabControl.SelectedIndex)
            {
                    // 1st tab item
                case 0:
                    // change background of tab control to gold
                    MainCameraTabControl.Background = Brushes.Gold;

                    /* disable ability to focus for SubCamera1 because its the same camera, enable the rest
                    SubCamera1.Focusable = false;
                    SubCamera2.Focusable = true;
                    SubCamera3.Focusable = true;
                    SubCamera4.Focusable = true;*/
                    return;
                    // 2nd tab item
                case 1:
                    // change background of tab control to red
                    MainCameraTabControl.Background = Brushes.Red;

                    /* disable ability to focus for SubCamera2 because its the same camera, enable the rest
                    SubCamera1.Focusable = true;
                    SubCamera2.Focusable = false;
                    SubCamera3.Focusable = true;
                    SubCamera4.Focusable = true;*/
                    return;
                    // 3rd tab item
                case 2:
                    // change background of tab control to green
                    MainCameraTabControl.Background = Brushes.Green;

                    /* disable ability to focus for SubCamera3 because its the same camera, enable the rest
                    SubCamera1.Focusable = true;
                    SubCamera2.Focusable = true;
                    SubCamera3.Focusable = false;
                    SubCamera4.Focusable = true;*/
                    return;
                    // 4th tab item
                case 3:
                    // change background of tab control to blue
                    MainCameraTabControl.Background = Brushes.Blue;

                    /* disable ability to focus for SubCamera4 because its the same camera, enable the rest
                    SubCamera1.Focusable = true;
                    SubCamera2.Focusable = true;
                    SubCamera3.Focusable = true;
                    SubCamera4.Focusable = false;*/
                    return;
            }
        }

        // When SubCameraTabControl tabs are selected
        private void SubCameraTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // switch tab item index (0 to 3)
            switch (SubCameraTabControl.SelectedIndex)
            {
                    // 1st tab itm
                case 0:
                    // change background of tab control to gold
                    SubCameraTabControl.Background = Brushes.Gold;

                    /* disable ability to focus for MainCamera1 because its the same camera, enable the rest
                    MainCamera1.Focusable = false;
                    MainCamera2.Focusable = true;
                    MainCamera3.Focusable = true;
                    MainCamera4.Focusable = true;*/
                    return;
                    // 2nd tab item
                case 1:
                    // change background of tab control to gred
                    SubCameraTabControl.Background = Brushes.Red;

                    /* disable ability to focus for MainCamera2 because its the same camera, enable the rest
                    MainCamera1.Focusable = true;
                    MainCamera2.Focusable = false;
                    MainCamera3.Focusable = true;
                    MainCamera4.Focusable = true;*/
                    return;
                    // 3rd tab item
                case 2:
                    // change background of tab control to green
                    SubCameraTabControl.Background = Brushes.Green;

                    /* disable ability to focus for MainCamera3 because its the same camera, enable the rest
                    MainCamera1.Focusable = true;
                    MainCamera2.Focusable = true;
                    MainCamera3.Focusable = false;
                    MainCamera4.Focusable = true;*/
                    return;
                    // 4th tab item
                case 3:
                    // change background of tab control to blue
                    SubCameraTabControl.Background = Brushes.Blue;

                    /* disable ability to focus for MainCamera4 because its the same camera, enable the rest
                    MainCamera1.Focusable = true;
                    MainCamera2.Focusable = true;
                    MainCamera3.Focusable = true;
                    MainCamera4.Focusable = false;*/
                    return;
            }
        }

        // stuff that happens when to move arond the ring buffer
        void ringSwitch()
        {
            switch (rockRing)
            {
                // bold text block for red rocks and normalizes neighboring text blocks
                case 0:
                    PurpleTextBlock.FontWeight = FontWeights.Normal;
                    RedTextBlock.FontWeight = FontWeights.UltraBold;
                    OrangeTextBlock.FontWeight = FontWeights.Normal;

                    PurpleRock.Stroke = Brushes.Black;
                    RedRock.Stroke = Brushes.White;
                    OrangeRock.Stroke = Brushes.Black;

                    PurpleRock.StrokeThickness = 1;
                    RedRock.StrokeThickness = 4;
                    OrangeRock.StrokeThickness = 1;
                    return;
                // bold text block for orange rocks and normalizes neighboring text blocks
                case 1:
                    RedTextBlock.FontWeight = FontWeights.Normal;
                    OrangeTextBlock.FontWeight = FontWeights.UltraBold;
                    YellowTextBlock.FontWeight = FontWeights.Normal;

                    RedRock.Stroke = Brushes.Black;
                    OrangeRock.Stroke = Brushes.White;
                    YellowRock.Stroke = Brushes.Black;

                    RedRock.StrokeThickness = 1;
                    OrangeRock.StrokeThickness = 4;
                    YellowRock.StrokeThickness = 1;
                    return;
                // bold text block for yellow rocks and normalizes neighboring text blocks
                case 2:
                    OrangeTextBlock.FontWeight = FontWeights.Normal;
                    YellowTextBlock.FontWeight = FontWeights.UltraBold;
                    GreenTextBlock.FontWeight = FontWeights.Normal;

                    OrangeRock.Stroke = Brushes.Black;
                    YellowRock.Stroke = Brushes.White;
                    GreenRock.Stroke = Brushes.Black;

                    OrangeRock.StrokeThickness = 1;
                    YellowRock.StrokeThickness = 4;
                    GreenRock.StrokeThickness = 1;
                    return;
                // bold text block for green rocks and normalizes neighboring text blocks
                case 3:
                    YellowTextBlock.FontWeight = FontWeights.Normal;
                    GreenTextBlock.FontWeight = FontWeights.UltraBold;
                    BlueTextBlock.FontWeight = FontWeights.Normal;

                    YellowRock.Stroke = Brushes.Black;
                    GreenRock.Stroke = Brushes.White;
                    BlueRock.Stroke = Brushes.Black;

                    YellowRock.StrokeThickness = 1;
                    GreenRock.StrokeThickness = 4;
                    BlueRock.StrokeThickness = 1;
                    return;
                // bold text block for blue rocks and normalizes neighboring text blocks
                case 4:
                    GreenTextBlock.FontWeight = FontWeights.Normal;
                    BlueTextBlock.FontWeight = FontWeights.UltraBold;
                    PurpleTextBlock.FontWeight = FontWeights.Normal;

                    GreenRock.Stroke = Brushes.Black;
                    BlueRock.Stroke = Brushes.White;
                    PurpleRock.Stroke = Brushes.Black;

                    GreenRock.StrokeThickness = 1;
                    BlueRock.StrokeThickness = 4;
                    PurpleRock.StrokeThickness = 1;
                    return;
                // bold text block for purple rocks and normalizes neighboring text blocks
                case 5:
                    BlueTextBlock.FontWeight = FontWeights.Normal;
                    PurpleTextBlock.FontWeight = FontWeights.UltraBold;
                    RedTextBlock.FontWeight = FontWeights.Normal;

                    BlueRock.Stroke = Brushes.Black;
                    PurpleRock.Stroke = Brushes.White;
                    RedRock.Stroke = Brushes.Black;

                    BlueRock.StrokeThickness = 1;
                    PurpleRock.StrokeThickness = 4;
                    RedRock.StrokeThickness = 1;
                    return;
            }
        }

        // the function that changes rock count
        void rockIncrement()
        {
            switch (rockRing)
            {
                // change red count and display it
                case 0:
                    red = red + incrementValue;
                    if (red < 0)
                        red = 0;
                    RedCount.Text = red.ToString();
                    RedCountShadow.Text = red.ToString();
                    return;
                // change red count and display it
                case 1:
                    orange = orange + incrementValue;
                    if (orange < 0)
                        orange = 0;
                    OrangeCount.Text = orange.ToString();
                    OrangeCountShadow.Text = orange.ToString();
                    return;
                // change red count and display it
                case 2:
                    yellow = yellow + incrementValue;
                    if (yellow < 0)
                        yellow = 0;
                    YellowCount.Text = yellow.ToString();
                    YellowCountShadow.Text = yellow.ToString();
                    return;
                // change red count and display it
                case 3:
                    green = green + incrementValue;
                    if (green < 0)
                        green = 0;
                    GreenCount.Text = green.ToString();
                    GreenCountShadow.Text = green.ToString();
                    return;
                // change red count and display it
                case 4:
                    blue = blue + incrementValue;
                    if (blue < 0)
                        blue = 0;
                    BlueCount.Text = blue.ToString();
                    BlueCountShadow.Text = blue.ToString();
                    return;
                // change red count and display it
                case 5:
                    purple = purple + incrementValue;
                    if (purple == -1)
                        purple = 0;
                    PurpleCount.Text = purple.ToString();
                    PurpleCountShadow.Text = purple.ToString();
                    return;
            }
        }

        private void MainCamera1_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }
    }
}