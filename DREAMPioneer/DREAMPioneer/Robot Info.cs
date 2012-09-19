using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Collections.Generic;
using System.Windows;


namespace DREAMPioneer
{
    public class Robot_Info
    {
        public int RoboNum;
        public List<Point> myList;
        public Brush Color;
        private bool _done;
        private int _Position;
        public System.Windows.Point Next = new System.Windows.Point();
        public int Position
        {
            get { return _Position; }
            set { _Position = value; }
        }
        public Robot_Info()
        {}

        public Robot_Info(int robo_num, List<Point>p_list, Brush robo_color, int pos)
        {
            RoboNum = robo_num;
            myList = new List<Point>(p_list);
            Color = robo_color;
            Position = pos;
        }
        public bool done
        {
            get { return _done; }
            set
            {
                _done = value;
                //  bool Done;
                //  foreach (CommonList CL in MarkSurfaceWindow.window.OneInAMillion)
                //  {
                //          Done = true;
                //      foreach(Robot_Info RI in CL.RoboInfo)
                //          if(RI == this)
                //          {
                //          foreach (Robot_Info DoneCheck in CL.RoboInfo)
                //                  if (!DoneCheck.done)
                //                      Done = false;

                //              if (Done)
                //              {
                //                  foreach (GoalDot GD in CL.Dots)
                //                      MarkSurfaceWindow.window.WayPointCanvas.Children.Remove(GD);
                //                  CL.P_List.Clear();
                //                  CL.RoboInfo.Clear();
                //                  lock (MarkSurfaceWindow.window.OneInAMillion)
                //                      MarkSurfaceWindow.window.OneInAMillion.Remove(CL);
                //                  break;
                //              }
                //          }
                //      if (Done) break;
                //  }

                //}  
            }

        }
    }
}
