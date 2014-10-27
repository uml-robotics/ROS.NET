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
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Close();
            }

            topicPoller = new Thread(() =>
            {
                while (ROS.ok)
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        lock (this)
                        {
                            if (!ROS.ok)
                                return;
                            TopicInfo[] topics = new TopicInfo[0];
                            master.getTopics(ref topics);
                            List<string> prevlist = new List<string>(knownConfigurations.Keys);
                            bool changed = false;
                            foreach (TopicInfo ti in topics)
                            {
                                if (ti.data_type == "dynamic_reconfigure/Config")
                                {
                                    string prefix = ti.name.Replace("/parameter_updates", "");
                                    prevlist.Remove(prefix);
                                    if (!knownConfigurations.ContainsKey(prefix))
                                    {
                                        DynamicReconfigurePage drp = new DynamicReconfigurePage(nh, prefix);
                                        TargetBox.Items.Add(prefix);
                                        if (drp != null)
                                            knownConfigurations.Add(prefix, drp);
                                        changed = true;
                                    }
                                }
                            }
                            foreach (string s in prevlist)
                            {
                                foreach (string S in TargetBox.Items)
                                {
                                    if (S == s)
                                    {
                                        changed = true;
                                        TargetBox.Items.Remove(s);
                                        break;
                                    }
                                }
                                knownConfigurations.Remove(s);
                            }
                            if (changed)
                            {
                                string sel = (TargetBox.SelectedItem as string);
                                List<string> keys = knownConfigurations.Keys.ToList();
                                keys.Sort();
                                TargetBox.Items.Clear();
                                string none = "None";
                                TargetBox.Items.Add(none);
                                if (string.Equals(sel, none))
                                    TargetBox.SelectedIndex = TargetBox.Items.Count - 1;
                                foreach (string k in keys)
                                {
                                    TargetBox.Items.Add(k);
                                    if (string.Equals(sel, k))
                                        TargetBox.SelectedIndex = TargetBox.Items.Count - 1;
                                }
                            }
                        }
                    }));
                    if (!ROS.ok)
                        return;
                    Thread.Sleep(1000);
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