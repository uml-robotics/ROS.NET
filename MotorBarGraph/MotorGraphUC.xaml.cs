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
using System.Threading;
using Messages;
using Messages.custom_msgs;
using Ros_CSharp;
using XmlRpc_Wrapper;
using Int32 = Messages.std_msgs.Int32;
using String = Messages.std_msgs.String;
using m = Messages.std_msgs;

namespace MotorBarGraph
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class MotorGraph : UserControl
    {

        //Subscriber<> something something something

        public MotorGraph()
        {
            InitializeComponent();
        }

        public void startListening(NodeHandle node)
        {

            //sub = something something something
            new Thread(() =>
            {
                while (!ROS.shutting_down)
                {
                    ROS.spinOnce(node);
                    Thread.Sleep(100);
                }
            }).Start();

        }

        public void callbackMotorMonitor(m.Float32 msg)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                rectangle1.Height = 280;//place msg.value here.  duh
                textBlock1.Text = 280 + "v";

                rectangle2.Height = 50;
                textBlock2.Text = 50 + "v";

                rectangle3.Height = 100;
                textBlock3.Text = 100 + "v";

                rectangle4.Height = 30;
                textBlock4.Text = 30 + "v";
            }));

        }
    }
}
