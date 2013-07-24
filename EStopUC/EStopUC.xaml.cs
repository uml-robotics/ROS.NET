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
using System.IO;
using System.Threading;
using Messages;
using Messages.custom_msgs;
using Ros_CSharp;
using XmlRpc_Wrapper;
using Int32 = Messages.std_msgs.Int32;
using String = Messages.std_msgs.String;
using m = Messages.std_msgs;

namespace EStopUC
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class UserControl1 : UserControl
    {

        private Subscriber<m.Bool> sub;
        private Publisher<m.Bool> pub;
        private Boolean state ;

        public UserControl1()
        {
            InitializeComponent();
        }

        public void startListening(NodeHandle node)
        {

            sub = node.subscribe<m.Bool>("/estopState", 1000, callbackEStop);
            pub = node.advertise<m.Bool>("/setEstop", 1000);

        }

        public void setMode(Boolean b)
        {
            state = b;
            if (b)
            {
                EStopCircle.Fill = Brushes.Red;
                EStopText.Text = "ON";
            }
            else
            {
                EStopCircle.Fill = Brushes.Green;
                EStopText.Text = "OFF";
            }
        }

        private void callbackEStop(m.Bool msg)
        {

            state = msg.data;

            Dispatcher.Invoke(new Action(() =>
            {
                if (msg.data == false)
                {
                    EStopCircle.Fill = Brushes.Green;
                    EStopText.Text = "OFF";
                }
                else
                {
                    EStopCircle.Fill = Brushes.Red;
                    EStopText.Text = "ON";
                }
            }));


        }

        private void EStopCircle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            setMode(!state);
            pub.publish(new m.Bool() { data = !state });
        }

    }
}
