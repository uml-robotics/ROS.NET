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
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Surface;
using Microsoft.Surface.Presentation;
using Microsoft.Surface.Presentation.Controls;

namespace DREAMPioneer
{
    class Waypoint
    {
        public Ellipse dot;
        private Point _Location;
        private Canvas mycanv;
        private Canvas Maincanv;
        private ScaleTransform Zoom;
        private TranslateTransform Translation;
        private static List<Point> _PointLocations = new List<Point>();


        public static List<Point> PointLocations
        {
            get
            {
                lock (_PointLocations)
                {

                    return _PointLocations;

                }
            }
            set { _PointLocations = value; }

        }


        public Waypoint(Canvas WPC, Point loc, double DPI, Canvas MainCanvas, ScaleTransform z, TranslateTransform t, Brush b)
        {
            mycanv = WPC;
            Zoom = z;
            Translation = t;
            Maincanv = MainCanvas;
            dot = new Ellipse();
            dot.Width = 5 * DPI / 43;
            dot.Height = 5 * DPI / 43;
            dot.Fill = b;

            Location = loc;
            PointLocations.Add(Location);
            mycanv.Children.Add(dot);


        }



        public Point Location
        {
            get { return _Location; }
            set
            {
                _Location = ToWayPointCanvas(value);
                Canvas.SetTop(dot, Location.Y);
                Canvas.SetLeft(dot, Location.X);
            }
        }

        public Point ToWayPointCanvas(Point p)
        {
            return new Point((p.X - Maincanv.ActualWidth / 2 + mycanv.Width * Zoom.ScaleX / 2) / Zoom.ScaleX - Translation.X - dot.Width / 2,
                                       (p.Y - Maincanv.ActualHeight / 2 + mycanv.Height * Zoom.ScaleY / 2) / Zoom.ScaleY - Translation.Y - dot.Height / 2);
        }


    }
}
