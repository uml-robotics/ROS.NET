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

using System.Windows.Threading;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

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

        // timer near end
        // timer ended
        bool end, near;

        // left vibration motor value, right vibration motor value
        float leftMotor, rightMotor;

        // initialize timer values; 1 full hour
        int hours = 1, minutes = 0, seconds = 0;

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
            ROS.ROS_HOSTNAME = "10.0.3.117";
            ROS.Init(new string[0], "Image_Test");
            nh = new NodeHandle();
            new Thread(() =>
            {
                while (!ROS.shutting_down)
                {
                    ROS.spinOnce(ROS.GlobalNodeHandle);
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

        public void Link()
        {
            currentState = GamePad.GetState(PlayerIndex.One);

            if (currentState.IsConnected)
            {
                LinkTextBlock.Text = "Link: Connected";
                LinkTextBlock.Foreground = Brushes.Green;

                // Close Application
                if ((currentState.Buttons.Back == ButtonState.Pressed) &&
                    (currentState.Buttons.Start == ButtonState.Pressed) &&
                    (currentState.Buttons.LeftShoulder == ButtonState.Pressed) &&
                    (currentState.Buttons.RightShoulder == ButtonState.Pressed)
                    )
                {
                    Close();
                }

                Window1.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new LoopDelegate(LeftTriggerButton));
                Window1.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new LoopDelegate(RightTriggerButton));

                Window1.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new LoopDelegate(LeftShoulderButton));
                Window1.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new LoopDelegate(RightShoulderButton));

                Window1.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new LoopDelegate(BackButton));
                Window1.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new LoopDelegate(StartButton));

                Window1.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new LoopDelegate(LeftStickButton));
                Window1.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new LoopDelegate(YButton));
                Window1.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new LoopDelegate(XButton));
                Window1.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new LoopDelegate(BButton));
                Window1.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new LoopDelegate(AButton));

                Window1.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new LoopDelegate(RightStickButton));
                Window1.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new LoopDelegate(DPadUpButton));
                Window1.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new LoopDelegate(DPadLeftButton));
                Window1.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new LoopDelegate(DPadRightButton));
                Window1.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new LoopDelegate(DPadDownButton));
            }
            else if (!currentState.IsConnected)
            {
                LinkTextBlock.Text = "Link: Disconnected";
                LinkTextBlock.Foreground = Brushes.Red;
            }

            GamePad.SetVibration(PlayerIndex.One, leftMotor, rightMotor);
            Window1.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new LoopDelegate(Link));
        }

        private void MainCameraTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (MainCameraTabControl.SelectedIndex)
            {
                case 0:
                    MainCameraTabControl.Background = Brushes.Gold;
                    SubCamera1.Focusable = false;
                    SubCamera2.Focusable = true;
                    SubCamera3.Focusable = true;
                    SubCamera4.Focusable = true;
                    return;
                case 1:
                    MainCameraTabControl.Background = Brushes.Red;
                    SubCamera1.Focusable = true;
                    SubCamera2.Focusable = false;
                    SubCamera3.Focusable = true;
                    SubCamera4.Focusable = true;
                    return;
                case 2:
                    MainCameraTabControl.Background = Brushes.Green;
                    SubCamera1.Focusable = true;
                    SubCamera2.Focusable = true;
                    SubCamera3.Focusable = false;
                    SubCamera4.Focusable = true;
                    return;
                case 3:
                    MainCameraTabControl.Background = Brushes.Blue;
                    SubCamera1.Focusable = true;
                    SubCamera2.Focusable = true;
                    SubCamera3.Focusable = true;
                    SubCamera4.Focusable = false;
                    return;
            }
        }

        private void SubCameraTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (SubCameraTabControl.SelectedIndex)
            {
                case 0:
                    SubCameraTabControl.Background = Brushes.Gold;
                    MainCamera1.Focusable = false;
                    MainCamera2.Focusable = true;
                    MainCamera3.Focusable = true;
                    MainCamera4.Focusable = true;
                    return;
                case 1:
                    SubCameraTabControl.Background = Brushes.Red;
                    MainCamera1.Focusable = true;
                    MainCamera2.Focusable = false;
                    MainCamera3.Focusable = true;
                    MainCamera4.Focusable = true;
                    return;
                case 2:
                    SubCameraTabControl.Background = Brushes.Green;
                    MainCamera1.Focusable = true;
                    MainCamera2.Focusable = true;
                    MainCamera3.Focusable = false;
                    MainCamera4.Focusable = true;
                    return;
                case 3:
                    SubCameraTabControl.Background = Brushes.Blue;
                    MainCamera1.Focusable = true;
                    MainCamera2.Focusable = true;
                    MainCamera3.Focusable = true;
                    MainCamera4.Focusable = false;
                    return;
            }
        }
    }
}