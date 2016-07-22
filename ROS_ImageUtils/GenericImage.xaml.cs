// File: GenericImage.xaml.cs
// Project: ROS_ImageWPF
// 
// ROS.NET
// Eric McCann <emccann@cs.uml.edu>
// UMass Lowell Robotics Laboratory
// 
// Reimplementation of the ROS (ros.org) ros_cpp client in C#.
// 
// Created: 04/28/2015
// Updated: 10/07/2015

#region USINGZ

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Point = System.Drawing.Point;
using Size = System.Windows.Size;

#endregion

namespace ROS_ImageWPF
{
    /// <summary>
    ///     Interaction logic for GenericImage.xaml
    /// </summary>
    public partial class GenericImage : UserControl
    {
        public event FPSEvent fpsevent;
        private int frames;
        private DateTime lastFrame = DateTime.Now;

        public GenericImage()
        {
            InitializeComponent();
            impl = new GenericImageIMPL(image);
        }

        private GenericImageIMPL impl;

        /// <summary>
        ///     Looks up the bitmaps dress, then starts passing the image around as a Byte[] and a System.Media.Size to the
        ///     overloaded UpdateImages that make this work
        /// </summary>
        /// <param name="bmp">
        /// </param>
        public void UpdateImage(Bitmap bmp)
        {
            impl.UpdateImage(bmp);
        }

        /// <summary>
        ///     if hasHeader is true, then UpdateImage(byte[]) is called
        ///     otherwise, the size is compared to lastSize,
        ///     if they differ or header is null, a header is created, and concatinated with data, then UpdateImage(byte[]) is
        ///     called
        /// </summary>
        /// <param name="data">
        ///     image data
        /// </param>
        /// <param name="size">
        ///     image size
        /// </param>
        /// <param name="hasHeader">
        ///     whether or not a header needs to be concatinated
        /// </param>
        public void UpdateImage(byte[] data, Size size, bool hasHeader, string encoding = null)
        {
            impl.UpdateImage(data, size, hasHeader, encoding);
        }

        /// <summary>
        ///     Uses a memory stream to turn a byte array into a BitmapImage via helper method, BytesToImage, then passes the image
        ///     to UpdateImage(BitmapImage)
        /// </summary>
        /// <param name="data">
        /// </param>
        public void UpdateImage(byte[] data)
        {
            impl.UpdateImage(data);
            frames = (frames + 1) % 10;
            if (frames == 0)
            {
                if (fps.Visibility == Visibility.Visible)
                    fps.Content = "" + Math.Round(10.0 / DateTime.Now.Subtract(lastFrame).TotalMilliseconds * 1000.0, 2);
                if (fpsevent != null)
                    fpsevent(Math.Round(10.0 / DateTime.Now.Subtract(lastFrame).TotalMilliseconds * 1000.0, 2));
                lastFrame = DateTime.Now;
            }
        }

        /// <summary>
        /// Directly sets this GenericImage's image based on an already created WPF ImageSource
        /// </summary>
        /// <param name="imgsrc">The image to show on this GenericImage</param>
        public void UpdateImage(ImageSource imgsrc)
        {
            impl.UpdateImage(imgsrc);
        }

        /// <summary>
        /// Apply specified scale transformation to this GenericImage's image (most useful with 1 or -1 to correct inversions)
        /// </summary>
        /// <param name="scalex">X Scale</param>
        /// <param name="scaley">Y Scale</param>
        public void Transform(double scalex, double scaley)
        {
            image.Transform = new ScaleTransform(scalex, scaley, ActualWidth/2, ActualHeight/2);
        }

        /// <summary>
        /// Apply specified scale transformation to this GenericImage's image (most useful with 1 or -1 to correct inversions)
        /// </summary>
        /// <param name="scalex">X Scale</param>
        /// <param name="scaley">Y Scale</param>
        public void Transform(int scalex, int scaley)
        {
            image.Transform = new ScaleTransform(1.0*scalex, 1.0*scaley, ActualWidth/2, ActualHeight/2);
        }

        /// <summary>
        /// Returns a frozen deep copy of this GenericImage's ImageSource
        /// </summary>
        /// <returns>a clone of the ImageSource</returns>
        public ImageSource CloneImage()
        {
            ImageSource ret = image.ImageSource.Clone();
            if (ret.CanFreeze) ret.Freeze();
            return ret;
        }
    }

    public class GenericImageIMPL
    {
        private ImageBrush image;

        /// <summary>
        /// Create a GenericImageIMPL to render images on specified ImageBrush UIElement
        /// </summary>
        /// <param name="ib">Target ImageBrush</param>
        public GenericImageIMPL(ImageBrush ib)
        {
            image = ib;
        }

        #region variables and such

        /// <summary>
        ///     54 byte bitmap file header to be stuck on the front of every byte array from the blimp
        /// </summary>
        private byte[] header;

        /// <summary>
        ///     Used to update the header (if it's needed) if the size of the image is different than the one used to make the
        ///     header
        /// </summary>
        private Size lastSize;

        #endregion

        #region UpdateImage overloads that will take a byte[] (with or without header), a System.Drawing.Bitmap, or a System.Windows.Media.whatever.BitmapImage

