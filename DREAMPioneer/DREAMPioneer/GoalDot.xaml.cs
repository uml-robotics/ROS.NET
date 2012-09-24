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
//using System.Windows.Presentation;
using window = DREAMPioneer.SurfaceWindow1;

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
        
        private bool _BeenHere = false;
        private bool _NextOne = false;
               

        public GoalDot(Canvas WPC, Point loc, double DPI, Canvas MainCanvas, Brush b,bool IsThisMine = true)
        {
            InitializeComponent();

            mycanv = WPC;
            mycanv.Children.Add(this);
           
            Maincanv = MainCanvas;
            
            NextC1.Fill = b;
            BeenThereC2.Fill = b;
            if (!IsThisMine)
                me.Opacity = .5;
            Location = loc;






        }

        //public GoalDot(Waypoint wp)
        //{
        //    InitializeComponent();

        //    mycanv = wp.mycanv;
        //    mycanv.Children.Add(this);
        //    Zoom = wp.Zoom;
        //    Translation = wp.Translation;
        //    Maincanv = wp.Maincanv;
        //    NextC1.Fill= wp.dot.Fill;
        //    BeenThereC2.Fill = wp.dot.Fill;
        //    _Location = wp.Location;
        //    Canvas.SetTop(this, Location.Y);
        //    Canvas.SetLeft(this, Location.X);




        //}

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
                _Location = value; //MAY NEED THIS FIX MAYBE NOT SurfaceWindow1.current.MainCanvas.TranslatePoint(value, mycanv);
                Canvas.SetTop(this, Location.Y - this.Height/2);
                Canvas.SetLeft(this, Location.X - this.Width/2);

            }
        }
                
    }
}