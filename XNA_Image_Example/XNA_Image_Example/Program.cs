using System;
using Ros_CSharp;

namespace WindowsGameTest
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            ROS.Init(args, "xna_game");
            using (TheGame game = new TheGame())
            {
                game.Run();
            }
        }
    }
#endif
}

