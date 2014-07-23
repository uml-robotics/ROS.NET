#region USINGZ

using System;
using System.Collections.Generic;
using System.Windows.Controls;
using DynamicReconfigure;
using Messages.dynamic_reconfigure;
using Ros_CSharp;

#endregion

namespace DynamicReconfigureSharp
{
    /// <summary>
    ///     Interaction logic for DynamicReconfigurePage.xaml
    /// </summary>
    public partial class DynamicReconfigurePage : UserControl
    {
        private Config def;
        private DynamicReconfigureInterface dynamic;
        private Config max, min;
        private string name;
        private NodeHandle nh;

        public DynamicReconfigurePage()
        {
            InitializeComponent();
        }

        public DynamicReconfigurePage(NodeHandle n, string name) : this()
        {
            nh = n;
            this.name = name + ":";
            Loaded += (sender, args) =>
            {
                dynamic = new DynamicReconfigureInterface(nh, name);
                dynamic.SubscribeForUpdates();
                dynamic.DescribeParameters(DescriptionRecieved);
            };
        }

        private void DescriptionRecieved(ConfigDescription cd)
        {
            def = cd.dflt;
            max = cd.max;
            min = cd.min;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                SortedList<int, DynamicReconfigureGroup> hierarchy = new SortedList<int, DynamicReconfigureGroup>();
                foreach (Group g in cd.groups)
                {
                    DynamicReconfigureGroup drg = new DynamicReconfigureGroup(g, def, min, max, name, dynamic);
                    hierarchy.Add(drg.id, drg);
                }
                GroupHolder.Children.Add(hierarchy[0]);
            }));
        }
    }
}