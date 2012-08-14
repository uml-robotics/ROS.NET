using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace DREAMPioneer
{
    class RobotColor
    {
        
        public int RobotNumber;   //the number of the robot using this color -1 if noone is
        public Brush Color;
        

        static Random r = new Random();
        static List<Brush> Bright_Colors = new List<Brush>()
             {
             {Brushes.LimeGreen}, {Brushes.Red}, {Brushes.Fuchsia}, {Brushes.Orange},
             {Brushes.White}, {Brushes.DeepPink}, {Brushes.DodgerBlue}, {Brushes.LightSeaGreen}
             };
        public static List<RobotColor> ColorInUse = new List<RobotColor>();

        static RobotColor()
        {
            foreach (Brush b in Bright_Colors)
                ColorInUse.Add(new RobotColor(b));
        }
        RobotColor(Brush b)
        {
            RobotNumber = -1;
            Color = b;
        }
        public static Brush getMyColor(int num)
        {
            foreach (RobotColor RC in ColorInUse)
                if (RC.RobotNumber == num)
                {
                    return RC.Color;
                }
            return getNextColor(num) ;
        }
        public static Brush getNextColor(int num)
        {
            foreach (RobotColor RC in ColorInUse)
                if (RC.RobotNumber == -1)
                {
                    RC.RobotNumber = num;
                    return RC.Color;
                }
            ColorInUse.Add(new RobotColor(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xFF,
            (byte)(55 + r.Next(200)),
            (byte)(55 + r.Next(200)),
            (byte)(55 + r.Next(200))))));

            ColorInUse.Last().RobotNumber = num;
            return ColorInUse.Last().Color;
        }

        public static void freeMe(int index)
        { 
            foreach(RobotColor RC in ColorInUse)
                if (RC.RobotNumber == index)
                {
                    RC.RobotNumber = -1;
                    SurfaceWindow1.current.ROSStuffs[index].myRobot.robot.ChangeIconColors(
                    SurfaceWindow1.current.ROSStuffs[index].myRobot.robot.circles.IndexOf(
                    SurfaceWindow1.current.ROSStuffs[index].myRobot.robot.Border.Stroke));
                    if (SurfaceWindow1.current.ROSStuffs.ContainsKey(index))
                        SurfaceWindow1.current.ROSStuffs[index].myRobot.robot.ChangeIconColors(
                            SurfaceWindow1.current.ROSStuffs[index].myRobot.robot.circles.IndexOf(
                            SurfaceWindow1.current.ROSStuffs[index].myRobot.robot.Border.Stroke));
                    return;
                }
        }
        public static void freeAll()
        {
            foreach (RobotColor RC in ColorInUse)
                RC.RobotNumber = -1;
            for (int i = 0; i < ColorInUse.Count; i++)
            {
                ColorInUse[i].RobotNumber = -1;
                if (SurfaceWindow1.current.ROSStuffs.ContainsKey(i))
                    SurfaceWindow1.current.ROSStuffs[i].myRobot.robot.ChangeIconColors(
                        SurfaceWindow1.current.ROSStuffs[i].myRobot.robot.circles.IndexOf(
                        SurfaceWindow1.current.ROSStuffs[i].myRobot.robot.Border.Stroke));
            }
        }
        


    }
}
