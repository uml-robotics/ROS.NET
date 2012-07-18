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
using System.Windows.Presentation;
namespace DREAMPioneer
{
    /// <summary>
    /// Interaction logic for GoalDot.xaml
    /// </summary>
    public partial class GoalDot : UserControl
    {

        private Point _Location;
        private Canvas mycanv;
        private Canvas Maincanv;
        private ScaleTransform Zoom;
        private TranslateTransform Translation;
        private bool _BeenHere = false;
        private bool _NextOne = false;



        public static Dictionary<int, Brush> ColorInUse = new Dictionary<int, Brush>()
             {
             {-1,Brushes.DodgerBlue}, {-2,Brushes.Red}, {-3,Brushes.YellowGreen}, {-4,Brushes.Orange},
             {-5,Brushes.White}, {-6,Brushes.DeepPink}, {-7,Brushes.Fuchsia}, {-8,Brushes.LightSeaGreen}
             };

        public GoalDot(Canvas WPC, Point loc, double DPI, Canvas MainCanvas, ScaleTransform z, TranslateTransform t, Brush b)
        {
            InitializeComponent();

            mycanv = WPC;
            mycanv.Children.Add(this);
            Zoom = z;
            Translation = t;
            Maincanv = MainCanvas;
            Zoom = z;
            NextC1.Fill = b;
            BeenThereC2.Fill = b;
            Location = loc;




        }

        public bool BeenHere
        {
            get { return _BeenHere; }
            set
            {
                _BeenHere = value;
                if (value)
                {
                    BeenThereC1.Visibility = Visibility.Visible;
                    BeenThereC2.Visibility = Visibility.Visible;
                    NextC1.Visibility = Visibility.Hidden;
                    NextC2.Visibility = Visibility.Hidden;

                }
                else
                {
                    BeenThereC1.Visibility = Visibility.Hidden;
                    BeenThereC2.Visibility = Visibility.Hidden;
                    NextC1.Visibility = Visibility.Visible;
                    NextC2.Visibility = Visibility.Hidden;

                }

            }
        }
        public bool NextOne
        {
            get { return _NextOne; }
            set
            {
                _NextOne = value;
                if (_NextOne)
                    NextC2.Visibility = Visibility.Visible;
                else
                {
                    NextC2.Visibility = Visibility.Hidden;
                }
            }

        }

        public Point Location
        {
            get { return _Location; }
            set
            {
                _Location = ToWayPointCanvas(value);
                Canvas.SetTop(this, Location.Y);
                Canvas.SetLeft(this, Location.X);

            }
        }


        public Point ToWayPointCanvas(Point p)
        {
            Point ret = new Point((p.X - Maincanv.ActualWidth / 2 + mycanv.Width * Zoom.ScaleX / 2) / Zoom.ScaleX - Translation.X - this.Width / 2,
                                       (p.Y - Maincanv.ActualHeight / 2 + mycanv.Height * Zoom.ScaleY / 2) / Zoom.ScaleY - Translation.Y - this.Height / 2);
            return ret;
        }


    }
}