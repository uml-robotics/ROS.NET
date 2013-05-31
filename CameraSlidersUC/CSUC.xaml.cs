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

namespace CameraSlidersUC
{
    public class SliderStuff
    {
        private Publisher<m.Int32> pub;
        private Subscriber<m.Int32> sub;
        private Slider _slider;
        private Label _label;
        public int cameraNumber;
        public string topicIn, topicOut;
        private int slider_default = -1;
        private bool inited;
        public SliderStuff(NodeHandle nh, int cameraNumber, string param, Slider s, Label l)
        {
            topicOut="/camera"+cameraNumber+"/"+param;
            topicIn = topicOut + "_info";
            Console.WriteLine("Trying to make a slider for: " + topicOut);
            sub = nh.subscribe<m.Int32>(topicIn, 1, callback);
            pub = nh.advertise<m.Int32>(topicOut, 1);
            this.cameraNumber = cameraNumber;
            _slider = s;
            _label = l;
            s.ValueChanged += sliderChanged;
        }
        void sliderChanged(object Sender, RoutedPropertyChangedEventArgs<double> dub)
        {
            Value = (int)Math.Round(dub.NewValue);
        }
        void callback(m.Int32 msg)
        {
            if (!inited)
            {
                slider_default = msg.data;
                return;
            }
            _slider.Dispatcher.BeginInvoke(new Action(() => { _slider.Value = msg.data; }));
            _label.Dispatcher.BeginInvoke(new Action(() => { _label.Content = "" + msg.data; }));
        }
        public int Value
        {
            get { return (int)Math.Round(_slider.Value); }
            set { if (pub != null) pub.publish(new Int32() { data = value }); }
        }
    }

    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class CSUC : UserControl
    {
        NodeHandle node;
        SliderStuff[][] SUBS;
        Publisher<m.Int32>[] pub_exposureauto;
        Publisher<m.Int32>[] pub_wbtauto;
        Publisher<m.Int32>[] pub_focusauto;
        public CSUC()
        {
            InitializeComponent();
        }

            //ROS
            private void UserControl_Loaded(object sender, RoutedEventArgs e)
            {
                new Thread(() =>
                {
                    while (!ROS.initialized)
                    {
                        Thread.Sleep(200);
                    }
                    node = new NodeHandle();

                    //setup up temporary arrays to make setup clean
                    Slider[][] sliders = new[]
                                         { new []{ MC1_Brigh_Sl, MC1_Cont_Sl, MC1_Exp_Sl, MC1_Gain_Sl, MC1_Sat_Sl, MC1_WBT_Sl, MC1_Foc_Sl}, 
                    new []{ RC_Brigh_Sl, RC_Cont_Sl, RC_Exp_Sl, RC_Gain_Sl, RC_Sat_Sl, RC_WBT_Sl, RC_Foc_Sl }, 
                    new []{ MC3_Brigh_Sl, MC3_Cont_Sl, MC3_Exp_Sl, MC3_Gain_Sl, MC3_Sat_Sl, MC3_WBT_Sl, MC3_Foc_Sl }, 
                    new []{ MC4_Brigh_Sl, MC4_Cont_Sl, MC4_Exp_Sl, MC4_Gain_Sl, MC4_Sat_Sl, MC4_WBT_Sl, MC4_Foc_Sl }};
                    Label[][] labels = new[]
                                       { new[]{ MC1_Brigh_Lvl, MC1_Cont_Lvl, MC1_Exp_Lvl, MC1_Gain_Lvl, MC1_Sat_Lvl, MC1_WBT_Lvl, MC1_Foc_Lvl }, 
                    new[]{ RC_Brigh_Lvl, RC_Cont_Lvl, RC_Exp_Lvl, RC_Gain_Lvl, RC_Sat_Lvl, RC_WBT_Lvl, RC_Foc_Lvl }, 
                    new[]{ MC3_Brigh_Lvl, MC3_Cont_Lvl, MC3_Exp_Lvl, MC3_Gain_Lvl, MC3_Sat_Lvl, MC3_WBT_Lvl, MC3_Foc_Lvl }, 
                    new[]{ MC4_Brigh_Lvl, MC4_Cont_Lvl, MC4_Exp_Lvl, MC4_Gain_Lvl, MC4_Sat_Lvl, MC4_WBT_Lvl, MC4_Foc_Lvl}};
                    string[] info = new[] { "brightness", "contrast", "exposure", "gain", "saturation", "wbt", "focus" };
                    //end setup

                    //setup persistent array of slider stuff for storage
                    SUBS = new[] { new SliderStuff[info.Length], new SliderStuff[info.Length], new SliderStuff[info.Length], new SliderStuff[info.Length] };
                    
                    pub_exposureauto = new Publisher<m.Int32>[4];
                    pub_wbtauto = new Publisher<m.Int32>[4];
                    pub_focusauto = new Publisher<m.Int32>[4];

                    for (int i = 0; i < sliders.Length; i++)
                    {
                        pub_exposureauto[i] = node.advertise<m.Int32>("camera" + i + "/exposureauto", 1);
                        pub_wbtauto[i] = node.advertise<m.Int32>("camera" + i + "/wbtauto", 1);
                        pub_focusauto[i] = node.advertise<m.Int32>("camera" + i + "/focusauto", 1);

                        for (int j = 0; j < sliders[i].Length; j++)
                        {
                            SUBS[i][j] = new SliderStuff(node, i, info[j],sliders[i][j], labels[i][j]);
                        }
                    }
                    while (!ROS.shutting_down)
                    {
                        ROS.spinOnce(node);
                        Thread.Sleep(100);
                    }
                }).Start();
            }

            private void MC1_Exp_CB_Checked(object sender, RoutedEventArgs e)
            {
                MC1_Exp_Sl.IsEnabled = false;
                //3
                pub_exposureauto[MainCameraSliderTabControl.SelectedIndex].publish(new Int32() { data = 3 });
            }

            private void MC1_Exp_CB_Unchecked(object sender, RoutedEventArgs e)
            {
                MC1_Exp_Sl.IsEnabled = true;
                //1
                pub_exposureauto[MainCameraSliderTabControl.SelectedIndex].publish(new Int32() { data = 1 });
            }

            private void MC1_WBT_CB_Checked(object sender, RoutedEventArgs e)
            {
                MC1_WBT_Sl.IsEnabled = false;
                //1
                pub_wbtauto[MainCameraSliderTabControl.SelectedIndex].publish(new Int32() { data = 1 });
            }

            private void MC1_WBT_CB_Unchecked(object sender, RoutedEventArgs e)
            {
                MC1_WBT_Sl.IsEnabled = true;
                //0
                pub_wbtauto[MainCameraSliderTabControl.SelectedIndex].publish(new Int32() { data = 0 });
            }

            private void MC1_Foc_CB_Checked(object sender, RoutedEventArgs e)
            {
                MC1_Foc_Sl.IsEnabled = false;
                pub_focusauto[MainCameraSliderTabControl.SelectedIndex].publish(new Int32() { data = 1 });
            }

            private void MC1_Foc_CB_Unchecked(object sender, RoutedEventArgs e)
            {
                MC1_Foc_Sl.IsEnabled = true;
                pub_focusauto[MainCameraSliderTabControl.SelectedIndex].publish(new Int32() { data = 0 });
            }
            //END Main Camera 1 Sliders Changes

            //Rear Camera Slider Changes
            private void RC_Exp_CB_Checked(object sender, RoutedEventArgs e)
            {
                RC_Exp_Sl.IsEnabled = false;
                //3
                pub_exposureauto[MainCameraSliderTabControl.SelectedIndex].publish(new Int32() { data = 3 });
            }

            private void RC_Exp_CB_Unchecked(object sender, RoutedEventArgs e)
            {
                RC_Exp_Sl.IsEnabled = true;
                //1
                pub_exposureauto[MainCameraSliderTabControl.SelectedIndex].publish(new Int32() { data = 1 });
            }

            private void RC_WBT_CB_Checked(object sender, RoutedEventArgs e)
            {
                RC_WBT_Sl.IsEnabled = false;
                //1
                pub_wbtauto[MainCameraSliderTabControl.SelectedIndex].publish(new Int32() { data = 1 });
            }

            private void RC_WBT_CB_Unchecked(object sender, RoutedEventArgs e)
            {
                RC_WBT_Sl.IsEnabled = true;
                //0
                pub_wbtauto[MainCameraSliderTabControl.SelectedIndex].publish(new Int32() { data = 0 });
            }

            private void RC_Foc_CB_Checked(object sender, RoutedEventArgs e)
            {
                RC_Foc_Sl.IsEnabled = false;
                pub_focusauto[MainCameraSliderTabControl.SelectedIndex].publish(new Int32() { data = 1 });
            }

            private void RC_Foc_CB_Unchecked(object sender, RoutedEventArgs e)
            {
                RC_Foc_Sl.IsEnabled = true;
                pub_focusauto[MainCameraSliderTabControl.SelectedIndex].publish(new Int32() { data = 0 });
            }
            //END Rear Camera Slider Changes

            //Main Camera 3 Slider Changes
            private void MC3_Exp_CB_Checked(object sender, RoutedEventArgs e)
            {
                MC3_Exp_Sl.IsEnabled = false;
                //3
                pub_exposureauto[MainCameraSliderTabControl.SelectedIndex].publish(new Int32() { data = 3 });
            }

            private void MC3_Exp_CB_Unchecked(object sender, RoutedEventArgs e)
            {
                MC3_Exp_Sl.IsEnabled = true;
                //1
                pub_exposureauto[MainCameraSliderTabControl.SelectedIndex].publish(new Int32() { data = 1 });
            }

            private void MC3_WBT_CB_Checked(object sender, RoutedEventArgs e)
            {
                MC3_WBT_Sl.IsEnabled = false;
                //1
                pub_wbtauto[MainCameraSliderTabControl.SelectedIndex].publish(new Int32() { data = 1 });
            }

            private void MC3_WBT_CB_Unchecked(object sender, RoutedEventArgs e)
            {
                MC3_WBT_Sl.IsEnabled = true;
                //0
                pub_wbtauto[MainCameraSliderTabControl.SelectedIndex].publish(new Int32() { data = 0 });
            }

            private void MC3_Foc_CB_Checked(object sender, RoutedEventArgs e)
            {
                MC3_Foc_Sl.IsEnabled = false;
                pub_focusauto[MainCameraSliderTabControl.SelectedIndex].publish(new Int32() { data = 1 });
            }

            private void MC3_Foc_CB_Unchecked(object sender, RoutedEventArgs e)
            {
                MC3_Foc_Sl.IsEnabled = true;
                pub_focusauto[MainCameraSliderTabControl.SelectedIndex].publish(new Int32() { data = 0 });
            }
            //END Main Camera 3 Slider Changes

            //Main Camera 4 Slider Changes
            private void MC4_Exp_CB_Checked(object sender, RoutedEventArgs e)
            {
                MC4_Exp_Sl.IsEnabled = false;
                //3
                pub_exposureauto[MainCameraSliderTabControl.SelectedIndex].publish(new Int32() { data = 3 });
            }

            private void MC4_Exp_CB_Unchecked(object sender, RoutedEventArgs e)
            {
                MC4_Exp_Sl.IsEnabled = true;
                //1
                pub_exposureauto[MainCameraSliderTabControl.SelectedIndex].publish(new Int32() { data = 1 });
            }

            private void MC4_WBT_CB_Checked(object sender, RoutedEventArgs e)
            {
                MC4_WBT_Sl.IsEnabled = false;
                //1
                pub_wbtauto[MainCameraSliderTabControl.SelectedIndex].publish(new Int32() { data = 1 });
            }

            private void MC4_WBT_CB_Unchecked(object sender, RoutedEventArgs e)
            {
                MC4_WBT_Sl.IsEnabled = true;
                //0
                pub_wbtauto[MainCameraSliderTabControl.SelectedIndex].publish(new Int32() { data = 0 });
            }


            private void MC4_Foc_CB_Checked(object sender, RoutedEventArgs e)
            {
                MC4_Foc_Sl.IsEnabled = false;
                pub_focusauto[MainCameraSliderTabControl.SelectedIndex].publish(new Int32() { data = 1 });
            }

            private void MC4_Foc_CB_Unchecked(object sender, RoutedEventArgs e)
            {
                MC4_Foc_Sl.IsEnabled = true;
                pub_focusauto[MainCameraSliderTabControl.SelectedIndex].publish(new Int32() { data = 0 });
            }
            //End Maine 4 Slider Changed
            private void MainTab_SelectionChanged(object sender, RoutedEventArgs e)
            {
            }
        }
    }
