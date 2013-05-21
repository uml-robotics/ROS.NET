using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Threading;
using Microsoft.DirectX;
using Microsoft.DirectX.DirectInput;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Trust_Interface
{
    public class JoystickManager
    {
        #region variables
        Device joystick;
        const byte ButtonUp = 0;
        const byte ButtonDown = 128;
        /// <summary>
        /// Remembers the previous state of the drive trigger button.
        /// Currently button #6 (index 5) is the driver trigger button. 
        /// This must be true to pass drive commands.
        /// </summary>
        bool driveTriggerState = true;
        #endregion

        #region events
        public delegate void DriveHandler(object sender, JoystickArgs ja);
        public event DriveHandler driveHandler;
        public delegate void BrakeHandler(bool brakeStatus);
        public delegate void PTZVelocityHandler(object sender, JoystickArgs ja);
        public event PTZVelocityHandler ptzVelocityHandler;
        public delegate void ResetPTZHandler();
        public event ResetPTZHandler resetPTZHandler;
        //stuff to be called from the DREAM wrapper because apparently events in a base class can't be fired nor successfully overriden in a derrived class.
        internal void driveHandlerFire(object sender, JoystickArgs ja) { if (driveHandler != null) driveHandler(sender, ja); }
        internal void ptzVelocityHandlerFire(object sender, JoystickArgs ja) { if (ptzVelocityHandler != null) ptzVelocityHandler(sender, ja); }
        internal void resetPTZHandlerFire() { if (resetPTZHandler != null) resetPTZHandler(); }
        #endregion

        public bool connected;

        GamePadState _xboxState;
        public GamePadState XboxState() { return _xboxState; }

        /// <summary>
        /// This is the constructor that sets up the device for use. 
        /// If you plan to run without a joystick, use JoystickManager(bool b).
        /// </summary>
        /// <seealso cref="JoystickManager(bool b)"/>
        public JoystickManager()
        {
            try
            {
                DeviceList dl = Manager.GetDevices(DeviceType.Joystick, EnumDevicesFlags.AttachedOnly);
                while (dl.MoveNext())
                {
                    DeviceInstance di = (DeviceInstance)dl.Current;
                    if (di.DeviceType == DeviceType.Joystick)
                    {
                        InitializeJoystick(di);
                        break;
                    }
                }
                if (joystick == null)
                {
                    dl = Manager.GetDevices(DeviceType.Gamepad, EnumDevicesFlags.AttachedOnly);
                    while (dl.MoveNext())
                    {
                        DeviceInstance di = (DeviceInstance)dl.Current;
                        if (di.DeviceType == DeviceType.Gamepad)
                        {
                            InitializeJoystick(di);
                            break;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Shit the bed while trying to connectToBackend to the joystick.... are you sure you've got a joystick connected? " + e.Message);
            }
        }

        /// <summary>
        /// This constructor is used when there is no joystick and the UI needs to 
        /// be run for debuggin purposes.
        /// </summary>
        /// <param name="b"></param>
        public JoystickManager(bool b)
        {
        }

        /// <summary>
        /// Init the joystick and start polling the device.
        /// </summary>
        /// <param name="pDeviceInstance"></param>
        void InitializeJoystick(DeviceInstance pDeviceInstance)
        {
            connected = true;

            joystick = new Device(pDeviceInstance.ProductGuid);

            Thread t = new Thread(new ThreadStart(DoWork));
            t.Start();
            joystick.Acquire();
        }

        const double half = 65535 / 2;
        JoystickArgs CIRCULIZE(JoystickArgs input)
        {
            JoystickArgs ret;
            double x = input.originalX - half;
            double y = input.originalY - half;
            double angle = Math.Atan2(y, x);
            //amax = amax > Math.PI / 4 ? Math.PI / 2 - amax : amax;
            //if (amax == Math.PI / 2 || amax == 3*Math.PI/2) amax += Math.PI / 2;
            double max = Math.Min(Math.Abs(half / Math.Sin(angle)), Math.Abs(half / Math.Cos(angle)));
            if (double.IsNaN(max))
                max = half;
            double dist = half * (Math.Sqrt(x * x + y * y) / max);
            ret = new JoystickArgs(dist * Math.Cos(angle) + half, dist * Math.Sin(angle) + half, true);
            return ret;
        }

        /// <summary>
        /// This function is executed by the thread every 100ms. It 
        /// polls the device and generates different events based on the 
        /// joystick regions, buttons, etc.
        /// </summary>
        void DoWork()
        {
            byte[] prevButtonState = new byte[200];

            while (true)
            {
                JoystickState jss;

                try
                {
                    jss = joystick.CurrentJoystickState;
                }
                catch (Exception)
                {
                    Console.WriteLine("Can't get Joystick State.");
                    continue;
                }
                
                JoystickArgs ja = CIRCULIZE(new JoystickArgs(jss.X, jss.Y, true));
                JoystickArgs jb = CIRCULIZE(new JoystickArgs(jss.Rx, -jss.Ry, true));
                Buttons pressed = 0;

                Dictionary<int, Buttons> ps3map = new Dictionary<int,Buttons>() { 
                    {2, Buttons.X},
                    {3, Buttons.Y},
                    {1, Buttons.A},
                    {4, Buttons.B},
                    {9, Buttons.Back},
                    {10, Buttons.Start},
                    {11, Buttons.LeftStick},
                    {12, Buttons.RightStick},
                    {5, Buttons.LeftShoulder},
                    {6, Buttons.RightShoulder}};

                for (int i = 0; i < jss.GetButtons().Length; i++)
                {
                    if (jss.GetButtons()[i] == ButtonDown)
                    {
                        if (ps3map.ContainsKey(i+1))
                            pressed |= ps3map[i+1];
                    }
                }
                GamePadDPad gdp = new GamePadDPad();
                if (jss.GetPointOfView()[0] >= 0)
                {
                    switch (jss.GetPointOfView()[0] / 100)
                    {
                        case 0: gdp = new GamePadDPad(ButtonState.Pressed, ButtonState.Released, ButtonState.Released, ButtonState.Released); pressed |= Buttons.DPadUp; break;
                        case 45: gdp = new GamePadDPad(ButtonState.Pressed, ButtonState.Released, ButtonState.Released, ButtonState.Pressed); pressed |= Buttons.DPadUp | Buttons.DPadRight; break;
                        case 90: gdp = new GamePadDPad(ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Pressed); pressed |= Buttons.DPadRight; break;
                        case 135: gdp = new GamePadDPad(ButtonState.Released, ButtonState.Pressed, ButtonState.Released, ButtonState.Pressed); pressed |= Buttons.DPadRight | Buttons.DPadDown; break;
                        case 180: gdp = new GamePadDPad(ButtonState.Released, ButtonState.Pressed, ButtonState.Released, ButtonState.Released); pressed |= Buttons.DPadDown; break;
                        case 225: gdp = new GamePadDPad(ButtonState.Released, ButtonState.Pressed, ButtonState.Pressed, ButtonState.Released); pressed |= Buttons.DPadDown | Buttons.DPadLeft; break;
                        case 270: gdp = new GamePadDPad(ButtonState.Released, ButtonState.Released, ButtonState.Pressed, ButtonState.Released); pressed |= Buttons.DPadLeft; break;
                        case 315: gdp = new GamePadDPad(ButtonState.Pressed, ButtonState.Released, ButtonState.Pressed, ButtonState.Released); pressed |= Buttons.DPadUp | Buttons.DPadLeft; break;
                    }
                }
                _xboxState = new GamePadState(new GamePadThumbSticks(new Microsoft.Xna.Framework.Vector2((float)(ja.GetX() / half), (float)(ja.GetY() / half)), new Microsoft.Xna.Framework.Vector2((float)(jb.GetX() / half), (float)(jb.GetY() / half))),
                    new GamePadTriggers((float)(jss.GetButtons()[6] == ButtonDown ? 1 : 0), (float)(jss.GetButtons()[7] == ButtonDown ? 1 : 0)),
                    new GamePadButtons(pressed), gdp);

                // Fire events for joystick's button press & release
                // TODO: This can be optimized. Only button 6 & 7 are important.
                for (byte i = 0; i < jss.GetButtons().Length; i++)
                {
                    if (jss.GetButtons()[i] == ButtonUp && prevButtonState[i] == ButtonDown)
                    {
                        JsButtonReleased(i);
                    }
                    else if (jss.GetButtons()[i] == ButtonDown && prevButtonState[i] == ButtonUp)
                    {
                        JsButtonPressed(i);
                    }
                }

                // Only raise a drive event if the trigger is pressed.
                if (driveTriggerState)
                {
                    JoystickArgs j = CIRCULIZE(new JoystickArgs(jss.X, jss.Y, true));
                    if (driveHandler!=null)
                        driveHandler(this, j);
                }


                // Send ptz info regardless of the trigger state.
                if (ptzVelocityHandler != null)
                    ptzVelocityHandler(this, CIRCULIZE(new JoystickArgs(jss.Rz, jss.Z, true)));


                prevButtonState = jss.GetButtons();
                Thread.Sleep(10);
            }
        }

        void JsButtonPressed(byte jsButton)
        {
            ////// Drive trigger.
            ////if (jsButton == 4)
            ////{
            ////    driveTriggerState = !driveTriggerState;
            ////    driveTriggerHandler(driveTriggerState);
            ////}
            //if (jsButton == 4 && !driveTriggerState)
            //{
            //    driveTriggerState = true;
            //    driveTriggerHandler(true);
            //}
            // Reset PTZ.
            if (jsButton == 5)
            {
                if (resetPTZHandler != null)
                    resetPTZHandler();
            }
        }

        void JsButtonReleased(byte jsButton)
        {
            // Drive trigger.
            //if (jsButton == 4 && driveTriggerState)
            //{
            //    driveTriggerState = false;
            //    driveTriggerHandler(false);
            //}
        }

        public void SetTrigger(bool status)
        {
            driveTriggerState = status;
        }
    }
}

