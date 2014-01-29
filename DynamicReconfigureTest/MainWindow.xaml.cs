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
using DynamicReconfigure;
using Messages.dynamic_reconfigure;
using Ros_CSharp;

namespace DynamicReconfigureTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DynamicReconfigureInterface dynamic;
        private NodeHandle nh;
        private Subscriber<GroupState> test;

        public MainWindow()
        {
            ROS.Init(new string[0], "dynamic_reconfigure_sharp_" + Environment.MachineName);
            nh = new NodeHandle();
            InitializeComponent();

            test = nh.subscribe<GroupState>("/bool", 1, (t) => {
                                                                                          Console.WriteLine("GOT ONE!");
            });
            ROS.Error("FUCK");

            /*dynamic = new DynamicReconfigureInterface(nh, "/camera/driver", 0,
                (c) =>
                {
                    Console.WriteLine("CONFIG CHANGED");
                },
                (d) =>
                {
                    Console.WriteLine("DESC CHANGED");
                });
            dynamic.SubscribeForUpdates();*/

        }

        private void _checkBox_OnChecked(object sender, RoutedEventArgs e)
        {
            dynamic.Set("depth_registration", true);
        }

        private void _checkBox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            dynamic.Set("depth_registration", false);
        }
    }
}
