// same stuff from MainWindow.xaml.cs
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
            // default values; when not pressed

            // the light yellow button is invisible
            LTrigPress.Fill = Brushes.Transparent;

            // value is 0
            LeftTriggerProgressBar.Value = 0;
            // text says 0%
            LeftTriggerValueTextBlock.Text = "0%";
            // original margin location
            LeftTriggerValueTextBlock.Margin = new Thickness(111, 0, 0, 203);

            // get state of player one
            currentState = GamePad.GetState(PlayerIndex.One);

            // if left trigger is pushed, it gets a value greater than 0
            if (currentState.Triggers.Left != 0)
            {
                // reveal the light yellow button
                LTrigPress.Fill = Brushes.White;
                // set progress bar value to trigger value
                LeftTriggerProgressBar.Value = currentState.Triggers.Left;
                // display trigger value as a %
                LeftTriggerValueTextBlock.Text = (LeftTriggerProgressBar.Value * 100).ToString("F0") + '%';
                // move display trigger value up as trigger value increases
                LeftTriggerValueTextBlock.Margin = new Thickness(111, 0, 0, 203 + (currentState.Triggers.Left * 100));
                // set left vibration motor to trigger value (to be removed)
                leftMotor = currentState.Triggers.Left;
                
                if (swapCam == true)
                {
                    swapCam = false;
                    // get current main camera index
                    mainCam = MainCameraTabControl.SelectedIndex;
                    // get current sub camera index
                    subCam = SubCameraTabControl.SelectedIndex;
                    // set the main camera index the same as the sub camera index
                    MainCameraTabControl.SelectedIndex = subCam;
                    // set the sub camera index the same as the main camera index
                    SubCameraTabControl.SelectedIndex = mainCam;
                }
            }
            // if trigger is released (entire else is to be removed)
            if (currentState.Triggers.Left == 0)
            {
                swapCam = true;

                // left vibration motor value is 0
                leftMotor = 0;
            }

            // vibrate motors
            GamePad.SetVibration(PlayerIndex.One, leftMotor, rightMotor);

            // run dispatcher again, forever
            Window1.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new LoopDelegate(LeftTriggerButton));
        }

        // right trigger functions
        public void RightTriggerButton()
        {
            // default values; when not pressed

            // the light yellow button is invisible
            RTrigPress.Fill = Brushes.Transparent;

            // value is 0
            RightTriggerProgressBar.Value = 0;
            // display 0%
            RightTriggerValueTextBlock.Text = "0%";
            // original margins
            RightTriggerValueTextBlock.Margin = new Thickness(223, 0, 0, 203);

            // get state of player one
            currentState = GamePad.GetState(PlayerIndex.One);

            // if right trigger is pushed, it gets a value greater than 0
            if (currentState.Triggers.Right != 0)
            {
                // reveal the light yellow button
                RTrigPress.Fill = Brushes.White;
                // set progress bar value to trigger value
                RightTriggerProgressBar.Value = currentState.Triggers.Right;
                // display trigger value as a %
                RightTriggerValueTextBlock.Text = (RightTriggerProgressBar.Value * 100).ToString("F0") + '%';
                // move display trigger value up as trigger value increases
                RightTriggerValueTextBlock.Margin = new Thickness(223, 0, 0, 203 + (currentState.Triggers.Right * 100));
                // set right vibration motor to trigger value (to be removed)
                rightMotor = currentState.Triggers.Right;
            }
                // if trigger is released (entire else is to be removed)
            else
            {
                // right vibration motor value is 0
                rightMotor = 0;
            }

            // vibrate motors
            GamePad.SetVibration(PlayerIndex.One, leftMotor, rightMotor);

            // run dispatcher again, forever
            Window1.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new LoopDelegate(RightTriggerButton));
        }

        // left shoulder functions
        public void LeftShoulderButton()
        {
            // default values; when not pressed

            // rectangle box is transparent
            LShPress.Fill = Brushes.Transparent;

            // get state of player one
            currentState = GamePad.GetState(PlayerIndex.One);

            // if left shoulder is pressed and right shoulder is NOT pressed
            if ((currentState.Buttons.LeftShoulder == ButtonState.Pressed) && (currentState.Buttons.RightShoulder == ButtonState.Released))
            {
                // ractangle box is white
                LShPress.Fill = Brushes.White;

                // if Y is pressed while left shoulder is pressed
                if (currentState.Buttons.Y == ButtonState.Pressed)
                {
                    // show MainCamera1; 1st tab of the big tab control
                    MainCamera1.Focus();
                }

                // if B is pressed while left shoulder is pressed
                if (currentState.Buttons.B == ButtonState.Pressed)
                {
                    // show MainCamera2; 2nd tab of the big tab control
                    MainCamera2.Focus();
                }

                // if A is pressed while left shoulder is pressed
                if (currentState.Buttons.A == ButtonState.Pressed)
                {
                    // show MainCamera3; 3rd tab of the big tab control
                    MainCamera3.Focus();
                }

                // if X is pressed while left shoulder is pressed
                if (currentState.Buttons.X == ButtonState.Pressed)
                {
                    // show MainCamera4; 4th tab of the big tab control
                    MainCamera4.Focus();
                }
            }

            // run dispatcher again, forever
            Window1.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new LoopDelegate(LeftShoulderButton));
        }

        // right shoulder functions
        public void RightShoulderButton()
        {
            // default values; when not pressed

            // rectangle box is transparent
            RShPress.Fill = Brushes.Transparent;

            // get status of player one
            currentState = GamePad.GetState(PlayerIndex.One);

            // if right shoulder is pressed and left shoulder is NOT pressed
            if ((currentState.Buttons.RightShoulder == ButtonState.Pressed) && (currentState.Buttons.LeftShoulder == ButtonState.Released))
            {
                // rectangle box is white
                RShPress.Fill = Brushes.White;

                // if Y is pressed while right shoulder is pressed
                if (currentState.Buttons.Y == ButtonState.Pressed)
                {
                    // show SubCamera1; 1st tab of the small tab control
                    SubCamera1.Focus();
                }

                // if B is pressed while right shoulder is pressed
                if (currentState.Buttons.B == ButtonState.Pressed)
                {
                    // show SubCamera2; 2nd tab of the small tab control
                    SubCamera2.Focus();
                }

                // if A is pressed while right shoulder is pressed
                if (currentState.Buttons.A == ButtonState.Pressed)
                {
                    // show SubCamera3; 3rd tab of the small tab control
                    SubCamera3.Focus();
                }

                // if X is pressed while right shoulder is pressed
                if (currentState.Buttons.X == ButtonState.Pressed)
                {
                    // show SubCamera4; 4th tab of the small tab control
                    SubCamera4.Focus();
                }
            }

            // run dispatcher again, forever
            Window1.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new LoopDelegate(RightShoulderButton));
        }

        // back button functions; pause timer
        public void BackButton()
        {
            // default values; when not pressed

            // ellipse is transparent
            Back.Fill = Brushes.Transparent;

            // get state of player one
            currentState = GamePad.GetState(PlayerIndex.One);

            // if back is pressed
            if (currentState.Buttons.Back == ButtonState.Pressed)
            {
                // ellipse is white
                Back.Fill = Brushes.White;

                // is timer is not at the end
                if (end == false)
                {
                    // pause timer
                    aTimer.Enabled = false;
                    // display that timer is paused
                    TimerStatusTextBlock.Text = "Paused";
                    // display in yellow
                    TimerStatusTextBlock.Foreground = Brushes.Yellow;
                }
            }

            // run dispatcher again, forever
            Window1.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new LoopDelegate(BackButton));
        }

        // start button funcions; start timer
        public void StartButton()
        {
            // default values; when not pressed

            // ellipse is transparent
            Start.Fill = Brushes.Transparent;

            // get state of player one
            currentState = GamePad.GetState(PlayerIndex.One);

            // if start is pressed
            if (currentState.Buttons.Start == ButtonState.Pressed)
            {
                // ellipse is white
                Start.Fill = Brushes.White;

                // if timer is not at the end
                if (end == false)
                {
                    // begin timer
                    aTimer.Enabled = true;
                    // display that timer is ticking
                    TimerStatusTextBlock.Text = "Ticking...";
                    // display in blue
                    TimerStatusTextBlock.Foreground = Brushes.Blue;
                }
            }

            // run dispatcher again, forever
            Window1.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new LoopDelegate(StartButton));
        }

        // left stick functions
        public void LeftStickButton()
        {
            // bigger circle is actually dark gray; some color name mistake which is still unfixed
            LeftStick.Fill = Brushes.Gray;

            // get state of player one
            currentState = GamePad.GetState(PlayerIndex.One);

            // if left stick is clicked/pressed
            if (currentState.Buttons.LeftStick == ButtonState.Pressed)
            {
                // bigger circle is actually light gray; some color name mistake which is still unfixed
                LeftStick.Fill = Brushes.DarkGray;
}

            // move the smaller circle with x and y values of the stick
            LeftStickValue.Margin = new Thickness(131 + (currentState.ThumbSticks.Left.X * 25), 0, 0, 87 + (currentState.ThumbSticks.Left.Y * 25));

            // run dispatcher again, forever
            Window1.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new LoopDelegate(LeftStickButton));
        }

        // y functions
        public void YButton()
        {
            // default values; when not pressed

            // circle is transparent
            Y.Fill = Brushes.Transparent;

            // get state of player one
            currentState = GamePad.GetState(PlayerIndex.One);

            // if y is pressed
            if (currentState.Buttons.Y == ButtonState.Pressed)
            {
                // make circle gold
                Y.Fill = Brushes.Gold;
            }

            // run dispatcher again, forever
            Window1.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new LoopDelegate(YButton));
        }

        // x functions
        public void XButton()
        {
            // default values; when nor pressed

            // circle is transparent
            X.Fill = Brushes.Transparent;

            // get state of player one
            currentState = GamePad.GetState(PlayerIndex.One);

            // if x is pressed
            if (currentState.Buttons.X == ButtonState.Pressed)
            {
                // circle is blue
                X.Fill = Brushes.Blue;
            }

            // rundispatcher again forever
            Window1.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new LoopDelegate(XButton));
        }

        // b functions
        public void BButton()
        {
            // circle is transparent
            B.Fill = Brushes.Transparent;

            // get state of player one
            currentState = GamePad.GetState(PlayerIndex.One);

            // if b is pressed
            if (currentState.Buttons.B == ButtonState.Pressed)
            {
                // circle is red
                B.Fill = Brushes.Red;
            }

            // run dispatcher again, forever
            Window1.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new LoopDelegate(BButton));
        }

        // a functions
        public void AButton()
        {
            // default values; when not pressed

            // circle is transparent
            A.Fill = Brushes.Transparent;

            // get state of player one
            currentState = GamePad.GetState(PlayerIndex.One);

            // if a is pressed
            if (currentState.Buttons.A == ButtonState.Pressed)
            {
                // circle is green
                A.Fill = Brushes.Green;
            }

            // run dispatcher again, forever
            Window1.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new LoopDelegate(AButton));
        }

        // right stick function; reset timer
        public void RightStickButton()
        {
            // default values; when not pressed

            // bigger circle is actually dark gray; some color name mistake which is still unfixed
            RightStick.Fill = Brushes.Gray;

            // get state of player one
            currentState = GamePad.GetState(PlayerIndex.One);

            // if right stick is clicked/pressed
            if (currentState.Buttons.RightStick == ButtonState.Pressed)
            {
                // bigger circle is actually light gray; some color name mistake which is still unfixed
                RightStick.Fill = Brushes.DarkGray;

                // if timer is at the end
                if (end == true)
                {
                    // stop timer
                    aTimer.Enabled = false;
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

            // move smaller circle with x and y values of the right stick
            RightStickValue.Margin = new Thickness(243 + (currentState.ThumbSticks.Right.X * 25), 0, 0, 87 + (currentState.ThumbSticks.Right.Y * 25));

            // run dispatcher again, forever
            Window1.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new LoopDelegate(RightStickButton));
        }

        // dpad up functions
        public void DPadUpButton()
        {
            // default values; when not pressed

            // upper square is transparent
            DPadUp.Fill = Brushes.Transparent;

            // get state of player one
            currentState = GamePad.GetState(PlayerIndex.One);

            // if up is pressed
            if (currentState.DPad.Up == ButtonState.Pressed)
            {
                // upper square is actually light gray; some color name mistake which is still unfixed
                DPadUp.Fill = Brushes.DarkGray;

                // run this ring stuff one time when button is pressed
                if (ringIsFree == true)
                {
                    ringIsFree = false;

                    // set the increment value to 1
                    incrementValue = 1;
                    // call the function
                    rockIncrement();
                }
            }

            // allow ring stuff to run again when pressed
            if ((currentState.DPad.Left == ButtonState.Released) &&
                (currentState.DPad.Right == ButtonState.Released) &&
                (currentState.DPad.Up == ButtonState.Released) &&
                (currentState.DPad.Down == ButtonState.Released))
                ringIsFree = true;

            // run dispatcher again, forever
            Window1.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new LoopDelegate(DPadUpButton));
        }

        // dpad left functions
        public void DPadLeftButton()
        {
            // default values; when not pressed

            // left square is transparent
            DPadLeft.Fill = Brushes.Transparent;

            // get state of player one
            currentState = GamePad.GetState(PlayerIndex.One);

            // if dpad left is pressed
            if (currentState.DPad.Left == ButtonState.Pressed)
            {
                // left square is actually ligght gray; some color name mistake which is still unfixed
                DPadLeft.Fill = Brushes.DarkGray;

                // run this ring stuff once when button is pressed
                if (ringIsFree == true)
                {
                    ringIsFree = false;
                    rockRing--;

                    if (rockRing < 0)
                        rockRing = 5;

                    ringSwitch();
                }
            }

            // allow ring stuff to run again 
            if ((currentState.DPad.Left == ButtonState.Released) &&
                (currentState.DPad.Right == ButtonState.Released) &&
                (currentState.DPad.Up == ButtonState.Released) &&
                (currentState.DPad.Down == ButtonState.Released))
                ringIsFree = true;


            // run dispatcher again, forever
            Window1.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new LoopDelegate(DPadLeftButton));
        }

        // dpad right functions
        public void DPadRightButton()
        {
            // default values; when not pressed

            // right square is transparent
            DPadRight.Fill = Brushes.Transparent;

            // get state of player one
            currentState = GamePad.GetState(PlayerIndex.One);

            // if dpad right is pressed
            if (currentState.DPad.Right == ButtonState.Pressed)
            {
                // right square is actually light gray; some color name mistake which is still unfixed
                DPadRight.Fill = Brushes.DarkGray;

                // run this ring stuff once when button is pressed
                if (ringIsFree == true)
                {
                    ringIsFree = false;
                    rockRing++;
                    rockRing = rockRing % 6;
                    ringSwitch();
                }
            }

            // allow ring stuff to run again when pressed again
            if ((currentState.DPad.Left == ButtonState.Released) &&
                (currentState.DPad.Right == ButtonState.Released) &&
                (currentState.DPad.Up == ButtonState.Released) &&
                (currentState.DPad.Down == ButtonState.Released))
                ringIsFree = true;

            // run dispatcher again, forever
            Window1.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new LoopDelegate(DPadRightButton));
        }

        // dpad down functions
        public void DPadDownButton()
        {
            // default values; when not pressed

            // lower square is transparent
            DPadDown.Fill = Brushes.Transparent;

            // get state of player one
            currentState = GamePad.GetState(PlayerIndex.One);

            // if dpad down is pressed
            if (currentState.DPad.Down == ButtonState.Pressed)
            {
                // lower square is actually light gray; some color name mistake which is still unfixed
                DPadDown.Fill = Brushes.DarkGray;

                // run this ring stuff when button is pressed
                if (ringIsFree == true)
                {
                    ringIsFree = false;
                    // set the increment value to -1
                    incrementValue = -1;
                    // call this function
                    rockIncrement();
                }
            }

            // allow ring stuff to run again when pressed again
            if ((currentState.DPad.Left == ButtonState.Released) &&
                (currentState.DPad.Right == ButtonState.Released) &&
                (currentState.DPad.Up == ButtonState.Released) &&
                (currentState.DPad.Down == ButtonState.Released))
                ringIsFree = true;


            // run dispatcher again, forever
            Window1.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new LoopDelegate(DPadDownButton));
        }
    }
}