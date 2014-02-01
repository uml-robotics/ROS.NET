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
using DynamicReconfigure;
using Messages.dynamic_reconfigure;
using Ros_CSharp;

namespace DynamicReconfigureSharp
{
    /// <summary>
    /// Interaction logic for DynamicReconfigurePage.xaml
    /// </summary>
    public partial class DynamicReconfigurePage : UserControl
    {
        private NodeHandle nh = null;
        private DynamicReconfigureInterface dynamic;
        private Config def, max, min;
        private string name;

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
