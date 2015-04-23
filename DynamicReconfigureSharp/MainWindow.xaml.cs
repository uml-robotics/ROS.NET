#region USINGZ

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using DynamicReconfigureSharp;
using Ros_CSharp;

#endregion

namespace DynamicReconfigureTest
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Dictionary<string, DynamicReconfigurePage> knownConfigurations = new Dictionary<string, DynamicReconfigurePage>();
        private NodeHandle nh;
        private Thread topicPoller;

        public MainWindow()
        {
            new Thread(() =>
            {
                ROS.Init(new string[0], "dynamic_reconfigure_sharp_" + Environment.MachineName);
                nh = new NodeHandle();
                Dispatcher.Invoke(new Action(() => { ConnecitonLabel.Content = "Connected"; }));
            }).Start();
            try
            {
                InitializeComponent();
                TargetBox.Items.SortDescriptions.Clear();
                TargetBox.Items.SortDescriptions.Add(new System.ComponentModel.SortDescription("", System.ComponentModel.ListSortDirection.Ascending));
                TargetBox.ItemsSource = knownConfigurations.Keys;
                knownConfigurations.Add("-", null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Close();
            }

            topicPoller = new Thread(() =>
            {
                while (ROS.ok && !ROS.shutting_down)
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        TopicInfo[] topics = new TopicInfo[0];
                        master.getTopics(ref topics);
                        List<string> prevlist = new List<string>(knownConfigurations.Keys);
                        List<string> additions = new List<string>();
                        foreach (TopicInfo ti in topics)
                        {
                            if (ti.data_type == "dynamic_reconfigure/Config")
                            {
                                string prefix = ti.name.Replace("/parameter_updates", "");
                                prevlist.Remove(prefix);
                                if (!knownConfigurations.ContainsKey(prefix))
                                {
                                    additions.Add(prefix);
                                }
                            }
                        }
                        lock (this)
                        {
                            if (!ROS.ok || ROS.shutting_down)
                                return;
                            foreach (string prefix in additions)
                            {
                                DynamicReconfigurePage drp = new DynamicReconfigurePage(nh, prefix);
                                if (drp != null)
                                    knownConfigurations.Add(prefix, drp);
                            }
                            foreach (string s in prevlist)
                            {
                                if (!knownConfigurations.ContainsKey(s))
                                {
                                    knownConfigurations.Remove(s);
                                }
                            }
                        }
                    }));
                    Thread.Sleep(500);
                }
            });
            topicPoller.Start();
        }

        protected override void OnClosed(EventArgs e)
        {
            ROS.shutdown();
            topicPoller.Join();
            base.OnClosed(e);
        }

        private void TargetBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            lock (this)
            {
                IEnumerable<string> prevs = e.RemovedItems.OfType<string>();
                IEnumerable<string> news = e.AddedItems.OfType<string>();
                if (prevs.Count() != 1 || news.Count() != 1) return;
                PageContainer.Children.Clear();
                string newprefix = news.ElementAt(0);
                if (newprefix != null)
                {
                    if (knownConfigurations.ContainsKey(newprefix))
                        PageContainer.Children.Add(knownConfigurations[newprefix]);
                }
            }
        }
    }
}