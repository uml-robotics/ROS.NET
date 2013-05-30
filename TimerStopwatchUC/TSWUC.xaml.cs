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
using System.Diagnostics;
using System.Windows.Threading;
using System.IO;

namespace TimerStopwatchUC
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class TSWUC : UserControl
    {
        DispatcherTimer dtimer = new DispatcherTimer();
        Stopwatch sw = new Stopwatch();
        TimeSpan ts = new TimeSpan();
        TimeSpan res = new TimeSpan();
        TimeSpan zero = TimeSpan.Zero;
        string hrs, mins, secs, mils;

        public TSWUC()
        {
            InitializeComponent();
            dtimer.Tick += new EventHandler(dtimer_tick);
            dtimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
        }

        private void StartSW_Click(object sender, RoutedEventArgs e)
        {

            if (StartSW.Content.ToString() == "Start")
            {
                StartSW.Content = "Stop";
                ResetSW.Content = "Reset";
                dtimer.Start();
                sw.Start();
                ResetSW.IsEnabled = false;
            }
            else
            {
                dtimer.Stop();
                sw.Stop();
                StartSW.Content = "Start";
                ResetSW.IsEnabled = true;
            }
        }

        private void ResetSW_Click(object sender, RoutedEventArgs e)
        {
            if (ResetSW.Content.ToString() == "Resume")
            {
                StreamReader sreader = new StreamReader(@"stopwatch.txt");
                hrs = sreader.ReadLine();
                mins = sreader.ReadLine();
                secs = sreader.ReadLine();
                mils = sreader.ReadLine();
                sreader.Close();
                int hr = int.Parse(hrs);
                int min = int.Parse(mins);
                int sec = int.Parse(secs);
                int mil = int.Parse(mils);
                ResetSW.Content = "Reset";
                res = new TimeSpan(0, hr, min, sec, mil);
                ts = res;
                StopwatchTB.Text = ts.ToString();
                sw.Start();
                dtimer.Start();
                ResetSW.IsEnabled = false;
                StartSW.Content = "Pause";
            }
            else
            {
                sw.Reset();
                res = zero;
                StopwatchTB.Text = sw.Elapsed.ToString("mm\\:ss\\.ff");
                ResetSW.IsEnabled = false;
            }
        }

        private void dtimer_tick(object sender, EventArgs e)
        {
            
            ts = sw.Elapsed + res;
            StopwatchTB.Text = ts.ToString();
            StreamWriter swriter = new StreamWriter(@"stopwatch.txt");
            swriter.WriteLine(ts.Hours);
            swriter.WriteLine(ts.Minutes);
            swriter.WriteLine(ts.Seconds);
            swriter.WriteLine(ts.Milliseconds);
            swriter.Close();
        }

    }
}
