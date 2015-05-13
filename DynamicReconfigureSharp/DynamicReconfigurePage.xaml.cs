#region USINGZ

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows;
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
        private NodeHandle nh;
        private Thread waitforinit;

        public DynamicReconfigureInterface ParameterInterface
        {
            get { return dynamic; }
        }

        public DynamicReconfigurePage()
        {
            InitializeComponent();
        }

        public DynamicReconfigurePage(NodeHandle n, string name) : this()
        {
            nh = n;
            Loaded += (sender, args) =>
            {
                Namespace = name;
            };
        }

        private void DescriptionRecieved(ConfigDescription cd)
        {
            def = cd.dflt;
            max = cd.max;
            min = cd.min;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                GroupHolder.Children.Clear();
                SortedList<int, DynamicReconfigureGroup> hierarchy = new SortedList<int, DynamicReconfigureGroup>();
                foreach (Group g in cd.groups)
                {
                    DynamicReconfigureGroup drg = new DynamicReconfigureGroup(g, def, min, max, Namespace, dynamic);
                    hierarchy.Add(drg.id, drg);
                    drg.BoolChanged += (a, v) => { if (BoolChanged != null) BoolChanged(a, v); };
                    drg.IntChanged += (a, v) => { if (IntChanged != null) IntChanged(a, v); };
                    drg.StringChanged += (a, v) => { if (StringChanged != null) StringChanged(a, v); };
                    drg.DoubleChanged += (a, v) => { if (BoolChanged != null) DoubleChanged(a, v); };
                }
                GroupHolder.Children.Add(hierarchy[0]);
            }));
        }

#region xaml markup property interface for setting dynparam namespace ("/amcl" sets dynparams of a dynparam server running in the node named /amcl)

        public static readonly DependencyProperty NamespaceProperty = DependencyProperty.Register(
            "Namespace",
            typeof(string),
            typeof(DynamicReconfigurePage),
            new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.None, (obj, args) =>
                {
                    try
                    {
                        if (obj is DynamicReconfigurePage)
                        {
                            DynamicReconfigurePage target = obj as DynamicReconfigurePage;
                            target.Namespace = (string)args.NewValue;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }));

        public string Namespace
        {
            get { return GetValue(NamespaceProperty) as string; }
            set
            {
                if (Process.GetCurrentProcess().ProcessName == "devenv")
                    return;
                Console.WriteLine("CHANGING DYNPARAM PAGE NAMESPACE FROM " + Namespace + " to " + value);
                SetValue(NamespaceProperty, value);
                Init();
            }
        }

        public void shutdown()
        {
            if (dynamic != null)
            {
                dynamic = null;
            }
            if (nh != null)
            {
                nh.shutdown();
                nh = null;
            }
        }

        private void Init()
        {
            lock (this)
            {
                if (!ROS.isStarted())
                {
                    if (waitforinit == null)
                    {
                        string workaround = Namespace;
                        waitforinit = new Thread(() => waitfunc(workaround));
                    }
                    if (!waitforinit.IsAlive)
                    {
                        waitforinit.Start();
                    }
                }
                else
                    SetupNamespace(Namespace);
            }
        }

        private void waitfunc(string Namespace)
        {
            while (!ROS.isStarted())
            {
                Thread.Sleep(100);
            }
            SetupNamespace(Namespace);
        }

        private void SetupNamespace(string Namespace)
        {
            if (Process.GetCurrentProcess().ProcessName == "devenv")
                return;
            if (nh == null)
                nh = new NodeHandle();
            if (dynamic != null && dynamic.Namespace != Namespace)
            {
                dynamic = null;
            }
            if (dynamic == null)
            {
                dynamic = new DynamicReconfigureInterface(nh, Namespace);
                dynamic.SubscribeForUpdates();
                dynamic.DescribeParameters(DescriptionRecieved);
            }
        }

        public event Action<string, bool> BoolChanged;
        public event Action<string, string> StringChanged;
        public event Action<string, int> IntChanged;
        public event Action<string, double> DoubleChanged;
#endregion
    }
}