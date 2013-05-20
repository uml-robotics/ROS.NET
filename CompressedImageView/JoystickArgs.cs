using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows;
using System.Linq;
using System.Text;


namespace Trust_Interface
{
    public class JoystickArgs : System.EventArgs
    {
        internal double originalX, originalY;
        #region variables
        /// <summary>
        /// This is the max value that the gamppad return for the analog joystick.
        /// The min value is 0.
        /// </summary>
        const int iAnalogMax = 65535;
        /// <summary>
        /// This should contain a normalized value between -100 and 100. This value is
        /// based on the incoming x and y raw joystick values, the null zone, and the 
        /// trigger values.
        /// </summary>
        Point joyValue = new Point(0.0, 0.0);
        /// <summary>
        /// The trigger is nessary for the analog joystick values to be passed along. 
        /// If the trigger is not pressed then the analog joystick values as set to 0,0.
        /// </summary>
        bool trigger = false;
        /// <summary>
        /// This is the null zone area in raw value (10% of 65535 = 6553.5). If the analog joystick values are 
        /// within this range then they are automatically set to zero. 
        /// </summary>
        const double iNullZoneTolerance = (65535*0.1);
        #endregion

        #region constructors
        public JoystickArgs(double X, double Y, bool Trigger)
        {
            originalX = X;
            originalY = Y;
            trigger = Trigger;
            joyValue = FilterNullZone(X, Y);

            if (!trigger)
            {
                joyValue.X = 0;
                joyValue.Y = 0;
            }
        }
        #endregion

        #region public_api
        public double GetX()
        {
            return joyValue.X;
        }

        public double GetY()
        {
            return joyValue.Y;
        }

        public bool GetTrigger()
        {
            return trigger;
        }
        #endregion

        #region misc
        /// <summary>
        /// Converts the raw analog joystick values into % and 
        /// checks to see if they are greater than the null zone values. 
        /// If they are not greater, then the value is set to zero.
        /// </summary>
        /// <param name="joyValue"></param>
        /// <returns></returns>
        const double m = (100 / (32767.5 - iNullZoneTolerance));
        Point FilterNullZone(double x, double y)
        {
            x -= 32767.5; // 65535/2 = 32767.5
            y -= 32767.5;
            double ang = Math.Atan2(y,x);
            double dist = Math.Sqrt(x * x + y * y);
            dist = m * dist + (100 - m * 32767.5);
            if (dist < 0)
                dist = 0;
            return new Point(dist * Math.Cos(ang), dist * Math.Sin(ang));
        }
        #endregion
    }
}