        /// <summary>
        ///     Looks up the bitmaps dress, then starts passing the image around as a Byte[] and a System.Media.Size to the
        ///     overloaded UpdateImages that make this work
        /// </summary>
        /// <param name="bmp">
        /// </param>
        public void UpdateImage(Bitmap bmp)
        {
            try
            {
                // look up the image's dress
                BitmapData bData = bmp.LockBits(new Rectangle(new Point(), bmp.Size),
                    ImageLockMode.ReadOnly,
                    PixelFormat.Format24bppRgb);

                // d.Imaging.PixelFormat.Format32bppArgb);
                int byteCount = bData.Stride*bmp.Height;
                byte[] rgbData = new byte[byteCount];

                // turn the bitmap into a byte[]
                Marshal.Copy(bData.Scan0, rgbData, 0, byteCount);
                bmp.UnlockBits(bData);

                // starts the overload cluster-mess to show the image
                UpdateImage(rgbData, SizeConverter(bmp.Size), false);

                // get that stuff out of memory so it doesn't mess our day up.
                bmp.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        ///     if hasHeader is true, then UpdateImage(byte[]) is called
        ///     otherwise, the size is compared to lastSize,
        ///     if they differ or header is null, a header is created, and concatinated with data, then UpdateImage(byte[]) is
        ///     called
        /// </summary>
        /// <param name="data">
        ///     image data
        /// </param>
        /// <param name="size">
        ///     image size
        /// </param>
        /// <param name="hasHeader">
        ///     whether or not a header needs to be concatinated
        /// </param>
        public void UpdateImage(byte[] data, Size size, bool hasHeader, string encoding = null)
        {
            //Console.WriteLine(1 / DateTime.Now.Subtract(wtf).TotalSeconds);
            //wtf = DateTime.Now;
            if (hasHeader)
            {
                UpdateImage(data);
                return;
            }

            if (data != null)
            {
                byte[] correcteddata;
                switch (encoding)
                {
                    case "mono16":
                        correcteddata = new byte[(int) Math.Round(3d*data.Length/2d)];
                        for (int i = 0, ci = 0; i < data.Length; i += 2, ci += 3)
                        {
                            ushort realDepth = (ushort) ((data[i] << 8) | (data[i + 1]));
                            byte pixelcomponent = (byte) Math.Floor(realDepth/255d);
                            correcteddata[ci] = correcteddata[ci + 1] = correcteddata[ci + 2] = pixelcomponent;
                        }
                        break;
                    case "16UC1":
                    {
                        correcteddata = new byte[(int) Math.Round(3d*data.Length/2d)];
                        int[] balls = new int[(int) Math.Round(data.Length/2d)];
                        int maxball = 0;
                        int minball = int.MaxValue;
                        for (int i = 0, ci = 0; i < data.Length; i += 2, ci += 3)
                        {
                            balls[i/2] = (data[i + 1] << 8) | (data[i]);
                            if (balls[i/2] > maxball)
                                maxball = balls[i/2];
                            if (balls[i/2] < minball)
                                minball = balls[i/2];
                        }
                        for (int i = 0, ci = 0; i < balls.Length; i++, ci += 3)
                        {
                            byte intensity = (byte) Math.Round((maxball - (255d*balls[i]/(maxball - minball))));
                            correcteddata[ci] = correcteddata[ci + 1] = correcteddata[ci + 2] = intensity;
                        }
                    }
                        break;
                    case "mono8":
                    case "bayer_grbg8":
                        correcteddata = new byte[3*data.Length];
                        for (int i = 0, ci = 0; i < data.Length; i += 1, ci += 3)
                        {
                            correcteddata[ci] = correcteddata[ci + 1] = correcteddata[ci + 2] = data[i];
                        }
                        break;
                    case "32FC1":
                    {
                        correcteddata = new byte[(int) Math.Round(3d*data.Length/4d)];
                        for (int i = 0, ci = 0; i < data.Length; i += 4, ci += 3)
                        {
                            correcteddata[ci] = correcteddata[ci + 1] = correcteddata[ci + 2] = (byte) Math.Floor(255d*BitConverter.ToSingle(data, i));
                        }
                    }
                        break;
                    default:
                        correcteddata = data;
                        break;
                }
                // make a bitmap file header if we don't already have one
                if (header == null || size != lastSize)
                    MakeHeader(correcteddata, size);

                // stick it on the bitmap data
                try
                {
                    UpdateImage(concat(header, correcteddata));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                data = null;
                correcteddata = null;
            }
        }

        /// <summary>
        ///     Uses a memory stream to turn a byte array into a BitmapImage via helper method, BytesToImage, then passes the image
        ///     to UpdateImage(BitmapImage)
        /// </summary>
        /// <param name="data">
        /// </param>
        public void UpdateImage(byte[] data)
        {
            BitmapImage img = new BitmapImage();
            ImageSource target = image.ImageSource;
            try
            {
                if (BytesToImage(data, ref img) && img.DecodePixelWidth >= 0)
                {
                    target = img;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            image.ImageSource = target;
            img = null;
            target = null;
        }

        /// <summary>
        /// Directly sets this GenericImage's image based on an already created WPF ImageSource
        /// </summary>
        /// <param name="imgsrc">The image to show on this GenericImage</param>
        public void UpdateImage(ImageSource imgsrc)
        {
            image.ImageSource = imgsrc;
        }

        /// <summary>
        ///     turns a System.Drawing.Size into the WPF double,double version
        /// </summary>
        /// <param name="s">
        /// </param>
        /// <returns>
        /// </returns>
        protected static Size SizeConverter(System.Drawing.Size s)
        {
            return new Size(s.Width, s.Height);
        }

        /// <summary>
        /// Attempts to convert the byte array, data, into a valid Image, img
        /// </summary>
        /// <param name="data">image data (ideally)</param>
        /// <param name="img">target image</param>
        /// <returns>true if successful</returns>
        public static bool BytesToImage(byte[] data, ref BitmapImage img)
        {
            bool ret = false;
            if (data.Length > 0)
            {
                // makes a memory stream with the data
                using (MemoryStream ms = new MemoryStream())
                {
                    try
                    {
                        ms.Write(data, 0, data.Length);
                        ms.Seek(0, SeekOrigin.Begin);
                        // tries to turn the memory stream into an image
                        img.BeginInit();
                        img.CacheOption = BitmapCacheOption.OnLoad; //stop being lazy. for serious.
                        img.StreamSource = ms;
                        img.EndInit();
                        ret = true;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                    ms.Close();
                }
            }
            return ret;
        }

        #endregion

        #region helper methods

        /// <summary>
        ///     makes a file header with appropriate width, height, length, and such for the bytearray + drawing.size sent as
        ///     members of the lazycopypastefixerupper
        /// </summary>
        /// <param name="rawdata">
        ///     The rawdata.
        /// </param>
        /// <param name="size">
        ///     The size.
        /// </param>
        private void MakeHeader(byte[] rawdata, Size size)
        {
            lastSize = size;
            int length = rawdata.Length;
            int wholelength = rawdata.Length + 54;
            int width = (int) size.Width;
            int height = (int) size.Height;

            //Console.WriteLine("width= " + width + "\nheight= " + height + "\nlength=" + length + "\nbpp= " + (length / (width * height)));
            byte bitmask = 255;
            header = new byte[54];
            header[0] = (byte) 'B';
            header[1] = (byte) 'M';
            header[2] = (byte) (wholelength & bitmask);
            wholelength = wholelength >> 8;
            header[3] = (byte) (wholelength & bitmask);
            wholelength = wholelength >> 8;
            header[4] = (byte) (wholelength & bitmask);
            wholelength = wholelength >> 8;
            header[5] = (byte) (wholelength & bitmask);

            header[10] = 54;
            header[11] = 0;
            header[12] = 0;
            header[13] = 0;

            header[14] = 40;
            header[15] = 0;
            header[16] = 0;
            header[17] = 0;

            header[18] = (byte) (width & bitmask);
            width = width >> 8;
            header[19] = (byte) (width & bitmask);
            width = width >> 8;
            header[20] = (byte) (width & bitmask);
            width = width >> 8;
            header[21] = (byte) (width & bitmask);

            header[22] = (byte) (height & bitmask);
            height = height >> 8;
            header[23] = (byte) (height & bitmask);
            height = height >> 8;
            header[24] = (byte) (height & bitmask);
            height = height >> 8;
            header[25] = (byte) (height & bitmask);
            header[26] = 1;
            header[27] = 0;
            int bpp = ((int) (Math.Floor(rawdata.Length/(size.Width*size.Height)))*8);
            //Console.WriteLine("BPP = " + bpp);
            header[28] = (byte) bpp;
            header[29] = 0;

            header[30] = 0;
            header[31] = 0;
            header[32] = 0;
            header[33] = 0;

            header[34] = 0x13;
            header[35] = 0x0B;
            header[36] = 0;
            header[37] = 0;

            header[38] = 0x13;
            header[39] = 0x0B;
            header[40] = 0;
            header[41] = 0;

            header[42] = 0;
            header[43] = 0;
            header[44] = 0;
            header[45] = 0;

            header[46] = 0;
            header[47] = 0;
            header[48] = 0;
            header[49] = 0;

            header[50] = (byte) (length & bitmask);
            length = length >> 8;
            header[51] = (byte) (length & bitmask);
            length = length >> 8;
            header[52] = (byte) (length & bitmask);
            length = length >> 8;
            header[53] = (byte) (length & bitmask);
        }

        /// <summary>
        ///     takes 1 byte array, and 1 byte array, and then returns 1 byte array
        /// </summary>
        /// <param name="a">
        /// </param>
        /// <param name="b">
        /// </param>
        /// <returns>
        /// </returns>
        private static byte[] concat(byte[] a, byte[] b)
        {
            byte[] result = new byte[a.Length + b.Length];
            Array.Copy(a, result, a.Length);
            Array.Copy(b, 0, result, a.Length, b.Length);
            return result;
        }

        #endregion
    }

    public delegate void FPSEvent(double fps);
}