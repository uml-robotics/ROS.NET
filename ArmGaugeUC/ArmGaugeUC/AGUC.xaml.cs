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
        //gross, I know...
        private double degrees;
        Publisher<am.ArmMovement> pub;
        Subscriber<am.ArmMovement> sub;

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

                double tilt = msg.tilt_motor_position;
                double pan = msg.pan_motor_position;
                double grip = msg.cable_motor_position;

                PanAnim.To = (pan * -90 + 180);
                TiltAnim.To = (tilt * -50);
                //GripAnim.To = (grip * -30 + 30);
                PanStory.Begin();
                TiltStory.Begin();
                //GripStory.Begin();

            }));

        }

        private void PanCirle_MouseDown(object sender, MouseButtonEventArgs e)
        {

            Point clickPoint = e.GetPosition(PanCirle);

            double y = clickPoint.X;
            double x = clickPoint.Y;

            //correct the points so they align with cartesian coordinate system, with the center of the circle being point (0,0)
            x = (x - 50) * -1;
            y = (y - 50) * -1;

            //double dist = Math.Sqrt( Math.Pow( (x), 2) + Math.Pow( (y), 2) );

            double radians = Math.Atan2(x, y);

            degrees = radians * 180 / Math.PI;

            ROS.Info("x:" + x + " y:" + y + " angle:" + degrees);

            am.ArmMovement movecommand = new am.ArmMovement();

            movecommand.pan_motor_velocity = 1;
            movecommand.tilt_motor_velocity = 1;

            pub.publish(movecommand);


        }

        private void callbackMove(am.ArmMovement msg)
        {

            Dispatcher.BeginInvoke(new Action(() =>
            {
                am.ArmMovement movecommand = new am.ArmMovement();

                double tilt = msg.tilt_motor_position;
                double pan = msg.pan_motor_position;
                double grip = msg.cable_motor_position;

                PanAnim.To = (pan * -90 + 180);
                TiltAnim.To = (tilt * -50);
                PanStory.Begin();
                TiltStory.Begin();

                if ((msg.pan_motor_position * -90 + 180) < degrees)
                    movecommand.pan_motor_velocity = 1;
                else movecommand.pan_motor_velocity = -1;

                pub.publish(movecommand);

            }));

        }

    }
}
