// same shit from MainWindow.xaml.cs
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

namespace WpfApplication1
{
    public partial class MainWindow : Window
    {
        // Left Trigger functions
        public void LeftTriggerButton()
        {
            // default values

            // rectangle box is transparent
            LeftTrigger.Fill = Brushes.Transparent;
            // value is 0
            LeftTriggerProgressBar.Value = 0;
            // text says 0%
            LeftTriggerValueTextBlock.Text = "0%";
            // original margin location
            LeftTriggerValueTextBlock.Margin = new Thickness(101, 0, 0, 219);

            // get state of player one
            currentState = GamePad.GetState(PlayerIndex.One);

            // if left trigger gets a value
            if (currentState.Triggers.Left != 0)
            {
                // make the rectangle white
                LeftTrigger.Fill = Brushes.White;
                // set progress bar value to trigger value
                LeftTriggerProgressBar.Value = currentState.Triggers.Left;
                // display trigger value as %
                LeftTriggerValueTextBlock.Text = (LeftTriggerProgressBar.Value * 100).ToString("F0") + '%';
                // move display up as trigger value increases
                LeftTriggerValueTextBlock.Margin = new Thickness(101, 0, 0, 219 + (currentState.Triggers.Left * 100));
                // set left vibration motor to trigger value (to be removed)
                leftMotor = currentState.Triggers.Left;
            }
                // if trigger is equal to 0
            else
            {
                // remove left vibration motor value
                leftMotor = 0;
            }

            // run dispatcher again, forever
            Window1.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new LoopDelegate(LeftTriggerButton));
        }

