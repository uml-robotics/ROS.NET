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
    public partial class UserControl1 : UserControl
    {

        //Subscriber<> something something something

        public UserControl1()
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
                Textblock1.Text = 280;

                rectangle2.Height = 50;
                Textblock2.Text = 50;

                rectangle3.Height = 100;
                Textblock3.Text = 100;

                rectangle4.Height = 30;
                Textblock4.Text = 30;
            }));

        }
    }
}
