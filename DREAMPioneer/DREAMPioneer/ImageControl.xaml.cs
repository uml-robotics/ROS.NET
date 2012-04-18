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

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using d = System.Drawing;
using Point = System.Drawing.Point;
using Size = System.Windows.Size;

#endregion

namespace DREAMPioneer
{
    /// <summary>
    ///   A general Surface WPF control for the displaying of bitmaps
    /// </summary>
    public partial class ImageControl : UserControl
    {
        #region variables and such

        /// <summary>
        ///   54 byte bitmap file header to be stuck on the front of every byte array from the blimp
        /// </summary>
        private byte[] header;

        /// <summary>
        ///   Used to update the header (if it's needed) if the size of the image is different than the one used to make the header
        /// </summary>
        private Size lastSize;

        #endregion

        #region UpdateImage overloads that will take a byte[] (with or without header), a System.Drawing.Bitmap, or a System.Windows.Media.whatever.BitmapImage

        /// <summary>
        ///   Looks up the bitmaps dress, then starts passing the image around as a Byte[] and a System.Media.Size to the overloaded UpdateImages that make this work
        /// </summary>
        /// <param name = "bmp">
        /// </param>
        public void UpdateImage(Bitmap bmp)
        {
            try
            {
                // look up the image's dress
                BitmapData bData = bmp.LockBits(new Rectangle(new Point(), bmp.Size),
                                                d.Imaging.ImageLockMode.ReadOnly,
                                                d.Imaging.PixelFormat.Format24bppRgb);

// d.Imaging.PixelFormat.Format32bppArgb);
                int byteCount = bData.Stride*bmp.Height;
                byte[] rgbData = new byte[byteCount];

                // turn the bitmap into a byte[]
                Marshal.Copy(bData.Scan0, rgbData, 0, byteCount);
                bmp.UnlockBits(bData);

                // starts the overload cluster-fuck to show the image
                UpdateImage(rgbData, SizeConverter(bmp.Size), false);

                // get that shit out of memory so it doesn't fuck our day up.
                bmp.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        ///   same as the one above, but allows for 3 bpp bitmaps to be drawn without failing... like the surroundings.
        /// </summary>
        /// <param name = "bmp">
        /// </param>
        /// <param name = "bpp">
        /// </param>
        public void UpdateImage(Bitmap bmp, int bpp)
        {
            if (bpp == 4)
            {
                UpdateImage(bmp);
                return;
            }
            else if (bpp == 3)
            {
                try
                {
                    // look up the image's dress
                    BitmapData bData = bmp.LockBits(new Rectangle(new Point(), bmp.Size),
                                                    d.Imaging.ImageLockMode.ReadOnly,
                                                    d.Imaging.PixelFormat.Format24bppRgb);
                    int byteCount = bData.Stride*bmp.Height;
                    byte[] rgbData = new byte[byteCount];

                    // turn the bitmap into a byte[]
                    Marshal.Copy(bData.Scan0, rgbData, 0, byteCount);
                    bmp.UnlockBits(bData);

                    // starts the overload cluster-fuck to show the image
                    UpdateImage(rgbData, SizeConverter(bmp.Size), false);

                    // get that shit out of memory so it doesn't fuck our day up.
                    bmp.Dispose();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            else
                Console.WriteLine("FUCK YOUR BPP!");
        }

        /// <summary>
        ///   if hasHeader is true, then UpdateImage(byte[]) is called
        ///   otherwise, the size is compared to lastSize, 
        ///   if they differ or header is null, a header is created, and concatinated with data, then UpdateImage(byte[]) is called
        /// </summary>
        /// <param name = "data">
        ///   image data
        /// </param>
        /// <param name = "size">
        ///   image size
        /// </param>
        /// <param name = "hasHeader">
        ///   whether or not a header needs to be concatinated
        /// </param>
        public void UpdateImage(byte[] data, Size size, bool hasHeader)
        {
            if (hasHeader)
            {
                UpdateImage(data);
                return;
            }

            if (data != null)
            {
                // make a bitmap file header if we don't already have one
                if (header == null || size != lastSize)
                    MakeHeader(data, size);

                // stick it on the bitmap data
                byte[] wholearray = concat(header, data);

                try
                {
                    UpdateImage(wholearray);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        /// <summary>
        ///   Uses a memory stream to turn a byte array into a BitmapImage via helper method, BytesToImage, then passes the image to UpdateImage(BitmapImage)
        /// </summary>
        /// <param name = "data">
        /// </param>
        public void UpdateImage(byte[] data)
        {
            try
            {
                BitmapImage img = BytesToImage(data);
                if (img == null)
                {
                    UpdateImage(lastgood);
                }
                else if (img.DecodePixelWidth != -1)
                    UpdateImage(img);
            }
            catch (Exception e) { }
        }

        /// <summary>
        ///   just throws the image into the ImageBrush's ImageSource
        /// </summary>
        /// <param name = "img">
        /// </param>
        public void UpdateImage(BitmapImage img)
        {
            if (img != null)
            {
                image.ImageSource = img;
            }
        }

        /// <summary>
        ///   turns a System.Drawing.Size into the WPF double,double version
        /// </summary>
        /// <param name = "s">
        /// </param>
        /// <returns>
        /// </returns>
        public static Size SizeConverter(System.Drawing.Size s)
        {
            return new Size(s.Width, s.Height);
        }

        /// <summary>
        ///   takes a byte array (NEEDS TO HAVE HAD THE HEADER APPENDED PREVIOUSLY) and turns it into a WPF BitmapImage
        /// </summary>
        /// <param name = "data">
        ///   duh
        /// </param>
        /// <returns>
        ///   duh
        /// </returns>
        static byte[] lastgood;
        public static BitmapImage BytesToImage(byte[] data)
        {
            // makes a memory stream with the data
            MemoryStream ms = new MemoryStream(data);

            // makes an image
            BitmapImage img = new BitmapImage();

            try
            {
                // tries to turn the memory stream into an image
                img.BeginInit();
                img.StreamSource = ms;
                img.EndInit();
                lastgood = new byte[data.Length];
                data.CopyTo(lastgood, 0);
            }
            catch (Exception e)
            {
                return null;
            }

            return img;
        }

        #endregion

        #region helper methods

        /// <summary>
        ///   makes a file header with appropriate width, height, length, and such for the bytearray + drawing.size sent as members of the lazycopypastefixerupper
        /// </summary>
        /// <param name = "rawdata">
        ///   The rawdata.
        /// </param>
        /// <param name = "size">
        ///   The size.
        /// </param>
        private void MakeHeader(byte[] rawdata, Size size)
        {
            lastSize = size;
            int length = rawdata.Length;
            int wholelength = rawdata.Length + 54;
            int width = (int) size.Width;
            int height = (int) size.Height;

// Console.WriteLine("width= " + width + "\nheight= " + height + "\nlength=" + length + "\nbpp= " + (length / (width * height)));
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
            //Console.WriteLine(bpp);
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
        ///   takes 1 byte array, and 1 byte array, and then returns 1 byte array
        /// </summary>
        /// <param name = "a">
        /// </param>
        /// <param name = "b">
        /// </param>
        /// <returns>
        /// </returns>
        private static byte[] concat(byte[] a, byte[] b)
        {
            byte[] result = new byte[a.Length + b.Length];
            System.Array.Copy(a, result, a.Length);
            System.Array.Copy(b, 0, result, a.Length, b.Length);
            return result;
        }

        #endregion

        /// <summary>
        ///   Initializes a new instance of the <see cref = "ImageControl" /> class. 
        ///   constructor... nothing fancy
        /// </summary>
        public ImageControl()
        {
            InitializeComponent();
        }

        #region Events

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
        private void SurfaceUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // image.Transform = new ScaleTransform(1, -1, ActualWidth / 2, ActualHeight / 2);
        }

        #endregion
    }
}