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
using System.Windows.Threading;
using System.Threading;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;


namespace TiltSliderUC
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class TSUC : UserControl
    {
        GamePadState state = GamePad.GetState(PlayerIndex.One);
        DispatcherTimer updater;
        public bool rs_pressed = false;
        private Subscriber<m.Int32> sub;
        private Publisher<m.Int32> pub;
        NodeHandle node;
        public TSUC()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            updater = new DispatcherTimer() { Interval = new TimeSpan(0, 0, 0, 0, 100) };
            updater.Tick += Link;
            updater.Start();

            new Thread(() =>
            {
                while (!ROS.initialized)
                {
                    Thread.Sleep(200);
                }
                node = new NodeHandle();

                sub = node.subscribe<m.Int32>("/camera1/tilt_info", 1, callback);
                pub = node.advertise<m.Int32>("/camera1/tilt", 1);
            }).Start();
        }

        public void Link(object sender, EventArgs dontcare)
        {
            if (state.Buttons.RightShoulder == ButtonState.Pressed)
            {
                Tilt_Slider.Value += 3600;
            }
            if (state.Buttons.LeftShoulder == ButtonState.Pressed)
            {
                Tilt_Slider.Value -= 3600;
            }
        }
        private void callback(m.Int32 msg)
        {

            Console.WriteLine("Tilt: " + msg.data.ToString());
        }


       public void Tilt_Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int tilt = (int)Tilt_Slider.Value;
            Tilt_Lvl.Content = tilt.ToString();
            if (pub != null) pub.publish(new Int32 { data = tilt });

        }

    }
}
