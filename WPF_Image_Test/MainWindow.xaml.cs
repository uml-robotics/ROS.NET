using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
using Messages.roscpp_tutorials;
using Ros_CSharp;
using XmlRpc_Wrapper;
using Int32 = Messages.std_msgs.Int32;
using String = Messages.std_msgs.String;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;
using sm = Messages.sensor_msgs;

namespace WPF_Image_Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ROS.ROS_MASTER_URI = "http://10.0.2.18:11311";
            ROS.ROS_HOSTNAME = "10.0.2.69";
            ROS.Init(new string[0], "ImageTest");
            NodeHandle handle = new NodeHandle();
            ServiceClient<TypedMessage<Messages.roscpp_tutorials.TwoInts.Request>, TypedMessage<Messages.roscpp_tutorials.TwoInts.Response>>
                testclient = handle.serviceClient<TypedMessage<Messages.roscpp_tutorials.TwoInts.Request>, TypedMessage<Messages.roscpp_tutorials.TwoInts.Response>>("add_two_ints");
            TwoInts.Request tin = new TwoInts.Request();
            TwoInts.Response tout = new TwoInts.Response();
            tin.a = 6;
            tin.b = 9;
            TypedMessage<TwoInts.Response> tGENERICUGLINESSZOMG = new TypedMessage<TwoInts.Response>(tout);
            testclient.call(new TypedMessage<TwoInts.Request>(tin), ref tGENERICUGLINESSZOMG, "*");
            Console.WriteLine(tout.sum);
        }
    }
}
