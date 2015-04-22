using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
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

        public RosoutDebug()
        {
            InitializeComponent();
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

        public void startListening(NodeHandle nh)
        {
            sub = nh.subscribe<Messages.rosgraph_msgs.Log>("/rosout_agg", 100, callback);
        }

        private void callback(Messages.rosgraph_msgs.Log msg)
        {
            rosoutString rss = new rosoutString((1.0 * msg.header.stamp.data.sec + (1.0 * msg.header.stamp.data.nsec) / 1000000000.0),
                msg.level,
                msg.msg.data,
                msg.name.data,
                msg.file.data,
                msg.function.data,
                ""+msg.line);
            Dispatcher.BeginInvoke(new Action(() =>
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