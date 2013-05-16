/*****************************************************************************/
/* Project  : AviationInstruments                                  */
/* File     : AltimeterInstrumentControl.cs                                  */
/* Version  : 1                                                              */
/* Language : C#                                                             */
/* Summary  : The altimeter instrument control                     */
/* Creation : 16/06/2008                                                     */
/* Autor    : Guillaume CHOUTEAU                                             */
/* History  :                                                                */
/*****************************************************************************/

using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Collections;
using System.Drawing;
using System.Text;
using System.Data;

namespace AviationInstruments
{
    public class AltimeterInstrumentControl : InstrumentControl
    {
        #region Fields

        // Parameters
        int altitude; 

        // Images
        Bitmap bmpCadran = new Bitmap(AviationInstruments.AvionicsInstrumentsControlsRessources.Altimeter_Background);
        Bitmap bmpSmallNeedle = new Bitmap(AviationInstruments.AvionicsInstrumentsControlsRessources.SmallNeedleAltimeter);
        Bitmap bmpLongNeedle = new Bitmap(AviationInstruments.AvionicsInstrumentsControlsRessources.LongNeedleAltimeter);
        Bitmap bmpScroll = new Bitmap(AviationInstruments.AvionicsInstrumentsControlsRessources.Bandeau_Dérouleur);

        #endregion

        #region Contructor

        /// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

        public AltimeterInstrumentControl()
		{
			// Double bufferisation
			SetStyle(ControlStyles.DoubleBuffer | ControlStyles.UserPaint |
				ControlStyles.AllPaintingInWmPaint, true);
        }

        #endregion

        #region Component Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
        }
        #endregion

        #region Paint

        protected override void OnPaint(PaintEventArgs pe)
        {
            // Calling the base class OnPaint
            base.OnPaint(pe);

            // Pre Display computings
            Point ptCounter = new Point(35, 135);
            Point ptRotation = new Point(150, 150);
            Point ptimgNeedle = new Point(136,39);

            bmpCadran.MakeTransparent(Color.Yellow);
            bmpLongNeedle.MakeTransparent(Color.Yellow);
            bmpSmallNeedle.MakeTransparent(Color.Yellow);

            double alphaSmallNeedle = InterpolPhyToAngle(altitude,0,10000,0,359);
            double alphaLongNeedle = InterpolPhyToAngle(altitude%1000,0,1000,0,359);

            float scale = (float)this.Width / bmpCadran.Width;

            // display counter
            ScrollCounter(pe, bmpScroll, 5, altitude, ptCounter, scale);

            // diplay mask
            Pen maskPen = new Pen(this.BackColor, 30 * scale);
            pe.Graphics.DrawRectangle(maskPen, 0, 0, bmpCadran.Width * scale, bmpCadran.Height * scale);

            // display cadran
            pe.Graphics.DrawImage(bmpCadran, 0, 0, (float)(bmpCadran.Width * scale), (float)(bmpCadran.Height * scale));

            // display small needle
            RotateImage(pe, bmpSmallNeedle, alphaSmallNeedle, ptimgNeedle, ptRotation, scale);

            // display long needle
            RotateImage(pe, bmpLongNeedle, alphaLongNeedle, ptimgNeedle, ptRotation, scale);

        }

        #endregion

        #region Methods


        /// <summary>
        /// Define the physical value to be displayed on the indicator
        /// </summary>
        /// <param name="aircraftAltitude">The aircraft altitude in ft</param>
        public void SetAlimeterParameters(int aircraftAltitude)
        {
            altitude = aircraftAltitude;

            this.Refresh();
        }

        #endregion

    }
}
