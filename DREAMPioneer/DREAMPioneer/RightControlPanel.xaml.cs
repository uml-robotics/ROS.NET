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
    public partial class RightControlPanel : ControlPanel
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref = "RightControlPanel" /> class.
        /// </summary>
        public RightControlPanel()
        {
            Init(new HitTestDelegate(hitTest), owns, slowcheck, dolayout, cleanup);
            InitializeComponent();
        }

        public void SetTopic(string name)
        {
            webcam.TopicName = name;
        }

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
            //Console.WriteLine("RIGHT = " + width + " x " + height);
            Width = width;
            Height = height;
            webcam.Width = Width;
            webcam.Height = Height;
        }

        /// <summary>
        ///   The cleanup.
        /// </summary>
        public void cleanup()
        {
            if (webcam != null)
            {
                webcam.shutdown();
                webcam = null;
            }
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
            return false;
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