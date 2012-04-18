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
using System.Windows.Controls;
using DREAMController;
using GenericTouchTypes;

#endregion

namespace DREAMPioneer
{
    /// <summary>
    ///   Interaction logic for RightControlPanel.xaml
    /// </summary>
    public partial class LeftControlPanel : ControlPanel
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref = "LeftControlPanel" /> class.
        /// </summary>
        public LeftControlPanel()
        {
            Init(new HitTestDelegate(hitTest), owns, slowcheck, dolayout, cleanup);
            InitializeComponent();
        }

        // Timer sugaryCereal = null;
        /// <summary>
        ///   The dolayout.
        /// </summary>
        /// <param name = "relativeto">
        ///   The relativeto.
        /// </param>
        /// <param name = "width">
        ///   The width.
        /// </param>
        /// <param name = "height">
        ///   The height.
        /// </param>
        public void dolayout(Canvas relativeto, double width, double height)
        {
            //Console.WriteLine("LEFT = " + width + " x " + height);
            Width = width;
            Height = height;
            newRangeCanvas.Width = width;
            newRangeCanvas.Height = height;
        }

        /// <summary>
        ///   The slowcheck.
        /// </summary>
        /// <param name = "e">
        ///   The e.
        /// </param>
        /// <returns>
        ///   The slowcheck.
        /// </returns>
        public bool slowcheck(Touch e)
        {
            ViewportMD touched = PointHelper.TryFindFromPoint<ViewportMD>(this, e.Position);
            if (touched != null)
            {
                Console.WriteLine("YAY YOU TOUCHED IT!");
                return true;
            }
            return false;
        }

        /// <summary>
        ///   The cleanup.
        /// </summary>
        public void cleanup()
        {
        }

        /// <summary>
        ///   The hit test.
        /// </summary>
        /// <param name = "e">
        ///   The e.
        /// </param>
        public void hitTest(Touch e)
        {
        }

        /// <summary>
        ///   The owns.
        /// </summary>
        /// <param name = "Id">
        ///   The id.
        /// </param>
        /// <returns>
        ///   The owns.
        /// </returns>
        public bool owns(int Id)
        {
            return false;
        }
    }
}