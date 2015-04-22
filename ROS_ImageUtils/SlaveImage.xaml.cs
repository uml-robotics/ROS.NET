// File: SlaveImage.xaml.cs
// Project: ROS_ImageWPF
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 11/06/2013
// Updated: 07/23/2014

#region USINGZ

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using d = System.Drawing;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;
using sm = Messages.sensor_msgs;

#endregion

namespace ROS_ImageWPF
{
    /// <summary>
    ///     A general Surface WPF control for the displaying of bitmaps
    /// </summary>
    public partial class SlaveImage : UserControl
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ImageControl" /> class.
        ///     constructor... nothing fancy
        /// </summary>
        public SlaveImage()
        {
            InitializeComponent();
        }

        #region Events

        private const double _scalex = -1;
        private const double _scaley = 1;

        /// <summary>
        ///     when going from a System.Drawing.Bitmap's byte array, throwing a bmp file header on it, and sticking it in a
        ///     BitmapImage with a MemoryStream,
        ///     the image gets flipped upside down from how it would look in a  PictureBox in a Form, so this transform corrects
        ///     that inversion
        /// </summary>
        /// <param name="sender">
        ///     The sender.
        /// </param>
        /// <param name="e">
        ///     The e.
        /// </param>
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Transform(_scalex, _scaley);
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Transform(_scalex, _scaley);
        }

        #endregion

        public SolidColorBrush ColorConverter(m.ColorRGBA c)
        {
            return new SolidColorBrush(Color.FromArgb(128, (byte) Math.Round(c.r), (byte) Math.Round(c.g), (byte) Math.Round(c.b)));
        }

        public Rectangle DrawABox(Point topleft, double width, double height, double imgwidth, double imgheight, m.ColorRGBA color)
        {
            Point tl = new Point(topleft.X*ActualWidth/imgwidth, topleft.Y*ActualHeight/imgheight);
            Point br = new Point((topleft.X + width)*ActualWidth/imgwidth, (topleft.Y + height)*ActualHeight/imgheight);
            ;
            Rectangle r = new Rectangle {Width = br.X - tl.X, Height = br.Y - tl.Y, Stroke = Brushes.White, Fill = ColorConverter(color), StrokeThickness = 1, Opacity = 1.0};
            r.SetValue(Canvas.LeftProperty, tl.X);
            r.SetValue(Canvas.TopProperty, tl.Y);
            ROI_Container.Children.Add(r);
            return r;
        }

        public bool EraseABox(Rectangle r)
        {
            if (ROI_Container.Children.Contains(r))
            {
                ROI_Container.Children.Remove(r);
                return true;
            }
            return false;
        }

        public void UpdateImage(byte[] data)
        {
            guts.UpdateImage(data);
        }

        public void Transform(double scalex, double scaley)
        {
            guts.Transform(scalex, scaley);
        }
    }
}