using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System;
using System.IO;
using System.Threading;
using Messages;
using Messages.custom_msgs;
using Ros_CSharp;
using XmlRpc_Wrapper;
using Int32 = Messages.std_msgs.Int32;
using String = Messages.std_msgs.String;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;
using sm = Messages.sensor_msgs;
using System.Collections.ObjectModel;

namespace RosoutDebugUC
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class RosoutDebug : UserControl
    {

        ObservableCollection<rosoutString> rosoutdata = new ObservableCollection<rosoutString>();
        Subscriber<Messages.rosgraph_msgs.Log> sub;
        private NodeHandle nh;
        private Thread waitforinit;

        //right now, these are split from a semicolon-delimited string, and matching is REALLY DUMB... just a containment check.
        private List<string> ignoredStrings = new List<string>();

        public RosoutDebug()
        {
            InitializeComponent();
        }

        public RosoutDebug(NodeHandle n, string IgnoredStrings)
            : this()
        {
            nh = n;
            Loaded += (sender, args) =>
                          {
                              this.IgnoredStrings = IgnoredStrings;
                          };
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {

            if (System.Diagnostics.Process.GetCurrentProcess().ProcessName == "devenv")
                return;
            abraCadabra.ItemsSource = rosoutdata;
            abraCadabra.LoadingRow += (o, a) => {
                                                    rosoutString rs = a.Row.Item as rosoutString;
                                                    a.Row.Background = Brushes.Black;
                                                    a.Row.Foreground = Colorize(rs._level);
            };

            /* THESE LINES CAUSE THE NEWEST MESSAGE TO BE AT THE TOP. THE SORT CONTROLS SHOULD OVERRIDE THIS BEHAVIOR PER-VIEW LIFETIME. TODO: MAKE AN OPT-IN OPTION */
            abraCadabra.Items.SortDescriptions.Clear();
            abraCadabra.Items.SortDescriptions.Add(new SortDescription("stamp", ListSortDirection.Descending));
            abraCadabra.Items.Refresh();
        }

        /// <summary>
        /// A DependencyProperty for setting a list of semicolon-delimited substrings to ignore when found in any rosout msg field
        /// </summary>
        public static readonly DependencyProperty IgnoredStringsProperty = DependencyProperty.Register(
            "IgnoredStrings",
            typeof(string),
            typeof(RosoutDebug),
            new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.None, (obj, args) =>
                {
                    try
                    {
                        if (obj is RosoutDebug)
                        {
                            RosoutDebug target = obj as RosoutDebug;
                            target.IgnoredStrings = (string)args.NewValue;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }));

        /// <summary>
        /// A semicolon-delimited list of substrings that, when found in a concatenation of any rosout msgs fields, will not display that message
        /// </summary>
        public string IgnoredStrings
        {
            get { return GetValue(IgnoredStringsProperty) as string; }
            set
            {
                if (Process.GetCurrentProcess().ProcessName == "devenv")
                    return;
                ignoredStrings.Clear();
                ignoredStrings.AddRange(IgnoredStrings.Split(';'));
                SetValue(IgnoredStringsProperty, value);
                Init();
            }
        }



        public void shutdown()
        {
            if (sub != null)
            {
                sub.shutdown();
                sub = null;
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
                        string workaround = IgnoredStrings;
                        waitforinit = new Thread(() => waitfunc(workaround));
                    }
                    if (!waitforinit.IsAlive)
                    {
                        waitforinit.Start();
                    }
                }
                else
                    SetupIgnore(IgnoredStrings);
            }
        }

        private void waitfunc(string ignored)
        {
            while (!ROS.isStarted())
            {
                Thread.Sleep(100);
            }
            SetupIgnore(ignored);
        }

        private void SetupIgnore(string ignored)
        {
            if (Process.GetCurrentProcess().ProcessName == "devenv")
                return;
            if (nh == null)
                nh = new NodeHandle();
            if (sub == null)
                sub = nh.subscribe<Messages.rosgraph_msgs.Log>("/rosout_agg", 100, callback);
        }

        private void callback(Messages.rosgraph_msgs.Log msg)
        {
            string teststring = string.Format("{0}\n{1}\n{2}\n{3}\n{4}\n{5}", msg.level, msg.msg.data, msg.name.data, msg.file.data, msg.function.data, msg.line);
            if (ignoredStrings.Count > 0 && ignoredStrings.Any(teststring.Contains))
                return;
            rosoutString rss = new rosoutString((1.0 * msg.header.stamp.data.sec + (1.0 * msg.header.stamp.data.nsec) / 1000000000.0),
                msg.level,
                msg.msg.data,
                msg.name.data,
                msg.file.data,
                msg.function.data,
                ""+msg.line);
            Dispatcher.Invoke(new Action(() =>
            {
                rosoutdata.Add(rss);
                if (rosoutdata.Count > 1000)
                    rosoutdata.RemoveAt(0);

                //auto-sticky scrolling IFF the vertical scrollbar is at its maximum or minimum
                if (VisualTreeHelper.GetChildrenCount(abraCadabra) > 0)
                {
                    var border = VisualTreeHelper.GetChild(abraCadabra, 0) as Decorator;
                    if (border != null)
                    {
                        var scroll = border.Child as ScrollViewer;
                        if (scroll != null && !scroll.IsMouseCaptured)
                        {
                            if (scroll.VerticalOffset <= 0 || scroll.VerticalOffset >= scroll.ScrollableHeight - 2)
                                abraCadabra.ScrollIntoView(rss);
                        }
                        else Console.WriteLine("yay");
                    }
                }
            }));

        }

        private Brush Colorize(int level)
        {
            switch (level)
            {
                case 1:
                    return Brushes.Green;
                case 2:
                    return Brushes.White;
                case 4:
                    return Brushes.Yellow;
                case 8:
                    return Brushes.Red;
                case 16:
                    return Brushes.OrangeRed;
                default:
                    return Brushes.White;
            }
        }
    }

    //used for datagrid.  this is the data structure bound to it.
    public class rosoutString
    {

        //converts the int warning value from msg to a meaningful string
        private string ConvertVerbosityLevel(int level)
        {
            switch (level)
            {
                case 1:
                    return "DEBUG";
                case 2:
                    return "INFO";
                case 4:
                    return "WARN";
                case 8:
                    return "ERROR";
                case 16:
                    return "FATAL";
                default:
                    return "" + level;
            }
        }

        public string timestamp { get; set; }
        public string level { get; set; }
        public string msgdata { get; set; }
        public string msgname { get; set; }
        public string filename { get; set; }
        public string functionname { get; set; }
        public string lineno { get; set; }
        public double stamp  { get; set; }
        public int _level = 0;

        public rosoutString(double stamp, int level, string data, string name, string filename, string function, string lineno)
        {
            this.stamp = stamp;
            timestamp = ""+stamp;
            this._level = level;
            this.level = ConvertVerbosityLevel(level);
            msgdata = data;
            msgname = name;
            this.filename = Regex.Replace(filename, "(.*/)|(.*\\\\)", "");
            this.functionname = function;
            this.lineno = lineno;
        }
    }
}