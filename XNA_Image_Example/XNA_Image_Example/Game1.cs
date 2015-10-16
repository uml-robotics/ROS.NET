using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ros_CSharp;
using Color = Microsoft.Xna.Framework.Color;
using Game = Microsoft.Xna.Framework.Game;
using Image = Messages.sensor_msgs.Image;
using Matrix = Microsoft.Xna.Framework.Matrix;
using Point = Microsoft.Xna.Framework.Point;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace WindowsGameTest
{
    public class TheGame : Game
    {
        private Quad[] quad;
        private Texture2D texture;
        private Texture2D next_texture;
        private BasicEffect effect;
        private Camera camera;

        #region ROS stuff

        private Mutex padlock = new Mutex();
        private NodeHandle nh;
        private Subscriber<Messages.sensor_msgs.Image> imgSub;
        private TextureUtils util = new TextureUtils();

        #endregion

        private GraphicsDeviceManager manager;
        public TheGame() : base()
        {
            manager = new GraphicsDeviceManager(this);
        }

        protected override void Initialize()
        {
            quad = new Quad[6];
            quad[0] = new Quad(Vector3.Backward, Vector3.Backward, Vector3.Up, 2, 2);
            quad[1] = new Quad(Vector3.Left, Vector3.Left, Vector3.Up, 2, 2);
            quad[2] = new Quad(Vector3.Right, Vector3.Right, Vector3.Up, 2, 2);
            quad[3] = new Quad(Vector3.Forward, Vector3.Forward, Vector3.Up, 2, 2);
            quad[4] = new Quad(Vector3.Down, Vector3.Down, Vector3.Right, 2, 2);
            quad[5] = new Quad(Vector3.Up, Vector3.Up, Vector3.Left, 2, 2);
            nh = new NodeHandle();
            imgSub = nh.subscribe<Messages.sensor_msgs.Image>("/camera/rgb/image_rect_color", 1, (img) =>
                                                                    {
                                                                        if (padlock.WaitOne(10))
                                                                        {
                                                                            if (next_texture == null)
                                                                            {
                                                                                next_texture = new Texture2D(GraphicsDevice, (int) img.width, (int) img.height);
                                                                            }
                                                                            util.UpdateImage(GraphicsDevice, img.data, new Size((int)img.width, (int)img.height), ref next_texture, img.encoding.data);
                                                                            padlock.ReleaseMutex();
                                                                        }
                                                                    });

            base.Initialize();
        }

        protected override void LoadContent()
        {
            // Initialize the BasicEffect
            effect = new BasicEffect(GraphicsDevice);

            camera = new Camera(this, new Vector3(0, 0, 5), Vector3.Zero, Vector3.Up);
            Components.Add(camera);
            base.LoadContent();
        }

        protected override void UnloadContent()
        {
            base.UnloadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            if (padlock.WaitOne(10))
            {
                if (next_texture != null)
                {
                    if (texture != null)
                    {
                        effect.Texture.Dispose();
                        effect.Texture = null;
                        texture.Dispose();
                        texture = null;
                    }
                    texture = next_texture;
                    next_texture = null;
                    effect.Texture = texture;
                    effect.TextureEnabled = true;
                }
                padlock.ReleaseMutex();
            }
            
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            float amount = 0.5f+(float)Math.Sin((double)gameTime.TotalGameTime.TotalSeconds / 4.0)/2f;

            effect.World = Matrix.CreateRotationX(MathHelper.Lerp(-(float)Math.PI, (float)Math.PI, amount))*
                           Matrix.CreateRotationY(MathHelper.Lerp(-(float)Math.PI, (float)Math.PI, amount)) *
                           Matrix.CreateRotationZ(MathHelper.Lerp(-(float)Math.PI, (float)Math.PI, amount));

            effect.View = camera.view;
            effect.Projection = camera.projection;

            // Begin effect and draw for each pass
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                for(int i=0;i<quad.Length;i++)
                    GraphicsDevice.DrawUserIndexedPrimitives<VertexPositionNormalTexture>(PrimitiveType.TriangleList,quad[i].Vertices, 0, 4,quad[i].Indexes, 0, 2);
            }
            base.Draw(gameTime);
        }
    }

    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    public class Camera : GameComponent
    {
        public Matrix view { get; protected set; }
        public Matrix projection { get; protected set; }

        public Camera(Game g, Vector3 position, Vector3 target, Vector3 up)
            : base(g)
        {
            view = Matrix.CreateLookAt(position, target, up);
            projection = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.PiOver4,
            (float)g.Window.ClientBounds.Width / g.Window.ClientBounds.Height,
            1, 100);
        }

        /// <summary>
        /// Called when the GameComponent needs to be updated. Override this method with component-specific update code.
        /// </summary>
        /// <param name="gameTime">Time elapsed since the last call to Update</param>
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }
    }

    public class Quad
    {
        public VertexPositionNormalTexture[] Vertices;
        public short[] Indexes;
        private Vector3 Origin, Normal, Up, Left, UpperLeft, UpperRight, LowerLeft, LowerRight;

        public Quad(Vector3 origin, Vector3 normal, Vector3 up, float width, float height)
        {
            Vertices = new VertexPositionNormalTexture[4];
            Indexes = new short[6];
            Origin = origin;
            Normal = normal;
            Up = up;

            // Calculate the quad corners
            Left = Vector3.Cross(normal, Up);
            Vector3 uppercenter = (Up * height / 2) + origin;
            UpperLeft = uppercenter + (Left * width / 2);
            UpperRight = uppercenter - (Left * width / 2);
            LowerLeft = UpperLeft - (Up * height);
            LowerRight = UpperRight - (Up * height);

            FillVertices();
        }

        private void FillVertices()
        {
            // Fill in texture coordinates to display full texture
            // on quad
            Vector2 textureUpperLeft = new Vector2( 0.0f, 0.0f );
            Vector2 textureUpperRight = new Vector2( 1.0f, 0.0f );
            Vector2 textureLowerLeft = new Vector2( 0.0f, 1.0f );
            Vector2 textureLowerRight = new Vector2( 1.0f, 1.0f );

            // Provide a normal for each vertex
            for (int i = 0; i < Vertices.Length; i++)
            {
                Vertices[i].Normal = Normal;
            }
                        // Set the position and texture coordinate for each
            // vertex
            Vertices[0].Position = LowerLeft;
            Vertices[0].TextureCoordinate = textureLowerLeft;
            Vertices[1].Position = UpperLeft;
            Vertices[1].TextureCoordinate = textureUpperLeft;
            Vertices[2].Position = LowerRight;
            Vertices[2].TextureCoordinate = textureLowerRight;
            Vertices[3].Position = UpperRight;
            Vertices[3].TextureCoordinate = textureUpperRight;
            // Set the index buffer for each vertex, using
            // clockwise winding
            Indexes[0] = 0;
            Indexes[1] = 1;
            Indexes[2] = 2;
            Indexes[3] = 2;
            Indexes[4] = 1;
            Indexes[5] = 3;
        }
    }

    public class TextureUtils
    {
        /// <summary>
        ///     Looks up the bitmaps dress, then starts passing the image around as a Byte[] and a System.Media.Size to the
        ///     overloaded UpdateImages that make this work
        /// </summary>
        /// <param name="bmp">
        /// </param>
        public void UpdateImage(GraphicsDevice dev, Bitmap bmp, ref Texture2D target)
        {
            try
            {
                // look up the image's dress
                BitmapData bData = bmp.LockBits(new System.Drawing.Rectangle(new System.Drawing.Point(), bmp.Size),
                    ImageLockMode.ReadOnly,
                    PixelFormat.Format24bppRgb);

                // d.Imaging.PixelFormat.Format32bppArgb);
                int byteCount = bData.Stride*bmp.Height;
                byte[] rgbData = new byte[byteCount];

                // turn the bitmap into a byte[]
                Marshal.Copy(bData.Scan0, rgbData, 0, byteCount);
                bmp.UnlockBits(bData);

                // starts the overload cluster-mess to show the image
                UpdateImage(dev, rgbData, SizeConverter(bmp.Size), ref target);

                // get that stuff out of memory so it doesn't mess our day up.
                bmp.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        ///     same as the one above, but allows for 3 bpp bitmaps to be drawn without failing... like the surroundings.
        /// </summary>
        /// <param name="bmp">
        /// </param>
        /// <param name="bpp">
        /// </param>
        public void UpdateImage(GraphicsDevice dev, Bitmap bmp, int bpp, ref Texture2D target)
        {
            if (bpp == 4)
            {
                UpdateImage(dev, bmp, ref target);
            }
            if (bpp == 3)
            {
                try
                {
                    // look up the image's dress
                    BitmapData bData = bmp.LockBits(new System.Drawing.Rectangle(new System.Drawing.Point(), bmp.Size),
                        ImageLockMode.ReadOnly,
                        PixelFormat.Format24bppRgb);
                    int byteCount = bData.Stride*bmp.Height;
                    byte[] rgbData = new byte[byteCount];

                    // turn the bitmap into a byte[]
                    Marshal.Copy(bData.Scan0, rgbData, 0, byteCount);
                    bmp.UnlockBits(bData);

                    // starts the overload cluster-mess to show the image
                    UpdateImage(dev, rgbData, SizeConverter(bmp.Size), ref target);

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
        public void UpdateImage(GraphicsDevice dev, byte[] data, Size size, ref Texture2D target, string encoding = null)
        {
            if (data != null)
            {
                byte[] correcteddata;
                switch (encoding)
                {
                    case "rgb8":
                        correcteddata = new byte[(int) Math.Round(4d*data.Length/3d)];
                        for (int i = 0, ci = 0; i < data.Length; i += 3, ci += 4)
                        {
                            correcteddata[ci] = data[i];
                            correcteddata[ci+1] = data[i+1];
                            correcteddata[ci+2] = data[i+2];
                            correcteddata[ci+3] = 0xFF;
                        }
                        break;
                    default:
                        throw new Exception("Unhandled texture conversion input, " + encoding);
                        break;
                }

                // stick it on the bitmap data
                try
                {
                    UpdateImage(dev, correcteddata, ref target);
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
        public void UpdateImage(GraphicsDevice dev, byte[] data, ref Texture2D target)
        {
            int[] imgData = new int[target.Width * target.Height];
            if (data.Length/4 != imgData.Length)
                throw new Exception("Invalid input data size! Texture data must be RGBA w/32bpp");
            IntPtr addr = Marshal.AllocHGlobal(data.Length);
            Marshal.Copy(data, 0, addr, data.Length);
            Marshal.Copy(addr, imgData, 0, imgData.Length);
            target.SetData(imgData);
            Marshal.FreeHGlobal(addr);
            imgData = null;
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
    }
}
