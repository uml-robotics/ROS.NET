#region USINGZ
using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using DREAMController;
using GenericTouchTypes;
using GenericTypes_Surface_Adapter;
using Microsoft.Surface;
using Microsoft.Surface.Presentation;
using Microsoft.Surface.Presentation.Controls;
using Vector3 = System.Windows.Media.Media3D.Vector3D;
using F = System.Windows.Forms;

#if DEMOROS
using Ros_CSharp;
using Messages;
#endif
#endregion

namespace DREAMPioneer
{
    public class CommonList
    {
        public List<Point> P_List = new List<Point>();
        public IEnumerable<Robot_Info> RoboInfo
        {
            get
            {
                return RobotInfowned.Values;
            }
        }
        public Dictionary<int,Robot_Info> RobotInfowned = new Dictionary<int, Robot_Info>();
        public List<GoalDot> Dots = new List<GoalDot>();

        public CommonList()
        { Dots = new List<GoalDot>(); }

        public CommonList(List<Point> Point_List)
        {
            P_List = Point_List;
            Dots = new List<GoalDot>();
        }
    }
}


/*lock (CLists)
            {
                foreach (CommonList CL in CLists)
                    if (CL.Robots.Contains(r))
                    {
                        Console.WriteLine(PList.Count + "\n" + CL.GDList.Count);
                        int index = CL.Robots.IndexOf(r);
                        CL.PointsLeft[index] = PList.Count - 1 ;
                        
                        //foreach (int i in CL.PointsLeft)
                        //{
                        //    if (i > CL.PointsLeft[index])
                        //        CL.Position[index]++;
                        //}
                        
                        if (CL.Position[index] == 0)
                        {
                            for (int i = CL.GDList.Count - CL.PointsLeft[index]; i < CL.GDList.Count; i++)
                            {
                                CL.GDList[i].NextC1.Fill = GoalDot.ColorInUse[r];
                                CL.GDList[i].BeenThereC2.Fill = GoalDot.ColorInUse[r];
                            }
                        }
                        else if (CL.Position[index] == CL.Position.Count - 1)
                        {
                            for (int i = 0; i < CL.GDList.Count - CL.PointsLeft[CL.Position.IndexOf(CL.Position[index-1])]+1; i++)
                            {
                                CL.GDList[i].NextC1.Fill = GoalDot.ColorInUse[r];
                                CL.GDList[i].BeenThereC2.Fill = GoalDot.ColorInUse[r];
                            }
                        }
                        else
                            for (int i = CL.GDList.Count - CL.PointsLeft[index]; i < CL.GDList.Count - CL.PointsLeft[CL.Position.IndexOf(CL.Position[index-1])]+1; i++)
                            {
                                CL.GDList[i].NextC1.Fill = GoalDot.ColorInUse[r];
                                CL.GDList[i].BeenThereC2.Fill = GoalDot.ColorInUse[r];
                            }

                        
                    }
            }
 
 
 if (selectedList.Count > 1)
            {
                foreach (Point p in p_list)
                    TmpGDList.Add(new GoalDot(WayPointCanvas, p, joymgr.DPI, MainCanvas, ZOOM, Translation, Brushes.Yellow));
                CLists.Add(new CommonList(TmpGDList, selectedList));
            }
 */