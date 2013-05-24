using System;
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
using System.Threading;


namespace TiltSliderUC
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class TSUC : UserControl
    {
        private Publisher<m.Int32> pub;
        public TSUC()
        {
            InitializeComponent();
        }

        public void startListening(NodeHandle node)
        {
            pub = node.advertise<m.Int32>("/tilt", 1000);

            new Thread(() =>
            {
                while (!ROS.shutting_down)
                {
                    ROS.spinOnce(node);
                    Thread.Sleep(100);
                }
            }).Start();

        }

        private void Tilt_Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int tilt = (int)Tilt_Slider.Value;
            Tilt_Lvl.Content = tilt.ToString();

            pub.publish(new Int32() { data = tilt });

        }
    }
}
