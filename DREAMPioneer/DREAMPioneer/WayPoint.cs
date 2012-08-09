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
    public class Waypoint
    {
        
        private Point _Location;
        public Ellipse dot;
        public Canvas mycanv;
        public Canvas Maincanv;
        public ScaleTransform Zoom;
        public TranslateTransform Translation;
        public static List<Point> _PointLocations = new List<Point>();


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
                _Location = SurfaceWindow1.current.MainCanvas.TranslatePoint(value, mycanv);
                Canvas.SetTop(dot, Location.Y);
                Canvas.SetLeft(dot, Location.X);
                //Console.WriteLine(SurfaceWindow1.current.MainCanvas.TranslatePoint).Transform(new Point()));
            }
        }

        public Point ToWayPointCanvas(Point p)
        {

            return SurfaceWindow1.current.MainCanvas.TranslatePoint(p, mycanv);
        }


    }
}
