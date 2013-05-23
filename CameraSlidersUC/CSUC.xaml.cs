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
using am = Messages.arm_status_msgs;
using System.Threading;

namespace CameraSlidersUC
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class CSUC : UserControl
    {
        Publisher<m.Int32> pub;
        Subscriber<m.Int32> sub;
        int h = 0;
        int k = 0;
        string[] cameras = new string[] { "/camera1/", "/camera2/", "/camera3/", "/camera4/" };
        string[] infosub = new string[] { "brightness_info", "contrast_info", "exposure_info", "gain_info", "saturation_info", "wbt_info" };
        string[] infopub = new string[] { "brightness", "contrast", "exposure", "gain", "saturation", "wbt" };
        public CSUC()
        {
            InitializeComponent();
        }

        public class CrapRepository
        {
            public CrapRepository(NodeHandle nh, string topic, int _i, int _j)
            {
                sub = nh.subscribe<m.Int32>(topic + "_info", 1000, callback);
                pub = nh.advertise<m.Int32>(topic, 1000);
                i = _i;
                j = _j;
                this.topic = topic;

            }
            public string topic;
            public int i, j;
            public int[][] slider_default = new int[4][] { new int[6], new int[6], new int[6], new int[6] };
            Subscriber<m.Int32> sub;
            Publisher<m.Int32> pub;
            void callback(m.Int32 msg)
            {
                slider_default[i][j] = msg.data;
            }
        }
            NodeHandle node;
            CrapRepository[][] cr;

            //ROS
            private void UserControl_Loaded(object sender, RoutedEventArgs e)
            {
                node = new NodeHandle();
                //string[][] topicsub = new string[4][] { new string[infosub.Length], new string[infosub.Length], new string[infosub.Length], new string[infosub.Length] };


                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 6; j++)
                    {
                        //cameras[i] + infosub[j]
                        //topicsub[i][j] = cameras[i] + infosub[j];
                        //Console.WriteLine(topicsub[i][j]);
                    }
                }
                //sub = node.subscribe<m.Int32>("/camera1/brightness", 1000, callbackMonitor);
                //pub = node.advertise<Int32>("/camera1/brightness", 1000);

                new Thread(() =>
                {
                    while (!ROS.shutting_down)
                    {
                        ROS.spinOnce(node);
                        Thread.Sleep(1);
                    }
                }).Start();
            }

            /*private void callback(m.Int32 msg)
            {

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    //slider_default[i][j] = msg.data;
                }));

            }*/




            //Main Camera 1 Sliders Changes
            private void MC1_Brigh_Sl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            {
                int brightness = (int)MC1_Brigh_Sl.Value;
                MC1_Brigh_Lvl.Content = brightness.ToString();
            }

            private void MC1_Cont_Sl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            {
                int contrast = (int)MC1_Cont_Sl.Value;
                MC1_Cont_Lvl.Content = contrast.ToString();

            }

            private void MC1_Exp_Sl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            {
                if (MC1_Sat_Lvl != null)
                {
                    int exposure = (int)MC1_Exp_Sl.Value;
                    MC1_Exp_Lvl.Content = exposure.ToString();
                }
            }

            private void MC1_Gain_Sl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            {
                int gain = (int)MC1_Gain_Sl.Value;
                MC1_Gain_Lvl.Content = gain.ToString();
            }

            private void MC1_Sat_Sl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            {
                int saturation = (int)MC1_Sat_Sl.Value;
                MC1_Sat_Lvl.Content = saturation.ToString();
            }

            private void MC1_WBT_Sl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            {
                if (MC1_WBT_Lvl != null)
                {
                    int wbt = (int)MC1_WBT_Sl.Value;
                    MC1_WBT_Lvl.Content = wbt.ToString();
                }
            }

            private void MC1_Exp_CB_Checked(object sender, RoutedEventArgs e)
            {
                MC1_Exp_Sl.IsEnabled = false;
                //3
            }

            private void MC1_Exp_CB_Unchecked(object sender, RoutedEventArgs e)
            {
                MC1_Exp_Sl.IsEnabled = true;
                //1
            }

            private void MC1_WBT_CB_Checked(object sender, RoutedEventArgs e)
            {
                MC1_WBT_Sl.IsEnabled = false;
                //1
            }

            private void MC1_WBT_CB_Unchecked(object sender, RoutedEventArgs e)
            {
                MC1_WBT_Sl.IsEnabled = true;
                //0
            }

            private void MC1_Foc_Sl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            {
                int focus = (int)MC1_Foc_Sl.Value;
                MC1_Foc_Lvl.Content = focus * 17;
            }

            private void MC1_Foc_CB_Checked(object sender, RoutedEventArgs e)
            {
                MC1_Foc_Sl.IsEnabled = false;
            }

            private void MC1_Foc_CB_Unchecked(object sender, RoutedEventArgs e)
            {
                MC1_Foc_Sl.IsEnabled = true;
            }
            //END Main Camera 1 Sliders Changes

            //Rear Camera Slider Changes
            private void RC_Brigh_Sl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            {
                int brightness = (int)RC_Brigh_Sl.Value;
                RC_Brigh_Lvl.Content = brightness.ToString();
            }

            private void RC_Cont_Sl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            {
                int contrast = (int)RC_Cont_Sl.Value;
                RC_Cont_Lvl.Content = contrast.ToString();

            }

            private void RC_Exp_Sl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            {
                if (RC_Sat_Lvl != null)
                {
                    int exposure = (int)RC_Exp_Sl.Value;
                    RC_Exp_Lvl.Content = exposure.ToString();
                }
            }

            private void RC_Gain_Sl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            {
                int gain = (int)RC_Gain_Sl.Value;
                RC_Gain_Lvl.Content = gain.ToString();
            }

            private void RC_Sat_Sl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            {
                int saturation = (int)RC_Sat_Sl.Value;
                RC_Sat_Lvl.Content = saturation.ToString();
            }

            private void RC_WBT_Sl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            {
                if (RC_WBT_Lvl != null)
                {
                    int wbt = (int)RC_WBT_Sl.Value;
                    RC_WBT_Lvl.Content = wbt.ToString();
                }
            }

            private void RC_Exp_CB_Checked(object sender, RoutedEventArgs e)
            {
                RC_Exp_Sl.IsEnabled = false;
                //3
            }

            private void RC_Exp_CB_Unchecked(object sender, RoutedEventArgs e)
            {
                RC_Exp_Sl.IsEnabled = true;
                //1
            }

            private void RC_WBT_CB_Checked(object sender, RoutedEventArgs e)
            {
                RC_WBT_Sl.IsEnabled = false;
                //1
            }

            private void RC_WBT_CB_Unchecked(object sender, RoutedEventArgs e)
            {
                RC_WBT_Sl.IsEnabled = true;
                //0
            }

            private void RC_Foc_Sl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            {
                int focus = (int)RC_Foc_Sl.Value;
                RC_Foc_Lvl.Content = focus * 17;
            }

            private void RC_Foc_CB_Checked(object sender, RoutedEventArgs e)
            {
                RC_Foc_Sl.IsEnabled = false;
            }

            private void RC_Foc_CB_Unchecked(object sender, RoutedEventArgs e)
            {
                RC_Foc_Sl.IsEnabled = true;
            }
            //END Rear Camera Slider Changes

            //Main Camera 3 Slider Changes
            private void MC3_Brigh_Sl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            {
                int brightness = (int)MC3_Brigh_Sl.Value;
                MC3_Brigh_Lvl.Content = brightness.ToString();
            }

            private void MC3_Cont_Sl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            {
                int contrast = (int)MC3_Cont_Sl.Value;
                MC3_Cont_Lvl.Content = contrast.ToString();

            }

            private void MC3_Exp_Sl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            {
                if (MC3_Sat_Lvl != null)
                {
                    int exposure = (int)MC3_Exp_Sl.Value;
                    MC3_Exp_Lvl.Content = exposure.ToString();
                }
            }

            private void MC3_Gain_Sl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            {
                int gain = (int)MC3_Gain_Sl.Value;
                MC3_Gain_Lvl.Content = gain.ToString();
            }

            private void MC3_Sat_Sl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            {
                int saturation = (int)MC3_Sat_Sl.Value;
                MC3_Sat_Lvl.Content = saturation.ToString();
            }

            private void MC3_WBT_Sl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            {
                if (MC3_WBT_Lvl != null)
                {
                    int wbt = (int)MC3_WBT_Sl.Value;
                    MC3_WBT_Lvl.Content = wbt.ToString();
                }
            }

            private void MC3_Exp_CB_Checked(object sender, RoutedEventArgs e)
            {
                MC3_Exp_Sl.IsEnabled = false;
                //3
            }

            private void MC3_Exp_CB_Unchecked(object sender, RoutedEventArgs e)
            {
                MC3_Exp_Sl.IsEnabled = true;
                //1
            }

            private void MC3_WBT_CB_Checked(object sender, RoutedEventArgs e)
            {
                MC3_WBT_Sl.IsEnabled = false;
                //1
            }

            private void MC3_WBT_CB_Unchecked(object sender, RoutedEventArgs e)
            {
                MC3_WBT_Sl.IsEnabled = true;
                //0
            }

            private void MC3_Foc_Sl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            {
                int focus = (int)MC3_Foc_Sl.Value;
                MC3_Foc_Lvl.Content = focus * 17;
            }

            private void MC3_Foc_CB_Checked(object sender, RoutedEventArgs e)
            {
                MC3_Foc_Sl.IsEnabled = false;
            }

            private void MC3_Foc_CB_Unchecked(object sender, RoutedEventArgs e)
            {
                MC3_Foc_Sl.IsEnabled = true;
            }
            //END Main Camera 3 Slider Changes

            //Main Camera 4 Slider Changes
            private void MC4_Brigh_Sl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            {
                int brightness = (int)MC4_Brigh_Sl.Value;
                MC4_Brigh_Lvl.Content = brightness.ToString();
            }

            private void MC4_Cont_Sl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            {
                int contrast = (int)MC4_Cont_Sl.Value;
                MC4_Cont_Lvl.Content = contrast.ToString();

            }

            private void MC4_Exp_Sl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            {
                if (MC4_Sat_Lvl != null)
                {
                    int exposure = (int)MC4_Exp_Sl.Value;
                    MC4_Exp_Lvl.Content = exposure.ToString();
                }
            }

            private void MC4_Gain_Sl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            {
                int gain = (int)MC4_Gain_Sl.Value;
                MC4_Gain_Lvl.Content = gain.ToString();
            }

            private void MC4_Sat_Sl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            {
                int saturation = (int)MC4_Sat_Sl.Value;
                MC4_Sat_Lvl.Content = saturation.ToString();
            }

            private void MC4_WBT_Sl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            {
                if (MC4_WBT_Lvl != null)
                {
                    int wbt = (int)MC4_WBT_Sl.Value;
                    MC4_WBT_Lvl.Content = wbt.ToString();
                }
            }

            private void MC4_Exp_CB_Checked(object sender, RoutedEventArgs e)
            {
                MC4_Exp_Sl.IsEnabled = false;
                //3
            }

            private void MC4_Exp_CB_Unchecked(object sender, RoutedEventArgs e)
            {
                MC4_Exp_Sl.IsEnabled = true;
                //1
            }

            private void MC4_WBT_CB_Checked(object sender, RoutedEventArgs e)
            {
                MC4_WBT_Sl.IsEnabled = false;
                //1
            }

            private void MC4_WBT_CB_Unchecked(object sender, RoutedEventArgs e)
            {
                MC4_WBT_Sl.IsEnabled = true;
                //0
            }

            private void MC4_Foc_Sl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
            {
                int focus = (int)MC4_Foc_Sl.Value;
                MC4_Foc_Lvl.Content = focus * 17;
            }

            private void MC4_Foc_CB_Checked(object sender, RoutedEventArgs e)
            {
                MC4_Foc_Sl.IsEnabled = false;
            }

            private void MC4_Foc_CB_Unchecked(object sender, RoutedEventArgs e)
            {
                MC4_Foc_Sl.IsEnabled = true;
            }
            //End Maine 4 Slider Changes
        }
    }
