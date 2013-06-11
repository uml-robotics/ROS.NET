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
using System.Linq;

namespace MotorBarGraph
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class MotorGraph : UserControl
    {

        Subscriber<m.String>[] telemsub = new Subscriber<String>[2];
        Dictionary<int, Rectangle[]> rects = new Dictionary<int, Rectangle[]>();
        TextBlock[] texts;
        Dictionary<char, string>[] telem = new []{new Dictionary<char, string>(), new Dictionary<char, string>()};

        public MotorGraph()
        {
            InitializeComponent();
        }

        private void cb0(m.String msg)
        {
            cb(0, msg);
        }

        private void cb1(m.String msg)
        {
            cb(1, msg);
        }

        private void cb(int i, m.String msg)
        {
            string[] split = msg.data.Split('=');
            if (split.Length != 2)
            {
                Console.WriteLine("CRAP");
                return;
            }
            if (split[0].Equals("BA"))
            {
                string[] perside = split[1].Split(':');
                float[] vals = new[] { 0f, 0f };
                for (int j = 0; j < perside.Length; j++)
                {
                    float.TryParse(perside[j], out vals[j]);
                    vals[j] /= 10.0f;
                }                
                setSingle(i*2, vals[i]);
                setSingle(i*2+1, vals[1-i]);
            }
            /*StringBuilder sb = new StringBuilder("mc[" + i + "] = \n");
            lock(telem[i])
            {
                if (!telem[i].ContainsKey(split[0][0]))
                    telem[i].Add(split[0][0], split[1]);
                else
                    telem[i][split[0][0]] = split[1];
                List<KeyValuePair<char, string>> sorted = telem[i].ToList();
                sorted.Sort((x,y)=>x.Key.CompareTo(y.Key));
                foreach (KeyValuePair<char, string> kvp in sorted)
                {
                    sb.AppendLine("" + kvp.Key + " = " + kvp.Value);
                }
            }
            Console.WriteLine(sb);*/
        }

        public void startListening(NodeHandle node)
        {
            telemsub[0] = node.subscribe<m.String>("/mc1/telemetry", 1, cb0);
            telemsub[1] = node.subscribe<m.String>("/mc2/telemetry", 1, cb1);

            new Thread(() =>
            {
                while (!ROS.shutting_down)
                {
                    ROS.spinOnce(node);
                    Thread.Sleep(100);
                }
            }).Start();
        }

        private float highestever = 5.0f;

        private void setSingle(int i, float thing)
        {
            float abs = Math.Abs(thing);
            if (abs > highestever)
                highestever = abs;
            float percentage = abs / highestever;
            int s = Math.Sign(thing);
            if (s >= 0) 
                s = 1;
            else
                s = 0;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                rects[s][i].Fill = (abs > 5.0) ? Brushes.Red : Brushes.Yellow;
                rects[1-s][i].Fill = Brushes.Yellow;
                rects[s][i].Height = percentage * container.ActualHeight / 2.0;
                rects[1 - s][i].Height = 0;
                texts[i].Text = "" + thing;
            }));
        }

        public void setBars(float fl, float fr, float bl, float br)
        {
            setSingle(0, fl);
            setSingle(1, fr);
            setSingle(2, bl);
            setSingle(3, br);
        }        

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            rects.Add(1, new Rectangle[] { rectangle1, rectangle2, rectangle3, rectangle4 });
            rects.Add(0, new Rectangle[] { rectangle1neg, rectangle2neg, rectangle3neg, rectangle4neg });
            texts = new[] { textBlock1, textBlock2, textBlock3, textBlock4 };
            setBars(0, 0, 0, 0);
        }
    }
}