        // right trigger functions
        public void RightTriggerButton()
        {
            // rectangle box is transparent
            RightTrigger.Fill = Brushes.Transparent;
            // value is 0
            RightTriggerProgressBar.Value = 0;
            // display 0%
            RightTriggerValueTextBlock.Text = "0%";
            // original margins
            RightTriggerValueTextBlock.Margin = new Thickness(220, 0, 0, 219);

            // get state of player one controller
            currentState = GamePad.GetState(PlayerIndex.One);

            if (currentState.Triggers.Right != 0)
            {
                RightTrigger.Fill = Brushes.White;
                RightTriggerProgressBar.Value = currentState.Triggers.Right;
                RightTriggerValueTextBlock.Text = (RightTriggerProgressBar.Value * 100).ToString("F0") + '%';
                RightTriggerValueTextBlock.Margin = new Thickness(220, 0, 0, 219 + (currentState.Triggers.Right * 100));
                rightMotor = currentState.Triggers.Right;
                GamePad.SetVibration(PlayerIndex.One, leftMotor, rightMotor);
            }
            else
            {
                rightMotor = 0;
                GamePad.SetVibration(PlayerIndex.One, leftMotor, rightMotor);
            }

            Window1.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new LoopDelegate(RightTriggerButton));
        }

        public void LeftShoulderButton()
        {
            LeftShoulder.Fill = Brushes.Transparent;
            currentState = GamePad.GetState(PlayerIndex.One);

            if ((currentState.Buttons.LeftShoulder == ButtonState.Pressed) && (currentState.Buttons.RightShoulder == ButtonState.Released))
            {
                LeftShoulder.Fill = Brushes.White;

                if (currentState.Buttons.Y == ButtonState.Pressed)
                {
                    MainCamera1.Focus();
                }

                if (currentState.Buttons.B == ButtonState.Pressed)
                {
                    MainCamera2.Focus();
                }

                if (currentState.Buttons.A == ButtonState.Pressed)
                {
                    MainCamera3.Focus();
                }

                if (currentState.Buttons.X == ButtonState.Pressed)
                {
                    MainCamera4.Focus();
                }
            }

            Window1.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new LoopDelegate(LeftShoulderButton));
        }

        public void RightShoulderButton()
        {
            RightShoulder.Fill = Brushes.Transparent;
            currentState = GamePad.GetState(PlayerIndex.One);

            if ((currentState.Buttons.RightShoulder == ButtonState.Pressed) && (currentState.Buttons.LeftShoulder == ButtonState.Released))
            {
                RightShoulder.Fill = Brushes.White;

                if (currentState.Buttons.Y == ButtonState.Pressed)
                {
                    SubCamera1.Focus();
                }

                if (currentState.Buttons.B == ButtonState.Pressed)
                {
                    SubCamera2.Focus();
                }

                if (currentState.Buttons.A == ButtonState.Pressed)
                {
                    SubCamera3.Focus();
                }

                if (currentState.Buttons.X == ButtonState.Pressed)
                {
                    SubCamera4.Focus();
                }
            }

            Window1.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new LoopDelegate(RightShoulderButton));
        }

        public void BackButton()
        {
            Back.Fill = Brushes.Transparent;
            currentState = GamePad.GetState(PlayerIndex.One);

            if (currentState.Buttons.Back == ButtonState.Pressed)
            {
                Back.Fill = Brushes.White;

                if (end == false)
                {
                    aTimer.Enabled = false;
                    TimerStatusTextBlock.Text = "Paused";
                    TimerStatusTextBlock.Foreground = Brushes.Yellow;
                }
            }

            Window1.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new LoopDelegate(BackButton));
        }

        public void StartButton()
        {
            Start.Fill = Brushes.Transparent;
            currentState = GamePad.GetState(PlayerIndex.One);

            if (currentState.Buttons.Start == ButtonState.Pressed)
            {
                Start.Fill = Brushes.White;

                if (end == false)
                {

                    aTimer.Enabled = true;
                    TimerStatusTextBlock.Text = "Ticking...";
                    TimerStatusTextBlock.Foreground = Brushes.Blue;
                }
            }

            Window1.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new LoopDelegate(StartButton));
        }

        public void LeftStickButton()
        {
            LeftStick.Fill = Brushes.Gray;
            currentState = GamePad.GetState(PlayerIndex.One);

            if (currentState.Buttons.LeftStick == ButtonState.Pressed)
            {
                LeftStick.Fill = Brushes.DarkGray;
            }

            LeftStickValue.Margin = new Thickness(52 + (currentState.ThumbSticks.Left.X * 25), 0, 0, 111 + (currentState.ThumbSticks.Left.Y * 25));
            Window1.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new LoopDelegate(LeftStickButton));
        }

        public void YButton()
        {
            Y.Fill = Brushes.Transparent;
            currentState = GamePad.GetState(PlayerIndex.One);

            if (currentState.Buttons.Y == ButtonState.Pressed)
            {
                Y.Fill = Brushes.Gold;
            }
            else if (currentState.Buttons.Y == ButtonState.Pressed)
            {
                Y.Fill = Brushes.Yellow;
            }

            Window1.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new LoopDelegate(YButton));
        }

        public void XButton()
        {
            X.Fill = Brushes.Transparent;
            currentState = GamePad.GetState(PlayerIndex.One);

            if (currentState.Buttons.X == ButtonState.Pressed)
            {
                X.Fill = Brushes.Blue;
            }

            Window1.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new LoopDelegate(XButton));
        }

        public void BButton()
        {
            B.Fill = Brushes.Transparent;
            currentState = GamePad.GetState(PlayerIndex.One);

            if (currentState.Buttons.B == ButtonState.Pressed)
            {
                B.Fill = Brushes.Red;
            }

            Window1.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new LoopDelegate(BButton));
        }

        public void AButton()
        {
            A.Fill = Brushes.Transparent;
            currentState = GamePad.GetState(PlayerIndex.One);

            if (currentState.Buttons.A == ButtonState.Pressed)
            {
                A.Fill = Brushes.Green;
            }

            Window1.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new LoopDelegate(AButton));
        }

        public void RightStickButton()
        {
            RightStick.Fill = Brushes.Gray;
            currentState = GamePad.GetState(PlayerIndex.One);

            if (currentState.Buttons.RightStick == ButtonState.Pressed)
            {
                RightStick.Fill = Brushes.DarkGray;

                if (end == true)
                {
                    aTimer.Enabled = false;
                    hours = 1;
                    minutes = 0;
                    seconds = 0;
                    near = false;
                    end = false;
                    TimerStatusTextBlock.Foreground = Brushes.Green;
                    TimerStatusTextBlock.Text = "Ready";
                    TimerTextBlock.Foreground = Brushes.Black;
                }
            }

            RightStickValue.Margin = new Thickness(238 + (currentState.ThumbSticks.Right.X * 25), 0, 0, 53 + (currentState.ThumbSticks.Right.Y * 25));
            Window1.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new LoopDelegate(RightStickButton));
        }

        public void DPadUpButton()
        {
            DPadUp.Fill = Brushes.Transparent;
            currentState = GamePad.GetState(PlayerIndex.One);

            if (currentState.DPad.Up == ButtonState.Pressed)
            {
                DPadUp.Fill = Brushes.DarkGray;
            }

            Window1.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new LoopDelegate(DPadUpButton));
        }

        public void DPadLeftButton()
        {
            DPadLeft.Fill = Brushes.Transparent;
            currentState = GamePad.GetState(PlayerIndex.One);

            if (currentState.DPad.Left == ButtonState.Pressed)
            {
                DPadLeft.Fill = Brushes.DarkGray;
            }

            Window1.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new LoopDelegate(DPadLeftButton));
        }

        public void DPadRightButton()
        {
            DPadRight.Fill = Brushes.Transparent;
            currentState = GamePad.GetState(PlayerIndex.One);

            if (currentState.DPad.Right == ButtonState.Pressed)
            {
                DPadRight.Fill = Brushes.DarkGray;
            }

            Window1.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new LoopDelegate(DPadRightButton));
        }

        public void DPadDownButton()
        {
            DPadDown.Fill = Brushes.Transparent;
            currentState = GamePad.GetState(PlayerIndex.One);

            if (currentState.DPad.Down == ButtonState.Pressed)
            {
                DPadDown.Fill = Brushes.DarkGray;
            }

            Window1.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new LoopDelegate(DPadDownButton));
        }
    }
}