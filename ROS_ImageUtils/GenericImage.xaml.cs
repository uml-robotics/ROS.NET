using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Rectangle = System.Windows.Shapes.Rectangle;
using Size = System.Windows.Size;

namespace ROS_ImageWPF
{
    /// <summary>
    /// Interaction logic for GenericImage.xaml
    /// </summary>
    public partial class GenericImage : UserControl
    {
        public GenericImage()
        {
            InitializeComponent();
        }

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
                BitmapData bData = bmp.LockBits(new System.Drawing.Rectangle(new System.Drawing.Point(), bmp.Size),
                                                ImageLockMode.ReadOnly,
                                                System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                // d.Imaging.PixelFormat.Format32bppArgb);
                int byteCount = bData.Stride * bmp.Height;
                byte[] rgbData = new byte[byteCount];

                // turn the bitmap into a byte[]
                Marshal.Copy(bData.Scan0, rgbData, 0, byteCount);
                bmp.UnlockBits(bData);

                // starts the overload cluster-mess to show the image
                UpdateImage(ref rgbData, SizeConverter(bmp.Size), false);

                // get that stuff out of memory so it doesn't mess our day up.
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
                    BitmapData bData = bmp.LockBits(new System.Drawing.Rectangle(new System.Drawing.Point(), bmp.Size),
                                                    ImageLockMode.ReadOnly,
                                                    System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                    int byteCount = bData.Stride * bmp.Height;
                    byte[] rgbData = new byte[byteCount];

                    // turn the bitmap into a byte[]
                    Marshal.Copy(bData.Scan0, rgbData, 0, byteCount);
                    bmp.UnlockBits(bData);

                    // starts the overload cluster-mess to show the image
                    UpdateImage(ref rgbData, SizeConverter(bmp.Size), false);

                    // get that stuff out of memory so it doesn't mess our day up.
                    bmp.Dispose();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            else
                Console.WriteLine("non-fatal BPP mismatch. If you see images, then you should go to vegas and bet your life savings on black.");
        }




        public void UpdateImage(ref byte[] data, Size size, bool hasHeader)
        {
            UpdateImage(ref data, size, hasHeader, null);
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
        public void UpdateImage(ref byte[] data, Size size, bool hasHeader, string encoding)
        {

            //Console.WriteLine(1 / DateTime.Now.Subtract(wtf).TotalSeconds);
            //wtf = DateTime.Now;
            if (hasHeader)
            {
                UpdateImage(ref data);
                return;
            }

            if (data != null)
            {
                byte[] correcteddata;
                switch (encoding)
                {
                    case "mono16":
                        correcteddata = new byte[(int)Math.Round(3d * data.Length / 2d)];
                        for (int i = 0, ci = 0; i < data.Length; i += 2, ci += 3)
                        {
                            ushort realDepth = (ushort)((data[i] << 8) | (data[i + 1]));
                            byte pixelcomponent = (byte)Math.Floor(((double)realDepth) / 255d);
                            correcteddata[ci] = correcteddata[ci + 1] = correcteddata[ci + 2] = pixelcomponent;
                        }
                        break;
                    case "16UC1":
                        {
                            correcteddata = new byte[(int)Math.Round(3d * data.Length / 2d)];
                            int[] balls = new int[(int)Math.Round(data.Length / 2d)];
                            int maxball = 0;
                            int minball = int.MaxValue;
                            for (int i = 0, ci = 0; i < data.Length; i += 2, ci += 3)
                            {
                                balls[i / 2] = (data[i + 1] << 8) | (data[i]);
                                if (balls[i / 2] > maxball)
                                    maxball = balls[i / 2];
                                if (balls[i / 2] < minball)
                                    minball = balls[i / 2];
                            }
                            for (int i = 0, ci = 0; i < balls.Length; i++, ci += 3)
                            {
                                byte intensity = (byte)Math.Round((maxball - (255d * balls[i] / (maxball - minball))));
                                correcteddata[ci] = correcteddata[ci + 1] = correcteddata[ci + 2] = intensity;
                            }
                        }
                        break;
                    case "mono8":
                    case "bayer_grbg8":
                        correcteddata = new byte[3 * data.Length];
                        for (int i = 0, ci = 0; i < data.Length; i += 1, ci += 3)
                        {
                            correcteddata[ci] = correcteddata[ci + 1] = correcteddata[ci + 2] = data[i];
                        }
                        break;
                    case "32FC1":
                        {
                            correcteddata = new byte[(int)Math.Round(3d * data.Length / 4d)];
                            for (int i = 0, ci = 0; i < data.Length; i += 4, ci += 3)
                            {
                                correcteddata[ci] = correcteddata[ci + 1] = correcteddata[ci + 2] = (byte)Math.Floor(255d * BitConverter.ToSingle(data, i));
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
                byte[] wholearray = concat(header, correcteddata);

                try
                {
                    UpdateImage(ref wholearray);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                data = null;
                correcteddata = null;
            }
        }

        DateTime lastFrame = DateTime.Now;
        int frames = 0;

        /// <summary>
        ///   Uses a memory stream to turn a byte array into a BitmapImage via helper method, BytesToImage, then passes the image to UpdateImage(BitmapImage)
        /// </summary>
        /// <param name = "data">
        /// </param>
        public void UpdateImage(ref byte[] data)
        {
            try
            {
                BitmapImage img = BytesToImage(ref data);
                if (img.DecodePixelWidth != -1)
                    UpdateImage(ref img);
                img = null;
            }
            catch (Exception e)
            {
            }

            frames = (frames + 1) % 10;
            if (frames == 0)
            {
                fps.Content = "" + Math.Round(10.0 / DateTime.Now.Subtract(lastFrame).TotalMilliseconds * 1000.0, 2);
                lastFrame = DateTime.Now;
            }
        }

        /// <summary>
        ///   just throws the image into the ImageBrush's ImageSource
        /// </summary>
        /// <param name = "img">
        /// </param>
        public void UpdateImage(ref BitmapImage img)
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
        protected static Size SizeConverter(System.Drawing.Size s)
        {
            return new Size(s.Width, s.Height);
        }

        public static BitmapImage BytesToImage(ref byte[] data)
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
            int width = (int)size.Width;
            int height = (int)size.Height;

            //Console.WriteLine("width= " + width + "\nheight= " + height + "\nlength=" + length + "\nbpp= " + (length / (width * height)));
            byte bitmask = 255;
            header = new byte[54];
            header[0] = (byte)'B';
            header[1] = (byte)'M';
            header[2] = (byte)(wholelength & bitmask);
            wholelength = wholelength >> 8;
            header[3] = (byte)(wholelength & bitmask);
            wholelength = wholelength >> 8;
            header[4] = (byte)(wholelength & bitmask);
            wholelength = wholelength >> 8;
            header[5] = (byte)(wholelength & bitmask);

            header[10] = 54;
            header[11] = 0;
            header[12] = 0;
            header[13] = 0;

            header[14] = 40;
            header[15] = 0;
            header[16] = 0;
            header[17] = 0;

            header[18] = (byte)(width & bitmask);
            width = width >> 8;
            header[19] = (byte)(width & bitmask);
            width = width >> 8;
            header[20] = (byte)(width & bitmask);
            width = width >> 8;
            header[21] = (byte)(width & bitmask);

            header[22] = (byte)(height & bitmask);
            height = height >> 8;
            header[23] = (byte)(height & bitmask);
            height = height >> 8;
            header[24] = (byte)(height & bitmask);
            height = height >> 8;
            header[25] = (byte)(height & bitmask);
            header[26] = 1;
            header[27] = 0;
            int bpp = ((int)(Math.Floor(rawdata.Length / (size.Width * size.Height))) * 8);
            //Console.WriteLine("BPP = " + bpp);
            header[28] = (byte)bpp;
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

            header[50] = (byte)(length & bitmask);
            length = length >> 8;
            header[51] = (byte)(length & bitmask);
            length = length >> 8;
            header[52] = (byte)(length & bitmask);
            length = length >> 8;
            header[53] = (byte)(length & bitmask);
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
            Array.Copy(a, result, a.Length);
            Array.Copy(b, 0, result, a.Length, b.Length);
            return result;
        }

        #endregion

        public void Transform(double scalex, double scaley)
        {
            image.Transform = new ScaleTransform(scalex, scaley, ActualWidth / 2, ActualHeight / 2);
        }
    }
}
