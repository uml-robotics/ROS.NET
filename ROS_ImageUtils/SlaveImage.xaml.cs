#region

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Point = System.Drawing.Point;
using Size = System.Windows.Size;
using d = System.Drawing;
using Messages;
using Messages.custom_msgs;
using Ros_CSharp;
using XmlRpc_Wrapper;
using Int32 = Messages.std_msgs.Int32;
using PixelFormat = System.Windows.Media.PixelFormat;
using String = Messages.std_msgs.String;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using m = Messages.std_msgs;
using gm = Messages.geometry_msgs;
using nm = Messages.nav_msgs;
using sm = Messages.sensor_msgs;
using System.Linq;
#endregion

namespace ROS_ImageWPF
{
    /// <summary>
    ///   A general Surface WPF control for the displaying of bitmaps
    /// </summary>
    /// 

    public partial class SlaveImage : UserControl
    {
        public SolidColorBrush ColorConverter(Messages.std_msgs.ColorRGBA c)
        {
            return new SolidColorBrush(Color.FromArgb(128, (byte)Math.Round((double)c.r), (byte)Math.Round((double)c.g), (byte)Math.Round((double)c.b)));
        }
        public System.Windows.Shapes.Rectangle DrawABox(System.Windows.Point topleft, double width, double height, double imgwidth, double imgheight, Messages.std_msgs.ColorRGBA color)
        {
            System.Windows.Point tl = new System.Windows.Point(topleft.X * ActualWidth / imgwidth, topleft.Y * ActualHeight / imgheight);
            System.Windows.Point br = new System.Windows.Point((topleft.X + width) * ActualWidth / imgwidth, (topleft.Y + height) * ActualHeight / imgheight); ;
            System.Windows.Shapes.Rectangle r = new System.Windows.Shapes.Rectangle() { Width = br.X - tl.X, Height = br.Y - tl.Y, Stroke = Brushes.White, Fill = ColorConverter(color), StrokeThickness = 1, Opacity = 1.0 };
            r.SetValue(Canvas.LeftProperty, (object)tl.X);
            r.SetValue(Canvas.TopProperty, (object)tl.Y);
            ROI_Container.Children.Add(r);
            return r;
        }

        public bool EraseABox(System.Windows.Shapes.Rectangle r)
        {
            if (ROI_Container.Children.Contains(r))
            {
                ROI_Container.Children.Remove(r);
                return true;
            }
            return false;
        }

        public void UpdateImage(ref byte[] data)
        {
            guts.UpdateImage(ref data);
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref = "ImageControl" /> class. 
        ///   constructor... nothing fancy
        /// </summary>
        public SlaveImage()
        {
            InitializeComponent();
        }

        #region Events

        private const double _scalex = -1;
        private const double _scaley = 1;

        /// <summary>
        ///   when going from a System.Drawing.Bitmap's byte array, throwing a bmp file header on it, and sticking it in a BitmapImage with a MemoryStream,
        ///   the image gets flipped upside down from how it would look in a  PictureBox in a Form, so this transform corrects that inversion
        /// </summary>
        /// <param name = "sender">
        ///   The sender.
        /// </param>
        /// <param name = "e">
        ///   The e.
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

        public void Transform(double scalex, double scaley)
        {
            guts.Transform(scalex, scaley);
        }
    }
}