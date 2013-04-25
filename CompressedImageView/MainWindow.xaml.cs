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
        public delegate void LoopDelegate();
        private static System.Timers.Timer aTimer;
        GamePadState currentState;
        NodeHandle nh;

        bool end, near;
        float leftMotor, rightMotor;
        int hours = 1, minutes = 0, seconds = 0;

        public MainWindow()
        {
            InitializeComponent();

            aTimer = new System.Timers.Timer(10000);
            aTimer.Elapsed += new ElapsedEventHandler(UpdateTimer);
            aTimer.Interval = 1000;
        }

        private void UpdateTimer(object source, ElapsedEventArgs e)
        {
            if (minutes == 0)
            {
                hours--;
                minutes = 60;
            }

            if (seconds == 0)
            {
                minutes--;
                seconds = 60;
            }

            seconds--;

            if ((minutes == 59) && (seconds == 55))
            {
                near = true;
            }

            if ((minutes == 59) && (seconds == 50))
            {
                aTimer.Enabled = false;
                end = true;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Window1.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new LoopDelegate(Timer));
            Window1.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new LoopDelegate(Link));
            Window1.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle, new LoopDelegate(Voice));

            ROS.ROS_MASTER_URI = "http://10.0.3.88:11311";
            ROS.ROS_HOSTNAME = "10.0.3.101";
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

        protected override void OnClosed(EventArgs e)
        {
            ROS.shutdown();
            base.OnClosed(e);
        }

        public void Timer()
        {
            TimerTextBlock.Text = "Elapsed: " + hours.ToString("D2") + ':' + minutes.ToString("D2") + ':' + seconds.ToString("D2");

            if (near == true)
                TimerTextBlock.Foreground = Brushes.Yellow;

            if (end == true)
            {
                TimerTextBlock.Foreground = Brushes.Red;
                TimerStatusTextBlock.Text = "END OF TIME";
                TimerTextBlock.Text = "Press Right Stick to restart";
                TimerStatusTextBlock.Foreground = Brushes.Red;
            }

            Window1.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new LoopDelegate(Timer));
        }

        public void Voice()
        {
            currentState = GamePad.GetState(PlayerIndex.One);

            Window1.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new LoopDelegate(Voice));
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
                    return;
                case 1:
                    MainCameraTabControl.Background = Brushes.Blue;
                    return;
                case 2:
                    MainCameraTabControl.Background = Brushes.Red;
                    return;
                case 3:
                    MainCameraTabControl.Background = Brushes.Green;
                    return;
            }
        }

        private void SubCameraTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Create a vertical linear gradient 
            switch (SubCameraTabControl.SelectedIndex)
            {
                case 0:
                    SubCameraTabControl.Background = Brushes.Gold;
                    return;
                case 1:
                    SubCameraTabControl.Background = Brushes.Blue;
                    return;
                case 2:
                    SubCameraTabControl.Background = Brushes.Red;
                    return;
                case 3:
                    SubCameraTabControl.Background = Brushes.Green;
                    return;
            }
        }
    }
}