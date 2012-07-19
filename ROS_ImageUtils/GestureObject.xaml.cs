#region License Stuff

// Eric McCann - 2011
// University of Massachusetts Lowell
//  
//  
// The DREAMController is intellectual property of the University of Massachusetts lowell, and is patent pending.
//  
// Your rights to distribute, videotape, etc. any works that make use of the DREAMController are entirely contingent on the specific terms of your licensing agreement.
// 
// Feel free to edit any of the supplied samples, or reuse the code in other projects that make use of the DREAMController. They are provided as a resource.
//  
//  
// For license-related questions, contact:
//  	Kerry Lee Andken
//  	kerrylee_andken@uml.edu
//  
// For technical questions, contact:
//  	Eric McCann
//  	emccann@cs.uml.edu
//  	
//  	

#endregion

#region USINGZ

using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using Microsoft.Surface.Presentation.Controls;
using System.Windows.Shapes;
using System.Collections.Generic;

#endregion

namespace ROS_ImageWPF
{
    /// <summary>
    ///   The gesture object.
    /// </summary>
    public partial class GestureObject : SurfaceUserControl
    {
        /// <summary>
        ///   The id.
        /// </summary>
        public int ID = -1;

        public Ellipse getBorder()
        {
            return Border;
        }
        /// <summary>
        ///   The rot.
        /// </summary>
        private RotateTransform rot;

        /// <summary>
        ///   Initializes a new instance of the <see cref = "GestureObject" /> class. 
        ///   This constructor creates the GestureObject with the specified color. It maintains 
        ///   the default height and width of 100 pixels each.
        /// </summary>
        /// <param name = "b">
        ///   New background color..
        /// </param>
        public GestureObject()
        {
            InitializeComponent();
        }

        #region PublicStuff

        /// <summary>
        ///   The arrows.
        /// </summary>
        public List<Brush> arrows = new List<Brush>(new Brush[] {Brushes.Blue, Brushes.Yellow, Brushes.DarkBlue});

        /// <summary>
        ///   The circles.
        /// </summary>
        public List<Brush> circles = new List<Brush>(new Brush[] {Brushes.Black, Brushes.CornflowerBlue, Brushes.LightGreen});

        /// <summary>
        ///   Allows changing the background color for the gesture object.
        /// </summary>
        /// <param name = "b">
        ///   New background color
        /// </param>
        public void SetColor(Brush b)
        {
            Dot.Fill = b;
        }

        /// <summary>
        ///   The change icon colors.
        /// </summary>
        /// <param name = "c">
        ///   The c.
        /// </param>
        public void ChangeIconColors(int c)
        {
            Border.Stroke = circles[c];
            Arrow.Fill = arrows[c];
        }
        public void setArrowColor(Brush b)
        { 
            Arrow.Fill = b;
        }
        /// <summary>
        ///   The set opacity.
        /// </summary>
        /// <param name = "d">
        ///   The d.
        /// </param>
        [System.Diagnostics.DebuggerStepThrough]
        public void SetOpacity(double d)
        {
            Dot.Opacity = d;
        }

        public Brush GetColor()
        {
            return Dot.Fill;
        }

        /// <summary>
        ///   Allows changing the dimensions of the gesture object.
        /// </summary>
        /// <param name = "w">
        ///   New width
        /// </param>
        /// <param name = "h">
        ///   New height
        /// </param>
        public void SetSize(double w, double h)
        {
            DotWidth = w;
            DotHeight = h;
        }

        #endregion

        /// <summary>
        ///   Sets DotWidth.
        /// </summary>
        public double DotWidth
        {
            set
            {
                Width = value + 20;
                Dot.Width = value;
                Border.Width = value + 4;
                Applez.Width = value + 1;
                Arrow.Points =
                    new PointCollection(new[]
                                            {
                                                new Point(MainCanvas.ActualWidth/2, MainCanvas.ActualHeight/2 - Dot.ActualHeight/2 -10),
                                                new Point((MainCanvas.ActualWidth/2) - 5, MainCanvas.ActualHeight/2 - Dot.ActualHeight/2),
                                                new Point((MainCanvas.ActualWidth/2) + 5, MainCanvas.ActualHeight/2 - Dot.ActualHeight/2)
                                            });
                if (rot == null)
                {
                    rot = new RotateTransform();
                    MainCanvas.RenderTransform = rot;
                }

                rot.CenterX = MainCanvas.ActualWidth/2;
                rot.CenterY = MainCanvas.ActualHeight/2;
                Canvas.SetLeft(Dot, MainCanvas.ActualWidth / 2 - Dot.ActualWidth / 2);
                Canvas.SetTop(Dot, MainCanvas.ActualHeight / 2 - Dot.ActualHeight / 2);
                Canvas.SetLeft(Border, MainCanvas.ActualWidth / 2 - Border.ActualWidth / 2);
                Canvas.SetTop(Border, MainCanvas.ActualHeight / 2 - Border.ActualHeight / 2);
                Canvas.SetLeft(Applez, MainCanvas.ActualWidth / 2 - Applez.ActualWidth / 2);
                Canvas.SetTop(Applez, MainCanvas.ActualHeight / 2 - Applez.ActualHeight / 2);
            }
        }

        public void AppleOpacity(double op)
        {
            Applez.Opacity = .7;
        }

        public void Apple(bool shown)
        {
            if (shown)
            {
                Dot.Fill = Brushes.Black;
                SetOpacity(0.8);
            }
            Applez.Visibility = shown ? Visibility.Visible : Visibility.Hidden;
        }

        /// <summary>
        ///   Sets Theta.
        /// </summary>
        public double Theta
        {
            set
            {
                if (rot == null)
                {
                    rot = new RotateTransform();
                    MainCanvas.RenderTransform = rot;
                }

                rot.CenterX = ActualWidth/2;
                rot.CenterY = ActualHeight/2;
                rot.Angle = value;
            }
        }

        /// <summary>
        ///   Sets DotHeight.
        /// </summary>
        public double DotHeight
        {
            set
            {
                Height = value + 20;
                MainCanvas.Height = value + 20;
                Dot.Height = value;
                Border.Height = value + 4;
                Applez.Height = value + 1;
                Canvas.SetLeft(Dot, MainCanvas.ActualWidth / 2 - Dot.ActualWidth / 2);
                Canvas.SetTop(Dot, MainCanvas.ActualHeight / 2 - Dot.ActualHeight / 2);
                Canvas.SetLeft(Border, MainCanvas.ActualWidth / 2 - Border.ActualWidth / 2);
                Canvas.SetTop(Border, MainCanvas.ActualHeight / 2 - Border.ActualHeight / 2);
            }
        }
    }
}