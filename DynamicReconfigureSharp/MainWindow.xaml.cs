#region USINGZ

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using DynamicReconfigureSharp;
using Ros_CSharp;
using XmlRpc_Wrapper;

#endregion

namespace DynamicReconfigureTest
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SortedDictionary<string, DynamicReconfigurePage> knownConfigurations = new SortedDictionary<string, DynamicReconfigurePage>(); 
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
                System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Critical;
                TargetBox.ItemsSource = knownConfigurations.Keys;
                knownConfigurations.Add("-", null);
                TargetBox.SelectedIndex = 0;
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
                    
                    TopicInfo[] topics = new TopicInfo[0];
                    master.getTopics(ref topics);
                    string[] nodes = new string[0];
                    master.getNodes(ref nodes);
                    List<string> prevlist = new List<string>(knownConfigurations.Keys);
                    List<string> additions = new List<string>();
                    foreach (TopicInfo ti in topics)
                    {
                        if (ti.data_type == "dynamic_reconfigure/Config")
                        {
                            string prefix = ti.name.Replace("/parameter_updates", "");
                            if (!knownConfigurations.ContainsKey(prefix))
                                additions.Add(prefix);
                            else
                                prevlist.Remove(prefix);
                        }
                    }
                    lock (this)
                    {
                        if (!ROS.ok || ROS.shutting_down)
                            return;
                        foreach (string prefix in additions)
                        {
                            string pfx = prefix;
                            Dispatcher.Invoke(new Action(() =>
                                                             {
                                                                 DynamicReconfigurePage drp = new DynamicReconfigurePage(nh, pfx);
                                                                 if (drp != null)
                                                                 {
                                                                     knownConfigurations.Add(pfx, drp);
                                                                     TargetBox.Items.Refresh();
                                                                 }
                                                             }), new TimeSpan(0,0,0,1));
                        }
                    }
                    foreach (string s in prevlist)
                    {
                        if (!s.Equals("-"))
                        {
                            string pfx = s;
                            Dispatcher.Invoke(new Action(() =>
                                                                {
                                                                    if (TargetBox.SelectedItem != null && ((string) TargetBox.SelectedItem).Equals(pfx))
                                                                    {
                                                                        TargetBox.SelectedIndex = 0;
                                                                    }
                                                                    lock (this)
                                                                    {
                                                                        knownConfigurations.Remove(pfx);
                                                                        TargetBox.Items.Refresh();
                                                                    }
                                                                }), new TimeSpan(0, 0, 0, 1));
                        }
                    }
                    Thread.Sleep(500);
                }
            });
            topicPoller.Start();
        }

        //This freezes if the window closes and the topicPoller is waiting to
        //acquire the dispatcher. The code was moved to the "Window_Closing" method.
        //This is not the only case, I added a timeout to the threadPoller dispatcher
        //protected override void OnClosed(EventArgs e)
        //{
        //    ROS.shutdown();
        //    ROS.waitForShutdown();
        //    topicPoller.Join();
        //    base.OnClosed(e);
        //}

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
                    if (knownConfigurations.ContainsKey(newprefix) && !newprefix.Equals("-"))
                        PageContainer.Children.Add(knownConfigurations[newprefix]);
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ROS.shutdown();
            ROS.waitForShutdown();
            topicPoller.Join();
        }
    }
}