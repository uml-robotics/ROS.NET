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
using am = Messages.arm_status_msgs;

namespace ArmGaugeUC
{
    /// <summary>
    /// Interaction logic for ArmGauge.xaml
    /// </summary>
    public partial class ArmGauge : UserControl
    {
       
        double ClickPanAngle, pan, tilt;
        double ArmPanAngle;
        Publisher<am.ArmMovement> pub;
        Subscriber<am.ArmMovement> sub;
        NodeHandle nodecopy;
        DestinationMarker destMark;
        am.ArmMovement movecommand;

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
            this.nodecopy = node;
            this.destMark = new DestinationMarker();
            this.movecommand = new am.ArmMovement();

            sub = node.subscribe<am.ArmMovement>("/arm/status", 1000, callbackMonitor);
            pub = node.advertise<am.ArmMovement>("/arm/movement", 1000);
            new Thread(() =>
            {
                while (!ROS.shutting_down)
                {
                    ROS.spinOnce(node);
                    Thread.Sleep(1);
                }
            }).Start();
        }

        private void callbackMonitor(am.ArmMovement msg)
        {

            Dispatcher.BeginInvoke(new Action(() =>
            {

                tilt = msg.tilt_motor_position;
                pan = msg.pan_motor_position;
                double grip = msg.cable_motor_position;

                ArmPanAngle = (pan * -90 + 180);

                PanAnim.To = ArmPanAngle;
                TiltAnim.To = (tilt * -50);
                GripStatus.Value = grip;
                PanStory.Begin();
                TiltStory.Begin();

                //checks to see if the destination marker is set, and moves to that location.  one publish at a time.
                //this action is asynchronous.  If another click happens before gets to the destination, 
                //the destMark will be moved without issue
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
                        

            }));

        }


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

            pub.publish(movecommand);

        }

        
        
    }


    public class DestinationMarker
    {

        public double PanAngle { set; get; }
        public double TiltAngle { set; get; }
        public bool isActive { set; get; }

        public DestinationMarker() 
        {
            isActive = false;
        }

    }

}
