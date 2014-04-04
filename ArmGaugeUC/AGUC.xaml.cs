using System;
using System.ComponentModel.Design;
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
using System.Windows.Media.Animation;
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
using am = Messages.sample_acquisition;

namespace ArmGaugeUC
{
    /// <summary>
    /// Interaction logic for ArmGauge.xaml
    /// </summary>
    public partial class ArmGauge : UserControl
    {
       
        //double ClickPanAngle;
        
        //Publisher<am.ArmMovement> pub;
        
        //NodeHandle nodecopy;
        //DestinationMarker destMark;
        //am.ArmMovement movecommand;

        double ArmPanAngle, ArmTiltAngle;
        long tilt_max, pan_max, tilt_min, pan_min;
        Subscriber<am.ArmStatus> sub;

        public ArmGauge()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {

            if (System.Diagnostics.Process.GetCurrentProcess().ProcessName == "devenv")
                return;
        }

        public void startListening(NodeHandle node)
        {

            tilt_min = -600;
            tilt_max = 600;
            pan_min = 0;
            pan_max = 5100;

            sub = node.subscribe<am.ArmStatus>("/arm/status", 1000, callbackMonitor);
            //pub = node.advertise<am.ArmMovement>("/arm/movement", 1000);
        }

        private void callbackMonitor(am.ArmStatus msg)
        {

            Dispatcher.BeginInvoke(new Action(() =>
            {

                //tilt lowest = 600, tilt highest = ???
                if (msg.tilt_position > tilt_max) tilt_max = msg.tilt_position; 
                if (msg.tilt_position > tilt_min) tilt_min = -msg.tilt_position;
                //pan lowest = assuming 0, pan max = ~5100
                if (msg.pan_position > pan_max) pan_max = msg.pan_position;
                if (msg.pan_position < pan_min) pan_min = msg.pan_position;

                ArmPanAngle = ( (-1.0*msg.pan_position) / (double)pan_max) * 250.0;

                ArmTiltAngle = ( (double)msg.tilt_position / (double)tilt_max) * 40.0;

                PanAnim.To = ArmPanAngle;
                TiltAnim.To = ArmTiltAngle;

                /*tilt = msg.tilt_motor_position;
                //pan = msg.pan_motor_position;
                //double grip = msg.cable_motor_position;

                ArmPanAngle = (pan * -90 + 180);

                PanAnim.To = ArmPanAngle;
                TiltAnim.To = (tilt * -30);
                //GripStatus.Value = grip;
                PanStory.Begin();
                TiltStory.Begin();

                //checks to see if the destination marker is set, and moves to that location.  one publish at a time.
                //this action is asynchronous.  If another click happens before gets to the destination, 
                /*the destMark will be moved without issue
                if (destMark.isActive == true)
                    if (!(ArmPanAngle < (destMark.PanAngle + 5) && ArmPanAngle > (destMark.PanAngle - 5)))
                    {
                        if (ClickPanAngle < ArmPanAngle)
                            movecommand.pan_motor_velocity = 1;
                        else
                            movecommand.pan_motor_velocity = -1;

                        pub.publish(movecommand);
                    }
                    else destMark.isActive = false;
                 */

            }));

        }

        /*
        //registers the click, converts the click location into an angle, and saves it in the destination marker
        private void PanCirle_MouseDown(object sender, MouseButtonEventArgs e)
        {

            Point clickPoint = e.GetPosition(PanCirle);

            double y = clickPoint.X;
            double x = clickPoint.Y;

            //correct the points so they align with cartesian coordinate system, with the center of the circle being point (0,0)
            x = (x - 50) * -1;
            y = (y - 50) * -1;

            double angleRad = Math.Atan2(x, y);

            ClickPanAngle = (angleRad * 180 / Math.PI) + 270;

            ROS.Info("x:" + x + " y:" + y + " ClickAngle:" + ClickPanAngle + " ArmAngle:" + ArmPanAngle);

            destMark.PanAngle = ClickPanAngle;
            destMark.isActive = true;

            if (ClickPanAngle < ArmPanAngle)
                movecommand.pan_motor_velocity = 1;
            else
                movecommand.pan_motor_velocity = -1;

            //pub.publish(movecommand);

        }*/

        
        
    }

    /*
    public class DestinationMarker
    {

        public double PanAngle { set; get; }
        public double TiltAngle { set; get; }
        public bool isActive { set; get; }

        public DestinationMarker() 
        {
            isActive = false;
        }

    }*/

}
