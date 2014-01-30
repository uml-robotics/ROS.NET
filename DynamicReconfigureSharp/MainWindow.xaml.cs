using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using DynamicReconfigureSharp;
using Messages.dynamic_reconfigure;
using Ros_CSharp;

namespace DynamicReconfigureTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NodeHandle nh;
        private Thread topicPoller;
        private Dictionary<string, DynamicReconfigurePage> knownConfigurations = new Dictionary<string, DynamicReconfigurePage>();

        protected override void OnClosed(EventArgs e)
        {
            ROS.shutdown();
            topicPoller.Join();
            base.OnClosed(e);
        }
        public MainWindow()
        {
            ROS.Init(new string[0], "dynamic_reconfigure_sharp_" + Environment.MachineName);
            nh = new NodeHandle();
            InitializeComponent();

            topicPoller = new Thread(() =>
            {
                while (ROS.ok)
                {
                    TopicInfo[] topics = new TopicInfo[0];
                    master.getTopics(ref topics);
                    foreach (TopicInfo ti in topics)
                    {
                        if (ti.data_type == "dynamic_reconfigure/Config")
                        {
                            string prefix = ti.name.Replace("/parameter_updates", "");
                            if (!knownConfigurations.ContainsKey(prefix))
                            {
                                DynamicReconfigurePage drp = null;
                                Dispatcher.Invoke(new Action(() =>
                                {
                                    drp = new DynamicReconfigurePage(nh, prefix);
                                    TargetBox.Items.Add(new ComboBoxItem {Content = prefix});
                                }));
                                if (drp != null)
                                    knownConfigurations.Add(prefix, drp);
                            }
                        }
                    }
                    Thread.Sleep(1000);
                }
            });
            topicPoller.Start();
        }

        private void TargetBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            IEnumerable<ComboBoxItem> prevs = e.RemovedItems.OfType<ComboBoxItem>();
            IEnumerable<ComboBoxItem> news = e.AddedItems.OfType<ComboBoxItem>();
            if (prevs.Count() != 1 || news.Count() != 1) return;
            PageContainer.Children.Clear();
            string newprefix = news.ElementAt(0).Content as string;
            if (newprefix != null)
            {
                if (knownConfigurations.ContainsKey(newprefix))
                    PageContainer.Children.Add(knownConfigurations[newprefix]);
            }
        }
    }
}
